// Copyright © Erickson Lopez. MIT License.
using System;
using System.Globalization;
using AwesomeAssertions;
using EricksonLopez.Processes.Abstractions;
using Xunit;

namespace EricksonLopez.Processes.Tests.Identifiers;

#pragma warning disable CA1305 // Specify IFormatProvider

[Trait("Category", "Unit")]
public class ProcessVersionTests
{
    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    [InlineData(-100)]
    public void ProcessVersion_Constructor_ShouldThrowOnLessThanOne(int invalid)
    {
        var act = () => new ProcessVersion(invalid);
        act.Should().Throw<ArgumentOutOfRangeException>()
            .WithMessage("Process version must be greater than or equal to 1.*")
            .WithParameterName("value");
    }

    [Fact]
    public void ProcessVersion_FactoriesAndIncrement_ShouldWork()
    {
        ProcessVersion.Initial.Value.Should().Be(1);

        var v1 = ProcessVersion.From(1);
        var v2 = ProcessVersion.FromInt32(1);
        v1.ToInt32().Should().Be(1);
        v1.Should().Be(v2);

        var next = v1.Next();
        next.Value.Should().Be(2);
        (next > v1).Should().BeTrue();
    }

    [Fact]
    public void ProcessVersion_ParseAndTryParse_StringAndSpan_ShouldWork()
    {
        var customNfi = new NumberFormatInfo { PositiveSign = "@" };

        ProcessVersion.Parse("@3", customNfi).Value.Should().Be(3);
        ProcessVersion.Parse("3").Value.Should().Be(3);
        ProcessVersion.Parse("@4".AsSpan(), customNfi).Value.Should().Be(4);
        ProcessVersion.Parse("4".AsSpan()).Value.Should().Be(4);

        ProcessVersion.TryParse("@1", customNfi, out var v1Custom).Should().BeTrue();
        v1Custom.Value.Should().Be(1);

        ProcessVersion.TryParse("1", null, out var v1).Should().BeTrue();
        v1.Value.Should().Be(1);

        ProcessVersion.TryParse("@5", customNfi, out var v5).Should().BeTrue();
        v5.Value.Should().Be(5);

        ProcessVersion.TryParse("5", null, out var v5Default).Should().BeTrue();
        v5Default.Value.Should().Be(5);

        ProcessVersion.TryParse("@1".AsSpan(), customNfi, out var v1SpanCustom).Should().BeTrue();
        v1SpanCustom.Value.Should().Be(1);

        ProcessVersion.TryParse("1".AsSpan(), null, out var v1Span).Should().BeTrue();
        v1Span.Value.Should().Be(1);

        ProcessVersion.TryParse("@6".AsSpan(), customNfi, out var v6).Should().BeTrue();
        v6.Value.Should().Be(6);

        ProcessVersion.TryParse("6".AsSpan(), null, out var v6Default).Should().BeTrue();
        v6Default.Value.Should().Be(6);

        ProcessVersion.TryParse("0", customNfi, out _).Should().BeFalse();
        ProcessVersion.TryParse("-5", customNfi, out _).Should().BeFalse();
        ProcessVersion.TryParse("abc", customNfi, out _).Should().BeFalse();
        ProcessVersion.TryParse(null, customNfi, out _).Should().BeFalse();

        ProcessVersion.TryParse("0".AsSpan(), customNfi, out _).Should().BeFalse();
        ProcessVersion.TryParse("-1".AsSpan(), customNfi, out _).Should().BeFalse();
        ProcessVersion.TryParse("invalid".AsSpan(), customNfi, out _).Should().BeFalse();
    }

    [Fact]
    public void ProcessVersion_Formatting_ShouldWork()
    {
        var v = ProcessVersion.From(42);
        var customNfi = new NumberFormatInfo { CurrencySymbol = "VER" };

        v.ToString().Should().Be("42");
        v.ToString("C0", customNfi).Should().Contain("VER");
        v.ToString("D4", null).Should().Be("0042");

        Span<char> buffer = stackalloc char[20];
        v.TryFormat(buffer, out var written, "C0".AsSpan(), customNfi).Should().BeTrue();
        buffer[..written].ToString().Should().Contain("VER");

        v.TryFormat(buffer, out var writtenNull, "D".AsSpan(), null).Should().BeTrue();
        writtenNull.Should().Be(2);

        // Small buffer failure
        Span<char> smallBuffer = stackalloc char[1];
        v.TryFormat(smallBuffer, out var writtenSmall, "D".AsSpan(), null).Should().BeFalse();
        writtenSmall.Should().Be(0);

        Span<char> zeroBuffer = stackalloc char[0];
        v.TryFormat(zeroBuffer, out var writtenZero, default, null).Should().BeFalse();
        writtenZero.Should().Be(0);
    }

    [Fact]
    public void ProcessVersion_Parse_Invalid_ShouldThrow()
    {
        var actNull = () => ProcessVersion.Parse((string)null!);
        actNull.Should().Throw<ArgumentNullException>();

        var actInvalid = () => ProcessVersion.Parse("not-a-number");
        actInvalid.Should().Throw<FormatException>();

        var actZero = () => ProcessVersion.Parse("0");
        actZero.Should().Throw<ArgumentOutOfRangeException>();

        var actNeg = () => ProcessVersion.Parse("-1");
        actNeg.Should().Throw<ArgumentOutOfRangeException>();

        var actSpanInvalid = () => ProcessVersion.Parse("abc".AsSpan());
        actSpanInvalid.Should().Throw<FormatException>();

        var actSpanZero = () => ProcessVersion.Parse("0".AsSpan());
        actSpanZero.Should().Throw<ArgumentOutOfRangeException>();
    }

    [Fact]
    public void ProcessVersion_ComparisonsAndOperators_ShouldWork()
    {
        var v1 = ProcessVersion.From(1);
        var v2 = ProcessVersion.From(2);
        var v1Copy = ProcessVersion.From(1);

        (v1 < v2).Should().BeTrue();
        (v1 < v1Copy).Should().BeFalse();
        (v2 < v1).Should().BeFalse();

        (v1 <= v2).Should().BeTrue();
        (v1 <= v1Copy).Should().BeTrue();
        (v2 <= v1).Should().BeFalse();

        (v2 > v1).Should().BeTrue();
        (v1 > v1Copy).Should().BeFalse();
        (v1 > v2).Should().BeFalse();

        (v2 >= v1).Should().BeTrue();
        (v1 >= v1Copy).Should().BeTrue();
        (v1 >= v2).Should().BeFalse();

        v1.CompareTo(v2).Should().BeLessThan(0);
        ((IComparable)v1).CompareTo(v2).Should().BeLessThan(0);
        ((IComparable)v1).CompareTo(null).Should().Be(1);

        var act = () => ((IComparable)v1).CompareTo("not version");
        act.Should().Throw<ArgumentException>()
            .WithMessage("Object must be of type ProcessVersion*")
            .WithParameterName("obj");

        int implicitInt = v1;
        implicitInt.Should().Be(1);

        var explicitVersion = (ProcessVersion)1;
        explicitVersion.Should().Be(v1);
    }
}


