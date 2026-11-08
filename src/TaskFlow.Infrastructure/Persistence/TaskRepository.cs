using Microsoft.EntityFrameworkCore;
using TaskFlow.Application.Tasks;
using TaskFlow.Domain;

namespace TaskFlow.Infrastructure.Persistence;

public sealed class TaskRepository(AppDbContext dbContext) : ITaskRepository
{
    public async Task<IReadOnlyList<TaskItem>> ListByUserAsync(Guid userId, CancellationToken cancellationToken)
        => await dbContext.Tasks
            .AsNoTracking()
            .Where(t => t.UserId == userId)
            .OrderByDescending(t => t.CreatedAt)
            .ToListAsync(cancellationToken);

    // Filtering by owner here is what makes another user's task indistinguishable from a missing one.
    public Task<TaskItem?> GetByIdAsync(Guid userId, Guid taskId, CancellationToken cancellationToken)
        => dbContext.Tasks.FirstOrDefaultAsync(t => t.Id == taskId && t.UserId == userId, cancellationToken);

    public async Task AddAsync(TaskItem task, CancellationToken cancellationToken)
    {
        dbContext.Tasks.Add(task);
        await dbContext.SaveChangesAsync(cancellationToken);
    }

    public async Task UpdateAsync(TaskItem task, CancellationToken cancellationToken)
    {
        // Update() also covers a task that is not tracked by this context.
        dbContext.Tasks.Update(task);
        await dbContext.SaveChangesAsync(cancellationToken);
    }

    public async Task DeleteAsync(TaskItem task, CancellationToken cancellationToken)
    {
        dbContext.Tasks.Remove(task);
        await dbContext.SaveChangesAsync(cancellationToken);
    }
}
