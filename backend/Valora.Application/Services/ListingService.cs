using System.ComponentModel.DataAnnotations;
using Valora.Application.Common.Exceptions;
using Valora.Application.Common.Interfaces;
using Valora.Application.Common.Mappings;
using Valora.Application.Common.Models;
using Valora.Application.DTOs;
using Valora.Application.Enrichment;
using Valora.Domain.Common;
using Valora.Domain.Entities;
using ValidationException = Valora.Application.Common.Exceptions.ValidationException;

namespace Valora.Application.Services;

public class ListingService : IListingService
{
    private const int DefaultSearchRadiusMeters = 1000;

    private readonly IListingRepository _repository;
    private readonly IPdokListingService _pdokService;
    private readonly IContextReportService _contextReportService;

    public ListingService(
        IListingRepository repository,
        IPdokListingService pdokService,
        IContextReportService contextReportService)
    {
        _repository = repository;
        _pdokService = pdokService;
        _contextReportService = contextReportService;
    }

    public async Task<PaginatedList<ListingSummaryDto>> GetListingsAsync(ListingFilterDto filter, CancellationToken cancellationToken = default)
    {
        ValidateFilter(filter);
        return await _repository.GetSummariesAsync(filter, cancellationToken);
    }

    private static void ValidateFilter(ListingFilterDto filter)
    {
        var validationContext = new ValidationContext(filter);
        var validationResults = new List<ValidationResult>();

        if (!Validator.TryValidateObject(filter, validationContext, validationResults, true))
        {
            var errors = validationResults
                .GroupBy(x => x.MemberNames.FirstOrDefault() ?? "General")
                .ToDictionary(
                    g => g.Key,
                    g => g.Select(x => x.ErrorMessage ?? "Unknown error").ToArray());

            throw new ValidationException(errors);
        }
    }

    public async Task<ListingDto?> GetListingByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var listing = await _repository.GetByIdAsync(id, cancellationToken);
        return listing == null ? null : ListingMapper.ToDto(listing);
    }

    /// <summary>
    /// Retrieves a listing from the external PDOK service and persists it to the database.
    /// </summary>
    /// <remarks>
    /// <para>
    /// This method acts as a "Read-Through" cache. If the listing is found in PDOK,
    /// it is immediately upserted (created or updated) in the local database.
    /// </para>
    /// <para>
    /// This ensures that:
    /// 1. We always have the latest data (price, status) when a user views a listing.
    /// 2. We build up our own database of listings organically without a massive scraper.
    /// </para>
    /// </remarks>
    public async Task<ListingDto?> GetPdokListingAsync(string externalId, CancellationToken cancellationToken = default)
    {
        ValidateExternalId(externalId);

        var listingDto = await _pdokService.GetListingDetailsAsync(externalId, cancellationToken);
        if (listingDto is null)
        {
            return null;
        }

        await CreateOrUpdateListingAsync(listingDto, cancellationToken);

        return listingDto;
    }

    /// <summary>
    /// Performs an upsert operation for the listing entity.
    /// </summary>
    /// <remarks>
    /// <para>
    /// We use the FundaId (or external ID) as the unique key for deduplication.
    /// Even if the listing exists, we update it to capture price changes or status updates.
    /// </para>
    /// <para>
    /// <c>LastFundaFetchUtc</c> is updated to track data freshness.
    /// </para>
    /// </remarks>
    private async Task CreateOrUpdateListingAsync(ListingDto listingDto, CancellationToken cancellationToken)
    {
        var existing = await _repository.GetByFundaIdAsync(listingDto.FundaId, cancellationToken);
        var utcNow = DateTime.UtcNow;

        if (existing is null)
        {
            var newListing = ListingMapper.ToEntity(listingDto);
            newListing.LastFundaFetchUtc = utcNow;
            await _repository.AddAsync(newListing, cancellationToken);
            return;
        }

        ListingMapper.UpdateEntity(existing, listingDto);
        existing.LastFundaFetchUtc = utcNow;
        await _repository.UpdateAsync(existing, cancellationToken);
    }

    /// <summary>
    /// Generates a full context report for an existing listing and persists the results.
    /// </summary>
    /// <remarks>
    /// <para>
    /// This process involves:
    /// 1. Fetching the listing address.
    /// 2. Generating a heavy-weight <see cref="ContextReportDto"/> via the <see cref="IContextReportService"/>.
    /// 3. Mapping the report to a JSON-serializable domain model.
    /// 4. Extracting key scores (Composite, Safety, etc.) into indexed columns for performant filtering.
    /// </para>
    /// <para>
    /// This dual-storage strategy (JSON for details, Columns for queries) balances flexibility and performance.
    /// </para>
    /// </remarks>
    public async Task<double> EnrichListingAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var listing = await _repository.GetByIdAsync(id, cancellationToken);
        if (listing is null)
        {
            throw new NotFoundException(nameof(Listing), id);
        }

        // 1. Generate Report
        ContextReportRequestDto request = new(Input: listing.Address, RadiusMeters: DefaultSearchRadiusMeters);

        // We use the application DTO for the service call...
        var reportDto = await _contextReportService.BuildAsync(request, cancellationToken);

        // ...and map it to the Domain model for storage
        var contextReportModel = ListingMapper.MapToDomain(reportDto);

        // 2. Update Entity
        listing.ContextReport = contextReportModel;
        UpdateContextScores(listing, reportDto);

        // 3. Save
        await _repository.UpdateAsync(listing, cancellationToken);

        return reportDto.CompositeScore;
    }

    private static void UpdateContextScores(Listing listing, ContextReportDto reportDto)
    {
        var scores = reportDto.CategoryScores;
        listing.ContextSocialScore = scores.TryGetValue(ContextScoreCalculator.CategorySocial, out var social) ? social : null;
        listing.ContextSafetyScore = scores.TryGetValue(ContextScoreCalculator.CategorySafety, out var safety) ? safety : null;
        listing.ContextAmenitiesScore = scores.TryGetValue(ContextScoreCalculator.CategoryAmenities, out var amenities) ? amenities : null;
        listing.ContextEnvironmentScore = scores.TryGetValue(ContextScoreCalculator.CategoryEnvironment, out var env) ? env : null;

        listing.ContextCompositeScore = reportDto.CompositeScore;
    }

    private static void ValidateExternalId(string externalId)
    {
        if (string.IsNullOrWhiteSpace(externalId))
        {
            throw new ValidationException(new[] { "External ID (PDOK ID) is required." });
        }

        if (externalId.Length > ValidationConstants.Listing.FundaIdMaxLength || !externalId.All(c => char.IsLetterOrDigit(c) || c == '-'))
        {
            throw new ValidationException(new[] { "Invalid External ID format." });
        }
    }
}
