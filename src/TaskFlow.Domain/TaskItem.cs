namespace TaskFlow.Domain;

// RED STUB: signatures only so the tests compile. Behavior arrives in the green commit.
public sealed class TaskItem
{
    public const int TitleMaxLength = 200;
    public const int DescriptionMaxLength = 2000;

    public Guid Id { get; private set; }
    public Guid UserId { get; private set; }
    public string Title { get; private set; } = string.Empty;
    public string Description { get; private set; } = string.Empty;
    public TaskItemStatus Status { get; private set; }
    public DateOnly? DueDate { get; private set; }
    public DateTimeOffset CreatedAt { get; private set; }
    public DateTimeOffset UpdatedAt { get; private set; }

    public static TaskItem Create(Guid userId, string? title, string? description, DateOnly? dueDate, DateTimeOffset now)
        => throw new NotImplementedException();

    public void Update(string? title, string? description, TaskItemStatus status, DateOnly? dueDate, DateTimeOffset now)
        => throw new NotImplementedException();
}
