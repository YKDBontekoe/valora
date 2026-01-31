namespace Valora.Application.Common.Interfaces;

public interface ITestSeedService
{
    Task SeedAsync(CancellationToken cancellationToken);
}
