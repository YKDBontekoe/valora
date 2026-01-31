using System.Security.Claims;
using Hangfire.Dashboard;
using Microsoft.AspNetCore.Http;
using Moq;
using Valora.Api.Filters;
using Xunit;

namespace Valora.UnitTests.Filters;

public class HangfireAuthorizationFilterTests
{
    private readonly Mock<DashboardContext> _mockContext;
    private readonly DefaultHttpContext _httpContext;

    public HangfireAuthorizationFilterTests()
    {
        _mockContext = new Mock<DashboardContext>();
        _httpContext = new DefaultHttpContext();
        _mockContext.Setup(c => c.GetHttpContext()).Returns(_httpContext);
    }

    [Fact]
    public void Authorize_ShouldReturnFalse_WhenUserIsNotAuthenticated()
    {
        // Arrange
        _httpContext.User = new ClaimsPrincipal(new ClaimsIdentity()); // Unauthenticated
        var filter = new HangfireAuthorizationFilter();

        // Act
        var result = filter.Authorize(_mockContext.Object);

        // Assert
        Assert.False(result);
    }

    [Fact]
    public void Authorize_ShouldReturnTrue_WhenUserIsAuthenticated()
    {
        // Arrange
        _httpContext.User = new ClaimsPrincipal(new ClaimsIdentity(new[] { new Claim(ClaimTypes.Name, "admin") }, "Basic"));
        var filter = new HangfireAuthorizationFilter();

        // Act
        var result = filter.Authorize(_mockContext.Object);

        // Assert
        Assert.True(result);
    }
}
