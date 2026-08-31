// Copyright © Erickson Lopez. MIT License.
using System;
using System.Globalization;
using AwesomeAssertions;
using EricksonLopez.Processes.Abstractions;
using Xunit;

namespace EricksonLopez.Processes.Tests.Identifiers;

#pragma warning disable CA1305 // Specify IFormatProvider

[Trait("Category", "Unit")]
public class MessageIdTests
{
    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void MessageId_Constructor_ShouldThrowOnInvalidInput(string? invalid)
    {
        var act = () => new MessageId(invalid!);
        act.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void MessageId_FactoriesAndProperties_ShouldWork()
    {
        var msgNew = MessageId.NewId();
        msgNew.Value.Should().NotBeNullOrWhiteSpace();
        msgNew.ToString().Should().Be(msgNew.Value);
        msgNew.ToString(null, null).Should().Be(msgNew.Value);
        msgNew.ToString("G", null).Should().Be(msgNew.Value);
        msgNew.ToString("G", CultureInfo.InvariantCulture).Should().Be(msgNew.Value);

        var guid = Guid.NewGuid();
        MessageId.FromGuid(guid).Value.Should().Be(guid.ToString());
        MessageId.From(guid.ToString()).Value.Should().Be(guid.ToString());
        MessageId.FromString("m-1").Value.Should().Be("m-1");
        MessageId.From("m-1").Value.Should().Be("m-1");

        MessageId defMsg = default;
        defMsg.ToString().Should().BeEmpty();
        defMsg.ToString(null, null).Should().BeEmpty();
        defMsg.ToString("G", null).Should().BeEmpty();
        defMsg.ToString("G", CultureInfo.InvariantCulture).Should().BeEmpty();
        defMsg.Value.Should().BeNull();
    }

    [Fact]
    public void MessageId_ParseAndTryParse_ShouldWork()
    {
        MessageId.Parse("msg", CultureInfo.InvariantCulture).Value.Should().Be("msg");
        MessageId.Parse("msg").Value.Should().Be("msg");

        MessageId.TryParse("m", CultureInfo.InvariantCulture, out var m).Should().BeTrue();
        m.Value.Should().Be("m");

        MessageId.TryParse("m", null, out var mNullProv).Should().BeTrue();
        mNullProv.Value.Should().Be("m");

        MessageId.TryParse("m".AsSpan(), CultureInfo.InvariantCulture, out var mSpan).Should().BeTrue();
        mSpan.Value.Should().Be("m");

        MessageId.TryParse("m".AsSpan(), null, out var mSpanNullProv).Should().BeTrue();
        mSpanNullProv.Value.Should().Be("m");

        MessageId.Parse("m".AsSpan(), CultureInfo.InvariantCulture).Value.Should().Be("m");
        MessageId.Parse("m".AsSpan()).Value.Should().Be("m");

        MessageId.TryParse((string?)null, CultureInfo.InvariantCulture, out var nullRes).Should().BeFalse();
        nullRes.Should().Be(default);

        MessageId.TryParse("", CultureInfo.InvariantCulture, out var emptyRes).Should().BeFalse();
        emptyRes.Should().Be(default);

        MessageId.TryParse("  ", CultureInfo.InvariantCulture, out var wsRes).Should().BeFalse();
        wsRes.Should().Be(default);

        MessageId.TryParse("  ".AsSpan(), CultureInfo.InvariantCulture, out var wsSpanRes).Should().BeFalse();
        wsSpanRes.Should().Be(default);

        MessageId.TryParse(ReadOnlySpan<char>.Empty, CultureInfo.InvariantCulture, out var emptySpanRes).Should().BeFalse();
        emptySpanRes.Should().Be(default);

        var actMsgEmptySpan = () => MessageId.Parse(ReadOnlySpan<char>.Empty);
        actMsgEmptySpan.Should().Throw<ArgumentException>()
            .WithParameterName("s")
            .WithMessage("Span cannot be empty or whitespace.*");

        var actMsgWsSpan = () => MessageId.Parse("  ".AsSpan(), CultureInfo.InvariantCulture);
        actMsgWsSpan.Should().Throw<ArgumentException>()
            .WithParameterName("s")
            .WithMessage("Span cannot be empty or whitespace.*");

        var actNullStr = () => MessageId.Parse((string)null!);
        actNullStr.Should().Throw<ArgumentException>();

        var actEmptyStr = () => MessageId.Parse("");
        actEmptyStr.Should().Throw<ArgumentException>();

        var actWsStr = () => MessageId.Parse("   ");
        actWsStr.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void MessageId_TryFormat_ExactBufferAndSmallerBuffer_ShouldBehaveCorrectly()
    {
        var id = MessageId.From("test-message");
        var exactLength = "test-message".Length;

        // Exact buffer length (kills >= mutated to >)
        Span<char> exactBuffer = stackalloc char[exactLength];
        var successExact = id.TryFormat(exactBuffer, out var charsWrittenExact, default, null);
        successExact.Should().BeTrue();
        charsWrittenExact.Should().Be(exactLength);
        exactBuffer.ToString().Should().Be("test-message");

        // Larger buffer
        Span<char> largerBuffer = stackalloc char[exactLength + 10];
        var successLarger = id.TryFormat(largerBuffer, out var charsWrittenLarger, default, null);
        successLarger.Should().BeTrue();
        charsWrittenLarger.Should().Be(exactLength);
        largerBuffer[..charsWrittenLarger].ToString().Should().Be("test-message");

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
        MessageId defaultId = default;
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
    public void MessageId_ComparisonsAndOperators_ShouldWork()
    {
        var m1 = MessageId.From("A");
        var m2 = MessageId.From("B");
        var m1Copy = MessageId.From("a"); // Case-insensitive

        m1.CompareTo(m2).Should().BeLessThan(0);
        m2.CompareTo(m1).Should().BeGreaterThan(0);
        m1.CompareTo(m1Copy).Should().Be(0);

        // Differentiate OrdinalIgnoreCase from Culture comparison (e.g. sharp s 'ß' vs 'SS')
        var idSharpS = MessageId.From("ß");
        var idDoubleS = MessageId.From("SS");
        idSharpS.CompareTo(idDoubleS).Should().NotBe(0);

        // Default comparison
        MessageId def1 = default;
        MessageId def2 = default;
        def1.CompareTo(def2).Should().Be(0);
        def1.CompareTo(m1).Should().BeLessThan(0);
        m1.CompareTo(def1).Should().BeGreaterThan(0);

        (m1 < m2).Should().BeTrue();
        (m1 < m1Copy).Should().BeFalse();
        (m2 < m1).Should().BeFalse();

        (m1 <= m2).Should().BeTrue();
        (m1 <= m1Copy).Should().BeTrue();
        (m2 <= m1).Should().BeFalse();

        (m2 > m1).Should().BeTrue();
        (m1 > m1Copy).Should().BeFalse();
        (m1 > m2).Should().BeFalse();

        (m2 >= m1).Should().BeTrue();
        (m1 >= m1Copy).Should().BeTrue();
        (m1 >= m2).Should().BeFalse();

        ((IComparable)m1).CompareTo(m2).Should().BeLessThan(0);
        ((IComparable)m2).CompareTo(m1).Should().BeGreaterThan(0);
        ((IComparable)m1).CompareTo(m1Copy).Should().Be(0);
        ((IComparable)m1).CompareTo(null).Should().Be(1);

        var actM = () => ((IComparable)m1).CompareTo(999);
        actM.Should().Throw<ArgumentException>()
            .WithMessage("Object must be of type MessageId*")
            .WithParameterName("obj");

        string sM = m1;
        sM.Should().Be("A");

        var explicitMsg = (MessageId)"A";
        explicitMsg.Should().Be(m1);
    }
}
