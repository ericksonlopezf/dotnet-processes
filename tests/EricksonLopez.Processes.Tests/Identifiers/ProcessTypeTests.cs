// Copyright © Erickson Lopez. MIT License.
using System;
using System.Globalization;
using AwesomeAssertions;
using EricksonLopez.Processes.Abstractions;
using Xunit;

namespace EricksonLopez.Processes.Tests.Identifiers;

#pragma warning disable CA1305 // Specify IFormatProvider

[Trait("Category", "Unit")]
public class ProcessTypeTests
{
    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void ProcessType_Constructor_ShouldThrowOnInvalidInput(string? invalid)
    {
        var act = () => new ProcessType(invalid!);
        act.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void ProcessType_FactoriesAndProperties_ShouldWork()
    {
        var type1 = ProcessType.From("order.fulfillment");
        var type2 = ProcessType.FromString("order.fulfillment");

        type1.Value.Should().Be("order.fulfillment");
        type2.Value.Should().Be("order.fulfillment");
        type1.ToString().Should().Be("order.fulfillment");
        type1.ToString(null, null).Should().Be("order.fulfillment");
        type1.ToString("G", null).Should().Be("order.fulfillment");
        type1.ToString("G", CultureInfo.InvariantCulture).Should().Be("order.fulfillment");
        type1.Should().Be(type2);

        ProcessType defaultType = default;
        defaultType.ToString().Should().BeEmpty();
        defaultType.ToString(null, null).Should().BeEmpty();
        defaultType.ToString("G", null).Should().BeEmpty();
        defaultType.ToString("G", CultureInfo.InvariantCulture).Should().BeEmpty();
        defaultType.Value.Should().BeNull();
    }

    [Fact]
    public void ProcessType_ParseAndTryParse_ShouldWork()
    {
        var parsed = ProcessType.Parse("payment.process", CultureInfo.InvariantCulture);
        parsed.Value.Should().Be("payment.process");

        var parsedNoProvider = ProcessType.Parse("payment.process");
        parsedNoProvider.Value.Should().Be("payment.process");

        ProcessType.TryParse("inventory.hold", CultureInfo.InvariantCulture, out var valid).Should().BeTrue();
        valid.Value.Should().Be("inventory.hold");

        ProcessType.TryParse("inventory.hold", null, out var validNullProv).Should().BeTrue();
        validNullProv.Value.Should().Be("inventory.hold");

        ProcessType.TryParse("inventory.hold".AsSpan(), CultureInfo.InvariantCulture, out var validSpan).Should().BeTrue();
        validSpan.Value.Should().Be("inventory.hold");

        ProcessType.TryParse("inventory.hold".AsSpan(), null, out var validSpanNullProv).Should().BeTrue();
        validSpanNullProv.Value.Should().Be("inventory.hold");

        var parsedSpanWithProv = ProcessType.Parse("inventory.hold".AsSpan(), CultureInfo.InvariantCulture);
        parsedSpanWithProv.Value.Should().Be("inventory.hold");

        var parsedSpan = ProcessType.Parse("inventory.hold".AsSpan());
        parsedSpan.Value.Should().Be("inventory.hold");

        ProcessType.TryParse(null, CultureInfo.InvariantCulture, out var nullResult).Should().BeFalse();
        nullResult.Should().Be(default);

        ProcessType.TryParse("", CultureInfo.InvariantCulture, out var emptyStrResult).Should().BeFalse();
        emptyStrResult.Should().Be(default);

        ProcessType.TryParse("   ", CultureInfo.InvariantCulture, out var emptyResult).Should().BeFalse();
        emptyResult.Should().Be(default);

        ProcessType.TryParse("   ".AsSpan(), CultureInfo.InvariantCulture, out var emptySpanResult).Should().BeFalse();
        emptySpanResult.Should().Be(default);

        ProcessType.TryParse(ReadOnlySpan<char>.Empty, CultureInfo.InvariantCulture, out var zeroSpanResult).Should().BeFalse();
        zeroSpanResult.Should().Be(default);

        var actSpanEmpty = () => ProcessType.Parse("   ".AsSpan());
        actSpanEmpty.Should().Throw<ArgumentException>()
            .WithParameterName("s")
            .WithMessage("Span cannot be empty or whitespace.*");

        var actZeroSpan = () => ProcessType.Parse(ReadOnlySpan<char>.Empty);
        actZeroSpan.Should().Throw<ArgumentException>()
            .WithParameterName("s")
            .WithMessage("Span cannot be empty or whitespace.*");

        var actNullStr = () => ProcessType.Parse((string)null!);
        actNullStr.Should().Throw<ArgumentException>();

        var actEmptyStr = () => ProcessType.Parse("");
        actEmptyStr.Should().Throw<ArgumentException>();

        var actWsStr = () => ProcessType.Parse("   ");
        actWsStr.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void ProcessType_TryFormat_ExactBufferAndSmallerBuffer_ShouldBehaveCorrectly()
    {
        var id = ProcessType.From("order.billing");
        var exactLength = "order.billing".Length;

        // Exact buffer length (kills >= mutated to >)
        Span<char> exactBuffer = stackalloc char[exactLength];
        var successExact = id.TryFormat(exactBuffer, out var charsWrittenExact, default, null);
        successExact.Should().BeTrue();
        charsWrittenExact.Should().Be(exactLength);
        exactBuffer.ToString().Should().Be("order.billing");

        // Larger buffer
        Span<char> largerBuffer = stackalloc char[exactLength + 10];
        var successLarger = id.TryFormat(largerBuffer, out var charsWrittenLarger, default, null);
        successLarger.Should().BeTrue();
        charsWrittenLarger.Should().Be(exactLength);
        largerBuffer[..charsWrittenLarger].ToString().Should().Be("order.billing");

        // Smaller buffer (kills >= mutated to <=)
        Span<char> smallerBuffer = stackalloc char[exactLength - 1];
        var successSmaller = id.TryFormat(smallerBuffer, out var charsWrittenSmaller, default, null);
        successSmaller.Should().BeFalse();
        charsWrittenSmaller.Should().Be(0); // kills charsWritten = 0 removal

        // Zero-length buffer with non-default ID
        Span<char> zeroBuffer = stackalloc char[0];
        var successZero = id.TryFormat(zeroBuffer, out var charsWrittenZero, default, null);
        successZero.Should().BeFalse();
        charsWrittenZero.Should().Be(0);

        // One-char buffer with multi-char ID
        Span<char> oneBuffer = stackalloc char[1];
        var successOne = id.TryFormat(oneBuffer, out var charsWrittenOne, default, null);
        successOne.Should().BeFalse();
        charsWrittenOne.Should().Be(0);

        // Empty destination buffer with default (null Value)
        ProcessType defaultId = default;
        Span<char> emptyBuffer = stackalloc char[0];
        var successDefaultEmpty = defaultId.TryFormat(emptyBuffer, out var charsWrittenDefaultEmpty, default, null);
        successDefaultEmpty.Should().BeTrue();
        charsWrittenDefaultEmpty.Should().Be(0);

        Span<char> defaultLargerBuffer = stackalloc char[10];
        var successDefaultLarger = defaultId.TryFormat(defaultLargerBuffer, out var charsWrittenDefaultLarger, default, null);
        successDefaultLarger.Should().BeTrue();
        charsWrittenDefaultLarger.Should().Be(0);
    }

    [Fact]
    public void ProcessType_ComparisonsAndOperators_ShouldWork()
    {
        var tA = ProcessType.From("A");
        var tB = ProcessType.From("B");
        var tACopy = ProcessType.From("a"); // Case-insensitive comparison

        tA.CompareTo(tB).Should().BeLessThan(0);
        tB.CompareTo(tA).Should().BeGreaterThan(0);
        tA.CompareTo(tACopy).Should().Be(0);

        // Differentiate OrdinalIgnoreCase from Culture comparison (e.g. sharp s 'ß' vs 'SS')
        var idSharpS = ProcessType.From("ß");
        var idDoubleS = ProcessType.From("SS");
        idSharpS.CompareTo(idDoubleS).Should().NotBe(0);

        // Default comparison
        ProcessType def1 = default;
        ProcessType def2 = default;
        def1.CompareTo(def2).Should().Be(0);
        def1.CompareTo(tA).Should().BeLessThan(0);
        tA.CompareTo(def1).Should().BeGreaterThan(0);

        (tA < tB).Should().BeTrue();
        (tA < tACopy).Should().BeFalse();
        (tB < tA).Should().BeFalse();

        (tA <= tB).Should().BeTrue();
        (tA <= tACopy).Should().BeTrue();
        (tB <= tA).Should().BeFalse();

        (tB > tA).Should().BeTrue();
        (tA > tACopy).Should().BeFalse();
        (tA > tB).Should().BeFalse();

        (tB >= tA).Should().BeTrue();
        (tA >= tACopy).Should().BeTrue();
        (tA >= tB).Should().BeFalse();

        ((IComparable)tA).CompareTo(tB).Should().BeLessThan(0);
        ((IComparable)tB).CompareTo(tA).Should().BeGreaterThan(0);
        ((IComparable)tA).CompareTo(tACopy).Should().Be(0);
        ((IComparable)tA).CompareTo(null).Should().Be(1);

        var act = () => ((IComparable)tA).CompareTo(123);
        act.Should().Throw<ArgumentException>()
            .WithMessage("Object must be of type ProcessType*")
            .WithParameterName("obj");

        string implicitStr = tA;
        implicitStr.Should().Be("A");

        var explicitType = (ProcessType)"A";
        explicitType.Should().Be(tA);
    }
}
