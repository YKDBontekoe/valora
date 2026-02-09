using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Mvc;
using Valora.Api.Filters;
using Valora.Application.DTOs;

namespace Valora.UnitTests.Filters;

public class ValidationFilterTests
{
    private readonly ValidationFilter _filter = new();

    [Fact]
    public async Task InvokeAsync_CallsNext_WhenArgumentIsValid()
    {
        var context = new DefaultEndpointFilterInvocationContext(new DefaultHttpContext(), new TestValidationDto { Name = "Valid Name" });
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
        var context = new DefaultEndpointFilterInvocationContext(new DefaultHttpContext(), new InvalidValidationDto { Name = null });
        var nextCalled = false;

        var result = await _filter.InvokeAsync(context, ctx =>
        {
            nextCalled = true;
            return ValueTask.FromResult<object?>(Results.Ok());
        });

        Assert.False(nextCalled);

        var problemResult = Assert.IsType<ProblemHttpResult>(result);
        Assert.NotNull(problemResult.ProblemDetails);

        // Results.ValidationProblem() creates a dictionary-based HttpValidationProblemDetails
        // or similar structure.
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
