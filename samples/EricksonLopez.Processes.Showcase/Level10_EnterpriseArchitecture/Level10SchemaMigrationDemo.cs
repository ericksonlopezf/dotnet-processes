// Copyright © Erickson Lopez. MIT License.
using System;
using System.Threading.Tasks;
using EricksonLopez.Processes;
using EricksonLopez.Processes.Abstractions;

namespace EricksonLopez.Processes.Showcase.Level10_EnterpriseArchitecture;

// Schema Version 1
public sealed record OrderStateV1(string OrderId, string Customer, decimal Total) : IProcessState;

// Schema Version 2 (added Currency & Tax)
public sealed record OrderStateV2(string OrderId, string Customer, decimal Total, string Currency, decimal Tax) : IProcessState;

// Schema Version 3 (added MigratedAt & AuditTag)
public sealed record OrderStateV3(string OrderId, string Customer, decimal Total, string Currency, decimal Tax, DateTimeOffset MigratedAt, string AuditTag) : IProcessState;

public sealed class OrderStateV1ToV2Migrator : IProcessStateMigrator<OrderStateV1, OrderStateV2>
{
    public ProcessVersion FromVersion => ProcessVersion.From(1);
    public ProcessVersion ToVersion => ProcessVersion.From(2);

    public OrderStateV2 Migrate(OrderStateV1 source)
    {
        ArgumentNullException.ThrowIfNull(source);
        return new OrderStateV2(
            OrderId: source.OrderId,
            Customer: source.Customer,
            Total: source.Total,
            Currency: "USD",
            Tax: source.Total * 0.18m);
    }
}

public sealed class OrderStateV2ToV3Migrator : IProcessStateMigrator<OrderStateV2, OrderStateV3>
{
    public ProcessVersion FromVersion => ProcessVersion.From(2);
    public ProcessVersion ToVersion => ProcessVersion.From(3);

    public OrderStateV3 Migrate(OrderStateV2 source)
    {
        ArgumentNullException.ThrowIfNull(source);
        return new OrderStateV3(
            OrderId: source.OrderId,
            Customer: source.Customer,
            Total: source.Total,
            Currency: source.Currency,
            Tax: source.Tax,
            MigratedAt: DateTimeOffset.UtcNow,
            AuditTag: "AUTO_MIGRATED_PIPELINE");
    }
}

/// <summary>
/// Level 10-A: Enterprise State Migration Pipeline
/// Demonstrates multi-version deterministic migration (V1 -> V2 -> V3) using ProcessStateMigrationPipeline.
/// </summary>
public static class Level10SchemaMigrationDemo
{
    public static Task RunAsync()
    {
        Console.ForegroundColor = ConsoleColor.Cyan;
        Console.WriteLine("================================================================================");
        Console.WriteLine(" LEVEL 10-A: ENTERPRISE STATE MIGRATION PIPELINE (V1 -> V2 -> V3)");
        Console.WriteLine("================================================================================");
        Console.ResetColor();

        // 1. Compose multi-version migration pipeline
        var pipeline = ProcessStateMigrationPipeline
            .Create<OrderStateV1>(ProcessVersion.From(1))
            .AddStep(new OrderStateV1ToV2Migrator())
            .AddStep(new OrderStateV2ToV3Migrator())
            .Build<OrderStateV1>();

        Console.WriteLine($"Pipeline Configured: v{pipeline.FromVersion.Value} -> v{pipeline.ToVersion.Value}");

        // 2. Legacy V1 stored instance
        var v1State = new OrderStateV1("ORD-2024-8899", "Enterprise Client Alpha", 45000.00m);
        var v1Instance = ProcessInstance<OrderStateV1>.Create(
            id: ProcessId.NewId(),
            type: ProcessType.From("order.fulfillment"),
            version: ProcessVersion.From(1),
            correlationId: CorrelationId.NewId(),
            initialState: v1State,
            now: DateTimeOffset.UtcNow.AddYears(-2));

        Console.WriteLine($"\nLegacy Stored Instance (v{v1Instance.Version.Value}):");
        Console.WriteLine($"  OrderId:  {v1Instance.State.OrderId}");
        Console.WriteLine($"  Customer: {v1Instance.State.Customer}");
        Console.WriteLine($"  Total:    {v1Instance.State.Total:C}");

        // 3. Migrate state deterministically through the pipeline in one step
        var v3State = pipeline.Migrate(v1Instance.State);
        var v3Instance = new ProcessInstance<OrderStateV3>(
            id: v1Instance.Id,
            type: v1Instance.Type,
            version: pipeline.ToVersion,
            status: v1Instance.Status,
            revision: v1Instance.Revision.Next(),
            correlationId: v1Instance.CorrelationId,
            createdAt: v1Instance.CreatedAt,
            updatedAt: DateTimeOffset.UtcNow,
            completedAt: v1Instance.CompletedAt,
            state: v3State);

        Console.WriteLine($"\nMigrated Instance (v{v3Instance.Version.Value}):");
        Console.WriteLine($"  OrderId:    {v3Instance.State.OrderId}");
        Console.WriteLine($"  Customer:   {v3Instance.State.Customer}");
        Console.WriteLine($"  Total:      {v3Instance.State.Total:C} {v3Instance.State.Currency}");
        Console.WriteLine($"  Tax:        {v3Instance.State.Tax:C}");
        Console.WriteLine($"  Audit Tag:  {v3Instance.State.AuditTag}");
        Console.WriteLine($"  MigratedAt: {v3Instance.State.MigratedAt:O}");

        Console.ForegroundColor = ConsoleColor.Green;
        Console.WriteLine("\n✔ Level 10-A State Migration Pipeline demo completed successfully.");
        Console.ResetColor();
        return Task.CompletedTask;
    }
}
