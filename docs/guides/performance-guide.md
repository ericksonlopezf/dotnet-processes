# Performance Guide — EricksonLopez.Processes

Design principles, zero-allocation techniques, and benchmark throughput analysis for `EricksonLopez.Processes`.

---

## Design Principles

The library is designed for minimal allocations and maximum throughput in hot paths:

1. **`readonly record struct` identifiers** — `ProcessId`, `CorrelationId`, `Revision`, `ProcessType`, `ProcessVersion`, `CausationId`, `MessageId` are all value types. No heap allocation on creation.
2. **`ValueTask<T>` handlers** — All `IProcessHandler<TState, TEvent>.HandleAsync` and `ICompensationHandler<TState>.CompensateAsync` return `ValueTask<T>`. Synchronous completions avoid `Task` allocation.
3. **Static generic dispatch** — `ProcessCoordinator<TState>` and `SagaCompensationEngine` use fully generic, static dispatch paths. No `Activator.CreateInstance`, no `DynamicMethod`, no `Expression.Compile` at runtime.
4. **Span-formattable identifiers** — All identifiers implement `ISpanFormattable`, enabling zero-allocation string formatting in logging pipelines (e.g., `$"Process {processId:D}"` with `StringBuilder`).
5. **`ISpanParsable<T>` identifiers** — Parsing from incoming headers/messages does not allocate via `string.Parse`.
6. **Deterministic builds** — `<Deterministic>true</Deterministic>` ensures byte-for-byte reproducible compilation, enabling strong caching.

---

## Zero-Allocation Identifier Formatting

```csharp
// ✅ Zero-allocation — uses ISpanFormattable
Span<char> buffer = stackalloc char[36];
processId.TryFormat(buffer, out int written, "D", CultureInfo.InvariantCulture);
var slice = buffer[..written];

// ✅ AOT-safe string interpolation — calls TryFormat internally
logger.LogInformation("Executing process {ProcessId}", processId);

// ❌ Avoid — allocates via .ToString() before every log call
logger.LogInformation($"Executing process {processId.Value}");
```

---

## Span-Parsable Correlation Extraction

```csharp
// ✅ Zero-allocation parsing from message headers
public CorrelationId ExtractCorrelationId(IMessageContext ctx)
{
    ReadOnlySpan<char> header = ctx.Headers["X-Correlation-Id"].AsSpan();
    return CorrelationId.TryParse(header, null, out var id)
        ? id
        : CorrelationId.From(Guid.NewGuid());
}
```

---

## OCC Retry Tuning

The `ProcessCoordinatorOptions` controls the **linear** backoff retry loop for OCC conflicts (`delay = InitialBackoffDelay × attempt`):

```csharp
services.AddProcessCoordinator<MyState>(options =>
{
    // Increase retries for very high-concurrency scenarios
    options.MaxConcurrencyRetries = 10;
    // Decrease initial delay for low-latency requirements
    options.InitialBackoffDelay = TimeSpan.FromMilliseconds(10);
});
```

**Backoff formula**: `delay = InitialBackoffDelay × attempt` (linear backoff, no jitter by default).

Custom backoff strategies can be injected via the `backoffStrategy` constructor parameter:

```csharp
// Example: exponential backoff override
new ProcessCoordinator<MyState>(store, options, backoffStrategy: attempt =>
    TimeSpan.FromMilliseconds(50 * Math.Pow(2, attempt - 1)));
```

| Scenario | Recommended `MaxRetries` | Recommended `InitialDelay` |
| :--- | :--- | :--- |
| Low concurrency (<10 concurrent writes/process) | 3 (default) | 50ms (default) |
| Medium concurrency (10–50 concurrent writes/process) | 5–8 | 25ms |
| High concurrency (>50 concurrent writes/process) | 8–10 | 10ms |

> **Tip**: Monitor `process.occ.retries` via OpenTelemetry to tune these values for your workload.

---

## Benchmark Results

Benchmarks are located in `benchmarks/`. Run with:

```bash
dotnet run --project benchmarks/EricksonLopez.Processes.Benchmarks -c Release
```

See [`docs/benchmarks/results.md`](../benchmarks/results.md) for the latest recorded results.

---

## Throughput Targets

Based on the benchmark suite and production design goals:

| Scenario | Target Throughput | Allocations per op |
| :--- | :--- | :--- |
| ProcessId creation (`From(Guid)`) | >50M ops/s | 0 bytes |
| CorrelationId parsing (`TryParse`) | >20M ops/s | 0 bytes |
| `ProcessTransitionResult.Advance` (no effects) | >5M ops/s | <64 bytes |
| `InMemoryProcessStore.SaveAsync` (no OCC conflict) | >2M ops/s | <256 bytes |
| Full coordinator round-trip (in-memory store) | >500K ops/s | <1KB |

---

## Native AOT & Trimming

The library is 100% AOT-compatible when used with `SystemTextJsonProcessStateSerializer` and a registered `JsonSerializerContext`. AOT analyzers are enabled globally via `Directory.Build.props`:

```xml
<IsAotCompatible>true</IsAotCompatible>
<EnableTrimAnalyzer>true</EnableTrimAnalyzer>
<EnableSingleFileAnalyzer>true</EnableSingleFileAnalyzer>
<EnableAotAnalyzer>true</EnableAotAnalyzer>
```

`TreatWarningsAsErrors=true` ensures zero trim warnings ship in any package release.

---

## Storage Performance Tips

- **PostgreSQL**: Use JSONB column type for `StateJson` — faster binary parsing than `JSON`. Enable `pg_trgm` for correlation ID lookups. Use `UNLOGGED TABLE` for ephemeral test processes.
- **SQL Server**: Index the `correlation_id` + `process_type` composite column. Use `READ_COMMITTED_SNAPSHOT` isolation to reduce lock contention.
- **SQLite**: Use `WAL` journal mode (`PRAGMA journal_mode=WAL`) for concurrent read/write workloads.
- **General**: Keep `tableName` table per state type — avoids single-table discriminator hot spots.
