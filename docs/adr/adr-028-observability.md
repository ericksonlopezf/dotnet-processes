# ADR-028: Observability and Telemetry Policy

## Context
Production monitoring requires distributed tracing (OpenTelemetry / W3C TraceContext) and runtime metrics (latency, state transitions, concurrency conflicts, compensations).

## Problem
How should observability be integrated without adding heavyweight external dependencies or introducing performance overhead when telemetry is disabled?

## Options
1. Add hard NuGet references to OpenTelemetry packages.
2. Use standard .NET BCL `System.Diagnostics.ActivitySource` and `System.Diagnostics.Metrics.Meter`.

## Decision
We adopt **Option 2: Built-in BCL `ActivitySource` and `Meter` instruments**.

- `ProcessDiagnostics.ActivitySource` (`"EricksonLopez.Processes"`) creates spans for process execution, state transitions, and compensation steps, automatically propagating `TraceParent`, `CorrelationId`, and `ProcessId`.
- `ProcessDiagnostics.Meter` exports counters:
  - `processes.started`
  - `processes.completed`
  - `processes.failed`
  - `processes.compensated`
  - `processes.concurrency_conflicts`
  - `processes.transition.duration`

## Rationale
- Zero external package dependencies.
- Zero allocation and near-zero CPU overhead when listeners are not attached.
- 100% compliant with standard OpenTelemetry collectors and Azure Application Insights / Prometheus.

## Consequences
- Clean, standard telemetry across all .NET environments.

## Rejected Alternatives
- Direct OpenTelemetry SDK dependency in core.
