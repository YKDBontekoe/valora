using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Valora.Application.Common.Interfaces;
using Valora.Application.Common.Models;
using Valora.Domain.Entities;
using Valora.Infrastructure.Persistence;
using Valora.Infrastructure.Utilities;

namespace Valora.Infrastructure.Services;

public class IdentityService : IIdentityService
{
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly RoleManager<IdentityRole> _roleManager;
    private readonly ValoraDbContext _context;

    // Static dummy hash to avoid recomputation and thread contention
    // We use a fixed hash generated with default Identity settings (PBKDF2)
    private static readonly string _dummyHash = "AQAAAAIAAYagAAAAEP0w+Z3w+Z3w+Z3w+Z3w+Z3w+Z3w+Z3w+Z3w+Z3w+Z3w+Z3w+Z3w+Z3w+Z3w+Z3w+Q==";

    public IdentityService(
        UserManager<ApplicationUser> userManager,
        RoleManager<IdentityRole> roleManager,
        ValoraDbContext context)
    {
        _userManager = userManager;
        _roleManager = roleManager;
        _context = context;
    }

    public async Task<(Result Result, string UserId)> CreateUserAsync(string email, string password)
    {
        var user = new ApplicationUser
        {
            UserName = email,
            Email = email
        };

        var userCreationResult = await _userManager.CreateAsync(user, password);

        return (ToApplicationResult(userCreationResult), user.Id);
    }

    public async Task<bool> CheckPasswordAsync(string email, string password)
    {
        var user = await _userManager.FindByEmailAsync(email);
        if (user == null)
        {
            // Mitigate timing attacks by performing a dummy verification
            // forcing this path to take approximately the same time as the success path
            // We call VerifyHashedPassword directly (CPU bound) instead of Task.Run to avoid thread pool scheduling variance
            _userManager.PasswordHasher.VerifyHashedPassword(new ApplicationUser(), _dummyHash, password);
            return false;
        }

        return await _userManager.CheckPasswordAsync(user, password);
    }

    public async Task<ApplicationUser?> GetUserByEmailAsync(string email)
    {
        return await _userManager.FindByEmailAsync(email);
    }

    public async Task EnsureRoleAsync(string roleName)
    {
        if (!await _roleManager.RoleExistsAsync(roleName))
        {
            var roleCreationResult = await _roleManager.CreateAsync(new IdentityRole(roleName));
            if (!roleCreationResult.Succeeded)
            {
                throw new Exception($"Failed to create role '{roleName}': {string.Join(", ", roleCreationResult.Errors.Select(e => e.Description))}");
            }
        }
    }

    public async Task<Result> AddToRoleAsync(string userId, string roleName)
    {
        var user = await _userManager.FindByIdAsync(userId);
        if (user == null) return Result.Failure(new[] { "Operation failed." });

        if (!await _userManager.IsInRoleAsync(user, roleName))
        {
            var roleAssignmentResult = await _userManager.AddToRoleAsync(user, roleName);
            return ToApplicationResult(roleAssignmentResult);
        }

        return Result.Success();
    }

    public async Task<PaginatedList<ApplicationUser>> GetUsersAsync(int pageNumber, int pageSize, string? searchQuery = null, string? sortBy = null)
    {
        var query = _userManager.Users.AsQueryable();

        if (!string.IsNullOrWhiteSpace(searchQuery))
        {
            if (_context.Database.IsRelational())
            {
                var escapedQuery = EscapeLikePattern(searchQuery);
                query = query.Where(u => EF.Functions.Like(u.Email!, $"%{escapedQuery}%", "\\"));
            }
            else
            {
                query = query.Where(u => u.Email != null && u.Email.Contains(searchQuery));
            }
        }

        query = sortBy?.ToLower() switch
        {
            "email_desc" => query.OrderByDescending(u => u.Email),
            "email_asc" => query.OrderBy(u => u.Email),
            _ => query.OrderBy(u => u.Email) // Default sort
        };

        var totalUsers = await query.CountAsync();
        var usersPage = await query
            .Skip((pageNumber - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();

        return new PaginatedList<ApplicationUser>(usersPage, totalUsers, pageNumber, pageSize);
    }

    private static string EscapeLikePattern(string pattern)
    {
        return pattern
            .Replace("\\", "\\\\")
            .Replace("%", "\\%")
            .Replace("_", "\\_");
    }

    public async Task<Result> DeleteUserAsync(string userId)
    {
        var user = await _userManager.FindByIdAsync(userId);
        if (user == null) return Result.Failure(new[] { "User not found." });

        // Manually clean up related entities because many are configured with NoAction delete behavior
        // to prevent cycles or accidental data loss.
        if (_context.Database.IsRelational())
        {
            await using var transaction = await _context.Database.BeginTransactionAsync();
            try
            {
                await UserCleanupUtility.CleanupUserDataAsync(_context, userId);

                var deleteResult = await _userManager.DeleteAsync(user);

                if (deleteResult.Succeeded)
                {
                    await transaction.CommitAsync();
                }
                else
                {
                    await transaction.RollbackAsync();
                }

                return ToApplicationResult(deleteResult);
            }
            catch
            {
                await transaction.RollbackAsync();
                throw;
            }
        }
        else
        {
            await UserCleanupUtility.CleanupUserDataAsync(_context, userId);

            try
            {
                var deleteResult = await _userManager.DeleteAsync(user);
                return ToApplicationResult(deleteResult);
            }
            catch (Exception)
            {
                throw;
            }
        }
    }

    public async Task<IList<string>> GetUserRolesAsync(ApplicationUser user)
    {
        return await _userManager.GetRolesAsync(user);
    }

    public async Task<int> CountAsync()
    {
        return await _userManager.Users.CountAsync();
    }

    public async Task<IDictionary<string, IList<string>>> GetRolesForUsersAsync(IEnumerable<ApplicationUser> users)
    {
        var userIds = users.Select(u => u.Id).ToList();

        var userRoles = await _context.UserRoles
            .Where(ur => userIds.Contains(ur.UserId))
            .Join(_context.Roles,
                ur => ur.RoleId,
                r => r.Id,
                (ur, r) => new { ur.UserId, RoleName = r.Name })
            .ToListAsync();

        return userRoles
            .GroupBy(ur => ur.UserId)
            .ToDictionary(
                g => g.Key,
                g => (IList<string>)g.Select(ur => ur.RoleName!).ToList());
    }

    private static Result ToApplicationResult(IdentityResult result)
    {
        return result.Succeeded
            ? Result.Success()
            : Result.Failure(result.Errors.Select(e => e.Description));
    }
}
