using Microsoft.EntityFrameworkCore;
using TaskFlow.Application.Auth;
using TaskFlow.Domain;

namespace TaskFlow.Infrastructure.Persistence;

/// <summary>
/// Demo data for local runs. It only seeds a database with no users, so running it again,
/// or against a database that already has real accounts, changes nothing.
/// </summary>
public sealed class DbSeeder(AppDbContext dbContext, IPasswordHasher passwordHasher, TimeProvider timeProvider)
{
    public sealed record DemoAccount(string Email, string Password);

    // Demo-only credentials, published in the README so reviewers can log in.
    public static IReadOnlyList<DemoAccount> DemoAccounts { get; } =
    [
        new("ana@example.com", "TaskFlow-demo1"),
        new("bruno@example.com", "TaskFlow-demo2"),
    ];

    public const int DemoTaskCount = 6;

    public async Task SeedAsync(CancellationToken cancellationToken)
    {
        if (await dbContext.Users.AnyAsync(cancellationToken))
        {
            return;
        }

        var now = timeProvider.GetUtcNow();
        var today = DateOnly.FromDateTime(now.UtcDateTime);

        var ana = CreateUser(DemoAccounts[0], now);
        var bruno = CreateUser(DemoAccounts[1], now);
        dbContext.Users.AddRange(ana, bruno);

        dbContext.Tasks.AddRange(
            CreateTask(ana, "Prepare sprint demo", "Slides and a short live walkthrough.", today.AddDays(2), TaskItemStatus.InProgress, now),
            CreateTask(ana, "Review pull requests", "Two PRs waiting on the API team.", today.AddDays(1), TaskItemStatus.Pending, now),
            CreateTask(ana, "Renew domain", "Expired last week; still needs doing.", today.AddDays(-3), TaskItemStatus.Pending, now),
            CreateTask(ana, "Book dentist appointment", string.Empty, null, TaskItemStatus.Completed, now),
            CreateTask(bruno, "Plan team offsite", "Shortlist three venues.", today.AddDays(14), TaskItemStatus.Pending, now),
            CreateTask(bruno, "Submit expense report", "October receipts.", today.AddDays(-1), TaskItemStatus.Completed, now));

        await dbContext.SaveChangesAsync(cancellationToken);
    }

    private User CreateUser(DemoAccount account, DateTimeOffset now)
        => User.Create(account.Email, passwordHasher.Hash(account.Password), now);

    private static TaskItem CreateTask(
        User owner, string title, string description, DateOnly? dueDate, TaskItemStatus status, DateTimeOffset now)
    {
        var task = TaskItem.Create(owner.Id, title, description, dueDate, now);
        if (status != TaskItemStatus.Pending)
        {
            task.Update(title, description, status, dueDate, now);
        }

        return task;
    }
}
