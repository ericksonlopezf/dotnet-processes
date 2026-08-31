// Copyright © Erickson Lopez. MIT License.
using System;
using EricksonLopez.Processes.Abstractions;
using EricksonLopez.Processes.Storage.Oracle;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Logging;

namespace EricksonLopez.Processes.Storage.Oracle;

/// <summary>
/// Provides extension methods for registering Oracle process store persistence with an <see cref="IServiceCollection"/>.
/// </summary>
public static class ProcessOracleServiceCollectionExtensions
{
    /// <summary>
    /// Registers <see cref="OracleProcessStore{TState}"/> as the implementation of <see cref="IProcessStore{TState}"/> in the service collection.
    /// </summary>
    /// <typeparam name="TState">The strongly typed domain state type.</typeparam>
    /// <param name="services">The target service collection.</param>
    /// <param name="connectionString">The Oracle database connection string.</param>
    /// <param name="tableName">The table name for process records (defaults to 'PROCESS_INSTANCES').</param>
    /// <returns>The <paramref name="services"/> instance for method chaining.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="services"/> is <see langword="null"/></exception>
    /// <exception cref="ArgumentException"><paramref name="connectionString"/> is <see langword="null"/> or white-space</exception>
    public static IServiceCollection AddOracleProcessStore<TState>(
        this IServiceCollection services,
        string connectionString,
        string tableName = "PROCESS_INSTANCES")
        where TState : notnull
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentException.ThrowIfNullOrWhiteSpace(connectionString);

        services.TryAddSingleton<IProcessStore<TState>>(sp =>
        {
            var serializer = sp.GetRequiredService<IProcessStateSerializer<TState>>();
            var logger = sp.GetService<ILogger<OracleProcessStore<TState>>>();
            return new OracleProcessStore<TState>(connectionString, serializer, tableName, logger);
        });

        return services;
    }
}
