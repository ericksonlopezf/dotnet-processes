# ADR-040: Multi-Database Storage Dialects (SQLite, MySQL, MariaDB, Oracle, SQL Server, PostgreSQL)

## Status
**Accepted**

---

## Context
`EricksonLopez.Processes` defines the persistence contract through `IProcessStore<TState>`:

```csharp
public interface IProcessStore<TState> where TState : notnull
{
    ValueTask<ProcessInstance<TState>?> GetByIdAsync(ProcessId id, CancellationToken cancellationToken = default);
    ValueTask<ProcessSaveResult> SaveAsync(ProcessInstance<TState> instance, CancellationToken cancellationToken = default);
    ValueTask<bool> ExistsAsync(ProcessId id, CancellationToken cancellationToken = default);
    ValueTask<ProcessInstance<TState>?> GetByCorrelationIdAsync(CorrelationId correlationId, CancellationToken cancellationToken = default) => ...;
}
```

To support heterogeneous enterprise architectures, cloud providers, and edge/embedded environments, native persistence packages were required for **SQLite**, **MySQL**, **MariaDB**, **Oracle**, **SQL Server**, and **PostgreSQL**.

---

## Decision
Implement 6 isolated, decoupled persistence packages under `src/EricksonLopez.Processes.Storage.*`:

| Package | Database Engine | ADO.NET Driver | Key Characteristics |
| :--- | :--- | :--- | :--- |
| `EricksonLopez.Processes.Storage.Sqlite` | SQLite 3 | `Microsoft.Data.Sqlite` | In-memory / embedded file, 100% Native AOT, ideal for edge and testing. |
| `EricksonLopez.Processes.Storage.MySql` | MySQL 8.0+ | `MySqlConnector` | High-performance asynchronous ADO.NET driver, JSON state storage. |
| `EricksonLopez.Processes.Storage.MariaDb` | MariaDB 10.5+ | `MySqlConnector` | Tailored MariaDB dialect, optimized table schemas and index constraints. |
| `EricksonLopez.Processes.Storage.Oracle` | Oracle 19c / 21c / 23c | `Oracle.ManagedDataAccess.Core` | Parameter `BindByName=true`, `TIMESTAMP WITH TIME ZONE`, `CLOB` state storage. |
| `EricksonLopez.Processes.Storage.SqlServer` | SQL Server 2019+ | `Microsoft.Data.SqlClient` | High-performance SQL Server provider with atomic CAS revision queries. |
| `EricksonLopez.Processes.Storage.PostgreSql` | PostgreSQL 13+ | `Npgsql` | High-performance provider with JSONB binary storage. |

### Architectural Invariants
1. **Zero Runtime Reflection & Native AOT**: All providers use parameterized ADO.NET commands. State serialization is handled via `IProcessStateSerializer<TState>` backed by source-generated `JsonSerializerContext`.
2. **Atomic Compare-And-Swap (CAS)**:
   - **Initial Insert (`Revision == 1`)**: Conditional atomic insert (`ON CONFLICT DO NOTHING` / `WHERE NOT EXISTS`). Returns `ProcessSaveResult.ConcurrencyConflict` if row already exists.
   - **Update (`Revision > 1`)**: `UPDATE ... WHERE ProcessId = @Id AND Revision = @ExpectedRevision`. If 0 rows are updated, checks existence to return `ConcurrencyConflict` vs `NotFound`.
3. **Fluent Dependency Injection Extensions**: Each package provides a clean `IServiceCollection` extension:
   - `services.AddSqliteProcessStore<TState>(connectionString, tableName)`
   - `services.AddMySqlProcessStore<TState>(connectionString, tableName)`
   - `services.AddMariaDbProcessStore<TState>(connectionString, tableName)`
   - `services.AddOracleProcessStore<TState>(connectionString, tableName)`
   - `services.AddSqlServerProcessStore<TState>(connectionString, tableName)`
   - `services.AddPostgreSqlProcessStore<TState>(connectionString, tableName)`

---

## Consequences
- **Positive**:
  - Full native persistence coverage for the top 6 industry database engines.
  - Granular packages avoiding unwanted dependency bloat.
  - Automated integration testing with Testcontainers for PostgreSQL, SQL Server, MySQL, MariaDB, Oracle, and in-memory SQLite.
- **Negative**:
  - Requires maintaining 6 distinct SQL dialect templates and migration scripts.
