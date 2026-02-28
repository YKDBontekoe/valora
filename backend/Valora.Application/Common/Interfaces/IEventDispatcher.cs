using System.Threading;
using System.Threading.Tasks;
using Valora.Application.Common.Events;

namespace Valora.Application.Common.Interfaces;

public interface IEventDispatcher
{
    Task DispatchAsync<TEvent>(TEvent domainEvent, CancellationToken cancellationToken = default) where TEvent : IDomainEvent;
}
