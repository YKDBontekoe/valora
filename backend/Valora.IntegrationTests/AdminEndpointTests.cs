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
        await AuthenticateAsync("user@example.com", "Password123!");

        // Act
        var response = await Client.GetAsync("/api/admin/users");

        // Assert
        response.StatusCode.ShouldBe(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task GetUsers_ReturnsUsers_WhenAdmin()
    {
        // Arrange
        await AuthenticateAsAdminAsync();

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
                var user = new ApplicationUser { UserName = "alpha", Email = "alpha@test.com" };
                await userManager.CreateAsync(user, "Password123!");
            }
            if (await userManager.FindByEmailAsync("zeta@test.com") == null)
            {
                var user = new ApplicationUser { UserName = "zeta", Email = "zeta@test.com" };
                await userManager.CreateAsync(user, "Password123!");
            }
        }

        // Act
        var response = await Client.GetAsync("/api/admin/users?q=test.com&sort=email_desc");

        // Assert
        response.StatusCode.ShouldBe(HttpStatusCode.OK);
        var content = await response.Content.ReadFromJsonAsync<UsersResponse>();
        content.ShouldNotBeNull();
        var testUsers = content!.Items.Where(u => u.Email.Contains("test.com")).ToList();

        testUsers.ShouldNotBeEmpty();

        var zetaIndex = testUsers.FindIndex(u => u.Email.Contains("zeta"));
        var alphaIndex = testUsers.FindIndex(u => u.Email.Contains("alpha"));

        zetaIndex.ShouldBeGreaterThanOrEqualTo(0);
        alphaIndex.ShouldBeGreaterThanOrEqualTo(0);
        zetaIndex.ShouldBeLessThan(alphaIndex);
    }

    [Fact]
    public async Task CreateUser_ReturnsCreated_WhenValid()
    {
        // Arrange
        await AuthenticateAsAdminAsync();
        var request = new AdminCreateUserDto("newadmin@test.com", "SecurePass123!", new List<string> { "Admin" });

        // Act
        var response = await Client.PostAsJsonAsync("/api/admin/users", request);

        // Assert
        response.StatusCode.ShouldBe(HttpStatusCode.Created);

        using (var scope = Factory.Services.CreateScope())
        {
            var userManager = scope.ServiceProvider.GetRequiredService<Microsoft.AspNetCore.Identity.UserManager<ApplicationUser>>();
            var user = await userManager.FindByEmailAsync(request.Email);
            user.ShouldNotBeNull();
            var roles = await userManager.GetRolesAsync(user);
            roles.ShouldContain("Admin");
        }
    }

    [Fact]
    public async Task CreateUser_ReturnsConflict_WhenUserExists()
    {
        // Arrange
        await AuthenticateAsAdminAsync();
        var request = new AdminCreateUserDto("existing@test.com", "SecurePass123!", new List<string> { "User" });

        await Client.PostAsJsonAsync("/api/admin/users", request);

        // Act
        var response = await Client.PostAsJsonAsync("/api/admin/users", request);

        // Assert
        response.StatusCode.ShouldBe(HttpStatusCode.Conflict);
        // We now check for generic error
        var content = await response.Content.ReadFromJsonAsync<ErrorResponse>();
        content!.Error.ShouldBe("Unable to create user.");
    }

    [Fact]
    public async Task CreateUser_ReturnsBadRequest_WhenRoleInvalid()
    {
        // Arrange
        await AuthenticateAsAdminAsync();
        var request = new AdminCreateUserDto("badrole@test.com", "SecurePass123!", new List<string> { "Hacker" });

        // Act
        var response = await Client.PostAsJsonAsync("/api/admin/users", request);

        // Assert
        response.StatusCode.ShouldBe(HttpStatusCode.BadRequest);
        var content = await response.Content.ReadFromJsonAsync<ErrorResponse>();
        content!.Error.ShouldBe("Operation failed.");
    }

    private class UsersResponse
    {
        public List<AdminUserDto> Items { get; set; } = new();
        public int TotalPages { get; set; }
        public int TotalCount { get; set; }
    }

    private class ErrorResponse
    {
        public string Error { get; set; } = string.Empty;
    }
}
