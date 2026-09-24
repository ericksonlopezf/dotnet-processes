// Copyright © Erickson Lopez. MIT License.
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using AwesomeAssertions;
using EricksonLopez.Processes.Abstractions;
using EricksonLopez.Processes.Events;
using EricksonLopez.Processes.Mediator;
using EricksonLopez.Processes.Outbox;
using EricksonLopez.Processes.Storage.MariaDb;
using EricksonLopez.Processes.Storage.MySql;
using EricksonLopez.Processes.Storage.Oracle;
using EricksonLopez.Processes.Storage.PostgreSql;
using EricksonLopez.Processes.Storage.Sqlite;
using EricksonLopez.Processes.Storage.SqlServer;
using EricksonLopez.Processes.SystemTextJson;
using NetArchTest.Rules;
using Xunit;

namespace EricksonLopez.Processes.ArchitectureTests;

[Trait("Category", "Architecture")]
public class ArchitectureBoundaryTests
{
    [Fact]
    public void AbstractionsAssembly_ShouldNotDependOn_ForbiddenInfrastructure()
    {
        var result = Types.InAssembly(typeof(ProcessId).Assembly)
            .ShouldNot()
            .HaveDependencyOnAny(
                "Microsoft.EntityFrameworkCore",
                "Dapper",
                "RabbitMQ.Client",
                "Confluent.Kafka",
                "Microsoft.AspNetCore",
                "System.Reflection.Emit",
                "EricksonLopez.Events.Contracts",
                "EricksonLopez.Events")
            .GetResult();

        result.IsSuccessful.Should().BeTrue();
    }

    [Fact]
    public void AbstractionsAssembly_ShouldNotDependOn_EventsContracts()
    {
        var result = Types.InAssembly(typeof(ProcessId).Assembly)
            .ShouldNot()
            .HaveDependencyOnAny("EricksonLopez.Events.Contracts", "EricksonLopez.Events")
            .GetResult();

        result.IsSuccessful.Should().BeTrue();
    }

    [Fact]
    public void AbstractionsAssembly_ShouldNotDependOn_OtherProcessesAssemblies()
    {
        var result = Types.InAssembly(typeof(ProcessId).Assembly)
            .ShouldNot()
            .HaveDependencyOnAny(
                "EricksonLopez.Processes.Execution",
                "EricksonLopez.Processes.Compensation",
                "EricksonLopez.Processes.Migration",
                "EricksonLopez.Processes.Registry",
                "EricksonLopez.Processes.Diagnostics",
                "EricksonLopez.Processes.DependencyInjection",
                "EricksonLopez.Processes.Storage.Sqlite",
                "EricksonLopez.Processes.Storage.MySql",
                "EricksonLopez.Processes.Storage.MariaDb",
                "EricksonLopez.Processes.Storage.PostgreSql",
                "EricksonLopez.Processes.Storage.SqlServer",
                "EricksonLopez.Processes.Storage.Oracle",
                "EricksonLopez.Processes.SystemTextJson",
                "EricksonLopez.Processes.Testing",
                "EricksonLopez.Processes.Mediator",
                "EricksonLopez.Processes.Events",
                "EricksonLopez.Processes.Outbox")
            .GetResult();

        result.IsSuccessful.Should().BeTrue();
    }

    [Fact]
    public void CoreProcessesAssembly_ShouldNotDependOn_ForbiddenInfrastructure()
    {
        var result = Types.InAssembly(typeof(ProcessContext).Assembly)
            .ShouldNot()
            .HaveDependencyOnAny(
                "Microsoft.EntityFrameworkCore",
                "Dapper",
                "RabbitMQ.Client",
                "Confluent.Kafka",
                "Microsoft.AspNetCore",
                "System.Reflection.Emit")
            .GetResult();

        result.IsSuccessful.Should().BeTrue();
    }

    [Fact]
    public void StorageSqliteAssembly_ShouldNotDependOn_ForbiddenInfrastructure()
    {
        var result = Types.InAssembly(typeof(SqliteProcessStore<>).Assembly)
            .ShouldNot()
            .HaveDependencyOnAny(
                "EricksonLopez.Processes.DependencyInjection",
                "Microsoft.EntityFrameworkCore",
                "RabbitMQ.Client",
                "Confluent.Kafka",
                "Microsoft.AspNetCore")
            .GetResult();

        result.IsSuccessful.Should().BeTrue();
    }

    [Fact]
    public void StorageMySqlAssembly_ShouldNotDependOn_ForbiddenInfrastructure()
    {
        var result = Types.InAssembly(typeof(MySqlProcessStore<>).Assembly)
            .ShouldNot()
            .HaveDependencyOnAny(
                "EricksonLopez.Processes.DependencyInjection",
                "Microsoft.EntityFrameworkCore",
                "RabbitMQ.Client",
                "Confluent.Kafka",
                "Microsoft.AspNetCore")
            .GetResult();

        result.IsSuccessful.Should().BeTrue();
    }

    [Fact]
    public void StorageMariaDbAssembly_ShouldNotDependOn_ForbiddenInfrastructure()
    {
        var result = Types.InAssembly(typeof(MariaDbProcessStore<>).Assembly)
            .ShouldNot()
            .HaveDependencyOnAny(
                "EricksonLopez.Processes.DependencyInjection",
                "Microsoft.EntityFrameworkCore",
                "RabbitMQ.Client",
                "Confluent.Kafka",
                "Microsoft.AspNetCore")
            .GetResult();

        result.IsSuccessful.Should().BeTrue();
    }

    [Fact]
    public void StoragePostgreSqlAssembly_ShouldNotDependOn_ForbiddenInfrastructure()
    {
        var result = Types.InAssembly(typeof(PostgreSqlProcessStore<>).Assembly)
            .ShouldNot()
            .HaveDependencyOnAny(
                "EricksonLopez.Processes.DependencyInjection",
                "Microsoft.EntityFrameworkCore",
                "RabbitMQ.Client",
                "Confluent.Kafka",
                "Microsoft.AspNetCore")
            .GetResult();

        result.IsSuccessful.Should().BeTrue();
    }

    [Fact]
    public void StorageSqlServerAssembly_ShouldNotDependOn_ForbiddenInfrastructure()
    {
        var result = Types.InAssembly(typeof(SqlServerProcessStore<>).Assembly)
            .ShouldNot()
            .HaveDependencyOnAny(
                "EricksonLopez.Processes.DependencyInjection",
                "Microsoft.EntityFrameworkCore",
                "RabbitMQ.Client",
                "Confluent.Kafka",
                "Microsoft.AspNetCore")
            .GetResult();

        result.IsSuccessful.Should().BeTrue();
    }

    [Fact]
    public void StorageOracleAssembly_ShouldNotDependOn_ForbiddenInfrastructure()
    {
        var result = Types.InAssembly(typeof(OracleProcessStore<>).Assembly)
            .ShouldNot()
            .HaveDependencyOnAny(
                "EricksonLopez.Processes.DependencyInjection",
                "Microsoft.EntityFrameworkCore",
                "RabbitMQ.Client",
                "Confluent.Kafka",
                "Microsoft.AspNetCore")
            .GetResult();

        result.IsSuccessful.Should().BeTrue();
    }

    [Fact]
    public void SystemTextJsonAssembly_ShouldNotDependOn_StorageOrExternalBrokers()
    {
        var result = Types.InAssembly(typeof(SystemTextJsonProcessStateSerializer<>).Assembly)
            .ShouldNot()
            .HaveDependencyOnAny(
                "EricksonLopez.Processes.Storage.Sqlite",
                "EricksonLopez.Processes.Storage.MySql",
                "EricksonLopez.Processes.Storage.MariaDb",
                "EricksonLopez.Processes.Storage.PostgreSql",
                "EricksonLopez.Processes.Storage.SqlServer",
                "EricksonLopez.Processes.Storage.Oracle",
                "Npgsql",
                "Microsoft.Data.SqlClient",
                "MySqlConnector",
                "Oracle.ManagedDataAccess.Client",
                "Dapper")
            .GetResult();

        result.IsSuccessful.Should().BeTrue();
    }

    [Fact]
    public void IntegrationEventsAssembly_ShouldOnlyDependOn_AbstractionsAndEventsContracts()
    {
        var result = Types.InAssembly(typeof(EventProcessDispatcher).Assembly)
            .ShouldNot()
            .HaveDependencyOnAny(
                "EricksonLopez.Processes.Storage.Sqlite",
                "EricksonLopez.Processes.Storage.MySql",
                "EricksonLopez.Processes.Storage.PostgreSql",
                "EricksonLopez.Processes.Storage.SqlServer",
                "EricksonLopez.Processes.Storage.Oracle")
            .GetResult();

        result.IsSuccessful.Should().BeTrue();
    }

    [Fact]
    public void IntegrationMediatorAssembly_ShouldOnlyDependOn_Abstractions()
    {
        var result = Types.InAssembly(typeof(MediatorProcessDispatcher).Assembly)
            .ShouldNot()
            .HaveDependencyOnAny(
                "EricksonLopez.Processes.Storage.Sqlite",
                "EricksonLopez.Processes.Storage.MySql",
                "EricksonLopez.Processes.Storage.PostgreSql",
                "EricksonLopez.Processes.Storage.SqlServer",
                "EricksonLopez.Processes.Storage.Oracle")
            .GetResult();

        result.IsSuccessful.Should().BeTrue();
    }

    [Fact]
    public void IntegrationOutboxAssembly_ShouldOnlyDependOn_Abstractions()
    {
        var result = Types.InAssembly(typeof(OutboxProcessDispatcher).Assembly)
            .ShouldNot()
            .HaveDependencyOnAny(
                "EricksonLopez.Processes.Storage.Sqlite",
                "EricksonLopez.Processes.Storage.MySql",
                "EricksonLopez.Processes.Storage.PostgreSql",
                "EricksonLopez.Processes.Storage.SqlServer",
                "EricksonLopez.Processes.Storage.Oracle")
            .GetResult();

        result.IsSuccessful.Should().BeTrue();
    }

    [Theory]
    [InlineData(typeof(ProcessId))]
    [InlineData(typeof(CorrelationId))]
    [InlineData(typeof(CausationId))]
    [InlineData(typeof(MessageId))]
    [InlineData(typeof(ProcessType))]
    [InlineData(typeof(ProcessVersion))]
    [InlineData(typeof(Revision))]
    public void ValueObjectStructs_ShouldBeValueTypesAndImplementRequiredInterfaces(Type type)
    {
        type.IsValueType.Should().BeTrue();
        typeof(ISpanFormattable).IsAssignableFrom(type).Should().BeTrue();
        typeof(IFormattable).IsAssignableFrom(type).Should().BeTrue();
        typeof(IComparable).IsAssignableFrom(type).Should().BeTrue();

        var interfaces = type.GetInterfaces();
        interfaces.Any(i => i.IsGenericType && i.GetGenericTypeDefinition() == typeof(IComparable<>)).Should().BeTrue();
        interfaces.Any(i => i.IsGenericType && i.GetGenericTypeDefinition() == typeof(IEquatable<>)).Should().BeTrue();
    }

    [Theory]
    [InlineData(typeof(ProcessInstance<>))]
    [InlineData(typeof(ProcessTransitionResult<>))]
    [InlineData(typeof(ProcessExecutionResult<>))]
    public void CoreModelRecords_ShouldBeSealedClasses(Type genericTypeDef)
    {
        genericTypeDef.IsClass.Should().BeTrue();
        genericTypeDef.IsSealed.Should().BeTrue();
    }

    [Theory]
    [InlineData(typeof(ProcessNotFoundException))]
    [InlineData(typeof(ConcurrencyConflictException))]
    [InlineData(typeof(InvalidProcessTransitionException))]
    [InlineData(typeof(CompensationFailedException))]
    public void DomainExceptions_ShouldInheritFromProcessException(Type exceptionType)
    {
        typeof(ProcessException).IsAssignableFrom(exceptionType).Should().BeTrue();
    }

    [Fact]
    public void ProductionAssemblies_ShouldHaveZeroObsoleteAttributes()
    {
        var assemblies = new[]
        {
            typeof(ProcessId).Assembly,
            typeof(ProcessCoordinator<>).Assembly,
            typeof(SqliteProcessStore<>).Assembly,
            typeof(SqlServerProcessStore<>).Assembly,
            typeof(PostgreSqlProcessStore<>).Assembly,
            typeof(MySqlProcessStore<>).Assembly,
            typeof(MariaDbProcessStore<>).Assembly,
            typeof(OracleProcessStore<>).Assembly,
            typeof(SystemTextJsonProcessStateSerializer<>).Assembly,
            typeof(EventProcessDispatcher).Assembly,
            typeof(MediatorProcessDispatcher).Assembly,
            typeof(OutboxProcessDispatcher).Assembly
        };

        foreach (var assembly in assemblies)
        {
            var typesWithObsolete = assembly.GetTypes()
                .Where(t => t.GetCustomAttributes(typeof(ObsoleteAttribute), inherit: false).Length > 0)
                .ToList();

            typesWithObsolete.Should().BeEmpty($"Assembly '{assembly.GetName().Name}' should contain zero [Obsolete] types.");
        }
    }
}
