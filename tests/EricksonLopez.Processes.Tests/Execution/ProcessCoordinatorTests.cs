// Copyright © Erickson Lopez. MIT License.
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.Metrics;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using AwesomeAssertions;
using EricksonLopez.Processes.Abstractions;
using EricksonLopez.Processes.Testing;
using Microsoft.Extensions.Time.Testing;
using Xunit;

namespace EricksonLopez.Processes.Tests.Execution;

[Trait("Category", "Unit")]
public class ProcessCoordinatorTests
{
    private sealed record OrderState(string CustomerId, decimal Amount, bool PaymentCompleted, bool InventoryReserved) : IProcessState;

    private sealed record OrderCreated(Guid OrderId, string CustomerId, decimal Amount);
    private sealed record PaymentCompleted(Guid OrderId);
    private sealed record OrderFailed(Guid OrderId, string Reason);
    private sealed record OrderCompensated(Guid OrderId);
    private sealed record ReserveInventoryCommand(Guid OrderId);

    private sealed class OrderCreatedCorrelation : IProcessCorrelation<OrderCreated>
    {
        public ProcessId ExtractProcessId(OrderCreated @event) => ProcessId.From(@event.OrderId);
        public CorrelationId ExtractCorrelationId(OrderCreated @event) => CorrelationId.From(@event.OrderId.ToString());
        public CausationId? ExtractCausationId(OrderCreated @event) => CausationId.From("custom-cause-1");
    }

    private sealed class PaymentCompletedCorrelation : IProcessCorrelation<PaymentCompleted>
    {
        public ProcessId ExtractProcessId(PaymentCompleted @event) => ProcessId.From(@event.OrderId);
        public CorrelationId ExtractCorrelationId(PaymentCompleted @event) => CorrelationId.From(@event.OrderId.ToString());
    }

    private sealed class OrderFailedCorrelation : IProcessCorrelation<OrderFailed>
    {
        public ProcessId ExtractProcessId(OrderFailed @event) => ProcessId.From(@event.OrderId);
        public CorrelationId ExtractCorrelationId(OrderFailed @event) => CorrelationId.From(@event.OrderId.ToString());
    }

    private sealed class OrderCompensatedCorrelation : IProcessCorrelation<OrderCompensated>
    {
        public ProcessId ExtractProcessId(OrderCompensated @event) => ProcessId.From(@event.OrderId);
        public CorrelationId ExtractCorrelationId(OrderCompensated @event) => CorrelationId.From(@event.OrderId.ToString());
    }

    private sealed class OrderFulfillmentProcessHandler :
        IProcessHandler<OrderState, OrderCreated>,
        IProcessHandler<OrderState, PaymentCompleted>,
        IProcessHandler<OrderState, OrderFailed>,
        IProcessHandler<OrderState, OrderCompensated>
    {
        public ProcessType Type => ProcessType.From("order.fulfillment");
        public ProcessVersion Version => ProcessVersion.Initial;

        public CausationId? LastReceivedCausationId { get; private set; }

        public ValueTask<ProcessTransitionResult<OrderState>> HandleAsync(
            OrderState state,
            OrderCreated eventMessage,
            ProcessContext context)
        {
            LastReceivedCausationId = context.CausationId;
            var updated = state with { CustomerId = eventMessage.CustomerId, Amount = eventMessage.Amount };
            var effect = new ProcessEffect.Command(new { Action = "RequestPayment", Amount = eventMessage.Amount });

            return ValueTask.FromResult(ProcessTransitionResult<OrderState>.Advance(
                updated,
                ProcessStatus.Running,
                effects: [effect]));
        }

        public ValueTask<ProcessTransitionResult<OrderState>> HandleAsync(
            OrderState state,
            PaymentCompleted eventMessage,
            ProcessContext context)
        {
            LastReceivedCausationId = context.CausationId;
            var updated = state with { PaymentCompleted = true, InventoryReserved = true };
            var effect = new ProcessEffect.Command(new ReserveInventoryCommand(eventMessage.OrderId));

            return ValueTask.FromResult(ProcessTransitionResult<OrderState>.Complete(
                updated,
                effects: [effect]));
        }

        public ValueTask<ProcessTransitionResult<OrderState>> HandleAsync(
            OrderState state,
            OrderFailed eventMessage,
            ProcessContext context)
        {
            LastReceivedCausationId = context.CausationId;
            return ValueTask.FromResult(ProcessTransitionResult<OrderState>.Fail(
                state,
                eventMessage.Reason));
        }

        public ValueTask<ProcessTransitionResult<OrderState>> HandleAsync(
            OrderState state,
            OrderCompensated eventMessage,
            ProcessContext context)
        {
            LastReceivedCausationId = context.CausationId;
            return ValueTask.FromResult(ProcessTransitionResult<OrderState>.Compensated(
                state));
        }
    }

    private sealed class TestOrderSaga :
        IProcess<OrderState>,
        ICompensationHandler<OrderState>
    {
        public ProcessType Type => ProcessType.From("order.saga");
        public ProcessVersion Version => ProcessVersion.Initial;

        public bool ShouldFailCompensation { get; set; }
        public string? FailureReason { get; set; }
        public List<string> HandledSteps { get; } = new();

        public ValueTask<ProcessTransitionResult<OrderState>> CompensateAsync(
            OrderState state,
            CompensationAction action,
            ProcessContext context)
        {
            HandledSteps.Add(action.StepName);

            if (ShouldFailCompensation)
            {
                return ValueTask.FromResult(ProcessTransitionResult<OrderState>.Fail(
                    state, FailureReason ?? "Compensation step failed"));
            }

            var updated = action.StepName switch
            {
                "ChargePayment" => state with { PaymentCompleted = false },
                "ReserveInventory" => state with { InventoryReserved = false },
                _ => state
            };

            var effect = new ProcessEffect.Command(new { Action = $"Compensate_{action.StepName}" });

            return ValueTask.FromResult(ProcessTransitionResult<OrderState>.Advance(
                updated,
                ProcessStatus.Compensating,
                effects: [effect]));
        }
    }


    private static ValueTask<ProcessExecutionResult<OrderState>> SeedOrderAsync(
        ProcessCoordinator<OrderState> coordinator,
        OrderFulfillmentProcessHandler handler,
        Guid orderId,
        string customerId = "cust-100",
        decimal amount = 250.00m)
    {
        return coordinator.ExecuteAsync(
            handler: handler,
            correlation: new OrderCreatedCorrelation(),
            eventMessage: new OrderCreated(orderId, customerId, amount),
            initialStateFactory: e => new OrderState(e.CustomerId, e.Amount, false, false),
            canInitiate: true);
    }

    [Fact]
    public void Constructor_ShouldThrowArgumentNullException_WhenStoreIsNull()
    {
        var act = () => new ProcessCoordinator<OrderState>(null!);
        act.Should().Throw<ArgumentNullException>().WithParameterName("store");
    }

    [Fact]
    public async Task ExecuteAsync_ShouldThrowArgumentNullException_WhenRequiredArgumentsAreNull()
    {
        var store = new FaultInjectingProcessStore<OrderState>();
        var coordinator = new ProcessCoordinator<OrderState>(store);
        var handler = new OrderFulfillmentProcessHandler();
        var correlation = new OrderCreatedCorrelation();
        var @event = new OrderCreated(Guid.NewGuid(), "cust-1", 100m);

        var actNullHandler = async () => await coordinator.ExecuteAsync<OrderCreated>(
            null!, correlation, @event);
        await actNullHandler.Should().ThrowAsync<ArgumentNullException>().WithParameterName("handler");

        var actNullCorr = async () => await coordinator.ExecuteAsync(
            handler, null!, @event);
        await actNullCorr.Should().ThrowAsync<ArgumentNullException>().WithParameterName("correlation");

        var actNullEvent = async () => await coordinator.ExecuteAsync<OrderCreated>(
            handler, correlation, null!);
        await actNullEvent.Should().ThrowAsync<ArgumentNullException>().WithParameterName("eventMessage");
    }

    [Fact]
    public async Task ExecuteAsync_ShouldInitiateNewProcess_WhenCanInitiateIsTrue()
    {
        var store = new FaultInjectingProcessStore<OrderState>();
        var expectedTime = new DateTimeOffset(2030, 5, 20, 10, 0, 0, TimeSpan.Zero);
        var fakeTime = new FakeTimeProvider(expectedTime);
        var coordinator = new ProcessCoordinator<OrderState>(store, timeProvider: fakeTime);
        var handler = new OrderFulfillmentProcessHandler();
        var correlation = new OrderCreatedCorrelation();

        var orderId = Guid.NewGuid();
        var @event = new OrderCreated(orderId, "cust-100", 250.00m);

        var result = await coordinator.ExecuteAsync(
            handler: handler,
            correlation: correlation,
            eventMessage: @event,
            initialStateFactory: e => new OrderState(e.CustomerId, e.Amount, false, false),
            canInitiate: true);

        result.IsSuccess.Should().BeTrue();
        result.Instance.Id.Value.Should().Be(orderId);
        result.Instance.State.CustomerId.Should().Be("cust-100");
        result.Instance.State.Amount.Should().Be(250.00m);
        result.Instance.Status.Should().Be(ProcessStatus.Running);
        result.Instance.CreatedAt.Should().Be(expectedTime);
        result.Instance.UpdatedAt.Should().Be(expectedTime);
        result.Effects.Should().HaveCount(1);
        handler.LastReceivedCausationId.Should().Be(CausationId.From("custom-cause-1"));

        var stored = await store.GetByIdAsync(ProcessId.From(orderId));
        stored.Should().NotBeNull();
        stored!.Revision.Value.Should().Be(2); // Initial (1) + Advance (2)
    }

    [Fact]
    public async Task ExecuteAsync_ShouldGenerateNewCausationId_WhenCorrelationReturnsNull()
    {
        var store = new FaultInjectingProcessStore<OrderState>();
        var coordinator = new ProcessCoordinator<OrderState>(store);
        var handler = new OrderFulfillmentProcessHandler();
        var correlation = new PaymentCompletedCorrelation();
        var orderId = Guid.NewGuid();

        // 1. Initial creation
        await SeedOrderAsync(coordinator, handler, orderId);

        // 2. Event with null CausationId
        var result = await coordinator.ExecuteAsync(
            handler: handler,
            correlation: correlation,
            eventMessage: new PaymentCompleted(orderId),
            canInitiate: false);

        result.IsSuccess.Should().BeTrue();
        handler.LastReceivedCausationId.Should().NotBeNull();
        handler.LastReceivedCausationId!.Value.Value.Should().NotBeEmpty();
        handler.LastReceivedCausationId!.Value.Value.Should().NotBe("custom-cause-1");
    }

    [Fact]
    public async Task ExecuteAsync_ShouldThrowProcessNotFoundException_WhenInstanceDoesNotExistAndCannotInitiate()
    {
        var store = new FaultInjectingProcessStore<OrderState>();
        var coordinator = new ProcessCoordinator<OrderState>(store);
        var handler = new OrderFulfillmentProcessHandler();
        var correlation = new PaymentCompletedCorrelation();

        var orderId = Guid.NewGuid();
        var @event = new PaymentCompleted(orderId);

        var act = async () => await coordinator.ExecuteAsync(
            handler: handler,
            correlation: correlation,
            eventMessage: @event,
            canInitiate: false);

        var ex = await act.Should().ThrowAsync<ProcessNotFoundException>();
        ex.Which.Message.Should().Contain("order.fulfillment")
            .And.Contain(orderId.ToString())
            .And.Contain("not found and incoming message cannot initiate it.");
    }

    [Fact]
    public async Task ExecuteAsync_ShouldAdvanceToCompleted_WhenCompletingEventArrives()
    {
        var store = new FaultInjectingProcessStore<OrderState>();
        var coordinator = new ProcessCoordinator<OrderState>(store);
        var handler = new OrderFulfillmentProcessHandler();
        var orderId = Guid.NewGuid();

        // 1. Initial creation
        await SeedOrderAsync(coordinator, handler, orderId);

        // 2. Progression to completion
        var result = await coordinator.ExecuteAsync(
            handler: handler,
            correlation: new PaymentCompletedCorrelation(),
            eventMessage: new PaymentCompleted(orderId),
            canInitiate: false);

        result.IsSuccess.Should().BeTrue();
        result.Instance.Status.Should().Be(ProcessStatus.Completed);
        result.Instance.State.PaymentCompleted.Should().BeTrue();
        result.Instance.CompletedAt.Should().NotBeNull();
        result.Effects.Should().HaveCount(1);
    }

    [Fact]
    public async Task ExecuteAsync_ShouldRecordFailedAndCompensatedTransitions()
    {
        var store = new FaultInjectingProcessStore<OrderState>();
        var coordinator = new ProcessCoordinator<OrderState>(store);
        var handler = new OrderFulfillmentProcessHandler();
        var orderId = Guid.NewGuid();

        // Creation
        await SeedOrderAsync(coordinator, handler, orderId);

        // Fail transition
        var failResult = await coordinator.ExecuteAsync(
            handler: handler,
            correlation: new OrderFailedCorrelation(),
            eventMessage: new OrderFailed(orderId, "Fraud detected"),
            canInitiate: false);

        failResult.Instance.Status.Should().Be(ProcessStatus.Failed);
        failResult.Instance.CompletedAt.Should().NotBeNull();

        // Compensated transition on new process
        var order2 = Guid.NewGuid();
        await SeedOrderAsync(coordinator, handler, order2, "cust-200", 100m);

        var compResult = await coordinator.ExecuteAsync(
            handler: handler,
            correlation: new OrderCompensatedCorrelation(),
            eventMessage: new OrderCompensated(order2),
            canInitiate: false);

        compResult.Instance.Status.Should().Be(ProcessStatus.Compensated);
        compResult.Instance.CompletedAt.Should().NotBeNull();
    }

    [Fact]
    public async Task ExecuteAsync_ShouldBeIdempotent_WhenEventArrivesOnCompletedOrFailedProcess()
    {
        var store = new FaultInjectingProcessStore<OrderState>();
        var coordinator = new ProcessCoordinator<OrderState>(store);
        var handler = new OrderFulfillmentProcessHandler();
        var orderId = Guid.NewGuid();

        await SeedOrderAsync(coordinator, handler, orderId);

        await coordinator.ExecuteAsync(
            handler: handler,
            correlation: new PaymentCompletedCorrelation(),
            eventMessage: new PaymentCompleted(orderId),
            canInitiate: false);

        // Duplicate event on already completed instance
        var duplicateResult = await coordinator.ExecuteAsync(
            handler: handler,
            correlation: new PaymentCompletedCorrelation(),
            eventMessage: new PaymentCompleted(orderId),
            canInitiate: false);

        duplicateResult.IsSuccess.Should().BeTrue();
        duplicateResult.Instance.Status.Should().Be(ProcessStatus.Completed);
        duplicateResult.Effects.Should().BeEmpty();

        // Duplicate on failed instance
        var failOrderId = Guid.NewGuid();
        await SeedOrderAsync(coordinator, handler, failOrderId, "cust-f", 100m);
        await coordinator.ExecuteAsync(
            handler: handler,
            correlation: new OrderFailedCorrelation(),
            eventMessage: new OrderFailed(failOrderId, "err"),
            canInitiate: false);
        var dupFailed = await coordinator.ExecuteAsync(
            handler: handler,
            correlation: new PaymentCompletedCorrelation(),
            eventMessage: new PaymentCompleted(failOrderId),
            canInitiate: false);
        dupFailed.IsSuccess.Should().BeTrue();
        dupFailed.Instance.Status.Should().Be(ProcessStatus.Failed);

        // Duplicate on compensated instance
        var compOrderId = Guid.NewGuid();
        await SeedOrderAsync(coordinator, handler, compOrderId, "cust-c", 100m);
        await coordinator.ExecuteAsync(
            handler: handler,
            correlation: new OrderCompensatedCorrelation(),
            eventMessage: new OrderCompensated(compOrderId),
            canInitiate: false);
        var dupComp = await coordinator.ExecuteAsync(
            handler: handler,
            correlation: new PaymentCompletedCorrelation(),
            eventMessage: new PaymentCompleted(compOrderId),
            canInitiate: false);
        dupComp.IsSuccess.Should().BeTrue();
        dupComp.Instance.Status.Should().Be(ProcessStatus.Compensated);
    }

    [Fact]
    public async Task ExecuteAsync_ShouldThrowProcessNotFoundException_WhenCanInitiateIsTrueButFactoryIsNull()
    {
        var store = new FaultInjectingProcessStore<OrderState>();
        var coordinator = new ProcessCoordinator<OrderState>(store);
        var handler = new OrderFulfillmentProcessHandler();

        var act = async () => await coordinator.ExecuteAsync(
            handler: handler,
            correlation: new OrderCreatedCorrelation(),
            eventMessage: new OrderCreated(Guid.NewGuid(), "cust-null-fact", 100m),
            initialStateFactory: null,
            canInitiate: true);

        await act.Should().ThrowAsync<ProcessNotFoundException>();
    }

    [Fact]
    public void Constructor_ShouldClampNegativeMaxConcurrencyRetriesToZero()
    {
        var store = new FaultInjectingProcessStore<OrderState>();
        var coordinator = new ProcessCoordinator<OrderState>(store, options: new ProcessCoordinatorOptions { MaxConcurrencyRetries = -5 });
        coordinator.Should().NotBeNull();
    }

    [Fact]
    public async Task ExecuteAsync_ShouldRetryOnConcurrencyConflict_AndSucceedIfWithinMaxRetries()
    {
        var store = new FaultInjectingProcessStore<OrderState>
        {
            ConcurrencyConflictsToSimulate = 2
        };

        var recordedAttempts = new List<int>();
        var coordinator = new ProcessCoordinator<OrderState>(
            store,
            options: new ProcessCoordinatorOptions { MaxConcurrencyRetries = 2 },
            backoffStrategy: attempt =>
            {
                recordedAttempts.Add(attempt);
                return TimeSpan.Zero;
            });
        var handler = new OrderFulfillmentProcessHandler();
        var orderId = Guid.NewGuid();

        var result = await coordinator.ExecuteAsync(
            handler: handler,
            correlation: new OrderCreatedCorrelation(),
            eventMessage: new OrderCreated(orderId, "cust-conflict", 100m),
            initialStateFactory: e => new OrderState(e.CustomerId, e.Amount, false, false),
            canInitiate: true);

        result.IsSuccess.Should().BeTrue();
        result.Instance.Status.Should().Be(ProcessStatus.Running);
        recordedAttempts.Should().Equal(1, 2);
    }

    [Theory]
    [InlineData(1, 10)]
    [InlineData(2, 20)]
    [InlineData(3, 30)]
    [InlineData(5, 50)]
    public void DefaultBackoffStrategy_ShouldScaleLinearlyWithAttempts(int attempt, double expectedMs)
    {
        var delay = ProcessCoordinator<OrderState>.DefaultBackoffStrategy(attempt);
        delay.TotalMilliseconds.Should().Be(expectedMs);
    }

    [Fact]
    public async Task ExecuteAsync_ShouldUseCustomBackoffStrategy_WhenProvided()
    {
        var store = new FaultInjectingProcessStore<OrderState>
        {
            ConcurrencyConflictsToSimulate = 2
        };

        var recordedAttempts = new List<int>();
        var coordinator = new ProcessCoordinator<OrderState>(
            store,
            options: new ProcessCoordinatorOptions { MaxConcurrencyRetries = 2 },
            backoffStrategy: attempt =>
            {
                recordedAttempts.Add(attempt);
                return TimeSpan.Zero;
            });

        var handler = new OrderFulfillmentProcessHandler();
        var result = await coordinator.ExecuteAsync(
            handler: handler,
            correlation: new OrderCreatedCorrelation(),
            eventMessage: new OrderCreated(Guid.NewGuid(), "cust-custom-backoff", 100m),
            initialStateFactory: e => new OrderState(e.CustomerId, e.Amount, false, false),
            canInitiate: true);

        result.IsSuccess.Should().BeTrue();
        recordedAttempts.Should().Equal(1, 2);
    }

    [Fact]
    public async Task ExecuteAsync_ShouldThrowConcurrencyConflictException_WhenExceedingMaxRetries()
    {
        var store = new FaultInjectingProcessStore<OrderState>
        {
            ConcurrencyConflictsToSimulate = 5
        };

        var coordinator = new ProcessCoordinator<OrderState>(store, options: new ProcessCoordinatorOptions { MaxConcurrencyRetries = 2 });
        var handler = new OrderFulfillmentProcessHandler();
        var orderId = Guid.NewGuid();

        var act = async () => await coordinator.ExecuteAsync(
            handler: handler,
            correlation: new OrderCreatedCorrelation(),
            eventMessage: new OrderCreated(orderId, "cust-conflict-fail", 100m),
            initialStateFactory: e => new OrderState(e.CustomerId, e.Amount, false, false),
            canInitiate: true);

        await act.Should().ThrowAsync<ConcurrencyConflictException>();
    }

    [Fact]
    public async Task ExecuteAsync_ShouldReturnNonSuccessResult_WhenPersistenceErrorOccurs()
    {
        var store = new FaultInjectingProcessStore<OrderState>
        {
            ForcedSaveResult = ProcessSaveResult.PersistenceError
        };

        var coordinator = new ProcessCoordinator<OrderState>(store);
        var handler = new OrderFulfillmentProcessHandler();
        var orderId = Guid.NewGuid();

        var result = await coordinator.ExecuteAsync(
            handler: handler,
            correlation: new OrderCreatedCorrelation(),
            eventMessage: new OrderCreated(orderId, "cust-error", 100m),
            initialStateFactory: e => new OrderState(e.CustomerId, e.Amount, false, false),
            canInitiate: true);

        result.IsSuccess.Should().BeFalse();
        result.SaveResult.Should().Be(ProcessSaveResult.PersistenceError);
    }

    [Fact]
    public async Task ExecuteAsync_ShouldThrowOperationCanceledException_WhenCancellationTokenIsCancelled()
    {
        var store = new FaultInjectingProcessStore<OrderState>();
        var coordinator = new ProcessCoordinator<OrderState>(store);
        var handler = new OrderFulfillmentProcessHandler();
        using var cts = new CancellationTokenSource();
        await cts.CancelAsync();

        var act = async () => await coordinator.ExecuteAsync(
            handler: handler,
            correlation: new OrderCreatedCorrelation(),
            eventMessage: new OrderCreated(Guid.NewGuid(), "cust-cancel", 100m),
            initialStateFactory: e => new OrderState(e.CustomerId, e.Amount, false, false),
            canInitiate: true,
            cancellationToken: cts.Token);

        await act.Should().ThrowAsync<OperationCanceledException>();
    }

    [Fact]
    public async Task ExecuteAsync_WithActiveActivityListener_ShouldPopulateActivityTags()
    {
        Activity? capturedActivity = null;
        using var listener = new ActivityListener
        {
            ShouldListenTo = s => s.Name == ProcessDiagnostics.SourceName,
            Sample = (ref ActivityCreationOptions<ActivityContext> _) => ActivitySamplingResult.AllDataAndRecorded,
            ActivityStarted = a => capturedActivity = a
        };
        ActivitySource.AddActivityListener(listener);

        var store = new FaultInjectingProcessStore<OrderState>();
        var coordinator = new ProcessCoordinator<OrderState>(store);
        var handler = new OrderFulfillmentProcessHandler();
        var correlation = new OrderCreatedCorrelation();
        var orderId = Guid.NewGuid();

        var result = await coordinator.ExecuteAsync(
            handler: handler,
            correlation: correlation,
            eventMessage: new OrderCreated(orderId, "cust-act", 100m),
            initialStateFactory: e => new OrderState(e.CustomerId, e.Amount, false, false),
            canInitiate: true);

        result.IsSuccess.Should().BeTrue();
        capturedActivity.Should().NotBeNull();
        capturedActivity!.DisplayName.Should().Be("Process order.fulfillment.Execute");
        capturedActivity.GetTagItem("process.id").Should().Be(orderId.ToString());
        capturedActivity.GetTagItem("process.type").Should().Be("order.fulfillment");
        capturedActivity.GetTagItem("process.version").Should().Be(1);
        capturedActivity.GetTagItem("correlation.id").Should().Be(orderId.ToString());
    }

    [Fact]
    public async Task ExecuteAsync_ShouldEmitDiagnosticMetrics_DuringLifecycleTransitions()
    {
        var startedCount = 0L;
        var completedCount = 0L;
        var failedCount = 0L;
        var compensatedCount = 0L;
        var conflictCount = 0L;
        var durationCount = 0;

        using var listener = new MeterListener();
        listener.InstrumentPublished = (instrument, l) =>
        {
            if (instrument.Meter.Name == ProcessDiagnostics.SourceName)
            {
                l.EnableMeasurementEvents(instrument);
            }
        };
        listener.SetMeasurementEventCallback<long>((instrument, measurement, tags, _) =>
        {
            if (instrument.Name == "processes.started") startedCount += measurement;
            else if (instrument.Name == "processes.completed") completedCount += measurement;
            else if (instrument.Name == "processes.failed") failedCount += measurement;
            else if (instrument.Name == "processes.compensated") compensatedCount += measurement;
            else if (instrument.Name == "processes.concurrency_conflicts") conflictCount += measurement;
        });
        listener.SetMeasurementEventCallback<double>((instrument, measurement, tags, _) =>
        {
            if (instrument.Name == "processes.transition.duration") durationCount++;
        });
        listener.Start();

        var store = new FaultInjectingProcessStore<OrderState>
        {
            ConcurrencyConflictsToSimulate = 1
        };
        var coordinator = new ProcessCoordinator<OrderState>(store, options: new ProcessCoordinatorOptions { MaxConcurrencyRetries = 2 });
        var handler = new OrderFulfillmentProcessHandler();

        // 1. Initial creation with 1 simulated conflict -> verifies started, conflict and duration
        var order1 = Guid.NewGuid();
        await coordinator.ExecuteAsync(
            handler: handler,
            correlation: new OrderCreatedCorrelation(),
            eventMessage: new OrderCreated(order1, "cust-diag", 100m),
            initialStateFactory: e => new OrderState(e.CustomerId, e.Amount, false, false),
            canInitiate: true);

        // 2. Complete order -> verifies completed metric
        await coordinator.ExecuteAsync(
            handler: handler,
            correlation: new PaymentCompletedCorrelation(),
            eventMessage: new PaymentCompleted(order1),
            canInitiate: false);

        // 3. Fail order -> verifies failed metric
        var order2 = Guid.NewGuid();
        await coordinator.ExecuteAsync(
            handler: handler,
            correlation: new OrderCreatedCorrelation(),
            eventMessage: new OrderCreated(order2, "cust-diag-2", 100m),
            initialStateFactory: e => new OrderState(e.CustomerId, e.Amount, false, false),
            canInitiate: true);
        await coordinator.ExecuteAsync(
            handler: handler,
            correlation: new OrderFailedCorrelation(),
            eventMessage: new OrderFailed(order2, "Reason"),
            canInitiate: false);

        // 4. Compensated order -> verifies compensated metric
        var order3 = Guid.NewGuid();
        await coordinator.ExecuteAsync(
            handler: handler,
            correlation: new OrderCreatedCorrelation(),
            eventMessage: new OrderCreated(order3, "cust-diag-3", 100m),
            initialStateFactory: e => new OrderState(e.CustomerId, e.Amount, false, false),
            canInitiate: true);
        await coordinator.ExecuteAsync(
            handler: handler,
            correlation: new OrderCompensatedCorrelation(),
            eventMessage: new OrderCompensated(order3),
            canInitiate: false);

        startedCount.Should().Be(4);
        conflictCount.Should().Be(1);
        completedCount.Should().Be(1);
        failedCount.Should().Be(1);
        compensatedCount.Should().Be(1);
        durationCount.Should().BeGreaterThanOrEqualTo(4);
    }

    #region CompensateAsync Tests

    [Fact]
    public async Task CompensateAsync_ShouldThrowArgumentNullException_WhenRequiredArgumentsAreNull()
    {
        var store = new FaultInjectingProcessStore<OrderState>();
        var coordinator = new ProcessCoordinator<OrderState>(store);
        var saga = new TestOrderSaga();
        var processId = ProcessId.NewId();
        var steps = new List<CompensationStep>();

        var actNullSteps = async () => await coordinator.CompensateAsync(
            processId, null!, saga);
        await actNullSteps.Should().ThrowAsync<ArgumentNullException>().WithParameterName("recordedSteps");

        var actNullSaga = async () => await coordinator.CompensateAsync<TestOrderSaga>(
            processId, steps, null!);
        await actNullSaga.Should().ThrowAsync<ArgumentNullException>().WithParameterName("saga");
    }

    [Fact]
    public async Task CompensateAsync_ShouldThrowProcessNotFoundException_WhenInstanceDoesNotExist()
    {
        var store = new FaultInjectingProcessStore<OrderState>();
        var coordinator = new ProcessCoordinator<OrderState>(store);
        var saga = new TestOrderSaga();
        var processId = ProcessId.NewId();
        var steps = new List<CompensationStep>();

        var act = async () => await coordinator.CompensateAsync(processId, steps, saga);

        var ex = await act.Should().ThrowAsync<ProcessNotFoundException>();
        ex.Which.Message.Should().Contain("order.saga")
            .And.Contain(processId.ToString())
            .And.Contain("not found for compensation");
    }

    [Theory]
    [InlineData(ProcessStatus.Compensated)]
    [InlineData(ProcessStatus.Failed)]
    public async Task CompensateAsync_ShouldReturnSuccessDirectly_WhenInstanceAlreadyTerminal(ProcessStatus terminalStatus)
    {
        var store = new FaultInjectingProcessStore<OrderState>();
        var coordinator = new ProcessCoordinator<OrderState>(store);
        var saga = new TestOrderSaga();
        var processId = ProcessId.NewId();
        var initialState = new OrderState("cust-term", 100m, true, true);

        var instance = ProcessInstance<OrderState>.Create(
            processId,
            saga.Type,
            saga.Version,
            CorrelationId.NewId(),
            initialState,
            DateTimeOffset.UtcNow);

        var terminalInstance = instance.Advance(initialState, terminalStatus, DateTimeOffset.UtcNow);
        await store.SaveAsync(terminalInstance);

        var step = new CompensationStep("ChargePayment", new { Amount = 100m }, DateTimeOffset.UtcNow);
        var result = await coordinator.CompensateAsync(processId, [step], saga);

        result.IsSuccess.Should().BeTrue();
        result.Instance.Status.Should().Be(terminalStatus);
        result.Effects.Should().BeEmpty();
        saga.HandledSteps.Should().BeEmpty();
    }

    [Fact]
    public async Task CompensateAsync_ShouldExecuteCompensation_AndAdvanceToCompensated()
    {
        var store = new FaultInjectingProcessStore<OrderState>();
        var coordinator = new ProcessCoordinator<OrderState>(store);
        var saga = new TestOrderSaga();
        var processId = ProcessId.NewId();
        var initialState = new OrderState("cust-1", 100m, true, true);

        var instance = ProcessInstance<OrderState>.Create(
            processId,
            saga.Type,
            saga.Version,
            CorrelationId.NewId(),
            initialState,
            DateTimeOffset.UtcNow);

        var runningInstance = instance.Advance(initialState, ProcessStatus.Running, DateTimeOffset.UtcNow);
        await store.SaveAsync(runningInstance);

        var step1 = new CompensationStep("ChargePayment", new { Amount = 100m }, DateTimeOffset.UtcNow.AddMinutes(-5));
        var step2 = new CompensationStep("ReserveInventory", new { Sku = "ITEM-1" }, DateTimeOffset.UtcNow.AddMinutes(-2));

        var result = await coordinator.CompensateAsync(processId, [step1, step2], saga);

        result.IsSuccess.Should().BeTrue();
        result.Instance.Status.Should().Be(ProcessStatus.Compensated);
        result.Instance.State.PaymentCompleted.Should().BeFalse();
        result.Instance.State.InventoryReserved.Should().BeFalse();
        result.Effects.Should().HaveCount(2);

        // Verify LIFO order of compensation execution
        saga.HandledSteps.Should().Equal("ReserveInventory", "ChargePayment");

        var stored = await store.GetByIdAsync(processId);
        stored.Should().NotBeNull();
        stored!.Status.Should().Be(ProcessStatus.Compensated);
    }

    [Fact]
    public async Task CompensateAsync_ShouldRetryOnConcurrencyConflict_AndSucceedWithinMaxRetries()
    {
        var store = new FaultInjectingProcessStore<OrderState>
        {
            ConcurrencyConflictsToSimulate = 2
        };

        var recordedAttempts = new List<int>();
        var coordinator = new ProcessCoordinator<OrderState>(
            store,
            options: new ProcessCoordinatorOptions { MaxConcurrencyRetries = 2 },
            backoffStrategy: attempt =>
            {
                recordedAttempts.Add(attempt);
                return TimeSpan.Zero;
            });

        var saga = new TestOrderSaga();
        var processId = ProcessId.NewId();
        var initialState = new OrderState("cust-retry", 50m, true, false);

        var instance = ProcessInstance<OrderState>.Create(
            processId, saga.Type, saga.Version, CorrelationId.NewId(), initialState, DateTimeOffset.UtcNow);
        var runningInstance = instance.Advance(initialState, ProcessStatus.Running, DateTimeOffset.UtcNow);
        await store.InnerStore.SaveAsync(runningInstance);

        var step = new CompensationStep("ChargePayment", new { Amount = 50m }, DateTimeOffset.UtcNow);
        var result = await coordinator.CompensateAsync(processId, [step], saga);

        result.IsSuccess.Should().BeTrue();
        result.Instance.Status.Should().Be(ProcessStatus.Compensated);
        recordedAttempts.Should().Equal(1, 2);
    }

    [Fact]
    public async Task CompensateAsync_ShouldThrowConcurrencyConflictException_WhenExceedingMaxRetries()
    {
        var store = new FaultInjectingProcessStore<OrderState>
        {
            ConcurrencyConflictsToSimulate = 5
        };

        var coordinator = new ProcessCoordinator<OrderState>(
            store,
            options: new ProcessCoordinatorOptions { MaxConcurrencyRetries = 2 },
            backoffStrategy: _ => TimeSpan.Zero);

        var saga = new TestOrderSaga();
        var processId = ProcessId.NewId();
        var initialState = new OrderState("cust-conflict", 50m, true, false);

        var instance = ProcessInstance<OrderState>.Create(
            processId, saga.Type, saga.Version, CorrelationId.NewId(), initialState, DateTimeOffset.UtcNow);
        var runningInstance = instance.Advance(initialState, ProcessStatus.Running, DateTimeOffset.UtcNow);
        await store.InnerStore.SaveAsync(runningInstance);

        var step = new CompensationStep("ChargePayment", new { Amount = 50m }, DateTimeOffset.UtcNow);
        var act = async () => await coordinator.CompensateAsync(processId, [step], saga);

        await act.Should().ThrowAsync<ConcurrencyConflictException>();
    }

    [Fact]
    public async Task CompensateAsync_ShouldHandleFailedCompensationStep_AndRecordFailedTransition()
    {
        var store = new FaultInjectingProcessStore<OrderState>();
        var coordinator = new ProcessCoordinator<OrderState>(store);
        var saga = new TestOrderSaga
        {
            ShouldFailCompensation = true,
            FailureReason = "Payment gateway refund failed"
        };
        var processId = ProcessId.NewId();
        var initialState = new OrderState("cust-fail", 100m, true, false);

        var instance = ProcessInstance<OrderState>.Create(
            processId, saga.Type, saga.Version, CorrelationId.NewId(), initialState, DateTimeOffset.UtcNow);
        var runningInstance = instance.Advance(initialState, ProcessStatus.Running, DateTimeOffset.UtcNow);
        await store.SaveAsync(runningInstance);

        var step = new CompensationStep("ChargePayment", new { Amount = 100m }, DateTimeOffset.UtcNow);
        var result = await coordinator.CompensateAsync(processId, [step], saga);

        result.IsSuccess.Should().BeTrue();
        result.Instance.Status.Should().Be(ProcessStatus.Failed);
        result.Instance.CompletedAt.Should().NotBeNull();
    }

    [Fact]
    public async Task CompensateAsync_ShouldReturnNonSuccessResult_WhenPersistenceErrorOccurs()
    {
        var store = new FaultInjectingProcessStore<OrderState>();
        var saga = new TestOrderSaga();
        var processId = ProcessId.NewId();
        var initialState = new OrderState("cust-err", 100m, true, false);

        var instance = ProcessInstance<OrderState>.Create(
            processId, saga.Type, saga.Version, CorrelationId.NewId(), initialState, DateTimeOffset.UtcNow);
        var runningInstance = instance.Advance(initialState, ProcessStatus.Running, DateTimeOffset.UtcNow);
        await store.SaveAsync(runningInstance);

        // Force persistence error on subsequent save
        store.ForcedSaveResult = ProcessSaveResult.PersistenceError;
        var coordinator = new ProcessCoordinator<OrderState>(store);

        var step = new CompensationStep("ChargePayment", new { Amount = 100m }, DateTimeOffset.UtcNow);
        var result = await coordinator.CompensateAsync(processId, [step], saga);

        result.IsSuccess.Should().BeFalse();
        result.SaveResult.Should().Be(ProcessSaveResult.PersistenceError);
    }

    [Fact]
    public async Task CompensateAsync_ShouldThrowOperationCanceledException_WhenCancellationTokenCancelled()
    {
        var store = new FaultInjectingProcessStore<OrderState>();
        var coordinator = new ProcessCoordinator<OrderState>(store);
        var saga = new TestOrderSaga();
        var processId = ProcessId.NewId();
        var initialState = new OrderState("cust-canc", 100m, true, false);

        var instance = ProcessInstance<OrderState>.Create(
            processId, saga.Type, saga.Version, CorrelationId.NewId(), initialState, DateTimeOffset.UtcNow);
        await store.SaveAsync(instance);

        using var cts = new CancellationTokenSource();
        await cts.CancelAsync();

        var step = new CompensationStep("ChargePayment", new { Amount = 100m }, DateTimeOffset.UtcNow);
        var act = async () => await coordinator.CompensateAsync(processId, [step], saga, cancellationToken: cts.Token);

        await act.Should().ThrowAsync<OperationCanceledException>();
    }

    [Fact]
    public async Task CompensateAsync_WithActiveActivityListener_ShouldPopulateActivityTags()
    {
        Activity? capturedActivity = null;
        using var listener = new ActivityListener
        {
            ShouldListenTo = s => s.Name == ProcessDiagnostics.SourceName,
            Sample = (ref ActivityCreationOptions<ActivityContext> _) => ActivitySamplingResult.AllDataAndRecorded,
            ActivityStarted = a => capturedActivity = a
        };
        ActivitySource.AddActivityListener(listener);

        var store = new FaultInjectingProcessStore<OrderState>();
        var coordinator = new ProcessCoordinator<OrderState>(store);
        var saga = new TestOrderSaga();
        var processId = ProcessId.NewId();
        var initialState = new OrderState("cust-act", 100m, true, false);

        var instance = ProcessInstance<OrderState>.Create(
            processId, saga.Type, saga.Version, CorrelationId.NewId(), initialState, DateTimeOffset.UtcNow);
        var running = instance.Advance(initialState, ProcessStatus.Running, DateTimeOffset.UtcNow);
        await store.SaveAsync(running);

        var step = new CompensationStep("ChargePayment", new { Amount = 100m }, DateTimeOffset.UtcNow);
        var result = await coordinator.CompensateAsync(processId, [step], saga);

        result.IsSuccess.Should().BeTrue();
        capturedActivity.Should().NotBeNull();
        capturedActivity!.DisplayName.Should().Be("Process order.saga.Compensate");
        capturedActivity.GetTagItem("process.id").Should().Be(processId.ToString());
        capturedActivity.GetTagItem("process.type").Should().Be("order.saga");
        capturedActivity.GetTagItem("process.version").Should().Be(1);
    }

    [Fact]
    public void ProcessCoordinatorOptions_ShouldHaveCorrectDefaultsAndBeSettable()
    {
        var options = new ProcessCoordinatorOptions();
        options.MaxConcurrencyRetries.Should().Be(3);
        options.InitialBackoffDelay.Should().Be(TimeSpan.FromMilliseconds(50));

        options.MaxConcurrencyRetries = 5;
        options.InitialBackoffDelay = TimeSpan.FromMilliseconds(100);

        options.MaxConcurrencyRetries.Should().Be(5);
        options.InitialBackoffDelay.Should().Be(TimeSpan.FromMilliseconds(100));
    }

    [Fact]
    public async Task ExecuteAsync_ShouldUseDefaultBackoffStrategyLambda_WhenStrategyIsNull()
    {
        var store = new FaultInjectingProcessStore<OrderState>
        {
            ConcurrencyConflictsToSimulate = 1
        };

        var coordinator = new ProcessCoordinator<OrderState>(
            store,
            options: new ProcessCoordinatorOptions
            {
                MaxConcurrencyRetries = 2,
                InitialBackoffDelay = TimeSpan.FromMilliseconds(1)
            },
            backoffStrategy: null);

        var handler = new OrderFulfillmentProcessHandler();
        var result = await coordinator.ExecuteAsync(
            handler: handler,
            correlation: new OrderCreatedCorrelation(),
            eventMessage: new OrderCreated(Guid.NewGuid(), "cust-def-lambda", 100m),
            initialStateFactory: e => new OrderState(e.CustomerId, e.Amount, false, false),
            canInitiate: true);

        result.IsSuccess.Should().BeTrue();
    }

    [Fact]
    public async Task CompensateAsync_ShouldEmitDiagnosticMetrics()
    {
        var compensatedCount = 0L;
        var failedCount = 0L;
        var conflictCount = 0L;
        var durationCount = 0;

        using var listener = new MeterListener();
        listener.InstrumentPublished = (instrument, l) =>
        {
            if (instrument.Meter.Name == ProcessDiagnostics.SourceName)
            {
                l.EnableMeasurementEvents(instrument);
            }
        };
        listener.SetMeasurementEventCallback<long>((instrument, measurement, tags, _) =>
        {
            if (instrument.Name == "processes.compensated") compensatedCount += measurement;
            else if (instrument.Name == "processes.failed") failedCount += measurement;
            else if (instrument.Name == "processes.concurrency_conflicts") conflictCount += measurement;
        });
        listener.SetMeasurementEventCallback<double>((instrument, measurement, tags, _) =>
        {
            if (instrument.Name == "processes.transition.duration") durationCount++;
        });
        listener.Start();

        var store = new FaultInjectingProcessStore<OrderState>
        {
            ConcurrencyConflictsToSimulate = 1
        };
        var coordinator = new ProcessCoordinator<OrderState>(
            store,
            options: new ProcessCoordinatorOptions
            {
                MaxConcurrencyRetries = 2,
                InitialBackoffDelay = TimeSpan.FromMilliseconds(1)
            });

        var saga = new TestOrderSaga();
        var processId = ProcessId.NewId();
        var initialState = new OrderState("cust-comp-metric", 100m, true, true);

        var instance = ProcessInstance<OrderState>.Create(
            processId, saga.Type, saga.Version, CorrelationId.NewId(), initialState, DateTimeOffset.UtcNow);
        var running = instance.Advance(initialState, ProcessStatus.Running, DateTimeOffset.UtcNow);
        await store.InnerStore.SaveAsync(running);

        var step = new CompensationStep("ChargePayment", new { Amount = 100m }, DateTimeOffset.UtcNow);
        var result = await coordinator.CompensateAsync(processId, [step], saga);

        result.IsSuccess.Should().BeTrue();
        result.Instance.Status.Should().Be(ProcessStatus.Compensated);

        // Fail case
        var failSaga = new TestOrderSaga { ShouldFailCompensation = true, FailureReason = "fail" };
        var failProcessId = ProcessId.NewId();
        var failInstance = ProcessInstance<OrderState>.Create(
            failProcessId, failSaga.Type, failSaga.Version, CorrelationId.NewId(), initialState, DateTimeOffset.UtcNow);
        var failRunning = failInstance.Advance(initialState, ProcessStatus.Running, DateTimeOffset.UtcNow);
        await store.InnerStore.SaveAsync(failRunning);

        var failResult = await coordinator.CompensateAsync(failProcessId, [step], failSaga);
        failResult.Instance.Status.Should().Be(ProcessStatus.Failed);

        compensatedCount.Should().Be(1);
        failedCount.Should().Be(1);
        conflictCount.Should().Be(1);
        durationCount.Should().BeGreaterThanOrEqualTo(2);
    }

    [Fact]
    public async Task ExecuteAsync_ShouldRespectCustomOptionsWithZeroRetries()
    {
        var store = new FaultInjectingProcessStore<OrderState>
        {
            ConcurrencyConflictsToSimulate = 1
        };

        var coordinator = new ProcessCoordinator<OrderState>(
            store,
            options: new ProcessCoordinatorOptions { MaxConcurrencyRetries = 0 });

        var handler = new OrderFulfillmentProcessHandler();
        var act = async () => await coordinator.ExecuteAsync(
            handler: handler,
            correlation: new OrderCreatedCorrelation(),
            eventMessage: new OrderCreated(Guid.NewGuid(), "cust-zero-retries", 100m),
            initialStateFactory: e => new OrderState(e.CustomerId, e.Amount, false, false),
            canInitiate: true);

        await act.Should().ThrowAsync<ConcurrencyConflictException>();
    }

    [Fact]
    public async Task ExecuteAsync_ShouldUseMultipliedBackoffDelay_OnSecondRetryAttempt()
    {
        var store = new FaultInjectingProcessStore<OrderState>
        {
            ConcurrencyConflictsToSimulate = 2
        };

        var coordinator = new ProcessCoordinator<OrderState>(
            store,
            options: new ProcessCoordinatorOptions
            {
                MaxConcurrencyRetries = 3,
                InitialBackoffDelay = TimeSpan.FromMilliseconds(100)
            },
            backoffStrategy: null);

        var sw = Stopwatch.StartNew();
        var handler = new OrderFulfillmentProcessHandler();
        var result = await coordinator.ExecuteAsync(
            handler: handler,
            correlation: new OrderCreatedCorrelation(),
            eventMessage: new OrderCreated(Guid.NewGuid(), "cust-delay2", 100m),
            initialStateFactory: e => new OrderState(e.CustomerId, e.Amount, false, false),
            canInitiate: true);
        sw.Stop();

        result.IsSuccess.Should().BeTrue();
        // attempt 1: 100ms, attempt 2: 200ms => total delay >= 300ms (with division it would be 100 + 50 = 150ms)
        sw.ElapsedMilliseconds.Should().BeGreaterThanOrEqualTo(250);
    }

    [Fact]
    public async Task ExecuteAsync_ShouldThrowOperationCanceledException_WhenCancellationTokenCanceledDuringBackoff()
    {
        var store = new FaultInjectingProcessStore<OrderState>
        {
            ConcurrencyConflictsToSimulate = 5
        };

        using var cts = new CancellationTokenSource();

        var coordinator = new ProcessCoordinator<OrderState>(
            store,
            options: new ProcessCoordinatorOptions
            {
                MaxConcurrencyRetries = 5
            },
            backoffStrategy: attempt =>
            {
                // Cancel token immediately when entering backoff
                cts.Cancel();
                return TimeSpan.FromSeconds(5);
            });

        var handler = new OrderFulfillmentProcessHandler();
        var act = async () => await coordinator.ExecuteAsync(
            handler: handler,
            correlation: new OrderCreatedCorrelation(),
            eventMessage: new OrderCreated(Guid.NewGuid(), "cust-cancel", 100m),
            initialStateFactory: e => new OrderState(e.CustomerId, e.Amount, false, false),
            canInitiate: true,
            cancellationToken: cts.Token);

        await act.Should().ThrowAsync<OperationCanceledException>();
    }

    [Fact]
    public async Task CompensateAsync_ShouldThrowOperationCanceledException_WhenCancellationTokenCanceledDuringBackoff()
    {
        var store = new FaultInjectingProcessStore<OrderState>();
        var saga = new TestOrderSaga();
        var processId = ProcessId.NewId();
        var initialState = new OrderState("cust-cancel-comp", 100m, true, true);
        var instance = ProcessInstance<OrderState>.Create(
            processId,
            saga.Type,
            saga.Version,
            CorrelationId.NewId(),
            initialState,
            DateTimeOffset.UtcNow);

        await store.SaveAsync(instance);

        // Simulate concurrency conflicts on subsequent updates
        store.ConcurrencyConflictsToSimulate = 5;

        using var cts = new CancellationTokenSource();

        var coordinator = new ProcessCoordinator<OrderState>(
            store,
            options: new ProcessCoordinatorOptions
            {
                MaxConcurrencyRetries = 5
            },
            backoffStrategy: attempt =>
            {
                cts.Cancel();
                return TimeSpan.FromSeconds(5);
            });

        var steps = new List<CompensationStep>
        {
            new("ChargePayment", new { Amount = 100m }, DateTimeOffset.UtcNow)
        };

        var act = async () => await coordinator.CompensateAsync(
            processId,
            steps,
            saga,
            cancellationToken: cts.Token);

        await act.Should().ThrowAsync<OperationCanceledException>();
    }

    [Fact]
    public async Task ExecuteAsync_WithDefaultBackoffStrategy_ShouldScaleLinearlyWithAttempts()
    {
        var store = new FaultInjectingProcessStore<OrderState>();
        store.ConcurrencyConflictsToSimulate = 2;

        var options = new ProcessCoordinatorOptions
        {
            MaxConcurrencyRetries = 3,
            InitialBackoffDelay = TimeSpan.FromMilliseconds(8)
        };

        var coordinator = new ProcessCoordinator<OrderState>(store, options);

        var handler = new OrderFulfillmentProcessHandler();
        var sw = System.Diagnostics.Stopwatch.StartNew();
        var result = await coordinator.ExecuteAsync(
            handler: handler,
            correlation: new OrderCreatedCorrelation(),
            eventMessage: new OrderCreated(Guid.NewGuid(), "cust-retry-default", 100m),
            initialStateFactory: e => new OrderState(e.CustomerId, e.Amount, false, false),
            canInitiate: true);
        sw.Stop();

        result.SaveResult.Should().Be(ProcessSaveResult.Success);
        sw.ElapsedMilliseconds.Should().BeGreaterThanOrEqualTo(10);
    }

    [Fact]
    public async Task CompensateAsync_WithDefaultBackoffStrategy_ShouldScaleLinearlyWithAttempts()
    {
        var store = new FaultInjectingProcessStore<OrderState>();
        var saga = new TestOrderSaga();
        var processId = ProcessId.NewId();
        var initialState = new OrderState("cust-comp-default", 100m, true, true);
        var instance = ProcessInstance<OrderState>.Create(
            processId,
            saga.Type,
            saga.Version,
            CorrelationId.NewId(),
            initialState,
            DateTimeOffset.UtcNow);

        await store.SaveAsync(instance);
        store.ConcurrencyConflictsToSimulate = 2;

        var options = new ProcessCoordinatorOptions
        {
            MaxConcurrencyRetries = 3,
            InitialBackoffDelay = TimeSpan.FromMilliseconds(8)
        };

        var coordinator = new ProcessCoordinator<OrderState>(store, options);

        var steps = new List<CompensationStep>
        {
            new("ChargePayment", new { Amount = 100m }, DateTimeOffset.UtcNow)
        };

        var sw = System.Diagnostics.Stopwatch.StartNew();
        var result = await coordinator.CompensateAsync(processId, steps, saga);
        sw.Stop();

        result.SaveResult.Should().Be(ProcessSaveResult.Success);
        sw.ElapsedMilliseconds.Should().BeGreaterThanOrEqualTo(10);
    }

    #endregion
}









