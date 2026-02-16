using Valora.Application.Common.Exceptions;

namespace Valora.UnitTests.Exceptions;

public class ValidationExceptionTests
{
    [Fact]
    public void DefaultConstructor_CreatesEmptyErrors()
    {
        var ex = new ValidationException();
        Assert.NotNull(ex.Errors);
        Assert.Empty(ex.Errors);
        Assert.Equal("One or more validation failures have occurred.", ex.Message);
    }

    [Fact]
    public void MessageConstructor_SetsMessageAndGeneralError()
    {
        var message = "Custom error message";
        var ex = new ValidationException(message);
        Assert.NotNull(ex.Errors);
        Assert.Single(ex.Errors);
        Assert.Equal(message, ex.Message);
        Assert.True(ex.Errors.ContainsKey("General"));
        Assert.Contains(message, ex.Errors["General"]);
    }

    [Fact]
    public void EnumerableConstructor_CreatesGeneralError()
    {
        var errors = new[] { "Error 1", "Error 2" };
        var ex = new ValidationException(errors);

        Assert.NotNull(ex.Errors);
        Assert.Single(ex.Errors);
        Assert.True(ex.Errors.ContainsKey("General"));
        Assert.Equal(errors, ex.Errors["General"]);
    }

    [Fact]
    public void DictionaryConstructor_SetsErrors()
    {
        var errors = new Dictionary<string, string[]>
        {
            { "Field1", new[] { "Error 1" } },
            { "Field2", new[] { "Error 2", "Error 3" } }
        };

        var ex = new ValidationException(errors);

        Assert.NotNull(ex.Errors);
        Assert.Equal(2, ex.Errors.Count);
        Assert.Equal(errors["Field1"], ex.Errors["Field1"]);
        Assert.Equal(errors["Field2"], ex.Errors["Field2"]);
    }
}

public class NotFoundExceptionTests
{
    [Fact]
    public void DefaultConstructor_Works()
    {
        var ex = new NotFoundException();
        Assert.NotNull(ex);
    }

    [Fact]
    public void MessageConstructor_SetsMessage()
    {
        var message = "Not Found!";
        var ex = new NotFoundException(message);
        Assert.Equal(message, ex.Message);
    }

    [Fact]
    public void MessageInnerExceptionConstructor_SetsBoth()
    {
        var message = "Not Found!";
        var inner = new Exception("Inner");
        var ex = new NotFoundException(message, inner);
        Assert.Equal(message, ex.Message);
        Assert.Equal(inner, ex.InnerException);
    }

    [Fact]
    public void NameKeyConstructor_FormatsMessage()
    {
        var name = "Listing";
        var key = 123;
        var ex = new NotFoundException(name, key);

        Assert.Equal($"Entity \"{name}\" ({key}) was not found.", ex.Message);
    }
}
