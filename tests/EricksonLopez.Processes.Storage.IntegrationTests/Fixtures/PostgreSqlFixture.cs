// Copyright © Erickson Lopez. MIT License.
using System;
using System.Threading.Tasks;
using Npgsql;
using Testcontainers.PostgreSql;
using Xunit;

namespace EricksonLopez.Processes.Storage.IntegrationTests.Fixtures;

/// <summary>
/// Manages the shared PostgreSQL Testcontainers instance lifecycle for integration testing.
/// </summary>
public sealed class PostgreSqlFixture : IAsyncLifetime
{
    private readonly PostgreSqlContainer _postgres = new PostgreSqlBuilder("postgres:16-alpine")
        .Build();

    /// <summary>
    /// Gets the connection string to the running PostgreSQL container.
    /// </summary>
    public string ConnectionString => _postgres.GetConnectionString();

    public async Task InitializeAsync()
    {
        await _postgres.StartAsync();

        // Execute initial DDL schema creation once per fixture
        await using var connection = new NpgsqlConnection(ConnectionString);
        await connection.OpenAsync();

        const string ddl = """
            CREATE TABLE IF NOT EXISTS process_instances (
                process_id UUID PRIMARY KEY,
                process_type TEXT NOT NULL,
                version INT NOT NULL,
                status INT NOT NULL,
                revision BIGINT NOT NULL,
                correlation_id TEXT NOT NULL,
                state_payload JSONB NOT NULL,
                created_at TIMESTAMPTZ NOT NULL,
                updated_at TIMESTAMPTZ NOT NULL,
                completed_at TIMESTAMPTZ NULL
            );
            CREATE INDEX IF NOT EXISTS idx_process_instances_correlation_id ON process_instances(correlation_id);
            """;

        await using var cmd = new NpgsqlCommand(ddl, connection);
        await cmd.ExecuteNonQueryAsync();
    }

    public async Task DisposeAsync()
    {
        await _postgres.DisposeAsync();
    }

    /// <summary>
    /// Restarts the container to simulate a database crash or connection drop.
    /// </summary>
    public async Task RestartAsync()
    {
        await _postgres.StopAsync();
        await _postgres.StartAsync();
    }
}
