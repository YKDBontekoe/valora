using Valora.Application.Common.Exceptions;
using Valora.Application.Common.Interfaces;
using Valora.Application.DTOs;
using Valora.Domain.Entities;
using Valora.Application.Enrichment.Builders;
using System.Globalization;

namespace Valora.Application.Services;

public class ListingService : IListingService
{
    private readonly IWorkspaceRepository _repository;
    private readonly IContextDataProvider _contextDataProvider;

    public ListingService(IWorkspaceRepository repository, IContextDataProvider contextDataProvider)
    {
        _repository = repository;
        _contextDataProvider = contextDataProvider;
    }

    public async Task<ListingDetailDto> GetListingDetailAsync(Guid listingId, CancellationToken ct = default)
    {
        var listing = await _repository.GetListingAsync(listingId, ct);
        if (listing == null) throw new NotFoundException(nameof(Listing), listingId);

        var currentReport = listing.ContextReport;

        // Try to fetch context report real-time if not in DB yet (graceful partial responses)
        if (currentReport == null && listing.Latitude.HasValue && listing.Longitude.HasValue)
        {
            var location = new Valora.Application.DTOs.ResolvedLocationDto(
                listing.Address, // Query
                listing.Address, // DisplayAddress
                listing.Latitude.Value,
                listing.Longitude.Value,
                null, // RdX
                null, // RdY
                null, // MunicipalityCode
                null, // MunicipalityName
                null, // DistrictCode
                null, // DistrictName
                null, // NeighborhoodCode
                null, // NeighborhoodName
                listing.PostalCode
            );

            try
            {
                using var cts = CancellationTokenSource.CreateLinkedTokenSource(ct);
                cts.CancelAfter(TimeSpan.FromSeconds(3)); // Tight timeout
                var sourceData = await _contextDataProvider.GetSourceDataAsync(location, 1000, cts.Token);
                var warnings = new List<string>(sourceData.Warnings);
                var dto = ContextReportBuilder.Build(location, sourceData, warnings);

                // For simplicity we create a dummy model, real implementation would map the dto to the Model.
                currentReport = new Valora.Domain.Models.ContextReportModel {
                    CompositeScore = dto.CompositeScore
                };
            }
            catch(Exception)
            {
                 // Ignore timeout/error, partial response is better than failure
            }
        }

        return new ListingDetailDto(
            listing.Id,
            listing.Address,
            listing.City,
            listing.PostalCode,
            listing.Price,
            listing.Bedrooms,
            listing.Bathrooms,
            listing.LivingAreaM2,
            listing.PlotAreaM2,
            listing.PropertyType,
            listing.Status,
            listing.Url,
            listing.ImageUrl,
            listing.ListedDate,
            listing.Description,
            listing.EnergyLabel,
            listing.YearBuilt,
            listing.ImageUrls,
            listing.Latitude,
            listing.Longitude,
            listing.ContextCompositeScore,
            listing.ContextSafetyScore,
            listing.ContextSocialScore,
            listing.ContextAmenitiesScore,
            listing.ContextEnvironmentScore,
            currentReport
        );
    }
}
