using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TaskFlow.Application.Auth;

namespace TaskFlow.Api.Controllers;

[ApiController]
[Route("api/auth")]
[AllowAnonymous]
[ApiExplorerSettings(GroupName = ApiDocuments.Identity)]
#pragma warning disable CS9113 // RED stub: the service is used once the controller is implemented.
public sealed class AuthController(IAuthService auth) : ControllerBase
{
    [HttpPost("register")]
    public Task<ActionResult<UserResponse>> Register(RegisterRequest request, CancellationToken cancellationToken)
        => throw new NotImplementedException();

    [HttpPost("login")]
    public Task<ActionResult<AuthResponse>> Login(LoginRequest request, CancellationToken cancellationToken)
        => throw new NotImplementedException();
}
