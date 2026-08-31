# ADR-027: Process Version Coexistence

## Context
When a new workflow revision (`OrderFulfillmentProcessV2`) is deployed, existing long-running instances started under `OrderFulfillmentProcessV1` may need to complete under the original V1 rules rather than migrating to V2.

## Problem
How can multiple versions of the same logical process definition coexist concurrently in the same runtime host?

## Options
1. Require all instances to immediately migrate to the newest version upon deployment.
2. Allow version coexistence where instances carry their `ProcessVersion`, and the coordinator routes incoming events to the version-specific handler matching the instance's version.

## Decision
We adopt **Option 2: Version coexistence support**.

The process registry can hold both `OrderFulfillmentProcessV1` and `OrderFulfillmentProcessV2`. Incoming events for an existing V1 instance route to V1 handlers, while newly initiated processes instantiate under V2.

## Rationale
- Crucial for workflows lasting days or months where migrating in-flight state is too risky.
- Fully supported via static dispatch tables.

## Consequences
- Clean operational rollouts with zero forced breaking changes to running workflows.

## Rejected Alternatives
- Hard-failing or forcing runtime down-casting of active processes.
