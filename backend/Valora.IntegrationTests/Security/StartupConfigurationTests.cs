using Xunit;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.Configuration;

namespace Valora.IntegrationTests.Security;

[Collection("TestDatabase")]
public class StartupConfigurationTests
{
    [Fact]
    public async Task Startup_MissingJwtSecret_ThrowsInvalidOperationException()
    {
        // Arrange
        // We use the base factory but override the config to unset the secret
        // Note: IntegrationTestWebAppFactory sets it in ConfigureWebHost.
        // We add our own config source afterwards to overwrite it with empty string.
        using var factory = new IntegrationTestWebAppFactory("InMemory")
            .WithWebHostBuilder(builder =>
            {
                builder.ConfigureAppConfiguration((context, config) =>
                {
                    // Overwrite JWT_SECRET with empty string to trigger the validation error
                    config.AddInMemoryCollection(new Dictionary<string, string?>
                    {
                        { "JWT_SECRET", "" }
                    });
                });
            });

        var client = factory.CreateClient();

        // Act & Assert
        // The exception happens during request processing when Options are resolved
        // We call a protected endpoint to ensure the Authentication middleware runs and triggers option validation

        var response = await client.GetAsync("/api/listings");

        // The middleware catches the exception and returns 500
        Assert.Equal(System.Net.HttpStatusCode.InternalServerError, response.StatusCode);
    }
}
