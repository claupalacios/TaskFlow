using Microsoft.Extensions.Time.Testing;
using NSubstitute;
using TaskFlow.Application.Common;
using TaskFlow.Application.Tasks;
using TaskFlow.Domain;

namespace TaskFlow.Application.Tests.Tasks;

public class TaskServiceTests
{
    private static readonly Guid UserId = Guid.Parse("11111111-1111-1111-1111-111111111111");
    private static readonly Guid OtherUserId = Guid.Parse("22222222-2222-2222-2222-222222222222");
    private static readonly DateTimeOffset Now = new(2026, 11, 8, 12, 0, 0, TimeSpan.Zero);
    private static readonly DateOnly Today = new(2026, 11, 8);
    private static readonly CancellationToken Ct = CancellationToken.None;

    private readonly ITaskRepository _repository = Substitute.For<ITaskRepository>();
    private readonly FakeTimeProvider _clock = new(Now);
    private readonly TaskService _service;

    public TaskServiceTests()
    {
        _service = new TaskService(_repository, _clock);
    }

    private static TaskItem ExistingTask(DateOnly? dueDate = null)
        => TaskItem.Create(UserId, "Write report", "Q4 numbers", dueDate ?? Today.AddDays(5), Now.AddDays(-1));

    private void RepositoryReturns(TaskItem task)
        => _repository.GetByIdAsync(UserId, task.Id, Ct).Returns(task);

    // ---- Create ----

    [Fact]
    public async Task CreateAsync_WithValidRequest_AddsTaskOwnedByCallerAndReturnsIt()
    {
        var request = new CreateTaskRequest("Write report", "Q4 numbers", Today.AddDays(3));

        var response = await _service.CreateAsync(UserId, request, Ct);

        await _repository.Received(1).AddAsync(
            Arg.Is<TaskItem>(t => t.UserId == UserId && t.Title == "Write report" && t.Status == TaskItemStatus.Pending && t.CreatedAt == Now),
            Ct);
        Assert.NotEqual(Guid.Empty, response.Id);
        Assert.Equal("Write report", response.Title);
        Assert.Equal("Q4 numbers", response.Description);
        Assert.Equal(TaskItemStatus.Pending, response.Status);
        Assert.Equal(Today.AddDays(3), response.DueDate);
        Assert.Equal(Now, response.CreatedAt);
    }

    [Fact]
    public async Task CreateAsync_WithDueDateToday_Succeeds()
    {
        var response = await _service.CreateAsync(UserId, new CreateTaskRequest("Title", null, Today), Ct);

        Assert.Equal(Today, response.DueDate);
    }

    [Fact]
    public async Task CreateAsync_WithoutDueDate_Succeeds()
    {
        var response = await _service.CreateAsync(UserId, new CreateTaskRequest("Title", null, null), Ct);

        Assert.Null(response.DueDate);
    }

    [Fact]
    public async Task CreateAsync_WithDueDateInThePast_Succeeds()
    {
        // Overdue deadlines are valid: users may want to record or manage work that is already late.
        var request = new CreateTaskRequest("Title", null, Today.AddDays(-1));

        var response = await _service.CreateAsync(UserId, request, Ct);

        await _repository.Received(1).AddAsync(Arg.Is<TaskItem>(t => t.DueDate == Today.AddDays(-1)), Ct);
        Assert.Equal(Today.AddDays(-1), response.DueDate);
    }

    [Fact]
    public async Task CreateAsync_WithBlankTitle_ThrowsAndDoesNotSave()
    {
        var request = new CreateTaskRequest("  ", null, null);

        await Assert.ThrowsAsync<DomainValidationException>(() => _service.CreateAsync(UserId, request, Ct));

        await _repository.DidNotReceiveWithAnyArgs().AddAsync(default!, default);
    }

    // ---- Read ----

    [Fact]
    public async Task GetAllAsync_ReturnsCallersTasksOnly()
    {
        var mine = ExistingTask();
        _repository.ListByUserAsync(UserId, Ct).Returns([mine]);

        var result = await _service.GetAllAsync(UserId, Ct);

        var single = Assert.Single(result);
        Assert.Equal(mine.Id, single.Id);
        await _repository.DidNotReceive().ListByUserAsync(OtherUserId, Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task GetAllAsync_WhenUserHasNoTasks_ReturnsEmptyList()
    {
        _repository.ListByUserAsync(UserId, Ct).Returns([]);

        var result = await _service.GetAllAsync(UserId, Ct);

        Assert.Empty(result);
    }

    [Fact]
    public async Task GetByIdAsync_WhenTaskExists_ReturnsIt()
    {
        var task = ExistingTask();
        RepositoryReturns(task);

        var response = await _service.GetByIdAsync(UserId, task.Id, Ct);

        Assert.Equal(task.Id, response.Id);
        Assert.Equal("Write report", response.Title);
    }

    [Fact]
    public async Task GetByIdAsync_WhenTaskIsMissingOrBelongsToAnotherUser_ThrowsNotFound()
    {
        // The repository is user-scoped: another user's task comes back as null, same as a missing one.
        var someoneElsesTaskId = Guid.NewGuid();
        _repository.GetByIdAsync(UserId, someoneElsesTaskId, Ct).Returns((TaskItem?)null);

        await Assert.ThrowsAsync<NotFoundException>(() => _service.GetByIdAsync(UserId, someoneElsesTaskId, Ct));
    }

    // ---- Update ----

    [Fact]
    public async Task UpdateAsync_WhenTaskExists_AppliesChangesSavesAndReturnsIt()
    {
        var task = ExistingTask();
        RepositoryReturns(task);
        _clock.Advance(TimeSpan.FromHours(2));
        var request = new UpdateTaskRequest("New title", "New description", TaskItemStatus.InProgress, Today.AddDays(10));

        var response = await _service.UpdateAsync(UserId, task.Id, request, Ct);

        await _repository.Received(1).UpdateAsync(task, Ct);
        Assert.Equal("New title", response.Title);
        Assert.Equal("New description", response.Description);
        Assert.Equal(TaskItemStatus.InProgress, response.Status);
        Assert.Equal(Today.AddDays(10), response.DueDate);
        Assert.Equal(Now.AddHours(2), response.UpdatedAt);
    }

    [Fact]
    public async Task UpdateAsync_PassesCancellationTokenToRepository()
    {
        using var cts = new CancellationTokenSource();
        var task = ExistingTask();
        _repository.GetByIdAsync(UserId, task.Id, cts.Token).Returns(task);
        var request = new UpdateTaskRequest("New title", null, TaskItemStatus.Pending, null);

        await _service.UpdateAsync(UserId, task.Id, request, cts.Token);

        await _repository.Received(1).GetByIdAsync(UserId, task.Id, cts.Token);
        await _repository.Received(1).UpdateAsync(task, cts.Token);
    }

    [Fact]
    public async Task UpdateAsync_AllowsDueDateInThePast()
    {
        // Past due dates are allowed everywhere, so an overdue task can still be edited or completed.
        var task = ExistingTask();
        RepositoryReturns(task);
        var request = new UpdateTaskRequest("Write report", "Q4 numbers", TaskItemStatus.Completed, Today.AddDays(-2));

        var response = await _service.UpdateAsync(UserId, task.Id, request, Ct);

        Assert.Equal(Today.AddDays(-2), response.DueDate);
    }

    [Fact]
    public async Task UpdateAsync_WhenTaskIsMissingOrBelongsToAnotherUser_ThrowsNotFoundAndDoesNotSave()
    {
        var taskId = Guid.NewGuid();
        _repository.GetByIdAsync(UserId, taskId, Ct).Returns((TaskItem?)null);
        var request = new UpdateTaskRequest("Title", null, TaskItemStatus.Pending, null);

        await Assert.ThrowsAsync<NotFoundException>(() => _service.UpdateAsync(UserId, taskId, request, Ct));

        await _repository.DidNotReceiveWithAnyArgs().UpdateAsync(default!, default);
    }

    [Fact]
    public async Task UpdateAsync_WithInvalidData_ThrowsAndDoesNotSave()
    {
        var task = ExistingTask();
        RepositoryReturns(task);
        var request = new UpdateTaskRequest(" ", null, TaskItemStatus.Pending, null);

        await Assert.ThrowsAsync<DomainValidationException>(() => _service.UpdateAsync(UserId, task.Id, request, Ct));

        await _repository.DidNotReceiveWithAnyArgs().UpdateAsync(default!, default);
    }

    // ---- Delete ----

    [Fact]
    public async Task DeleteAsync_WhenTaskExists_DeletesIt()
    {
        var task = ExistingTask();
        RepositoryReturns(task);

        await _service.DeleteAsync(UserId, task.Id, Ct);

        await _repository.Received(1).DeleteAsync(task, Ct);
    }

    [Fact]
    public async Task DeleteAsync_WhenTaskIsMissingOrBelongsToAnotherUser_ThrowsNotFoundAndDoesNotDelete()
    {
        var taskId = Guid.NewGuid();
        _repository.GetByIdAsync(UserId, taskId, Ct).Returns((TaskItem?)null);

        await Assert.ThrowsAsync<NotFoundException>(() => _service.DeleteAsync(UserId, taskId, Ct));

        await _repository.DidNotReceiveWithAnyArgs().DeleteAsync(default!, default);
    }
}
