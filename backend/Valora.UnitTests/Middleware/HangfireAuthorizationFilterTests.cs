using Hangfire;
using Hangfire.Dashboard;
using Microsoft.AspNetCore.Http;
using Moq;
using System.Text;
using Valora.Api.Middleware;
using Xunit;

namespace Valora.UnitTests.Middleware;

public class HangfireAuthorizationFilterTests
{
    private readonly HttpContext _httpContext;
    private readonly TestableHangfireAuthorizationFilter _filter;
    private const string Username = "admin";
    private const string Password = "password";

    public HangfireAuthorizationFilterTests()
    {
        _httpContext = new DefaultHttpContext();
        _filter = new TestableHangfireAuthorizationFilter(Username, Password, _httpContext);
    }

    [Fact]
    public void Authorize_ShouldReturnFalse_WhenHeaderIsMissing()
    {
        // Arrange
        var dashboardContext = CreateMockDashboardContext();

        // Act
        var result = _filter.Authorize(dashboardContext);

        // Assert
        Assert.False(result);
        Assert.Equal(401, _httpContext.Response.StatusCode);
        Assert.True(_httpContext.Response.Headers.ContainsKey("WWW-Authenticate"));
        Assert.Equal("Basic realm=\"Hangfire Dashboard\"", _httpContext.Response.Headers["WWW-Authenticate"]);
    }

    [Fact]
    public void Authorize_ShouldReturnFalse_WhenHeaderIsNotBasic()
    {
        // Arrange
        var dashboardContext = CreateMockDashboardContext();
        _httpContext.Request.Headers["Authorization"] = "Bearer token";

        // Act
        var result = _filter.Authorize(dashboardContext);

        // Assert
        Assert.False(result);
        Assert.Equal(401, _httpContext.Response.StatusCode);
    }

    [Fact]
    public void Authorize_ShouldReturnFalse_WhenCredentialsAreInvalid()
    {
        // Arrange
        var dashboardContext = CreateMockDashboardContext();
        var credentials = Convert.ToBase64String(Encoding.UTF8.GetBytes("wrong:wrong"));
        _httpContext.Request.Headers["Authorization"] = $"Basic {credentials}";

        // Act
        var result = _filter.Authorize(dashboardContext);

        // Assert
        Assert.False(result);
        Assert.Equal(401, _httpContext.Response.StatusCode);
    }

    [Fact]
    public void Authorize_ShouldReturnTrue_WhenCredentialsAreValid()
    {
        // Arrange
        var dashboardContext = CreateMockDashboardContext();
        var credentials = Convert.ToBase64String(Encoding.UTF8.GetBytes($"{Username}:{Password}"));
        _httpContext.Request.Headers["Authorization"] = $"Basic {credentials}";

        // Act
        var result = _filter.Authorize(dashboardContext);

        // Assert
        Assert.True(result);
        Assert.NotEqual(401, _httpContext.Response.StatusCode);
    }

    private DashboardContext CreateMockDashboardContext()
    {
        var storage = new Mock<JobStorage>();
        var options = new DashboardOptions();
        return new Mock<DashboardContext>(storage.Object, options).Object;
    }

    public class TestableHangfireAuthorizationFilter : HangfireAuthorizationFilter
    {
        private readonly HttpContext _httpContext;

        public TestableHangfireAuthorizationFilter(string username, string password, HttpContext httpContext)
            : base(username, password)
        {
            _httpContext = httpContext;
        }

        protected override HttpContext GetHttpContext(DashboardContext context)
        {
            return _httpContext;
        }
    }
}
