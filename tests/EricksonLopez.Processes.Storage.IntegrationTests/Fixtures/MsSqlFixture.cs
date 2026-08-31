// Copyright © Erickson Lopez. MIT License.
using System;
using System.Threading.Tasks;
using Microsoft.Data.SqlClient;
using Testcontainers.MsSql;
using Xunit;

namespace EricksonLopez.Processes.Storage.IntegrationTests.Fixtures;

/// <summary>
/// Manages the shared Microsoft SQL Server Testcontainers instance lifecycle for integration testing.
/// </summary>
public sealed class MsSqlFixture : IAsyncLifetime
{
    private readonly MsSqlContainer _mssql = new MsSqlBuilder("mcr.microsoft.com/mssql/server:2022-latest")
        .Build();

    /// <summary>
    /// Gets the connection string to the running SQL Server container.
    /// </summary>
    public string ConnectionString => _mssql.GetConnectionString();

    public async Task InitializeAsync()
    {
        await _mssql.StartAsync();

        // Execute initial DDL schema creation once per fixture
        await using var connection = new SqlConnection(ConnectionString);
        await connection.OpenAsync();

        const string ddl = """
            IF NOT EXISTS (SELECT * FROM sys.tables WHERE name = 'ProcessInstances')
            BEGIN
                CREATE TABLE ProcessInstances (
                    ProcessId UNIQUEIDENTIFIER PRIMARY KEY,
                    ProcessType NVARCHAR(128) NOT NULL,
                    Version INT NOT NULL,
                    Status INT NOT NULL,
                    Revision BIGINT NOT NULL,
                    CorrelationId NVARCHAR(128) NOT NULL,
                    StatePayload NVARCHAR(MAX) NOT NULL,
                    CreatedAt DATETIMEOFFSET NOT NULL,
                    UpdatedAt DATETIMEOFFSET NOT NULL,
                    CompletedAt DATETIMEOFFSET NULL
                );
                CREATE INDEX IX_ProcessInstances_CorrelationId ON ProcessInstances(CorrelationId);
            END
            """;

        await using var cmd = new SqlCommand(ddl, connection);
        await cmd.ExecuteNonQueryAsync();
    }

    public async Task DisposeAsync()
    {
        await _mssql.DisposeAsync();
    }

    /// <summary>
    /// Restarts the container to simulate a database crash or connection drop.
    /// </summary>
    public async Task RestartAsync()
    {
        await _mssql.StopAsync();
        await _mssql.StartAsync();
    }
}
