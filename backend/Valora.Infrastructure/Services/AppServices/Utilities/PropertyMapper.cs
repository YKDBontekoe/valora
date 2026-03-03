using Valora.Application.DTOs;
using Valora.Domain.Entities;

namespace Valora.Infrastructure.Services.AppServices.Utilities;

public static class PropertyMapper
{
    public static Property FromContextReport(ContextReportDto report)
    {
        return new Property
        {
            Address = report.Location.DisplayAddress,
            City = report.Location.MunicipalityName,
            PostalCode = report.Location.PostalCode,
            Latitude = report.Location.Latitude,
            Longitude = report.Location.Longitude,
            ContextCompositeScore = report.CompositeScore,
            ContextSafetyScore = report.CategoryScores.TryGetValue("Safety", out var safety) ? safety : null,
            ContextSocialScore = report.CategoryScores.TryGetValue("Social", out var social) ? social : null,
            ContextAmenitiesScore = report.CategoryScores.TryGetValue("Amenities", out var amenities) ? amenities : null,
            ContextEnvironmentScore = report.CategoryScores.TryGetValue("Environment", out var environment) ? environment : null,
        };
    }
}
