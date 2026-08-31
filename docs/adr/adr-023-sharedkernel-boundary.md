# ADR-023: SharedKernel and Minimal Dependencies Policy

## Context
Third-party dependencies introduce security vulnerabilities, version conflicts, breaking changes across .NET upgrades, and trimming obstacles.

## Problem
What is the dependency policy for `EricksonLopez.Processes` and its core packages?

## Options
1. Reference popular open-source packages (e.g. MediatR, Newtonsoft.Json, Polly, Quartz).
2. Zero external dependencies in `Abstractions` and `Core`; rely strictly on the standard .NET 10 BCL.

## Decision
We adopt **Option 2: Zero external dependencies in core packages**.

`EricksonLopez.Processes.Abstractions` and `EricksonLopez.Processes` reference only standard BCL types. Roslyn generator and DI extensions live in dedicated optional packages.

## Rationale
- Rock-solid stability and zero supply-chain risk.
- Absolute trimming and Native AOT guarantees.

## Consequences
- Clean, unpolluted dependency tree for consumers.

## Rejected Alternatives
- Adding helper NuGet packages to core abstractions.
