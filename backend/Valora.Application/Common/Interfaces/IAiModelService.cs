using Valora.Domain.Entities;

namespace Valora.Application.Common.Interfaces;

public interface IAiModelService
{
    Task<AiModelConfig?> GetConfigByIntentAsync(string intent, CancellationToken cancellationToken = default);
    Task<IEnumerable<AiModelConfig>> GetAllConfigsAsync(CancellationToken cancellationToken = default);
    Task<AiModelConfig> CreateConfigAsync(AiModelConfig config, CancellationToken cancellationToken = default);
    Task<AiModelConfig> UpdateConfigAsync(AiModelConfig config, CancellationToken cancellationToken = default);
    Task DeleteConfigAsync(Guid id, CancellationToken cancellationToken = default);
    Task<(string PrimaryModel, List<string> FallbackModels)> GetModelsForIntentAsync(string intent, CancellationToken cancellationToken = default);
}
