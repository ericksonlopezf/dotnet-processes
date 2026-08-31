# Level 02: Storage Drivers & Durable Persistence

## 1. Multi-Engine Storage Architecture
`EricksonLopez.Processes` segregates storage implementation across dedicated, lightweight database packages:
- `EricksonLopez.Processes.Storage.PostgreSql`
- `EricksonLopez.Processes.Storage.SqlServer`
- `EricksonLopez.Processes.Storage.MySql`
- `EricksonLopez.Processes.Storage.MariaDb`
- `EricksonLopez.Processes.Storage.Oracle`
- `EricksonLopez.Processes.Storage.Sqlite`

```csharp
// Register PostgreSQL durable storage provider
services.AddProcesses(options =>
{
    options.UsePostgreSqlStorage(connectionString);
    options.UseSystemTextJsonSerializer();
});
```

---

## 2. Optimistic Concurrency & Invariant Enforcement
Each state transition increments a monotonically increasing `Version` timestamp column. Any concurrent modification yields an `OptimisticConcurrencyException`, allowing automatic retry or compensation based on configurable backoff policies.
