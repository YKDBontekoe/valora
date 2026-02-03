using System.Net;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Moq;
using Moq.Protected;
using Valora.Infrastructure.Scraping;
using Valora.Infrastructure.Scraping.Models;
using Xunit;

namespace Valora.IntegrationTests;

public class ResilienceTests : IClassFixture<TestDatabaseFixture>
{
    private readonly TestDatabaseFixture _fixture;

    public ResilienceTests(TestDatabaseFixture fixture)
    {
        _fixture = fixture;
    }

    [Fact]
    public async Task FundaApiClient_ShouldRetry_OnTransientErrors()
    {
        // This test verifies that the DI configuration for Polly is working.
        // We use the real ServiceCollection but replace the HttpHandler.

        var handlerMock = new Mock<HttpMessageHandler>();
        var callCount = 0;

        handlerMock
            .Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.IsAny<HttpRequestMessage>(),
                ItExpr.IsAny<CancellationToken>()
            )
            .Returns(async (HttpRequestMessage request, CancellationToken token) =>
            {
                callCount++;
                if (callCount < 3)
                {
                    // Fail first 2 times
                    return new HttpResponseMessage(HttpStatusCode.ServiceUnavailable);
                }
                // Succeed 3rd time
                return new HttpResponseMessage(HttpStatusCode.OK)
                {
                    Content = new StringContent("{}")
                };
            });

        // Use the factory to create a scope with our mocked handler
        using var clientFactory = _fixture.Factory!.WithWebHostBuilder(builder =>
        {
            builder.ConfigureServices(services =>
            {
                // We need to re-register FundaApiClient to use our handler
                // Remove existing
                var descriptor = services.FirstOrDefault(d => d.ServiceType == typeof(FundaApiClient));
                if (descriptor != null) services.Remove(descriptor);

                // Add with Polly (we assume AddInfrastructure adds it, but we need to inject the handler)
                // Actually, AddInfrastructure adds HttpClient. We can't easily inject the handler into the *existing* registration logic
                // without copying the AddHttpClient code including the policy.

                // Instead, we can try to ConfigurePrimaryHttpMessageHandler on the named client if we knew the name,
                // but Typed clients use the type name.

                services.AddHttpClient<FundaApiClient>()
                    .ConfigurePrimaryHttpMessageHandler(() => handlerMock.Object)
                    // We must RE-ADD the policy because we are overwriting the registration or at least modifying it.
                    // But wait, AddHttpClient is additive. If we just configure the handler, the policy from AddInfrastructure might persist
                    // IF AddInfrastructure runs *after* or we append.
                    // However, we are in WithWebHostBuilder, which runs *after* Startup.
                    // But ConfigureTestServices runs after.
                    // If we use ConfigurePrimaryHttpMessageHandler here, it should apply to the client registered in Startup.
                    ;
            });
        });

        // NOTE: The above approach to inject the handler into the *existing* HttpClient with Polly is tricky.
        // Polly policies are added via AddTransientHttpErrorPolicy.
        // If we modify the client, we might lose the policy if we are not careful.
        // Let's try to see if the policy triggers.

        using var scope = clientFactory.Services.CreateScope();
        var client = scope.ServiceProvider.GetRequiredService<FundaApiClient>();

        // Act
        // This should trigger retries.
        // We call a method that makes a request.
        // SearchBuyAsync calls SearchAsync calls PostAsJsonAsync.
        try
        {
            await client.SearchBuyAsync("retry-test");
        }
        catch
        {
            // Ignore deserialization errors, we just want to verify retries
        }

        // Assert
        // If Polly works, callCount should be > 1.
        // If no Polly, callCount would be 1 (fails immediately).
        // Since we fail 2 times and succeed 3rd, and Polly retries 3 times, we expect 3 calls.
        Assert.True(callCount > 1, $"Expected retries, but only called {callCount} times");
    }
}
