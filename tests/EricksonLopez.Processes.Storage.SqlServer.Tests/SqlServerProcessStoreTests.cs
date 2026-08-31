// Copyright © Erickson Lopez. MIT License.
using System;
using System.Collections.Generic;
using System.Data;
using System.Text.Json.Serialization;
using System.Threading;
using System.Threading.Tasks;
using AwesomeAssertions;
using EricksonLopez.Processes.Abstractions;
using EricksonLopez.Processes.Storage.SqlServer;
using EricksonLopez.Processes.SystemTextJson;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;

namespace EricksonLopez.Processes.Storage.SqlServer.Tests;

public sealed record SampleOrderState(string CustomerId, decimal TotalAmount, bool IsDelivered) : IProcessState;

public sealed record Address(string Street, string City, string Country);
public sealed record OrderItem(string Sku, int Quantity, decimal UnitPrice);

public sealed record ComplexOrderState(
    string CustomerId,
    Address BillingAddress,
    Address ShippingAddress,
    Dictionary<string, string> Metadata,
    List<OrderItem> Items
) : IProcessState;

[JsonSerializable(typeof(SampleOrderState))]
[JsonSerializable(typeof(Address))]
[JsonSerializable(typeof(OrderItem))]
[JsonSerializable(typeof(ComplexOrderState))]
[JsonSerializable(typeof(ProcessId))]
[JsonSerializable(typeof(CorrelationId))]
[JsonSerializable(typeof(ProcessVersion))]
[JsonSerializable(typeof(Revision))]
internal sealed partial class SqlServerTestJsonContext : JsonSerializerContext
{
}

[Trait("Category", "Integration")]
public sealed class SqlServerProcessStoreTests : IClassFixture<MsSqlFixture>
{
    private readonly SqlServerProcessStore<SampleOrderState> _store;
    private readonly SystemTextJsonProcessStateSerializer<SampleOrderState> _serializer;
    private readonly MsSqlFixture _fixture;

    public SqlServerProcessStoreTests(MsSqlFixture fixture)
    {
        ArgumentNullException.ThrowIfNull(fixture);
        _fixture = fixture;
        _serializer = new SystemTextJsonProcessStateSerializer<SampleOrderState>(
            SqlServerTestJsonContext.Default.SampleOrderState);
        _store = new SqlServerProcessStore<SampleOrderState>(fixture.ConnectionString, _serializer);
    }

    [Fact]
    public async Task SaveAsync_InitialInsert_ShouldPersistInSqlServerAndReturnSuccess()
    {
        var id = ProcessId.NewId();
        var correlationId = CorrelationId.From("corr-sql-1");
        var state = new SampleOrderState("cust-sql-1", 149.99m, false);
        var now = DateTimeOffset.UtcNow;

        var instance = ProcessInstance<SampleOrderState>.Create(
            id,
            ProcessType.From("order.sql"),
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
        loaded.Type.Value.Should().Be("order.sql");
        loaded.Version.Value.Should().Be(1);
        loaded.Status.Should().Be(ProcessStatus.Initialized);
        loaded.Revision.Should().Be(Revision.Initial);
        loaded.CorrelationId.Should().Be(correlationId);
        loaded.CreatedAt.ToUniversalTime().Should().BeCloseTo(now.ToUniversalTime(), TimeSpan.FromMilliseconds(10));
        loaded.UpdatedAt.ToUniversalTime().Should().BeCloseTo(now.ToUniversalTime(), TimeSpan.FromMilliseconds(10));
        loaded.State.CustomerId.Should().Be("cust-sql-1");
        loaded.State.TotalAmount.Should().Be(149.99m);
        loaded.State.IsDelivered.Should().BeFalse();
        loaded.CompletedAt.Should().BeNull();
    }

    [Fact]
    public async Task GetByCorrelationIdAsync_ShouldRetrieveMatchingInstanceFromSqlServer()
    {
        var id = ProcessId.NewId();
        var correlationId = CorrelationId.From($"corr-sql-{Guid.NewGuid()}");
        var state = new SampleOrderState("cust-sql-lookup", 89.50m, false);
        var now = DateTimeOffset.UtcNow;

        var instance = ProcessInstance<SampleOrderState>.Create(
            id,
            ProcessType.From("order.sql.lookup"),
            ProcessVersion.Initial,
            correlationId,
            state,
            now);

        await _store.SaveAsync(instance);

        var loaded = await _store.GetByCorrelationIdAsync(correlationId);

        loaded.Should().NotBeNull();
        loaded!.Id.Should().Be(id);
        loaded.CreatedAt.ToUniversalTime().Should().BeCloseTo(now.ToUniversalTime(), TimeSpan.FromMilliseconds(10));
        loaded.UpdatedAt.ToUniversalTime().Should().BeCloseTo(now.ToUniversalTime(), TimeSpan.FromMilliseconds(10));
        loaded.State.CustomerId.Should().Be("cust-sql-lookup");
    }

    [Fact]
    public async Task SaveAsync_SequentialAdvances_ShouldUpdateRevisionAndStateInSqlServer()
    {
        var id = ProcessId.NewId();
        var correlationId = CorrelationId.From("corr-sql-seq");
        var initialTime = DateTimeOffset.UtcNow;
        var initial = ProcessInstance<SampleOrderState>.Create(
            id,
            ProcessType.From("order.sql"),
            ProcessVersion.Initial,
            correlationId,
            new SampleOrderState("cust-sql-2", 60m, false),
            initialTime);

        await _store.SaveAsync(initial);

        // Advance to Rev 2 (Running)
        var runningTime = initialTime.AddSeconds(5);
        var advanced1 = initial.Advance(
            new SampleOrderState("cust-sql-2", 60m, false),
            ProcessStatus.Running,
            runningTime);

        var result1 = await _store.SaveAsync(advanced1);
        result1.Should().Be(ProcessSaveResult.Success);

        // Advance to Rev 3 (Completed)
        var completedTime = initialTime.AddSeconds(10);
        var advanced2 = advanced1.Advance(
            new SampleOrderState("cust-sql-2", 60m, true),
            ProcessStatus.Completed,
            completedTime);

        var result2 = await _store.SaveAsync(advanced2);
        result2.Should().Be(ProcessSaveResult.Success);

        var finalLoaded = await _store.GetByIdAsync(id);
        finalLoaded.Should().NotBeNull();
        finalLoaded!.Revision.Value.Should().Be(3);
        finalLoaded.Status.Should().Be(ProcessStatus.Completed);
        finalLoaded.State.IsDelivered.Should().BeTrue();
        finalLoaded.CreatedAt.ToUniversalTime().Should().BeCloseTo(initialTime.ToUniversalTime(), TimeSpan.FromMilliseconds(10));
        finalLoaded.UpdatedAt.ToUniversalTime().Should().BeCloseTo(completedTime.ToUniversalTime(), TimeSpan.FromMilliseconds(10));
        finalLoaded.CompletedAt.Should().NotBeNull();
        finalLoaded.CompletedAt!.Value.ToUniversalTime().Should().BeCloseTo(completedTime.ToUniversalTime(), TimeSpan.FromMilliseconds(10));

        var loadedByCorr = await _store.GetByCorrelationIdAsync(correlationId);
        loadedByCorr.Should().NotBeNull();
        loadedByCorr!.CompletedAt.Should().NotBeNull();
    }

    [Fact]
    public async Task SaveAsync_ConcurrentStaleRevision_ShouldReturnConcurrencyConflictInSqlServer()
    {
        var id = ProcessId.NewId();
        var initial = ProcessInstance<SampleOrderState>.Create(
            id,
            ProcessType.From("order.sql"),
            ProcessVersion.Initial,
            CorrelationId.From("corr-sql-conflict"),
            new SampleOrderState("cust-sql-3", 100m, false),
            DateTimeOffset.UtcNow);

        await _store.SaveAsync(initial);

        // Competitor 1 advances to Rev 2 and commits
        var advanced1 = initial.Advance(
            new SampleOrderState("cust-sql-3", 250m, false),
            ProcessStatus.Running,
            DateTimeOffset.UtcNow);
        var result1 = await _store.SaveAsync(advanced1);
        result1.Should().Be(ProcessSaveResult.Success);

        // Competitor 2 tries to advance from Rev 1 to Rev 2 (stale base revision)
        var staleAdvance = initial.Advance(
            new SampleOrderState("cust-sql-3", 300m, false),
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
    public async Task SaveAsync_NonExistentUpdate_ShouldReturnNotFoundInSqlServer()
    {
        var id = ProcessId.NewId();
        var initial = ProcessInstance<SampleOrderState>.Create(
            id,
            ProcessType.From("order.sql"),
            ProcessVersion.Initial,
            CorrelationId.From("corr-sql-notfound"),
            new SampleOrderState("cust-sql-4", 100m, false),
            DateTimeOffset.UtcNow);

        // Advance to Rev 2 without having saved Rev 1
        var unseededAdvance = initial.Advance(
            new SampleOrderState("cust-sql-4", 100m, false),
            ProcessStatus.Running,
            DateTimeOffset.UtcNow);

        var result = await _store.SaveAsync(unseededAdvance);

        result.Should().Be(ProcessSaveResult.NotFound);
    }

    [Fact]
    public async Task SaveAsync_WithComplexNestedState_ShouldPersistAndRetrieveCorrectly()
    {
        var complexSerializer = new SystemTextJsonProcessStateSerializer<ComplexOrderState>(
            SqlServerTestJsonContext.Default.ComplexOrderState);
        var store = new SqlServerProcessStore<ComplexOrderState>(_fixture.ConnectionString, complexSerializer);

        var id = ProcessId.NewId();
        var correlationId = CorrelationId.From("corr-sql-complex");

        var state = new ComplexOrderState(
            "complex-cust-sql-1",
            new Address("123 Main St", "Metropolis", "USA"),
            new Address("456 Delivery Ave", "Gotham", "USA"),
            new Dictionary<string, string> { { "Priority", "High" }, { "Campaign", "SummerSale" } },
            new List<OrderItem>
            {
                new OrderItem("SKU-SQL-1", 2, 99.99m),
                new OrderItem("SKU-SQL-2", 1, 149.50m)
            });

        var instance = ProcessInstance<ComplexOrderState>.Create(
            id,
            ProcessType.From("order.sql.complex"),
            ProcessVersion.Initial,
            correlationId,
            state,
            DateTimeOffset.UtcNow);

        var saveResult = await store.SaveAsync(instance);
        saveResult.Should().Be(ProcessSaveResult.Success);

        var loaded = await store.GetByIdAsync(id);
        loaded.Should().NotBeNull();

        loaded!.State.CustomerId.Should().Be("complex-cust-sql-1");
        loaded.State.BillingAddress.City.Should().Be("Metropolis");
        loaded.State.ShippingAddress.City.Should().Be("Gotham");
        loaded.State.Metadata.Should().ContainKey("Priority").WhoseValue.Should().Be("High");
        loaded.State.Items.Should().HaveCount(2);
        loaded.State.Items[1].Sku.Should().Be("SKU-SQL-2");
    }

    [Fact]
    public void Constructor_InvalidArguments_ShouldThrowExpectedExceptions()
    {
        var actNullConn = () => new SqlServerProcessStore<SampleOrderState>(null!, _serializer);
        actNullConn.Should().Throw<ArgumentException>();

        var actEmptyConn = () => new SqlServerProcessStore<SampleOrderState>("   ", _serializer);
        actEmptyConn.Should().Throw<ArgumentException>();

        var actNullTable = () => new SqlServerProcessStore<SampleOrderState>(_fixture.ConnectionString, _serializer, tableName: null!);
        actNullTable.Should().Throw<ArgumentException>();

        var actEmptyTable = () => new SqlServerProcessStore<SampleOrderState>(_fixture.ConnectionString, _serializer, tableName: "  ");
        actEmptyTable.Should().Throw<ArgumentException>();

        var actNullSerializer = () => new SqlServerProcessStore<SampleOrderState>(_fixture.ConnectionString, null!);
        actNullSerializer.Should().Throw<ArgumentNullException>();

        var customLogger = NullLogger<SqlServerProcessStore<SampleOrderState>>.Instance;
        var storeWithLogger = new SqlServerProcessStore<SampleOrderState>(_fixture.ConnectionString, _serializer, "ProcessInstances", customLogger);
        storeWithLogger.Should().NotBeNull();
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
        var correlationId = CorrelationId.From("corr-sql-dup-initial");
        var instance = ProcessInstance<SampleOrderState>.Create(
            id,
            ProcessType.From("order.sql.dup"),
            ProcessVersion.Initial,
            correlationId,
            new SampleOrderState("cust-sql-dup", 50m, false),
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
        var nonExistentCorr = CorrelationId.From($"corr-sql-none-{Guid.NewGuid():N}");
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
        const string customTable = "CustomSqlServerProcessInstances";
        await using (var conn = new SqlConnection(_fixture.ConnectionString))
        {
            await conn.OpenAsync();
            const string ddl = $"""
                IF NOT EXISTS (SELECT * FROM sys.tables WHERE name = '{customTable}')
                BEGIN
                    CREATE TABLE {customTable} (
                        ProcessId UNIQUEIDENTIFIER PRIMARY KEY,
                        ProcessType NVARCHAR(128) NOT NULL,
                        Version INT NOT NULL,
                        Status INT NOT NULL,
                        Revision BIGINT NOT NULL,
                        CorrelationId NVARCHAR(128) NOT NULL,
                        StatePayload NVARCHAR(MAX) NOT NULL,
                        CreatedAt DATETIMEOFFSET NOT NULL,
                        UpdatedAt DATETIMEOFFSET NOT NULL,
                        CompletedAt DATETIMEOFFSET NULL
                    );
                END
                """;
            await using var cmd = new SqlCommand(ddl, conn);
            await cmd.ExecuteNonQueryAsync();
        }

        var customStore = new SqlServerProcessStore<SampleOrderState>(_fixture.ConnectionString, _serializer, tableName: customTable);
        var id = ProcessId.NewId();
        var corr = CorrelationId.From("corr-sql-custom-table");
        var instance = ProcessInstance<SampleOrderState>.Create(
            id,
            ProcessType.From("order.sql.custom"),
            ProcessVersion.Initial,
            corr,
            new SampleOrderState("cust-sql-custom", 99m, false),
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
    public void AddSqlServerProcessStore_DI_Registration_ShouldResolveStoreSuccessfully()
    {
        var services = new ServiceCollection();
        services.AddSingleton<IProcessStateSerializer<SampleOrderState>>(_serializer);
        services.AddSingleton(typeof(ILogger<>), typeof(NullLogger<>));

        services.AddSqlServerProcessStore<SampleOrderState>(_fixture.ConnectionString, "ProcessInstances");

        var provider = services.BuildServiceProvider();
        var resolvedStore = provider.GetService<IProcessStore<SampleOrderState>>();

        resolvedStore.Should().NotBeNull();
        resolvedStore.Should().BeOfType<SqlServerProcessStore<SampleOrderState>>();
    }

    [Fact]
    public void AddSqlServerProcessStore_InvalidArguments_ShouldThrowExpectedExceptions()
    {
        var services = new ServiceCollection();

        var actNullServices = () => ProcessSqlServerServiceCollectionExtensions.AddSqlServerProcessStore<SampleOrderState>(null!, _fixture.ConnectionString);
        actNullServices.Should().Throw<ArgumentNullException>().WithParameterName("services");

        var actNullConn = () => services.AddSqlServerProcessStore<SampleOrderState>(null!);
        actNullConn.Should().Throw<ArgumentException>().WithParameterName("connectionString");

        var actEmptyConn = () => services.AddSqlServerProcessStore<SampleOrderState>("   ");
        actEmptyConn.Should().Throw<ArgumentException>().WithParameterName("connectionString");
    }
}
