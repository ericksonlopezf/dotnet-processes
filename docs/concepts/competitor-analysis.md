# Competitor Analysis & Feature Matrix

## Landscape Overview

| Framework / Engine | Focus Area | Runtime Model | Native AOT / Trimming | Persistence Model | Broker Coupling |
| :--- | :--- | :--- | :--- | :--- | :--- |
| **MassTransit** | Messaging & Saga State Machine | In-memory actor / bus consumer | ❌ Heavy Reflection & IL | EF Core, Dapper, Mongo, Redis | High (Transport coupled) |
| **NServiceBus** | Enterprise Service Bus | Endpoint saga worker | ❌ Heavy Reflection | SQL Persistence, RavenDB | High (NServiceBus pipeline) |
| **Wolverine** | Command / Message handler | Generated code pipeline | ⚠️ Partial (Marten / EF) | Marten, EF Core | Moderate |
| **Temporal / Dapr** | Durable Workflow Replay | Replay event log from gRPC | ⚠️ Framework dependent | Cluster / Durable Store | Engine dependent |
| **Camunda / Elsa** | BPMN & Visual Workflow | Engine graph interpreter | ❌ Heavy Reflection / Dynamic | SQL / Document stores | Standalone engine |
| **EricksonLopez.Processes**| **AOT-first Process & Saga Primitives** | **Pure State Machine / Coordinator** | **✅ 100% Native AOT & Trimming** | **Agnostic (`IProcessStore<TState>`)** | **Zero (Transport Agnostic)** |

## Detailed Feature Matrix

| Feature | EricksonLopez.Processes | NServiceBus | MassTransit | Wolverine | Rebus | Temporal | Dapr | Orleans | Decision |
| :--- | :---: | :---: | :---: | :---: | :---: | :---: | :---: | :---: | :--- |
| **AOT & Trimming Native** | ✅ Zero Reflection | ❌ | ❌ | ⚠️ | ❌ | ⚠️ | ⚠️ | ⚠️ | **KEEP (Core Axiom)** |
| **Immutable State Record** | ✅ | ❌ | ❌ | ⚠️ | ❌ | ✅ | ✅ | ⚠️ | **KEEP** |
| **Optimistic Concurrency (CAS)**| ✅ `Revision` token | ✅ | ✅ | ✅ | ✅ | N/A | ✅ | ✅ | **KEEP** |
| **Explicit Compensation (Saga)**| ✅ Reverse-order | ⚠️ Manual | ✅ Courier | ⚠️ Manual | ⚠️ Manual | ✅ | ✅ | ⚠️ | **KEEP** |
| **Pure Intent Effects (Outbox)**| ✅ `ProcessEffect` | ❌ Bus direct | ❌ Bus direct | ⚠️ Outbox | ❌ Bus direct | ❌ Engine | ❌ Engine | ❌ Stream | **KEEP** |
| **Compile-time Source Gen** | ✅ Roslyn Gen | ❌ | ❌ | ✅ Codegen | ❌ | ❌ | ❌ | ✅ CodeGen | **KEEP** |
| **Database Agnostic Store** | ✅ `IProcessStore<T>` | ⚠️ Heavy | ⚠️ Heavy | ⚠️ Marten/EF | ⚠️ Heavy | ❌ Cluster | ❌ Component| ⚠️ Grain Storage | **KEEP** |
| **Zero Broker Dependencies**| ✅ 100% decoupled | ❌ Bus coupled | ❌ Bus coupled | ❌ Wolverine | ❌ Rebus | ❌ Cluster | ❌ Sidecar | ❌ Orleans | **KEEP** |
| **Visual Workflow Designer** | ❌ | ❌ | ❌ | ❌ | ❌ | ✅ | ❌ | ❌ | **REJECT (Non-goal)** |
| **BPMN 2.0 Parser / XML** | ❌ | ❌ | ❌ | ❌ | ❌ | ❌ | ❌ | ❌ | **REJECT (Non-goal)** |
| **Built-in Quartz/Cron Engine** | ❌ | ⚠️ Schedulers | ⚠️ Hangfire | ⚠️ Schedulers | ⚠️ | ✅ | ✅ | ⚠️ Timers | **REJECT (Non-goal)** |

## Strategic Takeaways
- `EricksonLopez.Processes` does not attempt to clone MassTransit or Temporal.
- It solves the missing architectural layer in modern .NET: **pure, high-performance, trimming-safe, AOT-native process manager and saga state machine primitives** that integrate cleanly with DDD, Clean Architecture, and modular ecosystem components.
