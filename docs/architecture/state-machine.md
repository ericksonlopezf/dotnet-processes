# Process Lifecycle & State Machine

## Process Status Transitions

A process lifecycle in `EricksonLopez.Processes` is governed by the `ProcessStatus` enumeration:

```mermaid
stateDiagram-v2
    [*] --> Initialized
    Initialized --> Running: Initial Event
    Running --> Running: Advance
    Running --> Suspended: Suspend (Timeout/Wait)
    Suspended --> Running: Resume Event
    Running --> Completed: Complete
    Running --> Compensating: Compensate (Failure)
    Compensating --> Compensated: All Compensations Success
    Compensating --> Failed: Compensation Failed
    Running --> Failed: Fail
    Completed --> [*]
    Compensated --> [*]
    Failed --> [*]
```

### Status Descriptions

- **`Initialized`**: Instance created, not yet advanced.
- **`Running`**: Active workflow executing forward steps.
- **`Suspended`**: Paused waiting for a temporal deadline (`ProcessEffect.ScheduleTimeout`) or external callback.
- **`Completed`**: Successfully reached its terminal milestone.
- **`Compensating`**: Rollback in progress across recorded steps.
- **`Compensated`**: All compensations succeeded. Terminal state.
- **`Failed`**: Terminal failure (unrecoverable error or compensation failure).
