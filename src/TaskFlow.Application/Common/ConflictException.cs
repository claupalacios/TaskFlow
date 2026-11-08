namespace TaskFlow.Application.Common;

/// <summary>The request conflicts with existing data (for example, an email that is already registered).</summary>
public sealed class ConflictException(string message) : Exception(message);
