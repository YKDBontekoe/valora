using Valora.Application.DTOs;

namespace Valora.Application.Common.Interfaces;

public interface IAiModelService
{
    Task<AiModelConfigDto?> GetConfigByIntentAsync(string intent, CancellationToken cancellationToken = default);
    Task<IEnumerable<AiModelConfigDto>> GetAllConfigsAsync(CancellationToken cancellationToken = default);
    Task<AiModelConfigDto> CreateConfigAsync(AiModelConfigDto configDto, CancellationToken cancellationToken = default);
    Task<AiModelConfigDto> UpdateConfigAsync(AiModelConfigDto configDto, CancellationToken cancellationToken = default);
    Task DeleteConfigAsync(Guid id, CancellationToken cancellationToken = default);
    Task<(string PrimaryModel, List<string> FallbackModels)> GetModelsForIntentAsync(string intent, CancellationToken cancellationToken = default);
}
