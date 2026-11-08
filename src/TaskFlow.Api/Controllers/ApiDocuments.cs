namespace TaskFlow.Api.Controllers;

/// <summary>
/// The two APIs this host exposes, each with its own OpenAPI document:
/// task management, and identity (register, login, current user).
/// </summary>
public static class ApiDocuments
{
    public const string Tasks = "tasks";
    public const string Identity = "identity";
}
