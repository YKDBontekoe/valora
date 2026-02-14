using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Logging;
using Valora.Application.Common.Interfaces;
using Valora.Application.Common.Models;
using Valora.Domain.Entities;

namespace Valora.Infrastructure.Services;

public class IdentityService : IIdentityService
{
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly RoleManager<IdentityRole> _roleManager;
    private readonly ILogger<IdentityService> _logger;

    public IdentityService(
        UserManager<ApplicationUser> userManager,
        RoleManager<IdentityRole> roleManager,
        ILogger<IdentityService> logger)
    {
        _userManager = userManager;
        _roleManager = roleManager;
        _logger = logger;
    }

    public async Task<(Result Result, string UserId)> CreateUserAsync(string email, string password)
    {
        var user = new ApplicationUser
        {
            UserName = email,
            Email = email
        };

        var result = await _userManager.CreateAsync(user, password);

        if (result.Succeeded)
        {
            _logger.LogInformation("User created: {UserId}", user.Id);
        }
        else
        {
            _logger.LogWarning("User creation failed for {Email}: {Errors}", email, string.Join(", ", result.Errors.Select(e => e.Description)));
        }

        return (ToApplicationResult(result), user.Id);
    }

    public async Task<bool> CheckPasswordAsync(string email, string password)
    {
        var user = await _userManager.FindByEmailAsync(email);
        if (user == null)
        {
            return false;
        }

        return await _userManager.CheckPasswordAsync(user, password);
    }

    public async Task<ApplicationUser?> GetUserByEmailAsync(string email)
    {
        return await _userManager.FindByEmailAsync(email);
    }

    public async Task<ApplicationUser?> GetUserByIdAsync(string userId)
    {
        return await _userManager.FindByIdAsync(userId);
    }

    public async Task EnsureRoleAsync(string roleName)
    {
        if (!await _roleManager.RoleExistsAsync(roleName))
        {
            var result = await _roleManager.CreateAsync(new IdentityRole(roleName));
            if (!result.Succeeded)
            {
                _logger.LogError("Failed to create role {RoleName}: {Errors}", roleName, string.Join(", ", result.Errors.Select(e => e.Description)));
                throw new Exception($"Failed to create role '{roleName}': {string.Join(", ", result.Errors.Select(e => e.Description))}");
            }
            _logger.LogInformation("Role created: {RoleName}", roleName);
        }
    }

    public async Task<Result> AddToRoleAsync(string userId, string roleName)
    {
        var user = await _userManager.FindByIdAsync(userId);
        if (user == null)
        {
            _logger.LogWarning("AddToRole failed: User {UserId} not found", userId);
            return Result.Failure(new[] { "Operation failed." });
        }

        if (!await _userManager.IsInRoleAsync(user, roleName))
        {
            var result = await _userManager.AddToRoleAsync(user, roleName);
            if (result.Succeeded)
            {
                _logger.LogInformation("User {UserId} added to role {RoleName}", userId, roleName);
            }
            else
            {
                _logger.LogWarning("Failed to add user {UserId} to role {RoleName}: {Errors}", userId, roleName, string.Join(", ", result.Errors.Select(e => e.Description)));
            }
            return ToApplicationResult(result);
        }

        return Result.Success();
    }

    public async Task<Result> UpdateProfileAsync(string userId, string? firstName, string? lastName, int defaultRadiusMeters, bool biometricsEnabled)
    {
        var user = await _userManager.FindByIdAsync(userId);
        if (user == null)
        {
            _logger.LogWarning("UpdateProfile failed: User {UserId} not found", userId);
            return Result.Failure(new[] { "User not found." });
        }

        user.FirstName = firstName;
        user.LastName = lastName;
        user.DefaultRadiusMeters = defaultRadiusMeters;
        user.BiometricsEnabled = biometricsEnabled;

        var result = await _userManager.UpdateAsync(user);
        if (result.Succeeded)
        {
            _logger.LogInformation("Profile updated for user {UserId}", userId);
        }
        else
        {
            _logger.LogWarning("Profile update failed for user {UserId}: {Errors}", userId, string.Join(", ", result.Errors.Select(e => e.Description)));
        }
        return ToApplicationResult(result);
    }

    public async Task<Result> ChangePasswordAsync(string userId, string currentPassword, string newPassword)
    {
        var user = await _userManager.FindByIdAsync(userId);
        if (user == null)
        {
            _logger.LogWarning("ChangePassword failed: User {UserId} not found", userId);
            return Result.Failure(new[] { "User not found." });
        }

        var result = await _userManager.ChangePasswordAsync(user, currentPassword, newPassword);
        if (result.Succeeded)
        {
            _logger.LogInformation("Password changed for user {UserId}", userId);
        }
        else
        {
            _logger.LogWarning("Password change failed for user {UserId}: {Errors}", userId, string.Join(", ", result.Errors.Select(e => e.Description)));
        }
        return ToApplicationResult(result);
    }

    private static Result ToApplicationResult(IdentityResult result)
    {
        return result.Succeeded
            ? Result.Success()
            : Result.Failure(result.Errors.Select(e => e.Description));
    }
}
