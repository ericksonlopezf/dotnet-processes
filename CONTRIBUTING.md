# Contributing to EricksonLopez.Processes

Thank you for your interest in contributing to **`EricksonLopez.Processes`**! We are committed to building high-performance, trimming-safe, and Native AOT-ready Process Manager and Saga primitives for modern .NET.

---

## 1. Code of Conduct

All contributors and maintainers are expected to abide by our [Code of Conduct](CODE_OF_CONDUCT.md). Please report unacceptable behavior to the project maintainers.

---

## 2. Prerequisites & Environment Setup

- **.NET SDK**: .NET 10.0 SDK (`net10.0`).
- **C# Language Version**: C# Preview (`LangVersion=preview`).
- **IDE**: Visual Studio 2022 / 2025, JetBrains Rider 2024.3+, or Visual Studio Code with the C# Dev Kit.
- **Docker**: Required only for running database integration tests with Testcontainers.

---

## 3. Build & Test Commands

### Restoring and Building the Solution

```bash
# Restore dependencies centrally
dotnet restore EricksonLopez.Processes.slnx

# Build the solution in Release configuration
dotnet build EricksonLopez.Processes.slnx -c Release --no-restore
```

### Running Tests

```bash
# Fast local TDD: Run unit, architecture, analyzer, and generator tests (skips slow containerized tests)
./test-unit.ps1
# Or equivalent dotnet CLI:
dotnet test EricksonLopez.Processes.slnx --filter "Category!=Integration"

# Run the complete test suite (requires Docker active for Testcontainers)
dotnet test EricksonLopez.Processes.slnx -c Release
```

### Mutation Testing with Stryker

The project enforces strict mutation score quality gates (`100%` target, `95%` break threshold):

```bash
# Run Stryker mutation testing
dotnet stryker -c stryker-config.json
```

### Native AOT Publishing Validation

```bash
# Validate that the Native AOT sample compiles cleanly with zero trim/AOT warnings
dotnet publish samples/NativeAotSample/NativeAotSample.csproj -c Release -r win-x64 -p:PublishAot=true
```

---

## 4. Architectural Rules & Invariants

When writing or modifying code in this repository:

1. **Zero Runtime Reflection**: Do not use `Assembly.GetTypes()`, `Type.GetType()`, or `Activator.CreateInstance()`. All registrations must use static generic dispatch or Roslyn Source Generators (`EricksonLopez.Processes.Generator`).
2. **Zero-Allocation Primitives**: All identifier types must be implemented as `readonly record struct` implementing `ISpanParsable<TSelf>` and `ISpanFormattable`.
3. **Pure State Transitions**: Handlers implementing `IProcessHandler<TState, in TEvent>` must be pure, deterministic transition functions that yield `ProcessEffect` records without performing external network I/O.
4. **Optimistic Concurrency Control (OCC)**: State commits must execute through atomic Compare-And-Swap (CAS) token updates via monotonic `Revision` numbers.
5. **Clean Architecture Boundaries**: `EricksonLopez.Processes.Abstractions` must have **zero** external or third-party package dependencies (BCL only).

---

## 5. Branching & Commit Conventions

### Branch Strategy
- `main`: Primary production and development branch.
- Feature branches: `feat/<short-description>` or `feature/<short-description>`.
- Bug fix branches: `fix/<issue-number>-<short-description>`.
- Refactoring / Documentation: `docs/<topic>` or `refactor/<topic>`.

### Conventional Commits
All commit messages must follow the [Conventional Commits](https://www.conventionalcommits.org/) specification:

- `feat: add MariaDB dialect support in storage engine`
- `fix: correct linear backoff multiplier calculation in ProcessCoordinator`
- `docs: update architectural diagrams and cookbook recipes`
- `perf: eliminate string allocations in TagList diagnostics`
- `test: add property-based tests for CompositeCorrelationKey`

---

## 6. Pull Request Guidelines

Before submitting a Pull Request:

1. Ensure the solution builds cleanly with **0 warnings** (`TreatWarningsAsErrors=true`).
2. Verify that all unit and architecture tests pass (`./test-unit.ps1`).
3. If introducing or changing public APIs, update the corresponding documentation in `/docs/` and add test coverage.
4. Fill out the [Pull Request Template](.github/PULL_REQUEST_TEMPLATE.md) completely.
