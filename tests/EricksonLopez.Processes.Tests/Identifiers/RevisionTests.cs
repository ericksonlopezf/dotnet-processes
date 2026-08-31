// Copyright © Erickson Lopez. MIT License.
using System;
using System.Globalization;
using AwesomeAssertions;
using EricksonLopez.Processes.Abstractions;
using Xunit;

namespace EricksonLopez.Processes.Tests.Identifiers;

#pragma warning disable CA1305 // Specify IFormatProvider

[Trait("Category", "Unit")]
public class RevisionTests
{
    [Theory]
    [InlineData(-1)]
    [InlineData(-999)]
    public void Revision_Constructor_ShouldThrowOnNegative(long invalid)
    {
        var act = () => new Revision(invalid);
        act.Should().Throw<ArgumentOutOfRangeException>()
            .WithMessage("Revision must be greater than or equal to 0.*")
            .WithParameterName("value");
    }

    [Fact]
    public void Revision_FactoriesAndIncrement_ShouldWork()
    {
        Revision.None.Value.Should().Be(0);
        Revision.Initial.Value.Should().Be(1);

        var r1 = Revision.From(5);
        var r2 = Revision.FromInt64(5);
        r1.ToInt64().Should().Be(5);
        r1.Should().Be(r2);

        var next = r1.Next();
        next.Value.Should().Be(6);
        (next > r1).Should().BeTrue();
    }

    [Fact]
    public void Revision_ParseAndTryParse_StringAndSpan_ShouldWork()
    {
        var customNfi = new NumberFormatInfo { PositiveSign = "@" };

        Revision.Parse("@0", customNfi).Value.Should().Be(0);
        Revision.Parse("0").Value.Should().Be(0);
        Revision.Parse("@100".AsSpan(), customNfi).Value.Should().Be(100);
        Revision.Parse("100".AsSpan()).Value.Should().Be(100);

        Revision.TryParse("@0", customNfi, out var r0Custom).Should().BeTrue();
        r0Custom.Value.Should().Be(0);

        Revision.TryParse("0", null, out var r0).Should().BeTrue();
        r0.Value.Should().Be(0);

        Revision.TryParse("@25", customNfi, out var r25).Should().BeTrue();
        r25.Value.Should().Be(25);

        Revision.TryParse("25", null, out var r25Default).Should().BeTrue();
        r25Default.Value.Should().Be(25);

        Revision.TryParse("@0".AsSpan(), customNfi, out var r0SpanCustom).Should().BeTrue();
        r0SpanCustom.Value.Should().Be(0);

        Revision.TryParse("0".AsSpan(), null, out var r0Span).Should().BeTrue();
        r0Span.Value.Should().Be(0);

        Revision.TryParse("@50".AsSpan(), customNfi, out var r50).Should().BeTrue();
        r50.Value.Should().Be(50);

        Revision.TryParse("50".AsSpan(), null, out var r50Default).Should().BeTrue();
        r50Default.Value.Should().Be(50);

        Revision.TryParse("-1", customNfi, out _).Should().BeFalse();
        Revision.TryParse("abc", customNfi, out _).Should().BeFalse();
        Revision.TryParse(null, customNfi, out _).Should().BeFalse();

        Revision.TryParse("-5".AsSpan(), customNfi, out _).Should().BeFalse();
        Revision.TryParse("invalid".AsSpan(), customNfi, out _).Should().BeFalse();
    }

    [Fact]
    public void Revision_Formatting_ShouldWork()
    {
        var r = Revision.From(1234);
        var customNfi = new NumberFormatInfo { CurrencySymbol = "REV" };

        r.ToString().Should().Be("1234");
        r.ToString("C0", customNfi).Should().Contain("REV");
        r.ToString("D6", null).Should().Be("001234");

        Span<char> buffer = stackalloc char[30];
        r.TryFormat(buffer, out var written, "C0".AsSpan(), customNfi).Should().BeTrue();
        buffer[..written].ToString().Should().Contain("REV");

        r.TryFormat(buffer, out var writtenNull, "D".AsSpan(), null).Should().BeTrue();
        writtenNull.Should().Be(4);

        // Small buffer failure
        Span<char> smallBuffer = stackalloc char[1];
        r.TryFormat(smallBuffer, out var writtenSmall, "D".AsSpan(), null).Should().BeFalse();
        writtenSmall.Should().Be(0);

        Span<char> zeroBuffer = stackalloc char[0];
        r.TryFormat(zeroBuffer, out var writtenZero, default, null).Should().BeFalse();
        writtenZero.Should().Be(0);
    }

    [Fact]
    public void Revision_Parse_Invalid_ShouldThrow()
    {
        var actNull = () => Revision.Parse((string)null!);
        actNull.Should().Throw<ArgumentNullException>();

        var actInvalid = () => Revision.Parse("not-a-number");
        actInvalid.Should().Throw<FormatException>();

        var actNeg = () => Revision.Parse("-1");
        actNeg.Should().Throw<ArgumentOutOfRangeException>();

        var actSpanInvalid = () => Revision.Parse("abc".AsSpan());
        actSpanInvalid.Should().Throw<FormatException>();

        var actSpanNeg = () => Revision.Parse("-5".AsSpan());
        actSpanNeg.Should().Throw<ArgumentOutOfRangeException>();
    }

    [Fact]
    public void Revision_ComparisonsAndOperators_ShouldWork()
    {
        var r1 = Revision.From(10);
        var r2 = Revision.From(20);
        var r1Copy = Revision.From(10);

        (r1 < r2).Should().BeTrue();
        (r1 < r1Copy).Should().BeFalse();
        (r2 < r1).Should().BeFalse();

        (r1 <= r2).Should().BeTrue();
        (r1 <= r1Copy).Should().BeTrue();
        (r2 <= r1).Should().BeFalse();

        (r2 > r1).Should().BeTrue();
        (r1 > r1Copy).Should().BeFalse();
        (r1 > r2).Should().BeFalse();

        (r2 >= r1).Should().BeTrue();
        (r1 >= r1Copy).Should().BeTrue();
        (r1 >= r2).Should().BeFalse();

        r1.CompareTo(r2).Should().BeLessThan(0);
        ((IComparable)r1).CompareTo(r2).Should().BeLessThan(0);
        ((IComparable)r1).CompareTo(null).Should().Be(1);

        var act = () => ((IComparable)r1).CompareTo("not revision");
        act.Should().Throw<ArgumentException>()
            .WithMessage("Object must be of type Revision*")
            .WithParameterName("obj");

        long implicitLong = r1;
        implicitLong.Should().Be(10);

        var explicitRevision = (Revision)10L;
        explicitRevision.Should().Be(r1);
    }
}


