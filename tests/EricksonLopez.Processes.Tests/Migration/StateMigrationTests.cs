// Copyright © Erickson Lopez. MIT License.
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using AwesomeAssertions;
using EricksonLopez.Processes.Abstractions;
using Xunit;

namespace EricksonLopez.Processes.Tests.Migration;

[Trait("Category", "Unit")]
public class StateMigrationTests
{
    public sealed record OrderStateV1(string OrderId, decimal Amount, bool IsPaid) : IProcessState;

    public sealed record OrderStateV2(string OrderId, decimal Amount, string Currency, decimal TaxRate, bool IsPaid) : IProcessState;

    public sealed class OrderStateV1ToV2Migrator : IProcessStateMigrator<OrderStateV1, OrderStateV2>
    {
        public ProcessVersion FromVersion => ProcessVersion.From(1);
        public ProcessVersion ToVersion => ProcessVersion.From(2);

        public OrderStateV2 Migrate(OrderStateV1 sourceState)
        {
            ArgumentNullException.ThrowIfNull(sourceState);

            return new OrderStateV2(
                OrderId: sourceState.OrderId,
                Amount: sourceState.Amount,
                Currency: "USD",
                TaxRate: 0.18m,
                IsPaid: sourceState.IsPaid);
        }
    }

    [Fact]
    public void Migrate_ShouldCorrectlyTransformStateToNewSchema()
    {
        var migrator = new OrderStateV1ToV2Migrator();
        var v1State = new OrderStateV1("ORD-999", 150.00m, true);

        var v2State = migrator.Migrate(v1State);

        v2State.OrderId.Should().Be("ORD-999");
        v2State.Amount.Should().Be(150.00m);
        v2State.Currency.Should().Be("USD");
        v2State.TaxRate.Should().Be(0.18m);
        v2State.IsPaid.Should().BeTrue();
        migrator.FromVersion.Value.Should().Be(1);
        migrator.ToVersion.Value.Should().Be(2);
    }

    public sealed record OrderStateV3(string OrderId, decimal Amount, string Currency, decimal TaxRate, bool IsPaid, string Note) : IProcessState;

    public sealed class OrderStateV2ToV3Migrator : IProcessStateMigrator<OrderStateV2, OrderStateV3>
    {
        public ProcessVersion FromVersion => ProcessVersion.From(2);
        public ProcessVersion ToVersion => ProcessVersion.From(3);

        public OrderStateV3 Migrate(OrderStateV2 sourceState)
        {
            ArgumentNullException.ThrowIfNull(sourceState);

            return new OrderStateV3(
                OrderId: sourceState.OrderId,
                Amount: sourceState.Amount,
                Currency: sourceState.Currency,
                TaxRate: sourceState.TaxRate,
                IsPaid: sourceState.IsPaid,
                Note: "Migrated from V2");
        }
    }

    [Fact]
    public void Pipeline_Empty_ShouldActAsIdentityMigration()
    {
        var pipeline = ProcessStateMigrationPipeline
            .Create<OrderStateV1>(ProcessVersion.From(1))
            .Build<OrderStateV1>();

        pipeline.FromVersion.Should().Be(ProcessVersion.From(1));
        pipeline.ToVersion.Should().Be(ProcessVersion.From(1));

        var state = new OrderStateV1("ORD-000", 10m, true);
        var result = pipeline.Migrate(state);

        result.Should().Be(state);
    }

    [Fact]
    public void Pipeline_ShouldExecuteSequentialMigrations_V1_To_V3()
    {
        var v1ToV2 = new OrderStateV1ToV2Migrator();
        var v2ToV3 = new OrderStateV2ToV3Migrator();

        var pipeline = ProcessStateMigrationPipeline
            .Create<OrderStateV1>(ProcessVersion.From(1))
            .AddStep(v1ToV2)
            .AddStep(v2ToV3)
            .Build<OrderStateV1>();

        pipeline.FromVersion.Should().Be(ProcessVersion.From(1));
        pipeline.ToVersion.Should().Be(ProcessVersion.From(3));

        var v1State = new OrderStateV1("ORD-123", 250m, false);
        var v3State = pipeline.Migrate(v1State);

        v3State.OrderId.Should().Be("ORD-123");
        v3State.Amount.Should().Be(250m);
        v3State.Currency.Should().Be("USD");
        v3State.TaxRate.Should().Be(0.18m);
        v3State.IsPaid.Should().BeFalse();
        v3State.Note.Should().Be("Migrated from V2");
    }

    [Fact]
    public void Pipeline_WithCustomTransformerStep_ShouldWork()
    {
        var pipeline = ProcessStateMigrationPipeline
            .Create<OrderStateV1>(ProcessVersion.From(1))
            .AddStep(ProcessVersion.From(2), v1 => new OrderStateV2(v1.OrderId, v1.Amount, "EUR", 0.20m, v1.IsPaid))
            .AddStep(ProcessVersion.From(3), v2 => new OrderStateV3(v2.OrderId, v2.Amount, v2.Currency, v2.TaxRate, v2.IsPaid, "Custom Step"))
            .Build<OrderStateV1>();

        var v3 = pipeline.Migrate(new OrderStateV1("ORD-456", 50m, true));

        v3.Currency.Should().Be("EUR");
        v3.TaxRate.Should().Be(0.20m);
        v3.Note.Should().Be("Custom Step");
        pipeline.FromVersion.Should().Be(ProcessVersion.From(1));
        pipeline.ToVersion.Should().Be(ProcessVersion.From(3));
    }

    [Fact]
    public void Pipeline_ShouldThrow_WhenStepHasDiscontinuousVersionMismatch()
    {
        // Arrange: Step 1 goes V1 -> V2, but Step 2 expects FromVersion = V3 (discontinuous jump from V2)
        var v1ToV2 = new OrderStateV1ToV2Migrator();

        var builder = ProcessStateMigrationPipeline
            .Create<OrderStateV1>(ProcessVersion.From(1))
            .AddStep(v1ToV2); // Pipeline is now at version 2

        // Create a migrator with mismatched FromVersion (e.g. 4)
        var mismatchedStep = new DiscontinuousV4ToV5Migrator();

        // Act
        var act = () => builder.AddStep(mismatchedStep);

        // Assert
        act.Should().Throw<InvalidOperationException>()
            .WithMessage("Migrator source version '4' does not match pipeline current version '2'.");
    }

    private sealed class DiscontinuousV4ToV5Migrator : IProcessStateMigrator<OrderStateV2, OrderStateV3>
    {
        public ProcessVersion FromVersion => ProcessVersion.From(4);
        public ProcessVersion ToVersion => ProcessVersion.From(5);

        public OrderStateV3 Migrate(OrderStateV2 sourceState) =>
            new(sourceState.OrderId, sourceState.Amount, sourceState.Currency, sourceState.TaxRate, sourceState.IsPaid, "Discontinuous");
    }

    [Fact]
    public void Pipeline_ShouldThrowOnVersionMismatch()
    {
        var v2ToV3 = new OrderStateV2ToV3Migrator();

        var act = () => ProcessStateMigrationPipeline
            .Create<OrderStateV2>(ProcessVersion.From(1)) // Mismatched starting version (1 instead of 2)
            .AddStep(v2ToV3);

        act.Should().Throw<InvalidOperationException>()
            .WithMessage("*does not match pipeline current version*");
    }

    [Fact]
    public void Pipeline_ShouldThrowOnNullArguments()
    {
        var actNullMigrator = () => ProcessStateMigrationPipeline
            .Create<OrderStateV1>(ProcessVersion.From(1))
            .AddStep<OrderStateV2>((IProcessStateMigrator<OrderStateV1, OrderStateV2>)null!);

        actNullMigrator.Should().Throw<ArgumentNullException>();

        var actNullTransformer = () => ProcessStateMigrationPipeline
            .Create<OrderStateV1>(ProcessVersion.From(1))
            .AddStep<OrderStateV2>(ProcessVersion.From(2), null!);

        actNullTransformer.Should().Throw<ArgumentNullException>();

        var pipeline = ProcessStateMigrationPipeline
            .Create<OrderStateV1>(ProcessVersion.From(1))
            .AddStep(new OrderStateV1ToV2Migrator())
            .Build<OrderStateV1>();

        var actNullSource = () => pipeline.Migrate(null!);
        actNullSource.Should().Throw<ArgumentNullException>();
    }

    [Fact]
    public void ComposedProcessStateMigrator_Constructor_ShouldThrowOnNullMigrateFunc()
    {
        var act = () => new ComposedProcessStateMigrator<OrderStateV1, OrderStateV2>(
            ProcessVersion.From(1), ProcessVersion.From(2), null!);

        act.Should().Throw<ArgumentNullException>().WithParameterName("migrateFunc");
    }

    [Fact]
    public void ComposedProcessStateMigrator_Migrate_ShouldThrowOnNullSourceState()
    {
        var composed = new ComposedProcessStateMigrator<string, string>(
            ProcessVersion.From(1), ProcessVersion.From(2), s => s);

        var act = () => composed.Migrate(null!);
        act.Should().Throw<ArgumentNullException>().WithParameterName("sourceState");
    }
}
