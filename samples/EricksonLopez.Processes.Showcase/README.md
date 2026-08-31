# EricksonLopez.Processes Showcase

The official executable reference implementation and learning architecture for **EricksonLopez.Processes**.

This showcase demonstrates 100% of the library's public API across 11 progressive learning levels, adhering strictly to zero-reflection, trimming-safe, and Native AOT-ready design constraints in .NET 10.

---

## How to Run

### Run All Progressive Levels (Automated)
```bash
dotnet run --project samples/EricksonLopez.Processes.Showcase -- --all
```

### Run a Specific Level
```bash
dotnet run --project samples/EricksonLopez.Processes.Showcase -- --level=3
```
Levels supported: `0` (or `conceptual`), `1` (or `quickstart`), `2` (or `config`), `3` (or `sagas`), `4` (or `integration`), `5` (or `concurrency`), `6` (or `errors`), `7` (or `performance`), `8` (or `customization`), `9` (or `storage`), `10` (or `enterprise`).

### Interactive Menu
```bash
dotnet run --project samples/EricksonLopez.Processes.Showcase
```

---

## Progressive Architecture

| Level | Directory | Topic | Key Types & APIs Covered |
| :--- | :--- | :--- | :--- |
| **00** | `Level00_Conceptual` | Architectural Axioms & Comparison | State persistence vs runtime locks, pure functions, OCC CAS tokens |
| **01** | `Level01_QuickStart` | Minimal Functional Process | `IProcessState`, `IProcess<TState>`, `IProcessHandler`, `IProcessCorrelation`, `InMemoryProcessStore`, `ProcessCoordinator` |
| **02** | `Level02_FullConfiguration` | DI, Options, Time, JSON & Value Objects | `services.AddProcesses()`, `AddProcessCoordinator`, `ProcessCoordinatorOptions`, `TimeProvider`, `SystemTextJsonProcessStateSerializer`, `ProcessJsonSerializerOptions`, `ProcessRegistry`, `ProcessContext.Create()`, value object `Parse()`/`TryParse()`/operators |
| **03** | `Level03_RealWorldUseCases` | Sagas & Long-Running Workflows | `ISaga<TState>`, `ICompensationHandler`, LIFO reverse compensation, `ProcessEffect.ScheduleTimeout`, `ProcessStatus.Suspended` |
| **04** | `Level04_AdvancedIntegration` | Outbox, Mediator & Event Bus | `IProcessOutboxDispatcher`, `OutboxProcessDispatcher`, `IMediatorProcessDispatcher`, `MediatorProcessDispatcher`, `IEventProcessDispatcher` |
| **05-A** | `Level05_ProcessingAndConcurrency` | OCC CAS & Composite Keys | `FaultInjectingProcessStore`, automatic backoff retry loop, `CompositeCorrelationKey`, deterministic UUIDv5 `ToCorrelationId()` |
| **05-C** | `Level05_ProcessingAndConcurrency` | All ProcessEffect Variants | `ProcessEffect.Command.GetPayload<T>()`, `ProcessEffect.Event.GetPayload<T>()`, `ProcessEffect.ScheduleTimeout.GetTrigger<T>()`, `ProcessEffect.Compensation`, `CompensationStep.ExtractPayload<T>()`, all `CreateXxx<T>()` factory methods |
| **06-A/B** | `Level06_ErrorHandlingAndRecovery` | Exception Taxonomy & Resilience | `ProcessNotFoundException`, `ConcurrencyConflictException`, `InvalidProcessTransitionException`, `CompensationFailedException` |
| **06-C** | `Level06_ErrorHandlingAndRecovery` | All ProcessSaveResult Values | `ProcessSaveResult.Success`, `.ConcurrencyConflict`, `.NotFound`, `.PersistenceError`, `FaultInjectingProcessStore.ForcedSaveResult`, `.ForcedExistsResult`, `.ExceptionToThrowOnSave` |
| **07-A** | `Level07_ScalabilityAndPerformance` | Zero-Allocation High Throughput | >800k state transitions/sec in memory |
| **07-B** | `Level07_ScalabilityAndPerformance` | ProcessDiagnostics & OpenTelemetry | `ProcessDiagnostics.ActivitySource`, all `RecordXxx()` metric methods, BCL `ActivitySource` & `Meter` integration |
| **08-A/B** | `Level08_Customization` | Custom Extensibility Ports | Custom `IProcessStore<TState>`, custom `IProcessStateSerializer<TState>` |
| **08-C** | `Level08_Customization` | ISagaSnapshotRepository | `ISagaSnapshotRepository<TState>.SaveSnapshotAsync()`, `GetLatestSnapshotAsync()` |
| **08-D** | `Level08_Customization` | ProcessStateRecord Mapping | `ProcessStateRecord` — all properties, bidirectional `ProcessInstance<TState>` ↔ `ProcessStateRecord` mapping |
| **09-A** | `Level09_ExtensionsAndStorage` | Relational Storage Engines | `AddPostgreSqlProcessStore`, `AddSqlServerProcessStore`, `AddSqliteProcessStore`, `AddMySqlProcessStore`, `AddMariaDbProcessStore`, `AddOracleProcessStore` |
| **09-B** | `Level09_ExtensionsAndStorage` | Cross-Library Identifier Bridging | `CorrelationId.ToEventsCorrelationId()`, `ToProcessesCorrelationId()`, `CausationId.ToEventsCausationId()`, `ToProcessesCausationId()` |
| **10** | `Level10_EnterpriseArchitecture` | Schema Migration & Native AOT | `ProcessStateMigrationPipeline`, `IProcessStateMigrator`, Roslyn source generation (`[ProcessDefinition]`, `AddGeneratedProcesses`) |
