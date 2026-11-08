using TaskFlow.Application.Tasks;
using TaskFlow.Domain;

namespace TaskFlow.Infrastructure.Persistence;

#pragma warning disable CS9113 // RED stub: dbContext is used once the repository is implemented.
public sealed class TaskRepository(AppDbContext dbContext) : ITaskRepository
{
    public Task<IReadOnlyList<TaskItem>> ListByUserAsync(Guid userId, CancellationToken cancellationToken)
        => throw new NotImplementedException();

    public Task<TaskItem?> GetByIdAsync(Guid userId, Guid taskId, CancellationToken cancellationToken)
        => throw new NotImplementedException();

    public Task AddAsync(TaskItem task, CancellationToken cancellationToken)
        => throw new NotImplementedException();

    public Task UpdateAsync(TaskItem task, CancellationToken cancellationToken)
        => throw new NotImplementedException();

    public Task DeleteAsync(TaskItem task, CancellationToken cancellationToken)
        => throw new NotImplementedException();
}
