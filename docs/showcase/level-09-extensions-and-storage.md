# Showcase — Level 09: Extensions & Storage Engines

**Level 09** demonstrates integration with the 6 supported relational database engines and extension methods for bridging identifiers across libraries.

---

## Covered Components

1. **The 6 Official Storage Adapters**:
   - `AddPostgreSqlProcessStore<TState>` (`Npgsql`, optimized for JSONB and CAS).
   - `AddSqlServerProcessStore<TState>` (`Microsoft.Data.SqlClient`, Snapshot isolation and CAS).
   - `AddSqliteProcessStore<TState>` (`Microsoft.Data.Sqlite`, embedded, ideal for local dev and edge).
   - `AddMySqlProcessStore<TState>` (`MySqlConnector`, MySQL 8.x syntax).
   - `AddMariaDbProcessStore<TState>` (`MySqlConnector`, native MariaDB dialect).
   - `AddOracleProcessStore<TState>` (`Oracle.ManagedDataAccess.Core`, CLOB and Oracle uppercase identifier convention).

2. **Cross-Library Identifier Bridge (`ProcessEventsIdentifierExtensions`)**:
   - `CorrelationId.ToEventsCorrelationId()`: Zero-copy conversion to `EricksonLopez.Events.Identifiers.CorrelationId`.
   - `CorrelationId.ToProcessesCorrelationId()`: Reverse conversion back to `Processes`.
   - `CausationId.ToEventsCausationId()`: Conversion to `Events.Identifiers.CausationId`.
   - `CausationId.ToProcessesCausationId()`: Reverse conversion.

---

## Persistence Adapter Comparison

| Engine | NuGet Package | ADO.NET Driver | Key Characteristics |
|---|---|---|---|
| **PostgreSQL** | `EricksonLopez.Processes.Storage.PostgreSql` | `Npgsql` | JSONB dialect, ADO.NET parameterized queries |
| **SQL Server** | `EricksonLopez.Processes.Storage.SqlServer` | `Microsoft.Data.SqlClient` | OCC CAS locks, Azure SQL compatibility |
| **SQLite** | `EricksonLopez.Processes.Storage.Sqlite` | `Microsoft.Data.Sqlite` | Zero configuration, embeddable |
| **MySQL** | `EricksonLopez.Processes.Storage.MySql` | `MySqlConnector` | MySQL 8.0+ syntax compatibility |
| **MariaDB** | `EricksonLopez.Processes.Storage.MariaDb` | `MySqlConnector` | Optimized for MariaDB native syntax |
| **Oracle** | `EricksonLopez.Processes.Storage.Oracle` | `Oracle.ManagedDataAccess.Core` | CLOB support and uppercase naming conventions |

---

## Showcase Execution

Executable code:
- `samples/EricksonLopez.Processes.Showcase/Level09_ExtensionsAndStorage/Level09StorageDialectsDemo.cs`
- `samples/EricksonLopez.Processes.Showcase/Level09_ExtensionsAndStorage/Level09EventsIdentifierExtensionsDemo.cs`

```bash
dotnet run --project samples/EricksonLopez.Processes.Showcase -- --level=9
```
