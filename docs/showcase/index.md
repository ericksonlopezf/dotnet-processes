# Showcase Official Documentation — EricksonLopez.Processes

Progressive learning guide and executable documentation for the official reference architecture of **EricksonLopez.Processes** in .NET 10.

---

## Progressive Pedagogical Structure (Levels 00 to 10)

| Level | Guide Document | Code Module | Topic and Demonstrated Capabilities |
| :---: | :--- | :--- | :--- |
| **00** | [Level 00: Conceptual Architecture](level-00-conceptual.md) | `Level00_Conceptual/` | Architectural axioms, functional purity, OCC vs lock models, alternative comparison. |
| **01** | [Level 01: Quick Start](level-01-quickstart.md) | `Level01_QuickStart/` | Minimal working process: `IProcessState`, `IProcessHandler`, `IProcessCorrelation`, `InMemoryProcessStore`, `ProcessCoordinator`. |
| **02** | [Level 02: Full Configuration](level-02-full-configuration.md) | `Level02_FullConfiguration/` | Dependency injection (`AddProcesses`, `AddProcessCoordinator`), `ProcessCoordinatorOptions`, `ProcessContext`, value object parsing. |
| **03** | [Level 03: Real-World Use Cases](level-03-real-world-use-cases.md) | `Level03_RealWorldUseCases/` | Distributed sagas (`ISaga`, `ICompensationHandler`, LIFO rollback) and invoice certification process managers. |
| **04** | [Level 04: Advanced Integration](level-04-advanced-integration.md) | `Level04_AdvancedIntegration/` | Side-effect dispatching: `OutboxProcessDispatcher`, `MediatorProcessDispatcher`, and `EventProcessDispatcher`. |
| **05** | [Level 05: Processing and Concurrency](level-05-processing-and-concurrency.md) | `Level05_ProcessingAndConcurrency/` | OCC CAS retry loop, `FaultInjectingProcessStore`, `CompositeCorrelationKey` (SHA-256 UUIDv5), all `ProcessEffect` variants. |
| **06** | [Level 06: Error Handling and Recovery](level-06-error-handling-and-recovery.md) | `Level06_ErrorHandlingAndRecovery/` | Complete exception taxonomy (`ProcessNotFoundException`, `ConcurrencyConflictException`, etc.) and `ProcessSaveResult` enum values. |
| **07** | [Level 07: Scalability and Performance](level-07-scalability-and-performance.md) | `Level07_ScalabilityAndPerformance/` | Zero-allocation throughput (>800k transitions/sec) and native OpenTelemetry observability (`ProcessDiagnostics`). |
| **08** | [Level 08: Customization and Extensibility](level-08-customization.md) | `Level08_Customization/` | Custom stores, binary serializers, snapshot repositories (`ISagaSnapshotRepository`), and mapping via `ProcessStateRecord`. |
| **09** | [Level 09: Extensions and Storage](level-09-extensions-and-storage.md) | `Level09_ExtensionsAndStorage/` | 6 RDBMS database engines (PostgreSql, SqlServer, Sqlite, MySql, MariaDb, Oracle) and cross-library identifier bridges. |
| **10** | [Level 10: Enterprise Architecture](level-10-enterprise-architecture.md) | `Level10_EnterpriseArchitecture/` | Zero-downtime hot schema migration pipeline (`ProcessStateMigrationPipeline`) and Native AOT compilation with Roslyn Source Generators. |

---

## Running the Showcase

### Run all progressive levels:
```bash
dotnet run --project samples/EricksonLopez.Processes.Showcase -- --all
```

### Run a specific level (by number or name):
```bash
dotnet run --project samples/EricksonLopez.Processes.Showcase -- --level=3
```
Supported level filters: `0` (conceptual), `1` (quickstart), `2` (config), `3` (sagas), `4` (integration), `5` (concurrency), `6` (errors), `7` (performance), `8` (customization), `9` (storage), `10` (enterprise).

### Interactive menu:
```bash
dotnet run --project samples/EricksonLopez.Processes.Showcase
```
