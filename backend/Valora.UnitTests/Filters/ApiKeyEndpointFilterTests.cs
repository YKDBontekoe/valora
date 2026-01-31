using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using Moq;
using Valora.Api.Filters;
using Xunit;

namespace Valora.UnitTests.Filters;

public class ApiKeyEndpointFilterTests
{
    private readonly Mock<IConfiguration> _mockConfig;
    private readonly DefaultHttpContext _context;
    private readonly EndpointFilterInvocationContext _filterContext;
    private readonly EndpointFilterDelegate _next;
    private bool _nextCalled;

    public ApiKeyEndpointFilterTests()
    {
        _mockConfig = new Mock<IConfiguration>();
        _context = new DefaultHttpContext();
        _filterContext = TestEndpointFilterInvocationContext.CreateContext(_context);
        _next = (context) =>
        {
            _nextCalled = true;
            return ValueTask.FromResult<object?>(Results.Ok());
        };
    }

    [Fact]
    public async Task InvokeAsync_ShouldReturnUnauthorized_WhenHeaderIsMissing()
    {
        // Arrange
        _mockConfig.Setup(c => c["ApiKey"]).Returns("secret-key");
        var filter = new ApiKeyEndpointFilter(_mockConfig.Object);

        // Act
        var result = await filter.InvokeAsync(_filterContext, _next);

        // Assert
        var unauthorizedResult = Assert.IsType<Microsoft.AspNetCore.Http.HttpResults.UnauthorizedHttpResult>(result);
        Assert.False(_nextCalled);
    }

    [Fact]
    public async Task InvokeAsync_ShouldReturnUnauthorized_WhenApiKeyIsNotConfigured()
    {
        // Arrange
        _mockConfig.Setup(c => c["ApiKey"]).Returns((string?)null);
        _context.Request.Headers["X-Api-Key"] = "some-key";
        var filter = new ApiKeyEndpointFilter(_mockConfig.Object);

        // Act
        var result = await filter.InvokeAsync(_filterContext, _next);

        // Assert
        var unauthorizedResult = Assert.IsType<Microsoft.AspNetCore.Http.HttpResults.UnauthorizedHttpResult>(result);
        Assert.False(_nextCalled);
    }

    [Fact]
    public async Task InvokeAsync_ShouldReturnUnauthorized_WhenKeyDoesNotMatch()
    {
        // Arrange
        _mockConfig.Setup(c => c["ApiKey"]).Returns("secret-key");
        _context.Request.Headers["X-Api-Key"] = "wrong-key";
        var filter = new ApiKeyEndpointFilter(_mockConfig.Object);

        // Act
        var result = await filter.InvokeAsync(_filterContext, _next);

        // Assert
        var unauthorizedResult = Assert.IsType<Microsoft.AspNetCore.Http.HttpResults.UnauthorizedHttpResult>(result);
        Assert.False(_nextCalled);
    }

    [Fact]
    public async Task InvokeAsync_ShouldCallNext_WhenKeyMatches()
    {
        // Arrange
        _mockConfig.Setup(c => c["ApiKey"]).Returns("secret-key");
        _context.Request.Headers["X-Api-Key"] = "secret-key";
        var filter = new ApiKeyEndpointFilter(_mockConfig.Object);

        // Act
        await filter.InvokeAsync(_filterContext, _next);

        // Assert
        Assert.True(_nextCalled);
    }
}

// Helper to create EndpointFilterInvocationContext which is abstract
public class TestEndpointFilterInvocationContext : EndpointFilterInvocationContext
{
    private readonly HttpContext _httpContext;

    private TestEndpointFilterInvocationContext(HttpContext httpContext)
    {
        _httpContext = httpContext;
    }

    public override HttpContext HttpContext => _httpContext;
    public override IList<object?> Arguments => new List<object?>();
    public override T GetArgument<T>(int index) => default!;

    public static EndpointFilterInvocationContext CreateContext(HttpContext httpContext)
    {
        return new TestEndpointFilterInvocationContext(httpContext);
    }
}
