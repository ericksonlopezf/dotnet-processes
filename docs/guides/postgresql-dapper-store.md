# Implementation Guide: PostgreSQL and Dapper Storage Provider

This technical guide demonstrates how to implement the `IProcessStore<TState>` persistence SPI using **PostgreSQL** and **Dapper** (or direct ADO.NET with `Npgsql`), enforcing atomic **Optimistic Concurrency Control (OCC CAS)** using monotonic `Revision` tokens.

---

## 1. PostgreSQL Schema Definition

```sql
CREATE TABLE IF NOT EXISTS process_instances (
    process_id UUID NOT NULL PRIMARY KEY,
    process_type VARCHAR(128) NOT NULL,
    process_version INT NOT NULL,
    correlation_id VARCHAR(128) NOT NULL,
    status VARCHAR(32) NOT NULL,
    revision BIGINT NOT NULL,
    state_payload JSONB NOT NULL,
    created_at TIMESTAMPTZ NOT NULL,
    updated_at TIMESTAMPTZ NOT NULL,
    completed_at TIMESTAMPTZ NULL
);

CREATE INDEX IF NOT EXISTS idx_process_instances_correlation 
ON process_instances (correlation_id);

CREATE INDEX IF NOT EXISTS idx_process_instances_status 
ON process_instances (status);
```

---

## 2. Implementation: `PostgresDapperProcessStore<TState>`

```csharp
using System;
using System.Data;
using System.Text.Json;
using System.Text.Json.Serialization.Metadata;
using System.Threading;
using System.Threading.Tasks;
using Dapper;
using EricksonLopez.Processes.Abstractions;
using Npgsql;

namespace MyApp.Infrastructure.Processes;

public sealed class PostgresDapperProcessStore<TState> : IProcessStore<TState>
    where TState : notnull
{
    private readonly string _connectionString;
    private readonly JsonTypeInfo<TState> _jsonTypeInfo;

    public PostgresDapperProcessStore(string connectionString, JsonTypeInfo<TState> jsonTypeInfo)
    {
        _connectionString = connectionString ?? throw new ArgumentNullException(nameof(connectionString));
        _jsonTypeInfo = jsonTypeInfo ?? throw new ArgumentNullException(nameof(jsonTypeInfo));
    }

    public async ValueTask<ProcessInstance<TState>?> GetByIdAsync(
        ProcessId id, 
        CancellationToken cancellationToken = default)
    {
        const string sql = """
            SELECT process_id, process_type, process_version, correlation_id, status, revision, state_payload, created_at, updated_at, completed_at
            FROM process_instances
            WHERE process_id = @Id
            """;

        await using var connection = new NpgsqlConnection(_connectionString);
        await connection.OpenAsync(cancellationToken);

        var row = await connection.QuerySingleOrDefaultAsync<ProcessInstanceRow>(
            new CommandDefinition(sql, new { Id = id.Value }, cancellationToken: cancellationToken));

        if (row is null) return null;

        var state = JsonSerializer.Deserialize(row.state_payload, _jsonTypeInfo)!;

        return ProcessInstance<TState>.Create(
            ProcessId.From(row.process_id),
            ProcessType.From(row.process_type),
            ProcessVersion.From(row.process_version),
            CorrelationId.From(row.correlation_id),
            state,
            row.created_at);
    }

    public async ValueTask<ProcessSaveResult> SaveAsync(
        ProcessInstance<TState> instance, 
        CancellationToken cancellationToken = default)
    {
        var jsonState = JsonSerializer.Serialize(instance.State, _jsonTypeInfo);

        await using var connection = new NpgsqlConnection(_connectionString);
        await connection.OpenAsync(cancellationToken);

        if (instance.Revision.Value == 1)
        {
            const string insertSql = """
                INSERT INTO process_instances (
                    process_id, process_type, process_version, correlation_id, status, revision, state_payload, created_at, updated_at, completed_at
                ) VALUES (
                    @Id, @Type, @Version, @CorrelationId, @Status, @Revision, @State::jsonb, @CreatedAt, @UpdatedAt, @CompletedAt
                ) ON CONFLICT (process_id) DO NOTHING;
                """;

            var rowsInserted = await connection.ExecuteAsync(new CommandDefinition(insertSql, new
            {
                Id = instance.Id.Value,
                Type = instance.Type.Value,
                Version = instance.Version.Value,
                CorrelationId = instance.CorrelationId.Value,
                Status = instance.Status.ToString(),
                Revision = instance.Revision.Value,
                State = jsonState,
                CreatedAt = instance.CreatedAt,
                UpdatedAt = instance.UpdatedAt,
                CompletedAt = instance.CompletedAt
            }, cancellationToken: cancellationToken));

            return rowsInserted == 1 
                ? ProcessSaveResult.Success 
                : ProcessSaveResult.ConcurrencyConflict;
        }

        const string updateSql = """
            UPDATE process_instances
            SET status = @Status,
                revision = @NewRevision,
                state_payload = @State::jsonb,
                updated_at = @UpdatedAt,
                completed_at = @CompletedAt
            WHERE process_id = @Id AND revision = @ExpectedRevision;
            """;

        var rowsUpdated = await connection.ExecuteAsync(new CommandDefinition(updateSql, new
        {
            Id = instance.Id.Value,
            Status = instance.Status.ToString(),
            NewRevision = instance.Revision.Value,
            ExpectedRevision = instance.Revision.Value - 1,
            State = jsonState,
            UpdatedAt = instance.UpdatedAt,
            CompletedAt = instance.CompletedAt
        }, cancellationToken: cancellationToken));

        return rowsUpdated == 1 
            ? ProcessSaveResult.Success 
            : ProcessSaveResult.ConcurrencyConflict;
    }

    public async ValueTask<bool> ExistsAsync(ProcessId id, CancellationToken cancellationToken = default)
    {
        const string sql = "SELECT COUNT(1) FROM process_instances WHERE process_id = @Id";
        await using var connection = new NpgsqlConnection(_connectionString);
        await connection.OpenAsync(cancellationToken);
        return await connection.ExecuteScalarAsync<int>(new CommandDefinition(sql, new { Id = id.Value }, cancellationToken: cancellationToken)) > 0;
    }

    private sealed record ProcessInstanceRow(
        Guid process_id,
        string process_type,
        int process_version,
        string correlation_id,
        string status,
        long revision,
        string state_payload,
        DateTimeOffset created_at,
        DateTimeOffset updated_at,
        DateTimeOffset? completed_at);
}
```

---

## 3. Native Package Option

Instead of writing manual Dapper queries, consuming applications can directly reference the pre-built, production-hardened provider package:
```csharp
builder.Services.AddPostgreSqlProcessStore<OrderState>(
    connectionString: "Host=localhost;Database=orders;Username=postgres;Password=secret",
    tableName: "order_processes");
```
