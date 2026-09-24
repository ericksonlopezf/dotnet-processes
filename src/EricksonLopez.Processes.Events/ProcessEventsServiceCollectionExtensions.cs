// Copyright © Erickson Lopez. MIT License.
using System;
using Microsoft.Extensions.DependencyInjection;

namespace EricksonLopez.Processes.Events;

/// <summary>
/// Provides extension methods for registering event process dispatcher services with dependency injection.
/// </summary>
public static class ProcessEventsServiceCollectionExtensions
{
    /// <summary>
    /// Registers the <see cref="EventProcessDispatcher"/> implementation of <see cref="IEventProcessDispatcher"/> into the service collection.
    /// </summary>
    /// <param name="services">The target service collection.</param>
    /// <returns>The <paramref name="services"/> instance for method chaining.</returns>
    public static IServiceCollection AddProcessEventsDispatcher(this IServiceCollection services)
    {
        return services.AddSingleton<IEventProcessDispatcher, EventProcessDispatcher>();
    }
}
