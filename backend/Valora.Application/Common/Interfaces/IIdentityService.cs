using Valora.Application.Common.Models;
using Valora.Application.DTOs;
using Valora.Domain.Entities;

namespace Valora.Application.Common.Interfaces;

public interface IIdentityService
{
    Task<(Result Result, string UserId)> CreateUserAsync(string email, string password, List<string>? preferredCities = null);
    Task<Result> UpdateUserAsync(string userId, UpdateProfileDto profileDto);
    Task<bool> CheckPasswordAsync(string email, string password);
    Task<ApplicationUser?> GetUserByEmailAsync(string email);
    Task<ApplicationUser?> GetUserByIdAsync(string userId);
}
