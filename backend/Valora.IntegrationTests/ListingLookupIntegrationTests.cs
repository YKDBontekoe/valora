using System.Net;
using System.Net.Http.Json;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Moq;
using Valora.Application.Common.Interfaces;
using Valora.Application.DTOs;
using Valora.Domain.Entities;
using Valora.Infrastructure.Persistence;
using Xunit;

namespace Valora.IntegrationTests;

[Collection("TestcontainersDatabase")]
public class ListingLookupIntegrationTests : IAsyncLifetime
{
    private readonly TestcontainersDatabaseFixture _fixture;
    private readonly Mock<IPdokListingService> _mockPdokService = new();

    private HttpClient _client = null!;
    private LookupTestWebAppFactory _factory = null!;

    public ListingLookupIntegrationTests(TestcontainersDatabaseFixture fixture)
    {
        _fixture = fixture;
    }

    public async Task InitializeAsync()
    {
        _factory = new LookupTestWebAppFactory(_fixture.ConnectionString, this);
        _client = _factory.CreateClient();

        // Ensure database is clean before each test
        using var scope = _factory.Services.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<ValoraDbContext>();

        context.Listings.RemoveRange(context.Listings);
        context.Notifications.RemoveRange(context.Notifications);
        context.RefreshTokens.RemoveRange(context.RefreshTokens);

        if (context.Users.Any())
        {
            context.Users.RemoveRange(context.Users);
        }
        await context.SaveChangesAsync();

        await AuthenticateAsync();
    }

    public async Task DisposeAsync()
    {
        await _factory.DisposeAsync();
    }

    private async Task AuthenticateAsync()
    {
        var password = "Password123!";
        var email = "lookup@test.local";

        var registerResponse = await _client.PostAsJsonAsync("/api/auth/register", new
        {
            Email = email,
            Password = password,
            ConfirmPassword = password
        });

        // We allow register to fail (e.g. user already exists if cleanup failed)
        // but login must succeed.

        var loginResponse = await _client.PostAsJsonAsync("/api/auth/login", new
        {
            Email = email,
            Password = password
        });

        loginResponse.EnsureSuccessStatusCode();

        var auth = await loginResponse.Content.ReadFromJsonAsync<AuthResponseDto>();
        Assert.NotNull(auth);
        Assert.False(string.IsNullOrWhiteSpace(auth.Token));

        _client.DefaultRequestHeaders.Authorization =
            new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", auth.Token);
    }

    private class LookupTestWebAppFactory : IntegrationTestWebAppFactory
    {
        private readonly ListingLookupIntegrationTests _testInstance;

        public LookupTestWebAppFactory(string connectionString, ListingLookupIntegrationTests testInstance)
            : base(connectionString)
        {
            _testInstance = testInstance;
        }

        protected override void ConfigureWebHost(IWebHostBuilder builder)
        {
            base.ConfigureWebHost(builder);
            builder.ConfigureTestServices(services =>
            {
                // Register mock for PdokListingService
                services.RemoveAll<IPdokListingService>();
                services.AddSingleton(_testInstance._mockPdokService.Object);
            });
        }
    }

    [Fact]
    public async Task Lookup_NewListing_FetchesFromPdok_AndSavesToDb()
    {
        // Arrange
        var fundaId = "funda-123";
        var listingDto = CreateListingDto(fundaId, "New Street 1", 500000, "Amsterdam") with
        {
            Bedrooms = 2,
            Bathrooms = 1,
            LivingAreaM2 = 80,
            Url = "https://funda.nl/123",
            ImageUrl = "https://funda.nl/img.jpg",
            ImageUrls = new List<string> { "https://funda.nl/img.jpg" }
        };

        _mockPdokService
            .Setup(x => x.GetListingDetailsAsync(fundaId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(listingDto);

        // Act
        var response = await _client.GetAsync($"/api/listings/lookup?id={fundaId}");

        // Assert
        response.EnsureSuccessStatusCode();
        var returnedListing = await response.Content.ReadFromJsonAsync<ListingDto>();
        Assert.NotNull(returnedListing);
        Assert.Equal(fundaId, returnedListing.FundaId);
        Assert.Equal("New Street 1", returnedListing.Address);

        // Verify DB Persistence
        using var scope = _factory.Services.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<ValoraDbContext>();
        var savedListing = dbContext.Listings.FirstOrDefault(l => l.FundaId == fundaId);
        Assert.NotNull(savedListing);
        Assert.Equal("Amsterdam", savedListing.City);
    }

    [Fact]
    public async Task Lookup_ExistingListing_UpdatesFromPdok_AndSavesToDb()
    {
        // Arrange
        var fundaId = "funda-456";

        // Seed initial listing
        using (var seedScope = _factory.Services.CreateScope())
        {
            var seedContext = seedScope.ServiceProvider.GetRequiredService<ValoraDbContext>();
            seedContext.Listings.Add(new Listing
            {
                FundaId = fundaId,
                Address = "Old Street 1",
                City = "Utrecht",
                Price = 400000,
                ListedDate = DateTime.UtcNow.AddDays(-10)
            });
            await seedContext.SaveChangesAsync();
        }

        // Prepare update from PDOK (Price change)
        var updatedListingDto = CreateListingDto(fundaId, "Old Street 1", 450000, "Utrecht") with
        {
            Bedrooms = 3,
            Bathrooms = 1,
            LivingAreaM2 = 100,
            Url = "https://funda.nl/456",
            ImageUrl = "https://funda.nl/img456.jpg",
            ImageUrls = new List<string> { "https://funda.nl/img456.jpg" }
        };

        _mockPdokService
            .Setup(x => x.GetListingDetailsAsync(fundaId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(updatedListingDto);

        // Act
        var response = await _client.GetAsync($"/api/listings/lookup?id={fundaId}");

        // Assert
        response.EnsureSuccessStatusCode();
        var returnedListing = await response.Content.ReadFromJsonAsync<ListingDto>();
        Assert.NotNull(returnedListing);
        Assert.Equal(450000, returnedListing.Price);

        // Verify DB Update
        using var scope = _factory.Services.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<ValoraDbContext>();

        // Refresh context to ensure we get latest data
        var updatedListing = dbContext.Listings.FirstOrDefault(l => l.FundaId == fundaId);
        Assert.NotNull(updatedListing);
        Assert.Equal(450000, updatedListing.Price);
    }

    [Fact]
    public async Task Lookup_NotFoundInPdok_Returns404()
    {
        // Arrange
        var fundaId = "non-existent";

        _mockPdokService
            .Setup(x => x.GetListingDetailsAsync(fundaId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((ListingDto?)null);

        // Act
        var response = await _client.GetAsync($"/api/listings/lookup?id={fundaId}");

        // Assert
        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    private static ListingDto CreateListingDto(string fundaId, string address, decimal price, string city)
    {
        return new ListingDto(
            Id: Guid.NewGuid(),
            FundaId: fundaId,
            Address: address,
            City: city,
            PostalCode: null,
            Price: price,
            Bedrooms: null,
            Bathrooms: null,
            LivingAreaM2: null,
            PlotAreaM2: null,
            PropertyType: null,
            Status: null,
            Url: null,
            ImageUrl: null,
            ListedDate: DateTime.UtcNow,
            CreatedAt: DateTime.UtcNow,
            Description: null,
            EnergyLabel: null,
            YearBuilt: null,
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
            Latitude: null,
            Longitude: null,
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
            ContextCompositeScore: null,
            ContextSafetyScore: null,
            ContextReport: null
        );
    }
}
