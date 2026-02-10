using System.Text.Json;
using Valora.Application.DTOs;
using Valora.Domain.Models;

namespace Valora.Application.Common.Interfaces;

public interface IPdokListingMapper
{
    ListingDto MapToDto(JsonElement doc, string pdokId, ContextReportModel? contextReport, double? compositeScore, double? safetyScore);
}
