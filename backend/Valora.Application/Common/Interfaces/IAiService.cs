using System.Threading;
using System.Threading.Tasks;

namespace Valora.Application.Common.Interfaces;

public interface IAiService
{
    // Updated signature to use intent instead of direct model ID
    Task<string> ChatAsync(string prompt, string? systemPrompt = null, string intent = "chat", CancellationToken cancellationToken = default);
}
