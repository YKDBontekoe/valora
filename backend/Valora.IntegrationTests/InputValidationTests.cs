using System.Net;
using System.Net.Http.Json;
using Xunit;

namespace Valora.IntegrationTests;

public class InputValidationTests : BaseIntegrationTest
{
    public InputValidationTests(TestDatabaseFixture fixture) : base(fixture)
    {
    }

    [Fact]
    public async Task SearchListings_WithInvalidPage_ReturnsBadRequest()
    {
        await AuthenticateAsync();

        // Page > 10000 should fail
        var query = "page=10001";
        var response = await Client.GetAsync($"/api/listings?{query}");

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task Login_WithInvalidEmail_ReturnsBadRequest()
    {
        // Act
        var response = await Client.PostAsJsonAsync("/api/auth/login", new
        {
            Email = "invalid-email",
            Password = "Password123!"
        });

        // Assert
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        // Verify it's a ValidationProblem
        var problem = await response.Content.ReadFromJsonAsync<Microsoft.AspNetCore.Mvc.ValidationProblemDetails>();
        Assert.NotNull(problem);
        Assert.Contains("Email", problem.Errors.Keys);
    }

    [Fact]
    public async Task ContextReport_WithEmptyInput_ReturnsBadRequest()
    {
        await AuthenticateAsync();

        // Act
        var response = await Client.PostAsJsonAsync("/api/context/report", new
        {
            Input = "", // Required
            RadiusMeters = 1000
        });

        // Assert
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        var problem = await response.Content.ReadFromJsonAsync<Microsoft.AspNetCore.Mvc.ValidationProblemDetails>();
        Assert.NotNull(problem);
        Assert.Contains("Input", problem.Errors.Keys);
    }

    [Fact]
    public async Task ContextReport_WithInvalidRadius_ReturnsBadRequest()
    {
        await AuthenticateAsync();

        // Act
        var response = await Client.PostAsJsonAsync("/api/context/report", new
        {
            Input = "Valid Address",
            RadiusMeters = 50 // Too small (min 100)
        });

        // Assert
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        var problem = await response.Content.ReadFromJsonAsync<Microsoft.AspNetCore.Mvc.ValidationProblemDetails>();
        Assert.NotNull(problem);
        Assert.Contains("RadiusMeters", problem.Errors.Keys);
    }

}
