using TaskFlow.Domain;

namespace TaskFlow.Application.Tests.Domain;

public class UserTests
{
    private static readonly DateTimeOffset Now = new(2026, 11, 8, 12, 0, 0, TimeSpan.Zero);
    private const string Hash = "hashed-password";

    // ---- Create ----

    [Fact]
    public void Create_WithValidData_SetsAllFields()
    {
        var user = User.Create("ana@example.com", Hash, Now);

        Assert.NotEqual(Guid.Empty, user.Id);
        Assert.Equal("ana@example.com", user.Email);
        Assert.Equal(Hash, user.PasswordHash);
        Assert.Equal(Now, user.CreatedAt);
    }

    [Fact]
    public void Create_NormalizesEmail()
    {
        var user = User.Create("  Ana@Example.COM ", Hash, Now);

        Assert.Equal("ana@example.com", user.Email);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void Create_WithMissingEmail_Throws(string? email)
    {
        Assert.Throws<DomainValidationException>(() => User.Create(email, Hash, Now));
    }

    [Fact]
    public void Create_WithEmailOverMaxLength_Throws()
    {
        var email = new string('a', User.EmailMaxLength - "@x.io".Length + 1) + "@x.io";

        Assert.Throws<DomainValidationException>(() => User.Create(email, Hash, Now));
    }

    [Fact]
    public void Create_WithEmailAtMaxLength_Succeeds()
    {
        var email = new string('a', User.EmailMaxLength - "@x.io".Length) + "@x.io";

        var user = User.Create(email, Hash, Now);

        Assert.Equal(User.EmailMaxLength, user.Email.Length);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void Create_WithMissingPasswordHash_Throws(string? passwordHash)
    {
        Assert.Throws<DomainValidationException>(() => User.Create("ana@example.com", passwordHash, Now));
    }

    // ---- NormalizeEmail ----

    [Fact]
    public void NormalizeEmail_TrimsAndLowercases()
    {
        Assert.Equal("ana@example.com", User.NormalizeEmail("  Ana@Example.COM "));
    }

    [Fact]
    public void NormalizeEmail_WithNull_ReturnsEmpty()
    {
        Assert.Equal(string.Empty, User.NormalizeEmail(null));
    }
}
