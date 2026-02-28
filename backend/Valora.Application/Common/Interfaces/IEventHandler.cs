using System.Threading;
using System.Threading.Tasks;
using Valora.Application.Common.Events;

namespace Valora.Application.Common.Interfaces;

public interface IEventHandler<in TEvent> where TEvent : IDomainEvent
{
    Task HandleAsync(TEvent domainEvent, CancellationToken cancellationToken = default);
}
