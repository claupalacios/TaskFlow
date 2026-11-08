namespace TaskFlow.Application.Common;

/// <summary>
/// The requested resource does not exist or is not visible to the caller.
/// Both cases look identical on purpose, so the existence of other users' data is never revealed.
/// </summary>
public sealed class NotFoundException(string message) : Exception(message);
