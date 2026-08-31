// Copyright © Erickson Lopez. MIT License.
using System;
using System.Data;
using System.Diagnostics.CodeAnalysis;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using EricksonLopez.Processes.Abstractions;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Npgsql;
using NpgsqlTypes;

namespace EricksonLopez.Processes.Storage.PostgreSql;

/// <summary>
/// Provides a PostgreSQL implementation of <see cref="IProcessStore{TState}"/> using raw parameterized Npgsql commands and JSONB payloads.
/// </summary>
/// <typeparam name="TState">The process domain state type.</typeparam>
[SuppressMessage("Security", "S2077:A formatted SQL query is vulnerable to SQL injection", Justification = "Table name is validated and injected via configuration")]
public sealed class PostgreSqlProcessStore<TState> : IProcessStore<TState>
    where TState : notnull
{
    private readonly string _connectionString;
    private readonly string _tableName;
    private readonly IProcessStateSerializer<TState> _serializer;

    /// <summary>
    /// Initializes a new instance of the <see cref="PostgreSqlProcessStore{TState}"/> class with the specified connection string and serializer.
    /// </summary>
    /// <param name="connectionString">The PostgreSQL connection string.</param>
    /// <param name="serializer">The state serializer instance.</param>
    /// <param name="tableName">The table name for process instances (defaults to 'process_instances').</param>
    /// <param name="logger">The optional logger instance, or <see langword="null"/>.</param>
    /// <exception cref="ArgumentException"><paramref name="connectionString"/> or <paramref name="tableName"/> is <see langword="null"/> or white-space</exception>
    /// <exception cref="ArgumentNullException"><paramref name="serializer"/> is <see langword="null"/></exception>
    public PostgreSqlProcessStore(
        string connectionString,
        IProcessStateSerializer<TState> serializer,
        string tableName = "process_instances",
        ILogger<PostgreSqlProcessStore<TState>>? logger = null)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(connectionString);
        ArgumentException.ThrowIfNullOrWhiteSpace(tableName);
        ArgumentNullException.ThrowIfNull(serializer);

        _connectionString = connectionString;
        _tableName = tableName;
        _serializer = serializer;
    }

    /// <inheritdoc />
    public async ValueTask<ProcessInstance<TState>?> GetByIdAsync(ProcessId id, CancellationToken cancellationToken = default)
    {
#pragma warning disable CA2100, S2077
        var sql = $"""
            SELECT process_id, process_type, version, status, revision, correlation_id,
                   state_payload, created_at, updated_at, completed_at
            FROM {_tableName}
            WHERE process_id = $1;
            """;

        await using var connection = new NpgsqlConnection(_connectionString);
        await connection.OpenAsync(cancellationToken);

        await using var command = new NpgsqlCommand(sql, connection);
#pragma warning restore CA2100, S2077
        command.Parameters.AddWithValue(NpgsqlDbType.Uuid, id.Value);

        await using var reader = await command.ExecuteReaderAsync(CommandBehavior.SingleRow, cancellationToken);
        if (!await reader.ReadAsync(cancellationToken))
        {
            return null;
        }

        return MapFromReader(reader);
    }

    /// <inheritdoc />
    public async ValueTask<ProcessInstance<TState>?> GetByCorrelationIdAsync(CorrelationId correlationId, CancellationToken cancellationToken = default)
    {
#pragma warning disable CA2100, S2077
        var sql = $"""
            SELECT process_id, process_type, version, status, revision, correlation_id,
                   state_payload, created_at, updated_at, completed_at
            FROM {_tableName}
            WHERE correlation_id = $1
            LIMIT 1;
            """;

        await using var connection = new NpgsqlConnection(_connectionString);
        await connection.OpenAsync(cancellationToken);

        await using var command = new NpgsqlCommand(sql, connection);
#pragma warning restore CA2100, S2077
        command.Parameters.AddWithValue(NpgsqlDbType.Text, correlationId.Value);

        await using var reader = await command.ExecuteReaderAsync(CommandBehavior.SingleRow, cancellationToken);
        if (!await reader.ReadAsync(cancellationToken))
        {
            return null;
        }

        return MapFromReader(reader);
    }

    /// <inheritdoc />
    public async ValueTask<bool> ExistsAsync(ProcessId id, CancellationToken cancellationToken = default)
    {
#pragma warning disable CA2100, S2077
        var sql = $"SELECT 1 FROM {_tableName} WHERE process_id = $1 LIMIT 1;";

        await using var connection = new NpgsqlConnection(_connectionString);
        await connection.OpenAsync(cancellationToken);

        await using var command = new NpgsqlCommand(sql, connection);
#pragma warning restore CA2100, S2077
        command.Parameters.AddWithValue(NpgsqlDbType.Uuid, id.Value);

        var result = await command.ExecuteScalarAsync(cancellationToken);
        return result is not null;
    }

    /// <inheritdoc />
    public async ValueTask<ProcessSaveResult> SaveAsync(ProcessInstance<TState> instance, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(instance);

        var payloadBytes = _serializer.Serialize(instance.State);
        var payloadJson = Encoding.UTF8.GetString(payloadBytes);

        await using var connection = new NpgsqlConnection(_connectionString);
        await connection.OpenAsync(cancellationToken);

        if (instance.Revision.Value == 1)
        {
            return await InsertInitialAsync(connection, instance, payloadJson, cancellationToken);
        }

        return await UpdateExistingAsync(connection, instance, payloadJson, cancellationToken);
    }

    private async ValueTask<ProcessSaveResult> InsertInitialAsync(
        NpgsqlConnection connection,
        ProcessInstance<TState> instance,
        string payloadJson,
        CancellationToken cancellationToken)
    {
#pragma warning disable CA2100, S2077
        var insertSql = $"""
            INSERT INTO {_tableName} (
                process_id, process_type, version, status, revision, correlation_id,
                state_payload, created_at, updated_at, completed_at
            )
            VALUES ($1, $2, $3, $4, $5, $6, $7::jsonb, $8, $9, $10)
            ON CONFLICT (process_id) DO NOTHING;
            """;

        await using var insertCmd = new NpgsqlCommand(insertSql, connection);
#pragma warning restore CA2100, S2077
        AddParameters(insertCmd, instance, payloadJson);

        var rows = await insertCmd.ExecuteNonQueryAsync(cancellationToken);
        return rows == 0 ? ProcessSaveResult.ConcurrencyConflict : ProcessSaveResult.Success;
    }

    private async ValueTask<ProcessSaveResult> UpdateExistingAsync(
        NpgsqlConnection connection,
        ProcessInstance<TState> instance,
        string payloadJson,
        CancellationToken cancellationToken)
    {
        var expectedPreviousRevision = instance.Revision.Value - 1;
#pragma warning disable CA2100, S2077
        var updateSql = $"""
            UPDATE {_tableName}
            SET status = $4,
                revision = $5,
                state_payload = $7::jsonb,
                updated_at = $9,
                completed_at = $10
            WHERE process_id = $1 AND revision = $11;
            """;

        await using var updateCmd = new NpgsqlCommand(updateSql, connection);
#pragma warning restore CA2100, S2077
        AddParameters(updateCmd, instance, payloadJson);
        updateCmd.Parameters.AddWithValue(NpgsqlDbType.Bigint, expectedPreviousRevision);

        var updatedRows = await updateCmd.ExecuteNonQueryAsync(cancellationToken);
        if (updatedRows == 0)
        {
            var exists = await ExistsAsync(instance.Id, cancellationToken);
            return exists ? ProcessSaveResult.ConcurrencyConflict : ProcessSaveResult.NotFound;
        }

        return ProcessSaveResult.Success;
    }

    private static void AddParameters(NpgsqlCommand cmd, ProcessInstance<TState> instance, string payloadJson)
    {
        cmd.Parameters.AddWithValue(NpgsqlDbType.Uuid, instance.Id.Value);
        cmd.Parameters.AddWithValue(NpgsqlDbType.Text, instance.Type.Value);
        cmd.Parameters.AddWithValue(NpgsqlDbType.Integer, instance.Version.Value);
        cmd.Parameters.AddWithValue(NpgsqlDbType.Integer, (int)instance.Status);
        cmd.Parameters.AddWithValue(NpgsqlDbType.Bigint, instance.Revision.Value);
        cmd.Parameters.AddWithValue(NpgsqlDbType.Text, instance.CorrelationId.Value);
        cmd.Parameters.AddWithValue(NpgsqlDbType.Jsonb, payloadJson);
        cmd.Parameters.AddWithValue(NpgsqlDbType.TimestampTz, instance.CreatedAt);
        cmd.Parameters.AddWithValue(NpgsqlDbType.TimestampTz, instance.UpdatedAt);
        if (instance.CompletedAt.HasValue)
        {
            cmd.Parameters.AddWithValue(NpgsqlDbType.TimestampTz, instance.CompletedAt.Value);
        }
        else
        {
            cmd.Parameters.AddWithValue(NpgsqlDbType.TimestampTz, DBNull.Value);
        }
    }

    private ProcessInstance<TState> MapFromReader(NpgsqlDataReader reader)
    {
        var id = new ProcessId(reader.GetGuid(0));
        var type = ProcessType.From(reader.GetString(1));
        var version = new ProcessVersion(reader.GetInt32(2));
        var status = (ProcessStatus)reader.GetInt32(3);
        var revision = Revision.From(reader.GetInt64(4));
        var correlationId = CorrelationId.From(reader.GetString(5));
        var jsonPayload = reader.GetString(6);
        var createdAt = reader.GetFieldValue<DateTimeOffset>(7);
        var updatedAt = reader.GetFieldValue<DateTimeOffset>(8);
        var completedAt = reader.IsDBNull(9) ? (DateTimeOffset?)null : reader.GetFieldValue<DateTimeOffset>(9);

        var payloadBytes = Encoding.UTF8.GetBytes(jsonPayload);
        var state = _serializer.Deserialize(payloadBytes);

        return new ProcessInstance<TState>(
            id: id,
            type: type,
            version: version,
            status: status,
            revision: revision,
            correlationId: correlationId,
            createdAt: createdAt,
            updatedAt: updatedAt,
            completedAt: completedAt,
            state: state);
    }
}
