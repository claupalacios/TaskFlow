using Microsoft.Extensions.Time.Testing;
using NSubstitute;
using TaskFlow.Application.Auth;
using TaskFlow.Application.Common;
using TaskFlow.Application.Users;
using TaskFlow.Domain;

namespace TaskFlow.Application.Tests.Auth;

public class AuthServiceTests
{
    private static readonly DateTimeOffset Now = new(2026, 11, 8, 12, 0, 0, TimeSpan.Zero);
    private const string Email = "ana@example.com";
    private const string Password = "s3cure-passphrase";
    private const string StoredHash = "HASH(s3cure-passphrase)";

    private readonly IUserRepository _users = Substitute.For<IUserRepository>();
    private readonly IPasswordHasher _hasher = Substitute.For<IPasswordHasher>();
    private readonly ITokenService _tokens = Substitute.For<ITokenService>();
    private readonly FakeTimeProvider _clock = new(Now);
    private readonly AuthService _service;

    public AuthServiceTests()
    {
        _hasher.Hash(Arg.Any<string>()).Returns(ci => $"HASH({ci.Arg<string>()})");
        _hasher.Verify(Arg.Any<string>(), Arg.Any<string>()).Returns(ci => ci.ArgAt<string>(0) == $"HASH({ci.ArgAt<string>(1)})");
        _service = new AuthService(_users, _hasher, _tokens, _clock);
    }

    private User ExistingUser()
    {
        var user = User.Create(Email, StoredHash, Now.AddDays(-10));
        _users.GetByEmailAsync(Arg.Is<string>(e => User.NormalizeEmail(e) == Email), Arg.Any<CancellationToken>()).Returns(user);
        _users.GetByIdAsync(user.Id, Arg.Any<CancellationToken>()).Returns(user);
        return user;
    }

    // ---- Register ----

    [Fact]
    public async Task RegisterAsync_WithValidData_StoresUserWithHashedPassword()
    {
        await _service.RegisterAsync(new RegisterRequest(Email, Password), CancellationToken.None);

        await _users.Received(1).AddAsync(
            Arg.Is<User>(u => u.Email == Email && u.PasswordHash == StoredHash && u.CreatedAt == Now),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task RegisterAsync_NeverStoresThePlainPassword()
    {
        await _service.RegisterAsync(new RegisterRequest(Email, Password), CancellationToken.None);

        await _users.Received(1).AddAsync(
            Arg.Is<User>(u => !u.PasswordHash.Equals(Password, StringComparison.Ordinal)),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task RegisterAsync_ReturnsTheNewUser()
    {
        var response = await _service.RegisterAsync(new RegisterRequest(Email, Password), CancellationToken.None);

        Assert.NotEqual(Guid.Empty, response.Id);
        Assert.Equal(Email, response.Email);
        Assert.Equal(Now, response.CreatedAt);
    }

    [Fact]
    public async Task RegisterAsync_NormalizesEmail()
    {
        var response = await _service.RegisterAsync(new RegisterRequest("  Ana@Example.COM ", Password), CancellationToken.None);

        Assert.Equal(Email, response.Email);
        await _users.Received(1).AddAsync(Arg.Is<User>(u => u.Email == Email), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task RegisterAsync_DoesNotIssueAToken()
    {
        await _service.RegisterAsync(new RegisterRequest(Email, Password), CancellationToken.None);

        _tokens.DidNotReceiveWithAnyArgs().CreateToken(default!);
    }

    [Fact]
    public async Task RegisterAsync_PassesTheCancellationTokenToTheRepository()
    {
        using var cts = new CancellationTokenSource();

        await _service.RegisterAsync(new RegisterRequest(Email, Password), cts.Token);

        await _users.Received(1).GetByEmailAsync(Email, cts.Token);
        await _users.Received(1).AddAsync(Arg.Any<User>(), cts.Token);
    }

    // ---- Register: duplicate email ----

    [Fact]
    public async Task RegisterAsync_EmailAlreadyRegistered_ThrowsConflict()
    {
        ExistingUser();

        await Assert.ThrowsAsync<ConflictException>(
            () => _service.RegisterAsync(new RegisterRequest(Email, Password), CancellationToken.None));
    }

    [Fact]
    public async Task RegisterAsync_EmailAlreadyRegisteredWithDifferentCase_ThrowsConflict()
    {
        ExistingUser();

        await Assert.ThrowsAsync<ConflictException>(
            () => _service.RegisterAsync(new RegisterRequest(" ANA@example.com", Password), CancellationToken.None));
    }

    [Fact]
    public async Task RegisterAsync_EmailAlreadyRegistered_DoesNotHashOrStore()
    {
        ExistingUser();

        await Assert.ThrowsAsync<ConflictException>(
            () => _service.RegisterAsync(new RegisterRequest(Email, Password), CancellationToken.None));

        _hasher.DidNotReceiveWithAnyArgs().Hash(default!);
        await _users.DidNotReceiveWithAnyArgs().AddAsync(default!, default);
    }

    // ---- Register: validation ----

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData("not-an-email")]
    [InlineData("ana@")]
    [InlineData("@example.com")]
    [InlineData("ana example@example.com")]
    [InlineData("Ana <ana@example.com>")]   // display-name form is not a bare address
    public async Task RegisterAsync_WithInvalidEmail_ThrowsValidationAndStoresNothing(string? email)
    {
        await Assert.ThrowsAsync<ValidationException>(
            () => _service.RegisterAsync(new RegisterRequest(email, Password), CancellationToken.None));

        await _users.DidNotReceiveWithAnyArgs().AddAsync(default!, default);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("short1")]
    [InlineData("onlyletters")]
    [InlineData("12345678")]
    public async Task RegisterAsync_WithPasswordBreakingPolicy_ThrowsValidationAndStoresNothing(string? password)
    {
        await Assert.ThrowsAsync<ValidationException>(
            () => _service.RegisterAsync(new RegisterRequest(Email, password), CancellationToken.None));

        _hasher.DidNotReceiveWithAnyArgs().Hash(default!);
        await _users.DidNotReceiveWithAnyArgs().AddAsync(default!, default);
    }

    [Fact]
    public async Task RegisterAsync_PasswordSameAsEmail_ThrowsValidation()
    {
        const string email = "ana2026@example.com";

        await Assert.ThrowsAsync<ValidationException>(
            () => _service.RegisterAsync(new RegisterRequest(email, "ANA2026@example.com"), CancellationToken.None));
    }

    [Fact]
    public async Task RegisterAsync_HashesThePasswordExactlyAsTyped()
    {
        const string withSpaces = "  spaced out 42  ";

        await _service.RegisterAsync(new RegisterRequest(Email, withSpaces), CancellationToken.None);

        _hasher.Received(1).Hash(withSpaces);
    }

    // ---- Login ----

    [Fact]
    public async Task LoginAsync_WithValidCredentials_ReturnsTokenForThatUser()
    {
        var user = ExistingUser();
        var expiresAt = Now.AddMinutes(60);
        _tokens.CreateToken(user).Returns(new AccessToken("jwt-token", expiresAt));

        var response = await _service.LoginAsync(new LoginRequest(Email, Password), CancellationToken.None);

        Assert.Equal("jwt-token", response.AccessToken);
        Assert.Equal(expiresAt, response.ExpiresAt);
    }

    [Fact]
    public async Task LoginAsync_EmailWithDifferentCaseAndSpaces_Succeeds()
    {
        var user = ExistingUser();
        _tokens.CreateToken(user).Returns(new AccessToken("jwt-token", Now.AddMinutes(60)));

        var response = await _service.LoginAsync(new LoginRequest("  ANA@Example.com ", Password), CancellationToken.None);

        Assert.Equal("jwt-token", response.AccessToken);
    }

    [Fact]
    public async Task LoginAsync_WrongPassword_ThrowsInvalidCredentials()
    {
        ExistingUser();

        await Assert.ThrowsAsync<InvalidCredentialsException>(
            () => _service.LoginAsync(new LoginRequest(Email, "wrong-password-1"), CancellationToken.None));
    }

    [Fact]
    public async Task LoginAsync_PasswordWithDifferentCase_ThrowsInvalidCredentials()
    {
        ExistingUser();

        await Assert.ThrowsAsync<InvalidCredentialsException>(
            () => _service.LoginAsync(new LoginRequest(Email, Password.ToUpperInvariant()), CancellationToken.None));
    }

    [Fact]
    public async Task LoginAsync_UnknownEmail_ThrowsInvalidCredentials()
    {
        await Assert.ThrowsAsync<InvalidCredentialsException>(
            () => _service.LoginAsync(new LoginRequest("nobody@example.com", Password), CancellationToken.None));
    }

    [Theory]
    [InlineData(null, Password)]
    [InlineData("", Password)]
    [InlineData(Email, null)]
    [InlineData(Email, "")]
    public async Task LoginAsync_MissingEmailOrPassword_ThrowsInvalidCredentials(string? email, string? password)
    {
        ExistingUser();

        await Assert.ThrowsAsync<InvalidCredentialsException>(
            () => _service.LoginAsync(new LoginRequest(email, password), CancellationToken.None));
    }

    [Fact]
    public async Task LoginAsync_FailureNeverIssuesAToken()
    {
        ExistingUser();

        await Assert.ThrowsAsync<InvalidCredentialsException>(
            () => _service.LoginAsync(new LoginRequest(Email, "wrong-password-1"), CancellationToken.None));

        _tokens.DidNotReceiveWithAnyArgs().CreateToken(default!);
    }

    // ---- Login: security ----

    [Fact]
    public async Task LoginAsync_UnknownEmailAndWrongPassword_FailWithTheSameMessage()
    {
        ExistingUser();

        var unknownEmail = await Assert.ThrowsAsync<InvalidCredentialsException>(
            () => _service.LoginAsync(new LoginRequest("nobody@example.com", Password), CancellationToken.None));
        var wrongPassword = await Assert.ThrowsAsync<InvalidCredentialsException>(
            () => _service.LoginAsync(new LoginRequest(Email, "wrong-password-1"), CancellationToken.None));

        Assert.Equal(unknownEmail.Message, wrongPassword.Message);
    }

    [Fact]
    public async Task LoginAsync_UnknownEmail_StillVerifiesAPassword()
    {
        // Skipping the (deliberately slow) hash check for unknown emails would let an
        // attacker tell registered emails apart by response time.
        await Assert.ThrowsAsync<InvalidCredentialsException>(
            () => _service.LoginAsync(new LoginRequest("nobody@example.com", Password), CancellationToken.None));

        _hasher.Received(1).Verify(Arg.Any<string>(), Password);
    }

    [Fact]
    public async Task LoginAsync_DoesNotApplyThePasswordPolicy()
    {
        // The policy guards new passwords only; an account created under older rules must still log in.
        const string legacyPassword = "legacy";
        var user = User.Create(Email, $"HASH({legacyPassword})", Now.AddYears(-1));
        _users.GetByEmailAsync(Email, Arg.Any<CancellationToken>()).Returns(user);
        _tokens.CreateToken(user).Returns(new AccessToken("jwt-token", Now.AddMinutes(60)));

        var response = await _service.LoginAsync(new LoginRequest(Email, legacyPassword), CancellationToken.None);

        Assert.Equal("jwt-token", response.AccessToken);
    }

    [Fact]
    public async Task LoginAsync_PassesTheCancellationTokenToTheRepository()
    {
        var user = ExistingUser();
        _tokens.CreateToken(user).Returns(new AccessToken("jwt-token", Now.AddMinutes(60)));
        using var cts = new CancellationTokenSource();

        await _service.LoginAsync(new LoginRequest(Email, Password), cts.Token);

        await _users.Received(1).GetByEmailAsync(Email, cts.Token);
    }

    // ---- Current user ----

    [Fact]
    public async Task GetCurrentUserAsync_ExistingUser_ReturnsProfile()
    {
        var user = ExistingUser();

        var response = await _service.GetCurrentUserAsync(user.Id, CancellationToken.None);

        Assert.Equal(user.Id, response.Id);
        Assert.Equal(Email, response.Email);
        Assert.Equal(user.CreatedAt, response.CreatedAt);
    }

    [Fact]
    public async Task GetCurrentUserAsync_UserNoLongerExists_ThrowsNotFound()
    {
        // A token can outlive its user; the API must not treat it as a valid profile.
        await Assert.ThrowsAsync<NotFoundException>(
            () => _service.GetCurrentUserAsync(Guid.NewGuid(), CancellationToken.None));
    }
}
