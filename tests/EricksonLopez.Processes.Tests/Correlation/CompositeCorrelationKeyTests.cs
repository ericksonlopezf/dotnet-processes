// Copyright © Erickson Lopez. MIT License.
using System;
using AwesomeAssertions;
using EricksonLopez.Processes.Abstractions;
using Xunit;

namespace EricksonLopez.Processes.Tests.Correlation;

[Trait("Category", "Unit")]
public class CompositeCorrelationKeyTests
{
    private sealed record TestEvent(string OrderId, string CustomerId, string? Causation);

    private sealed class DefaultCorrelationExtractor : IProcessCorrelation<TestEvent>
    {
        public ProcessId ExtractProcessId(TestEvent @event) => ProcessId.FromGuid(Guid.Parse("11111111-1111-1111-1111-111111111111"));
        public CorrelationId ExtractCorrelationId(TestEvent @event) => CompositeCorrelationKey.From(@event.OrderId, @event.CustomerId).ToCorrelationId();
    }

    private sealed class OverridingCorrelationExtractor : IProcessCorrelation<TestEvent>
    {
        public ProcessId ExtractProcessId(TestEvent @event) => ProcessId.FromGuid(Guid.Parse("22222222-2222-2222-2222-222222222222"));
        public CorrelationId ExtractCorrelationId(TestEvent @event) => CorrelationId.From(@event.OrderId);
        public CausationId? ExtractCausationId(TestEvent @event) => @event.Causation is not null ? CausationId.From(@event.Causation) : null;
    }

    [Fact]
    public void CompositeCorrelationKey_Constructor_ValidParts_ShouldSetExpectedValue()
    {
        var single = new CompositeCorrelationKey("single");
        single.Value.Should().Be("single");
        single.ToString().Should().Be("single");

        var multi = new CompositeCorrelationKey("tenant-1", "order-100", "step-2");
        multi.Value.Should().Be("tenant-1:order-100:step-2");
        multi.ToString().Should().Be("tenant-1:order-100:step-2");
    }

    [Fact]
    public void CompositeCorrelationKey_Constructor_NullOrEmptyParts_ShouldThrowArgumentException()
    {
        var actNull = () => new CompositeCorrelationKey(null!);
        actNull.Should().Throw<ArgumentNullException>().WithParameterName("parts");

        var actEmpty = () => new CompositeCorrelationKey(Array.Empty<string>());
        actEmpty.Should().Throw<ArgumentException>()
            .WithParameterName("parts")
            .WithMessage("At least one key part must be provided.*");
    }

    [Theory]
    [InlineData(0, null)]
    [InlineData(0, "")]
    [InlineData(0, "   ")]
    [InlineData(1, null)]
    [InlineData(1, "")]
    [InlineData(1, "   ")]
    [InlineData(2, null)]
    [InlineData(2, "")]
    [InlineData(2, "   ")]
    public void CompositeCorrelationKey_Constructor_InvalidPartAtIndex_ShouldThrowWithExactIndex(int index, string? invalidValue)
    {
        var parts = new string[] { "valid-0", "valid-1", "valid-2" };
        parts[index] = invalidValue!;

        var act = () => new CompositeCorrelationKey(parts);
        act.Should().Throw<ArgumentException>()
            .WithParameterName("parts")
            .WithMessage($"Key part at index {index} cannot be null or whitespace.*");
    }

    [Fact]
    public void CompositeCorrelationKey_GenericFromFactories_ShouldPreserveArgumentOrderAndSeparator()
    {
        var key2 = CompositeCorrelationKey.From("partA", "partB");
        key2.Value.Should().Be("partA:partB");
        key2.ToString().Should().Be("partA:partB");

        var key3 = CompositeCorrelationKey.From("partA", "partB", "partC");
        key3.Value.Should().Be("partA:partB:partC");
        key3.ToString().Should().Be("partA:partB:partC");

        var key4 = CompositeCorrelationKey.From("partA", "partB", "partC", "partD");
        key4.Value.Should().Be("partA:partB:partC:partD");
        key4.ToString().Should().Be("partA:partB:partC:partD");
    }

    [Fact]
    public void CompositeCorrelationKey_ToCorrelationId_ShouldMatchExactDeterministicUuidV5StyleGuid()
    {
        var key = new CompositeCorrelationKey("tenant-alpha", "order-999");
        var corrId = key.ToCorrelationId();

        // Compute expected bytes manually
        var bytes = System.Text.Encoding.UTF8.GetBytes("tenant-alpha:order-999");
        var hash = System.Security.Cryptography.SHA256.HashData(bytes);
        var expectedBytes = new byte[16];
        Array.Copy(hash, 0, expectedBytes, 0, 16);
        expectedBytes[6] = (byte)((expectedBytes[6] & 0x0F) | 0x40);
        expectedBytes[8] = (byte)((expectedBytes[8] & 0x3F) | 0x80);
        var expectedGuid = new Guid(expectedBytes);

        corrId.Value.Should().Be(expectedGuid.ToString());

        // Verify UUID version 4/5 bit in byte 6
        var actualGuidBytes = Guid.Parse(corrId.Value).ToByteArray();
        // Byte 7 or 6 depending on endianness:
        // In RFC 4122 / UUID representation, check time_hi_and_version and clock_seq_hi_and_reserved:
        (expectedBytes[6] & 0xF0).Should().Be(0x40);
        (expectedBytes[8] & 0xC0).Should().Be(0x80);

        // Verify second key gives exact different Guid
        var key2 = new CompositeCorrelationKey("tenant-beta", "order-999");
        var corrId2 = key2.ToCorrelationId();
        corrId2.Value.Should().NotBe(corrId.Value);
    }

    [Fact]
    public void CompositeCorrelationKey_LoopIndexValidation_SingleAndMultipleElements()
    {
        // Array with single whitespace element
        var actSingleWs = () => new CompositeCorrelationKey("   ");
        actSingleWs.Should().Throw<ArgumentException>()
            .WithParameterName("parts")
            .WithMessage("Key part at index 0 cannot be null or whitespace.*");

        // Array with 4 elements, last element is whitespace
        var actLastWs = () => new CompositeCorrelationKey("a", "b", "c", "  ");
        actLastWs.Should().Throw<ArgumentException>()
            .WithParameterName("parts")
            .WithMessage("Key part at index 3 cannot be null or whitespace.*");

        // Array with 4 elements, first element is whitespace
        var actFirstWs = () => new CompositeCorrelationKey("  ", "b", "c", "d");
        actFirstWs.Should().Throw<ArgumentException>()
            .WithParameterName("parts")
            .WithMessage("Key part at index 0 cannot be null or whitespace.*");
    }

    [Fact]
    public void CompositeCorrelationKey_EqualityAndRecordSemantics_ShouldWork()
    {
        var key1 = CompositeCorrelationKey.From("a", "b");
        var key2 = CompositeCorrelationKey.From("a", "b");
        var key3 = CompositeCorrelationKey.From("a", "c");

        (key1 == key2).Should().BeTrue();
        (key1 != key3).Should().BeTrue();
        key1.Equals(key2).Should().BeTrue();
        key1.Equals((object)key2).Should().BeTrue();
        key1.Equals(key3).Should().BeFalse();
        key1.Equals((object)key3).Should().BeFalse();
        key1.Equals((object?)null).Should().BeFalse();
        key1.GetHashCode().Should().Be(key2.GetHashCode());

        CompositeCorrelationKey defaultKey1 = default;
        CompositeCorrelationKey defaultKey2 = default;
        (defaultKey1 == defaultKey2).Should().BeTrue();
        defaultKey1.Equals(defaultKey2).Should().BeTrue();
        defaultKey1.Equals((object)defaultKey2).Should().BeTrue();
        defaultKey1.Value.Should().BeNull();
        defaultKey1.ToString().Should().BeNull();

        var actDefault = () => defaultKey1.ToCorrelationId();
        actDefault.Should().Throw<ArgumentNullException>();
    }

    [Fact]
    public void IProcessCorrelation_DefaultAndCustomImplementations_ShouldWork()
    {
        IProcessCorrelation<TestEvent> defaultExtractor = new DefaultCorrelationExtractor();
        var evtWithCausation = new TestEvent("order-1", "cust-1", "cause-99");
        var evtWithoutCausation = new TestEvent("order-2", "cust-2", null);

        // Default interface method implementation returns null for ExtractCausationId
        defaultExtractor.ExtractProcessId(evtWithCausation).Value.Should().Be(Guid.Parse("11111111-1111-1111-1111-111111111111"));
        defaultExtractor.ExtractCorrelationId(evtWithCausation).Should().NotBeNull();
        defaultExtractor.ExtractCausationId(evtWithCausation).Should().BeNull();

        // Custom overriding extractor returns causation when present
        IProcessCorrelation<TestEvent> overridingExtractor = new OverridingCorrelationExtractor();
        overridingExtractor.ExtractProcessId(evtWithCausation).Value.Should().Be(Guid.Parse("22222222-2222-2222-2222-222222222222"));
        overridingExtractor.ExtractCorrelationId(evtWithCausation).Value.Should().Be("order-1");
        overridingExtractor.ExtractCausationId(evtWithCausation)?.Value.Should().Be("cause-99");
        overridingExtractor.ExtractCausationId(evtWithoutCausation).Should().BeNull();
    }
}
