using Valora.Application.Common.Models;
using Valora.Domain.Entities;

namespace Valora.Application.Common.Interfaces;

public interface IIdentityService
{
    Task<(Result Result, string UserId)> CreateUserAsync(string email, string password);
    Task<bool> CheckPasswordAsync(string email, string password);
    Task<ApplicationUser?> GetUserByEmailAsync(string email);
}
