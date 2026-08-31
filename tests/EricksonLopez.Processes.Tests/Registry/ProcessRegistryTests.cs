// Copyright © Erickson Lopez. MIT License.
using System;
using System.Collections.Concurrent;
using System.Linq;
using System.Threading.Tasks;
using AwesomeAssertions;
using EricksonLopez.Processes.Abstractions;
using Xunit;

namespace EricksonLopez.Processes.Tests.Registry;

[Trait("Category", "Unit")]
public class ProcessRegistryTests
{
    private sealed class MinimalProcessRegistry : IProcessRegistry
    {
        public bool IsRegistered(ProcessType processType, ProcessVersion version) => false;
    }

    [Fact]
    public void IProcessRegistry_DefaultInterfaceRegisteredProcesses_ShouldReturnEmptyCollection()
    {
        IProcessRegistry registry = new MinimalProcessRegistry();
        registry.RegisteredProcesses.Should().BeEmpty();
    }

    [Fact]
    public void ProcessRegistry_RegisteredProcesses_WhenEmpty_ShouldReturnEmptyCollection()
    {
        var registry = new ProcessRegistry();
        registry.RegisteredProcesses.Should().BeEmpty();
    }

    [Fact]
    public void ProcessRegistry_RegisteredProcesses_WhenPopulated_ShouldReturnAllRegisteredEntries()
    {
        var registry = new ProcessRegistry();
        var typeA = ProcessType.From("type.a");
        var typeB = ProcessType.From("type.b");
        var v1 = ProcessVersion.From(1);
        var v2 = ProcessVersion.From(2);

        registry.Register(typeA, v1);
        registry.Register(typeA, v2);
        registry.Register(typeB, v1);

        var registered = registry.RegisteredProcesses;
        registered.Should().HaveCount(3);
        registered.Should().Contain((typeA, v1));
        registered.Should().Contain((typeA, v2));
        registered.Should().Contain((typeB, v1));
    }

    [Fact]
    public void Register_SingleProcessVersion_ShouldBeRegisteredAndIdentifiable()
    {
        var registry = new ProcessRegistry();
        var type = ProcessType.From("order.fulfillment");
        var v1 = ProcessVersion.From(1);
        var v2 = ProcessVersion.From(2);

        registry.IsRegistered(type, v1).Should().BeFalse();
        registry.IsRegistered(type, v2).Should().BeFalse();

        registry.Register(type, v1);

        registry.IsRegistered(type, v1).Should().BeTrue();
        registry.IsRegistered(type, v2).Should().BeFalse();

        registry.Register(type, v2);

        registry.IsRegistered(type, v1).Should().BeTrue();
        registry.IsRegistered(type, v2).Should().BeTrue();
        registry.IsRegistered(ProcessType.From("other.process"), v1).Should().BeFalse();
    }

    [Fact]
    public void Register_DuplicateRegistration_ShouldBeIdempotent()
    {
        var registry = new ProcessRegistry();
        var type = ProcessType.From("invoice.audit");
        var version = ProcessVersion.From(1);

        registry.Register(type, version);
        registry.Register(type, version); // Duplicate register

        registry.IsRegistered(type, version).Should().BeTrue();
        registry.RegisteredProcesses.Should().ContainSingle().Which.Should().Be((type, version));
    }

    [Fact]
    public void IsRegistered_UnregisteredTypesAndVersions_ShouldReturnFalse()
    {
        var registry = new ProcessRegistry();
        var typeA = ProcessType.From("type.a");
        var typeB = ProcessType.From("type.b");

        registry.Register(typeA, ProcessVersion.From(1));

        registry.IsRegistered(typeA, ProcessVersion.From(2)).Should().BeFalse();
        registry.IsRegistered(typeB, ProcessVersion.From(1)).Should().BeFalse();
        registry.IsRegistered(ProcessType.From("unknown"), ProcessVersion.From(99)).Should().BeFalse();
    }

    [Fact]
    [Trait("Category", "Concurrency")]
    public void Register_ConcurrentRegistrationsAndLookups_ShouldBeThreadSafeAndConsistent()
    {
        const int iterations = 200;
        var registry = new ProcessRegistry();
        var registeredTypes = new ConcurrentBag<(ProcessType Type, ProcessVersion Version)>();

        // Concurrent registrations from multiple threads
        Parallel.For(0, iterations, i =>
        {
            var type = ProcessType.From($"process.type.{i % 20}");
            var version = ProcessVersion.From((i % 5) + 1);

            registry.Register(type, version);
            registeredTypes.Add((type, version));

            // Concurrent read check during registration
            registry.IsRegistered(type, version).Should().BeTrue();
        });

        // Final verification: all registered pairs must be queryable and registered
        foreach (var (type, version) in registeredTypes.Distinct())
        {
            registry.IsRegistered(type, version).Should().BeTrue();
        }

        registry.RegisteredProcesses.Should().HaveCount(registeredTypes.Distinct().Count());
    }
}
