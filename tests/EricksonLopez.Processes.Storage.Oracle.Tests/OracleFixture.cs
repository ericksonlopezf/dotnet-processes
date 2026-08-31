// Copyright © Erickson Lopez. MIT License.
using System;
using System.Threading.Tasks;
using Oracle.ManagedDataAccess.Client;
using Testcontainers.Oracle;
using Xunit;

namespace EricksonLopez.Processes.Storage.Oracle.Tests;

/// <summary>
/// Manages the shared Oracle Testcontainers instance lifecycle for integration testing.
/// </summary>
public sealed class OracleFixture : IAsyncLifetime
{
    private readonly OracleContainer _oracle = new OracleBuilder("gvenzl/oracle-free:23-slim")
        .Build();

    /// <summary>
    /// Gets the connection string to the running Oracle container.
    /// </summary>
    public string ConnectionString => _oracle.GetConnectionString();

    public async Task InitializeAsync()
    {
        await _oracle.StartAsync();

        await using var connection = new OracleConnection(ConnectionString);
        await connection.OpenAsync();

        const string ddl = """
            BEGIN
                EXECUTE IMMEDIATE 'CREATE TABLE PROCESS_INSTANCES (
                    PROCESS_ID VARCHAR2(36) PRIMARY KEY,
                    PROCESS_TYPE VARCHAR2(128) NOT NULL,
                    VERSION NUMBER(10) NOT NULL,
                    STATUS NUMBER(10) NOT NULL,
                    REVISION NUMBER(19) NOT NULL,
                    CORRELATION_ID VARCHAR2(128) NOT NULL,
                    STATE_PAYLOAD CLOB NOT NULL,
                    CREATED_AT VARCHAR2(35) NOT NULL,
                    UPDATED_AT VARCHAR2(35) NOT NULL,
                    COMPLETED_AT VARCHAR2(35) NULL
                )';
                EXECUTE IMMEDIATE 'CREATE INDEX IDX_PI_CORR_ID ON PROCESS_INSTANCES (CORRELATION_ID)';
            EXCEPTION
                WHEN OTHERS THEN
                    IF SQLCODE != -955 THEN
                        RAISE;
                    END IF;
            END;
            """;

        await using var cmd = new OracleCommand(ddl, connection);
        await cmd.ExecuteNonQueryAsync();
    }

    public async Task DisposeAsync()
    {
        await _oracle.DisposeAsync();
    }

    /// <summary>
    /// Restarts the container to simulate a database crash or connection drop.
    /// </summary>
    public async Task RestartAsync()
    {
        await _oracle.StopAsync();
        await _oracle.StartAsync();
    }
}
