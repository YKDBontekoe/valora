using System.Net;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.EntityFrameworkCore;
using Valora.Infrastructure.Persistence;
using Moq;
using Valora.Application.Common.Interfaces;
using Valora.Application.Common.Models;
using Valora.Application.DTOs;
using Valora.Domain.Entities;
using Xunit;

namespace Valora.IntegrationTests;

public class IsolatedListingTests : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly WebApplicationFactory<Program> _factory;
    private readonly Mock<IListingRepository> _listingRepoMock = new();

    public IsolatedListingTests(WebApplicationFactory<Program> factory)
    {
        _factory = factory.WithWebHostBuilder(builder =>
        {
            builder.UseEnvironment("Testing");

            builder.ConfigureTestServices(services =>
            {
                // Mock the repo
                services.AddScoped(_ => _listingRepoMock.Object);

                // Remove DbContext to avoid Docker/Postgres dependency
                services.RemoveAll<DbContextOptions<ValoraDbContext>>();
                services.RemoveAll<DbContextOptions>();
                services.RemoveAll<ValoraDbContext>();

                // Add InMemory DbContext
                services.AddDbContext<ValoraDbContext>(options =>
                    options.UseInMemoryDatabase("IsolatedListingTestDb")
                           .ConfigureWarnings(w => w.Ignore(Microsoft.EntityFrameworkCore.Diagnostics.RelationalEventId.PendingModelChangesWarning)));
            });

            builder.ConfigureAppConfiguration((_, config) =>
            {
                config.AddInMemoryCollection(new Dictionary<string, string?>
                {
                    { "JWT_SECRET", "TestSecretKeyForIntegrationTestingOnly123!" },
                    { "JWT_ISSUER", "ValoraTest" },
                    { "JWT_AUDIENCE", "ValoraTest" },
                    { "HANGFIRE_ENABLED", "false" }
                });
            });
        });
    }

    private HttpClient CreateClientWithAuth()
    {
        var client = _factory.CreateClient();

        var key = System.Text.Encoding.UTF8.GetBytes("TestSecretKeyForIntegrationTestingOnly123!");
        var securityKey = new Microsoft.IdentityModel.Tokens.SymmetricSecurityKey(key);
        var credentials = new Microsoft.IdentityModel.Tokens.SigningCredentials(securityKey, Microsoft.IdentityModel.Tokens.SecurityAlgorithms.HmacSha256);

        var claims = new[]
        {
            new System.Security.Claims.Claim(System.Security.Claims.ClaimTypes.NameIdentifier, "test-user-id"),
            new System.Security.Claims.Claim(System.Security.Claims.ClaimTypes.Email, "test@example.com")
        };

        var token = new System.IdentityModel.Tokens.Jwt.JwtSecurityToken("ValoraTest", "ValoraTest", claims, expires: DateTime.Now.AddMinutes(15), signingCredentials: credentials);
        var tokenString = new System.IdentityModel.Tokens.Jwt.JwtSecurityTokenHandler().WriteToken(token);

        client.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", tokenString);
        return client;
    }

    [Fact]
    public async Task GetAllListings_WithValidFilter_ReturnsOk()
    {
        var client = CreateClientWithAuth();

        _listingRepoMock.Setup(x => x.GetAllAsync(It.IsAny<ListingFilterDto>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new PaginatedList<ListingDto>(new List<ListingDto>(), 0, 1, 10));

        var response = await client.GetAsync("/api/listings?page=1&pageSize=10");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        _listingRepoMock.Verify(x => x.GetAllAsync(It.IsAny<ListingFilterDto>(), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task GetAllListings_WithInvalidPage_ReturnsBadRequest()
    {
        var client = CreateClientWithAuth();

        // Page 10001 is out of range
        var response = await client.GetAsync("/api/listings?page=10001");

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }
}
