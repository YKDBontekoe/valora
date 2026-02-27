using Valora.Application.DTOs;

namespace Valora.Application.Common.Interfaces;

public interface IAiModelService
{
    Task<AiModelConfigDto?> GetConfigByIntentAsync(string intent, CancellationToken cancellationToken = default);
    Task<IEnumerable<AiModelConfigDto>> GetAllConfigsAsync(CancellationToken cancellationToken = default);
    Task<AiModelConfigDto> CreateConfigAsync(UpdateAiModelConfigDto config, CancellationToken cancellationToken = default);
    Task<AiModelConfigDto> UpdateConfigAsync(Guid id, UpdateAiModelConfigDto config, CancellationToken cancellationToken = default);
    Task DeleteConfigAsync(Guid id, CancellationToken cancellationToken = default);
    Task<(string PrimaryModel, List<string> FallbackModels)> GetModelsForIntentAsync(string intent, CancellationToken cancellationToken = default);
}
