// Copyright © Erickson Lopez. MIT License.
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using AwesomeAssertions;
using EricksonLopez.Processes.Abstractions;
using EricksonLopez.Processes.Storage.IntegrationTests.Fixtures;
using EricksonLopez.Processes.Storage.MariaDb;
using EricksonLopez.Processes.SystemTextJson;
using Xunit;

namespace EricksonLopez.Processes.Storage.IntegrationTests;

[Trait("Category", "Integration")]
public sealed class MariaDbProcessStoreIntegrationTests : IClassFixture<MariaDbFixture>
{
    private readonly MariaDbProcessStore<SampleOrderState> _store;
    private readonly SystemTextJsonProcessStateSerializer<SampleOrderState> _serializer;
    private readonly MariaDbFixture _fixture;

    public MariaDbProcessStoreIntegrationTests(MariaDbFixture fixture)
    {
        ArgumentNullException.ThrowIfNull(fixture);
        _fixture = fixture;
        _serializer = new SystemTextJsonProcessStateSerializer<SampleOrderState>(
            IntegrationTestJsonContext.Default.SampleOrderState);
        _store = new MariaDbProcessStore<SampleOrderState>(fixture.ConnectionString, _serializer);
    }

    [Fact]
    public async Task SaveAsync_InitialInsert_ShouldPersistInMariaDbAndReturnSuccess()
    {
        var id = ProcessId.NewId();
        var correlationId = CorrelationId.From("corr-mariadb-1");
        var state = new SampleOrderState("cust-mariadb-1", 219.99m, false);
        var now = DateTimeOffset.UtcNow;

        var instance = ProcessInstance<SampleOrderState>.Create(
            id,
            ProcessType.From("order.mariadb"),
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
        loaded.Type.Value.Should().Be("order.mariadb");
        loaded.Version.Value.Should().Be(1);
        loaded.Status.Should().Be(ProcessStatus.Initialized);
        loaded.Revision.Should().Be(Revision.Initial);
        loaded.CorrelationId.Should().Be(correlationId);
        loaded.State.CustomerId.Should().Be("cust-mariadb-1");
        loaded.State.TotalAmount.Should().Be(219.99m);
        loaded.State.IsDelivered.Should().BeFalse();
        loaded.CompletedAt.Should().BeNull();
    }

    [Fact]
    public async Task GetByCorrelationIdAsync_ShouldRetrieveMatchingInstanceFromMariaDb()
    {
        var id = ProcessId.NewId();
        var correlationId = CorrelationId.From($"corr-mariadb-{Guid.NewGuid()}");
        var state = new SampleOrderState("cust-mariadb-lookup", 95.00m, false);

        var instance = ProcessInstance<SampleOrderState>.Create(
            id,
            ProcessType.From("order.mariadb.lookup"),
            ProcessVersion.Initial,
            correlationId,
            state,
            DateTimeOffset.UtcNow);

        await _store.SaveAsync(instance);

        var loaded = await _store.GetByCorrelationIdAsync(correlationId);

        loaded.Should().NotBeNull();
        loaded!.Id.Should().Be(id);
        loaded.State.CustomerId.Should().Be("cust-mariadb-lookup");
    }

    [Fact]
    public async Task SaveAsync_SequentialAdvances_ShouldUpdateRevisionAndStateInMariaDb()
    {
        var id = ProcessId.NewId();
        var correlationId = CorrelationId.From("corr-mariadb-seq");
        var initial = ProcessInstance<SampleOrderState>.Create(
            id,
            ProcessType.From("order.mariadb"),
            ProcessVersion.Initial,
            correlationId,
            new SampleOrderState("cust-mariadb-2", 55m, false),
            DateTimeOffset.UtcNow);

        await _store.SaveAsync(initial);

        // Advance to Rev 2 (Running)
        var advanced1 = initial.Advance(
            new SampleOrderState("cust-mariadb-2", 55m, false),
            ProcessStatus.Running,
            DateTimeOffset.UtcNow);

        var result1 = await _store.SaveAsync(advanced1);
        result1.Should().Be(ProcessSaveResult.Success);

        // Advance to Rev 3 (Completed)
        var completedTime = DateTimeOffset.UtcNow;
        var advanced2 = advanced1.Advance(
            new SampleOrderState("cust-mariadb-2", 55m, true),
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
    public async Task SaveAsync_ConcurrentStaleRevision_ShouldReturnConcurrencyConflictInMariaDb()
    {
        var id = ProcessId.NewId();
        var initial = ProcessInstance<SampleOrderState>.Create(
            id,
            ProcessType.From("order.mariadb"),
            ProcessVersion.Initial,
            CorrelationId.From("corr-mariadb-conflict"),
            new SampleOrderState("cust-mariadb-3", 100m, false),
            DateTimeOffset.UtcNow);

        await _store.SaveAsync(initial);

        // Competitor 1 advances to Rev 2 and commits
        var advanced1 = initial.Advance(
            new SampleOrderState("cust-mariadb-3", 175m, false),
            ProcessStatus.Running,
            DateTimeOffset.UtcNow);
        var result1 = await _store.SaveAsync(advanced1);
        result1.Should().Be(ProcessSaveResult.Success);

        // Competitor 2 tries to advance from Rev 1 to Rev 2 (stale base revision)
        var staleAdvance = initial.Advance(
            new SampleOrderState("cust-mariadb-3", 220m, false),
            ProcessStatus.Running,
            DateTimeOffset.UtcNow);
        var conflictResult = await _store.SaveAsync(staleAdvance);

        conflictResult.Should().Be(ProcessSaveResult.ConcurrencyConflict);

        // Store state should retain competitor 1's commit (Amount == 175)
        var current = await _store.GetByIdAsync(id);
        current!.State.TotalAmount.Should().Be(175m);
        current.Revision.Value.Should().Be(2);
    }

    [Fact]
    public async Task SaveAsync_NonExistentUpdate_ShouldReturnNotFoundInMariaDb()
    {
        var id = ProcessId.NewId();
        var initial = ProcessInstance<SampleOrderState>.Create(
            id,
            ProcessType.From("order.mariadb"),
            ProcessVersion.Initial,
            CorrelationId.From("corr-mariadb-notfound"),
            new SampleOrderState("cust-mariadb-4", 100m, false),
            DateTimeOffset.UtcNow);

        // Advance to Rev 2 without having saved Rev 1
        var unseededAdvance = initial.Advance(
            new SampleOrderState("cust-mariadb-4", 100m, false),
            ProcessStatus.Running,
            DateTimeOffset.UtcNow);

        var result = await _store.SaveAsync(unseededAdvance);

        result.Should().Be(ProcessSaveResult.NotFound);
    }

    [Fact]
    public async Task SaveAsync_ShouldRecover_WhenDatabaseContainerIsRestarted()
    {
        var id = ProcessId.NewId();
        var correlationId = CorrelationId.From("corr-mariadb-restart");
        var state = new SampleOrderState("cust-restart-mariadb", 500m, false);

        var initial = ProcessInstance<SampleOrderState>.Create(
            id,
            ProcessType.From("order.mariadb.restart"),
            ProcessVersion.Initial,
            correlationId,
            state,
            DateTimeOffset.UtcNow);

        await _store.SaveAsync(initial);

        // Simulate crash
        await _fixture.RestartAsync();

        // Create new store against restarted container
        var newStore = new MariaDbProcessStore<SampleOrderState>(_fixture.ConnectionString, _serializer);

        var loaded = await newStore.GetByIdAsync(id);
        loaded.Should().NotBeNull();
        loaded!.Id.Should().Be(id);
        loaded.State.CustomerId.Should().Be("cust-restart-mariadb");
    }

    [Fact]
    public async Task SaveAsync_WithComplexNestedState_ShouldPersistAndRetrieveCorrectly()
    {
        var complexSerializer = new SystemTextJsonProcessStateSerializer<ComplexOrderState>(
            IntegrationTestJsonContext.Default.ComplexOrderState);
        var store = new MariaDbProcessStore<ComplexOrderState>(_fixture.ConnectionString, complexSerializer);

        var id = ProcessId.NewId();
        var correlationId = CorrelationId.From("corr-mariadb-complex");

        var state = new ComplexOrderState(
            "complex-cust-mariadb-1",
            new Address("300 Sea Ave", "San Diego", "USA"),
            new Address("400 Bay Dr", "San Diego", "USA"),
            new Dictionary<string, string> { { "Region", "West" } },
            new List<OrderItem>
            {
                new OrderItem("SKU-MARIADB-1", 2, 75.00m)
            });

        var instance = ProcessInstance<ComplexOrderState>.Create(
            id,
            ProcessType.From("order.mariadb.complex"),
            ProcessVersion.Initial,
            correlationId,
            state,
            DateTimeOffset.UtcNow);

        var saveResult = await store.SaveAsync(instance);
        saveResult.Should().Be(ProcessSaveResult.Success);

        var loaded = await store.GetByIdAsync(id);
        loaded.Should().NotBeNull();

        loaded!.State.CustomerId.Should().Be("complex-cust-mariadb-1");
        loaded.State.BillingAddress.City.Should().Be("San Diego");
        loaded.State.ShippingAddress.City.Should().Be("San Diego");
        loaded.State.Metadata.Should().ContainKey("Region").WhoseValue.Should().Be("West");
        loaded.State.Items.Should().HaveCount(1);
    }
}
