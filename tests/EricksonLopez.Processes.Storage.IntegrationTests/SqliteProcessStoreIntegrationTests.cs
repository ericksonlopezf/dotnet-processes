// Copyright © Erickson Lopez. MIT License.
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using AwesomeAssertions;
using EricksonLopez.Processes.Abstractions;
using EricksonLopez.Processes.Storage.IntegrationTests.Fixtures;
using EricksonLopez.Processes.Storage.Sqlite;
using EricksonLopez.Processes.SystemTextJson;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;

namespace EricksonLopez.Processes.Storage.IntegrationTests;

[Trait("Category", "Integration")]
public sealed class SqliteProcessStoreIntegrationTests : IClassFixture<SqliteFixture>
{
    private readonly SqliteProcessStore<SampleOrderState> _store;
    private readonly SystemTextJsonProcessStateSerializer<SampleOrderState> _serializer;
    private readonly SqliteFixture _fixture;

    public SqliteProcessStoreIntegrationTests(SqliteFixture fixture)
    {
        ArgumentNullException.ThrowIfNull(fixture);
        _fixture = fixture;
        _serializer = new SystemTextJsonProcessStateSerializer<SampleOrderState>(
            IntegrationTestJsonContext.Default.SampleOrderState);
        _store = new SqliteProcessStore<SampleOrderState>(fixture.ConnectionString, _serializer);
    }

    [Fact]
    public async Task SaveAsync_InitialInsert_ShouldPersistInSqliteAndReturnSuccess()
    {
        var id = ProcessId.NewId();
        var correlationId = CorrelationId.From("corr-sqlite-1");
        var state = new SampleOrderState("cust-sqlite-1", 149.99m, false);
        var now = DateTimeOffset.UtcNow;

        var instance = ProcessInstance<SampleOrderState>.Create(
            id,
            ProcessType.From("order.sqlite"),
            ProcessVersion.Initial,
            correlationId,
            state,
            now);

        var saveResult = await _store.SaveAsync(instance);

        saveResult.Should().Be(ProcessSaveResult.Success);

        var exists = await _store.ExistsAsync(id);
        exists.Should().BeTrue();

        var loaded = await _store.GetByIdAsync(id);
        loaded.Should().NotBeNull();
        loaded!.Id.Should().Be(id);
        loaded.Type.Value.Should().Be("order.sqlite");
        loaded.Version.Value.Should().Be(1);
        loaded.Status.Should().Be(ProcessStatus.Initialized);
        loaded.Revision.Should().Be(Revision.Initial);
        loaded.CorrelationId.Should().Be(correlationId);
        loaded.State.CustomerId.Should().Be("cust-sqlite-1");
        loaded.State.TotalAmount.Should().Be(149.99m);
        loaded.State.IsDelivered.Should().BeFalse();
        loaded.CompletedAt.Should().BeNull();
    }

    [Fact]
    public async Task GetByCorrelationIdAsync_ShouldRetrieveMatchingInstanceFromSqlite()
    {
        var id = ProcessId.NewId();
        var correlationId = CorrelationId.From($"corr-sqlite-{Guid.NewGuid()}");
        var state = new SampleOrderState("cust-sqlite-lookup", 89.50m, false);

        var instance = ProcessInstance<SampleOrderState>.Create(
            id,
            ProcessType.From("order.sqlite.lookup"),
            ProcessVersion.Initial,
            correlationId,
            state,
            DateTimeOffset.UtcNow);

        await _store.SaveAsync(instance);

        var loaded = await _store.GetByCorrelationIdAsync(correlationId);

        loaded.Should().NotBeNull();
        loaded!.Id.Should().Be(id);
        loaded.State.CustomerId.Should().Be("cust-sqlite-lookup");
    }

    [Fact]
    public async Task SaveAsync_SequentialAdvances_ShouldUpdateRevisionAndStateInSqlite()
    {
        var id = ProcessId.NewId();
        var correlationId = CorrelationId.From("corr-sqlite-seq");
        var initial = ProcessInstance<SampleOrderState>.Create(
            id,
            ProcessType.From("order.sqlite"),
            ProcessVersion.Initial,
            correlationId,
            new SampleOrderState("cust-sqlite-2", 60m, false),
            DateTimeOffset.UtcNow);

        await _store.SaveAsync(initial);

        // Advance to Rev 2 (Running)
        var advanced1 = initial.Advance(
            new SampleOrderState("cust-sqlite-2", 60m, false),
            ProcessStatus.Running,
            DateTimeOffset.UtcNow);

        var result1 = await _store.SaveAsync(advanced1);
        result1.Should().Be(ProcessSaveResult.Success);

        // Advance to Rev 3 (Completed)
        var completedTime = DateTimeOffset.UtcNow;
        var advanced2 = advanced1.Advance(
            new SampleOrderState("cust-sqlite-2", 60m, true),
            ProcessStatus.Completed,
            completedTime);

        var result2 = await _store.SaveAsync(advanced2);
        result2.Should().Be(ProcessSaveResult.Success);

        var finalLoaded = await _store.GetByIdAsync(id);
        finalLoaded.Should().NotBeNull();
        finalLoaded!.Revision.Value.Should().Be(3);
        finalLoaded.Status.Should().Be(ProcessStatus.Completed);
        finalLoaded.State.IsDelivered.Should().BeTrue();
        finalLoaded.CompletedAt.Should().NotBeNull();
    }

    [Fact]
    public async Task SaveAsync_ConcurrentStaleRevision_ShouldReturnConcurrencyConflictInSqlite()
    {
        var id = ProcessId.NewId();
        var initial = ProcessInstance<SampleOrderState>.Create(
            id,
            ProcessType.From("order.sqlite"),
            ProcessVersion.Initial,
            CorrelationId.From("corr-sqlite-conflict"),
            new SampleOrderState("cust-sqlite-3", 100m, false),
            DateTimeOffset.UtcNow);

        await _store.SaveAsync(initial);

        // Competitor 1 advances to Rev 2 and commits
        var advanced1 = initial.Advance(
            new SampleOrderState("cust-sqlite-3", 250m, false),
            ProcessStatus.Running,
            DateTimeOffset.UtcNow);
        var result1 = await _store.SaveAsync(advanced1);
        result1.Should().Be(ProcessSaveResult.Success);

        // Competitor 2 tries to advance from Rev 1 to Rev 2 (stale base revision)
        var staleAdvance = initial.Advance(
            new SampleOrderState("cust-sqlite-3", 300m, false),
            ProcessStatus.Running,
            DateTimeOffset.UtcNow);
        var conflictResult = await _store.SaveAsync(staleAdvance);

        conflictResult.Should().Be(ProcessSaveResult.ConcurrencyConflict);

        // Store state should retain competitor 1's commit (Amount == 250)
        var current = await _store.GetByIdAsync(id);
        current!.State.TotalAmount.Should().Be(250m);
        current.Revision.Value.Should().Be(2);
    }

    [Fact]
    public async Task SaveAsync_NonExistentUpdate_ShouldReturnNotFoundInSqlite()
    {
        var id = ProcessId.NewId();
        var initial = ProcessInstance<SampleOrderState>.Create(
            id,
            ProcessType.From("order.sqlite"),
            ProcessVersion.Initial,
            CorrelationId.From("corr-sqlite-notfound"),
            new SampleOrderState("cust-sqlite-4", 100m, false),
            DateTimeOffset.UtcNow);

        // Advance to Rev 2 without having saved Rev 1
        var unseededAdvance = initial.Advance(
            new SampleOrderState("cust-sqlite-4", 100m, false),
            ProcessStatus.Running,
            DateTimeOffset.UtcNow);

        var result = await _store.SaveAsync(unseededAdvance);

        result.Should().Be(ProcessSaveResult.NotFound);
    }

    [Fact]
    public async Task SaveAsync_WithComplexNestedState_ShouldPersistAndRetrieveCorrectly()
    {
        var complexSerializer = new SystemTextJsonProcessStateSerializer<ComplexOrderState>(
            IntegrationTestJsonContext.Default.ComplexOrderState);
        var store = new SqliteProcessStore<ComplexOrderState>(_fixture.ConnectionString, complexSerializer);

        var id = ProcessId.NewId();
        var correlationId = CorrelationId.From("corr-sqlite-complex");

        var state = new ComplexOrderState(
            "complex-cust-sqlite-1",
            new Address("789 Pine Rd", "Austin", "USA"),
            new Address("101 Palm Dr", "Miami", "USA"),
            new Dictionary<string, string> { { "Tier", "Gold" } },
            new List<OrderItem>
            {
                new OrderItem("SKU-SQLITE-1", 3, 45.00m)
            });

        var instance = ProcessInstance<ComplexOrderState>.Create(
            id,
            ProcessType.From("order.sqlite.complex"),
            ProcessVersion.Initial,
            correlationId,
            state,
            DateTimeOffset.UtcNow);

        var saveResult = await store.SaveAsync(instance);
        saveResult.Should().Be(ProcessSaveResult.Success);

        var loaded = await store.GetByIdAsync(id);
        loaded.Should().NotBeNull();

        loaded!.State.CustomerId.Should().Be("complex-cust-sqlite-1");
        loaded.State.BillingAddress.City.Should().Be("Austin");
        loaded.State.ShippingAddress.City.Should().Be("Miami");
        loaded.State.Metadata.Should().ContainKey("Tier").WhoseValue.Should().Be("Gold");
        loaded.State.Items.Should().HaveCount(1);
    }

    [Fact]
    public void Constructor_InvalidArguments_ShouldThrowExpectedExceptions()
    {
        var actNullConn = () => new SqliteProcessStore<SampleOrderState>(null!, _serializer);
        actNullConn.Should().Throw<ArgumentException>();

        var actEmptyConn = () => new SqliteProcessStore<SampleOrderState>("   ", _serializer);
        actEmptyConn.Should().Throw<ArgumentException>();

        var actNullTable = () => new SqliteProcessStore<SampleOrderState>(_fixture.ConnectionString, _serializer, tableName: null!);
        actNullTable.Should().Throw<ArgumentException>();

        var actEmptyTable = () => new SqliteProcessStore<SampleOrderState>(_fixture.ConnectionString, _serializer, tableName: "  ");
        actEmptyTable.Should().Throw<ArgumentException>();

        var actNullSerializer = () => new SqliteProcessStore<SampleOrderState>(_fixture.ConnectionString, null!);
        actNullSerializer.Should().Throw<ArgumentNullException>();
    }

    [Fact]
    public async Task SaveAsync_NullInstance_ShouldThrowArgumentNullException()
    {
        var act = async () => await _store.SaveAsync(null!);
        await act.Should().ThrowAsync<ArgumentNullException>();
    }

    [Fact]
    public async Task SaveAsync_DuplicateInitialInsert_ShouldReturnConcurrencyConflict()
    {
        var id = ProcessId.NewId();
        var correlationId = CorrelationId.From("corr-sqlite-dup-initial");
        var instance = ProcessInstance<SampleOrderState>.Create(
            id,
            ProcessType.From("order.sqlite.dup"),
            ProcessVersion.Initial,
            correlationId,
            new SampleOrderState("cust-dup", 50m, false),
            DateTimeOffset.UtcNow);

        var firstResult = await _store.SaveAsync(instance);
        firstResult.Should().Be(ProcessSaveResult.Success);

        var duplicateResult = await _store.SaveAsync(instance);
        duplicateResult.Should().Be(ProcessSaveResult.ConcurrencyConflict);
    }

    [Fact]
    public async Task GetByIdAsync_WhenNonExistent_ShouldReturnNull()
    {
        var nonExistentId = ProcessId.NewId();
        var result = await _store.GetByIdAsync(nonExistentId);
        result.Should().BeNull();
    }

    [Fact]
    public async Task GetByCorrelationIdAsync_WhenNonExistent_ShouldReturnNull()
    {
        var nonExistentCorr = CorrelationId.From($"corr-sqlite-none-{Guid.NewGuid():N}");
        var result = await _store.GetByCorrelationIdAsync(nonExistentCorr);
        result.Should().BeNull();
    }

    [Fact]
    public async Task ExistsAsync_WhenNonExistent_ShouldReturnFalse()
    {
        var nonExistentId = ProcessId.NewId();
        var exists = await _store.ExistsAsync(nonExistentId);
        exists.Should().BeFalse();
    }

    [Fact]
    public async Task SaveAsync_WithCustomTableName_ShouldPersistAndQueryCorrectly()
    {
        const string customTable = "CustomSqliteProcessInstances";
        await using (var conn = new Microsoft.Data.Sqlite.SqliteConnection(_fixture.ConnectionString))
        {
            await conn.OpenAsync();
            const string ddl = $"""
                CREATE TABLE IF NOT EXISTS {customTable} (
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
            await using var cmd = new Microsoft.Data.Sqlite.SqliteCommand(ddl, conn);
            await cmd.ExecuteNonQueryAsync();
        }

        var customStore = new SqliteProcessStore<SampleOrderState>(_fixture.ConnectionString, _serializer, tableName: customTable);
        var id = ProcessId.NewId();
        var corr = CorrelationId.From("corr-custom-table");
        var instance = ProcessInstance<SampleOrderState>.Create(
            id,
            ProcessType.From("order.custom"),
            ProcessVersion.Initial,
            corr,
            new SampleOrderState("cust-custom", 99m, false),
            DateTimeOffset.UtcNow);

        var saveResult = await customStore.SaveAsync(instance);
        saveResult.Should().Be(ProcessSaveResult.Success);

        var loaded = await customStore.GetByIdAsync(id);
        loaded.Should().NotBeNull();
        loaded!.Id.Should().Be(id);

        var loadedCorr = await customStore.GetByCorrelationIdAsync(corr);
        loadedCorr.Should().NotBeNull();
        loadedCorr!.Id.Should().Be(id);

        var exists = await customStore.ExistsAsync(id);
        exists.Should().BeTrue();
    }

    [Fact]
    public void AddSqliteProcessStore_DI_Registration_ShouldResolveStoreSuccessfully()
    {
        var services = new ServiceCollection();
        services.AddSingleton<IProcessStateSerializer<SampleOrderState>>(_serializer);
        services.AddSingleton(typeof(ILogger<>), typeof(NullLogger<>));

        services.AddSqliteProcessStore<SampleOrderState>(_fixture.ConnectionString, "ProcessInstances");

        var provider = services.BuildServiceProvider();
        var resolvedStore = provider.GetService<IProcessStore<SampleOrderState>>();

        resolvedStore.Should().NotBeNull();
        resolvedStore.Should().BeOfType<SqliteProcessStore<SampleOrderState>>();
    }

    [Fact]
    public void AddSqliteProcessStore_InvalidArguments_ShouldThrowExpectedExceptions()
    {
        var services = new ServiceCollection();

        var actNullServices = () => ProcessSqliteServiceCollectionExtensions.AddSqliteProcessStore<SampleOrderState>(null!, _fixture.ConnectionString);
        actNullServices.Should().Throw<ArgumentNullException>();

        var actNullConn = () => services.AddSqliteProcessStore<SampleOrderState>(null!);
        actNullConn.Should().Throw<ArgumentException>();

        var actEmptyConn = () => services.AddSqliteProcessStore<SampleOrderState>("   ");
        actEmptyConn.Should().Throw<ArgumentException>();
    }
}
