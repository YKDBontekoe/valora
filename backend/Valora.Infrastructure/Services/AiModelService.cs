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

    public async Task<AiModelConfigDto?> GetConfigByFeatureAsync(string feature, CancellationToken cancellationToken = default)
    {
        var config = await _context.AiModelConfigs
            .FirstOrDefaultAsync(c => c.Feature == feature.ToLowerInvariant(), cancellationToken);

        return config == null ? null : MapToDto(config);
    }

    public async Task<IEnumerable<AiModelConfigDto>> GetAllConfigsAsync(CancellationToken cancellationToken = default)
    {
        var configs = await _context.AiModelConfigs.ToListAsync(cancellationToken);
        return configs.Select(MapToDto).ToList();
    }

    public async Task<AiModelConfigDto> CreateConfigAsync(AiModelConfigDto configDto, CancellationToken cancellationToken = default)
    {
        var config = MapToEntity(configDto);
        config.Feature = config.Feature.ToLowerInvariant();
        _context.AiModelConfigs.Add(config);
        await _context.SaveChangesAsync(cancellationToken);
        return MapToDto(config);
    }

    public async Task<AiModelConfigDto> UpdateConfigAsync(AiModelConfigDto configDto, CancellationToken cancellationToken = default)
    {
        var config = await _context.AiModelConfigs.FindAsync(new object[] { configDto.Id }, cancellationToken);

        if (config == null)
        {
            throw new KeyNotFoundException($"AiModelConfig with ID '{configDto.Id}' not found.");
        }

        // Update properties
        config.Feature = configDto.Feature.ToLowerInvariant();
        config.ModelId = configDto.ModelId;
        config.Description = configDto.Description;
        config.IsEnabled = configDto.IsEnabled;
        config.SafetySettings = configDto.SafetySettings;
        config.SystemPrompt = configDto.SystemPrompt;
        config.Temperature = configDto.Temperature;
        config.MaxTokens = configDto.MaxTokens;

        _context.AiModelConfigs.Update(config);
        await _context.SaveChangesAsync(cancellationToken);
        return MapToDto(config);
    }

    public async Task<bool> DeleteConfigAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var config = await _context.AiModelConfigs.FindAsync(new object[] { id }, cancellationToken);
        if (config != null)
        {
            _context.AiModelConfigs.Remove(config);
            await _context.SaveChangesAsync(cancellationToken);
            return true;
        }
        return false;
    }

    public async Task<string> GetModelForFeatureAsync(string feature, CancellationToken cancellationToken = default)
    {
        var config = await GetConfigByFeatureAsync(feature, cancellationToken);

        if (config != null && config.IsEnabled)
        {
            return config.ModelId;
        }

        // Return default if not found or disabled
        if (_defaultModels.TryGetValue(feature.ToLowerInvariant(), out var defaultModel))
        {
            return defaultModel;
        }

        // Fallback for unknown features
        return "openai/gpt-4o-mini";
    }

    private static AiModelConfigDto MapToDto(AiModelConfig entity)
    {
        return new AiModelConfigDto
        {
            Id = entity.Id,
            Feature = entity.Feature,
            ModelId = entity.ModelId,
            Description = entity.Description,
            IsEnabled = entity.IsEnabled,
            SafetySettings = entity.SafetySettings,
            SystemPrompt = entity.SystemPrompt,
            Temperature = entity.Temperature,
            MaxTokens = entity.MaxTokens
        };
    }

    private static AiModelConfig MapToEntity(AiModelConfigDto dto)
    {
        return new AiModelConfig
        {
            Id = dto.Id,
            Feature = dto.Feature,
            ModelId = dto.ModelId,
            Description = dto.Description,
            IsEnabled = dto.IsEnabled,
            SafetySettings = dto.SafetySettings,
            SystemPrompt = dto.SystemPrompt,
            Temperature = dto.Temperature,
            MaxTokens = dto.MaxTokens
        };
    }
}
