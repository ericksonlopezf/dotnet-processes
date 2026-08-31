# Architecture Risk Matrix

| Risk Scenario | Probability | Impact | Mitigation Strategy |
| :--- | :---: | :---: | :--- |
| **Workflow Engine Scope Creep** | Medium | High | Maintain strict boundaries via ADR-001; enforce anti-feature matrix. |
| **Runtime Reflection in Hot Paths** | Low | High | Enforce `<TreatWarningsAsErrors>true</TreatWarningsAsErrors>` with Trim and AOT analyzers; build-time generator. |
| **Concurrency Conflicts under High Load** | High | Medium | Store returns `ConcurrencyConflict`; coordinator supports reload-and-retry loops. |
| **Dual-write Inconsistency** | Medium | High | Process yields `ProcessEffect` intents committed in the same DB transaction as process state update (Outbox pattern). |
| **Third-Party Compensation Failure** | Medium | High | Saga transitions to `ProcessStatus.Failed` with compensation failure details; flags for manual intervention instead of infinite loop. |
| **State Schema Evolution Breaking In-flight Workflows** | Medium | High | Explicit `IProcessStateMigrator<TFrom, TTo>` and process version coexistence (ADR-026, ADR-027). |
| **Memory Allocation / Boxing in Hot Paths** | Medium | Low | Use `readonly struct` IDs, `ValueTask`, and benchmark validation with BenchmarkDotNet. |
