## Description

Please provide a clear and concise summary of the changes introduced in this pull request.

---

## Affected Packages

Check all packages modified or affected by this PR:

- [ ] `EricksonLopez.Processes.Abstractions`
- [ ] `EricksonLopez.Processes`
- [ ] `EricksonLopez.Processes.Generator`
- [ ] `EricksonLopez.Processes.Analyzers`
- [ ] `EricksonLopez.Processes.DependencyInjection`
- [ ] `EricksonLopez.Processes.SystemTextJson`
- [ ] `EricksonLopez.Processes.Events`
- [ ] `EricksonLopez.Processes.Mediator`
- [ ] `EricksonLopez.Processes.Outbox`
- [ ] `EricksonLopez.Processes.Storage.PostgreSql`
- [ ] `EricksonLopez.Processes.Storage.SqlServer`
- [ ] `EricksonLopez.Processes.Storage.Sqlite`
- [ ] `EricksonLopez.Processes.Storage.MySql`
- [ ] `EricksonLopez.Processes.Storage.MariaDb`
- [ ] `EricksonLopez.Processes.Storage.Oracle`
- [ ] `EricksonLopez.Processes.Testing`
- [ ] `Showcase / Samples / Benchmarks / Documentation`

---

## Quality Checklist

- [ ] **Build**: Solution builds cleanly with 0 warnings (`TreatWarningsAsErrors=true`).
- [ ] **Tests**: All unit and architecture tests pass (`dotnet test EricksonLopez.Processes.slnx --filter "Category!=Integration"`).
- [ ] **Coverage**: 100% line, branch, and method coverage maintained for new code.
- [ ] **Mutation Testing**: Stryker mutation score verified against quality gates (high 100%, break 95%).
- [ ] **Benchmark Gate**: Zero heap allocations on hotpath combinators (0 B) and latency regression <= 5%.
- [ ] **Native AOT & Trimming**: No reflection or dynamic code generation introduced; passes Native AOT checks.
- [ ] **Documentation**: Updated relevant documentation in `/docs/` and root files where applicable.
- [ ] **ADR**: Architecture Decision Record created or updated if introducing architectural changes.
