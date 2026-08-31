# ADR-017: Trimming Strategy

## Context
When publishing applications with `PublishTrimmed=true`, the .NET IL trimmer strips unused metadata, classes, and members. Libraries that rely on unannotated reflection or dynamic type lookups break silently at runtime.

## Problem
How should `EricksonLopez.Processes` ensure that all code paths are safe under aggressive trimming?

## Options
1. Suppress trimming warnings using `[UnconditionalSuppressMessage]` without fixing root causes.
2. Design APIs with zero trimming warnings (`EnableTrimAnalyzer=true`, `TreatWarningsAsErrors=true`), annotating generic constraints where necessary with `[DynamicallyAccessedMembers]` and favoring compile-time static dispatch.

## Decision
We adopt **Option 2: Zero trimming warnings by architectural design**.

- `Directory.Build.props` enables `<EnableTrimAnalyzer>true</EnableTrimAnalyzer>` and `<TreatWarningsAsErrors>true</TreatWarningsAsErrors>`.
- Any necessary type preservation for JSON serialization uses explicit `JsonSerializerContext` source generators in the serializer package.

## Rationale
- Guarantees reliability in trimmed container images (e.g. `chiseled-aot` Linux containers).
- Avoids fragile runtime reflection hacks.

## Consequences
- Every library build verifies zero trimming analyzer warnings.

## Rejected Alternatives
- Masking warnings with blanket suppression attributes.
