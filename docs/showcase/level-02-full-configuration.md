# Showcase — Level 02: Full Configuration

**Level 02** covers comprehensive dependency injection configuration, coordinator options, execution context, strongly-typed value objects, and JSON serialization.

---

## Covered Components

- `services.AddProcesses()`: Central service registration and `ProcessRegistry` catalog.
- `services.AddProcessCoordinator<TState>(options => { ... })`: Coordinator registration with OCC options.
- `ProcessCoordinatorOptions`:
  - `MaxConcurrencyRetries` (default: 3)
  - `InitialBackoffDelay` (default: 50ms, applied as `InitialBackoffDelay * attempt`)
  - `MaxCompensations` (default: 100)
- `ProcessContext`: Injected with `TimeProvider`, `CancellationToken`, `CausationId`, `MessageId`, and ambient metadata in `Items`.
- Strongly-typed value objects (`ProcessId`, `CorrelationId`, `CausationId`, `MessageId`, `ProcessType`, `ProcessVersion`, `Revision`):
  - Span-safe parsing (`Parse`, `TryParse`).
  - Factories (`FromGuid`, `FromString`, `FromInt32`, `FromInt64`).
- `ProcessJsonSerializerOptions`: Trimming-safe and AOT-compatible `System.Text.Json` integration.

---

## Reference DI Code

```csharp
var services = new ServiceCollection();

// Official dependency injection setup
services.AddProcesses();
services.AddProcessCoordinator<OrderState>(options =>
{
    options.MaxConcurrencyRetries = 5;
    options.InitialBackoffDelay = TimeSpan.FromMilliseconds(25);
    options.MaxCompensations = 50;
});

// Abstract TimeProvider for deterministic unit testing
services.AddSingleton<TimeProvider>(TimeProvider.System);
```

---

## Showcase Execution

Executable code: `samples/EricksonLopez.Processes.Showcase/Level02_FullConfiguration/Level02FullConfigurationDemo.cs`
```bash
dotnet run --project samples/EricksonLopez.Processes.Showcase -- --level=2
```
