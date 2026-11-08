using TaskFlow.Application.Common;
using TaskFlow.Domain;

namespace TaskFlow.Application.Tasks;

public sealed class TaskService(ITaskRepository repository, TimeProvider timeProvider) : ITaskService
{
    public async Task<IReadOnlyList<TaskResponse>> GetAllAsync(Guid userId, CancellationToken cancellationToken)
    {
        var tasks = await repository.ListByUserAsync(userId, cancellationToken);

        return tasks.Select(ToResponse).ToList();
    }

    public async Task<TaskResponse> GetByIdAsync(Guid userId, Guid taskId, CancellationToken cancellationToken)
    {
        var task = await GetOwnedTaskAsync(userId, taskId, cancellationToken);

        return ToResponse(task);
    }

    public async Task<TaskResponse> CreateAsync(Guid userId, CreateTaskRequest request, CancellationToken cancellationToken)
    {
        var task = TaskItem.Create(userId, request.Title, request.Description, request.DueDate, timeProvider.GetUtcNow());

        await repository.AddAsync(task, cancellationToken);

        return ToResponse(task);
    }

    public async Task<TaskResponse> UpdateAsync(Guid userId, Guid taskId, UpdateTaskRequest request, CancellationToken cancellationToken)
    {
        var task = await GetOwnedTaskAsync(userId, taskId, cancellationToken);

        task.Update(request.Title, request.Description, request.Status, request.DueDate, timeProvider.GetUtcNow());

        await repository.UpdateAsync(task, cancellationToken);

        return ToResponse(task);
    }

    public async Task DeleteAsync(Guid userId, Guid taskId, CancellationToken cancellationToken)
    {
        var task = await GetOwnedTaskAsync(userId, taskId, cancellationToken);

        await repository.DeleteAsync(task, cancellationToken);
    }

    // The repository only returns the caller's own tasks, so "someone else's task" and
    // "no such task" are indistinguishable here by design.
    private async Task<TaskItem> GetOwnedTaskAsync(Guid userId, Guid taskId, CancellationToken cancellationToken)
        => await repository.GetByIdAsync(userId, taskId, cancellationToken)
           ?? throw new NotFoundException($"Task '{taskId}' was not found.");

    private static TaskResponse ToResponse(TaskItem task) => new(
        task.Id,
        task.Title,
        task.Description,
        task.Status,
        task.DueDate,
        task.CreatedAt,
        task.UpdatedAt);
}
