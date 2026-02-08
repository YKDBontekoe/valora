using Valora.Application.DTOs;

namespace Valora.Application.Common.Interfaces;

public interface IContextReportService
{
    Task<ContextReportDto> BuildAsync(ContextReportRequestDto request, CancellationToken cancellationToken = default);
}
