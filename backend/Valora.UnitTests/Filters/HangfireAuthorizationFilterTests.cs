using Hangfire;
using Hangfire.Dashboard;
using Microsoft.AspNetCore.Http;
using Moq;
using System.Security.Claims;
using Valora.Api.Filters;
using Hangfire.PostgreSql; // For storage if needed, or just mock JobStorage

namespace Valora.UnitTests.Filters;

public class HangfireAuthorizationFilterTests
{
    [Fact]
    public void Authorize_ReturnsTrue_WhenUserIsAdmin()
    {
        // Arrange
        var filter = new HangfireAuthorizationFilter();

        // Mock HttpContext
        var user = new ClaimsPrincipal(new ClaimsIdentity(new[]
        {
            new Claim(ClaimTypes.Name, "admin"),
            new Claim(ClaimTypes.Role, "Admin")
        }, "TestAuthType"));

        var httpContextMock = new Mock<HttpContext>();
        httpContextMock.Setup(c => c.User).Returns(user);

        // Mock ServiceProvider
        var serviceProviderMock = new Mock<IServiceProvider>();
        httpContextMock.Setup(c => c.RequestServices).Returns(serviceProviderMock.Object);

        // Mock DashboardContext
        // We need to create a context that GetHttpContext understands.
        // Assuming AspNetCoreDashboardContext is available and public.
        // It requires JobStorage, DashboardOptions, HttpContext.

        var storageMock = new Mock<JobStorage>();
        var options = new DashboardOptions();

        // AspNetCoreDashboardContext(JobStorage storage, DashboardOptions options, HttpContext httpContext)
        // If this constructor is available.
        var context = new AspNetCoreDashboardContext(storageMock.Object, options, httpContextMock.Object);

        // Act
        var result = filter.Authorize(context);

        // Assert
        Assert.True(result);
    }

    [Fact]
    public void Authorize_ReturnsFalse_WhenUserIsNotAuthenticated()
    {
        // Arrange
        var filter = new HangfireAuthorizationFilter();

        // Unauthenticated user
        var user = new ClaimsPrincipal(new ClaimsIdentity()); // No authentication type = not authenticated

        var httpContextMock = new Mock<HttpContext>();
        httpContextMock.Setup(c => c.User).Returns(user);

        var serviceProviderMock = new Mock<IServiceProvider>();
        httpContextMock.Setup(c => c.RequestServices).Returns(serviceProviderMock.Object);

        var storageMock = new Mock<JobStorage>();
        var options = new DashboardOptions();
        var context = new AspNetCoreDashboardContext(storageMock.Object, options, httpContextMock.Object);

        // Act
        var result = filter.Authorize(context);

        // Assert
        Assert.False(result);
    }

    [Fact]
    public void Authorize_ReturnsFalse_WhenUserIsNotAdmin()
    {
        // Arrange
        var filter = new HangfireAuthorizationFilter();

        // Authenticated but not Admin
        var user = new ClaimsPrincipal(new ClaimsIdentity(new[]
        {
            new Claim(ClaimTypes.Name, "user"),
            new Claim(ClaimTypes.Role, "User")
        }, "TestAuthType"));

        var httpContextMock = new Mock<HttpContext>();
        httpContextMock.Setup(c => c.User).Returns(user);

        var serviceProviderMock = new Mock<IServiceProvider>();
        httpContextMock.Setup(c => c.RequestServices).Returns(serviceProviderMock.Object);

        var storageMock = new Mock<JobStorage>();
        var options = new DashboardOptions();
        var context = new AspNetCoreDashboardContext(storageMock.Object, options, httpContextMock.Object);

        // Act
        var result = filter.Authorize(context);

        // Assert
        Assert.False(result);
    }
}
