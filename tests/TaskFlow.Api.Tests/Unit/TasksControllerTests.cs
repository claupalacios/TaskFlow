using Microsoft.AspNetCore.Mvc;
using NSubstitute;
using TaskFlow.Api.Controllers;
using TaskFlow.Application.Tasks;
using TaskFlow.Domain;

namespace TaskFlow.Api.Tests.Unit;

public class TasksControllerTests
{
    private static readonly Guid UserId = Guid.Parse("11111111-1111-1111-1111-111111111111");
    private static readonly Guid TaskId = Guid.Parse("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa");
    private static readonly DateTimeOffset Now = new(2026, 11, 8, 12, 0, 0, TimeSpan.Zero);

    private static readonly TaskResponse Task1 =
        new(TaskId, "Write report", "Q4", TaskItemStatus.Pending, new DateOnly(2026, 11, 20), Now, Now);

    private readonly ITaskService _service = Substitute.For<ITaskService>();
    private readonly TasksController _controller;
    private readonly CancellationToken _ct = new CancellationTokenSource().Token;

    public TasksControllerTests()
    {
        _controller = new TasksController(_service) { ControllerContext = ControllerTestContext.ForUser(UserId) };
    }

    [Fact]
    public async Task GetAll_ReturnsOkWithTheCallersTasks()
    {
        _service.GetAllAsync(UserId, _ct).Returns([Task1]);

        var result = await _controller.GetAll(_ct);

        var ok = Assert.IsType<OkObjectResult>(result.Result);
        Assert.Equal([Task1], Assert.IsAssignableFrom<IReadOnlyList<TaskResponse>>(ok.Value));
    }

    [Fact]
    public async Task GetById_ReturnsOkWithTheTask()
    {
        _service.GetByIdAsync(UserId, TaskId, _ct).Returns(Task1);

        var result = await _controller.GetById(TaskId, _ct);

        var ok = Assert.IsType<OkObjectResult>(result.Result);
        Assert.Equal(Task1, ok.Value);
    }

    [Fact]
    public async Task Create_ReturnsCreatedAtGetByIdWithTheNewTask()
    {
        var request = new CreateTaskRequest("Write report", "Q4", new DateOnly(2026, 11, 20));
        _service.CreateAsync(UserId, request, _ct).Returns(Task1);

        var result = await _controller.Create(request, _ct);

        var created = Assert.IsType<CreatedAtActionResult>(result.Result);
        Assert.Equal(nameof(TasksController.GetById), created.ActionName);
        Assert.Equal(TaskId, created.RouteValues?["id"]);
        Assert.Equal(Task1, created.Value);
    }

    [Fact]
    public async Task Update_ReturnsOkWithTheUpdatedTask()
    {
        var request = new UpdateTaskRequest("Write report", "Q4", TaskItemStatus.Completed, null);
        var updated = Task1 with { Status = TaskItemStatus.Completed, DueDate = null };
        _service.UpdateAsync(UserId, TaskId, request, _ct).Returns(updated);

        var result = await _controller.Update(TaskId, request, _ct);

        var ok = Assert.IsType<OkObjectResult>(result.Result);
        Assert.Equal(updated, ok.Value);
    }

    [Fact]
    public async Task Delete_ReturnsNoContentAndDeletesForTheCaller()
    {
        var result = await _controller.Delete(TaskId, _ct);

        Assert.IsType<NoContentResult>(result);
        await _service.Received(1).DeleteAsync(UserId, TaskId, _ct);
    }

    [Fact]
    public async Task EveryAction_UsesTheUserIdFromTheToken()
    {
        var create = new CreateTaskRequest("t", null, null);
        var update = new UpdateTaskRequest("t", null, TaskItemStatus.Pending, null);
        _service.CreateAsync(default, default!, default).ReturnsForAnyArgs(Task1);
        _service.UpdateAsync(default, default, default!, default).ReturnsForAnyArgs(Task1);
        _service.GetByIdAsync(default, default, default).ReturnsForAnyArgs(Task1);
        _service.GetAllAsync(default, default).ReturnsForAnyArgs([]);

        await _controller.GetAll(_ct);
        await _controller.GetById(TaskId, _ct);
        await _controller.Create(create, _ct);
        await _controller.Update(TaskId, update, _ct);
        await _controller.Delete(TaskId, _ct);

        await _service.Received(1).GetAllAsync(UserId, _ct);
        await _service.Received(1).GetByIdAsync(UserId, TaskId, _ct);
        await _service.Received(1).CreateAsync(UserId, create, _ct);
        await _service.Received(1).UpdateAsync(UserId, TaskId, update, _ct);
        await _service.Received(1).DeleteAsync(UserId, TaskId, _ct);
    }
}
