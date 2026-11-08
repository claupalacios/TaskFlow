using TaskFlow.Domain;

namespace TaskFlow.Application.Tasks;

/// <summary>
/// Persistence port. Every read is scoped to the owning user, so another user's task
/// is simply "not found". Each write persists immediately.
/// </summary>
public interface ITaskRepository
{
    Task<IReadOnlyList<TaskItem>> ListByUserAsync(Guid userId, CancellationToken cancellationToken);

    Task<TaskItem?> GetByIdAsync(Guid userId, Guid taskId, CancellationToken cancellationToken);

    Task AddAsync(TaskItem task, CancellationToken cancellationToken);

    Task UpdateAsync(TaskItem task, CancellationToken cancellationToken);

    Task DeleteAsync(TaskItem task, CancellationToken cancellationToken);
}
