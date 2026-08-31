// Copyright © Erickson Lopez. MIT License.
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using EricksonLopez.Outbox;
using EricksonLopez.Outbox.Persistence;
using EricksonLopez.Processes.Abstractions;
using EricksonLopez.Processes.Outbox;

namespace EricksonLopez.Processes.Showcase.Level04_AdvancedIntegration;

/// <summary>
/// In-memory test double for IOutbox to demonstrate outbox dispatching without external broker infrastructure.
/// </summary>
public sealed class InMemoryOutbox : IOutbox
{
    public List<(object Payload, OutboxMessageMetadata Metadata, DateTimeOffset? DeliverAt)> StoredMessages { get; } = new();

    public ValueTask StoreAsync<TMessage>(
        TMessage message,
        IOutboxTransactionContext transaction,
        CancellationToken cancellationToken = default)
        where TMessage : notnull
    {
        ArgumentNullException.ThrowIfNull(message);
        ArgumentNullException.ThrowIfNull(transaction);

        StoredMessages.Add((message, new OutboxMessageMetadata(string.Empty, string.Empty, typeof(TMessage).Name), null));
        return ValueTask.CompletedTask;
    }

    public ValueTask StoreAsync<TMessage>(
        ReadOnlyMemory<TMessage> messages,
        IOutboxTransactionContext transaction,
        CancellationToken cancellationToken = default)
        where TMessage : notnull
    {
        ArgumentNullException.ThrowIfNull(transaction);
        for (var i = 0; i < messages.Length; i++)
        {
            StoredMessages.Add((messages.Span[i]!, new OutboxMessageMetadata(string.Empty, string.Empty, typeof(TMessage).Name), null));
        }
        return ValueTask.CompletedTask;
    }

    public ValueTask StoreAsync<TMessage>(
        TMessage message,
        IOutboxTransactionContext transaction,
        OutboxMessageMetadata metadata,
        DateTimeOffset? deliverAt = null,
        CancellationToken cancellationToken = default)
        where TMessage : notnull
    {
        ArgumentNullException.ThrowIfNull(message);
        ArgumentNullException.ThrowIfNull(transaction);

        StoredMessages.Add((message, metadata, deliverAt));
        return ValueTask.CompletedTask;
    }

    public OutboxMessageBuilder<TMessage> Publish<TMessage>(TMessage message)
        where TMessage : notnull
    {
        throw new NotImplementedException("Publish builder is not used in this in-memory showcase demo.");
    }
}

public sealed class DummyOutboxTransaction : IOutboxTransactionContext
{
    public object Connection => new object();
    public object Transaction => new object();
}

/// <summary>
/// Level 4-A: Transactional Outbox Integration
/// Demonstrates dispatching process effects reliably via IProcessOutboxDispatcher and OutboxProcessDispatcher.
/// </summary>
public static class Level04OutboxIntegrationDemo
{
    public static async Task RunAsync()
    {
        Console.ForegroundColor = ConsoleColor.Cyan;
        Console.WriteLine("================================================================================");
        Console.WriteLine(" LEVEL 04-A: TRANSACTIONAL OUTBOX INTEGRATION (IProcessOutboxDispatcher)");
        Console.WriteLine("================================================================================");
        Console.ResetColor();

        var outbox = new InMemoryOutbox();
        var dispatcher = new OutboxProcessDispatcher(outbox);
        var transaction = new DummyOutboxTransaction();
        var processId = ProcessId.NewId();

        // 1. Prepare multiple process side effects
        var effects = new ProcessEffect[]
        {
            ProcessEffect.CreateCommand(new { Action = "ChargeCreditCard", Amount = 199.95m }, "ChargeCreditCardCommand"),
            ProcessEffect.CreateEvent(new { OrderId = processId.Value, Status = "OrderConfirmed" }, "OrderConfirmedEvent"),
            ProcessEffect.CreateTimeout(TimeSpan.FromHours(24), new { OrderId = processId.Value, Trigger = "SlaExpired" }, "SlaExpiredTrigger"),
            ProcessEffect.CreateCompensation("ChargeCreditCard", new { RefundAmount = 199.95m })
        };

        Console.WriteLine($"Dispatching {effects.Length} ProcessEffects to transactional Outbox...");

        // 2. Dispatch all effects atomically within the transaction
        await dispatcher.DispatchEffectsAsync(effects, processId, transaction);

        // 3. Verify messages stored in outbox with proper metadata
        Console.WriteLine();
        Console.WriteLine($"Total Outbox Messages Stored: {outbox.StoredMessages.Count}");
        foreach (var (payload, meta, deliverAt) in outbox.StoredMessages)
        {
            var delayInfo = deliverAt.HasValue ? $" (DeliverAt: {deliverAt.Value:O})" : "";
            Console.WriteLine($"  • Type: '{meta.MessageType}' | CorrelationId: '{meta.CorrelationId}'{delayInfo}");
        }

        Console.ForegroundColor = ConsoleColor.Green;
        Console.WriteLine("✔ Level 04-A Transactional Outbox integration completed successfully.");
        Console.ResetColor();
    }
}
