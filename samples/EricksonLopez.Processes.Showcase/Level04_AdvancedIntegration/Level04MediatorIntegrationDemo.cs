// Copyright © Erickson Lopez. MIT License.
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using EricksonLopez.Mediator;
using EricksonLopez.Processes.Abstractions;
using EricksonLopez.Processes.Mediator;

namespace EricksonLopez.Processes.Showcase.Level04_AdvancedIntegration;

public sealed record ProcessNotification(string EventName, Guid AggregateId) : INotification;
public sealed record ProcessCommand(string CommandName, Guid AggregateId) : ICommand<bool>;

/// <summary>
/// Lightweight in-memory test double for IMediator.
/// </summary>
public sealed class InMemoryMediator : IMediator
{
    public List<INotification> PublishedNotifications { get; } = new();
    public List<object> SentCommands { get; } = new();

    public ValueTask<TResponse> Send<TResponse>(ICommand<TResponse> command, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(command);
        SentCommands.Add(command);
        if (typeof(TResponse) == typeof(bool))
        {
            return ValueTask.FromResult((TResponse)(object)true);
        }
        return ValueTask.FromResult(default(TResponse)!);
    }

    public ValueTask<TResponse> Send<TResponse>(IQuery<TResponse> query, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(query);
        return ValueTask.FromResult(default(TResponse)!);
    }

    public ValueTask<TResponse> SendCommand<TCommand, TResponse>(TCommand command, CancellationToken cancellationToken = default)
        where TCommand : ICommand<TResponse>
    {
        ArgumentNullException.ThrowIfNull(command);
        SentCommands.Add(command);
        if (typeof(TResponse) == typeof(bool))
        {
            return ValueTask.FromResult((TResponse)(object)true);
        }
        return ValueTask.FromResult(default(TResponse)!);
    }

    public ValueTask<TResponse> SendQuery<TQuery, TResponse>(TQuery query, CancellationToken cancellationToken = default)
        where TQuery : IQuery<TResponse>
    {
        ArgumentNullException.ThrowIfNull(query);
        return ValueTask.FromResult(default(TResponse)!);
    }

    public ValueTask Publish(INotification notification, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(notification);
        PublishedNotifications.Add(notification);
        return ValueTask.CompletedTask;
    }

    public ValueTask Publish<TNotification>(TNotification notification, CancellationToken cancellationToken = default)
        where TNotification : INotification
    {
        ArgumentNullException.ThrowIfNull(notification);
        PublishedNotifications.Add(notification);
        return ValueTask.CompletedTask;
    }

    public IAsyncEnumerable<TResponse> CreateStream<TResponse>(IStreamRequest<TResponse> request, CancellationToken cancellationToken = default) =>
        throw new NotImplementedException();
}

/// <summary>
/// Level 4-B: In-Memory Mediator Integration
/// Demonstrates dispatching ProcessEffects directly into IMediator via IMediatorProcessDispatcher.
/// </summary>
public static class Level04MediatorIntegrationDemo
{
    public static async Task RunAsync()
    {
        Console.ForegroundColor = ConsoleColor.Cyan;
        Console.WriteLine("================================================================================");
        Console.WriteLine(" LEVEL 04-B: IN-MEMORY MEDIATOR INTEGRATION (IMediatorProcessDispatcher)");
        Console.WriteLine("================================================================================");
        Console.ResetColor();

        var mediator = new InMemoryMediator();
        var dispatcher = new MediatorProcessDispatcher(mediator);
        var processId = ProcessId.NewId();

        dispatcher.OnUnrecognizedPayload = (pId, effect, payload) =>
        {
            Console.WriteLine($"  [Warning] Unrecognized effect payload for process '{pId}': {payload?.GetType().Name}");
        };

        var effects = new ProcessEffect[]
        {
            ProcessEffect.CreateCommand(new ProcessCommand("ProvisionTenantResource", processId.Value)),
            ProcessEffect.CreateEvent(new ProcessNotification("TenantResourceProvisioned", processId.Value)),
            ProcessEffect.CreateTimeout(TimeSpan.FromMinutes(10), new ProcessNotification("ProvisioningTimeout", processId.Value))
        };

        Console.WriteLine($"Dispatching {effects.Length} ProcessEffects via MediatorProcessDispatcher...");

        await dispatcher.DispatchEffectsAsync(effects, processId);

        Console.WriteLine();
        Console.WriteLine($"Sent Commands Count:        {mediator.SentCommands.Count}");
        Console.WriteLine($"Published Notifications:    {mediator.PublishedNotifications.Count}");

        Console.ForegroundColor = ConsoleColor.Green;
        Console.WriteLine("✔ Level 04-B Mediator integration completed successfully.");
        Console.ResetColor();
    }
}
