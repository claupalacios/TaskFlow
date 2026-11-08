using System.Security.Claims;

namespace TaskFlow.Api.Auth;

public static class ClaimsPrincipalExtensions
{
    /// <summary>
    /// The caller's user id, from the token's <c>sub</c> claim. This is the only source of
    /// the user id: request bodies never carry it.
    /// </summary>
    public static Guid GetUserId(this ClaimsPrincipal principal)
        => throw new NotImplementedException();
}
