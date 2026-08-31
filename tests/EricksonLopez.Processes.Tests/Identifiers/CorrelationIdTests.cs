// Copyright © Erickson Lopez. MIT License.
using System;
using System.Globalization;
using AwesomeAssertions;
using EricksonLopez.Processes.Abstractions;
using Xunit;

namespace EricksonLopez.Processes.Tests.Identifiers;

#pragma warning disable CA1305 // Specify IFormatProvider

[Trait("Category", "Unit")]
public class CorrelationIdTests
{
    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void CorrelationId_Constructor_ShouldThrowOnInvalidInput(string? invalid)
    {
        var act = () => new CorrelationId(invalid!);
        act.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void CorrelationId_FactoriesAndProperties_ShouldWork()
    {
        var corrNew = CorrelationId.NewId();
        corrNew.Value.Should().NotBeNullOrWhiteSpace();
        corrNew.ToString().Should().Be(corrNew.Value);
        corrNew.ToString(null, null).Should().Be(corrNew.Value);
        corrNew.ToString("G", null).Should().Be(corrNew.Value);
        corrNew.ToString("G", CultureInfo.InvariantCulture).Should().Be(corrNew.Value);

        var guid = Guid.NewGuid();
        CorrelationId.FromGuid(guid).Value.Should().Be(guid.ToString());
        CorrelationId.From(guid.ToString()).Value.Should().Be(guid.ToString());
        CorrelationId.From(guid).Value.Should().Be(guid.ToString());
        CorrelationId.FromString("c-1").Value.Should().Be("c-1");
        CorrelationId.From("c-1").Value.Should().Be("c-1");

        CorrelationId defCorr = default;
        defCorr.ToString().Should().BeEmpty();
        defCorr.ToString(null, null).Should().BeEmpty();
        defCorr.ToString("G", null).Should().BeEmpty();
        defCorr.ToString("G", CultureInfo.InvariantCulture).Should().BeEmpty();
        defCorr.Value.Should().BeNull();
    }

    [Fact]
    public void CorrelationId_ParseAndTryParse_ShouldWork()
    {
        CorrelationId.Parse("corr", CultureInfo.InvariantCulture).Value.Should().Be("corr");
        CorrelationId.Parse("corr").Value.Should().Be("corr");

        CorrelationId.TryParse("c", CultureInfo.InvariantCulture, out var c).Should().BeTrue();
        c.Value.Should().Be("c");

        CorrelationId.TryParse("c", null, out var cNullProv).Should().BeTrue();
        cNullProv.Value.Should().Be("c");

        CorrelationId.TryParse("c".AsSpan(), CultureInfo.InvariantCulture, out var cSpan).Should().BeTrue();
        cSpan.Value.Should().Be("c");

        CorrelationId.TryParse("c".AsSpan(), null, out var cSpanNullProv).Should().BeTrue();
        cSpanNullProv.Value.Should().Be("c");

        CorrelationId.Parse("c".AsSpan(), CultureInfo.InvariantCulture).Value.Should().Be("c");
        CorrelationId.Parse("c".AsSpan()).Value.Should().Be("c");

        CorrelationId.TryParse((string?)null, CultureInfo.InvariantCulture, out var nullRes).Should().BeFalse();
        nullRes.Should().Be(default);

        CorrelationId.TryParse("", CultureInfo.InvariantCulture, out var emptyRes).Should().BeFalse();
        emptyRes.Should().Be(default);

        CorrelationId.TryParse("  ", CultureInfo.InvariantCulture, out var wsRes).Should().BeFalse();
        wsRes.Should().Be(default);

        CorrelationId.TryParse("  ".AsSpan(), CultureInfo.InvariantCulture, out var wsSpanRes).Should().BeFalse();
        wsSpanRes.Should().Be(default);

        CorrelationId.TryParse(ReadOnlySpan<char>.Empty, CultureInfo.InvariantCulture, out var emptySpanRes).Should().BeFalse();
        emptySpanRes.Should().Be(default);

        var actCorrEmptySpan = () => CorrelationId.Parse(ReadOnlySpan<char>.Empty);
        actCorrEmptySpan.Should().Throw<ArgumentException>()
            .WithParameterName("s")
            .WithMessage("Span cannot be empty or whitespace.*");

        var actCorrWsSpan = () => CorrelationId.Parse("  ".AsSpan(), CultureInfo.InvariantCulture);
        actCorrWsSpan.Should().Throw<ArgumentException>()
            .WithParameterName("s")
            .WithMessage("Span cannot be empty or whitespace.*");

        var actNullStr = () => CorrelationId.Parse((string)null!);
        actNullStr.Should().Throw<ArgumentException>();

        var actEmptyStr = () => CorrelationId.Parse("");
        actEmptyStr.Should().Throw<ArgumentException>();

        var actWsStr = () => CorrelationId.Parse("   ");
        actWsStr.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void CorrelationId_TryFormat_ExactBufferAndSmallerBuffer_ShouldBehaveCorrectly()
    {
        var id = CorrelationId.From("test-correlation");
        var exactLength = "test-correlation".Length;

        // Exact buffer length (kills >= mutated to >)
        Span<char> exactBuffer = stackalloc char[exactLength];
        var successExact = id.TryFormat(exactBuffer, out var charsWrittenExact, default, null);
        successExact.Should().BeTrue();
        charsWrittenExact.Should().Be(exactLength);
        exactBuffer.ToString().Should().Be("test-correlation");

        // Larger buffer
        Span<char> largerBuffer = stackalloc char[exactLength + 10];
        var successLarger = id.TryFormat(largerBuffer, out var charsWrittenLarger, default, null);
        successLarger.Should().BeTrue();
        charsWrittenLarger.Should().Be(exactLength);
        largerBuffer[..charsWrittenLarger].ToString().Should().Be("test-correlation");

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
        CorrelationId defaultId = default;
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
    public void CorrelationId_ComparisonsAndOperators_ShouldWork()
    {
        var c1 = CorrelationId.From("A");
        var c2 = CorrelationId.From("B");
        var c1Copy = CorrelationId.From("a"); // Case-insensitive

        c1.CompareTo(c2).Should().BeLessThan(0);
        c2.CompareTo(c1).Should().BeGreaterThan(0);
        c1.CompareTo(c1Copy).Should().Be(0);

        // Differentiate OrdinalIgnoreCase from Culture comparison (e.g. sharp s 'ß' vs 'SS')
        var idSharpS = CorrelationId.From("ß");
        var idDoubleS = CorrelationId.From("SS");
        idSharpS.CompareTo(idDoubleS).Should().NotBe(0);

        // Default comparison
        CorrelationId def1 = default;
        CorrelationId def2 = default;
        def1.CompareTo(def2).Should().Be(0);
        def1.CompareTo(c1).Should().BeLessThan(0);
        c1.CompareTo(def1).Should().BeGreaterThan(0);

        (c1 < c2).Should().BeTrue();
        (c1 < c1Copy).Should().BeFalse();
        (c2 < c1).Should().BeFalse();

        (c1 <= c2).Should().BeTrue();
        (c1 <= c1Copy).Should().BeTrue();
        (c2 <= c1).Should().BeFalse();

        (c2 > c1).Should().BeTrue();
        (c1 > c1Copy).Should().BeFalse();
        (c1 > c2).Should().BeFalse();

        (c2 >= c1).Should().BeTrue();
        (c1 >= c1Copy).Should().BeTrue();
        (c1 >= c2).Should().BeFalse();

        ((IComparable)c1).CompareTo(c2).Should().BeLessThan(0);
        ((IComparable)c2).CompareTo(c1).Should().BeGreaterThan(0);
        ((IComparable)c1).CompareTo(c1Copy).Should().Be(0);
        ((IComparable)c1).CompareTo(null).Should().Be(1);

        var act = () => ((IComparable)c1).CompareTo(999);
        act.Should().Throw<ArgumentException>()
            .WithMessage("Object must be of type CorrelationId*")
            .WithParameterName("obj");

        string sC = c1;
        sC.Should().Be("A");

        var explicitCorr = (CorrelationId)"A";
        explicitCorr.Should().Be(c1);
    }
}
