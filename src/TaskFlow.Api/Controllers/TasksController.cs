using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TaskFlow.Application.Tasks;

namespace TaskFlow.Api.Controllers;

[ApiController]
[Route("api/tasks")]
[Authorize]
[ApiExplorerSettings(GroupName = ApiDocuments.Tasks)]
#pragma warning disable CS9113 // RED stub: the service is used once the controller is implemented.
public sealed class TasksController(ITaskService tasks) : ControllerBase
{
    [HttpGet]
    public Task<ActionResult<IReadOnlyList<TaskResponse>>> GetAll(CancellationToken cancellationToken)
        => throw new NotImplementedException();

    [HttpGet("{id:guid}")]
    public Task<ActionResult<TaskResponse>> GetById(Guid id, CancellationToken cancellationToken)
        => throw new NotImplementedException();

    [HttpPost]
    public Task<ActionResult<TaskResponse>> Create(CreateTaskRequest request, CancellationToken cancellationToken)
        => throw new NotImplementedException();

    [HttpPut("{id:guid}")]
    public Task<ActionResult<TaskResponse>> Update(Guid id, UpdateTaskRequest request, CancellationToken cancellationToken)
        => throw new NotImplementedException();

    [HttpDelete("{id:guid}")]
    public Task<IActionResult> Delete(Guid id, CancellationToken cancellationToken)
        => throw new NotImplementedException();
}
