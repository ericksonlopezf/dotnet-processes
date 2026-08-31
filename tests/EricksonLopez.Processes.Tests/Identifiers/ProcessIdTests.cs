// Copyright © Erickson Lopez. MIT License.
using System;
using System.Globalization;
using AwesomeAssertions;
using EricksonLopez.Processes.Abstractions;
using Xunit;

namespace EricksonLopez.Processes.Tests.Identifiers;

#pragma warning disable CA1305 // Specify IFormatProvider

[Trait("Category", "Unit")]
public class ProcessIdTests
{
    [Fact]
    public void ProcessId_NewId_ShouldCreateUniqueGuidV7()
    {
        var id1 = ProcessId.NewId();
        var id2 = ProcessId.NewId();

        id1.Should().NotBe(ProcessId.Empty);
        id2.Should().NotBe(ProcessId.Empty);
        id1.Should().NotBe(id2);
        id1.Value.Should().NotBe(Guid.Empty);
        id1.ToGuid().Should().Be(id1.Value);
    }

    [Fact]
    public void ProcessId_Factories_ShouldCreateExpectedValues()
    {
        var guid = Guid.NewGuid();
        var id1 = ProcessId.From(guid);
        var id2 = ProcessId.FromGuid(guid);

        id1.Value.Should().Be(guid);
        id2.Value.Should().Be(guid);
        id1.Should().Be(id2);
    }

    [Fact]
    public void ProcessId_Empty_ShouldHaveEmptyGuid()
    {
        ProcessId.Empty.Value.Should().Be(Guid.Empty);
    }

    [Fact]
    public void ProcessId_ParseAndTryParse_String_ShouldBehaveCorrectly()
    {
        var guid = Guid.NewGuid();
        var guidStr = guid.ToString();
        var customProvider = new CultureInfo("en-US");

        var parsed = ProcessId.Parse(guidStr, customProvider);
        parsed.Value.Should().Be(guid);

        var parsedDefault = ProcessId.Parse(guidStr);
        parsedDefault.Value.Should().Be(guid);

        ProcessId.TryParse(guidStr, customProvider, out var tryParsed).Should().BeTrue();
        tryParsed.Value.Should().Be(guid);

        ProcessId.TryParse(guidStr, null, out var tryParsedNullProvider).Should().BeTrue();
        tryParsedNullProvider.Value.Should().Be(guid);

        ProcessId.TryParse(null, CultureInfo.InvariantCulture, out var nullParsed).Should().BeFalse();
        nullParsed.Should().Be(ProcessId.Empty);

        ProcessId.TryParse("not-a-guid", CultureInfo.InvariantCulture, out var invalidParsed).Should().BeFalse();
        invalidParsed.Should().Be(ProcessId.Empty);

        var actNull = () => ProcessId.Parse(null!, CultureInfo.InvariantCulture);
        actNull.Should().Throw<ArgumentException>();

        var actEmpty = () => ProcessId.Parse("   ", CultureInfo.InvariantCulture);
        actEmpty.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void ProcessId_ParseAndTryParse_Span_ShouldBehaveCorrectly()
    {
        var guid = Guid.NewGuid();
        var guidStr = guid.ToString();
        var customProvider = new CultureInfo("en-US");

        var parsed = ProcessId.Parse(guidStr.AsSpan(), customProvider);
        parsed.Value.Should().Be(guid);

        var parsedDefault = ProcessId.Parse(guidStr.AsSpan());
        parsedDefault.Value.Should().Be(guid);

        ProcessId.TryParse(guidStr.AsSpan(), customProvider, out var tryParsed).Should().BeTrue();
        tryParsed.Value.Should().Be(guid);

        ProcessId.TryParse(guidStr.AsSpan(), null, out var tryParsedNullProv).Should().BeTrue();
        tryParsedNullProv.Value.Should().Be(guid);

        ProcessId.TryParse("invalid".AsSpan(), CultureInfo.InvariantCulture, out var invalidParsed).Should().BeFalse();
        invalidParsed.Should().Be(ProcessId.Empty);
    }

    [Fact]
    public void ProcessId_Formatting_ShouldWorkCorrectly()
    {
        var guid = Guid.NewGuid();
        var id = ProcessId.From(guid);

        id.ToString().Should().Be(guid.ToString());
        id.ToString("N", null).Should().Be(guid.ToString("N"));
        id.ToString("B", CultureInfo.InvariantCulture).Should().Be(guid.ToString("B"));
        id.ToString("P", CultureInfo.InvariantCulture).Should().Be(guid.ToString("P"));
        id.ToString("D", null).Should().Be(guid.ToString("D"));

        Span<char> buffer = stackalloc char[36];
        id.TryFormat(buffer, out var charsWritten, "D".AsSpan(), CultureInfo.InvariantCulture).Should().BeTrue();
        charsWritten.Should().Be(36);
        buffer.ToString().Should().Be(guid.ToString("D"));

        Span<char> smallBuffer = stackalloc char[5];
        id.TryFormat(smallBuffer, out _, "D".AsSpan(), null).Should().BeFalse();
    }

    [Fact]
    public void ProcessId_ComparisonsAndOperators_ShouldBehaveDeterministically()
    {
        var guid1 = new Guid("00000000-0000-0000-0000-000000000001");
        var guid2 = new Guid("00000000-0000-0000-0000-000000000002");

        var id1 = new ProcessId(guid1);
        var id2 = new ProcessId(guid2);
        var id1Copy = new ProcessId(guid1);

        (id1 < id2).Should().BeTrue();
        (id1 < id1Copy).Should().BeFalse();
        (id2 < id1).Should().BeFalse();

        (id1 <= id2).Should().BeTrue();
        (id1 <= id1Copy).Should().BeTrue();
        (id2 <= id1).Should().BeFalse();

        (id2 > id1).Should().BeTrue();
        (id1 > id1Copy).Should().BeFalse();
        (id1 > id2).Should().BeFalse();

        (id2 >= id1).Should().BeTrue();
        (id1 >= id1Copy).Should().BeTrue();
        (id1 >= id2).Should().BeFalse();

        (id1 == id1Copy).Should().BeTrue();
        (id1 != id2).Should().BeTrue();

        id1.CompareTo(id2).Should().BeLessThan(0);
        id2.CompareTo(id1).Should().BeGreaterThan(0);
        id1.CompareTo(id1Copy).Should().Be(0);

        ((IComparable)id1).CompareTo(id2).Should().BeLessThan(0);
        ((IComparable)id1).CompareTo(null).Should().Be(1);

        var actCompareInvalid = () => ((IComparable)id1).CompareTo("not a process id");
        actCompareInvalid.Should().Throw<ArgumentException>()
            .WithMessage("Object must be of type ProcessId*")
            .WithParameterName("obj");

        Guid implicitGuid = id1;
        implicitGuid.Should().Be(guid1);

        var explicitId = (ProcessId)guid1;
        explicitId.Should().Be(id1);
    }
}


