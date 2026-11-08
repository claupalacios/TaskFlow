using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TaskFlow.Api.Auth;
using TaskFlow.Application.Auth;

namespace TaskFlow.Api.Controllers;

[ApiController]
[Route("api/users")]
[Authorize]
[ApiExplorerSettings(GroupName = ApiDocuments.Identity)]
public sealed class UsersController(IAuthService auth) : ControllerBase
{
    [HttpGet("me")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<UserResponse>> Me(CancellationToken cancellationToken)
        => Ok(await auth.GetCurrentUserAsync(User.GetUserId(), cancellationToken));
}
