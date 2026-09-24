# Architecture & Diagrams — EricksonLopez.Processes

Technical documentation and architectural diagrams for **EricksonLopez.Processes**, modeled rigorously from the library source code and verified against .NET 10 (C# Preview, Native AOT, and Trimming-safe).

---

## 1. Clean Architecture & System Layers

```mermaid
graph TD
    subgraph INGRESS["1. Transport & Ingress Layer (Host / Message Broker)"]
        MSG["External Message / Domain Event"] --> EXT["IProcessCorrelation&lt;TEvent&gt;"]
        EXT --> IDS["ProcessId, CorrelationId, CausationId"]
    end

    subgraph ENGINE["2. Domain & Orchestration Engine (Core Runtime)"]
        IDS --> COORD["ProcessCoordinator&lt;TState&gt;"]
        COORD --> CTX["ProcessContext (TimeProvider, Ambient Items)"]
        CTX --> HANDLER["IProcessHandler&lt;TState, TEvent&gt; / ICompensationHandler&lt;TState&gt;"]
        HANDLER --> RES["ProcessTransitionResult&lt;TState&gt; (Advance, Complete, Fail, Suspend, Compensate)"]
        RES --> ENGINE_OCC["OCC CAS Retry Loop"]
    end

    subgraph PERSISTENCE["3. Persistence Layer (Storage Ports & Adapters)"]
        ENGINE_OCC --> STORE_PORT["IProcessStore&lt;TState&gt; (SaveAsync, GetByIdAsync, ExistsAsync)"]
        STORE_PORT --> SER["IProcessStateSerializer&lt;TState&gt; (System.Text.Json AOT)"]
        STORE_PORT --> ADAPTERS["RDBMS Adapters (PostgreSQL, SQL Server, SQLite, MySQL, MariaDB, Oracle)"]
    end

    subgraph DISPATCH["4. Side-Effect Intent Dispatch Layer"]
        RES --> EFFECTS["ProcessEffect Collection"]
        EFFECTS -->|Command| DISP_MED["MediatorProcessDispatcher → IMediator"]
        EFFECTS -->|Event| DISP_EVT["EventProcessDispatcher → IEventPublisher"]
        EFFECTS -->|Durable Outbox| DISP_OBX["OutboxProcessDispatcher → IProcessOutbox"]
        EFFECTS -->|Timeout| SCHED["Host Scheduler / Background Worker"]
    end

    subgraph OBSERVABILITY["5. Observability & Telemetry Layer"]
        COORD --> DIAG["ProcessDiagnostics (ActivitySource & Metrics OpenTelemetry)"]
    end
```

---

## 2. Component Dependency Graph

```mermaid
graph TD
    subgraph "Tier 0 — Pure Contracts (Zero Dependencies)"
        ABS["EricksonLopez.Processes.Abstractions<br/>(net10.0 + netstandard2.0)"]
    end

    subgraph "Tier 1 — Core Engine & Tooling"
        CORE["EricksonLopez.Processes<br/>(net10.0)"]
        GEN["EricksonLopez.Processes.Generator<br/>(Roslyn Source Generator)"]
        ANA["EricksonLopez.Processes.Analyzers<br/>(Roslyn Diagnostic Analyzer)"]
    end

    subgraph "Tier 2 — Infrastructure & DI"
        DI["EricksonLopez.Processes.DependencyInjection<br/>(net10.0)"]
        STJ["EricksonLopez.Processes.SystemTextJson<br/>(net10.0)"]
        TST["EricksonLopez.Processes.Testing<br/>(net10.0)"]
    end

    subgraph "Tier 3 — Side-Effect Dispatch Bridges"
        EVT["EricksonLopez.Processes.Events<br/>(net10.0)"]
        MED["EricksonLopez.Processes.Mediator<br/>(net10.0)"]
        OBX["EricksonLopez.Processes.Outbox<br/>(net10.0)"]
    end

    subgraph "Tier 4 — RDBMS Persistence Adapters"
        PG["Storage.PostgreSql"]
        SS["Storage.SqlServer"]
        SL["Storage.Sqlite"]
        MY["Storage.MySql"]
        MA["Storage.MariaDb"]
        OR["Storage.Oracle"]
    end

    ABS --> CORE
    ABS --> DI
    ABS --> STJ
    ABS --> TST
    ABS --> EVT
    ABS --> MED
    ABS --> OBX
    ABS --> PG
    ABS --> SS
    ABS --> SL
    ABS --> MY
    ABS --> MA
    ABS --> OR
    CORE --> DI
    CORE --> EVT
    CORE --> MED
    CORE --> OBX
```

---

## 3. Finite State Machine Lifecycle (`ProcessStatus`)

```mermaid
stateDiagram-v2
    [*] --> Initialized : In-memory instantiation (ProcessInstance.Create)
    Initialized --> Running : First transition executed (canInitiate=true)
    Running --> Running : Advance(Running)
    Running --> Suspended : Suspend() (Awaiting event or timeout deadline)
    Suspended --> Running : Event received / Timeout expired
    Running --> Completed : Complete() (Terminal success)
    Running --> Compensating : Compensate() (Failure triggering Saga rollback)
    Compensating --> Compensating : AdvanceCompensation(Compensating)
    Compensating --> Compensated : AdvanceCompensation(Compensated)
    Compensating --> Failed : CompensationFailedException / Retries exhausted
    Running --> Failed : Fail() (Unrecoverable terminal failure)
    Completed --> [*]
    Compensated --> [*]
    Failed --> [*]
```

> **Terminal States**: `Completed`, `Compensated`, and `Failed`. Once an instance reaches a terminal state, the coordinator rejects any subsequent transition by throwing `InvalidProcessTransitionException`.

---

## 4. OCC CAS Execution Sequence

```mermaid
sequenceDiagram
    autonumber
    participant Host as Host Application
    participant Coord as ProcessCoordinator&lt;TState&gt;
    participant Store as IProcessStore&lt;TState&gt;
    participant Handler as IProcessHandler&lt;TState, TEvent&gt;
    participant Diag as ProcessDiagnostics

    Host->>Coord: ExecuteAsync(handler, correlation, event, canInitiate)
    loop OCC Retries (up to MaxConcurrencyRetries)
        Coord->>Store: GetByIdAsync(processId, ct)
        Store-->>Coord: ProcessInstance&lt;TState&gt;?
        alt Instance not found and canInitiate == false
            Coord-->>Host: throw ProcessNotFoundException
        else Instance loaded or created
            Coord->>Handler: HandleAsync(state, event, context)
            Handler-->>Coord: ProcessTransitionResult&lt;TState&gt;
            Coord->>Store: SaveAsync(instanceWithNextRevision, ct)
            alt SaveAsync returns Success
                Store-->>Coord: ProcessSaveResult.Success
                Coord->>Diag: RecordTransitionDuration(...)
                Coord-->>Host: ProcessExecutionResult&lt;TState&gt; (Instance, Effects)
            else SaveAsync returns ConcurrencyConflict
                Store-->>Coord: ProcessSaveResult.ConcurrencyConflict
                Coord->>Diag: RecordConcurrencyConflict(...)
                Note over Coord: Linear backoff delay (InitialBackoffDelay * attempt)
            else SaveAsync returns PersistenceError or NotFound
                Store-->>Coord: PersistenceError / NotFound
                Coord-->>Host: Returns ProcessExecutionResult with corresponding SaveResult
            end
        end
    end
    Note over Coord: If OCC retries exhausted → throw ConcurrencyConflictException
```

---

## 5. Reverse-Order LIFO Saga Compensation Flow

```mermaid
sequenceDiagram
    autonumber
    participant Coord as ProcessCoordinator&lt;TState&gt;
    participant Engine as SagaCompensationEngine
    participant Saga as ISaga&lt;TState&gt; &amp; ICompensationHandler&lt;TState&gt;
    participant Store as IProcessStore&lt;TState&gt;

    Note over Coord: Failure event received → HandleAsync yields Compensate()
    Coord->>Engine: CompensateAsync(processId, recordedCompensations, saga, ct)
    Note over Engine: Inverts compensation history stack (LIFO order)
    loop For each CompensationStep in reverse LIFO order
        Engine->>Saga: CompensateAsync(state, action, context)
        Saga-->>Engine: ProcessTransitionResult&lt;TState&gt;.Advance(Compensating, effects)
        Engine->>Store: SaveAsync(instance in Compensating status)
    end
    Note over Engine: All compensation steps executed successfully
    Engine->>Store: SaveAsync(instance in Compensated status)
    Engine-->>Coord: ProcessExecutionResult (Status = Compensated)
```

---

## 6. Decoupled Side-Effect Intent Dispatching

```mermaid
flowchart LR
    subgraph TRANSITION["Pure Process Transition"]
        TR["HandleAsync(...)"] --> RES["ProcessExecutionResult.Effects"]
    end

    subgraph EFFECT_DISPATCHERS["Effect Dispatchers"]
        RES -->|ProcessEffect.Command| MED["MediatorProcessDispatcher"]
        RES -->|ProcessEffect.Event| EVT["EventProcessDispatcher"]
        RES -->|Any ProcessEffect| OBX["OutboxProcessDispatcher"]
        RES -->|ProcessEffect.ScheduleTimeout| SCH["Host Application Scheduler"]
    end

    subgraph DOWNSTREAM["Downstream Systems"]
        MED --> HANDLERS_CQRS["CQRS Command Handlers (IMediator)"]
        EVT --> BUS["Enterprise Event Bus (IEventPublisher)"]
        OBX --> OUTBOX_TABLE["Transactional Outbox Table (Zero Dual-Write)"]
        SCH --> TIMER_QUEUE["Delayed Message Queue / Scheduler Worker"]
    end
```

---

## 7. Relational Persistence Adapters and CAS Dialects

```mermaid
graph TD
    STORE["IProcessStore&lt;TState&gt;"]
    
    STORE --> PG["PostgreSqlProcessStore<br/>• Driver: Npgsql<br/>• CAS: WHERE revision = @Revision<br/>• Serialization: Native JSONB"]
    STORE --> SS["SqlServerProcessStore<br/>• Driver: Microsoft.Data.SqlClient<br/>• CAS: WHERE Revision = @Revision<br/>• Optimized with Snapshot isolation"]
    STORE --> SL["SqliteProcessStore<br/>• Driver: Microsoft.Data.Sqlite<br/>• Embedded zero-allocation<br/>• Ideal for testing and edge"]
    STORE --> MY["MySqlProcessStore<br/>• Driver: MySqlConnector<br/>• CAS: WHERE revision = @Revision"]
    STORE --> MA["MariaDbProcessStore<br/>• Driver: MySqlConnector<br/>• Native MariaDB dialect syntax"]
    STORE --> OR["OracleProcessStore<br/>• Driver: Oracle.ManagedDataAccess.Core<br/>• Native CLOB and UPPERCASE identifiers"]
```

---

## 8. State Schema Migration Pipeline

```mermaid
flowchart LR
    STORED["Database: Serialized State v1"] --> DESER["IProcessStateSerializer.Deserialize"]
    DESER --> V1["OrderStateV1"]
    
    subgraph PIPELINE["ProcessStateMigrationPipeline"]
        V1 --> STEP1["IProcessStateMigrator&lt;V1, V2&gt;.Migrate"]
        STEP1 --> V2["OrderStateV2"]
        V2 --> STEP2["IProcessStateMigrator&lt;V2, V3&gt;.Migrate"]
        STEP2 --> V3["OrderStateV3 (Target Current Schema)"]
    end
    
    V3 --> EXEC["ProcessCoordinator.ExecuteAsync Continues Execution"]
```
