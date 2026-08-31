// Copyright © Erickson Lopez. MIT License.
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using AwesomeAssertions;
using EricksonLopez.Processes.Abstractions;
using EricksonLopez.Processes.Storage.IntegrationTests.Fixtures;
using EricksonLopez.Processes.Storage.MySql;
using EricksonLopez.Processes.SystemTextJson;
using Xunit;

namespace EricksonLopez.Processes.Storage.IntegrationTests;

[Trait("Category", "Integration")]
public sealed class MySqlProcessStoreIntegrationTests : IClassFixture<MySqlFixture>
{
    private readonly MySqlProcessStore<SampleOrderState> _store;
    private readonly SystemTextJsonProcessStateSerializer<SampleOrderState> _serializer;
    private readonly MySqlFixture _fixture;

    public MySqlProcessStoreIntegrationTests(MySqlFixture fixture)
    {
        ArgumentNullException.ThrowIfNull(fixture);
        _fixture = fixture;
        _serializer = new SystemTextJsonProcessStateSerializer<SampleOrderState>(
            IntegrationTestJsonContext.Default.SampleOrderState);
        _store = new MySqlProcessStore<SampleOrderState>(fixture.ConnectionString, _serializer);
    }

    [Fact]
    public async Task SaveAsync_InitialInsert_ShouldPersistInMySqlAndReturnSuccess()
    {
        var id = ProcessId.NewId();
        var correlationId = CorrelationId.From("corr-mysql-1");
        var state = new SampleOrderState("cust-mysql-1", 199.99m, false);
        var now = DateTimeOffset.UtcNow;

        var instance = ProcessInstance<SampleOrderState>.Create(
            id,
            ProcessType.From("order.mysql"),
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
        loaded.Type.Value.Should().Be("order.mysql");
        loaded.Version.Value.Should().Be(1);
        loaded.Status.Should().Be(ProcessStatus.Initialized);
        loaded.Revision.Should().Be(Revision.Initial);
        loaded.CorrelationId.Should().Be(correlationId);
        loaded.State.CustomerId.Should().Be("cust-mysql-1");
        loaded.State.TotalAmount.Should().Be(199.99m);
        loaded.State.IsDelivered.Should().BeFalse();
        loaded.CompletedAt.Should().BeNull();
    }

    [Fact]
    public async Task GetByCorrelationIdAsync_ShouldRetrieveMatchingInstanceFromMySql()
    {
        var id = ProcessId.NewId();
        var correlationId = CorrelationId.From($"corr-mysql-{Guid.NewGuid()}");
        var state = new SampleOrderState("cust-mysql-lookup", 80.00m, false);

        var instance = ProcessInstance<SampleOrderState>.Create(
            id,
            ProcessType.From("order.mysql.lookup"),
            ProcessVersion.Initial,
            correlationId,
            state,
            DateTimeOffset.UtcNow);

        await _store.SaveAsync(instance);

        var loaded = await _store.GetByCorrelationIdAsync(correlationId);

        loaded.Should().NotBeNull();
        loaded!.Id.Should().Be(id);
        loaded.State.CustomerId.Should().Be("cust-mysql-lookup");
    }

    [Fact]
    public async Task SaveAsync_SequentialAdvances_ShouldUpdateRevisionAndStateInMySql()
    {
        var id = ProcessId.NewId();
        var correlationId = CorrelationId.From("corr-mysql-seq");
        var initial = ProcessInstance<SampleOrderState>.Create(
            id,
            ProcessType.From("order.mysql"),
            ProcessVersion.Initial,
            correlationId,
            new SampleOrderState("cust-mysql-2", 50m, false),
            DateTimeOffset.UtcNow);

        await _store.SaveAsync(initial);

        // Advance to Rev 2 (Running)
        var advanced1 = initial.Advance(
            new SampleOrderState("cust-mysql-2", 50m, false),
            ProcessStatus.Running,
            DateTimeOffset.UtcNow);

        var result1 = await _store.SaveAsync(advanced1);
        result1.Should().Be(ProcessSaveResult.Success);

        // Advance to Rev 3 (Completed)
        var completedTime = DateTimeOffset.UtcNow;
        var advanced2 = advanced1.Advance(
            new SampleOrderState("cust-mysql-2", 50m, true),
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
    public async Task SaveAsync_ConcurrentStaleRevision_ShouldReturnConcurrencyConflictInMySql()
    {
        var id = ProcessId.NewId();
        var initial = ProcessInstance<SampleOrderState>.Create(
            id,
            ProcessType.From("order.mysql"),
            ProcessVersion.Initial,
            CorrelationId.From("corr-mysql-conflict"),
            new SampleOrderState("cust-mysql-3", 100m, false),
            DateTimeOffset.UtcNow);

        await _store.SaveAsync(initial);

        // Competitor 1 advances to Rev 2 and commits
        var advanced1 = initial.Advance(
            new SampleOrderState("cust-mysql-3", 150m, false),
            ProcessStatus.Running,
            DateTimeOffset.UtcNow);
        var result1 = await _store.SaveAsync(advanced1);
        result1.Should().Be(ProcessSaveResult.Success);

        // Competitor 2 tries to advance from Rev 1 to Rev 2 (stale base revision)
        var staleAdvance = initial.Advance(
            new SampleOrderState("cust-mysql-3", 200m, false),
            ProcessStatus.Running,
            DateTimeOffset.UtcNow);
        var conflictResult = await _store.SaveAsync(staleAdvance);

        conflictResult.Should().Be(ProcessSaveResult.ConcurrencyConflict);

        // Store state should retain competitor 1's commit (Amount == 150)
        var current = await _store.GetByIdAsync(id);
        current!.State.TotalAmount.Should().Be(150m);
        current.Revision.Value.Should().Be(2);
    }

    [Fact]
    public async Task SaveAsync_NonExistentUpdate_ShouldReturnNotFoundInMySql()
    {
        var id = ProcessId.NewId();
        var initial = ProcessInstance<SampleOrderState>.Create(
            id,
            ProcessType.From("order.mysql"),
            ProcessVersion.Initial,
            CorrelationId.From("corr-mysql-notfound"),
            new SampleOrderState("cust-mysql-4", 100m, false),
            DateTimeOffset.UtcNow);

        // Advance to Rev 2 without having saved Rev 1
        var unseededAdvance = initial.Advance(
            new SampleOrderState("cust-mysql-4", 100m, false),
            ProcessStatus.Running,
            DateTimeOffset.UtcNow);

        var result = await _store.SaveAsync(unseededAdvance);

        result.Should().Be(ProcessSaveResult.NotFound);
    }

    [Fact]
    public async Task SaveAsync_ShouldRecover_WhenDatabaseContainerIsRestarted()
    {
        var id = ProcessId.NewId();
        var correlationId = CorrelationId.From("corr-mysql-restart");
        var state = new SampleOrderState("cust-restart-mysql", 500m, false);

        var initial = ProcessInstance<SampleOrderState>.Create(
            id,
            ProcessType.From("order.mysql.restart"),
            ProcessVersion.Initial,
            correlationId,
            state,
            DateTimeOffset.UtcNow);

        await _store.SaveAsync(initial);

        // Simulate crash
        await _fixture.RestartAsync();

        // Create new store against restarted container
        var newStore = new MySqlProcessStore<SampleOrderState>(_fixture.ConnectionString, _serializer);

        var loaded = await newStore.GetByIdAsync(id);
        loaded.Should().NotBeNull();
        loaded!.Id.Should().Be(id);
        loaded.State.CustomerId.Should().Be("cust-restart-mysql");
    }

    [Fact]
    public async Task SaveAsync_WithComplexNestedState_ShouldPersistAndRetrieveCorrectly()
    {
        var complexSerializer = new SystemTextJsonProcessStateSerializer<ComplexOrderState>(
            IntegrationTestJsonContext.Default.ComplexOrderState);
        var store = new MySqlProcessStore<ComplexOrderState>(_fixture.ConnectionString, complexSerializer);

        var id = ProcessId.NewId();
        var correlationId = CorrelationId.From("corr-mysql-complex");

        var state = new ComplexOrderState(
            "complex-cust-mysql-1",
            new Address("100 Market St", "San Francisco", "USA"),
            new Address("200 Mission St", "San Francisco", "USA"),
            new Dictionary<string, string> { { "Priority", "Urgent" } },
            new List<OrderItem>
            {
                new OrderItem("SKU-MYSQL-1", 1, 99.00m)
            });

        var instance = ProcessInstance<ComplexOrderState>.Create(
            id,
            ProcessType.From("order.mysql.complex"),
            ProcessVersion.Initial,
            correlationId,
            state,
            DateTimeOffset.UtcNow);

        var saveResult = await store.SaveAsync(instance);
        saveResult.Should().Be(ProcessSaveResult.Success);

        var loaded = await store.GetByIdAsync(id);
        loaded.Should().NotBeNull();

        loaded!.State.CustomerId.Should().Be("complex-cust-mysql-1");
        loaded.State.BillingAddress.City.Should().Be("San Francisco");
        loaded.State.ShippingAddress.City.Should().Be("San Francisco");
        loaded.State.Metadata.Should().ContainKey("Priority").WhoseValue.Should().Be("Urgent");
        loaded.State.Items.Should().HaveCount(1);
    }
}
