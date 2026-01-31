using Valora.Application.Common.Interfaces;
using Valora.Domain.Entities;
using Valora.Infrastructure.Persistence;

namespace Valora.Infrastructure.Services;

public class TestSeedService : ITestSeedService
{
    private readonly ValoraDbContext _context;

    public TestSeedService(ValoraDbContext context)
    {
        _context = context;
    }

    public async Task SeedAsync(CancellationToken cancellationToken)
    {
        // Clear existing data
        _context.Listings.RemoveRange(_context.Listings);
        await _context.SaveChangesAsync(cancellationToken);

        // Seed deterministic data
        var listings = new List<Listing>
        {
            new Listing
            {
                Id = Guid.NewGuid(),
                FundaId = "test-1",
                Address = "Teststraat 1",
                City = "Amsterdam",
                PostalCode = "1000 AA",
                Price = 500000,
                Bedrooms = 3,
                Bathrooms = 1,
                LivingAreaM2 = 100,
                PlotAreaM2 = 0,
                PropertyType = "Apartment",
                Status = "Available",
                Url = "http://example.com/1",
                ImageUrl = "https://picsum.photos/id/10/800/600",
                ListedDate = DateTime.UtcNow.AddDays(-1),
                CreatedAt = DateTime.UtcNow
            },
            new Listing
            {
                Id = Guid.NewGuid(),
                FundaId = "test-2",
                Address = "Kerkstraat 10",
                City = "Utrecht",
                PostalCode = "3500 BB",
                Price = 350000,
                Bedrooms = 2,
                Bathrooms = 1,
                LivingAreaM2 = 85,
                PlotAreaM2 = 0,
                PropertyType = "Apartment",
                Status = "Sold",
                Url = "http://example.com/2",
                ImageUrl = "https://picsum.photos/id/20/800/600",
                ListedDate = DateTime.UtcNow.AddDays(-5),
                CreatedAt = DateTime.UtcNow
            },
            new Listing
            {
                Id = Guid.NewGuid(),
                FundaId = "test-3",
                Address = "Herenweg 42",
                City = "Haarlem",
                PostalCode = "2000 CC",
                Price = 750000,
                Bedrooms = 4,
                Bathrooms = 2,
                LivingAreaM2 = 150,
                PlotAreaM2 = 200,
                PropertyType = "House",
                Status = "Available",
                Url = "http://example.com/3",
                ImageUrl = "https://picsum.photos/id/30/800/600",
                ListedDate = DateTime.UtcNow.AddDays(-10),
                CreatedAt = DateTime.UtcNow
            },
             new Listing
            {
                Id = Guid.NewGuid(),
                FundaId = "test-4",
                Address = "Zeeweg 99",
                City = "Zandvoort",
                PostalCode = "2042 DD",
                Price = 450000,
                Bedrooms = 2,
                Bathrooms = 1,
                LivingAreaM2 = 90,
                PlotAreaM2 = 0,
                PropertyType = "Apartment",
                Status = "Available",
                Url = "http://example.com/4",
                ImageUrl = "https://picsum.photos/id/40/800/600",
                ListedDate = DateTime.UtcNow.AddDays(-2),
                CreatedAt = DateTime.UtcNow
            },
             new Listing
            {
                Id = Guid.NewGuid(),
                FundaId = "test-5",
                Address = "Dorpsstraat 5",
                City = "Amstelveen",
                PostalCode = "1181 EE",
                Price = 1200000,
                Bedrooms = 5,
                Bathrooms = 3,
                LivingAreaM2 = 250,
                PlotAreaM2 = 500,
                PropertyType = "Villa",
                Status = "Available",
                Url = "http://example.com/5",
                ImageUrl = "https://picsum.photos/id/50/800/600",
                ListedDate = DateTime.UtcNow.AddDays(-20),
                CreatedAt = DateTime.UtcNow
            }
        };

        _context.Listings.AddRange(listings);
        await _context.SaveChangesAsync(cancellationToken);
    }
}
