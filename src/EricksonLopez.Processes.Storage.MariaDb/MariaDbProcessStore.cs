// Copyright © Erickson Lopez. MIT License.
using System;
using System.Data;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using EricksonLopez.Processes.Abstractions;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using MySqlConnector;

namespace EricksonLopez.Processes.Storage.MariaDb;

/// <summary>
/// Provides a MariaDB implementation of <see cref="IProcessStore{TState}"/> using raw parameterized MySqlConnector commands and JSON payloads.
/// </summary>
/// <typeparam name="TState">The process domain state type.</typeparam>
[SuppressMessage("Security", "S2077:A formatted SQL query is vulnerable to SQL injection", Justification = "Table name is validated and injected via configuration")]
public sealed class MariaDbProcessStore<TState> : IProcessStore<TState>
    where TState : notnull
{
    private readonly string _connectionString;
    private readonly string _tableName;
    private readonly IProcessStateSerializer<TState> _serializer;

    /// <summary>
    /// Initializes a new instance of the <see cref="MariaDbProcessStore{TState}"/> class with the specified connection string and serializer.
    /// </summary>
    /// <param name="connectionString">The MariaDB connection string.</param>
    /// <param name="serializer">The state serializer instance.</param>
    /// <param name="tableName">The table name for process instances (defaults to 'process_instances').</param>
    /// <param name="logger">The optional logger instance, or <see langword="null"/>.</param>
    /// <exception cref="ArgumentException"><paramref name="connectionString"/> or <paramref name="tableName"/> is <see langword="null"/> or white-space</exception>
    /// <exception cref="ArgumentNullException"><paramref name="serializer"/> is <see langword="null"/></exception>
    public MariaDbProcessStore(
        string connectionString,
        IProcessStateSerializer<TState> serializer,
        string tableName = "process_instances",
        ILogger<MariaDbProcessStore<TState>>? logger = null)
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
            WHERE process_id = @ProcessId
            LIMIT 1;
            """;

        await using var connection = new MySqlConnection(_connectionString);
        await connection.OpenAsync(cancellationToken);

        await using var command = new MySqlCommand(sql, connection);
#pragma warning restore CA2100, S2077
        command.Parameters.Add("@ProcessId", MySqlDbType.VarChar, 36).Value = id.Value.ToString();

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
            WHERE correlation_id = @CorrelationId
            LIMIT 1;
            """;

        await using var connection = new MySqlConnection(_connectionString);
        await connection.OpenAsync(cancellationToken);

        await using var command = new MySqlCommand(sql, connection);
#pragma warning restore CA2100, S2077
        command.Parameters.Add("@CorrelationId", MySqlDbType.VarChar, 128).Value = correlationId.Value;

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
        var sql = $"SELECT 1 FROM {_tableName} WHERE process_id = @ProcessId LIMIT 1;";

        await using var connection = new MySqlConnection(_connectionString);
        await connection.OpenAsync(cancellationToken);

        await using var command = new MySqlCommand(sql, connection);
#pragma warning restore CA2100, S2077
        command.Parameters.Add("@ProcessId", MySqlDbType.VarChar, 36).Value = id.Value.ToString();

        var result = await command.ExecuteScalarAsync(cancellationToken);
        return result is not null;
    }

    /// <inheritdoc />
    public async ValueTask<ProcessSaveResult> SaveAsync(ProcessInstance<TState> instance, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(instance);

        var payloadBytes = _serializer.Serialize(instance.State);
        var payloadJson = Encoding.UTF8.GetString(payloadBytes);

        await using var connection = new MySqlConnection(_connectionString);
        await connection.OpenAsync(cancellationToken);

        if (instance.Revision.Value == 1)
        {
            return await InsertInitialAsync(connection, instance, payloadJson, cancellationToken);
        }

        return await UpdateExistingAsync(connection, instance, payloadJson, cancellationToken);
    }

    private async ValueTask<ProcessSaveResult> InsertInitialAsync(
        MySqlConnection connection,
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
            SELECT @ProcessId, @ProcessType, @Version, @Status, @Revision, @CorrelationId,
                   @StatePayload, @CreatedAt, @UpdatedAt, @CompletedAt
            WHERE NOT EXISTS (
                SELECT 1 FROM {_tableName} WHERE process_id = @ProcessId
            );
            """;

        await using var insertCmd = new MySqlCommand(insertSql, connection);
#pragma warning restore CA2100, S2077
        AddParameters(insertCmd, instance, payloadJson);

        var rows = await insertCmd.ExecuteNonQueryAsync(cancellationToken);
        return rows == 0 ? ProcessSaveResult.ConcurrencyConflict : ProcessSaveResult.Success;
    }

    private async ValueTask<ProcessSaveResult> UpdateExistingAsync(
        MySqlConnection connection,
        ProcessInstance<TState> instance,
        string payloadJson,
        CancellationToken cancellationToken)
    {
        var expectedPreviousRevision = instance.Revision.Value - 1;
#pragma warning disable CA2100, S2077
        var updateSql = $"""
            UPDATE {_tableName}
            SET status = @Status,
                revision = @Revision,
                state_payload = @StatePayload,
                updated_at = @UpdatedAt,
                completed_at = @CompletedAt
            WHERE process_id = @ProcessId AND revision = @ExpectedRevision;
            """;

        await using var updateCmd = new MySqlCommand(updateSql, connection);
#pragma warning restore CA2100, S2077
        AddParameters(updateCmd, instance, payloadJson);
        updateCmd.Parameters.Add("@ExpectedRevision", MySqlDbType.Int64).Value = expectedPreviousRevision;

        var updatedRows = await updateCmd.ExecuteNonQueryAsync(cancellationToken);
        if (updatedRows == 0)
        {
            var exists = await ExistsAsync(instance.Id, cancellationToken);
            return exists ? ProcessSaveResult.ConcurrencyConflict : ProcessSaveResult.NotFound;
        }

        return ProcessSaveResult.Success;
    }

    private static void AddParameters(MySqlCommand cmd, ProcessInstance<TState> instance, string payloadJson)
    {
        cmd.Parameters.Add("@ProcessId", MySqlDbType.VarChar, 36).Value = instance.Id.Value.ToString();
        cmd.Parameters.Add("@ProcessType", MySqlDbType.VarChar, 128).Value = instance.Type.Value;
        cmd.Parameters.Add("@Version", MySqlDbType.Int32).Value = instance.Version.Value;
        cmd.Parameters.Add("@Status", MySqlDbType.Int32).Value = (int)instance.Status;
        cmd.Parameters.Add("@Revision", MySqlDbType.Int64).Value = instance.Revision.Value;
        cmd.Parameters.Add("@CorrelationId", MySqlDbType.VarChar, 128).Value = instance.CorrelationId.Value;
        cmd.Parameters.Add("@StatePayload", MySqlDbType.LongText).Value = payloadJson;
        cmd.Parameters.Add("@CreatedAt", MySqlDbType.VarChar, 35).Value = instance.CreatedAt.ToString("o", CultureInfo.InvariantCulture);
        cmd.Parameters.Add("@UpdatedAt", MySqlDbType.VarChar, 35).Value = instance.UpdatedAt.ToString("o", CultureInfo.InvariantCulture);
        if (instance.CompletedAt.HasValue)
        {
            cmd.Parameters.Add("@CompletedAt", MySqlDbType.VarChar, 35).Value = instance.CompletedAt.Value.ToString("o", CultureInfo.InvariantCulture);
        }
        else
        {
            cmd.Parameters.Add("@CompletedAt", MySqlDbType.VarChar, 35).Value = DBNull.Value;
        }
    }

    private ProcessInstance<TState> MapFromReader(MySqlDataReader reader)
    {
        var id = new ProcessId(Guid.Parse(reader.GetString(0)));
        var type = ProcessType.From(reader.GetString(1));
        var version = new ProcessVersion(reader.GetInt32(2));
        var status = (ProcessStatus)reader.GetInt32(3);
        var revision = Revision.From(reader.GetInt64(4));
        var correlationId = CorrelationId.From(reader.GetString(5));
        var jsonPayload = reader.GetString(6);

        var createdAt = DateTimeOffset.Parse(reader.GetString(7), CultureInfo.InvariantCulture);
        var updatedAt = DateTimeOffset.Parse(reader.GetString(8), CultureInfo.InvariantCulture);
        var completedAt = reader.IsDBNull(9) ? (DateTimeOffset?)null : DateTimeOffset.Parse(reader.GetString(9), CultureInfo.InvariantCulture);

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
