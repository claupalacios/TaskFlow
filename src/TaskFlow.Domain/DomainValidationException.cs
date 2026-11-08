namespace TaskFlow.Domain;

/// <summary>Thrown when a domain invariant is violated.</summary>
public sealed class DomainValidationException(string message) : Exception(message);
