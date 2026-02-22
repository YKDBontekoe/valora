using System.Text.Json;
using Valora.Application.Common.Interfaces;
using Valora.Application.Common.Models;
using Valora.Application.DTOs;
using Valora.Domain.Entities;

namespace Valora.Application.Services;

public class UserAiProfileService : IUserAiProfileService
{
    private readonly IUserAiProfileRepository _repository;

    public UserAiProfileService(IUserAiProfileRepository repository)
    {
        _repository = repository;
    }

    public async Task<UserAiProfileDto> GetProfileAsync(string userId, CancellationToken cancellationToken)
    {
        var profile = await _repository.GetByUserIdAsync(userId, cancellationToken);

        if (profile == null)
        {
            return new UserAiProfileDto { UserId = userId };
        }

        return MapToDto(profile);
    }

    public async Task<UserAiProfileDto> UpdateProfileAsync(string userId, UserAiProfileDto dto, CancellationToken cancellationToken)
    {
        var profile = await _repository.GetByUserIdAsync(userId, cancellationToken);

        if (profile == null)
        {
            profile = new UserAiProfile
            {
                UserId = userId,
                CreatedAt = DateTime.UtcNow,
                Preferences = dto.Preferences,
                DisallowedSuggestions = dto.DisallowedSuggestions,
                HouseholdProfile = dto.HouseholdProfile,
                IsEnabled = dto.IsEnabled,
                IsSessionOnlyMode = dto.IsSessionOnlyMode,
                Version = 1,
                UpdatedAt = DateTime.UtcNow
            };

            await _repository.AddAsync(profile, cancellationToken);
        }
        else
        {
            profile.Preferences = dto.Preferences;
            profile.DisallowedSuggestions = dto.DisallowedSuggestions;
            profile.HouseholdProfile = dto.HouseholdProfile;
            profile.IsEnabled = dto.IsEnabled;
            profile.IsSessionOnlyMode = dto.IsSessionOnlyMode;
            profile.Version++;
            profile.UpdatedAt = DateTime.UtcNow;

            await _repository.UpdateAsync(profile, cancellationToken);
        }

        return MapToDto(profile);
    }

    public async Task<Result> DeleteProfileAsync(string userId, CancellationToken cancellationToken)
    {
        var profile = await _repository.GetByUserIdAsync(userId, cancellationToken);

        if (profile != null)
        {
            await _repository.DeleteAsync(profile, cancellationToken);
        }

        return Result.Success();
    }

    public async Task<string> ExportProfileAsync(string userId, CancellationToken cancellationToken)
    {
        var profile = await GetProfileAsync(userId, cancellationToken);
        return JsonSerializer.Serialize(profile, new JsonSerializerOptions { WriteIndented = true });
    }

    private static UserAiProfileDto MapToDto(UserAiProfile profile)
    {
        return new UserAiProfileDto
        {
            UserId = profile.UserId,
            Preferences = profile.Preferences,
            DisallowedSuggestions = profile.DisallowedSuggestions,
            HouseholdProfile = profile.HouseholdProfile,
            IsEnabled = profile.IsEnabled,
            IsSessionOnlyMode = profile.IsSessionOnlyMode,
            Version = profile.Version
        };
    }
}
