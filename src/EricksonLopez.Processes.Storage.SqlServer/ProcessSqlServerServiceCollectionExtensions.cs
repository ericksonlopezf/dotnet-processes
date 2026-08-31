// Copyright © Erickson Lopez. MIT License.
using System;
using EricksonLopez.Processes.Abstractions;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Logging;

namespace EricksonLopez.Processes.Storage.SqlServer;

/// <summary>
/// Provides extension methods for registering SQL Server process store persistence with an <see cref="IServiceCollection"/>.
/// </summary>
public static class ProcessSqlServerServiceCollectionExtensions
{
    /// <summary>
    /// Registers <see cref="SqlServerProcessStore{TState}"/> as the implementation of <see cref="IProcessStore{TState}"/> in the service collection.
    /// </summary>
    /// <typeparam name="TState">The strongly typed domain state type.</typeparam>
    /// <param name="services">The target service collection.</param>
    /// <param name="connectionString">The SQL Server database connection string.</param>
    /// <param name="tableName">The table name for process records (defaults to 'ProcessInstances').</param>
    /// <returns>The <paramref name="services"/> instance for method chaining.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="services"/> is <see langword="null"/></exception>
    /// <exception cref="ArgumentException"><paramref name="connectionString"/> is <see langword="null"/> or white-space</exception>
    public static IServiceCollection AddSqlServerProcessStore<TState>(
        this IServiceCollection services,
        string connectionString,
        string tableName = "ProcessInstances")
        where TState : notnull
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentException.ThrowIfNullOrWhiteSpace(connectionString);

        services.TryAddSingleton<IProcessStore<TState>>(sp =>
        {
            var serializer = sp.GetRequiredService<IProcessStateSerializer<TState>>();
            var logger = sp.GetService<ILogger<SqlServerProcessStore<TState>>>();
            return new SqlServerProcessStore<TState>(connectionString, serializer, tableName, logger);
        });

        return services;
    }
}
