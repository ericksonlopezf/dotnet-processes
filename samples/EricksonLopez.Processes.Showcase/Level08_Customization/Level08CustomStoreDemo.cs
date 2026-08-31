// Copyright © Erickson Lopez. MIT License.
using System;
using System.Collections.Concurrent;
using System.Threading;
using System.Threading.Tasks;
using EricksonLopez.Processes;
using EricksonLopez.Processes.Abstractions;

namespace EricksonLopez.Processes.Showcase.Level08_Customization;

public sealed record CustomAuditState(string Payload, int EditCount) : IProcessState;
public sealed record EditPayloadEvent(Guid ProcessId, string NewPayload);

/// <summary>
/// Custom implementation of IProcessStore demonstrating clean extension points.
/// </summary>
public sealed class AuditingProcessStore<TState> : IProcessStore<TState>
    where TState : notnull
{
    private readonly ConcurrentDictionary<ProcessId, ProcessInstance<TState>> _db = new();
    public int SaveOperationsCount { get; private set; }

    public ValueTask<ProcessInstance<TState>?> GetByIdAsync(ProcessId id, CancellationToken cancellationToken = default)
    {
        _db.TryGetValue(id, out var instance);
        return ValueTask.FromResult(instance);
    }

    public ValueTask<ProcessSaveResult> SaveAsync(ProcessInstance<TState> instance, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(instance);

        SaveOperationsCount++;
        Console.WriteLine($"    [CustomStore.SaveAsync] Persisting process '{instance.Id}' at Revision {instance.Revision.Value} (Total Saves: {SaveOperationsCount})");

        _db[instance.Id] = instance;
        return ValueTask.FromResult(ProcessSaveResult.Success);
    }

    public ValueTask<bool> ExistsAsync(ProcessId id, CancellationToken cancellationToken = default) =>
        ValueTask.FromResult(_db.ContainsKey(id));

    public ValueTask<ProcessInstance<TState>?> GetByCorrelationIdAsync(CorrelationId correlationId, CancellationToken cancellationToken = default)
    {
        foreach (var inst in _db.Values)
        {
            if (inst.CorrelationId == correlationId)
            {
                return ValueTask.FromResult<ProcessInstance<TState>?>(inst);
            }
        }
        return ValueTask.FromResult<ProcessInstance<TState>?>(null);
    }
}

public sealed class CustomAuditProcess :
    IProcess<CustomAuditState>,
    IProcessHandler<CustomAuditState, EditPayloadEvent>
{
    public ProcessType Type => ProcessType.From("custom.audit.process");
    public ProcessVersion Version => ProcessVersion.Initial;

    public ValueTask<ProcessTransitionResult<CustomAuditState>> HandleAsync(
        CustomAuditState state,
        EditPayloadEvent eventMessage,
        ProcessContext context)
    {
        var updated = state with { Payload = eventMessage.NewPayload, EditCount = state.EditCount + 1 };
        return ValueTask.FromResult(ProcessTransitionResult<CustomAuditState>.Advance(updated));
    }
}

public sealed class EditPayloadCorrelation : IProcessCorrelation<EditPayloadEvent>
{
    public ProcessId ExtractProcessId(EditPayloadEvent @event) => ProcessId.From(@event.ProcessId);
    public CorrelationId ExtractCorrelationId(EditPayloadEvent @event) => CorrelationId.From(@event.ProcessId.ToString());
}

/// <summary>
/// Level 8-A: Custom IProcessStore Implementation
/// </summary>
public static class Level08CustomStoreDemo
{
    public static async Task RunAsync()
    {
        Console.ForegroundColor = ConsoleColor.Cyan;
        Console.WriteLine("================================================================================");
        Console.WriteLine(" LEVEL 08-A: CUSTOM IPROCESSSTORE EXTENSION & AUDITING WRAPPER");
        Console.WriteLine("================================================================================");
        Console.ResetColor();

        var customStore = new AuditingProcessStore<CustomAuditState>();
        var coordinator = new ProcessCoordinator<CustomAuditState>(customStore);
        var process = new CustomAuditProcess();
        var correlation = new EditPayloadCorrelation();
        var processId = Guid.NewGuid();

        // 1. Initial creation
        await coordinator.ExecuteAsync(
            handler: process,
            correlation: correlation,
            eventMessage: new EditPayloadEvent(processId, "Initial Version"),
            initialStateFactory: e => new CustomAuditState(e.NewPayload, 0),
            canInitiate: true);

        // 2. Subsequent edits
        await coordinator.ExecuteAsync(
            handler: process,
            correlation: correlation,
            eventMessage: new EditPayloadEvent(processId, "Updated Version 2"),
            canInitiate: false);

        await coordinator.ExecuteAsync(
            handler: process,
            correlation: correlation,
            eventMessage: new EditPayloadEvent(processId, "Final Version 3"),
            canInitiate: false);

        Console.WriteLine();
        Console.WriteLine($"Total Custom Store Save Invocations: {customStore.SaveOperationsCount}");

        Console.ForegroundColor = ConsoleColor.Green;
        Console.WriteLine("✔ Level 08-A Custom Store demo completed successfully.");
        Console.ResetColor();
    }
}
