// Copyright © Erickson Lopez. MIT License.
using System;
using EricksonLopez.Processes.Abstractions;
using EricksonLopez.Processes.Storage.MariaDb;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Logging;

namespace EricksonLopez.Processes.Storage.MariaDb;

/// <summary>
/// Provides extension methods for registering MariaDB process store persistence with an <see cref="IServiceCollection"/>.
/// </summary>
public static class ProcessMariaDbServiceCollectionExtensions
{
    /// <summary>
    /// Registers <see cref="MariaDbProcessStore{TState}"/> as the implementation of <see cref="IProcessStore{TState}"/> in the service collection.
    /// </summary>
    /// <typeparam name="TState">The strongly typed domain state type.</typeparam>
    /// <param name="services">The target service collection.</param>
    /// <param name="connectionString">The MariaDB database connection string.</param>
    /// <param name="tableName">The table name for process records (defaults to 'process_instances').</param>
    /// <returns>The <paramref name="services"/> instance for method chaining.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="services"/> is <see langword="null"/></exception>
    /// <exception cref="ArgumentException"><paramref name="connectionString"/> is <see langword="null"/> or white-space</exception>
    public static IServiceCollection AddMariaDbProcessStore<TState>(
        this IServiceCollection services,
        string connectionString,
        string tableName = "process_instances")
        where TState : notnull
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentException.ThrowIfNullOrWhiteSpace(connectionString);

        services.TryAddSingleton<IProcessStore<TState>>(sp =>
        {
            var serializer = sp.GetRequiredService<IProcessStateSerializer<TState>>();
            var logger = sp.GetService<ILogger<MariaDbProcessStore<TState>>>();
            return new MariaDbProcessStore<TState>(connectionString, serializer, tableName, logger);
        });

        return services;
    }
}
