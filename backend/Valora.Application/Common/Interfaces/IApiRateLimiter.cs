namespace Valora.Application.Common.Interfaces;

public interface IApiRateLimiter
{
    Task WaitAsync(CancellationToken cancellationToken = default);
}
