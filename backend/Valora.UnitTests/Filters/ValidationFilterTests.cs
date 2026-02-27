using System.Reflection;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Mvc;
using Moq;
using Valora.Api.Filters;
using Xunit;

namespace Valora.UnitTests.Filters;

public class TestDto
{
    public string Name { get; set; } = string.Empty;
    public NestedDto? Child { get; set; }
    public List<string> Tags { get; set; } = new();
    public Dictionary<string, string> Metadata { get; set; } = new();
}

public class NestedDto
{
    public string Description { get; set; } = string.Empty;
}

public class ValidationFilterTests
{
    [Fact]
    public async Task InvokeAsync_ShouldReturnBadRequest_WhenInputContainsScriptTag()
    {
        // Arrange
        var dto = new TestDto { Name = "<script>alert('xss')</script>" };
        var filter = new ValidationFilter<TestDto>();
        var context = new DefaultEndpointFilterInvocationContext(new DefaultHttpContext(), new object[] { dto });
        EndpointFilterDelegate next = (c) => ValueTask.FromResult<object?>(Results.Ok());

        // Act
        var result = await filter.InvokeAsync(context, next);

        // Assert
        Assert.IsType<ProblemHttpResult>(result);
        var problem = (ProblemHttpResult)result;
        Assert.Equal(400, problem.StatusCode);
        Assert.Contains("Potentially malicious input detected", problem.ProblemDetails.Detail);
    }

    [Fact]
    public async Task InvokeAsync_ShouldReturnBadRequest_WhenInputContainsJavascriptProtocol()
    {
        // Arrange
        var dto = new TestDto { Name = "javascript:alert(1)" };
        var filter = new ValidationFilter<TestDto>();
        var context = new DefaultEndpointFilterInvocationContext(new DefaultHttpContext(), new object[] { dto });
        EndpointFilterDelegate next = (c) => ValueTask.FromResult<object?>(Results.Ok());

        // Act
        var result = await filter.InvokeAsync(context, next);

        // Assert
        Assert.IsType<ProblemHttpResult>(result);
        var problem = (ProblemHttpResult)result;
        Assert.Equal(400, problem.StatusCode);
    }

    [Fact]
    public async Task InvokeAsync_ShouldReturnBadRequest_WhenNestedInputIsMalicious()
    {
        // Arrange
        var dto = new TestDto
        {
            Name = "Safe",
            Child = new NestedDto { Description = "<img src=x onerror=alert(1)>" }
        };
        var filter = new ValidationFilter<TestDto>();
        var context = new DefaultEndpointFilterInvocationContext(new DefaultHttpContext(), new object[] { dto });
        EndpointFilterDelegate next = (c) => ValueTask.FromResult<object?>(Results.Ok());

        // Act
        var result = await filter.InvokeAsync(context, next);

        // Assert
        Assert.IsType<ProblemHttpResult>(result);
        var problem = (ProblemHttpResult)result;
        Assert.Equal(400, problem.StatusCode);
    }

    [Fact]
    public async Task InvokeAsync_ShouldReturnBadRequest_WhenListInputIsMalicious()
    {
        // Arrange
        var dto = new TestDto
        {
            Name = "Safe",
            Tags = new List<string> { "safe", "<svg/onload=alert(1)>" }
        };
        var filter = new ValidationFilter<TestDto>();
        var context = new DefaultEndpointFilterInvocationContext(new DefaultHttpContext(), new object[] { dto });
        EndpointFilterDelegate next = (c) => ValueTask.FromResult<object?>(Results.Ok());

        // Act
        var result = await filter.InvokeAsync(context, next);

        // Assert
        Assert.IsType<ProblemHttpResult>(result);
        var problem = (ProblemHttpResult)result;
        Assert.Equal(400, problem.StatusCode);
    }

    [Fact]
    public async Task InvokeAsync_ShouldReturnBadRequest_WhenDictionaryValueIsMalicious()
    {
        // Arrange
        var dto = new TestDto
        {
            Name = "Safe",
            Metadata = new Dictionary<string, string> { { "key", "<iframe src='x'>" } }
        };
        var filter = new ValidationFilter<TestDto>();
        var context = new DefaultEndpointFilterInvocationContext(new DefaultHttpContext(), new object[] { dto });
        EndpointFilterDelegate next = (c) => ValueTask.FromResult<object?>(Results.Ok());

        // Act
        var result = await filter.InvokeAsync(context, next);

        // Assert
        Assert.IsType<ProblemHttpResult>(result);
        var problem = (ProblemHttpResult)result;
        Assert.Equal(400, problem.StatusCode);
    }

    [Fact]
    public async Task InvokeAsync_ShouldPass_WhenInputIsSafe()
    {
        // Arrange
        var dto = new TestDto { Name = "Safe input" };
        var filter = new ValidationFilter<TestDto>();
        var context = new DefaultEndpointFilterInvocationContext(new DefaultHttpContext(), new object[] { dto });
        EndpointFilterDelegate next = (c) => ValueTask.FromResult<object?>(Results.Ok("Success"));

        // Act
        var result = await filter.InvokeAsync(context, next);

        // Assert
        Assert.IsType<Ok<string>>(result);
        var ok = (Ok<string>)result;
        Assert.Equal("Success", ok.Value);
    }

    [Fact]
    public async Task InvokeAsync_ShouldPass_WhenInputContainsDataUri()
    {
        // Arrange
        var dto = new TestDto { Name = "data:image/png;base64,aaaa" };
        var filter = new ValidationFilter<TestDto>();
        var context = new DefaultEndpointFilterInvocationContext(new DefaultHttpContext(), new object[] { dto });
        EndpointFilterDelegate next = (c) => ValueTask.FromResult<object?>(Results.Ok("Success"));

        // Act
        var result = await filter.InvokeAsync(context, next);

        // Assert
        Assert.IsType<Ok<string>>(result);
        var ok = (Ok<string>)result;
        Assert.Equal("Success", ok.Value);
    }
}
