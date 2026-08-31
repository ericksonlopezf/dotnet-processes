# EricksonLopez.Processes — Terminology and Ubiquitous Language

| Term | Definition |
| :--- | :--- |
| **Process** | A long-running, stateful business workflow spanning multiple asynchronous operations and bounded contexts. |
| **Process Manager** | An orchestration component that reacts to domain events, tracks state progression, and emits commands to coordinate distributed operations. |
| **Saga** | A specialized process manager coordinating distributed transactions across microservices, maintaining an audit log of completed steps and executing explicit compensating actions upon failure. |
| **Process Instance** | A unique, persisted execution of a process, identified by a `ProcessId`, containing technical metadata (`Revision`, `Status`, `Version`) and business state `TState`. |
| **Process State** | An immutable data record capturing the current business snapshot and milestones of a process instance. |
| **Process Definition** | The immutable set of transition rules, handler methods, and metadata governing how a process reacts to events. |
| **Process Effect** | An outgoing intent emitted by a state transition (e.g., `CommandIntent`, `EventIntent`, `ScheduleTimeoutIntent`, `CompensationAction`). |
| **Process Status** | The formal lifecycle phase of an instance: `Initialized`, `Running`, `Suspended`, `Completed`, `Compensating`, `Compensated`, `Failed`. |
| **Revision** | A monotonically increasing integer token used for Optimistic Concurrency Control and atomic Compare-And-Swap (CAS) updates in durable storage. |
| **Correlation** | The mechanism of linking incoming events to the specific `ProcessId` of a running process instance using business keys or IDs. |
| **Causation** | The identifier of the direct parent message/trigger that caused the current action or transition to occur. |
| **Compensation** | An explicit domain operation that semantically undoes or mitigates the side-effects of a previously completed step in a failed saga. |
| **Forward Recovery** | Progressing a failed workflow forward (via retry, alternative path, or manual override) instead of rolling back via compensation. |
| **Timeout Intent** | A declarative request emitted by a process to be woken up after a specified duration if an expected event has not arrived. |
