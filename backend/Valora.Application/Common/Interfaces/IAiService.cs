using System.Threading;
using System.Threading.Tasks;

namespace Valora.Application.Common.Interfaces;

public interface IAiService
{
    Task<string> ChatAsync(string prompt, string? model = null, string? systemPrompt = null, CancellationToken cancellationToken = default);
}
