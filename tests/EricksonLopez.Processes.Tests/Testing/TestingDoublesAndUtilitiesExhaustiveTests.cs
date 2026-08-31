// Copyright © Erickson Lopez. MIT License.
using System;
using System.Threading;
using System.Threading.Tasks;
using AwesomeAssertions;
using EricksonLopez.Processes.Abstractions;
using EricksonLopez.Processes.Testing;
using EricksonLopez.Processes.Testing.Doubles;
using Xunit;

namespace EricksonLopez.Processes.Tests.Testing;

[Trait("Category", "Unit")]
public class TestingDoublesAndUtilitiesExhaustiveTests
{
    [Fact]
    public async Task InMemoryProcessStore_GetByCorrelationIdAsync_MatchesAndReturnsNullOnMiss()
    {
        var store = new InMemoryProcessStore<TestOrderState>();
        var id1 = ProcessId.NewId();
        var id2 = ProcessId.NewId();
        var corr1 = CorrelationId.NewId();
        var corr2 = CorrelationId.NewId();

        var inst1 = ProcessInstance<TestOrderState>.Create(
            id1, ProcessType.From("order"), ProcessVersion.Initial, corr1,
            new TestOrderState("ord-1"), DateTimeOffset.UtcNow);

        var inst2 = ProcessInstance<TestOrderState>.Create(
            id2, ProcessType.From("order"), ProcessVersion.Initial, corr2,
            new TestOrderState("ord-2"), DateTimeOffset.UtcNow);

        await store.SaveAsync(inst1);
        await store.SaveAsync(inst2);

        var found1 = await store.GetByCorrelationIdAsync(corr1);
        found1.Should().NotBeNull();
        found1!.Id.Should().Be(id1);

        var found2 = await store.GetByCorrelationIdAsync(corr2);
        found2.Should().NotBeNull();
        found2!.Id.Should().Be(id2);

        var notFound = await store.GetByCorrelationIdAsync(CorrelationId.NewId());
        notFound.Should().BeNull();
    }

    [Fact]
    public async Task FaultInjectingProcessStore_ExistsAsync_FaultsAndForcedResults()
    {
        var customInner = new InMemoryProcessStore<TestOrderState>();
        var store = new FaultInjectingProcessStore<TestOrderState>(customInner);
        store.InnerStore.Should().BeSameAs(customInner);

        var id = ProcessId.NewId();
        var corr = CorrelationId.NewId();
        var inst = ProcessInstance<TestOrderState>.Create(
            id, ProcessType.From("order"), ProcessVersion.Initial, corr,
            new TestOrderState("ord-1"), DateTimeOffset.UtcNow);

        await store.SaveAsync(inst);

        // Normal delegation
        var existsNormal = await store.ExistsAsync(id);
        existsNormal.Should().BeTrue();

        // Forced exists
        store.ForcedExistsResult = false;
        store.ForcedExistsResult.Should().BeFalse();
        var existsForced = await store.ExistsAsync(id);
        existsForced.Should().BeFalse();
        store.ForcedExistsResult = null;

        // Exception on exists
        var expectedEx = new InvalidOperationException("Exists failed");
        store.ExceptionToThrowOnExists = expectedEx;
        store.ExceptionToThrowOnExists.Should().BeSameAs(expectedEx);

        var actExists = async () => await store.ExistsAsync(id);
        (await actExists.Should().ThrowAsync<InvalidOperationException>()).Which.Should().BeSameAs(expectedEx);
    }

    [Fact]
    public async Task FaultInjectingProcessStore_GetByCorrelationIdAsync_FaultsAndForcedResults()
    {
        var store = new FaultInjectingProcessStore<TestOrderState>();
        var id = ProcessId.NewId();
        var corr = CorrelationId.NewId();
        var inst = ProcessInstance<TestOrderState>.Create(
            id, ProcessType.From("order"), ProcessVersion.Initial, corr,
            new TestOrderState("ord-1"), DateTimeOffset.UtcNow);

        await store.SaveAsync(inst);

        // Normal delegation
        var foundNormal = await store.GetByCorrelationIdAsync(corr);
        foundNormal.Should().NotBeNull();
        foundNormal!.Id.Should().Be(id);

        // Forced result
        var forcedInstance = ProcessInstance<TestOrderState>.Create(
            ProcessId.NewId(), ProcessType.From("forced"), ProcessVersion.Initial, CorrelationId.NewId(),
            new TestOrderState("forced-order"), DateTimeOffset.UtcNow);

        store.ForcedGetByCorrelationIdResult = forcedInstance;
        store.ForcedGetByCorrelationIdResult.Should().BeSameAs(forcedInstance);

        var forcedResult = await store.GetByCorrelationIdAsync(corr);
        forcedResult.Should().BeSameAs(forcedInstance);
        store.ForcedGetByCorrelationIdResult = null;

        // Exception on get by correlation
        var expectedEx = new TimeoutException("Correlation lookup timeout");
        store.ExceptionToThrowOnGetByCorrelationId = expectedEx;
        store.ExceptionToThrowOnGetByCorrelationId.Should().BeSameAs(expectedEx);

        var actCorr = async () => await store.GetByCorrelationIdAsync(corr);
        (await actCorr.Should().ThrowAsync<TimeoutException>()).Which.Should().BeSameAs(expectedEx);
    }

    [Fact]
    public void TestDoubles_ModelsAndExtractors_ShouldWork()
    {
        // TestCounterState
        var counterDefault = new TestCounterState();
        counterDefault.Count.Should().Be(0);
        var counter5 = new TestCounterState(5);
        counter5.Count.Should().Be(5);

        // TestOrderState
        var orderDefault = new TestOrderState("order-1");
        orderDefault.OrderId.Should().Be("order-1");
        orderDefault.Amount.Should().Be(0m);
        orderDefault.IsPaid.Should().BeFalse();
        orderDefault.IsCompleted.Should().BeFalse();

        var orderCustom = new TestOrderState("order-2", 99.99m, true, true);
        orderCustom.Amount.Should().Be(99.99m);
        orderCustom.IsPaid.Should().BeTrue();
        orderCustom.IsCompleted.Should().BeTrue();

        // TestOrderCreatedEvent & Extractor
        var orderGuid = Guid.NewGuid();
        var orderCreated = new TestOrderCreatedEvent(orderGuid, 120m);
        orderCreated.OrderId.Should().Be(orderGuid);
        orderCreated.Amount.Should().Be(120m);

        var orderCreatedCorr = new TestOrderCreatedCorrelation();
        orderCreatedCorr.ExtractProcessId(orderCreated).Value.Should().Be(orderGuid.ToString());
        orderCreatedCorr.ExtractCorrelationId(orderCreated).Value.Should().Be(orderGuid.ToString());

        var actOrderNull1 = () => orderCreatedCorr.ExtractProcessId(null!);
        actOrderNull1.Should().Throw<ArgumentNullException>().WithParameterName("@event");

        var actOrderNull2 = () => orderCreatedCorr.ExtractCorrelationId(null!);
        actOrderNull2.Should().Throw<ArgumentNullException>().WithParameterName("@event");

        // TestOrderPaidEvent
        var orderPaid = new TestOrderPaidEvent(orderGuid);
        orderPaid.OrderId.Should().Be(orderGuid);

        // TestIncrementEvent & Extractor
        var pid = ProcessId.NewId();
        var incDefault = new TestIncrementEvent(pid);
        incDefault.TargetId.Should().Be(pid);
        incDefault.Delta.Should().Be(1);

        var incCustom = new TestIncrementEvent(pid, 5);
        incCustom.Delta.Should().Be(5);

        var incCorr = new TestIncrementCorrelation();
        incCorr.ExtractProcessId(incCustom).Should().Be(pid);
        incCorr.ExtractCorrelationId(incCustom).Should().Be(CorrelationId.From(pid.Value));

        var actIncNull1 = () => incCorr.ExtractProcessId(null!);
        actIncNull1.Should().Throw<ArgumentNullException>().WithParameterName("@event");

        var actIncNull2 = () => incCorr.ExtractCorrelationId(null!);
        actIncNull2.Should().Throw<ArgumentNullException>().WithParameterName("@event");
    }
}
