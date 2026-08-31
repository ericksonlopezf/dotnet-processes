// Copyright © Erickson Lopez. MIT License.
using System;
using System.Text.Json.Serialization;
using System.Threading.Tasks;
using AwesomeAssertions;
using EricksonLopez.Processes.Abstractions;
using EricksonLopez.Processes.Storage.IntegrationTests.Fixtures;
using EricksonLopez.Processes.Storage.PostgreSql;
using EricksonLopez.Processes.SystemTextJson;
using Npgsql;
using Xunit;

namespace EricksonLopez.Processes.Storage.IntegrationTests;

public sealed record SampleOrderState(string CustomerId, decimal TotalAmount, bool IsDelivered) : IProcessState;

public sealed record Address(string Street, string City, string Country);
public sealed record OrderItem(string Sku, int Quantity, decimal UnitPrice);

public sealed record ComplexOrderState(
    string CustomerId,
    Address BillingAddress,
    Address ShippingAddress,
    System.Collections.Generic.Dictionary<string, string> Metadata,
    System.Collections.Generic.List<OrderItem> Items
) : IProcessState;

[JsonSerializable(typeof(SampleOrderState))]
[JsonSerializable(typeof(Address))]
[JsonSerializable(typeof(OrderItem))]
[JsonSerializable(typeof(ComplexOrderState))]
[JsonSerializable(typeof(ProcessId))]
[JsonSerializable(typeof(CorrelationId))]
[JsonSerializable(typeof(ProcessVersion))]
[JsonSerializable(typeof(Revision))]
internal sealed partial class IntegrationTestJsonContext : JsonSerializerContext
{
}

[Trait("Category", "Integration")]
public sealed class PostgreSqlProcessStoreIntegrationTests : IClassFixture<PostgreSqlFixture>
{
    private readonly PostgreSqlProcessStore<SampleOrderState> _store;
    private readonly SystemTextJsonProcessStateSerializer<SampleOrderState> _serializer;
    private readonly PostgreSqlFixture _fixture;

    public PostgreSqlProcessStoreIntegrationTests(PostgreSqlFixture fixture)
    {
        ArgumentNullException.ThrowIfNull(fixture);
        _fixture = fixture;
        _serializer = new SystemTextJsonProcessStateSerializer<SampleOrderState>(
            IntegrationTestJsonContext.Default.SampleOrderState);
        _store = new PostgreSqlProcessStore<SampleOrderState>(fixture.ConnectionString, _serializer);
    }

    [Fact]
    public async Task SaveAsync_InitialInsert_ShouldPersistInPostgreSqlAndReturnSuccess()
    {
        var id = ProcessId.NewId();
        var correlationId = CorrelationId.From("corr-pg-1");
        var state = new SampleOrderState("cust-100", 250.75m, false);
        var now = DateTimeOffset.UtcNow;

        var instance = ProcessInstance<SampleOrderState>.Create(
            id,
            ProcessType.From("order.pg"),
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
        loaded.Type.Value.Should().Be("order.pg");
        loaded.Version.Value.Should().Be(1);
        loaded.Status.Should().Be(ProcessStatus.Initialized);
        loaded.Revision.Should().Be(Revision.Initial);
        loaded.CorrelationId.Should().Be(correlationId);
        loaded.State.CustomerId.Should().Be("cust-100");
        loaded.State.TotalAmount.Should().Be(250.75m);
        loaded.State.IsDelivered.Should().BeFalse();
        loaded.CompletedAt.Should().BeNull();
    }

    [Fact]
    public async Task GetByCorrelationIdAsync_ShouldRetrieveMatchingInstanceFromPostgreSql()
    {
        var id = ProcessId.NewId();
        var correlationId = CorrelationId.From($"corr-lookup-{Guid.NewGuid()}");
        var state = new SampleOrderState("cust-lookup", 80.00m, false);

        var instance = ProcessInstance<SampleOrderState>.Create(
            id,
            ProcessType.From("order.pg.lookup"),
            ProcessVersion.Initial,
            correlationId,
            state,
            DateTimeOffset.UtcNow);

        await _store.SaveAsync(instance);

        var loaded = await _store.GetByCorrelationIdAsync(correlationId);

        loaded.Should().NotBeNull();
        loaded!.Id.Should().Be(id);
        loaded.State.CustomerId.Should().Be("cust-lookup");
    }

    [Fact]
    public async Task SaveAsync_SequentialAdvances_ShouldUpdateRevisionAndStateInPostgreSql()
    {
        var id = ProcessId.NewId();
        var correlationId = CorrelationId.From("corr-pg-seq");
        var initial = ProcessInstance<SampleOrderState>.Create(
            id,
            ProcessType.From("order.pg"),
            ProcessVersion.Initial,
            correlationId,
            new SampleOrderState("cust-200", 50m, false),
            DateTimeOffset.UtcNow);

        await _store.SaveAsync(initial);

        // Advance to Rev 2 (Running)
        var advanced1 = initial.Advance(
            new SampleOrderState("cust-200", 50m, false),
            ProcessStatus.Running,
            DateTimeOffset.UtcNow);

        var result1 = await _store.SaveAsync(advanced1);
        result1.Should().Be(ProcessSaveResult.Success);

        // Advance to Rev 3 (Completed)
        var completedTime = DateTimeOffset.UtcNow;
        var advanced2 = advanced1.Advance(
            new SampleOrderState("cust-200", 50m, true),
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
    public async Task SaveAsync_ConcurrentStaleRevision_ShouldReturnConcurrencyConflictInPostgreSql()
    {
        var id = ProcessId.NewId();
        var initial = ProcessInstance<SampleOrderState>.Create(
            id,
            ProcessType.From("order.pg"),
            ProcessVersion.Initial,
            CorrelationId.From("corr-pg-conflict"),
            new SampleOrderState("cust-300", 100m, false),
            DateTimeOffset.UtcNow);

        await _store.SaveAsync(initial);

        // Competitor 1 advances to Rev 2 and commits
        var advanced1 = initial.Advance(
            new SampleOrderState("cust-300", 150m, false),
            ProcessStatus.Running,
            DateTimeOffset.UtcNow);
        var result1 = await _store.SaveAsync(advanced1);
        result1.Should().Be(ProcessSaveResult.Success);

        // Competitor 2 tries to advance from Rev 1 to Rev 2 (stale base revision)
        var staleAdvance = initial.Advance(
            new SampleOrderState("cust-300", 200m, false),
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
    public async Task SaveAsync_NonExistentUpdate_ShouldReturnNotFoundInPostgreSql()
    {
        var id = ProcessId.NewId();
        var initial = ProcessInstance<SampleOrderState>.Create(
            id,
            ProcessType.From("order.pg"),
            ProcessVersion.Initial,
            CorrelationId.From("corr-pg-notfound"),
            new SampleOrderState("cust-400", 100m, false),
            DateTimeOffset.UtcNow);

        // Advance to Rev 2 without having saved Rev 1
        var unseededAdvance = initial.Advance(
            new SampleOrderState("cust-400", 100m, false),
            ProcessStatus.Running,
            DateTimeOffset.UtcNow);

        var result = await _store.SaveAsync(unseededAdvance);

        result.Should().Be(ProcessSaveResult.NotFound);
    }

    [Fact]
    public async Task SaveAsync_ShouldRecover_WhenDatabaseContainerIsRestarted()
    {
        var id = ProcessId.NewId();
        var correlationId = CorrelationId.From("corr-pg-restart");
        var state = new SampleOrderState("cust-restart", 500m, false);

        var initial = ProcessInstance<SampleOrderState>.Create(
            id,
            ProcessType.From("order.pg.restart"),
            ProcessVersion.Initial,
            correlationId,
            state,
            DateTimeOffset.UtcNow);

        await _store.SaveAsync(initial);

        // Simulate crash
        await _fixture.RestartAsync();

        // Create new store to ensure connections are established against the restarted container
        var newStore = new PostgreSqlProcessStore<SampleOrderState>(_fixture.ConnectionString, _serializer);

        var loaded = await newStore.GetByIdAsync(id);
        loaded.Should().NotBeNull();
        loaded!.Id.Should().Be(id);
        loaded.State.CustomerId.Should().Be("cust-restart");
    }

    [Fact]
    public async Task SaveAsync_WithComplexNestedState_ShouldPersistAndRetrieveCorrectly()
    {
        var complexSerializer = new SystemTextJsonProcessStateSerializer<ComplexOrderState>(
            IntegrationTestJsonContext.Default.ComplexOrderState);
        var store = new PostgreSqlProcessStore<ComplexOrderState>(_fixture.ConnectionString, complexSerializer);

        var id = ProcessId.NewId();
        var correlationId = CorrelationId.From("corr-pg-complex");

        var state = new ComplexOrderState(
            "complex-cust-1",
            new Address("123 Main St", "Metropolis", "USA"),
            new Address("456 Delivery Ave", "Gotham", "USA"),
            new System.Collections.Generic.Dictionary<string, string> { { "Priority", "High" }, { "Campaign", "SummerSale" } },
            new System.Collections.Generic.List<OrderItem>
            {
                new OrderItem("SKU-1", 2, 99.99m),
                new OrderItem("SKU-2", 1, 149.50m)
            });

        var instance = ProcessInstance<ComplexOrderState>.Create(
            id,
            ProcessType.From("order.pg.complex"),
            ProcessVersion.Initial,
            correlationId,
            state,
            DateTimeOffset.UtcNow);

        var saveResult = await store.SaveAsync(instance);
        saveResult.Should().Be(ProcessSaveResult.Success);

        var loaded = await store.GetByIdAsync(id);
        loaded.Should().NotBeNull();

        loaded!.State.CustomerId.Should().Be("complex-cust-1");
        loaded.State.BillingAddress.City.Should().Be("Metropolis");
        loaded.State.ShippingAddress.City.Should().Be("Gotham");
        loaded.State.Metadata.Should().ContainKey("Priority").WhoseValue.Should().Be("High");
        loaded.State.Items.Should().HaveCount(2);
        loaded.State.Items[1].Sku.Should().Be("SKU-2");
    }
}
