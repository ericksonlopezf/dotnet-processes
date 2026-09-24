# Contributing to EricksonLopez.Processes

Thank you for your interest in contributing to **`EricksonLopez.Processes`**! We are committed to building high-performance, trimming-safe, and Native AOT-ready Process Manager and Saga primitives for modern .NET.

---

## 1. Code of Conduct

All contributors and maintainers are expected to abide by our [Code of Conduct](CODE_OF_CONDUCT.md). Please report unacceptable behavior to the project maintainers.

---

## 2. Prerequisites & Environment Setup

- **.NET SDK**: .NET 10.0 SDK pinned to `10.0.100` via `global.json` (`rollForward: latestMinor`, `allowPrerelease: true`).
- **C# Language Version**: C# Preview (`LangVersion=preview`).
- **IDE**: Visual Studio 2022 / 2025, JetBrains Rider 2024.3+, or Visual Studio Code with C# Dev Kit.
- **Docker**: Required only for running database integration tests with Testcontainers (`Storage.IntegrationTests`).

---

## 3. Build & Test Commands

### Restoring and Building the Solution

```bash
# Restore dependencies centrally
dotnet restore EricksonLopez.Processes.slnx

# Build the solution in Release configuration with zero warnings
dotnet build EricksonLopez.Processes.slnx -c Release --no-restore
```

### Running Tests

```bash
# Fast local TDD: Run unit, architecture, analyzer, and generator tests (skips containerized DB tests)
dotnet test EricksonLopez.Processes.slnx --filter "Category!=Integration"

# Optional: Spin up all 5 relational database engines locally via Docker Compose
docker compose -f docker-compose.test.yml up -d

# Run the complete test suite including integration tests (requires Docker active for Testcontainers)
dotnet test EricksonLopez.Processes.slnx -c Release
```

### Mutation Testing with Stryker

The project enforces strict mutation score quality gates defined in `stryker-*.json` (`100%` high target, `98%` low, `95%` break threshold):

```bash
# Run Stryker mutation testing for core library
dotnet stryker -c stryker-config.json

# Or for specific package components (e.g. Abstractions, Outbox, Storage.PostgreSql)
dotnet stryker -c stryker-abstractions-config.json
dotnet stryker -c stryker-outbox-config.json
```

### Benchmark Regression Gate Policy

Pull requests touching `src/**` or `benchmarks/**` execute the Benchmark Regression Gate workflow (`benchmark-regression-gate.yml`):
- **Heap Invariant**: Zero-allocation on hotpath combinators (**0 B allocated**).
- **Latency Threshold**: Mean execution latency regression must not exceed **5%** vs baseline (`benchmarks/results/baseline.json`).

Run benchmarks locally:
```bash
dotnet run --project benchmarks/EricksonLopez.Processes.Benchmarks/EricksonLopez.Processes.Benchmarks.csproj -c Release --framework net10.0 -- --filter "*" --job short --memory
```

### Native AOT Publishing Validation

```bash
# Validate that the Native AOT sample compiles cleanly with zero trim/AOT warnings
dotnet publish samples/NativeAotSample/NativeAotSample.csproj -c Release -r linux-x64 -p:PublishAot=true -p:TreatWarningsAsErrors=true
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
- `main`: Primary production release branch (protected).
- `develop`: Primary integration and ongoing development branch.
- Feature branches: `feat/<short-description>` or `feature/<short-description>`.
- Bug fix branches: `fix/<issue-number>-<short-description>`.
- Refactoring / Documentation: `docs/<topic>` or `refactor/<topic>`.

Both `main` and `develop` trigger Continuous Integration (`ci.yml`) and Native AOT smoke testing on every push and pull request.

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
2. Verify that all unit and architecture tests pass (`dotnet test EricksonLopez.Processes.slnx --filter "Category!=Integration"`).
3. If introducing or changing public APIs, update the corresponding documentation in `/docs/` and add test coverage.
4. Verify that no trim or Native AOT warnings (`IL2026`, `IL3050`) are introduced.
5. Fill out the [Pull Request Template](.github/PULL_REQUEST_TEMPLATE.md) completely.
