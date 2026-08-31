# Security Policy

## Supported Versions

We provide security updates and patches for the following versions of `EricksonLopez.Processes`:

| Version | Supported | .NET Target | Status |
| :--- | :---: | :--- | :--- |
| **1.0.x** | ✅ | .NET 10.0 (`net10.0`) | Current Stable Release |
| **< 1.0.0** | ❌ | .NET 10.0 (`net10.0`) | Unsupported Preview |

---

## Reporting a Vulnerability

The maintainers take security seriously. If you discover a vulnerability or security-related issue in `EricksonLopez.Processes`, please report it responsibly:

1. **Do not open a public GitHub issue**.
2. Submit a private vulnerability report via [GitHub Security Advisories](https://github.com/ericksonlopezf/dotnet-processes/security/advisories/new) or email the core maintainer directly at [ericksonlopezf@gmail.com](mailto:ericksonlopezf@gmail.com).
3. Provide detailed steps to reproduce the vulnerability, including minimal code samples, expected vs. actual outcomes, and any proof-of-concept exploits.

### Response Timeline
- **Acknowledgment**: Within 48 hours of receipt.
- **Triage & Assessment**: Within 5 business days.
- **Fix & Disclosure**: Coordinated disclosure within 30 days of vulnerability validation.

---

## Supply Chain Security

`EricksonLopez.Processes` adheres to modern software supply chain security standards:

1. **Deterministic Builds**: All packages are built deterministically (`<Deterministic>true</Deterministic>`) to guarantee bit-for-bit reproducibility from source.
2. **SourceLink & Symbol Packages**: All NuGet packages embed untracked sources and publish companion `.snupkg` symbol packages for verifiable debugging.
3. **Zero Third-Party Reflection**: The core abstraction package (`EricksonLopez.Processes.Abstractions`) has **zero** third-party dependencies, eliminating transitively inherited vulnerabilities in domain models.
4. **Automated CI/CD Validation**: Workflows run on clean GitHub Actions runners verifying code coverage, mutation testing gates, and Native AOT compilation before packages are pushed.

---

## Known Security Boundaries

### 1. Deserialization of Process State
- Process states stored in relational databases are deserialized via `IProcessStateSerializer<TState>` using `System.Text.Json` source-generated contexts (`JsonSerializerContext`).
- **Boundary**: Deserialization is constrained to explicitly registered CLR types. Unrestricted polymorphic type binders (such as `TypeNameHandling.All`) are strictly prohibited to prevent arbitrary code execution attacks.

### 2. SQL Injection Immunity
- All relational storage providers (`EricksonLopez.Processes.Storage.*`) strictly use parameterized ADO.NET commands (`NpgsqlParameter`, `SqlParameter`, `SqliteParameter`, `MySqlParameter`, `OracleParameter`).
- Table names provided during DI registration are validated to prevent identifier injection.

### 3. Tenant & Process Isolation
- The library does not enforce multi-tenant isolation internally; tenant discrimination should be incorporated into the `ProcessId` or business `CorrelationId` (e.g. using `CompositeCorrelationKey.From(tenantId, orderId)`).
