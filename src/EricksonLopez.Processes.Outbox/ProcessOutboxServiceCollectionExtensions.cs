// Copyright © Erickson Lopez. MIT License.
using System;

namespace Microsoft.Extensions.DependencyInjection;

using EricksonLopez.Processes.Outbox;
using Microsoft.Extensions.DependencyInjection.Extensions;

/// <summary>
/// Provides extension methods for registering transactional outbox dispatching services with <see cref="IServiceCollection"/>.
/// </summary>
public static class ProcessOutboxServiceCollectionExtensions
{
    /// <summary>
    /// Registers the <see cref="IProcessOutboxDispatcher"/> implementation into the service collection.
    /// </summary>
    /// <param name="services">The target service collection.</param>
    /// <returns>The <paramref name="services"/> instance for method chaining.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="services"/> is <see langword="null"/></exception>
    public static IServiceCollection AddProcessesOutbox(this IServiceCollection services)
    {
        ArgumentNullException.ThrowIfNull(services);

        services.TryAddScoped<IProcessOutboxDispatcher, OutboxProcessDispatcher>();
        return services;
    }
}


