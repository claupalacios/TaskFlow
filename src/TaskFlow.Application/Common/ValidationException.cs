namespace TaskFlow.Application.Common;

/// <summary>The request breaks an application rule (for example, the password policy).</summary>
public sealed class ValidationException(string message) : Exception(message);
