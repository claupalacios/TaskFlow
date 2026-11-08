using TaskFlow.Domain;

namespace TaskFlow.Application.Tasks;

public sealed record CreateTaskRequest(string? Title, string? Description, DateOnly? DueDate);

public sealed record UpdateTaskRequest(string? Title, string? Description, TaskItemStatus Status, DateOnly? DueDate);

public sealed record TaskResponse(
    Guid Id,
    string Title,
    string Description,
    TaskItemStatus Status,
    DateOnly? DueDate,
    DateTimeOffset CreatedAt,
    DateTimeOffset UpdatedAt);
