using System.ComponentModel.DataAnnotations;
using Valora.Application.Common.Exceptions;
using Valora.Application.Common.Interfaces;
using Valora.Application.Common.Mappings;
using Valora.Application.Common.Models;
using Valora.Application.DTOs;
using Valora.Application.Enrichment;
using Valora.Domain.Entities;
using ValidationException = Valora.Application.Common.Exceptions.ValidationException;

namespace Valora.Application.Services;

public class ListingService : IListingService
{
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
                .ToDictionary(g => g.Key, g => g.Select(x => x.ErrorMessage ?? "Unknown error").ToArray());

            throw new ValidationException(errors);
        }
    }

    public async Task<ListingDto?> GetListingByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var listing = await _repository.GetByIdAsync(id, cancellationToken);
        if (listing == null)
        {
             return null;
        }
        return ListingMapper.ToDto(listing);
    }

    public async Task<ListingDto?> GetPdokListingAsync(string externalId, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(externalId))
        {
             throw new ValidationException(new[] { "External ID is required" });
        }

        var listingDto = await _pdokService.GetListingDetailsAsync(externalId, cancellationToken);
        if (listingDto is null)
        {
            return null;
        }

        await CreateOrUpdateListingAsync(listingDto, cancellationToken);

        return listingDto;
    }

    private async Task CreateOrUpdateListingAsync(ListingDto listingDto, CancellationToken cancellationToken)
    {
        var existing = await _repository.GetByFundaIdAsync(listingDto.FundaId, cancellationToken);
        if (existing is null)
        {
            var newListing = ListingMapper.ToEntity(listingDto);
            newListing.LastFundaFetchUtc = DateTime.UtcNow;
            await _repository.AddAsync(newListing, cancellationToken);
        }
        else
        {
            ListingMapper.UpdateEntity(existing, listingDto);
            existing.LastFundaFetchUtc = DateTime.UtcNow;
            await _repository.UpdateAsync(existing, cancellationToken);
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
        ContextReportRequestDto request = new(Input: listing.Address); // Default 1km radius

        // We use the application DTO for the service call...
        var reportDto = await _contextReportService.BuildAsync(request, cancellationToken);

        // ...and map it to the Domain model for storage
        var contextReportModel = ListingMapper.MapToDomain(reportDto);

        // 2. Update Entity
        listing.ContextReport = contextReportModel;
        listing.ContextCompositeScore = reportDto.CompositeScore;

        UpdateContextScores(listing, reportDto);

        // 3. Save
        await _repository.UpdateAsync(listing, cancellationToken);

        return reportDto.CompositeScore;
    }

    private static void UpdateContextScores(Listing listing, ContextReportDto reportDto)
    {
        if (reportDto.CategoryScores.TryGetValue(ContextScoreCalculator.CategorySocial, out var social)) listing.ContextSocialScore = social;
        if (reportDto.CategoryScores.TryGetValue(ContextScoreCalculator.CategorySafety, out var safety)) listing.ContextSafetyScore = safety;
        if (reportDto.CategoryScores.TryGetValue(ContextScoreCalculator.CategoryAmenities, out var amenities)) listing.ContextAmenitiesScore = amenities;
        if (reportDto.CategoryScores.TryGetValue(ContextScoreCalculator.CategoryEnvironment, out var env)) listing.ContextEnvironmentScore = env;
    }
}
