using Moq;
using Valora.Application.Common.Exceptions;
using Valora.Application.Common.Interfaces;
using Valora.Application.DTOs;
using Valora.Application.Services;
using Valora.Domain.Entities;

namespace Valora.UnitTests.Services;

public class ListingServiceTests
{
    private readonly Mock<IListingRepository> _repositoryMock = new();
    private readonly Mock<IPdokListingService> _pdokServiceMock = new();
    private readonly Mock<IContextReportService> _contextReportServiceMock = new();

    private ListingService CreateService()
    {
        return new ListingService(
            _repositoryMock.Object,
            _pdokServiceMock.Object,
            _contextReportServiceMock.Object);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public async Task GetPdokListingAsync_ThrowsValidationException_WhenIdIsNullOrWhiteSpace(string invalidId)
    {
        // Arrange
        var service = CreateService();

        // Act & Assert
        await Assert.ThrowsAsync<ValidationException>(() => service.GetPdokListingAsync(invalidId));
    }

    [Theory]
    [InlineData("too-long-id-that-exceeds-the-maximum-length-of-fifty-characters-12345")]
    [InlineData("invalid_char!")]
    [InlineData("invalid@char")]
    public async Task GetPdokListingAsync_ThrowsValidationException_WhenIdIsInvalidFormat(string invalidId)
    {
        // Arrange
        var service = CreateService();

        // Act & Assert
        await Assert.ThrowsAsync<ValidationException>(() => service.GetPdokListingAsync(invalidId));
    }

    [Fact]
    public async Task GetPdokListingAsync_AddsListing_WhenNotStoredYet()
    {
        // Arrange
        const string pdokId = "adr-123";
        var listingDto = CreatePdokListingDto(pdokId, "Damrak 1, Amsterdam");
        var service = CreateService();

        _pdokServiceMock
            .Setup(x => x.GetListingDetailsAsync(pdokId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(listingDto);
        _repositoryMock
            .Setup(x => x.GetByFundaIdAsync(pdokId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((Listing?)null);

        // Act
        var result = await service.GetPdokListingAsync(pdokId);

        // Assert
        Assert.NotNull(result);
        _repositoryMock.Verify(
            x => x.AddAsync(It.Is<Listing>(l =>
                l.FundaId == pdokId &&
                l.Address == "Damrak 1, Amsterdam" &&
                l.City == "Amsterdam" &&
                l.ContextCompositeScore == 78.5 &&
                l.ContextSafetyScore == 81.2),
            It.IsAny<CancellationToken>()),
            Times.Once);
        _repositoryMock.Verify(x => x.UpdateAsync(It.IsAny<Listing>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task GetListingByIdAsync_CallsGetByIdAsNoTrackingAsync()
    {
        // Arrange
        var id = Guid.NewGuid();
        var listing = new Listing { Id = id, Address = "Test Address", FundaId = "12345" };
        var service = CreateService();

        _repositoryMock
            .Setup(x => x.GetByIdAsNoTrackingAsync(id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(listing);

        // Act
        var result = await service.GetListingByIdAsync(id);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(listing.Address, result.Address);
        _repositoryMock.Verify(x => x.GetByIdAsNoTrackingAsync(id, It.IsAny<CancellationToken>()), Times.Once);
        _repositoryMock.Verify(x => x.GetByIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task GetPdokListingAsync_UpdatesListing_WhenAlreadyStored()
    {
        // Arrange
        const string pdokId = "adr-123";
        var existing = new Listing
        {
            FundaId = pdokId,
            Address = "Old Address 5",
            City = "Rotterdam",
            Status = "Unknown"
        };

        var listingDto = CreatePdokListingDto(pdokId, "Damrak 1, Amsterdam");
        var service = CreateService();

        _pdokServiceMock
            .Setup(x => x.GetListingDetailsAsync(pdokId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(listingDto);
        _repositoryMock
            .Setup(x => x.GetByFundaIdAsync(pdokId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(existing);

        // Act
        await service.GetPdokListingAsync(pdokId);

        // Assert
        _repositoryMock.Verify(
            x => x.UpdateAsync(It.Is<Listing>(l =>
                l == existing &&
                l.Address == "Damrak 1, Amsterdam" &&
                l.City == "Amsterdam" &&
                l.ContextCompositeScore == 78.5 &&
                l.ContextSafetyScore == 81.2),
            It.IsAny<CancellationToken>()),
            Times.Once);
        _repositoryMock.Verify(x => x.AddAsync(It.IsAny<Listing>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    private static ListingDto CreatePdokListingDto(string pdokId, string address)
    {
        return new ListingDto(
            Id: Guid.NewGuid(),
            FundaId: pdokId,
            Address: address,
            City: "Amsterdam",
            PostalCode: "1012LG",
            Price: null,
            Bedrooms: null,
            Bathrooms: null,
            LivingAreaM2: 88,
            PlotAreaM2: null,
            PropertyType: "woonfunctie",
            Status: "Unknown",
            Url: null,
            ImageUrl: null,
            ListedDate: DateTime.UtcNow,
            CreatedAt: DateTime.UtcNow,
            Description: "Built in 1990.",
            EnergyLabel: null,
            YearBuilt: 1990,
            ImageUrls: new List<string>(),
            OwnershipType: null,
            CadastralDesignation: null,
            VVEContribution: null,
            HeatingType: null,
            InsulationType: null,
            GardenOrientation: null,
            HasGarage: false,
            ParkingType: null,
            AgentName: null,
            VolumeM3: null,
            BalconyM2: null,
            GardenM2: null,
            ExternalStorageM2: null,
            Features: new Dictionary<string, string>(),
            Latitude: 52.37,
            Longitude: 4.89,
            VideoUrl: null,
            VirtualTourUrl: null,
            FloorPlanUrls: new List<string>(),
            BrochureUrl: null,
            RoofType: null,
            NumberOfFloors: null,
            ConstructionPeriod: null,
            CVBoilerBrand: null,
            CVBoilerYear: null,
            BrokerPhone: null,
            BrokerLogoUrl: null,
            FiberAvailable: null,
            PublicationDate: null,
            IsSoldOrRented: false,
            Labels: new List<string>(),
            WozValue: null,
            WozReferenceDate: null,
            WozValueSource: null,
            ContextCompositeScore: 78.5,
            ContextSafetyScore: 81.2,
            ContextReport: null);
    }
}
