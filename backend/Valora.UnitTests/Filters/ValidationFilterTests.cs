using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Mvc;
using Valora.Api.Filters;

namespace Valora.UnitTests.Filters;

public class ValidationFilterTests
{
    private readonly ValidationFilter _filter = new();

    public record TestDto
    {
        [Required]
        public string Name { get; init; } = string.Empty;
    }

    public record InvalidDto
    {
        [Required]
        public string? Name { get; init; }
    }

    [Fact]
    public async Task InvokeAsync_CallsNext_WhenArgumentIsValid()
    {
        var context = new DefaultEndpointFilterInvocationContext(new DefaultHttpContext(), new TestDto { Name = "Valid Name" });
        var nextCalled = false;

        await _filter.InvokeAsync(context, ctx =>
        {
            nextCalled = true;
            return ValueTask.FromResult<object?>(Results.Ok());
        });

        Assert.True(nextCalled);
    }

    [Fact]
    public async Task InvokeAsync_ReturnsValidationProblem_WhenArgumentIsInvalid()
    {
        var context = new DefaultEndpointFilterInvocationContext(new DefaultHttpContext(), new InvalidDto { Name = null });
        var nextCalled = false;

        var result = await _filter.InvokeAsync(context, ctx =>
        {
            nextCalled = true;
            return ValueTask.FromResult<object?>(Results.Ok());
        });

        Assert.False(nextCalled);

        // Results.ValidationProblem returns Microsoft.AspNetCore.Http.HttpResults.ProblemHttpResult in minimal APIs
        // when using Results.ValidationProblem() without generic type arguments or when implementation details differ.
        // Actually, let's just inspect the ProblemDetails which is the standard contract.
        // The exact return type is an IResult implementation.

        // Assert.IsType<ProblemHttpResult>(result) would be more accurate, but ProblemHttpResult is internal or specific.
        // Let's rely on checking if it exposes IValueHttpResult or just reflection if needed,
        // OR better yet, let's cast to the specific public type if possible.
        // In ASP.NET Core 8+, Results.ValidationProblem returns ValidationProblem which implements IResult.
        // But the error says Actual: ProblemHttpResult.

        var problemResult = Assert.IsType<ProblemHttpResult>(result);
        Assert.NotNull(problemResult.ProblemDetails);
        // ProblemHttpResult doesn't guarantee ValidationProblemDetails unless we check, but usually it wraps it.
        // Let's check Extensions or just Errors if it's a ValidationProblemDetails.

        var validationProblem = Assert.IsType<HttpValidationProblemDetails>(problemResult.ProblemDetails);
        Assert.Contains("Name", validationProblem.Errors.Keys);
    }

    [Fact]
    public async Task InvokeAsync_SkipsSystemTypes()
    {
        var context = new DefaultEndpointFilterInvocationContext(new DefaultHttpContext(), "Some String");
        var nextCalled = false;

        await _filter.InvokeAsync(context, ctx =>
        {
            nextCalled = true;
            return ValueTask.FromResult<object?>(Results.Ok());
        });

        Assert.True(nextCalled);
    }

    [Fact]
    public async Task InvokeAsync_SkipsMicrosoftTypes()
    {
        var context = new DefaultEndpointFilterInvocationContext(new DefaultHttpContext(), new DefaultHttpContext());
        var nextCalled = false;

        await _filter.InvokeAsync(context, ctx =>
        {
            nextCalled = true;
            return ValueTask.FromResult<object?>(Results.Ok());
        });

        Assert.True(nextCalled);
    }

    // Helper class to mock EndpointFilterInvocationContext
    private class DefaultEndpointFilterInvocationContext : EndpointFilterInvocationContext
    {
        public DefaultEndpointFilterInvocationContext(HttpContext httpContext, params object[] arguments)
        {
            HttpContext = httpContext;
            Arguments = arguments;
        }

        public override HttpContext HttpContext { get; }
        public override IList<object?> Arguments { get; }

        public override TArgument GetArgument<TArgument>(int index)
        {
            return (TArgument)Arguments[index]!;
        }
    }
}
