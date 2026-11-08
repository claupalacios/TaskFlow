namespace TaskFlow.Domain;

public sealed class TaskItem
{
    public const int TitleMaxLength = 200;
    public const int DescriptionMaxLength = 2000;

    private TaskItem()
    {
    }

    public Guid Id { get; private set; }
    public Guid UserId { get; private set; }
    public string Title { get; private set; } = string.Empty;
    public string Description { get; private set; } = string.Empty;
    public TaskItemStatus Status { get; private set; }
    public DateOnly? DueDate { get; private set; }
    public DateTimeOffset CreatedAt { get; private set; }
    public DateTimeOffset UpdatedAt { get; private set; }

    public static TaskItem Create(Guid userId, string? title, string? description, DateOnly? dueDate, DateTimeOffset now)
    {
        if (userId == Guid.Empty)
        {
            throw new DomainValidationException("A task must belong to a user.");
        }

        return new TaskItem
        {
            Id = Guid.NewGuid(),
            UserId = userId,
            Title = NormalizeTitle(title),
            Description = NormalizeDescription(description),
            Status = TaskItemStatus.Pending,
            DueDate = dueDate,
            CreatedAt = now,
            UpdatedAt = now,
        };
    }

    public void Update(string? title, string? description, TaskItemStatus status, DateOnly? dueDate, DateTimeOffset now)
    {
        // Validate everything first so a failed update never leaves the task half-changed.
        var newTitle = NormalizeTitle(title);
        var newDescription = NormalizeDescription(description);

        if (!Enum.IsDefined(status))
        {
            throw new DomainValidationException($"'{status}' is not a valid task status.");
        }

        Title = newTitle;
        Description = newDescription;
        Status = status;
        DueDate = dueDate;
        UpdatedAt = now;
    }

    private static string NormalizeTitle(string? title)
    {
        var trimmed = title?.Trim();

        if (string.IsNullOrEmpty(trimmed))
        {
            throw new DomainValidationException("Title is required.");
        }

        if (trimmed.Length > TitleMaxLength)
        {
            throw new DomainValidationException($"Title cannot exceed {TitleMaxLength} characters.");
        }

        return trimmed;
    }

    private static string NormalizeDescription(string? description)
    {
        var value = description ?? string.Empty;

        if (value.Length > DescriptionMaxLength)
        {
            throw new DomainValidationException($"Description cannot exceed {DescriptionMaxLength} characters.");
        }

        return value;
    }
}
