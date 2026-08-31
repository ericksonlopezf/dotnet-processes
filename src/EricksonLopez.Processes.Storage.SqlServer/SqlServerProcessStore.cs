// Copyright © Erickson Lopez. MIT License.
using System;
using System.Data;
using System.Diagnostics.CodeAnalysis;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using EricksonLopez.Processes.Abstractions;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

namespace EricksonLopez.Processes.Storage.SqlServer;

/// <summary>
/// Provides a SQL Server implementation of <see cref="IProcessStore{TState}"/> using raw parameterized SqlClient commands and serialized JSON payloads.
/// </summary>
/// <typeparam name="TState">The process domain state type.</typeparam>
[SuppressMessage("Security", "S2077:A formatted SQL query is vulnerable to SQL injection", Justification = "Table name is validated and injected via configuration")]
public sealed class SqlServerProcessStore<TState> : IProcessStore<TState>
    where TState : notnull
{
    private readonly string _connectionString;
    private readonly string _tableName;
    private readonly IProcessStateSerializer<TState> _serializer;

    /// <summary>
    /// Initializes a new instance of the <see cref="SqlServerProcessStore{TState}"/> class with the specified connection string and serializer.
    /// </summary>
    /// <param name="connectionString">The SQL Server connection string.</param>
    /// <param name="serializer">The state serializer instance.</param>
    /// <param name="tableName">The table name for process instances (defaults to 'ProcessInstances').</param>
    /// <param name="logger">The optional logger instance, or <see langword="null"/>.</param>
    /// <exception cref="ArgumentException"><paramref name="connectionString"/> or <paramref name="tableName"/> is <see langword="null"/> or white-space</exception>
    /// <exception cref="ArgumentNullException"><paramref name="serializer"/> is <see langword="null"/></exception>
    public SqlServerProcessStore(
        string connectionString,
        IProcessStateSerializer<TState> serializer,
        string tableName = "ProcessInstances",
        ILogger<SqlServerProcessStore<TState>>? logger = null)
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
            SELECT ProcessId, ProcessType, Version, Status, Revision, CorrelationId,
                   StatePayload, CreatedAt, UpdatedAt, CompletedAt
            FROM {_tableName} WITH (NOLOCK)
            WHERE ProcessId = @ProcessId;
            """;

        await using var connection = new SqlConnection(_connectionString);
        await connection.OpenAsync(cancellationToken);

        await using var command = new SqlCommand(sql, connection);
#pragma warning restore CA2100, S2077
        command.Parameters.Add("@ProcessId", SqlDbType.UniqueIdentifier).Value = id.Value;

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
            SELECT TOP(1) ProcessId, ProcessType, Version, Status, Revision, CorrelationId,
                   StatePayload, CreatedAt, UpdatedAt, CompletedAt
            FROM {_tableName} WITH (NOLOCK)
            WHERE CorrelationId = @CorrelationId;
            """;

        await using var connection = new SqlConnection(_connectionString);
        await connection.OpenAsync(cancellationToken);

        await using var command = new SqlCommand(sql, connection);
#pragma warning restore CA2100, S2077
        command.Parameters.Add("@CorrelationId", SqlDbType.NVarChar, 128).Value = correlationId.Value;

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
        var sql = $"SELECT 1 FROM {_tableName} WITH (NOLOCK) WHERE ProcessId = @ProcessId;";

        await using var connection = new SqlConnection(_connectionString);
        await connection.OpenAsync(cancellationToken);

        await using var command = new SqlCommand(sql, connection);
#pragma warning restore CA2100, S2077
        command.Parameters.Add("@ProcessId", SqlDbType.UniqueIdentifier).Value = id.Value;

        var result = await command.ExecuteScalarAsync(cancellationToken);
        return result is not null;
    }

    /// <inheritdoc />
    public async ValueTask<ProcessSaveResult> SaveAsync(ProcessInstance<TState> instance, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(instance);

        var payloadBytes = _serializer.Serialize(instance.State);
        var payloadJson = Encoding.UTF8.GetString(payloadBytes);

        await using var connection = new SqlConnection(_connectionString);
        await connection.OpenAsync(cancellationToken);

        if (instance.Revision.Value == 1)
        {
            return await InsertInitialAsync(connection, instance, payloadJson, cancellationToken);
        }

        return await UpdateExistingAsync(connection, instance, payloadJson, cancellationToken);
    }

    private async ValueTask<ProcessSaveResult> InsertInitialAsync(
        SqlConnection connection,
        ProcessInstance<TState> instance,
        string payloadJson,
        CancellationToken cancellationToken)
    {
#pragma warning disable CA2100, S2077
        var insertSql = $"""
            IF NOT EXISTS (SELECT 1 FROM {_tableName} WHERE ProcessId = @ProcessId)
            BEGIN
                INSERT INTO {_tableName} (
                    ProcessId, ProcessType, Version, Status, Revision, CorrelationId,
                    StatePayload, CreatedAt, UpdatedAt, CompletedAt
                )
                VALUES (
                    @ProcessId, @ProcessType, @Version, @Status, @Revision, @CorrelationId,
                    @StatePayload, @CreatedAt, @UpdatedAt, @CompletedAt
                );
            END
            """;

        await using var insertCmd = new SqlCommand(insertSql, connection);
#pragma warning restore CA2100, S2077
        AddParameters(insertCmd, instance, payloadJson);

        var rows = await insertCmd.ExecuteNonQueryAsync(cancellationToken);
        return rows == 1 ? ProcessSaveResult.Success : ProcessSaveResult.ConcurrencyConflict;
    }

    private async ValueTask<ProcessSaveResult> UpdateExistingAsync(
        SqlConnection connection,
        ProcessInstance<TState> instance,
        string payloadJson,
        CancellationToken cancellationToken)
    {
        var expectedPreviousRevision = instance.Revision.Value - 1;
#pragma warning disable CA2100, S2077
        var updateSql = $"""
            UPDATE {_tableName}
            SET Status = @Status,
                Revision = @Revision,
                StatePayload = @StatePayload,
                UpdatedAt = @UpdatedAt,
                CompletedAt = @CompletedAt
            WHERE ProcessId = @ProcessId AND Revision = @ExpectedRevision;
            """;

        await using var updateCmd = new SqlCommand(updateSql, connection);
#pragma warning restore CA2100, S2077
        AddParameters(updateCmd, instance, payloadJson);
        updateCmd.Parameters.Add("@ExpectedRevision", SqlDbType.BigInt).Value = expectedPreviousRevision;

        var updatedRows = await updateCmd.ExecuteNonQueryAsync(cancellationToken);
        if (updatedRows <= 0)
        {
            var exists = await ExistsAsync(instance.Id, cancellationToken);
            return exists ? ProcessSaveResult.ConcurrencyConflict : ProcessSaveResult.NotFound;
        }

        return ProcessSaveResult.Success;
    }

    private static void AddParameters(SqlCommand cmd, ProcessInstance<TState> instance, string payloadJson)
    {
        cmd.Parameters.Add("@ProcessId", SqlDbType.UniqueIdentifier).Value = instance.Id.Value;
        cmd.Parameters.Add("@ProcessType", SqlDbType.NVarChar, 128).Value = instance.Type.Value;
        cmd.Parameters.Add("@Version", SqlDbType.Int).Value = instance.Version.Value;
        cmd.Parameters.Add("@Status", SqlDbType.Int).Value = (int)instance.Status;
        cmd.Parameters.Add("@Revision", SqlDbType.BigInt).Value = instance.Revision.Value;
        cmd.Parameters.Add("@CorrelationId", SqlDbType.NVarChar, 128).Value = instance.CorrelationId.Value;
        cmd.Parameters.Add("@StatePayload", SqlDbType.NVarChar, -1).Value = payloadJson;
        cmd.Parameters.Add("@CreatedAt", SqlDbType.DateTimeOffset).Value = instance.CreatedAt;
        cmd.Parameters.Add("@UpdatedAt", SqlDbType.DateTimeOffset).Value = instance.UpdatedAt;
        if (instance.CompletedAt.HasValue)
        {
            cmd.Parameters.Add("@CompletedAt", SqlDbType.DateTimeOffset).Value = instance.CompletedAt.Value;
        }
        else
        {
            cmd.Parameters.Add("@CompletedAt", SqlDbType.DateTimeOffset).Value = DBNull.Value;
        }
    }

    private ProcessInstance<TState> MapFromReader(SqlDataReader reader)
    {
        var id = new ProcessId(reader.GetGuid(0));
        var type = ProcessType.From(reader.GetString(1));
        var version = new ProcessVersion(reader.GetInt32(2));
        var status = (ProcessStatus)reader.GetInt32(3);
        var revision = Revision.From(reader.GetInt64(4));
        var correlationId = CorrelationId.From(reader.GetString(5));
        var jsonPayload = reader.GetString(6);
        var createdAt = reader.GetDateTimeOffset(7);
        var updatedAt = reader.GetDateTimeOffset(8);
        var completedAt = reader.IsDBNull(9) ? (DateTimeOffset?)null : reader.GetDateTimeOffset(9);

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
