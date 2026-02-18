using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using Valora.Application.Common.Models;
using Valora.Application.DTOs;
using Valora.Domain.Entities;
using Valora.Infrastructure.Persistence;
using Xunit;

namespace Valora.IntegrationTests;

[Collection("TestDatabase")]
public class AdminEndpointTests
{
    private readonly IntegrationTestWebAppFactory _factory;
    private readonly HttpClient _client;

    public AdminEndpointTests(TestDatabaseFixture fixture)
    {
        _factory = fixture.Factory;
        _client = _factory.CreateClient();
    }

    [Fact]
    public async Task GetUsers_ReturnsUnauthorized_WhenNotAuthenticated()
    {
        var response = await _client.GetAsync("/api/admin/users");
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    // Helper to authenticate
    private async Task AuthenticateAsync(string email, string role)
    {
        // 1. Create User via UserManager directly
        using var scope = _factory.Services.CreateScope();
        var userManager = scope.ServiceProvider.GetRequiredService<Microsoft.AspNetCore.Identity.UserManager<ApplicationUser>>();
        var roleManager = scope.ServiceProvider.GetRequiredService<Microsoft.AspNetCore.Identity.RoleManager<Microsoft.AspNetCore.Identity.IdentityRole>>();

        if (!await roleManager.RoleExistsAsync(role))
        {
            await roleManager.CreateAsync(new Microsoft.AspNetCore.Identity.IdentityRole(role));
        }

        var user = await userManager.FindByEmailAsync(email);
        if (user == null)
        {
            user = new ApplicationUser { UserName = email, Email = email };
            await userManager.CreateAsync(user, "Password123!");
        }

        if (!await userManager.IsInRoleAsync(user, role))
        {
            await userManager.AddToRoleAsync(user, role);
        }

        // 2. Login via API
        var loginResponse = await _client.PostAsJsonAsync("/api/auth/login", new
        {
            Email = email,
            Password = "Password123!"
        });
        loginResponse.EnsureSuccessStatusCode();
        var authResponse = await loginResponse.Content.ReadFromJsonAsync<Valora.Application.DTOs.AuthResponseDto>();
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", authResponse!.Token);
    }

    [Fact]
    public async Task GetUsers_ReturnsForbidden_WhenUserIsNotAdmin()
    {
        // Arrange
        await AuthenticateAsync("user@example.com", "User"); // Regular user

        // Act
        var response = await _client.GetAsync("/api/admin/users");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task GetUsers_ReturnsUsers_WhenAdmin()
    {
        // Arrange
        await AuthenticateAsync("admin@example.com", "Admin");

        // Act
        var response = await _client.GetAsync("/api/admin/users");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var content = await response.Content.ReadFromJsonAsync<UsersResponse>();
        content.Should().NotBeNull();
        content!.Items.Should().NotBeEmpty();
    }

    [Fact]
    public async Task GetUsers_FiltersAndSorts_WhenParametersProvided()
    {
        // Arrange
        await AuthenticateAsync("admin@example.com", "Admin");

        // Ensure we have known users
        using (var scope = _factory.Services.CreateScope())
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
        var response = await _client.GetAsync("/api/admin/users?q=test.com&sort=email_desc");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var content = await response.Content.ReadFromJsonAsync<UsersResponse>();
        content.Should().NotBeNull();
        content!.Items.Should().Contain(u => u.Email.Contains("test.com"));

        // Check sorting: Z before A
        // Note: The response list might contain other users, so we filter in memory to verify relative order of our test users
        var testUsers = content.Items.Where(u => u.Email.Contains("test.com")).ToList();
        var zetaIndex = testUsers.FindIndex(u => u.Email.Contains("zeta"));
        var alphaIndex = testUsers.FindIndex(u => u.Email.Contains("alpha"));

        // If both exist, verify order
        if (zetaIndex >= 0 && alphaIndex >= 0)
        {
            zetaIndex.Should().BeLessThan(alphaIndex);
        }
    }

    private class UsersResponse
    {
        public List<AdminUserDto> Items { get; set; } = new();
        public int TotalPages { get; set; }
        public int TotalCount { get; set; }
    }
}
