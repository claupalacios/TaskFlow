namespace TaskFlow.Application.Tasks;

/// <summary>Task use cases. The user id always comes from the authenticated caller, never from a request body.</summary>
public interface ITaskService
{
    Task<IReadOnlyList<TaskResponse>> GetAllAsync(Guid userId, CancellationToken cancellationToken);

    Task<TaskResponse> GetByIdAsync(Guid userId, Guid taskId, CancellationToken cancellationToken);

    Task<TaskResponse> CreateAsync(Guid userId, CreateTaskRequest request, CancellationToken cancellationToken);

    Task<TaskResponse> UpdateAsync(Guid userId, Guid taskId, UpdateTaskRequest request, CancellationToken cancellationToken);

    Task DeleteAsync(Guid userId, Guid taskId, CancellationToken cancellationToken);
}
