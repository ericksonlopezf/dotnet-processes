# Anti-Feature Matrix (Explicit Non-Goals)

To prevent scope creep and maintain high performance and Native AOT guarantees, the following capabilities are explicitly rejected from `EricksonLopez.Processes`:

| Rejected Feature | Reason for Rejection | Architectural Owner | Alternative / Recommended Solution |
| :--- | :--- | :--- | :--- |
| **BPMN 2.0 Engine** | Requires dynamic XML parsing and runtime interpreters that violate zero-reflection and AOT principles. | External BPMN Engine (Camunda) | Model workflows in strongly typed C# with compiler verification. |
| **Visual Workflow Designer** | Out of scope for a high-performance backend primitives library; introduces UI bloat. | Enterprise Workflow Systems (Elsa) | Pure C# code definitions with unit testability. |
| **Built-in Message Broker** | Violates Single Responsibility Principle and transport neutrality. | Transport / Host Worker | Host receives messages from RabbitMQ/Kafka and passes them to Process Coordinator. |
| **Built-in Cron/Quartz Scheduler** | Scheduler thread pools and cron parsers complicate durability and clustering. | Infrastructure Scheduler (Quartz/Hangfire) | Emit `ProcessEffect.ScheduleTimeout(...)` and delegate timing to host scheduler. |
| **Distributed Lock Manager** | Distributed locks cause operational bottlenecks and single points of failure. | Infrastructure / Partitioning | Rely on Optimistic Concurrency Control (`Revision` / CAS) in storage layer. |
| **Direct ORM / EF Core in Core** | Leaks database specifics into core domain abstractions. | Infrastructure Persistence Package | Implement `IProcessStore<TState>` in application persistence project. |
| **Runtime Assembly Scanning** | Reflection scanning breaks silently under Native AOT and IL trimming. | Roslyn Source Generator | `EricksonLopez.Processes.Generator` registers processes at build time. |
| **Service Locator in Handlers** | Violates explicit dependency principles and causes hidden runtime failures. | Application Dependency Injection | Inject dependencies into coordinator or pass via `ProcessContext`. |
