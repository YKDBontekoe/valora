using Microsoft.AspNetCore.Identity;
using Valora.Application.Common.Interfaces;
using Valora.Application.Common.Models;
using Valora.Application.DTOs;
using Valora.Domain.Entities;

namespace Valora.Infrastructure.Services;

public class IdentityService : IIdentityService
{
    private readonly UserManager<ApplicationUser> _userManager;

    public IdentityService(UserManager<ApplicationUser> userManager)
    {
        _userManager = userManager;
    }

    public async Task<(Result Result, string UserId)> CreateUserAsync(string email, string password, List<string>? preferredCities = null)
    {
        var user = new ApplicationUser
        {
            UserName = email,
            Email = email,
            PreferredCities = preferredCities ?? new List<string>()
        };

        var result = await _userManager.CreateAsync(user, password);

        return (ToApplicationResult(result), user.Id);
    }

    public async Task<Result> UpdateUserAsync(string userId, UpdateProfileDto profileDto)
    {
        var user = await _userManager.FindByIdAsync(userId);
        if (user == null)
        {
            return Result.Failure(new[] { "User not found" });
        }

        user.PreferredCities = profileDto.PreferredCities;

        var result = await _userManager.UpdateAsync(user);
        return ToApplicationResult(result);
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

    private static Result ToApplicationResult(IdentityResult result)
    {
        return result.Succeeded
            ? Result.Success()
            : Result.Failure(result.Errors.Select(e => e.Description));
    }
}
