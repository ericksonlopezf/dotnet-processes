// Copyright © Erickson Lopez. MIT License.
using System;
using System.Threading.Tasks;
using EricksonLopez.Processes.Abstractions;
using EricksonLopez.Processes.Storage.MariaDb;
using EricksonLopez.Processes.Storage.MySql;
using EricksonLopez.Processes.Storage.Oracle;
using EricksonLopez.Processes.Storage.PostgreSql;
using EricksonLopez.Processes.Storage.Sqlite;
using EricksonLopez.Processes.Storage.SqlServer;
using Microsoft.Extensions.DependencyInjection;

namespace EricksonLopez.Processes.Showcase.Level09_ExtensionsAndStorage;

public sealed record SampleStorageState(string EntityId, string Data) : IProcessState;

public sealed class DummySerializer : IProcessStateSerializer<SampleStorageState>
{
    public byte[] Serialize(SampleStorageState state) => Array.Empty<byte>();
    public SampleStorageState Deserialize(ReadOnlySpan<byte> data) => new("DUMMY", "DATA");
}

/// <summary>
/// Level 9: Official Multi-Database Storage Engine Extensions
/// Demonstrates registration and dialect characteristics for PostgreSQL, SQL Server, SQLite, MySQL, MariaDB, and Oracle.
/// </summary>
public static class Level09StorageDialectsDemo
{
    public static Task RunAsync()
    {
        Console.ForegroundColor = ConsoleColor.Cyan;
        Console.WriteLine("================================================================================");
        Console.WriteLine(" LEVEL 09: MULTI-DATABASE STORAGE ENGINES & DIALECT PERSISTENCE");
        Console.WriteLine("================================================================================");
        Console.ResetColor();

        var services = new ServiceCollection();
        services.AddSingleton<IProcessStateSerializer<SampleStorageState>, DummySerializer>();

        // 1. PostgreSQL (JSONB column, parameterized atomic CAS)
        services.AddPostgreSqlProcessStore<SampleStorageState>(
            connectionString: "Host=localhost;Database=processes_pg;Username=postgres;Password=secret",
            tableName: "process_instances");

        // 2. Microsoft SQL Server (NVARCHAR(MAX) JSON payload, atomic CAS update)
        services.AddSqlServerProcessStore<SampleStorageState>(
            connectionString: "Server=localhost,1433;Database=processes_sql;User Id=sa;Password=secret;",
            tableName: "ProcessInstances");

        // 3. SQLite (Embedded zero-config ACID, atomic CAS)
        services.AddSqliteProcessStore<SampleStorageState>(
            connectionString: "Data Source=showcase_processes.db",
            tableName: "ProcessInstances");

        // 4. MySQL (JSON column support, atomic CAS)
        services.AddMySqlProcessStore<SampleStorageState>(
            connectionString: "Server=localhost;Port=3306;Database=processes_mysql;Uid=root;Pwd=secret;",
            tableName: "process_instances");

        // 5. MariaDB (High performance InnoDB, atomic CAS)
        services.AddMariaDbProcessStore<SampleStorageState>(
            connectionString: "Server=localhost;Port=3306;Database=processes_mariadb;Uid=root;Pwd=secret;",
            tableName: "process_instances");

        // 6. Oracle (CLOB / JSON column, uppercase identifier standards)
        services.AddOracleProcessStore<SampleStorageState>(
            connectionString: "Data Source=localhost:1521/XEPDB1;User Id=system;Password=secret;",
            tableName: "PROCESS_INSTANCES");

        Console.WriteLine("Registered 6 Official Enterprise Storage Providers in DI:");
        Console.WriteLine("  1. PostgreSQL (AddPostgreSqlProcessStore) -> JSONB native queries");
        Console.WriteLine("  2. SQL Server (AddSqlServerProcessStore)   -> Microsoft.Data.SqlClient with CAS");
        Console.WriteLine("  3. SQLite     (AddSqliteProcessStore)      -> Zero-overhead embedded storage");
        Console.WriteLine("  4. MySQL      (AddMySqlProcessStore)       -> MySqlConnector optimized CAS");
        Console.WriteLine("  5. MariaDB    (AddMariaDbProcessStore)     -> Dedicated MariaDB adapter");
        Console.WriteLine("  6. Oracle     (AddOracleProcessStore)      -> Oracle.ManagedDataAccess.Core");

        Console.ForegroundColor = ConsoleColor.Green;
        Console.WriteLine("\n✔ Level 09 Multi-Database Storage Dialects demo completed successfully.");
        Console.ResetColor();
        return Task.CompletedTask;
    }
}
