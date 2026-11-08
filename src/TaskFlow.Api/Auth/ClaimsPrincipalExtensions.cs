using System.Security.Claims;
using Microsoft.IdentityModel.JsonWebTokens;

namespace TaskFlow.Api.Auth;

public static class ClaimsPrincipalExtensions
{
    /// <summary>
    /// The caller's user id, from the token's <c>sub</c> claim. This is the only source of
    /// the user id: request bodies never carry it.
    /// </summary>
    /// <remarks>
    /// Tokens without a valid subject are already rejected during authentication (401), so a
    /// failure here means a misconfigured endpoint, not a bad request.
    /// </remarks>
    public static Guid GetUserId(this ClaimsPrincipal principal)
        => TryGetUserId(principal, out var userId)
            ? userId
            : throw new InvalidOperationException("The authenticated principal has no valid 'sub' claim.");

    public static bool TryGetUserId(this ClaimsPrincipal principal, out Guid userId)
        => Guid.TryParse(principal.FindFirst(JwtRegisteredClaimNames.Sub)?.Value, out userId);
}
