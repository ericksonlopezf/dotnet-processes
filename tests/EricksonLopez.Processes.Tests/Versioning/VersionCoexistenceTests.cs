// Copyright © Erickson Lopez. MIT License.
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using AwesomeAssertions;
using EricksonLopez.Processes.Abstractions;
using Xunit;

namespace EricksonLopez.Processes.Tests.Versioning;

[Trait("Category", "Unit")]
public class VersionCoexistenceTests
{
    private sealed record StateV1(string OrderId) : IProcessState;
    private sealed record StateV2(string OrderId, string Channel) : IProcessState;

    private sealed class ProcessV1 : IProcess<StateV1>
    {
        public ProcessType Type => ProcessType.From("order.fulfillment");
        public ProcessVersion Version => ProcessVersion.From(1);
    }

    private sealed class ProcessV2 : IProcess<StateV2>
    {
        public ProcessType Type => ProcessType.From("order.fulfillment");
        public ProcessVersion Version => ProcessVersion.From(2);
    }

    [Fact]
    public void ProcessRegistry_ShouldSupportVersionCoexistence()
    {
        var registry = new ProcessRegistry();
        var v1 = new ProcessV1();
        var v2 = new ProcessV2();

        registry.Register(v1.Type, v1.Version);
        registry.Register(v2.Type, v2.Version);

        registry.IsRegistered(ProcessType.From("order.fulfillment"), ProcessVersion.From(1)).Should().BeTrue();
        registry.IsRegistered(ProcessType.From("order.fulfillment"), ProcessVersion.From(2)).Should().BeTrue();
        registry.IsRegistered(ProcessType.From("order.fulfillment"), ProcessVersion.From(3)).Should().BeFalse();
    }
}





