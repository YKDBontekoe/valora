using Valora.Application.DTOs;

namespace Valora.Application.Common.Interfaces;

public interface IAiModelProvider
{
    Task<List<ExternalAiModelDto>> GetAvailableModelsAsync(CancellationToken cancellationToken = default);
}
