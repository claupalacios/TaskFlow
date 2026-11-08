namespace TaskFlow.Application.Auth;

/// <summary>
/// Login failed. The message is always the same, whether the email is unknown or the
/// password is wrong, so a caller cannot use login to find out which emails are registered.
/// </summary>
public sealed class InvalidCredentialsException() : Exception(DefaultMessage)
{
    public const string DefaultMessage = "Invalid email or password.";
}
