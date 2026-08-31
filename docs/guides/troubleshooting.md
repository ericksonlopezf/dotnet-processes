# Troubleshooting — EricksonLopez.Processes

Common errors, diagnostic steps, and resolution guides for the `EricksonLopez.Processes` ecosystem.

---

## Error: `ConcurrencyConflictException` After All OCC Retries

**Symptom**: `ConcurrencyConflictException` is thrown from `ProcessCoordinator.ExecuteAsync`.

**Root Cause**: Multiple concurrent actors are racing to update the same process instance. The `Revision` CAS fails repeatedly beyond `MaxConcurrencyRetries`.

**Diagnostic Steps**:

1. Check the `process.occ.retries` OpenTelemetry metric — if it is consistently high, the problem is architectural.
2. Verify that your correlation strategy produces unique `ProcessId` / `CorrelationId` per business instance. If multiple events share the same ID accidentally, they collide.
3. Inspect whether multiple consumer threads/pods consume the same event topic partition.

**Resolution**:

```csharp
// Increase retries for high-concurrency scenarios
services.AddProcessCoordinator<MyState>(options =>
{
    options.MaxConcurrencyRetries = 10;
    options.InitialBackoffDelay = TimeSpan.FromMilliseconds(10);
});
```

Alternatively, ensure single-partition consumer assignment per `CorrelationId` at the broker level.

---

## Error: `ProcessNotFoundException`

**Symptom**: `ProcessNotFoundException` is thrown.

**Root Cause**: `LoadByCorrelationIdAsync` returned `null` and `canInitiate: false` was passed to `ExecuteAsync`.

**Resolution**:

```csharp
// For the first event in a saga lifecycle:
var result = await coordinator.ExecuteAsync(
    handler: saga,
    correlation: new MyCorrelation(),
    eventMessage: @event,
    initialStateFactory: e => new MySagaState(...),
    canInitiate: true);   // ← must be true for the first event
```

For subsequent events, `canInitiate: false` is correct — the instance must already exist.

---

## Error: Trim/AOT Warning During Publish

**Symptom**: `ILLink: Trim analysis warning IL2...` or `AOT: NETSDK1138` warnings.

**Root Cause**: A custom `IProcessStateSerializer` implementation uses reflection (e.g., `JsonSerializer.Serialize<T>(value)` without a `JsonSerializerContext`).

**Resolution**: Use `SystemTextJsonProcessStateSerializer` with a registered `JsonSerializerContext`:

```csharp
[JsonSerializable(typeof(MyState))]
[JsonSerializable(typeof(CompensationStep[]))]
internal partial class MySerializerContext : JsonSerializerContext { }

services.AddSystemTextJsonProcessStateSerializer<MyState>(
    jsonTypeInfo: MySerializerContext.Default.MyState);
```

---

## Error: Source Generator Not Generating `AddGeneratedProcesses()`

**Symptom**: Build error: `CS0117: 'ServiceCollectionExtensions' does not contain a definition for 'AddGeneratedProcesses'`.

**Root Cause**: The `EricksonLopez.Processes.Generator` package is not referenced, or the project doesn't have any types annotated with `[SagaDefinition]` / `[ProcessDefinition]`.

**Resolution**:

1. Add package reference:
   ```xml
   <PackageReference Include="EricksonLopez.Processes.Generator" Version="x.y.z">
     <PrivateAssets>all</PrivateAssets>
     <IncludeAssets>runtime; build; native; contentfiles; analyzers; buildtransitive</IncludeAssets>
   </PackageReference>
   ```
2. Verify at least one class has `[SagaDefinition(...)]` or `[ProcessDefinition(...)]`.
3. Clean and rebuild: `dotnet clean && dotnet build`.

---

## Error: OCC Conflict in Development / Tests

**Symptom**: Tests are flaky — sometimes pass, sometimes throw `ConcurrencyConflictException`.

**Root Cause**: Tests share a single `InMemoryProcessStore<TState>` across parallel test cases.

**Resolution**: Create a new `InMemoryProcessStore<TState>` per test case:

```csharp
// ✅ Correct — isolated store per test
[Fact]
public async Task MyTest()
{
    var store = new InMemoryProcessStore<MyState>();
    var coordinator = new ProcessCoordinator<MyState>(store, new ProcessCoordinatorOptions());
    // ...
}
```

---

## Error: Compensation Not Executing

**Symptom**: `ProcessTransitionResult.Compensate(...)` is returned but no compensation steps execute.

**Root Cause 1**: The process class does not implement `ICompensationHandler<TState>`.

**Root Cause 2**: `compensationActions` list is empty.

**Resolution**:

```csharp
// Ensure the saga implements ICompensationHandler
public sealed class MySaga :
    ISaga<MyState>,
    ICompensationHandler<MyState>,   // ← must be present
    IProcessHandler<MyState, MyEvent>
{
    // Both HandleAsync and CompensateAsync must be implemented
}

// Ensure compensationActions is not empty
return ProcessTransitionResult<MyState>.Compensate(
    state,
    compensationActions: [new CompensationAction("StepName", payload)]);  // ← non-empty
```

---

## Error: State Deserialization Fails After Rename

**Symptom**: `System.Text.Json.JsonException: The JSON value could not be converted...` on startup after renaming a state property.

**Root Cause**: Existing stored JSON uses the old property name. `System.Text.Json` does not match by default.

**Resolution Option A**: Increment `ProcessVersion` and add a migrator (recommended).

**Resolution Option B**: Add `[JsonPropertyName("oldName")]` to the new property for backward compatibility:

```csharp
public sealed record MyState(
    [property: JsonPropertyName("old_amount")] decimal NewAmount  // maps old JSON key
) : IProcessState;
```

---

## Diagnostics Checklist

| Symptom | Check |
| :--- | :--- |
| High `process.occ.retries` metric | Increase `MaxConcurrencyRetries`; review concurrency model |
| `ProcessNotFoundException` on second event | Verify `canInitiate: false` and instance was created |
| Effects are emitted but not dispatched | Confirm host application consumes `result.Effects` after `ExecuteAsync` |
| Compensation not triggered | Verify `ICompensationHandler<TState>` is implemented and injected |
| AOT publish fails | Use `JsonSerializerContext`; check `IsAotCompatible=true` |
| Generator method not found | Verify `[SagaDefinition]` / `[ProcessDefinition]` attribute presence |
| State deserialization error | Check if schema changed; add migrator or `JsonPropertyName` |
