using System.Security.Claims;
using TaskFlow.Api.Auth;

namespace TaskFlow.Api.Tests.Unit;

public class ClaimsPrincipalExtensionsTests
{
    private static ClaimsPrincipal PrincipalWith(params Claim[] claims) => new(new ClaimsIdentity(claims, "Test"));

    [Fact]
    public void GetUserId_ReadsTheSubjectClaim()
    {
        var userId = Guid.NewGuid();

        Assert.Equal(userId, PrincipalWith(new Claim("sub", userId.ToString())).GetUserId());
    }

    [Fact]
    public void GetUserId_IgnoresOtherIdentifierClaims()
    {
        var principal = PrincipalWith(new Claim(ClaimTypes.NameIdentifier, Guid.NewGuid().ToString()));

        Assert.Throws<InvalidOperationException>(() => principal.GetUserId());
    }

    [Theory]
    [InlineData("")]
    [InlineData("not-a-guid")]
    public void GetUserId_WithInvalidSubject_Throws(string subject)
    {
        Assert.Throws<InvalidOperationException>(() => PrincipalWith(new Claim("sub", subject)).GetUserId());
    }
}
