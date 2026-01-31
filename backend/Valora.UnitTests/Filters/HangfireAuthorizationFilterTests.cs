using System.Security.Claims;
using Hangfire;
using Hangfire.Dashboard;
using Microsoft.AspNetCore.Http;
using Moq;
using Valora.Api.Filters;
using Xunit;

namespace Valora.UnitTests.Filters;

public class HangfireAuthorizationFilterTests
{
    private class TestableHangfireAuthorizationFilter : HangfireAuthorizationFilter
    {
        private readonly HttpContext _contextToReturn;

        public TestableHangfireAuthorizationFilter(HttpContext contextToReturn)
        {
            _contextToReturn = contextToReturn;
        }

        protected override HttpContext? GetHttpContext(DashboardContext context)
        {
            return _contextToReturn;
        }
    }

    [Fact]
    public void Authorize_ShouldReturnFalse_WhenUserIsNotAuthenticated()
    {
        // Arrange
        var context = new DefaultHttpContext();
        context.User = new ClaimsPrincipal(new ClaimsIdentity()); // Unauthenticated

        var filter = new TestableHangfireAuthorizationFilter(context);

        // Mock DashboardContext with required constructor arguments
        var mockStorage = new Mock<JobStorage>();
        var options = new DashboardOptions();
        var mockDashboardContext = new Mock<DashboardContext>(mockStorage.Object, options);

        // Act
        var result = filter.Authorize(mockDashboardContext.Object);

        // Assert
        Assert.False(result);
    }

    [Fact]
    public void Authorize_ShouldReturnTrue_WhenUserIsAuthenticated()
    {
        // Arrange
        var context = new DefaultHttpContext();
        context.User = new ClaimsPrincipal(new ClaimsIdentity(new[] { new Claim(ClaimTypes.Name, "admin") }, "Basic"));

        var filter = new TestableHangfireAuthorizationFilter(context);

        // Mock DashboardContext with required constructor arguments
        var mockStorage = new Mock<JobStorage>();
        var options = new DashboardOptions();
        var mockDashboardContext = new Mock<DashboardContext>(mockStorage.Object, options);

        // Act
        var result = filter.Authorize(mockDashboardContext.Object);

        // Assert
        Assert.True(result);
    }
}
