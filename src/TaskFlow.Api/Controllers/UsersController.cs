using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TaskFlow.Application.Auth;

namespace TaskFlow.Api.Controllers;

[ApiController]
[Route("api/users")]
[Authorize]
[ApiExplorerSettings(GroupName = ApiDocuments.Identity)]
#pragma warning disable CS9113 // RED stub: the service is used once the controller is implemented.
public sealed class UsersController(IAuthService auth) : ControllerBase
{
    [HttpGet("me")]
    public Task<ActionResult<UserResponse>> Me(CancellationToken cancellationToken)
        => throw new NotImplementedException();
}
