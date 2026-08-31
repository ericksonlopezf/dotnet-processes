// Copyright © Erickson Lopez. MIT License.
using System;
using AwesomeAssertions;
using EricksonLopez.Processes.Abstractions;
using Xunit;

namespace EricksonLopez.Processes.Tests.Attributes;

[Trait("Category", "Unit")]
public class MetadataAttributesTests
{
    [Fact]
    public void ProcessDefinitionAttribute_DefaultVersion_ShouldSetProperties()
    {
        var attr = new ProcessDefinitionAttribute("order.fulfillment");
        attr.ProcessType.Should().Be("order.fulfillment");
        attr.Version.Should().Be(1);
    }

    [Fact]
    public void ProcessDefinitionAttribute_CustomVersion_ShouldSetProperties()
    {
        var attr = new ProcessDefinitionAttribute("order.fulfillment", 5);
        attr.ProcessType.Should().Be("order.fulfillment");
        attr.Version.Should().Be(5);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void ProcessDefinitionAttribute_InvalidProcessType_ShouldThrowArgumentException(string? invalidType)
    {
        var act1 = () => new ProcessDefinitionAttribute(invalidType!);
        act1.Should().Throw<ArgumentException>();

        var act2 = () => new ProcessDefinitionAttribute(invalidType!, 3);
        act2.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void SagaDefinitionAttribute_DefaultVersion_ShouldSetProperties()
    {
        var attr = new SagaDefinitionAttribute("travel.saga");
        attr.ProcessType.Should().Be("travel.saga");
        attr.Version.Should().Be(1);
    }

    [Fact]
    public void SagaDefinitionAttribute_CustomVersion_ShouldSetProperties()
    {
        var attr = new SagaDefinitionAttribute("travel.saga", 4);
        attr.ProcessType.Should().Be("travel.saga");
        attr.Version.Should().Be(4);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void SagaDefinitionAttribute_InvalidProcessType_ShouldThrowArgumentException(string? invalidType)
    {
        var act1 = () => new SagaDefinitionAttribute(invalidType!);
        act1.Should().Throw<ArgumentException>();

        var act2 = () => new SagaDefinitionAttribute(invalidType!, 4);
        act2.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void ProcessTypeAttribute_Valid_ShouldSetProcessType()
    {
        var attr = new ProcessTypeAttribute("billing.account");
        attr.ProcessType.Should().Be("billing.account");
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void ProcessTypeAttribute_Invalid_ShouldThrowArgumentException(string? invalidType)
    {
        var act = () => new ProcessTypeAttribute(invalidType!);
        act.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void ProcessHandlerAttribute_Constructors_ShouldSetCanInitiate()
    {
        var attrDefault = new ProcessHandlerAttribute();
        attrDefault.CanInitiate.Should().BeFalse();

        var attrExplicitFalse = new ProcessHandlerAttribute(false);
        attrExplicitFalse.CanInitiate.Should().BeFalse();

        var attrExplicitTrue = new ProcessHandlerAttribute(true);
        attrExplicitTrue.CanInitiate.Should().BeTrue();
    }
}
