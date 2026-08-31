# ADR-034: Osherove Test Naming Convention and Justified IDE1006 Suppression

## Status
**Accepted**

---

## Context
In the .NET ecosystem, default static code analysis rules (`IDE1006: Naming rule violation` and `CA1707: Identifiers should not contain underscores`) mandate PascalCase naming without underscores (`_`) across all methods.

However, in modern test engineering (xUnit), test methods are not production utility methods; they are **executable living specifications** and documentation of system invariants.

When a test fails in a Continuous Integration (CI) console runner, the method name is the primary diagnostic indicator. Dense PascalCase test names (e.g. `ExecuteAsyncConcurrentWritesConflictRetriesSucceeds`) degrade human readability and increase Mean Time to Detect (MTTD).

Roy Osherove's established convention:
```text
[UnitOfWork]_[ScenarioUnderTest]_[ExpectedBehavior]
```
clearly separates the three dimensions of a test case using underscores (`_`), allowing test runner outputs to read as structured natural language specifications.

---

## Decision
1. **Formal Adoption of the Osherove Convention**:
   - All test methods across test projects (`tests/**/*.cs`) adopt the pattern `UnitOfWork_Scenario_ExpectedResult`.
   - Examples:
     - `ExecuteAsync_ShouldRetryOnConcurrencyConflict_AndSucceedIfWithinMaxRetries`
     - `SaveAsync_ConflictingRevision_ShouldReturnConcurrencyConflict`
     - `ProcessId_ParseAndTryParse_String_ShouldBehaveCorrectly`

2. **Justified Suppression of IDE1006 and CA1707 in Test Projects**:
   - Suppress `IDE1006` and `CA1707` exclusively for projects located in `tests/` via `tests/Directory.Build.props`.
   - Formal Justification: Test methods are human-readable executable specifications where underscores provide essential semantic separation.

3. **Strict Invariant in Production Code (`src/`)**:
   - Production projects in `src/` strictly enforce standard Microsoft naming conventions with zero underscore exceptions under `TreatWarningsAsErrors=true`.

---

## Consequences
- **Positive**:
  - Maximum readability and instant diagnostic clarity in CI/CD failure logs.
  - Consistent naming standards across the entire test suite.
  - Zero compiler or analyzer warnings with `TreatWarningsAsErrors=true`.
- **Negative**:
  - None. Suppression is strictly confined to test assemblies.
