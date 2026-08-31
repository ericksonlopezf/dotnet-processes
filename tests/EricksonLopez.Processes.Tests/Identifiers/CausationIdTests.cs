// Copyright © Erickson Lopez. MIT License.
using System;
using System.Globalization;
using AwesomeAssertions;
using EricksonLopez.Processes.Abstractions;
using Xunit;

namespace EricksonLopez.Processes.Tests.Identifiers;

#pragma warning disable CA1305 // Specify IFormatProvider

[Trait("Category", "Unit")]
public class CausationIdTests
{
    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void CausationId_Constructor_ShouldThrowOnInvalidInput(string? invalid)
    {
        var act = () => new CausationId(invalid!);
        act.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void CausationId_FactoriesAndProperties_ShouldWork()
    {
        var causeNew = CausationId.NewId();
        causeNew.Value.Should().NotBeNullOrWhiteSpace();
        causeNew.ToString().Should().Be(causeNew.Value);
        causeNew.ToString(null, null).Should().Be(causeNew.Value);
        causeNew.ToString("G", null).Should().Be(causeNew.Value);
        causeNew.ToString("G", CultureInfo.InvariantCulture).Should().Be(causeNew.Value);

        var guid = Guid.NewGuid();
        CausationId.FromGuid(guid).Value.Should().Be(guid.ToString());
        CausationId.From(guid.ToString()).Value.Should().Be(guid.ToString());
        CausationId.FromString("cause-1").Value.Should().Be("cause-1");
        CausationId.From("cause-1").Value.Should().Be("cause-1");

        CausationId defCause = default;
        defCause.ToString().Should().BeEmpty();
        defCause.ToString(null, null).Should().BeEmpty();
        defCause.ToString("G", null).Should().BeEmpty();
        defCause.ToString("G", CultureInfo.InvariantCulture).Should().BeEmpty();
        defCause.Value.Should().BeNull();
    }

    [Fact]
    public void CausationId_ParseAndTryParse_ShouldWork()
    {
        CausationId.Parse("cause", CultureInfo.InvariantCulture).Value.Should().Be("cause");
        CausationId.Parse("cause").Value.Should().Be("cause");

        CausationId.TryParse("cause", CultureInfo.InvariantCulture, out var ca).Should().BeTrue();
        ca.Value.Should().Be("cause");

        CausationId.TryParse("cause", null, out var caNullProv).Should().BeTrue();
        caNullProv.Value.Should().Be("cause");

        CausationId.TryParse("cause".AsSpan(), CultureInfo.InvariantCulture, out var caSpan).Should().BeTrue();
        caSpan.Value.Should().Be("cause");

        CausationId.TryParse("cause".AsSpan(), null, out var caSpanNullProv).Should().BeTrue();
        caSpanNullProv.Value.Should().Be("cause");

        CausationId.Parse("cause".AsSpan(), CultureInfo.InvariantCulture).Value.Should().Be("cause");
        CausationId.Parse("cause".AsSpan()).Value.Should().Be("cause");

        CausationId.TryParse((string?)null, CultureInfo.InvariantCulture, out var nullRes).Should().BeFalse();
        nullRes.Should().Be(default);

        CausationId.TryParse("", CultureInfo.InvariantCulture, out var emptyRes).Should().BeFalse();
        emptyRes.Should().Be(default);

        CausationId.TryParse("  ", CultureInfo.InvariantCulture, out var wsRes).Should().BeFalse();
        wsRes.Should().Be(default);

        CausationId.TryParse("  ".AsSpan(), CultureInfo.InvariantCulture, out var wsSpanRes).Should().BeFalse();
        wsSpanRes.Should().Be(default);

        CausationId.TryParse(ReadOnlySpan<char>.Empty, CultureInfo.InvariantCulture, out var emptySpanRes).Should().BeFalse();
        emptySpanRes.Should().Be(default);

        var actCauseEmptySpan = () => CausationId.Parse(ReadOnlySpan<char>.Empty);
        actCauseEmptySpan.Should().Throw<ArgumentException>()
            .WithParameterName("s")
            .WithMessage("Span cannot be empty or whitespace.*");

        var actCauseWsSpan = () => CausationId.Parse("  ".AsSpan(), CultureInfo.InvariantCulture);
        actCauseWsSpan.Should().Throw<ArgumentException>()
            .WithParameterName("s")
            .WithMessage("Span cannot be empty or whitespace.*");

        var actNullStr = () => CausationId.Parse((string)null!);
        actNullStr.Should().Throw<ArgumentException>();

        var actEmptyStr = () => CausationId.Parse("");
        actEmptyStr.Should().Throw<ArgumentException>();

        var actWsStr = () => CausationId.Parse("   ");
        actWsStr.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void CausationId_TryFormat_ExactBufferAndSmallerBuffer_ShouldBehaveCorrectly()
    {
        var id = CausationId.From("test-causation");
        var exactLength = "test-causation".Length;

        // Exact buffer length (kills >= mutated to >)
        Span<char> exactBuffer = stackalloc char[exactLength];
        var successExact = id.TryFormat(exactBuffer, out var charsWrittenExact, default, null);
        successExact.Should().BeTrue();
        charsWrittenExact.Should().Be(exactLength);
        exactBuffer.ToString().Should().Be("test-causation");

        // Larger buffer
        Span<char> largerBuffer = stackalloc char[exactLength + 10];
        var successLarger = id.TryFormat(largerBuffer, out var charsWrittenLarger, default, null);
        successLarger.Should().BeTrue();
        charsWrittenLarger.Should().Be(exactLength);
        largerBuffer[..charsWrittenLarger].ToString().Should().Be("test-causation");

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
        CausationId defaultId = default;
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
    public void CausationId_ComparisonsAndOperators_ShouldWork()
    {
        var ca1 = CausationId.From("A");
        var ca2 = CausationId.From("B");
        var ca1Copy = CausationId.From("a"); // Case-insensitive

        ca1.CompareTo(ca2).Should().BeLessThan(0);
        ca2.CompareTo(ca1).Should().BeGreaterThan(0);
        ca1.CompareTo(ca1Copy).Should().Be(0);

        // Differentiate OrdinalIgnoreCase from Culture comparison (e.g. sharp s 'ß' vs 'SS')
        var idSharpS = CausationId.From("ß");
        var idDoubleS = CausationId.From("SS");
        idSharpS.CompareTo(idDoubleS).Should().NotBe(0);

        // Default comparison
        CausationId def1 = default;
        CausationId def2 = default;
        def1.CompareTo(def2).Should().Be(0);
        def1.CompareTo(ca1).Should().BeLessThan(0);
        ca1.CompareTo(def1).Should().BeGreaterThan(0);

        (ca1 < ca2).Should().BeTrue();
        (ca1 < ca1Copy).Should().BeFalse();
        (ca2 < ca1).Should().BeFalse();

        (ca1 <= ca2).Should().BeTrue();
        (ca1 <= ca1Copy).Should().BeTrue();
        (ca2 <= ca1).Should().BeFalse();

        (ca2 > ca1).Should().BeTrue();
        (ca1 > ca1Copy).Should().BeFalse();
        (ca1 > ca2).Should().BeFalse();

        (ca2 >= ca1).Should().BeTrue();
        (ca1 >= ca1Copy).Should().BeTrue();
        (ca1 >= ca2).Should().BeFalse();

        ((IComparable)ca1).CompareTo(ca2).Should().BeLessThan(0);
        ((IComparable)ca2).CompareTo(ca1).Should().BeGreaterThan(0);
        ((IComparable)ca1).CompareTo(ca1Copy).Should().Be(0);
        ((IComparable)ca1).CompareTo(null).Should().Be(1);

        var actCa = () => ((IComparable)ca1).CompareTo(999);
        actCa.Should().Throw<ArgumentException>()
            .WithMessage("Object must be of type CausationId*")
            .WithParameterName("obj");

        string sCa = ca1;
        sCa.Should().Be("A");

        var explicitCause = (CausationId)"A";
        explicitCause.Should().Be(ca1);
    }
}
