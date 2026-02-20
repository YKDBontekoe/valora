using System.Net;
using System.Net.Http.Json;
using Shouldly;
using Microsoft.Extensions.DependencyInjection;
using Valora.Application.DTOs;
using Valora.Domain.Entities;
using Xunit;

namespace Valora.IntegrationTests;

[Collection("TestDatabase")]
public class AdminEndpointTests : BaseIntegrationTest
{
    public AdminEndpointTests(TestDatabaseFixture fixture) : base(fixture)
    {
    }

    [Fact]
    public async Task GetUsers_ReturnsUnauthorized_WhenNotAuthenticated()
    {
        var response = await Client.GetAsync("/api/admin/users");
        response.StatusCode.ShouldBe(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task GetUsers_ReturnsForbidden_WhenUserIsNotAdmin()
    {
        // Arrange
        await AuthenticateAsync("user@example.com", "Password123!"); // Regular user (BaseIntegrationTest helper)

        // Act
        var response = await Client.GetAsync("/api/admin/users");

        // Assert
        response.StatusCode.ShouldBe(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task GetUsers_ReturnsUsers_WhenAdmin()
    {
        // Arrange
        await AuthenticateAsAdminAsync(); // BaseIntegrationTest helper

        // Act
        var response = await Client.GetAsync("/api/admin/users");

        // Assert
        response.StatusCode.ShouldBe(HttpStatusCode.OK);
        var content = await response.Content.ReadFromJsonAsync<UsersResponse>();
        content.ShouldNotBeNull();
        content!.Items.ShouldNotBeEmpty();
    }

    [Fact]
    public async Task GetUsers_FiltersAndSorts_WhenParametersProvided()
    {
        // Arrange
        await AuthenticateAsAdminAsync();

        // Ensure we have known users
        using (var scope = Factory.Services.CreateScope())
        {
            var userManager = scope.ServiceProvider.GetRequiredService<Microsoft.AspNetCore.Identity.UserManager<ApplicationUser>>();
            if (await userManager.FindByEmailAsync("alpha@test.com") == null)
            {
                await userManager.CreateAsync(new ApplicationUser { UserName = "alpha", Email = "alpha@test.com" }, "Password123!");
            }
            if (await userManager.FindByEmailAsync("zeta@test.com") == null)
            {
                await userManager.CreateAsync(new ApplicationUser { UserName = "zeta", Email = "zeta@test.com" }, "Password123!");
            }
        }

        // Act
        var response = await Client.GetAsync("/api/admin/users?q=test.com&sort=email_desc");

        // Assert
        response.StatusCode.ShouldBe(HttpStatusCode.OK);
        var content = await response.Content.ReadFromJsonAsync<UsersResponse>();
        content.ShouldNotBeNull();
        content!.Items.ShouldContain(u => u.Email.Contains("test.com"));

        // Check sorting: Z before A
        // Note: The response list might contain other users, so we filter in memory to verify relative order of our test users
        var testUsers = content.Items.Where(u => u.Email.Contains("test.com")).ToList();
        var zetaIndex = testUsers.FindIndex(u => u.Email.Contains("zeta"));
        var alphaIndex = testUsers.FindIndex(u => u.Email.Contains("alpha"));

        // If both exist, verify order
        if (zetaIndex >= 0 && alphaIndex >= 0)
        {
            zetaIndex.ShouldBeLessThan(alphaIndex);
        }
    }

    private class UsersResponse
    {
        public List<AdminUserDto> Items { get; set; } = new();
        public int TotalPages { get; set; }
        public int TotalCount { get; set; }
    }
}
