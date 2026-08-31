// Copyright © Erickson Lopez. MIT License.
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using EricksonLopez.Processes;
using EricksonLopez.Processes.Abstractions;
using EricksonLopez.Processes.Testing;

namespace EricksonLopez.Processes.Showcase.Level06_ErrorHandlingAndRecovery;

public sealed record RiskySagaState(
    string SagaId,
    bool Step1Done,
    bool CompensationAttempted) : IProcessState
{
    public static RiskySagaState Initial(string id) => new(id, true, false);
}

public sealed class RiskySaga :
    ISaga<RiskySagaState>,
    ICompensationHandler<RiskySagaState>
{
    public ProcessType Type => ProcessType.From("risky.saga");
    public ProcessVersion Version => ProcessVersion.Initial;

    public ValueTask<ProcessTransitionResult<RiskySagaState>> CompensateAsync(
        RiskySagaState state,
        CompensationAction action,
        ProcessContext context)
    {
        Console.WriteLine($"  [Compensation Handler] Attempting compensation for step '{action.StepName}'...");

        if (action.StepName == "ExternalBankingRefund")
        {
            // Simulate an unrecoverable third-party banking failure during compensation
            Console.WriteLine("  [Compensation Handler] External banking API rejected refund request!");
            return ValueTask.FromResult(ProcessTransitionResult<RiskySagaState>.Fail(
                state with { CompensationAttempted = true },
                "Third-party bank API unavailable for refund compensation"));
        }

        return ValueTask.FromResult(ProcessTransitionResult<RiskySagaState>.Advance(
            state with { CompensationAttempted = true },
            ProcessStatus.Compensating));
    }
}

/// <summary>
/// Level 6-A: Handling Compensation Failures &amp; Escalation
/// </summary>
public static class Level06CompensationFailureDemo
{
    public static async Task RunAsync()
    {
        Console.ForegroundColor = ConsoleColor.Cyan;
        Console.WriteLine("================================================================================");
        Console.WriteLine(" LEVEL 06-A: SAGA COMPENSATION FAILURE HANDLING & ESCALATION");
        Console.WriteLine("================================================================================");
        Console.ResetColor();

        var store = new InMemoryProcessStore<RiskySagaState>();
        var coordinator = new ProcessCoordinator<RiskySagaState>(store);
        var saga = new RiskySaga();
        var sagaId = ProcessId.NewId();

        // 1. Pre-populate a running saga instance in storage
        var instance = ProcessInstance<RiskySagaState>.Create(
            id: sagaId,
            type: saga.Type,
            version: saga.Version,
            correlationId: CorrelationId.From(sagaId.Value),
            initialState: RiskySagaState.Initial(sagaId.Value.ToString()),
            now: DateTimeOffset.UtcNow);

        await store.SaveAsync(instance);

        Console.WriteLine($"Initiating saga compensation for '{sagaId}'...");

        // 2. Execute compensation containing a failing step
        var recordedSteps = new List<CompensationStep>
        {
            CompensationStep.Create("ExternalBankingRefund", new { Amount = 500.00m }, DateTimeOffset.UtcNow)
        };

        var result = await coordinator.CompensateAsync(sagaId, recordedSteps, saga);

        Console.WriteLine();
        Console.WriteLine($"Saga Post-Compensation Status: {result.Instance.Status}");
        Console.WriteLine($"Compensation Was Attempted:    {result.Instance.State.CompensationAttempted}");
        Console.WriteLine($"Save Result:                   {result.SaveResult}");

        Console.ForegroundColor = ConsoleColor.Green;
        Console.WriteLine("✔ Level 06-A Compensation Failure handled and recorded as Failed status.");
        Console.ResetColor();
    }
}
