using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Valora.Application.Common.Events;
using Valora.Application.Common.Interfaces;

namespace Valora.Infrastructure.Services;

public class EventDispatcher : IEventDispatcher
{
    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<EventDispatcher> _logger;

    public EventDispatcher(IServiceProvider serviceProvider, ILogger<EventDispatcher> logger)
    {
        _serviceProvider = serviceProvider;
        _logger = logger;
    }

    public async Task DispatchAsync<TEvent>(TEvent domainEvent, CancellationToken cancellationToken = default) where TEvent : IDomainEvent
    {
        _logger.LogInformation("Dispatching event {EventType}", typeof(TEvent).Name);
        var handlers = _serviceProvider.GetServices<IEventHandler<TEvent>>();
        foreach (var handler in handlers)
        {
            try
            {
                await handler.HandleAsync(domainEvent, cancellationToken);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error handling event {EventType} with handler {HandlerType}", typeof(TEvent).Name, handler.GetType().Name);
                // Depending on requirements, we could rethrow or continue
            }
        }
    }
}
