// Copyright © Erickson Lopez. MIT License.
using System;

namespace Microsoft.Extensions.DependencyInjection;

using EricksonLopez.Processes.Mediator;
using Microsoft.Extensions.DependencyInjection.Extensions;

/// <summary>
/// Provides extension methods for registering mediator process dispatcher services with <see cref="IServiceCollection"/>.
/// </summary>
public static class ProcessMediatorServiceCollectionExtensions
{
    /// <summary>
    /// Registers the <see cref="IMediatorProcessDispatcher"/> implementation into the service collection.
    /// </summary>
    /// <param name="services">The target service collection.</param>
    /// <returns>The <paramref name="services"/> instance for method chaining.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="services"/> is <see langword="null"/></exception>
    public static IServiceCollection AddProcessesMediator(this IServiceCollection services)
    {
        ArgumentNullException.ThrowIfNull(services);

        services.TryAddScoped<IMediatorProcessDispatcher, MediatorProcessDispatcher>();
        return services;
    }
}


