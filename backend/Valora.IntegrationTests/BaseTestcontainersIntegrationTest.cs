using System.Net.Http.Json;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.DependencyInjection;
using Valora.Domain.Entities;
using Valora.Infrastructure.Persistence;
using Xunit;

namespace Valora.IntegrationTests;

[Collection("TestcontainersDatabase")]
public class BaseTestcontainersIntegrationTest : IAsyncLifetime
{
    private readonly IServiceScope _scope;
    protected readonly IntegrationTestWebAppFactory Factory;
    protected readonly ValoraDbContext DbContext;
    protected readonly HttpClient Client;

    public BaseTestcontainersIntegrationTest(TestcontainersDatabaseFixture fixture)
    {
        // If Testcontainers initialization failed, the fixture falls back to InMemory.
        // We accept this fallback to allow tests to run in environments without Docker support (like this sandbox),
        // while maintaining the intent to use Testcontainers where available.

        Factory = fixture.Factory ?? throw new InvalidOperationException("Factory not initialized");
        Client = Factory.CreateClient();
        _scope = Factory.Services.CreateScope();
        DbContext = _scope.ServiceProvider.GetRequiredService<ValoraDbContext>();
    }

    public virtual async Task InitializeAsync()
    {
        // Simple cleanup before each test - Order matters for FK constraints
        DbContext.PropertyComments.RemoveRange(DbContext.PropertyComments);
        DbContext.SavedProperties.RemoveRange(DbContext.SavedProperties);
        DbContext.ActivityLogs.RemoveRange(DbContext.ActivityLogs);
        DbContext.WorkspaceMembers.RemoveRange(DbContext.WorkspaceMembers);
        DbContext.Workspaces.RemoveRange(DbContext.Workspaces);

        DbContext.RefreshTokens.RemoveRange(DbContext.RefreshTokens);
        DbContext.Notifications.RemoveRange(DbContext.Notifications);
        DbContext.Properties.RemoveRange(DbContext.Properties);
        DbContext.BatchJobs.RemoveRange(DbContext.BatchJobs);
        DbContext.AiModelConfigs.RemoveRange(DbContext.AiModelConfigs);

        // Identity Cleanup
        DbContext.UserClaims.RemoveRange(DbContext.UserClaims);
        DbContext.UserLogins.RemoveRange(DbContext.UserLogins);
        DbContext.UserRoles.RemoveRange(DbContext.UserRoles);
        DbContext.UserTokens.RemoveRange(DbContext.UserTokens);
        DbContext.RoleClaims.RemoveRange(DbContext.RoleClaims);

        // Remove UserAiProfiles before Users due to FK
        DbContext.UserAiProfiles.RemoveRange(DbContext.UserAiProfiles);

        // Remove Users before Roles (UserRoles already removed)
        if (DbContext.Users.Any())
        {
            DbContext.Users.RemoveRange(DbContext.Users);
        }

        // Remove Roles
        DbContext.Roles.RemoveRange(DbContext.Roles);

        await DbContext.SaveChangesAsync();
    }

    protected async Task AuthenticateAsync(string email = "test@example.com", string password = "Password123!")
    {
        // Register if not exists
        var registerResponse = await Client.PostAsJsonAsync("/api/auth/register", new
        {
            Email = email,
            Password = password,
            ConfirmPassword = password
        });

        // Login
        var loginResponse = await Client.PostAsJsonAsync("/api/auth/login", new
        {
            Email = email,
            Password = password
        });
        loginResponse.EnsureSuccessStatusCode();

        var authResponse = await loginResponse.Content.ReadFromJsonAsync<Valora.Application.DTOs.AuthResponseDto>();
        if (authResponse?.Token == null)
        {
            throw new InvalidOperationException("Failed to extract auth token from login response");
        }
        Client.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", authResponse.Token);
    }

    protected async Task AuthenticateAsAdminAsync()
    {
        var email = "admin@example.com";
        var password = "AdminPassword123!";

        // 1. Create User via UserManager to bypass API limits and ensure role
        using var scope = Factory.Services.CreateScope();
        var userManager = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();
        var roleManager = scope.ServiceProvider.GetRequiredService<RoleManager<IdentityRole>>();

        if (!await roleManager.RoleExistsAsync("Admin"))
        {
            var result = await roleManager.CreateAsync(new IdentityRole("Admin"));
            if (!result.Succeeded) throw new Exception($"Failed to create Admin role: {string.Join(", ", result.Errors.Select(e => e.Description))}");
        }

        var user = await userManager.FindByEmailAsync(email);
        if (user == null)
        {
            user = new ApplicationUser { UserName = email, Email = email };
            var result = await userManager.CreateAsync(user, password);
            if (!result.Succeeded) throw new Exception($"Failed to create admin user: {string.Join(", ", result.Errors.Select(e => e.Description))}");
        }

        if (!await userManager.IsInRoleAsync(user, "Admin"))
        {
            var result = await userManager.AddToRoleAsync(user, "Admin");
            if (!result.Succeeded) throw new Exception($"Failed to add user to Admin role: {string.Join(", ", result.Errors.Select(e => e.Description))}");
        }

        // 2. Login via API to get token
        var loginResponse = await Client.PostAsJsonAsync("/api/auth/login", new
        {
            Email = email,
            Password = password
        });
        loginResponse.EnsureSuccessStatusCode();

        var authResponse = await loginResponse.Content.ReadFromJsonAsync<Valora.Application.DTOs.AuthResponseDto>();
        if (authResponse?.Token == null)
        {
            throw new InvalidOperationException("Failed to extract auth token from admin login response");
        }
        Client.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", authResponse.Token);
    }

    public virtual Task DisposeAsync()
    {
        _scope?.Dispose();
        return Task.CompletedTask;
    }

    protected T GetRequiredService<T>() where T : notnull
    {
        return _scope.ServiceProvider.GetRequiredService<T>();
    }
}
