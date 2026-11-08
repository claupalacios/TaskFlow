using TaskFlow.Application.Auth;
using TaskFlow.Application.Common;

namespace TaskFlow.Application.Tests.Auth;

/// <summary>
/// Password rules for new accounts:
/// 1. Required.
/// 2. Between 8 and 128 characters (the upper bound caps hashing cost per request).
/// 3. At least one letter and one digit.
/// 4. Not the same as the account email (case-insensitive).
/// Passwords are never trimmed, and spaces are allowed so passphrases work.
/// </summary>
public class PasswordPolicyTests
{
    private const string Email = "ana@example.com";

    [Theory]
    [InlineData("abcdefg1")]                  // exactly the minimum length
    [InlineData("correct horse battery 9")]   // passphrase with spaces
    [InlineData("Contraseña2026")]            // non-ASCII letters count as letters
    [InlineData(" abcdefg1 ")]                // surrounding spaces are part of the password
    public void Validate_WithCompliantPassword_DoesNotThrow(string password)
    {
        PasswordPolicy.Validate(password, Email);
    }

    [Fact]
    public void Validate_WithMaximumLength_DoesNotThrow()
    {
        var password = new string('a', PasswordPolicy.MaxLength - 1) + "1";

        PasswordPolicy.Validate(password, Email);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    public void Validate_WithMissingPassword_Throws(string? password)
    {
        Assert.Throws<ValidationException>(() => PasswordPolicy.Validate(password, Email));
    }

    [Fact]
    public void Validate_ShorterThanMinimum_Throws()
    {
        Assert.Throws<ValidationException>(() => PasswordPolicy.Validate("abcdef1", Email));
    }

    [Fact]
    public void Validate_LongerThanMaximum_Throws()
    {
        var password = new string('a', PasswordPolicy.MaxLength) + "1";

        Assert.Throws<ValidationException>(() => PasswordPolicy.Validate(password, Email));
    }

    [Theory]
    [InlineData("abcdefgh")]   // no digit
    [InlineData("12345678")]   // no letter
    [InlineData("        ")]   // whitespace only
    [InlineData("!@#$%^&*")]   // symbols only
    public void Validate_WithoutLetterAndDigit_Throws(string password)
    {
        Assert.Throws<ValidationException>(() => PasswordPolicy.Validate(password, Email));
    }

    [Theory]
    [InlineData("ana1@example.com", "ana1@example.com")]
    [InlineData("ANA1@Example.com", "ana1@example.com")]
    public void Validate_SameAsEmail_Throws(string password, string normalizedEmail)
    {
        Assert.Throws<ValidationException>(() => PasswordPolicy.Validate(password, normalizedEmail));
    }

    [Fact]
    public void Validate_FailureMessage_DoesNotEchoThePassword()
    {
        const string password = "secretpw";

        var exception = Assert.Throws<ValidationException>(() => PasswordPolicy.Validate(password, Email));

        Assert.DoesNotContain(password, exception.Message, StringComparison.OrdinalIgnoreCase);
    }
}
