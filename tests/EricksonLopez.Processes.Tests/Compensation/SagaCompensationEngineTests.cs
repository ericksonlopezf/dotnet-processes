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
    public async Task ExecuteNextCompensationStepAsync_ShouldThrowArgumentNullException_WhenHandlerOrContextIsNull()
    {
        var initialState = new OrderSagaState("order-1", true, true, false, new List<string>());
        var context = ProcessContext.Create(ProcessId.NewId(), CorrelationId.NewId());
        var step = new CompensationStep("TestStep", new { }, DateTimeOffset.UtcNow);

        var actNullHandler = async () => await SagaCompensationEngine.ExecuteNextCompensationStepAsync(
            initialState, step, null!, context);
        await actNullHandler.Should().ThrowAsync<ArgumentNullException>().WithParameterName("handler");

        var actNullContext = async () => await SagaCompensationEngine.ExecuteNextCompensationStepAsync(
            initialState, step, new OrderCompensationHandler(), null!);
        await actNullContext.Should().ThrowAsync<ArgumentNullException>().WithParameterName("context");
    }

    [Fact]
    public async Task ExecuteNextCompensationStepAsync_ShouldExecuteSingleStep_AndReturnResult()
    {
        var handler = new OrderCompensationHandler();
        var initialState = new OrderSagaState("order-100", true, true, false, new List<string>());

        var step = new CompensationStep("ReserveInventory", new { Sku = "ITEM-1" }, DateTimeOffset.UtcNow.AddMinutes(-2));
        var context = ProcessContext.Create(ProcessId.NewId(), CorrelationId.NewId());

        var result = await SagaCompensationEngine.ExecuteNextCompensationStepAsync(
            currentState: initialState,
            step: step,
            handler: handler,
            context: context);

        result.Status.Should().Be(ProcessStatus.Compensating);
        result.State.InventoryReserved.Should().BeFalse();
        result.State.ExecutedRollbacks.Should().ContainSingle().Which.Should().Be("ReserveInventory");
        result.Effects.Should().ContainSingle();
    }

    [Fact]
    public async Task ExecuteNextCompensationStepAsync_WithEmptyEffects_ShouldSucceedWithoutEffects()
    {
        var handler = new OrderCompensationHandler { EmitNoEffects = true };
        var initialState = new OrderSagaState("order-noeffect", true, true, false, new List<string>());
        var step = new CompensationStep("ChargePayment", new { Amount = 50m }, DateTimeOffset.UtcNow);
        var context = ProcessContext.Create(ProcessId.NewId(), CorrelationId.NewId());

        var result = await SagaCompensationEngine.ExecuteNextCompensationStepAsync(
            currentState: initialState,
            step: step,
            handler: handler,
            context: context);

        result.Status.Should().Be(ProcessStatus.Compensating);
        result.Effects.Should().BeEmpty();
    }

    [Fact]
    public async Task ExecuteNextCompensationStepAsync_ShouldTransitionToFailed_WhenStepFails()
    {
        var handler = new OrderCompensationHandler { FailOnReleaseInventory = true };
        var initialState = new OrderSagaState("order-100", true, true, false, new List<string>());

        var step = new CompensationStep("ReserveInventory", new { Sku = "ITEM-1" }, DateTimeOffset.UtcNow.AddMinutes(-2));
        var context = ProcessContext.Create(ProcessId.NewId(), CorrelationId.NewId());

        var result = await SagaCompensationEngine.ExecuteNextCompensationStepAsync(
            currentState: initialState,
            step: step,
            handler: handler,
            context: context);

        result.Status.Should().Be(ProcessStatus.Failed);
        result.FailureReason.Should().Contain("Warehouse service returned 500");
    }

    [Fact]
    public async Task ExecuteNextCompensationStepAsync_ShouldCatchUnexpectedException_AndTransitionToFailed()
    {
        var handler = new OrderCompensationHandler { ThrowUnexpectedException = true };
        var initialState = new OrderSagaState("order-100", true, true, false, new List<string>());
        var step = new CompensationStep("ChargePayment", new { Amount = 100m }, DateTimeOffset.UtcNow);

        var context = ProcessContext.Create(ProcessId.NewId(), CorrelationId.NewId());

        var result = await SagaCompensationEngine.ExecuteNextCompensationStepAsync(
            currentState: initialState,
            step: step,
            handler: handler,
            context: context);

        result.Status.Should().Be(ProcessStatus.Failed);
        result.FailureReason.Should().Contain("External service network socket closed");
        result.Effects.Should().BeEmpty();
        result.FailureReason.Should().Be("Compensation step 'ChargePayment' encountered an unexpected exception: External service network socket closed");
    }

    [Fact]
    public async Task ExecuteNextCompensationStepAsync_ShouldRethrowProcessException()
    {
        var handler = new OrderCompensationHandler { ThrowProcessException = true };
        var initialState = new OrderSagaState("order-100", true, true, false, new List<string>());
        var step = new CompensationStep("ChargePayment", new { Amount = 100m }, DateTimeOffset.UtcNow);
        var context = ProcessContext.Create(ProcessId.NewId(), CorrelationId.NewId());

        var act = async () => await SagaCompensationEngine.ExecuteNextCompensationStepAsync(
            currentState: initialState,
            step: step,
            handler: handler,
            context: context);

        await act.Should().ThrowAsync<ProcessNotFoundException>();
    }

    [Fact]
    public async Task ExecuteNextCompensationStepAsync_ShouldRethrowOperationCanceledException()
    {
        var handler = new OrderCompensationHandler { ThrowOperationCanceled = true };
        var initialState = new OrderSagaState("order-100", true, true, false, new List<string>());
        var step = new CompensationStep("ChargePayment", new { Amount = 100m }, DateTimeOffset.UtcNow);
        var context = ProcessContext.Create(ProcessId.NewId(), CorrelationId.NewId());

        var act = async () => await SagaCompensationEngine.ExecuteNextCompensationStepAsync(
            currentState: initialState,
            step: step,
            handler: handler,
            context: context);

        await act.Should().ThrowAsync<OperationCanceledException>();
    }
}







