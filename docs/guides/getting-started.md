# Getting Started with EricksonLopez.Processes

## Installation

Install the core abstractions and coordinator packages:

```bash
dotnet add package EricksonLopez.Processes.Abstractions
dotnet add package EricksonLopez.Processes
dotnet add package EricksonLopez.Processes.DependencyInjection
dotnet add package EricksonLopez.Processes.SystemTextJson
```

## Step 1: Define Process State

```csharp
using EricksonLopez.Processes.Abstractions;

public sealed record UserRegistrationState(
    string UserId,
    string Email,
    bool EmailVerified) : IProcessState;
```

## Step 2: Implement Handlers

```csharp
using EricksonLopez.Processes;
using EricksonLopez.Processes.Abstractions;

public sealed record UserRegisteredEvent(Guid UserId, string Email);
public sealed record EmailVerifiedEvent(Guid UserId);

[ProcessDefinition("user.registration", 1)]
public sealed class UserRegistrationProcess :
    IProcess<UserRegistrationState>,
    IProcessHandler<UserRegistrationState, UserRegisteredEvent>,
    IProcessHandler<UserRegistrationState, EmailVerifiedEvent>
{
    public ProcessType Type => ProcessType.From("user.registration");
    public ProcessVersion Version => ProcessVersion.Initial;

    public ValueTask<ProcessTransitionResult<UserRegistrationState>> HandleAsync(
        UserRegistrationState state,
        UserRegisteredEvent eventMessage,
        ProcessContext context)
    {
        var updated = state with { UserId = eventMessage.UserId.ToString(), Email = eventMessage.Email };
        var effect = new ProcessEffect.Command(new SendVerificationEmailCommand(eventMessage.UserId, eventMessage.Email));

        return ValueTask.FromResult(ProcessTransitionResult<UserRegistrationState>.Advance(
            updated,
            ProcessStatus.Running,
            effects: [effect]));
    }

    public ValueTask<ProcessTransitionResult<UserRegistrationState>> HandleAsync(
        UserRegistrationState state,
        EmailVerifiedEvent eventMessage,
        ProcessContext context)
    {
        var updated = state with { EmailVerified = true };
        return ValueTask.FromResult(ProcessTransitionResult<UserRegistrationState>.Complete(updated));
    }
}
```

## Step 3: Register in Dependency Injection

```csharp
builder.Services.AddProcesses();
builder.Services.AddProcessCoordinator<UserRegistrationState>();
```
