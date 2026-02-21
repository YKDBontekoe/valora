using System.ComponentModel.DataAnnotations;
using Valora.Application.Common.Validation;

namespace Valora.Application.DTOs;

public sealed record AiAnalysisRequest(
    [property: Required] [property: ValidateObject] ContextReportDto Report
);

public sealed record AiAnalysisResponse(
    string Summary,
    List<string> TopPositives,
    List<string> TopConcerns,
    int Confidence,
    string Disclaimer
);
