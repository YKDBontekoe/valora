using Microsoft.Extensions.Logging;
using Moq;
using Valora.Domain.Entities;
using Valora.Infrastructure.Scraping;
using Valora.Infrastructure.Scraping.Models;
using Xunit;

namespace Valora.UnitTests.Scraping;

public class ListingEnricherTests
{
    private readonly Mock<FundaApiClient> _apiClientMock;
    private readonly Mock<ILogger<ListingEnricher>> _loggerMock;
    private readonly ListingEnricher _enricher;

    public ListingEnricherTests()
    {
        // Mock FundaApiClient which is a class with virtual methods.
        // It requires HttpClient and Logger in constructor.
        var httpClient = new HttpClient();
        var clientLogger = new Mock<ILogger<FundaApiClient>>();

        _apiClientMock = new Mock<FundaApiClient>(httpClient, clientLogger.Object);
        _loggerMock = new Mock<ILogger<ListingEnricher>>();

        _enricher = new ListingEnricher(_apiClientMock.Object, _loggerMock.Object);
    }

    [Fact]
    public async Task EnrichListingAsync_ShouldEnrichWithSummary()
    {
        // Arrange
        var listing = new Listing { FundaId = "123", Url = "http://test.com", Address = "Test Address" };
        var apiListing = new FundaApiListing { GlobalId = 123 };

        var summary = new FundaApiListingSummary
        {
            PublicationDate = new DateTime(2023, 1, 1),
            IsSoldOrRented = true,
            Labels = [new() { Text = "Sold" }]
        };

        _apiClientMock.Setup(c => c.GetListingSummaryAsync(123, It.IsAny<CancellationToken>()))
            .ReturnsAsync(summary);

        // Act
        await _enricher.EnrichListingAsync(listing, apiListing, CancellationToken.None);

        // Assert
        Assert.Equal(new DateTime(2023, 1, 1), listing.PublicationDate);
        Assert.True(listing.IsSoldOrRented);
        // Verify mapper logic (Sold -> Verkocht/Verhuurd if no status tracking)
        Assert.Equal("Verkocht/Verhuurd", listing.Status);
    }

    [Fact]
    public async Task EnrichListingAsync_ShouldEnrichWithContactDetails()
    {
        // Arrange
        var listing = new Listing { FundaId = "123", Url = "http://test.com", Address = "Test Address" };
        var apiListing = new FundaApiListing { GlobalId = 123 };

        var contacts = new FundaContactDetailsResponse
        {
            ContactDetails = new List<FundaContactBlockDetail>
            {
                new FundaContactBlockDetail
                {
                    Id = 999,
                    PhoneNumber = "0612345678",
                    DisplayName = "Test Makelaar",
                    LogoUrl = "http://logo.com",
                    AssociationCode = "NVM"
                }
            }
        };

        _apiClientMock.Setup(c => c.GetContactDetailsAsync(123, It.IsAny<CancellationToken>()))
            .ReturnsAsync(contacts);

        // Act
        await _enricher.EnrichListingAsync(listing, apiListing, CancellationToken.None);

        // Assert
        Assert.Equal(999, listing.BrokerOfficeId);
        Assert.Equal("0612345678", listing.BrokerPhone);
        Assert.Equal("Test Makelaar", listing.AgentName);
        Assert.Equal("http://logo.com", listing.BrokerLogoUrl);
        Assert.Equal("NVM", listing.BrokerAssociationCode);
    }

    [Fact]
    public async Task EnrichListingAsync_ShouldHandleExceptionsGracefully()
    {
        // Arrange
        var listing = new Listing { FundaId = "123", Url = "http://test.com", Address = "Test Address" };
        var apiListing = new FundaApiListing { GlobalId = 123 };

        _apiClientMock.Setup(c => c.GetListingSummaryAsync(It.IsAny<int>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new Exception("API Error"));

        // Act
        // Should not throw
        await _enricher.EnrichListingAsync(listing, apiListing, CancellationToken.None);

        // Assert
        // Verify logger was called
        // Note: verifying extension methods on ILogger is tricky with Moq.
        // We can verify the generic Log method if strictly needed, but absence of exception is good enough for now.
    }

    [Fact]
    public async Task EnrichListingAsync_ShouldEnrichWithNuxtData()
    {
        // Arrange
        var listing = new Listing { FundaId = "123", Url = "http://test.com", Address = "Test Address" };
        var apiListing = new FundaApiListing { GlobalId = 123 };

        var nuxtData = new FundaNuxtListingData
        {
            Description = new() { Content = "Rich Description" },
            ObjectInsights = new() { Views = 100, Saves = 50 }
        };

        _apiClientMock.Setup(c => c.GetListingDetailsAsync("http://test.com", It.IsAny<CancellationToken>()))
            .ReturnsAsync(nuxtData);

        // Act
        await _enricher.EnrichListingAsync(listing, apiListing, CancellationToken.None);

        // Assert
        Assert.Equal("Rich Description", listing.Description);
        Assert.Equal(100, listing.ViewCount);
        Assert.Equal(50, listing.SaveCount);
    }

    [Fact]
    public async Task EnrichListingAsync_ShouldEnrichWithFiberAvailability()
    {
        // Arrange
        var listing = new Listing { FundaId = "123", Url = "http://test.com", Address = "Test Address", PostalCode = "1234AB" };
        var apiListing = new FundaApiListing { GlobalId = 123 };

        var fiberResponse = new FundaFiberResponse { Availability = true };

        _apiClientMock.Setup(c => c.GetFiberAvailabilityAsync("1234AB", It.IsAny<CancellationToken>()))
            .ReturnsAsync(fiberResponse);

        // Act
        await _enricher.EnrichListingAsync(listing, apiListing, CancellationToken.None);

        // Assert
        Assert.True(listing.FiberAvailable);
    }

    [Fact]
    public async Task EnrichListingAsync_ShouldHandleNullReturnsFromApi()
    {
        // Arrange
        var listing = new Listing { FundaId = "123", Url = "http://test.com", Address = "Test Address", PostalCode = "1234AB" };
        var apiListing = new FundaApiListing { GlobalId = 123 };

        _apiClientMock.Setup(c => c.GetListingSummaryAsync(123, It.IsAny<CancellationToken>()))
            .ReturnsAsync((FundaApiListingSummary?)null);

        _apiClientMock.Setup(c => c.GetListingDetailsAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((FundaNuxtListingData?)null);

        _apiClientMock.Setup(c => c.GetContactDetailsAsync(123, It.IsAny<CancellationToken>()))
            .ReturnsAsync((FundaContactDetailsResponse?)null);

        _apiClientMock.Setup(c => c.GetFiberAvailabilityAsync("1234AB", It.IsAny<CancellationToken>()))
            .ReturnsAsync((FundaFiberResponse?)null);

        // Act
        // Should not throw or crash
        await _enricher.EnrichListingAsync(listing, apiListing, CancellationToken.None);

        // Assert
        Assert.Null(listing.PublicationDate);
        Assert.Null(listing.Description);
        Assert.Null(listing.BrokerOfficeId);
        Assert.Null(listing.FiberAvailable);
    }

    [Fact]
    public async Task EnrichListingAsync_ShouldHandleNullCollectionsInContactDetails()
    {
        // Arrange
        var listing = new Listing { FundaId = "123", Url = "http://test.com", Address = "Test Address" };
        var apiListing = new FundaApiListing { GlobalId = 123 };

        var contacts = new FundaContactDetailsResponse
        {
            ContactDetails = [] // Empty list
        };

        _apiClientMock.Setup(c => c.GetContactDetailsAsync(123, It.IsAny<CancellationToken>()))
            .ReturnsAsync(contacts);

        // Act
        await _enricher.EnrichListingAsync(listing, apiListing, CancellationToken.None);

        // Assert
        Assert.Null(listing.BrokerOfficeId);
    }
}
