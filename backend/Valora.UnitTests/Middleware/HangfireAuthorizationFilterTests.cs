using Hangfire;
using Hangfire.Dashboard;
using Hangfire.Storage;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Moq;
using System.Security.Claims;
using Valora.Api.Middleware;

namespace Valora.UnitTests.Middleware;

public class HangfireAuthorizationFilterTests
{
    private readonly Mock<ILogger<HangfireAuthorizationFilter>> _loggerMock = new();

    public class TestableHangfireAuthorizationFilter : HangfireAuthorizationFilter
    {
        private readonly HttpContext _mockHttpContext;

        public TestableHangfireAuthorizationFilter(ILogger<HangfireAuthorizationFilter> logger, HttpContext mockHttpContext)
            : base(logger)
        {
            _mockHttpContext = mockHttpContext;
        }

        protected override HttpContext GetHttpContext(DashboardContext context)
        {
            return _mockHttpContext;
        }
    }

    [Fact]
    public void Authorize_ReturnsTrue_WhenUserIsAdmin()
    {
        // Arrange
        var httpContextMock = new Mock<HttpContext>();
        var user = new ClaimsPrincipal(new ClaimsIdentity(
            new[] { new Claim(ClaimTypes.Role, "Admin"), new Claim(ClaimTypes.Name, "AdminUser") },
            "TestAuth"));
        var connectionMock = new Mock<ConnectionInfo>();
        connectionMock.Setup(c => c.RemoteIpAddress).Returns(System.Net.IPAddress.Loopback);

        httpContextMock.Setup(c => c.User).Returns(user);
        httpContextMock.Setup(c => c.Connection).Returns(connectionMock.Object);

        var filter = new TestableHangfireAuthorizationFilter(_loggerMock.Object, httpContextMock.Object);
        var context = new MockDashboardContext();

        // Act
        var result = filter.Authorize(context);

        // Assert
        Assert.True(result);
        _loggerMock.Verify(
            x => x.Log(
                LogLevel.Information,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("GRANTED")),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once);
    }

    [Fact]
    public void Authorize_ReturnsFalse_WhenUserIsNotAuthenticated()
    {
        // Arrange
        var httpContextMock = new Mock<HttpContext>();
        var user = new ClaimsPrincipal(new ClaimsIdentity()); // Not authenticated
        var connectionMock = new Mock<ConnectionInfo>();
        connectionMock.Setup(c => c.RemoteIpAddress).Returns(System.Net.IPAddress.Loopback);

        httpContextMock.Setup(c => c.User).Returns(user);
        httpContextMock.Setup(c => c.Connection).Returns(connectionMock.Object);

        var filter = new TestableHangfireAuthorizationFilter(_loggerMock.Object, httpContextMock.Object);
        var context = new MockDashboardContext();

        // Act
        var result = filter.Authorize(context);

        // Assert
        Assert.False(result);
        _loggerMock.Verify(
            x => x.Log(
                LogLevel.Warning,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("DENIED")),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once);
    }

    [Fact]
    public void Authorize_ReturnsFalse_WhenUserIsNotAdmin()
    {
        // Arrange
        var httpContextMock = new Mock<HttpContext>();
        var user = new ClaimsPrincipal(new ClaimsIdentity(
            new[] { new Claim(ClaimTypes.Role, "User"), new Claim(ClaimTypes.Name, "RegularUser") },
            "TestAuth"));
        var connectionMock = new Mock<ConnectionInfo>();
        connectionMock.Setup(c => c.RemoteIpAddress).Returns(System.Net.IPAddress.Loopback);

        httpContextMock.Setup(c => c.User).Returns(user);
        httpContextMock.Setup(c => c.Connection).Returns(connectionMock.Object);

        var filter = new TestableHangfireAuthorizationFilter(_loggerMock.Object, httpContextMock.Object);
        var context = new MockDashboardContext();

        // Act
        var result = filter.Authorize(context);

        // Assert
        Assert.False(result);
        _loggerMock.Verify(
            x => x.Log(
                LogLevel.Warning,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("DENIED")),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once);
    }

    // Minimal concrete implementation of DashboardContext for testing
    private class MockDashboardContext : DashboardContext
    {
        // DashboardContext requires (JobStorage storage, DashboardOptions options)
        public MockDashboardContext() : base(Mock.Of<JobStorage>(), new DashboardOptions())
        {
        }
    }
}
