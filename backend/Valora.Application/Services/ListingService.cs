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

    public async Task<ListingDto?> GetPdokListingAsync(string externalId, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(externalId))
        {
             throw new ValidationException(new[] { "External ID (PDOK ID) is required." });
        }

        if (externalId.Length > ValidationConstants.Listing.FundaIdMaxLength || !externalId.All(c => char.IsLetterOrDigit(c) || c == '-'))
        {
             throw new ValidationException(new[] { "Invalid External ID format." });
        }

        var listingDto = await _pdokService.GetListingDetailsAsync(externalId, cancellationToken);
        if (listingDto is null)
        {
            return null;
        }

        var listing = await CreateOrUpdateListingAsync(listingDto, cancellationToken);

        // Auto-enrich if context report is missing to ensure the "Market Analysis" focus is immediate
        if (listing.ContextReport == null)
        {
            try
            {
                await EnrichListingAsync(listing.Id, cancellationToken);
                // Refresh listing info after enrichment to get the scores
                var updated = await _repository.GetByIdAsync(listing.Id, cancellationToken);
                if (updated != null) return ListingMapper.ToDto(updated);
            }
            catch (Exception)
            {
                // Fallback to basic details if enrichment fails
                return ListingMapper.ToDto(listing);
            }
        }

        return ListingMapper.ToDto(listing);
    }

    private async Task<Listing> CreateOrUpdateListingAsync(ListingDto listingDto, CancellationToken cancellationToken)
    {
        var existing = await _repository.GetByFundaIdAsync(listingDto.FundaId, cancellationToken);
        var utcNow = DateTime.UtcNow;

        if (existing is null)
        {
            var newListing = ListingMapper.ToEntity(listingDto);
            newListing.LastFundaFetchUtc = utcNow;
            return await _repository.AddAsync(newListing, cancellationToken);
        }
        else
        {
            ListingMapper.UpdateEntity(existing, listingDto);
            existing.LastFundaFetchUtc = utcNow;
            await _repository.UpdateAsync(existing, cancellationToken);
            return existing;
        }
    }

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
        listing.ContextCompositeScore = reportDto.CompositeScore;

        // Update WOZ if found in context report and not already set
        var (woz, wozDate, wozSource) = reportDto.EstimateWozValue(TimeProvider.System);
        if (woz.HasValue && !listing.WozValue.HasValue)
        {
            listing.WozValue = woz;
            listing.WozReferenceDate = wozDate;
            listing.WozValueSource = wozSource;
        }

        UpdateContextScores(listing, reportDto);

        // 3. Save
        await _repository.UpdateAsync(listing, cancellationToken);

        return reportDto.CompositeScore;
    }

    private static void UpdateContextScores(Listing listing, ContextReportDto reportDto)
    {
        var scores = reportDto.CategoryScores;
        if (scores.TryGetValue(ContextScoreCalculator.CategorySocial, out var social)) listing.ContextSocialScore = social;
        if (scores.TryGetValue(ContextScoreCalculator.CategorySafety, out var safety)) listing.ContextSafetyScore = safety;
        if (scores.TryGetValue(ContextScoreCalculator.CategoryAmenities, out var amenities)) listing.ContextAmenitiesScore = amenities;
        if (scores.TryGetValue(ContextScoreCalculator.CategoryEnvironment, out var env)) listing.ContextEnvironmentScore = env;
    }
}
