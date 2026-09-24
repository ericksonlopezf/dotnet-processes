// Copyright © Erickson Lopez. MIT License.
using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Json.Serialization;
using System.Threading;
using System.Threading.Tasks;
using AwesomeAssertions;
using EricksonLopez.Processes;
using EricksonLopez.Processes.Abstractions;
using EricksonLopez.Processes.Storage.Sqlite;
using EricksonLopez.Processes.SystemTextJson;
using Microsoft.Data.Sqlite;
using Xunit;

namespace EricksonLopez.Processes.Tests;

public sealed record AuditState(string Value, int Step) : IProcessState;

[JsonSerializable(typeof(AuditState))]
internal sealed partial class AuditJsonContext : JsonSerializerContext
{
}

/// <summary>
/// Adversarial audit regression tests reproducing critical architecture, security, and concurrency flaws.
/// </summary>
public sealed class AdversarialAuditTests : IAsyncDisposable
{
    private readonly string _dbPath;
    private readonly string _connectionString;

    public AdversarialAuditTests()
    {
        _dbPath = Path.Combine(Path.GetTempPath(), $"audit_test_{Guid.NewGuid():N}.db");
        _connectionString = $"Data Source={_dbPath}";
    }

    public async ValueTask DisposeAsync()
    {
        SqliteConnection.ClearAllPools();
        if (File.Exists(_dbPath))
        {
            try
            {
                File.Delete(_dbPath);
            }
            catch (IOException)
            {
                // Best effort cleanup in test temp folder
            }
        }
        await Task.CompletedTask;
    }

    private async Task InitializeDatabaseAsync(string tableName = "process_instances")
    {
        await using var conn = new SqliteConnection(_connectionString);
        await conn.OpenAsync();
#pragma warning disable CA2100
        await using var cmd = conn.CreateCommand();
        cmd.CommandText = $"""
            CREATE TABLE IF NOT EXISTS {tableName} (
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
            """;
        await cmd.ExecuteNonQueryAsync();
#pragma warning restore CA2100
    }

    public sealed record AuditInitiateEvent(Guid Id, string Data);

    public sealed class AuditCorrelation : IProcessCorrelation<AuditInitiateEvent>
    {
        public ProcessId ExtractProcessId(AuditInitiateEvent message) => ProcessId.From(message.Id);
        public CorrelationId ExtractCorrelationId(AuditInitiateEvent message) => CorrelationId.From(message.Id.ToString());
        public CausationId? ExtractCausationId(AuditInitiateEvent message) => null;
    }

    public sealed class AuditInitiateHandler : IProcessHandler<AuditState, AuditInitiateEvent>
    {
        public ProcessType Type => ProcessType.From("AuditProcess");
        public ProcessVersion Version => ProcessVersion.Initial;

        public ValueTask<ProcessTransitionResult<AuditState>> HandleAsync(
            AuditState currentState,
            AuditInitiateEvent message,
            ProcessContext context)
        {
            var newState = currentState with { Value = message.Data, Step = 1 };
            var comp = new CompensationStep("CompensateStep1", new { Reason = "Audit" }, DateTimeOffset.UtcNow);

            return ValueTask.FromResult(ProcessTransitionResult<AuditState>.Advance(
                newState,
                ProcessStatus.Running,
                effects: Array.Empty<ProcessEffect>(),
                recordedCompensations: new[] { comp }));
        }
    }

    [Fact]
    public async Task REMEDIATED_ProcessCoordinator_WithSqliteStore_SuccessfullyInitiatesAndPersists()
    {
        await InitializeDatabaseAsync();

        // ARRANGE
        var serializer = new SystemTextJsonProcessStateSerializer<AuditState>(AuditJsonContext.Default.AuditState);
        var store = new SqliteProcessStore<AuditState>(_connectionString, serializer, "process_instances");
        var coordinator = new ProcessCoordinator<AuditState>(store);

        var handler = new AuditInitiateHandler();
        var correlation = new AuditCorrelation();
        var processId = Guid.NewGuid();
        var evt = new AuditInitiateEvent(processId, "Order-123");

        // ACT
        var result = await coordinator.ExecuteAsync(
            handler,
            correlation,
            evt,
            initialStateFactory: _ => new AuditState("initial", 0),
            canInitiate: true);

        // ASSERT: Initial save succeeds even with revision advanced to 2
        result.SaveResult.Should().Be(ProcessSaveResult.Success);
        result.IsSuccess.Should().BeTrue();

        // Querying the store confirms the instance was successfully persisted to the database
        var savedInDb = await store.GetByIdAsync(ProcessId.From(processId));
        savedInDb.Should().NotBeNull();
        savedInDb!.Revision.Value.Should().Be(2);
        savedInDb.Status.Should().Be(ProcessStatus.Running);
        savedInDb.State.Value.Should().Be("Order-123");
    }

    [Fact]
    public async Task REMEDIATED_ProcessCoordinator_RetainsRecordedCompensations_OnInstanceAndResult()
    {
        await InitializeDatabaseAsync();

        // ARRANGE
        var serializer = new SystemTextJsonProcessStateSerializer<AuditState>(AuditJsonContext.Default.AuditState);
        var store = new SqliteProcessStore<AuditState>(_connectionString, serializer, "process_instances");
        var coordinator = new ProcessCoordinator<AuditState>(store);

        var handler = new AuditInitiateHandler();
        var correlation = new AuditCorrelation();
        var processId = Guid.NewGuid();
        var evt = new AuditInitiateEvent(processId, "Order-123");

        // ACT
        var result = await coordinator.ExecuteAsync(
            handler,
            correlation,
            evt,
            initialStateFactory: _ => new AuditState("initial", 0),
            canInitiate: true);

        // ASSERT: Compensations recorded during transition are retained on ProcessInstance and ProcessExecutionResult
        result.Instance.RecordedCompensations.Should().NotBeEmpty();
        result.Instance.RecordedCompensations.Should().HaveCount(1);
        result.Instance.RecordedCompensations[0].StepName.Should().Be("CompensateStep1");

        result.RecordedCompensations.Should().NotBeEmpty();
        result.RecordedCompensations[0].StepName.Should().Be("CompensateStep1");
    }

    [Fact]
    public void REMEDIATED_SqliteProcessStore_TableName_ValidatesSqlIdentifier_AndPreventsInjection()
    {
        // ARRANGE
        var serializer = new SystemTextJsonProcessStateSerializer<AuditState>(AuditJsonContext.Default.AuditState);
        string injectedTableName = "process_instances WHERE 1=0 UNION SELECT 'pwned', 'type', 1, 1, 1, 'corr', '{}', '2026-01-01', '2026-01-01', NULL --";

        // ACT & ASSERT: Constructor rejects malicious SQL injection payloads
        Action act = () =>
        {
            _ = new SqliteProcessStore<AuditState>(_connectionString, serializer, injectedTableName);
        };
        act.Should().Throw<ArgumentException>()
            .WithMessage("*Invalid table name*");
    }

    [Fact]
    public async Task REMEDIATED_SqliteProcessStore_UpdateExisting_UpdatesVersionColumn()
    {
        await InitializeDatabaseAsync();

        // ARRANGE
        var serializer = new SystemTextJsonProcessStateSerializer<AuditState>(AuditJsonContext.Default.AuditState);
        var store = new SqliteProcessStore<AuditState>(_connectionString, serializer, "process_instances");

        var pid = ProcessId.NewId();
        var initialInstance = ProcessInstance<AuditState>.Create(
            pid,
            ProcessType.From("Test"),
            ProcessVersion.From(1),
            CorrelationId.From("corr-1"),
            new AuditState("v1", 1),
            DateTimeOffset.UtcNow);

        // Insert initial instance (revision 1)
        var saveResult = await store.SaveAsync(initialInstance);
        saveResult.Should().Be(ProcessSaveResult.Success);

        // ACT: Advance and upgrade version from 1 to 2
        var updatedInstance = new ProcessInstance<AuditState>(
            id: initialInstance.Id,
            type: initialInstance.Type,
            version: ProcessVersion.From(2), // Version upgraded to 2
            status: ProcessStatus.Running,
            revision: initialInstance.Revision.Next(), // Revision 2
            correlationId: initialInstance.CorrelationId,
            createdAt: initialInstance.CreatedAt,
            updatedAt: DateTimeOffset.UtcNow,
            completedAt: null,
            state: new AuditState("v2", 2));

        var updateResult = await store.SaveAsync(updatedInstance);
        updateResult.Should().Be(ProcessSaveResult.Success);

        // Retrieve from database
        var retrieved = await store.GetByIdAsync(pid);

        // ASSERT: In the database, the Version column was correctly updated to 2
        retrieved.Should().NotBeNull();
        retrieved!.Version.Value.Should().Be(2);
    }

    [Fact]
    public void REMEDIATED_ProcessInstance_Advance_EnforcesTerminalStateInvariant_ThrowsInvalidProcessTransitionException()
    {
        // ARRANGE
        var instance = ProcessInstance<AuditState>.Create(
            ProcessId.NewId(),
            ProcessType.From("Test"),
            ProcessVersion.From(1),
            CorrelationId.From("corr-1"),
            new AuditState("initial", 0),
            DateTimeOffset.UtcNow);

        // Complete the process
        var completedInstance = instance.Advance(new AuditState("done", 1), ProcessStatus.Completed, DateTimeOffset.UtcNow);
        completedInstance.Status.Should().Be(ProcessStatus.Completed);

        // ACT: Attempt to transition from Completed back to Running
        Action act = () => completedInstance.Advance(new AuditState("reopened", 2), ProcessStatus.Running, DateTimeOffset.UtcNow);

        // ASSERT: Advance strictly enforces state machine invariant and throws InvalidProcessTransitionException
        act.Should().Throw<InvalidProcessTransitionException>()
            .WithMessage("*terminal status 'Completed'*");
    }

    [Fact]
    public async Task REMEDIATED_SystemTextJsonProcessStateSerializer_EnforcesMaxPayloadSize_PreventsMassivePayloadMemoryExhaustion()
    {
        await InitializeDatabaseAsync();

        // ARRANGE: Configure serializer with 100-byte maximum payload limit
        const int maxPayloadBytes = 100;
        var serializer = new SystemTextJsonProcessStateSerializer<AuditState>(
            AuditJsonContext.Default.AuditState,
            maxPayloadSizeBytes: maxPayloadBytes);

        var store = new SqliteProcessStore<AuditState>(_connectionString, serializer, "process_instances");

        // Create an adversarial massive payload (500-char string)
        var massivePayload = new string('A', 500);
        var massiveState = new AuditState(massivePayload, 999);
        var instance = ProcessInstance<AuditState>.Create(
            ProcessId.NewId(),
            ProcessType.From("AuditProcess"),
            ProcessVersion.From(1),
            CorrelationId.From("corr-massive"),
            massiveState,
            DateTimeOffset.UtcNow);

        // ACT & ASSERT: Saving state that exceeds maxPayloadBytes is rejected before DB persistence
        var actSave = async () => await store.SaveAsync(instance);
        (await actSave.Should().ThrowAsync<InvalidOperationException>())
            .WithMessage($"*Serialized state payload size*exceeds the configured maximum allowed size of {maxPayloadBytes} bytes.*");

        // ACT & ASSERT: Deserializing byte span that exceeds maxPayloadBytes is rejected before allocation/parsing
        var rawBytes = new byte[maxPayloadBytes + 50];
        Action actDeserialize = () => serializer.Deserialize(rawBytes);
        actDeserialize.Should().Throw<InvalidOperationException>()
            .WithMessage($"*Incoming state payload size ({rawBytes.Length} bytes) exceeds the configured maximum allowed size of {maxPayloadBytes} bytes.*");
    }
}
