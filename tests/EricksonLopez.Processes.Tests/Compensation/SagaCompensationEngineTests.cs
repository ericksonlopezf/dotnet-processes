// Copyright © Erickson Lopez. MIT License.
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using AwesomeAssertions;
using EricksonLopez.Processes.Abstractions;
using Xunit;

namespace EricksonLopez.Processes.Tests.Compensation;

[Trait("Category", "Unit")]
public class SagaCompensationEngineTests
{
    private sealed record OrderSagaState(
        string OrderId,
        bool PaymentCharged,
        bool InventoryReserved,
        bool ShipmentCreated,
        List<string> ExecutedRollbacks) : IProcessState;

    private sealed class OrderCompensationHandler : ICompensationHandler<OrderSagaState>
    {
        public bool FailOnReleaseInventory { get; set; }
        public bool ThrowProcessException { get; set; }
        public bool ThrowUnexpectedException { get; set; }
        public bool ThrowOperationCanceled { get; set; }
        public bool EmitNoEffects { get; set; }

        public ValueTask<ProcessTransitionResult<OrderSagaState>> CompensateAsync(
            OrderSagaState state,
            CompensationAction action,
            ProcessContext context)
        {
            state.ExecutedRollbacks.Add(action.StepName);

            if (ThrowProcessException)
            {
                throw new ProcessNotFoundException("Process not found during compensation");
            }

            if (ThrowUnexpectedException)
            {
                throw new InvalidOperationException("External service network socket closed");
            }

            if (ThrowOperationCanceled)
            {
                throw new OperationCanceledException(context.CancellationToken);
            }

            if (action.StepName == "ReserveInventory" && FailOnReleaseInventory)
            {
                return ValueTask.FromResult(ProcessTransitionResult<OrderSagaState>.Fail(
                    state,
                    "Warehouse service returned 500"));
            }

            var updated = action.StepName switch
            {
                "ChargePayment" => state with { PaymentCharged = false },
                "ReserveInventory" => state with { InventoryReserved = false },
                "CreateShipment" => state with { ShipmentCreated = false },
                _ => state
            };

            if (EmitNoEffects)
            {
                return ValueTask.FromResult(ProcessTransitionResult<OrderSagaState>.Advance(
                    updated,
                    ProcessStatus.Compensating));
            }

            var effect = new ProcessEffect.Command(new { Action = $"Rollback_{action.StepName}" });

            return ValueTask.FromResult(ProcessTransitionResult<OrderSagaState>.Advance(
                updated,
                ProcessStatus.Compensating,
                effects: [effect]));
        }
    }

    [Fact]
    public async Task ExecuteCompensationAsync_ShouldThrowArgumentNullException_WhenHandlerOrContextIsNull()
    {
        var initialState = new OrderSagaState("order-1", true, true, false, new List<string>());
        var context = ProcessContext.Create(ProcessId.NewId(), CorrelationId.NewId());

        var actNullHandler = async () => await SagaCompensationEngine.ExecuteCompensationAsync(
            initialState, [], null!, context);
        await actNullHandler.Should().ThrowAsync<ArgumentNullException>().WithParameterName("handler");

        var actNullContext = async () => await SagaCompensationEngine.ExecuteCompensationAsync(
            initialState, [], new OrderCompensationHandler(), null!);
        await actNullContext.Should().ThrowAsync<ArgumentNullException>().WithParameterName("context");
    }

    [Fact]
    public async Task ExecuteCompensationAsync_ShouldReturnCompensated_WhenRecordedStepsIsNullOrEmpty()
    {
        var handler = new OrderCompensationHandler();
        var initialState = new OrderSagaState("order-1", true, true, false, new List<string>());
        var context = ProcessContext.Create(ProcessId.NewId(), CorrelationId.NewId());

        var resultNull = await SagaCompensationEngine.ExecuteCompensationAsync(
            initialState, null!, handler, context);
        resultNull.Status.Should().Be(ProcessStatus.Compensated);
        resultNull.State.Should().Be(initialState);

        var resultEmpty = await SagaCompensationEngine.ExecuteCompensationAsync(
            initialState, new List<CompensationStep>(), handler, context);
        resultEmpty.Status.Should().Be(ProcessStatus.Compensated);
        resultEmpty.State.Should().Be(initialState);
    }

    [Fact]
    public async Task ExecuteCompensationAsync_ShouldRunStepsInReverseLIFOOrder_AndReachCompensated()
    {
        var handler = new OrderCompensationHandler();
        var initialState = new OrderSagaState("order-100", true, true, false, new List<string>());

        var step1 = new CompensationStep("ChargePayment", new { Amount = 100m }, DateTimeOffset.UtcNow.AddMinutes(-5));
        var step2 = new CompensationStep("ReserveInventory", new { Sku = "ITEM-1" }, DateTimeOffset.UtcNow.AddMinutes(-2));

        var recordedSteps = new List<CompensationStep> { step1, step2 };
        var context = ProcessContext.Create(ProcessId.NewId(), CorrelationId.NewId());

        var result = await SagaCompensationEngine.ExecuteCompensationAsync(
            initialState: initialState,
            recordedSteps: recordedSteps,
            handler: handler,
            context: context);

        result.Status.Should().Be(ProcessStatus.Compensated);
        result.State.PaymentCharged.Should().BeFalse();
        result.State.InventoryReserved.Should().BeFalse();

        // Check LIFO order: step2 ("ReserveInventory") must run before step1 ("ChargePayment")
        result.State.ExecutedRollbacks.Should().ContainInOrder("ReserveInventory", "ChargePayment");
        result.Effects.Should().HaveCount(2);
    }

    [Fact]
    public async Task ExecuteCompensationAsync_WithEmptyEffects_ShouldSucceedWithoutAccumulatedEffects()
    {
        var handler = new OrderCompensationHandler { EmitNoEffects = true };
        var initialState = new OrderSagaState("order-noeffect", true, true, false, new List<string>());
        var step = new CompensationStep("ChargePayment", new { Amount = 50m }, DateTimeOffset.UtcNow);
        var context = ProcessContext.Create(ProcessId.NewId(), CorrelationId.NewId());

        var result = await SagaCompensationEngine.ExecuteCompensationAsync(
            initialState: initialState,
            recordedSteps: [step],
            handler: handler,
            context: context);

        result.Status.Should().Be(ProcessStatus.Compensated);
        result.Effects.Should().BeEmpty();
    }

    [Fact]
    public async Task ExecuteCompensationAsync_ShouldTransitionToFailed_WhenStepFails()
    {
        var handler = new OrderCompensationHandler { FailOnReleaseInventory = true };
        var initialState = new OrderSagaState("order-100", true, true, false, new List<string>());

        var step1 = new CompensationStep("ChargePayment", new { Amount = 100m }, DateTimeOffset.UtcNow.AddMinutes(-5));
        var step2 = new CompensationStep("ReserveInventory", new { Sku = "ITEM-1" }, DateTimeOffset.UtcNow.AddMinutes(-2));

        var recordedSteps = new List<CompensationStep> { step1, step2 };
        var context = ProcessContext.Create(ProcessId.NewId(), CorrelationId.NewId());

        var result = await SagaCompensationEngine.ExecuteCompensationAsync(
            initialState: initialState,
            recordedSteps: recordedSteps,
            handler: handler,
            context: context);

        result.Status.Should().Be(ProcessStatus.Failed);
        result.FailureReason.Should().Contain("Warehouse service returned 500");
    }

    [Fact]
    public async Task ExecuteCompensationAsync_ShouldCatchUnexpectedException_AndTransitionToFailed()
    {
        var handler = new OrderCompensationHandler { ThrowUnexpectedException = true };
        var initialState = new OrderSagaState("order-100", true, true, false, new List<string>());
        var step = new CompensationStep("ChargePayment", new { Amount = 100m }, DateTimeOffset.UtcNow);

        var context = ProcessContext.Create(ProcessId.NewId(), CorrelationId.NewId());

        var result = await SagaCompensationEngine.ExecuteCompensationAsync(
            initialState: initialState,
            recordedSteps: [step],
            handler: handler,
            context: context);

        result.Status.Should().Be(ProcessStatus.Failed);
        result.FailureReason.Should().Contain("External service network socket closed");
    }

    [Fact]
    public async Task ExecuteCompensationAsync_ShouldRethrowProcessException()
    {
        var handler = new OrderCompensationHandler { ThrowProcessException = true };
        var initialState = new OrderSagaState("order-100", true, true, false, new List<string>());
        var step = new CompensationStep("ChargePayment", new { Amount = 100m }, DateTimeOffset.UtcNow);
        var context = ProcessContext.Create(ProcessId.NewId(), CorrelationId.NewId());

        var act = async () => await SagaCompensationEngine.ExecuteCompensationAsync(
            initialState: initialState,
            recordedSteps: [step],
            handler: handler,
            context: context);

        await act.Should().ThrowAsync<ProcessNotFoundException>();
    }

    [Fact]
    public async Task ExecuteCompensationAsync_ShouldRethrowOperationCanceledException()
    {
        var handler = new OrderCompensationHandler { ThrowOperationCanceled = true };
        var initialState = new OrderSagaState("order-100", true, true, false, new List<string>());
        var step = new CompensationStep("ChargePayment", new { Amount = 100m }, DateTimeOffset.UtcNow);
        var context = ProcessContext.Create(ProcessId.NewId(), CorrelationId.NewId());

        var act = async () => await SagaCompensationEngine.ExecuteCompensationAsync(
            initialState: initialState,
            recordedSteps: [step],
            handler: handler,
            context: context);

        await act.Should().ThrowAsync<OperationCanceledException>();
    }
}







