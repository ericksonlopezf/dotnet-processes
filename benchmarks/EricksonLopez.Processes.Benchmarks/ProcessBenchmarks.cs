// Copyright © Erickson Lopez. MIT License.
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json.Serialization;
using System.Threading;
using System.Threading.Tasks;
using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Running;
using EricksonLopez.Processes;
using EricksonLopez.Processes.Abstractions;
using EricksonLopez.Processes.SystemTextJson;

namespace EricksonLopez.Processes.Benchmarks;

public sealed record BenchmarkOrderState(string OrderId, decimal Amount, bool IsPaid) : IProcessState;

public sealed record BenchmarkOrderCreated(Guid OrderId, decimal Amount);

[JsonSerializable(typeof(BenchmarkOrderState))]
[JsonSerializable(typeof(ProcessId))]
[JsonSerializable(typeof(ProcessType))]
[JsonSerializable(typeof(ProcessVersion))]
[JsonSerializable(typeof(Revision))]
[JsonSerializable(typeof(CorrelationId))]
[JsonSerializable(typeof(CausationId))]
[JsonSerializable(typeof(MessageId))]
internal sealed partial class BenchmarkJsonContext : JsonSerializerContext
{
}

public sealed class BenchmarkOrderCreatedCorrelation : IProcessCorrelation<BenchmarkOrderCreated>
{
    public ProcessId ExtractProcessId(BenchmarkOrderCreated @event) => ProcessId.From(@event.OrderId);
    public CorrelationId ExtractCorrelationId(BenchmarkOrderCreated @event) => CorrelationId.From(@event.OrderId.ToString());
}

public sealed class BenchmarkOrderHandler : IProcessHandler<BenchmarkOrderState, BenchmarkOrderCreated>, ICompensationHandler<BenchmarkOrderState>
{
    public ProcessType Type => ProcessType.From("benchmark.order");
    public ProcessVersion Version => ProcessVersion.Initial;

    public ValueTask<ProcessTransitionResult<BenchmarkOrderState>> HandleAsync(
        BenchmarkOrderState state,
        BenchmarkOrderCreated eventMessage,
        ProcessContext context)
    {
        var updated = state with { Amount = eventMessage.Amount, IsPaid = true };
        return ValueTask.FromResult(ProcessTransitionResult<BenchmarkOrderState>.Advance(updated, ProcessStatus.Running));
    }

    public ValueTask<ProcessTransitionResult<BenchmarkOrderState>> CompensateAsync(
        BenchmarkOrderState state,
        CompensationAction action,
        ProcessContext context)
    {
        var updated = state with { IsPaid = false };
        return ValueTask.FromResult(ProcessTransitionResult<BenchmarkOrderState>.Advance(updated, ProcessStatus.Compensating));
    }
}

public sealed class BenchmarkProcessStore : IProcessStore<BenchmarkOrderState>
{
    private readonly Dictionary<ProcessId, ProcessInstance<BenchmarkOrderState>> _db = new();

    public ValueTask<ProcessInstance<BenchmarkOrderState>?> GetByIdAsync(ProcessId id, CancellationToken cancellationToken = default)
    {
        _db.TryGetValue(id, out var val);
        return ValueTask.FromResult(val);
    }

    public ValueTask<ProcessSaveResult> SaveAsync(ProcessInstance<BenchmarkOrderState> instance, CancellationToken cancellationToken = default)
    {
        _db[instance.Id] = instance;
        return ValueTask.FromResult(ProcessSaveResult.Success);
    }

    public ValueTask<bool> ExistsAsync(ProcessId id, CancellationToken cancellationToken = default) =>
        ValueTask.FromResult(_db.ContainsKey(id));
}

[MemoryDiagnoser]
public class ProcessBenchmarks
{
    private BenchmarkProcessStore _store = null!;
    private ProcessCoordinator<BenchmarkOrderState> _coordinator = null!;
    private BenchmarkOrderHandler _handler = null!;
    private BenchmarkOrderCreatedCorrelation _correlation = null!;
    private Guid _orderId;
    private BenchmarkOrderCreated _event = null!;
    private SystemTextJsonProcessStateSerializer<BenchmarkOrderState> _serializer = null!;
    private BenchmarkOrderState _state = null!;
    private byte[] _serializedState = null!;
    private ProcessContext _context = null!;
    private List<CompensationStep> _recordedSteps = null!;

    [GlobalSetup]
    public void Setup()
    {
        _store = new BenchmarkProcessStore();
        _coordinator = new ProcessCoordinator<BenchmarkOrderState>(_store);
        _handler = new BenchmarkOrderHandler();
        _correlation = new BenchmarkOrderCreatedCorrelation();
        _orderId = Guid.NewGuid();
        _event = new BenchmarkOrderCreated(_orderId, 199.99m);
        _state = new BenchmarkOrderState(_orderId.ToString(), 199.99m, true);

        _serializer = new SystemTextJsonProcessStateSerializer<BenchmarkOrderState>(
            BenchmarkJsonContext.Default.BenchmarkOrderState);
        _serializedState = _serializer.Serialize(_state);

        _context = ProcessContext.Create(ProcessId.From(_orderId), CorrelationId.From(_orderId.ToString()));
        _recordedSteps =
        [
            new CompensationStep("ChargePayment", new { Amount = 199.99m }, DateTimeOffset.UtcNow.AddMinutes(-5)),
            new CompensationStep("ReserveInventory", new { Sku = "SKU-1" }, DateTimeOffset.UtcNow.AddMinutes(-3)),
            new CompensationStep("CreateShipment", new { Tracking = "TRK-9" }, DateTimeOffset.UtcNow.AddMinutes(-1))
        ];

        // Seed store
        var initialInstance = ProcessInstance<BenchmarkOrderState>.Create(
            ProcessId.From(_orderId),
            _handler.Type,
            _handler.Version,
            CorrelationId.From(_orderId.ToString()),
            _state,
            DateTimeOffset.UtcNow);

        _store.SaveAsync(initialInstance).AsTask().GetAwaiter().GetResult();
    }

    [Benchmark]
    public ProcessId Benchmark_ProcessId_NewId() => ProcessId.NewId();

    [Benchmark]
    public async ValueTask<ProcessExecutionResult<BenchmarkOrderState>> Benchmark_ProcessCoordinator_ExecuteAsync() =>
        await _coordinator.ExecuteAsync(_handler, _correlation, _event, canInitiate: false);

    [Benchmark]
    public async ValueTask<ProcessTransitionResult<BenchmarkOrderState>> Benchmark_SagaCompensation_ExecutionAsync() =>
        await SagaCompensationEngine.ExecuteCompensationAsync(_state, _recordedSteps, _handler, _context);

    [Benchmark]
    public byte[] Benchmark_SystemTextJson_Serialize() => _serializer.Serialize(_state);

    [Benchmark]
    public BenchmarkOrderState Benchmark_SystemTextJson_Deserialize() => _serializer.Deserialize(_serializedState);
}





