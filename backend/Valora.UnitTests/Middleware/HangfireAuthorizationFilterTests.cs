using System.Security.Claims;
using Valora.Api.Middleware;
using Xunit;

namespace Valora.UnitTests.Middleware;

public class HangfireAuthorizationFilterTests
{
    private readonly HangfireAuthorizationFilter _filter;

    public HangfireAuthorizationFilterTests()
    {
        _filter = new HangfireAuthorizationFilter();
    }

    [Fact]
    public void AuthorizeUser_WhenUserIsNotAuthenticated_ReturnsFalse()
    {
        var user = new ClaimsPrincipal(new ClaimsIdentity()); // Unauthenticated
        var result = _filter.AuthorizeUser(user);
        Assert.False(result);
    }

    [Fact]
    public void AuthorizeUser_WhenUserIsAuthenticatedButNotAdmin_ReturnsFalse()
    {
        var identity = new ClaimsIdentity("TestAuthType");
        var user = new ClaimsPrincipal(identity);

        var result = _filter.AuthorizeUser(user);
        Assert.False(result);
    }

    [Fact]
    public void AuthorizeUser_WhenUserIsAuthenticatedAndAdmin_ReturnsTrue()
    {
        var identity = new ClaimsIdentity(new[]
        {
            new Claim(ClaimTypes.Role, "Admin")
        }, "TestAuthType");
        var user = new ClaimsPrincipal(identity);

        var result = _filter.AuthorizeUser(user);
        Assert.True(result);
    }
}
