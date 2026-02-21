using System.Threading;
using System.Threading.Tasks;
using Valora.Application.DTOs;

namespace Valora.Application.Common.Interfaces;

public interface IContextAnalysisService
{
    Task<string> ChatAsync(string prompt, string? model, CancellationToken cancellationToken);
    Task<AiAnalysisResponse> AnalyzeReportAsync(ContextReportDto report, CancellationToken cancellationToken);
}
