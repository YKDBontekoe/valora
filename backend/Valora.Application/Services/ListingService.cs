using System.ComponentModel.DataAnnotations;
using Valora.Application.Common.Exceptions;
using Valora.Application.Common.Interfaces;
using Valora.Application.Common.Mappings;
using Valora.Application.Common.Models;
using Valora.Application.DTOs;
using ValidationException = Valora.Application.Common.Exceptions.ValidationException;

namespace Valora.Application.Services;

public class ListingService : IListingService
{
    private readonly IListingRepository _listingRepository;
    private readonly IContextReportService _contextReportService;

    public ListingService(
        IListingRepository listingRepository,
        IContextReportService contextReportService)
    {
        _listingRepository = listingRepository;
        _contextReportService = contextReportService;
    }

    public async Task<Result<PaginatedList<ListingSummaryDto>>> GetSummariesAsync(ListingFilterDto filter, CancellationToken ct = default)
    {
        var validationContext = new ValidationContext(filter);
        var validationResults = new List<ValidationResult>();

        if (!Validator.TryValidateObject(filter, validationContext, validationResults, true))
        {
             var errors = validationResults
                .GroupBy(e => e.MemberNames.FirstOrDefault() ?? string.Empty)
                .ToDictionary(
                    g => g.Key,
                    g => g.Select(x => x.ErrorMessage ?? string.Empty).ToArray()
                );

            throw new ValidationException(errors);
        }

        var paginatedList = await _listingRepository.GetSummariesAsync(filter, ct);
        return Result<PaginatedList<ListingSummaryDto>>.Success(paginatedList);
    }

    public async Task<Result<ListingDto>> GetByIdAsync(Guid id, CancellationToken ct = default)
    {
        var listing = await _listingRepository.GetByIdAsync(id, ct);
        if (listing is null)
        {
            return Result<ListingDto>.Failure(new[] { "Listing not found." });
        }

        var dto = ListingMapper.ToDto(listing);
        return Result<ListingDto>.Success(dto);
    }

    public async Task<Result<double>> EnrichListingAsync(Guid id, CancellationToken ct = default)
    {
        var listing = await _listingRepository.GetByIdAsync(id, ct);
        if (listing is null)
        {
             return Result<double>.Failure(new[] { "Listing not found." });
        }

        // 1. Generate Report
        // Using default radius of 1000m
        ContextReportRequestDto request = new(Input: listing.Address);

        // We use the application DTO for the service call...
        var reportDto = await _contextReportService.BuildAsync(request, ct);

        // ...and map it to the Domain model for storage
        var contextReportModel = ListingMapper.MapToDomain(reportDto);

        // 2. Update Entity
        listing.ContextReport = contextReportModel;
        listing.ContextCompositeScore = reportDto.CompositeScore;

        if (reportDto.CategoryScores.TryGetValue("Social", out var social)) listing.ContextSocialScore = social;
        if (reportDto.CategoryScores.TryGetValue("Safety", out var crime)) listing.ContextSafetyScore = crime;
        if (reportDto.CategoryScores.TryGetValue("Amenities", out var amenities)) listing.ContextAmenitiesScore = amenities;
        if (reportDto.CategoryScores.TryGetValue("Environment", out var env)) listing.ContextEnvironmentScore = env;

        // 3. Save
        await _listingRepository.UpdateAsync(listing, ct);

        return Result<double>.Success(reportDto.CompositeScore);
    }
}
