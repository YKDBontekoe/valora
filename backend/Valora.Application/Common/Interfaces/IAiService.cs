using System.Threading;
using System.Threading.Tasks;
using Valora.Application.Common.Models;

namespace Valora.Application.Common.Interfaces;

public interface IAiService
{
    Task<string> ChatAsync(
        string prompt,
        string? model = null,
        AiExecutionOptions? options = null,
        CancellationToken cancellationToken = default);
}
