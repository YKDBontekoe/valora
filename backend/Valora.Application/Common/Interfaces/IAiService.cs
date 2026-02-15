using System.Threading;
using System.Threading.Tasks;

namespace Valora.Application.Common.Interfaces;

public interface IAiService
{
    // Rearrange signature for better ergonomics: (prompt, systemPrompt, model, cancellationToken)
    Task<string> ChatAsync(string prompt, string? systemPrompt = null, string? model = null, CancellationToken cancellationToken = default);
}
