// Copyright © Erickson Lopez. MIT License.
using System;
using System.Threading.Tasks;
using EricksonLopez.Processes;
using EricksonLopez.Processes.Abstractions;
using EricksonLopez.Processes.Testing;

namespace EricksonLopez.Processes.Showcase.Level01_QuickStart;

public sealed record UserOnboardingState(
    string UserId,
    string Email,
    bool WelcomeEmailSent,
    DateTimeOffset? CompletedAt) : IProcessState
{
    public static UserOnboardingState Initial(string userId, string email) =>
        new(userId, email, false, null);
}

public sealed record UserRegisteredEvent(Guid UserId, string Email);

public sealed class UserOnboardingProcess :
    IProcess<UserOnboardingState>,
    IProcessHandler<UserOnboardingState, UserRegisteredEvent>
{
    public ProcessType Type => ProcessType.From("user.onboarding");
    public ProcessVersion Version => ProcessVersion.Initial;

    public ValueTask<ProcessTransitionResult<UserOnboardingState>> HandleAsync(
        UserOnboardingState state,
        UserRegisteredEvent eventMessage,
        ProcessContext context)
    {
        Console.WriteLine($"  [ProcessHandler] Processing UserRegisteredEvent for User ID '{eventMessage.UserId}' ({eventMessage.Email}).");

        var updatedState = state with
        {
            UserId = eventMessage.UserId.ToString(),
            Email = eventMessage.Email,
            WelcomeEmailSent = true,
            CompletedAt = context.Now
        };

        // Emit an outbound command effect to send the welcome email
        var effect = ProcessEffect.CreateCommand(new { Action = "SendWelcomeEmail", To = eventMessage.Email });

        return ValueTask.FromResult(ProcessTransitionResult<UserOnboardingState>.Complete(
            updatedState,
            effects: [effect]));
    }
}

public sealed class UserRegisteredCorrelation : IProcessCorrelation<UserRegisteredEvent>
{
    public ProcessId ExtractProcessId(UserRegisteredEvent @event) => ProcessId.From(@event.UserId);
    public CorrelationId ExtractCorrelationId(UserRegisteredEvent @event) => CorrelationId.From(@event.UserId.ToString());
}

/// <summary>
/// Level 1: Quick Start
/// Demonstrates minimal setup: state record, event handler, correlation extractor, and coordinator execution.
/// </summary>
public static class QuickStartDemo
{
    public static async Task RunAsync()
    {
        Console.ForegroundColor = ConsoleColor.Cyan;
        Console.WriteLine("================================================================================");
        Console.WriteLine(" LEVEL 01: QUICK START (MINIMAL FUNCTIONAL PROCESS MANAGER)");
        Console.WriteLine("================================================================================");
        Console.ResetColor();

        // 1. Initialize thread-safe test store and coordinator
        var store = new InMemoryProcessStore<UserOnboardingState>();
        var coordinator = new ProcessCoordinator<UserOnboardingState>(store);
        var process = new UserOnboardingProcess();
        var correlation = new UserRegisteredCorrelation();

        var userId = Guid.NewGuid();
        var @event = new UserRegisteredEvent(userId, "ada.lovelace@example.com");

        Console.WriteLine($"Initiating process '{process.Type}' with ProcessId '{userId}'...");

        // 2. Execute process transition
        var result = await coordinator.ExecuteAsync(
            handler: process,
            correlation: correlation,
            eventMessage: @event,
            initialStateFactory: e => UserOnboardingState.Initial(e.UserId.ToString(), e.Email),
            canInitiate: true);

        // 3. Inspect persistent results
        Console.WriteLine();
        Console.WriteLine($"Result IsSuccess: {result.IsSuccess}");
        Console.WriteLine($"Process Status:   {result.Instance.Status}");
        Console.WriteLine($"Revision Token:   {result.Instance.Revision.Value}");
        Console.WriteLine($"State User ID:    {result.Instance.State.UserId}");
        Console.WriteLine($"Welcome Sent:     {result.Instance.State.WelcomeEmailSent}");
        Console.WriteLine($"Emitted Effects:  {result.Effects.Count} (Type: {result.Effects[0].GetType().Name})");

        Console.ForegroundColor = ConsoleColor.Green;
        Console.WriteLine("✔ Quick Start completed successfully.");
        Console.ResetColor();
    }
}
