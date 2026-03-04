using System.Threading;
using System.Threading.Tasks;
using System.Collections.Generic;
using Valora.Application.DTOs;

namespace Valora.Application.Common.Interfaces;

public interface IAiService
{
    // Updated signature to use intent instead of direct model ID
    Task<string> ChatAsync(string prompt, string? systemPrompt = null, string feature = "chat", CancellationToken cancellationToken = default);

    Task<IEnumerable<AiModelDto>> GetAvailableModelsAsync(CancellationToken cancellationToken = default);
}
