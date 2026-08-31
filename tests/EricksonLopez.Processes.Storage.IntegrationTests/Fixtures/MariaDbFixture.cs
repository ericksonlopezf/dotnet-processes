// Copyright © Erickson Lopez. MIT License.
using System;
using System.Threading.Tasks;
using MySqlConnector;
using Testcontainers.MariaDb;
using Xunit;

namespace EricksonLopez.Processes.Storage.IntegrationTests.Fixtures;

/// <summary>
/// Manages the shared MariaDB Testcontainers instance lifecycle for integration testing.
/// </summary>
public sealed class MariaDbFixture : IAsyncLifetime
{
    private readonly MariaDbContainer _mariadb = new MariaDbBuilder("mariadb:11.4")
        .Build();

    /// <summary>
    /// Gets the connection string to the running MariaDB container.
    /// </summary>
    public string ConnectionString => _mariadb.GetConnectionString();

    public async Task InitializeAsync()
    {
        await _mariadb.StartAsync();

        await using var connection = new MySqlConnection(ConnectionString);
        await connection.OpenAsync();

        const string ddl = """
            CREATE TABLE IF NOT EXISTS process_instances (
                process_id VARCHAR(36) PRIMARY KEY,
                process_type VARCHAR(128) NOT NULL,
                version INT NOT NULL,
                status INT NOT NULL,
                revision BIGINT NOT NULL,
                correlation_id VARCHAR(128) NOT NULL,
                state_payload LONGTEXT NOT NULL,
                created_at VARCHAR(35) NOT NULL,
                updated_at VARCHAR(35) NOT NULL,
                completed_at VARCHAR(35) NULL,
                INDEX idx_process_instances_correlation_id (correlation_id)
            );
            """;

        await using var cmd = new MySqlCommand(ddl, connection);
        await cmd.ExecuteNonQueryAsync();
    }

    public async Task DisposeAsync()
    {
        await _mariadb.DisposeAsync();
    }

    /// <summary>
    /// Restarts the container to simulate a database crash or connection drop.
    /// </summary>
    public async Task RestartAsync()
    {
        await _mariadb.StopAsync();
        await _mariadb.StartAsync();
    }
}
