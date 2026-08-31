// Copyright © Erickson Lopez. MIT License.
using System;
using System.Threading.Tasks;
using Microsoft.Data.Sqlite;
using Xunit;

namespace EricksonLopez.Processes.Storage.Sqlite.Tests;

/// <summary>
/// Manages an in-memory SQLite shared-cache instance for testing.
/// </summary>
public sealed class SqliteFixture : IAsyncLifetime, IDisposable
{
    private readonly string _dbName = $"ProcessStore_{Guid.NewGuid():N}";
    private SqliteConnection? _keepAliveConnection;

    /// <summary>
    /// Gets the SQLite connection string.
    /// </summary>
    public string ConnectionString => $"Data Source={_dbName};Mode=Memory;Cache=Shared";

    public async Task InitializeAsync()
    {
        _keepAliveConnection = new SqliteConnection(ConnectionString);
        await _keepAliveConnection.OpenAsync();

        const string ddl = """
            CREATE TABLE IF NOT EXISTS ProcessInstances (
                ProcessId TEXT PRIMARY KEY,
                ProcessType TEXT NOT NULL,
                Version INTEGER NOT NULL,
                Status INTEGER NOT NULL,
                Revision INTEGER NOT NULL,
                CorrelationId TEXT NOT NULL,
                StatePayload TEXT NOT NULL,
                CreatedAt TEXT NOT NULL,
                UpdatedAt TEXT NOT NULL,
                CompletedAt TEXT NULL
            );
            CREATE INDEX IF NOT EXISTS IX_ProcessInstances_CorrelationId ON ProcessInstances(CorrelationId);
            """;

        await using var cmd = new SqliteCommand(ddl, _keepAliveConnection);
        await cmd.ExecuteNonQueryAsync();
    }

    public async Task DisposeAsync()
    {
        if (_keepAliveConnection is not null)
        {
            await _keepAliveConnection.DisposeAsync();
            _keepAliveConnection = null;
        }
    }

    public void Dispose()
    {
        _keepAliveConnection?.Dispose();
        _keepAliveConnection = null;
    }
}
