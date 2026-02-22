using Valora.Application.DTOs;
using Valora.Application.DTOs.Ai;

namespace Valora.Application.Common.Interfaces;

public interface IContextAnalysisService
{
    Task<string> ChatAsync(string prompt, string? intent, CancellationToken cancellationToken);
    Task<string> AnalyzeReportAsync(ContextReportDto report, CancellationToken cancellationToken);
    Task<MapQueryResponse> PlanMapQueryAsync(MapQueryRequest request, CancellationToken cancellationToken);
}
