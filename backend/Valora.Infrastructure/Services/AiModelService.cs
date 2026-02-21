using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Valora.Application.Common.Interfaces;
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

    public async Task<AiModelConfig?> GetConfigByIntentAsync(string intent, CancellationToken cancellationToken = default)
    {
        return await _context.AiModelConfigs
            .FirstOrDefaultAsync(c => c.Intent == intent, cancellationToken);
    }

    public async Task<IEnumerable<AiModelConfig>> GetAllConfigsAsync(CancellationToken cancellationToken = default)
    {
        return await _context.AiModelConfigs.ToListAsync(cancellationToken);
    }

    public async Task<AiModelConfig> CreateConfigAsync(AiModelConfig config, CancellationToken cancellationToken = default)
    {
        _context.AiModelConfigs.Add(config);
        await _context.SaveChangesAsync(cancellationToken);
        return config;
    }

    public async Task<AiModelConfig> UpdateConfigAsync(AiModelConfig config, CancellationToken cancellationToken = default)
    {
        _context.AiModelConfigs.Update(config);
        await _context.SaveChangesAsync(cancellationToken);
        return config;
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
        var config = await GetConfigByIntentAsync(intent, cancellationToken);

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
}
