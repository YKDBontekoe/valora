using System.Net;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.EntityFrameworkCore;
using Valora.Infrastructure.Persistence;
using Moq;
using Valora.Application.Scraping;
using Xunit;

namespace Valora.IntegrationTests;

public class IsolatedSearchTests : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly WebApplicationFactory<Program> _factory;
    private readonly Mock<IFundaSearchService> _searchServiceMock = new();

    public IsolatedSearchTests(WebApplicationFactory<Program> factory)
    {
        _factory = factory.WithWebHostBuilder(builder =>
        {
            builder.UseEnvironment("Testing");

            builder.ConfigureTestServices(services =>
            {
                // Mock the search service
                services.AddScoped(_ => _searchServiceMock.Object);

                // Completely remove all EF Core registrations to ensure we don't accidentally use the production Npgsql provider
                services.RemoveAll<DbContextOptions<ValoraDbContext>>();
                services.RemoveAll<DbContextOptions>();
                services.RemoveAll<ValoraDbContext>();

                // Add InMemory DbContext
                services.AddDbContext<ValoraDbContext>(options =>
                    options.UseInMemoryDatabase("IsolatedSearchTestDb")
                           .ConfigureWarnings(w => w.Ignore(Microsoft.EntityFrameworkCore.Diagnostics.RelationalEventId.PendingModelChangesWarning)));
            });

            // Replicate key config from IntegrationTestWebAppFactory
            builder.ConfigureAppConfiguration((_, config) =>
            {
                config.AddInMemoryCollection(new Dictionary<string, string?>
                {
                    { "JWT_SECRET", "TestSecretKeyForIntegrationTestingOnly123!" },
                    { "JWT_ISSUER", "ValoraTest" },
                    { "JWT_AUDIENCE", "ValoraTest" },
                    { "HANGFIRE_ENABLED", "false" } // Disable hangfire to avoid DB connection
                });
            });
        });
    }

    private HttpClient CreateClientWithAuth()
    {
        var client = _factory.CreateClient();
        // Generate a token using the test secret
        // Since we don't have the handy helper from BaseIntegrationTest, we can manually create a token
        // OR, we can assume the endpoint returns 401 if we don't send one, which is also a test.
        // But to hit validation logic, we need to pass Auth.

        // Let's use a simpler approach: Override authentication to bypass it or use a mock scheme?
        // Or just generate a valid JWT.

        var issuer = "ValoraTest";
        var audience = "ValoraTest";
        var key = System.Text.Encoding.UTF8.GetBytes("TestSecretKeyForIntegrationTestingOnly123!");

        var securityKey = new Microsoft.IdentityModel.Tokens.SymmetricSecurityKey(key);
        var credentials = new Microsoft.IdentityModel.Tokens.SigningCredentials(securityKey, Microsoft.IdentityModel.Tokens.SecurityAlgorithms.HmacSha256);

        var claims = new[]
        {
            new System.Security.Claims.Claim(System.Security.Claims.ClaimTypes.NameIdentifier, "test-user-id"),
            new System.Security.Claims.Claim(System.Security.Claims.ClaimTypes.Email, "test@example.com")
        };

        var token = new System.IdentityModel.Tokens.Jwt.JwtSecurityToken(issuer, audience, claims, expires: DateTime.Now.AddMinutes(15), signingCredentials: credentials);
        var tokenString = new System.IdentityModel.Tokens.Jwt.JwtSecurityTokenHandler().WriteToken(token);

        client.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", tokenString);
        return client;
    }

    [Fact]
    public async Task Search_WithInvalidPage_ReturnsBadRequest()
    {
        var client = CreateClientWithAuth();
        // Page 10001 triggers validation error
        var response = await client.GetAsync("/api/search?region=amsterdam&page=10001");
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task Search_WithInvalidOfferingType_ReturnsBadRequest()
    {
        var client = CreateClientWithAuth();
        var response = await client.GetAsync("/api/search?region=amsterdam&offeringType=invalid");
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task Search_WithValidQuery_CallsService()
    {
        var client = CreateClientWithAuth();

        _searchServiceMock.Setup(x => x.SearchAsync(It.IsAny<FundaSearchQuery>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new FundaSearchResult
            {
                Items = new(),
                TotalCount = 0,
                Page = 1,
                PageSize = 20,
                FromCache = false
            });

        // Explicitly provide offeringType to ensure binding doesn't default to null/invalid in test environment
        var response = await client.GetAsync("/api/search?region=amsterdam&page=1&offeringType=buy");

        if (response.StatusCode != HttpStatusCode.OK)
        {
            var content = await response.Content.ReadAsStringAsync();
            Assert.Fail($"Expected OK but got {response.StatusCode}. Content: {content}");
        }

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        _searchServiceMock.Verify(x => x.SearchAsync(It.IsAny<FundaSearchQuery>(), It.IsAny<CancellationToken>()), Times.Once);
    }
}
