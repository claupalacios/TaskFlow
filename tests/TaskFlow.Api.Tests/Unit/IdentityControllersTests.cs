using Microsoft.AspNetCore.Mvc;
using NSubstitute;
using TaskFlow.Api.Controllers;
using TaskFlow.Application.Auth;

namespace TaskFlow.Api.Tests.Unit;

public class IdentityControllersTests
{
    private static readonly Guid UserId = Guid.Parse("11111111-1111-1111-1111-111111111111");
    private static readonly DateTimeOffset Now = new(2026, 11, 8, 12, 0, 0, TimeSpan.Zero);
    private static readonly UserResponse Ana = new(UserId, "ana@example.com", Now);

    private readonly IAuthService _auth = Substitute.For<IAuthService>();
    private readonly CancellationToken _ct = new CancellationTokenSource().Token;

    [Fact]
    public async Task Register_ReturnsCreatedPointingAtTheCurrentUserEndpoint()
    {
        var request = new RegisterRequest("ana@example.com", "s3cure-passphrase");
        _auth.RegisterAsync(request, _ct).Returns(Ana);
        var controller = new AuthController(_auth);

        var result = await controller.Register(request, _ct);

        var created = Assert.IsType<CreatedAtActionResult>(result.Result);
        Assert.Equal(nameof(UsersController.Me), created.ActionName);
        Assert.Equal("Users", created.ControllerName);
        Assert.Equal(Ana, created.Value);
    }

    [Fact]
    public async Task Login_ReturnsOkWithTheToken()
    {
        var request = new LoginRequest("ana@example.com", "s3cure-passphrase");
        var token = new AuthResponse("jwt", Now.AddHours(1));
        _auth.LoginAsync(request, _ct).Returns(token);
        var controller = new AuthController(_auth);

        var result = await controller.Login(request, _ct);

        var ok = Assert.IsType<OkObjectResult>(result.Result);
        Assert.Equal(token, ok.Value);
    }

    [Fact]
    public async Task Me_ReturnsTheProfileOfTheUserInTheToken()
    {
        _auth.GetCurrentUserAsync(UserId, _ct).Returns(Ana);
        var controller = new UsersController(_auth) { ControllerContext = ControllerTestContext.ForUser(UserId) };

        var result = await controller.Me(_ct);

        var ok = Assert.IsType<OkObjectResult>(result.Result);
        Assert.Equal(Ana, ok.Value);
    }
}
