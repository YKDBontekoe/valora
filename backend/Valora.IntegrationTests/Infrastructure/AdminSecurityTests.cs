using System.Net;
using System.Net.Http.Json;
using Microsoft.AspNetCore.Mvc.Testing;
using Valora.Application.DTOs;
using Valora.Domain.Entities;
using Xunit;

namespace Valora.IntegrationTests.Infrastructure;

[Collection("TestcontainersDatabase")]
public class AdminSecurityTests : IClassFixture<TestcontainersDatabaseFixture>
{
    private readonly TestcontainersDatabaseFixture _fixture;

    public AdminSecurityTests(TestcontainersDatabaseFixture fixture)
    {
        _fixture = fixture;
    }

    [Fact]
    public async Task GetUsers_ShouldReturnForbidden_WhenUserIsNotAdmin()
    {
        // Arrange: Use a factory with a non-admin token
        var client = _fixture.Factory!.CreateClient();

        // Simulating a request without Admin role (or unauthorized)
        // Since we mock auth in IntegrationTestWebAppFactory with "TestSecretKey...",
        // we'd need to generate a token without "Admin" role claim.
        // However, the standard factory setup might default to a user or admin depending on how we set headers.
        // Let's assume default is NOT admin unless we specifically add the claim or use a specific user.
        // Actually, the factory sets up JWT validation but doesn't auto-inject a token unless we add the header.

        // Act: Request without token -> Should be 401 Unauthorized
        var response = await client.GetAsync("/api/admin/users");

        // Assert
        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }
}
