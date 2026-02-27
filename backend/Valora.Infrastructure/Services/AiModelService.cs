using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Valora.Application.Common.Interfaces;
using Valora.Application.DTOs;
using Valora.Domain.Entities;
using Valora.Infrastructure.Persistence;

namespace Valora.Infrastructure.Services;

public class AiModelService : IAiModelService
{
    private readonly ValoraDbContext _context;
    private readonly IConfiguration _configuration;

    // Hardcoded defaults to ensure system works without DB entries
    private readonly Dictionary<string, string> _defaultModels = new()
    {
        { "quick_summary", "openai/gpt-4o-mini" },
        { "detailed_analysis", "openai/gpt-4o" },
        { "chat", "openai/gpt-4o-mini" }
    };

    public AiModelService(ValoraDbContext context, IConfiguration configuration)
    {
        _context = context;
        _configuration = configuration;
    }

    public async Task<AiModelConfigDto?> GetConfigByIntentAsync(string intent, CancellationToken cancellationToken = default)
    {
        var config = await _context.AiModelConfigs
            .FirstOrDefaultAsync(c => c.Intent == intent, cancellationToken);

        return config == null ? null : MapToDto(config);
    }

    public async Task<IEnumerable<AiModelConfigDto>> GetAllConfigsAsync(CancellationToken cancellationToken = default)
    {
        var configs = await _context.AiModelConfigs.ToListAsync(cancellationToken);
        return configs.Select(MapToDto);
    }

    public async Task<AiModelConfigDto> CreateConfigAsync(UpdateAiModelConfigDto dto, CancellationToken cancellationToken = default)
    {
        var config = new AiModelConfig
        {
            Intent = dto.Intent,
            PrimaryModel = dto.PrimaryModel,
            FallbackModels = dto.FallbackModels,
            Description = dto.Description,
            IsEnabled = dto.IsEnabled,
            SafetySettings = dto.SafetySettings
        };

        _context.AiModelConfigs.Add(config);
        await _context.SaveChangesAsync(cancellationToken);
        return MapToDto(config);
    }

    public async Task<AiModelConfigDto> UpdateConfigAsync(Guid id, UpdateAiModelConfigDto dto, CancellationToken cancellationToken = default)
    {
        var config = await _context.AiModelConfigs.FindAsync(new object[] { id }, cancellationToken);

        if (config == null)
        {
            // This case should ideally be handled by checking existence before calling or throwing an exception.
            // For now, assuming the caller ensures existence or handles the potential error.
            // We'll throw a standard exception if not found.
            throw new KeyNotFoundException($"AiModelConfig with ID {id} not found.");
        }

        config.Intent = dto.Intent;
        config.PrimaryModel = dto.PrimaryModel;
        config.FallbackModels = dto.FallbackModels;
        config.Description = dto.Description;
        config.IsEnabled = dto.IsEnabled;
        config.SafetySettings = dto.SafetySettings;

        _context.AiModelConfigs.Update(config);
        await _context.SaveChangesAsync(cancellationToken);
        return MapToDto(config);
    }

    public async Task DeleteConfigAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var config = await _context.AiModelConfigs.FindAsync(new object[] { id }, cancellationToken);
        if (config != null)
        {
            _context.AiModelConfigs.Remove(config);
            await _context.SaveChangesAsync(cancellationToken);
        }
    }

    public async Task<(string PrimaryModel, List<string> FallbackModels)> GetModelsForIntentAsync(string intent, CancellationToken cancellationToken = default)
    {
        var config = await _context.AiModelConfigs
            .FirstOrDefaultAsync(c => c.Intent == intent, cancellationToken);

        if (config != null && config.IsEnabled)
        {
            return (config.PrimaryModel, config.FallbackModels);
        }

        // Return default if not found or disabled
        if (_defaultModels.TryGetValue(intent, out var defaultModel))
        {
            return (defaultModel, new List<string>());
        }

        // Fallback for unknown intents
        return ("openai/gpt-4o-mini", new List<string>());
    }

    private static AiModelConfigDto MapToDto(AiModelConfig entity)
    {
        return new AiModelConfigDto
        {
            Id = entity.Id,
            Intent = entity.Intent,
            PrimaryModel = entity.PrimaryModel,
            FallbackModels = entity.FallbackModels,
            Description = entity.Description,
            IsEnabled = entity.IsEnabled,
            SafetySettings = entity.SafetySettings
        };
    }
}
