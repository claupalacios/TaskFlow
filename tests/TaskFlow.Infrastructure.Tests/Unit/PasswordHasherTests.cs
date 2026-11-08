using TaskFlow.Infrastructure.Security;

namespace TaskFlow.Infrastructure.Tests.Unit;

public class PasswordHasherTests
{
    private const string Password = "s3cure-passphrase";

    private readonly PasswordHasher _hasher = new();

    [Fact]
    public void Verify_WithTheOriginalPassword_ReturnsTrue()
    {
        var hash = _hasher.Hash(Password);

        Assert.True(_hasher.Verify(hash, Password));
    }

    [Theory]
    [InlineData("wrong-password-1")]
    [InlineData("S3CURE-PASSPHRASE")]    // case-sensitive
    [InlineData("s3cure-passphrase ")]   // not trimmed
    [InlineData("")]
    public void Verify_WithADifferentPassword_ReturnsFalse(string attempt)
    {
        var hash = _hasher.Hash(Password);

        Assert.False(_hasher.Verify(hash, attempt));
    }

    [Fact]
    public void Hash_DoesNotContainThePlainPassword()
    {
        var hash = _hasher.Hash(Password);

        Assert.DoesNotContain(Password, hash, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void Hash_SamePasswordTwice_ProducesDifferentHashes()
    {
        // A random salt per hash: equal passwords must not be detectable by comparing hashes.
        Assert.NotEqual(_hasher.Hash(Password), _hasher.Hash(Password));
    }

    [Theory]
    [InlineData("")]
    [InlineData("not-base64-!!")]
    [InlineData("AAAA")]
    public void Verify_WithMalformedHash_ReturnsFalseInsteadOfThrowing(string malformedHash)
    {
        Assert.False(_hasher.Verify(malformedHash, Password));
    }
}
