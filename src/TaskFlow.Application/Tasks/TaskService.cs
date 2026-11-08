namespace TaskFlow.Application.Tasks;

// RED STUB: signatures only so the tests compile. Behavior arrives in the green commit.
public sealed class TaskService : ITaskService
{
    private readonly ITaskRepository _repository;
    private readonly TimeProvider _timeProvider;

    public TaskService(ITaskRepository repository, TimeProvider timeProvider)
    {
        _repository = repository;
        _timeProvider = timeProvider;
    }

    public Task<IReadOnlyList<TaskResponse>> GetAllAsync(Guid userId, CancellationToken cancellationToken)
        => throw new NotImplementedException();

    public Task<TaskResponse> GetByIdAsync(Guid userId, Guid taskId, CancellationToken cancellationToken)
        => throw new NotImplementedException();

    public Task<TaskResponse> CreateAsync(Guid userId, CreateTaskRequest request, CancellationToken cancellationToken)
        => throw new NotImplementedException();

    public Task<TaskResponse> UpdateAsync(Guid userId, Guid taskId, UpdateTaskRequest request, CancellationToken cancellationToken)
        => throw new NotImplementedException();

    public Task DeleteAsync(Guid userId, Guid taskId, CancellationToken cancellationToken)
        => throw new NotImplementedException();
}
