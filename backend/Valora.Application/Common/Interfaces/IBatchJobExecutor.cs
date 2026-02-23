using System.Threading;
using System.Threading.Tasks;

namespace Valora.Application.Common.Interfaces;

public interface IBatchJobExecutor
{
    /// <summary>
    /// Picks the next pending job from the queue and executes it.
    /// </summary>
    Task ProcessNextJobAsync(CancellationToken cancellationToken = default);
}
