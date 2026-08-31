// Copyright © Erickson Lopez. MIT License.
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using EricksonLopez.Processes.Abstractions;

namespace StateMigrationSample;

public sealed record OrderStateV1(string OrderId, string CustomerName, decimal Amount, bool IsPaid) : IProcessState;

public sealed record OrderStateV2(
    string OrderId,
    string CustomerName,
    decimal Amount,
    string Currency,
    decimal TaxRate,
    bool IsPaid,
    DateTimeOffset MigratedAt) : IProcessState;

public sealed class OrderStateV1ToV2Migrator : IProcessStateMigrator<OrderStateV1, OrderStateV2>
{
    public ProcessVersion FromVersion => ProcessVersion.From(1);
    public ProcessVersion ToVersion => ProcessVersion.From(2);

    public OrderStateV2 Migrate(OrderStateV1 sourceState)
    {
        ArgumentNullException.ThrowIfNull(sourceState);

        return new OrderStateV2(
            OrderId: sourceState.OrderId,
            CustomerName: sourceState.CustomerName,
            Amount: sourceState.Amount,
            Currency: "USD",
            TaxRate: 0.18m,
            IsPaid: sourceState.IsPaid,
            MigratedAt: DateTimeOffset.UtcNow);
    }
}

public static class Program
{
    public static void Main()
    {
        Console.WriteLine("=========================================================");
        Console.WriteLine("  EricksonLopez.Processes — State Migration Sample       ");
        Console.WriteLine("=========================================================");

        var processId = ProcessId.NewId();
        var correlationId = CorrelationId.NewId();
        var now = DateTimeOffset.UtcNow;

        // 1. Existing stored V1 instance
        var v1State = new OrderStateV1("ORD-2024-001", "Alice Corp", 199.99m, true);
        var v1Instance = ProcessInstance<OrderStateV1>.Create(
            id: processId,
            type: ProcessType.From("order.fulfillment"),
            version: ProcessVersion.From(1),
            correlationId: correlationId,
            initialState: v1State,
            now: now);

        Console.WriteLine($"Original Stored Instance (v{v1Instance.Version.Value}):");
        Console.WriteLine($"  OrderId: {v1Instance.State.OrderId}, Customer: {v1Instance.State.CustomerName}, Amount: ${v1Instance.State.Amount}");

        // 2. Deterministic State Migration
        var migrator = new OrderStateV1ToV2Migrator();
        var v2State = migrator.Migrate(v1Instance.State);

        var v2Instance = new ProcessInstance<OrderStateV2>(
            id: v1Instance.Id,
            type: v1Instance.Type,
            version: migrator.ToVersion,
            status: v1Instance.Status,
            revision: v1Instance.Revision.Next(),
            correlationId: v1Instance.CorrelationId,
            createdAt: v1Instance.CreatedAt,
            updatedAt: DateTimeOffset.UtcNow,
            completedAt: v1Instance.CompletedAt,
            state: v2State);

        Console.WriteLine();
        Console.WriteLine($"Migrated Instance (v{v2Instance.Version.Value}):");
        Console.WriteLine($"  OrderId: {v2Instance.State.OrderId}, Customer: {v2Instance.State.CustomerName}");
        Console.WriteLine($"  Amount: ${v2Instance.State.Amount}, Currency: {v2Instance.State.Currency}, TaxRate: {v2Instance.State.TaxRate * 100}%");
        Console.WriteLine($"  Revision: {v2Instance.Revision.Value}, MigratedAt: {v2Instance.State.MigratedAt}");
        Console.WriteLine("=========================================================");
    }
}




