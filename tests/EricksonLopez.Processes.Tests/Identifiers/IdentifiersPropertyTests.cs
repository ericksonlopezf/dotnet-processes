// Copyright © Erickson Lopez. MIT License.
using System;
using System.Globalization;
using EricksonLopez.Processes.Abstractions;
using FsCheck;
using FsCheck.Xunit;
using Xunit;

namespace EricksonLopez.Processes.Tests.Identifiers;

#pragma warning disable CA1305 // Specify IFormatProvider
#pragma warning disable CA1308 // Normalize strings to uppercase
#pragma warning disable CA1720 // Identifier contains type name

/// <summary>
/// Property-based tests verifying algebraic invariants and round-trip laws for domain Value Objects using FsCheck.
/// </summary>
[Trait("Category", "Property")]
public class IdentifiersPropertyTests
{
    #region Revision Properties

    [Property]
    public bool Revision_Next_AlwaysIncrementsStrictlyMonotonic(PositiveInt rawValue)
    {
        var current = Revision.From(rawValue.Get);
        var next = current.Next();

        return next.Value == current.Value + 1
            && next > current
            && next >= current
            && current < next
            && current <= next
            && next != current;
    }

    [Property]
    public bool Revision_ValuePreservation_AndConversionRoundtrip(NonNegativeInt rawValue)
    {
        var revision = Revision.From(rawValue.Get);
        long implicitLong = revision;
        var explicitRevision = (Revision)implicitLong;

        return revision.Value == rawValue.Get
            && revision.ToInt64() == rawValue.Get
            && implicitLong == rawValue.Get
            && explicitRevision == revision;
    }

    [Property]
    public bool Revision_SpanFormatAndParse_AlwaysRoundtrips(NonNegativeInt rawValue)
    {
        var revision = Revision.From(rawValue.Get);
        Span<char> buffer = stackalloc char[32];

        if (!revision.TryFormat(buffer, out var charsWritten, default, CultureInfo.InvariantCulture))
        {
            return false;
        }

        var parsed = Revision.Parse(buffer[..charsWritten], CultureInfo.InvariantCulture);
        var tryParsedSuccess = Revision.TryParse(buffer[..charsWritten], CultureInfo.InvariantCulture, out var tryParsed);

        return parsed == revision && tryParsedSuccess && tryParsed == revision;
    }

    [Property]
    public bool Revision_ComparisonTotalOrder_IsConsistent(NonNegativeInt a, NonNegativeInt b)
    {
        var revA = Revision.From(a.Get);
        var revB = Revision.From(b.Get);

        if (a.Get < b.Get)
        {
            return revA < revB && revA <= revB && revB > revA && revB >= revA && revA.CompareTo(revB) < 0;
        }
        else if (a.Get > b.Get)
        {
            return revA > revB && revA >= revB && revB < revA && revB <= revA && revA.CompareTo(revB) > 0;
        }
        else
        {
            return revA == revB && revA <= revB && revA >= revB && revA.CompareTo(revB) == 0;
        }
    }

    #endregion

    #region ProcessVersion Properties

    [Property]
    public bool ProcessVersion_Next_AlwaysIncrementsStrictlyMonotonic(PositiveInt rawValue)
    {
        var current = ProcessVersion.From(rawValue.Get);
        var next = current.Next();

        return next.Value == current.Value + 1
            && next > current
            && next >= current
            && current < next
            && current <= next
            && next != current;
    }

    [Property]
    public bool ProcessVersion_SpanFormatAndParse_AlwaysRoundtrips(PositiveInt rawValue)
    {
        var version = ProcessVersion.From(rawValue.Get);
        Span<char> buffer = stackalloc char[32];

        if (!version.TryFormat(buffer, out var charsWritten, default, CultureInfo.InvariantCulture))
        {
            return false;
        }

        var parsed = ProcessVersion.Parse(buffer[..charsWritten], CultureInfo.InvariantCulture);
        var tryParsedSuccess = ProcessVersion.TryParse(buffer[..charsWritten], CultureInfo.InvariantCulture, out var tryParsed);

        return parsed == version && tryParsedSuccess && tryParsed == version;
    }

    #endregion

    #region ProcessId Properties

    [Property]
    public bool ProcessId_GuidPreservation_AndConversionRoundtrip(Guid guid)
    {
        var id = ProcessId.From(guid);
        var fromGuid = ProcessId.FromGuid(guid);

        return id.Value == guid
            && id.ToGuid() == guid
            && id == fromGuid;
    }

    [Property]
    public bool ProcessId_SpanFormatAndParse_AlwaysRoundtrips(Guid guid)
    {
        var id = ProcessId.From(guid);
        Span<char> buffer = stackalloc char[36];

        if (!id.TryFormat(buffer, out var written, default, null))
        {
            return false;
        }

        var parsed = ProcessId.Parse(buffer, null);
        var tryParsedSuccess = ProcessId.TryParse(buffer, null, out var tryParsed);

        return written == 36 && parsed == id && tryParsedSuccess && tryParsed == id;
    }

    [Property]
    public bool ProcessId_StringParseAndTryParse_AlwaysRoundtrips(Guid guid)
    {
        var id = ProcessId.From(guid);
        var str = id.ToString();

        var parsed = ProcessId.Parse(str);
        var tryParsedSuccess = ProcessId.TryParse(str, null, out var tryParsed);

        return parsed == id && tryParsedSuccess && tryParsed == id;
    }

    #endregion

    #region String Identifiers Properties (ProcessType, CorrelationId, CausationId, MessageId)

    [Property]
    public bool ProcessType_Comparison_IsCaseInsensitive_AndSpanRoundtrips(NonNull<string> input)
    {
        var trimmed = input.Get.Trim();
        if (string.IsNullOrWhiteSpace(trimmed))
        {
            return true;
        }

        var typeLower = ProcessType.From(trimmed.ToLowerInvariant());
        var typeUpper = ProcessType.From(trimmed.ToUpperInvariant());

        Span<char> buffer = stackalloc char[trimmed.Length + 16];
        if (!typeLower.TryFormat(buffer, out var written, default, null))
        {
            return false;
        }

        var parsed = ProcessType.Parse(buffer[..written], null);

        return typeLower.CompareTo(typeUpper) == 0
            && parsed == typeLower;
    }

    [Property]
    public bool CorrelationId_Comparison_IsCaseInsensitive_AndSpanRoundtrips(NonNull<string> input)
    {
        var trimmed = input.Get.Trim();
        if (string.IsNullOrWhiteSpace(trimmed))
        {
            return true;
        }

        var corrLower = CorrelationId.From(trimmed.ToLowerInvariant());
        var corrUpper = CorrelationId.From(trimmed.ToUpperInvariant());

        Span<char> buffer = stackalloc char[trimmed.Length + 16];
        if (!corrLower.TryFormat(buffer, out var written, default, null))
        {
            return false;
        }

        var parsed = CorrelationId.Parse(buffer[..written], null);

        return corrLower.CompareTo(corrUpper) == 0
            && parsed == corrLower;
    }

    [Property]
    public bool CausationId_Comparison_IsCaseInsensitive_AndSpanRoundtrips(NonNull<string> input)
    {
        var trimmed = input.Get.Trim();
        if (string.IsNullOrWhiteSpace(trimmed))
        {
            return true;
        }

        var causeLower = CausationId.From(trimmed.ToLowerInvariant());
        var causeUpper = CausationId.From(trimmed.ToUpperInvariant());

        Span<char> buffer = stackalloc char[trimmed.Length + 16];
        if (!causeLower.TryFormat(buffer, out var written, default, null))
        {
            return false;
        }

        var parsed = CausationId.Parse(buffer[..written], null);

        return causeLower.CompareTo(causeUpper) == 0
            && parsed == causeLower;
    }

    [Property]
    public bool MessageId_Comparison_IsCaseInsensitive_AndSpanRoundtrips(NonNull<string> input)
    {
        var trimmed = input.Get.Trim();
        if (string.IsNullOrWhiteSpace(trimmed))
        {
            return true;
        }

        var msgLower = MessageId.From(trimmed.ToLowerInvariant());
        var msgUpper = MessageId.From(trimmed.ToUpperInvariant());

        Span<char> buffer = stackalloc char[trimmed.Length + 16];
        if (!msgLower.TryFormat(buffer, out var written, default, null))
        {
            return false;
        }

        var parsed = MessageId.Parse(buffer[..written], null);

        return msgLower.CompareTo(msgUpper) == 0
            && parsed == msgLower;
    }

    #endregion
}
