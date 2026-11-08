using Microsoft.EntityFrameworkCore;
using TaskFlow.Domain;
using TaskFlow.Infrastructure.Persistence;

namespace TaskFlow.Infrastructure.Tests.Persistence;

public sealed class TaskRepositoryTests : IDisposable
{
    private static readonly DateTimeOffset Now = new(2026, 11, 8, 12, 0, 0, TimeSpan.Zero);
    private static readonly DateOnly Due = new(2026, 11, 20);
    private static readonly CancellationToken Ct = CancellationToken.None;

    private readonly SqliteTestDatabase _db = new();
    private readonly User _owner;
    private readonly User _otherUser;

    public TaskRepositoryTests()
    {
        _owner = User.Create("owner@example.com", "hash", Now);
        _otherUser = User.Create("other@example.com", "hash", Now);

        using var context = _db.CreateContext();
        context.Users.AddRange(_owner, _otherUser);
        context.SaveChanges();
    }

    public void Dispose() => _db.Dispose();

    private TaskRepository NewRepository() => new(_db.CreateContext());

    private async Task<TaskItem> AddTaskAsync(Guid userId, string title, DateTimeOffset createdAt, DateOnly? dueDate = null)
    {
        var task = TaskItem.Create(userId, title, "details", dueDate, createdAt);
        await NewRepository().AddAsync(task, Ct);
        return task;
    }

    // ---- AddAsync / GetByIdAsync ----

    [Fact]
    public async Task AddAsync_ThenGetByIdAsync_RoundTripsAllFields()
    {
        var task = TaskItem.Create(_owner.Id, "Write report", "Q4 numbers", Due, Now);

        await NewRepository().AddAsync(task, Ct);
        var loaded = await NewRepository().GetByIdAsync(_owner.Id, task.Id, Ct);

        Assert.NotNull(loaded);
        Assert.Equal(task.Id, loaded.Id);
        Assert.Equal(_owner.Id, loaded.UserId);
        Assert.Equal("Write report", loaded.Title);
        Assert.Equal("Q4 numbers", loaded.Description);
        Assert.Equal(TaskItemStatus.Pending, loaded.Status);
        Assert.Equal(Due, loaded.DueDate);
        Assert.Equal(Now, loaded.CreatedAt);
        Assert.Equal(Now, loaded.UpdatedAt);
    }

    [Fact]
    public async Task GetByIdAsync_TaskOwnedByAnotherUser_ReturnsNull()
    {
        var task = await AddTaskAsync(_owner.Id, "Private", Now);

        var loaded = await NewRepository().GetByIdAsync(_otherUser.Id, task.Id, Ct);

        Assert.Null(loaded);
    }

    [Fact]
    public async Task GetByIdAsync_UnknownId_ReturnsNull()
    {
        var loaded = await NewRepository().GetByIdAsync(_owner.Id, Guid.NewGuid(), Ct);

        Assert.Null(loaded);
    }

    // ---- ListByUserAsync ----

    [Fact]
    public async Task ListByUserAsync_ReturnsOnlyThatUsersTasks()
    {
        var mine = await AddTaskAsync(_owner.Id, "Mine", Now);
        await AddTaskAsync(_otherUser.Id, "Theirs", Now);

        var tasks = await NewRepository().ListByUserAsync(_owner.Id, Ct);

        var single = Assert.Single(tasks);
        Assert.Equal(mine.Id, single.Id);
    }

    [Fact]
    public async Task ListByUserAsync_UserWithoutTasks_ReturnsEmpty()
    {
        await AddTaskAsync(_owner.Id, "Mine", Now);

        var tasks = await NewRepository().ListByUserAsync(_otherUser.Id, Ct);

        Assert.Empty(tasks);
    }

    [Fact]
    public async Task ListByUserAsync_OrdersNewestFirst()
    {
        var oldest = await AddTaskAsync(_owner.Id, "Oldest", Now.AddDays(-2));
        var newest = await AddTaskAsync(_owner.Id, "Newest", Now);
        var middle = await AddTaskAsync(_owner.Id, "Middle", Now.AddDays(-1));

        var tasks = await NewRepository().ListByUserAsync(_owner.Id, Ct);

        Assert.Equal([newest.Id, middle.Id, oldest.Id], tasks.Select(t => t.Id));
    }

    // ---- UpdateAsync ----

    [Fact]
    public async Task UpdateAsync_PersistsChanges()
    {
        var task = await AddTaskAsync(_owner.Id, "Draft", Now, Due);
        var later = Now.AddHours(3);

        // Mirrors TaskService: load and update through the same (scoped) repository.
        var repository = NewRepository();
        var loaded = await repository.GetByIdAsync(_owner.Id, task.Id, Ct);
        Assert.NotNull(loaded);
        loaded.Update("Final", "Done", TaskItemStatus.Completed, null, later);
        await repository.UpdateAsync(loaded, Ct);

        var reloaded = await NewRepository().GetByIdAsync(_owner.Id, task.Id, Ct);
        Assert.NotNull(reloaded);
        Assert.Equal("Final", reloaded.Title);
        Assert.Equal("Done", reloaded.Description);
        Assert.Equal(TaskItemStatus.Completed, reloaded.Status);
        Assert.Null(reloaded.DueDate);
        Assert.Equal(Now, reloaded.CreatedAt);
        Assert.Equal(later, reloaded.UpdatedAt);
    }

    // ---- DeleteAsync ----

    [Fact]
    public async Task DeleteAsync_RemovesTheTask()
    {
        var task = await AddTaskAsync(_owner.Id, "Temporary", Now);

        var repository = NewRepository();
        var loaded = await repository.GetByIdAsync(_owner.Id, task.Id, Ct);
        Assert.NotNull(loaded);
        await repository.DeleteAsync(loaded, Ct);

        Assert.Null(await NewRepository().GetByIdAsync(_owner.Id, task.Id, Ct));
    }

    [Fact]
    public async Task DeleteAsync_LeavesOtherTasksUntouched()
    {
        var keep = await AddTaskAsync(_owner.Id, "Keep", Now);
        var remove = await AddTaskAsync(_owner.Id, "Remove", Now.AddMinutes(1));

        var repository = NewRepository();
        var loaded = await repository.GetByIdAsync(_owner.Id, remove.Id, Ct);
        Assert.NotNull(loaded);
        await repository.DeleteAsync(loaded, Ct);

        var remaining = await NewRepository().ListByUserAsync(_owner.Id, Ct);
        Assert.Equal(keep.Id, Assert.Single(remaining).Id);
    }

    // ---- Integrity ----

    [Fact]
    public async Task AddAsync_TaskForNonExistentUser_ThrowsDbUpdateException()
    {
        var orphan = TaskItem.Create(Guid.NewGuid(), "Orphan", null, null, Now);

        await Assert.ThrowsAsync<DbUpdateException>(() => NewRepository().AddAsync(orphan, Ct));
    }

    // ---- Dates ----

    [Fact]
    public async Task AddAsync_WithoutDueDate_PersistsNullDueDate()
    {
        var task = await AddTaskAsync(_owner.Id, "No deadline", Now, dueDate: null);

        var loaded = await NewRepository().GetByIdAsync(_owner.Id, task.Id, Ct);

        Assert.NotNull(loaded);
        Assert.Null(loaded.DueDate);
    }

    [Fact]
    public async Task AddAsync_PreservesTimestampPrecisionAndOffset()
    {
        // Sub-millisecond precision, UTC offset as produced by TimeProvider.GetUtcNow().
        var precise = new DateTimeOffset(2026, 11, 8, 12, 34, 56, TimeSpan.Zero).AddTicks(1234567);
        var task = await AddTaskAsync(_owner.Id, "Precise", precise);

        var loaded = await NewRepository().GetByIdAsync(_owner.Id, task.Id, Ct);

        Assert.NotNull(loaded);
        Assert.Equal(precise.UtcTicks, loaded.CreatedAt.UtcTicks);
        Assert.Equal(TimeSpan.Zero, loaded.CreatedAt.Offset);
    }

    [Theory]
    [InlineData(2026, 1, 1)]
    [InlineData(2026, 2, 28)]
    [InlineData(2028, 2, 29)]
    [InlineData(2026, 12, 31)]
    public async Task AddAsync_PersistsDueDateExactly(int year, int month, int day)
    {
        var dueDate = new DateOnly(year, month, day);
        var task = await AddTaskAsync(_owner.Id, "Deadline", Now, dueDate);

        var loaded = await NewRepository().GetByIdAsync(_owner.Id, task.Id, Ct);

        Assert.NotNull(loaded);
        Assert.Equal(dueDate, loaded.DueDate);
    }
}
