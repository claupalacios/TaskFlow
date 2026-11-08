using System.Security.Claims;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace TaskFlow.Api.Tests.Unit;

internal static class ControllerTestContext
{
    /// <summary>A controller context whose user is authenticated with the given <c>sub</c> claim.</summary>
    public static ControllerContext ForUser(Guid userId) => new()
    {
        HttpContext = new DefaultHttpContext
        {
            User = new ClaimsPrincipal(new ClaimsIdentity([new Claim("sub", userId.ToString())], authenticationType: "Test")),
        },
    };
}
