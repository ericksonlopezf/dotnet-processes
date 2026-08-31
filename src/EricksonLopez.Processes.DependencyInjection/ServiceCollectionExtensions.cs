// Copyright © Erickson Lopez. MIT License.
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using EricksonLopez.Processes;
using EricksonLopez.Processes.Abstractions;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace EricksonLopez.Processes.DependencyInjection;

/// <summary>
/// Provides extension methods for registering process framework services into an <see cref="IServiceCollection"/>.
/// </summary>
public static class ServiceCollectionExtensions
{
    /// <summary>
    /// Registers core process framework services, including the <see cref="ProcessRegistry"/>, into the service collection.
    /// </summary>
    /// <param name="services">The target service collection.</param>
    /// <returns>The <paramref name="services"/> instance for method chaining.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="services"/> is <see langword="null"/></exception>
    public static IServiceCollection AddProcesses(this IServiceCollection services)
    {
        ArgumentNullException.ThrowIfNull(services);

        services.TryAddSingleton<ProcessRegistry>();
        services.TryAddSingleton<IProcessRegistry>(sp => sp.GetRequiredService<ProcessRegistry>());
        services.TryAddSingleton(TimeProvider.System);

        return services;
    }

    /// <summary>
    /// Registers a <see cref="ProcessCoordinator{TState}"/> for the specified domain state schema.
    /// </summary>
    /// <typeparam name="TState">The strongly typed domain state type.</typeparam>
    /// <param name="services">The target service collection.</param>
    /// <param name="configureOptions">The optional action to configure coordinator options.</param>
    /// <returns>The <paramref name="services"/> instance for method chaining.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="services"/> is <see langword="null"/></exception>
    public static IServiceCollection AddProcessCoordinator<TState>(
        this IServiceCollection services,
        Action<ProcessCoordinatorOptions>? configureOptions = null)
        where TState : notnull
    {
        ArgumentNullException.ThrowIfNull(services);

        services.TryAddTransient<ProcessCoordinator<TState>>(sp =>
        {
            var store = sp.GetRequiredService<IProcessStore<TState>>();
            var timeProvider = sp.GetService<TimeProvider>() ?? TimeProvider.System;
            var options = new ProcessCoordinatorOptions();
            configureOptions?.Invoke(options);
            return new ProcessCoordinator<TState>(store, options, timeProvider);
        });

        return services;
    }
}




