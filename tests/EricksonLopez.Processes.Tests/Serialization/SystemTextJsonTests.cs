// Copyright © Erickson Lopez. MIT License.
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Text.Json.Serialization.Metadata;
using System.Threading;
using System.Threading.Tasks;
using AwesomeAssertions;
using EricksonLopez.Processes.Abstractions;
using EricksonLopez.Processes.SystemTextJson;
using Xunit;

#pragma warning disable IL2026, IL3050
namespace EricksonLopez.Processes.Tests.Serialization;

public sealed record SampleProcessState(string OrderId, decimal Amount, bool IsPaid) : IProcessState;

[JsonDerivedType(typeof(EmailNotificationStep), "email")]
[JsonDerivedType(typeof(SmsNotificationStep), "sms")]
public abstract record NotificationStep;
public sealed record EmailNotificationStep(string EmailAddress, string Subject) : NotificationStep;
public sealed record SmsNotificationStep(string PhoneNumber) : NotificationStep;

public sealed record PolymorphicProcessState(
    string FlowName,
    System.Collections.Generic.List<NotificationStep> Steps
) : IProcessState;

[JsonSerializable(typeof(SampleProcessState))]
[JsonSerializable(typeof(PolymorphicProcessState))]
[JsonSerializable(typeof(NotificationStep))]
[JsonSerializable(typeof(EmailNotificationStep))]
[JsonSerializable(typeof(SmsNotificationStep))]
[JsonSerializable(typeof(ProcessId))]
[JsonSerializable(typeof(ProcessType))]
[JsonSerializable(typeof(ProcessVersion))]
[JsonSerializable(typeof(Revision))]
[JsonSerializable(typeof(CorrelationId))]
[JsonSerializable(typeof(CausationId))]
[JsonSerializable(typeof(MessageId))]
internal sealed partial class TestJsonContext : JsonSerializerContext
{
}

[Trait("Category", "Unit")]
public class SystemTextJsonTests
{
    [Fact]
    public void ProcessJsonSerializerOptions_Configure_ShouldThrowOnNullOptions()
    {
        var act = () => ProcessJsonSerializerOptions.Configure(null!);
        act.Should().Throw<ArgumentNullException>().WithParameterName("options");
    }

    [Fact]
    public void PolymorphicState_ShouldSerializeAndDeserialize_WithoutReflection()
    {
        var options = ProcessJsonSerializerOptions.Create(TestJsonContext.Default);
        var typeInfo = (JsonTypeInfo<PolymorphicProcessState>)options.GetTypeInfo(typeof(PolymorphicProcessState));

        var state = new PolymorphicProcessState("onboarding", new List<NotificationStep>
        {
            new EmailNotificationStep("user@example.com", "Welcome!"),
            new SmsNotificationStep("+15550199")
        });

        var json = JsonSerializer.Serialize(state, typeInfo);

        json.Should().Contain("\"$type\":\"email\"");
        json.Should().Contain("\"$type\":\"sms\"");

        var deserialized = JsonSerializer.Deserialize(json, typeInfo);

        deserialized.Should().NotBeNull();
        deserialized!.FlowName.Should().Be("onboarding");
        deserialized.Steps.Should().HaveCount(2);

        var emailStep = deserialized.Steps[0].Should().BeOfType<EmailNotificationStep>().Subject;
        emailStep.EmailAddress.Should().Be("user@example.com");
        emailStep.Subject.Should().Be("Welcome!");

        var smsStep = deserialized.Steps[1].Should().BeOfType<SmsNotificationStep>().Subject;
        smsStep.PhoneNumber.Should().Be("+15550199");
    }

    [Fact]
    public void ProcessJsonSerializerOptions_Create_WithAndWithoutResolver_ShouldConfigureCorrectly()
    {
        var optionsWithout = ProcessJsonSerializerOptions.Create();
        optionsWithout.Should().NotBeNull();
        optionsWithout.Converters.Should().HaveCount(7);
        optionsWithout.PropertyNamingPolicy.Should().Be(JsonNamingPolicy.CamelCase);
        optionsWithout.PropertyNameCaseInsensitive.Should().BeTrue();
        optionsWithout.WriteIndented.Should().BeFalse();
        optionsWithout.TypeInfoResolver.Should().BeNull();

        var optionsWith = ProcessJsonSerializerOptions.Create(TestJsonContext.Default);
        optionsWith.Should().NotBeNull();
        optionsWith.TypeInfoResolver.Should().BeSameAs(TestJsonContext.Default);
        optionsWith.Converters.Should().HaveCount(7);
    }

    [Fact]
    public void Converters_DirectReadAndWrite_ShouldExecuteExpectedBranches()
    {
        var options = new JsonSerializerOptions();

        // ProcessIdJsonConverter
        var idConverter = new ProcessIdJsonConverter();
        var idGuid = Guid.NewGuid();
        var idJsonBytes = Encoding.UTF8.GetBytes($"\"{idGuid}\"");
        var idReader = new Utf8JsonReader(idJsonBytes);
        idReader.Read();
        var readId = idConverter.Read(ref idReader, typeof(ProcessId), options);
        readId.Value.Should().Be(idGuid.ToString());

        var invalidIdJson = Encoding.UTF8.GetBytes("true");
        var invalidIdReader = new Utf8JsonReader(invalidIdJson);
        invalidIdReader.Read();
        var idThrown = false;
        try
        {
            idConverter.Read(ref invalidIdReader, typeof(ProcessId), options);
        }
        catch (JsonException ex)
        {
            idThrown = true;
            ex.Message.Should().Be("Expected string representation of a GUID for ProcessId.");
        }
        idThrown.Should().BeTrue();

        // ProcessTypeJsonConverter
        var typeConverter = new ProcessTypeJsonConverter();
        var typeJsonBytes = Encoding.UTF8.GetBytes("\"order.created\"");
        var typeReader = new Utf8JsonReader(typeJsonBytes);
        typeReader.Read();
        var readType = typeConverter.Read(ref typeReader, typeof(ProcessType), options);
        readType.Value.Should().Be("order.created");

        var invalidTypeJson = Encoding.UTF8.GetBytes("false");
        var invalidTypeReader = new Utf8JsonReader(invalidTypeJson);
        invalidTypeReader.Read();
        var typeThrown = false;
        try
        {
            typeConverter.Read(ref invalidTypeReader, typeof(ProcessType), options);
        }
        catch (JsonException ex)
        {
            typeThrown = true;
            ex.Message.Should().Be("Expected string value for ProcessType.");
        }
        typeThrown.Should().BeTrue();

        // ProcessVersionJsonConverter
        var versionConverter = new ProcessVersionJsonConverter();
        var versionJsonBytes = Encoding.UTF8.GetBytes("10");
        var versionReader = new Utf8JsonReader(versionJsonBytes);
        versionReader.Read();
        var readVersion = versionConverter.Read(ref versionReader, typeof(ProcessVersion), options);
        readVersion.Value.Should().Be(10);

        var invalidVersionZero = Encoding.UTF8.GetBytes("0");
        var invalidVersionZeroReader = new Utf8JsonReader(invalidVersionZero);
        invalidVersionZeroReader.Read();
        var verZeroThrown = false;
        try
        {
            versionConverter.Read(ref invalidVersionZeroReader, typeof(ProcessVersion), options);
        }
        catch (JsonException ex)
        {
            verZeroThrown = true;
            ex.Message.Should().Be("Expected positive integer value for ProcessVersion.");
        }
        verZeroThrown.Should().BeTrue();

        var invalidVersionNeg = Encoding.UTF8.GetBytes("-5");
        var invalidVersionNegReader = new Utf8JsonReader(invalidVersionNeg);
        invalidVersionNegReader.Read();
        var verNegThrown = false;
        try
        {
            versionConverter.Read(ref invalidVersionNegReader, typeof(ProcessVersion), options);
        }
        catch (JsonException ex)
        {
            verNegThrown = true;
            ex.Message.Should().Be("Expected positive integer value for ProcessVersion.");
        }
        verNegThrown.Should().BeTrue();

        var invalidVersionNonNum = Encoding.UTF8.GetBytes("true");
        var invalidVersionNonNumReader = new Utf8JsonReader(invalidVersionNonNum);
        invalidVersionNonNumReader.Read();
        var verNonNumThrown = false;
        try
        {
            versionConverter.Read(ref invalidVersionNonNumReader, typeof(ProcessVersion), options);
        }
        catch (JsonException ex)
        {
            verNonNumThrown = true;
            ex.Message.Should().Be("Expected positive integer value for ProcessVersion.");
        }
        verNonNumThrown.Should().BeTrue();

        // RevisionJsonConverter
        var revConverter = new RevisionJsonConverter();
        var revJsonBytes = Encoding.UTF8.GetBytes("0");
        var revReader = new Utf8JsonReader(revJsonBytes);
        revReader.Read();
        var readRev = revConverter.Read(ref revReader, typeof(Revision), options);
        readRev.Value.Should().Be(0);

        var invalidRevNeg = Encoding.UTF8.GetBytes("-1");
        var invalidRevNegReader = new Utf8JsonReader(invalidRevNeg);
        invalidRevNegReader.Read();
        var revNegThrown = false;
        try
        {
            revConverter.Read(ref invalidRevNegReader, typeof(Revision), options);
        }
        catch (JsonException ex)
        {
            revNegThrown = true;
            ex.Message.Should().Be("Expected non-negative integer value for Revision.");
        }
        revNegThrown.Should().BeTrue();

        var invalidRevNonNum = Encoding.UTF8.GetBytes("null");
        var invalidRevNonNumReader = new Utf8JsonReader(invalidRevNonNum);
        invalidRevNonNumReader.Read();
        var revNonNumThrown = false;
        try
        {
            revConverter.Read(ref invalidRevNonNumReader, typeof(Revision), options);
        }
        catch (JsonException ex)
        {
            revNonNumThrown = true;
            ex.Message.Should().Be("Expected non-negative integer value for Revision.");
        }
        revNonNumThrown.Should().BeTrue();

        // CorrelationIdJsonConverter
        var corrConverter = new CorrelationIdJsonConverter();
        var corrJsonBytes = Encoding.UTF8.GetBytes("\"corr-abc\"");
        var corrReader = new Utf8JsonReader(corrJsonBytes);
        corrReader.Read();
        var readCorr = corrConverter.Read(ref corrReader, typeof(CorrelationId), options);
        readCorr.Value.Should().Be("corr-abc");

        var invalidCorrJson = Encoding.UTF8.GetBytes("123");
        var invalidCorrReader = new Utf8JsonReader(invalidCorrJson);
        invalidCorrReader.Read();
        var corrThrown = false;
        try
        {
            corrConverter.Read(ref invalidCorrReader, typeof(CorrelationId), options);
        }
        catch (JsonException ex)
        {
            corrThrown = true;
            ex.Message.Should().Be("Expected string value for CorrelationId.");
        }
        corrThrown.Should().BeTrue();

        // CausationIdJsonConverter
        var causeConverter = new CausationIdJsonConverter();
        var causeJsonBytes = Encoding.UTF8.GetBytes("\"cause-abc\"");
        var causeReader = new Utf8JsonReader(causeJsonBytes);
        causeReader.Read();
        var readCause = causeConverter.Read(ref causeReader, typeof(CausationId), options);
        readCause.Value.Should().Be("cause-abc");

        var invalidCauseJson = Encoding.UTF8.GetBytes("123");
        var invalidCauseReader = new Utf8JsonReader(invalidCauseJson);
        invalidCauseReader.Read();
        var causeThrown = false;
        try
        {
            causeConverter.Read(ref invalidCauseReader, typeof(CausationId), options);
        }
        catch (JsonException ex)
        {
            causeThrown = true;
            ex.Message.Should().Be("Expected string value for CausationId.");
        }
        causeThrown.Should().BeTrue();

        // MessageIdJsonConverter
        var msgConverter = new MessageIdJsonConverter();
        var msgJsonBytes = Encoding.UTF8.GetBytes("\"msg-abc\"");
        var msgReader = new Utf8JsonReader(msgJsonBytes);
        msgReader.Read();
        var readMsg = msgConverter.Read(ref msgReader, typeof(MessageId), options);
        readMsg.Value.Should().Be("msg-abc");

        var invalidMsgJson = Encoding.UTF8.GetBytes("123");
        var invalidMsgReader = new Utf8JsonReader(invalidMsgJson);
        invalidMsgReader.Read();
        var msgThrown = false;
        try
        {
            msgConverter.Read(ref invalidMsgReader, typeof(MessageId), options);
        }
        catch (JsonException ex)
        {
            msgThrown = true;
            ex.Message.Should().Be("Expected string value for MessageId.");
        }
        msgThrown.Should().BeTrue();
    }

    [Fact]
    public void ProcessIdConverter_ShouldSerializeAndDeserializeCorrectly()
    {
        var options = ProcessJsonSerializerOptions.Create(TestJsonContext.Default);
        var typeInfo = (JsonTypeInfo<ProcessId>)options.GetTypeInfo(typeof(ProcessId));
        var id = ProcessId.NewId();

        var json = JsonSerializer.Serialize(id, typeInfo);
        json.Should().Be($"\"{id.Value}\"");

        var deserialized = JsonSerializer.Deserialize(json, typeInfo);
        deserialized.Should().Be(id);

        var actInvalid = () => JsonSerializer.Deserialize("12345", typeInfo);
        actInvalid.Should().Throw<JsonException>().WithMessage("Expected string representation of a GUID for ProcessId.");

        var actInvalidGuid = () => JsonSerializer.Deserialize("\"not-a-guid\"", typeInfo);
        actInvalidGuid.Should().Throw<JsonException>().WithMessage("Expected string representation of a GUID for ProcessId.");

        var converter = new ProcessIdJsonConverter();
        var actNullWriter = () => converter.Write(null!, id, options);
        actNullWriter.Should().Throw<ArgumentNullException>().WithParameterName("writer");
    }

    [Fact]
    public void ProcessTypeConverter_ShouldSerializeAndDeserializeCorrectly()
    {
        var options = ProcessJsonSerializerOptions.Create(TestJsonContext.Default);
        var typeInfo = (JsonTypeInfo<ProcessType>)options.GetTypeInfo(typeof(ProcessType));
        var processType = ProcessType.From("order.fulfillment");

        var json = JsonSerializer.Serialize(processType, typeInfo);
        json.Should().Be("\"order.fulfillment\"");

        var deserialized = JsonSerializer.Deserialize(json, typeInfo);
        deserialized.Should().Be(processType);

        var actInvalid = () => JsonSerializer.Deserialize("123", typeInfo);
        actInvalid.Should().Throw<JsonException>().WithMessage("Expected string value for ProcessType.");

        var converter = new ProcessTypeJsonConverter();
        var actNullWriter = () => converter.Write(null!, processType, options);
        actNullWriter.Should().Throw<ArgumentNullException>().WithParameterName("writer");
    }

    [Fact]
    public void ProcessVersionConverter_ShouldSerializeAndDeserializeCorrectly()
    {
        var options = ProcessJsonSerializerOptions.Create(TestJsonContext.Default);
        var typeInfo = (JsonTypeInfo<ProcessVersion>)options.GetTypeInfo(typeof(ProcessVersion));
        var version = ProcessVersion.From(2);

        var json = JsonSerializer.Serialize(version, typeInfo);
        json.Should().Be("2");

        var deserialized = JsonSerializer.Deserialize(json, typeInfo);
        deserialized.Should().Be(version);

        // Boundary test: Deserializing 1 (Initial version boundary)
        var deserializedOne = JsonSerializer.Deserialize("1", typeInfo);
        deserializedOne.Should().Be(ProcessVersion.Initial);

        var actInvalidString = () => JsonSerializer.Deserialize("\"v2\"", typeInfo);
        actInvalidString.Should().Throw<JsonException>().WithMessage("Expected positive integer value for ProcessVersion.");

        var actInvalidNegative = () => JsonSerializer.Deserialize("-1", typeInfo);
        actInvalidNegative.Should().Throw<JsonException>().WithMessage("Expected positive integer value for ProcessVersion.");

        var actInvalidZero = () => JsonSerializer.Deserialize("0", typeInfo);
        actInvalidZero.Should().Throw<JsonException>().WithMessage("Expected positive integer value for ProcessVersion.");

        var converter = new ProcessVersionJsonConverter();
        var actNullWriter = () => converter.Write(null!, version, options);
        actNullWriter.Should().Throw<ArgumentNullException>().WithParameterName("writer");
    }

    [Fact]
    public void RevisionConverter_ShouldSerializeAndDeserializeCorrectly()
    {
        var options = ProcessJsonSerializerOptions.Create(TestJsonContext.Default);
        var typeInfo = (JsonTypeInfo<Revision>)options.GetTypeInfo(typeof(Revision));
        var revision = Revision.From(5);

        var json = JsonSerializer.Serialize(revision, typeInfo);
        json.Should().Be("5");

        var deserialized = JsonSerializer.Deserialize(json, typeInfo);
        deserialized.Should().Be(revision);

        // Boundary test: Deserializing 0 (Revision.None boundary)
        var deserializedZero = JsonSerializer.Deserialize("0", typeInfo);
        deserializedZero.Should().Be(Revision.None);

        var actInvalidString = () => JsonSerializer.Deserialize("\"rev-5\"", typeInfo);
        actInvalidString.Should().Throw<JsonException>().WithMessage("Expected non-negative integer value for Revision.");

        var actInvalidNegative = () => JsonSerializer.Deserialize("-1", typeInfo);
        actInvalidNegative.Should().Throw<JsonException>().WithMessage("Expected non-negative integer value for Revision.");

        var converter = new RevisionJsonConverter();
        var actNullWriter = () => converter.Write(null!, revision, options);
        actNullWriter.Should().Throw<ArgumentNullException>().WithParameterName("writer");
    }

    [Fact]
    public void CorrelationIdConverter_ShouldSerializeAndDeserializeCorrectly()
    {
        var options = ProcessJsonSerializerOptions.Create(TestJsonContext.Default);
        var typeInfo = (JsonTypeInfo<CorrelationId>)options.GetTypeInfo(typeof(CorrelationId));
        var correlationId = CorrelationId.From("corr-12345");

        var json = JsonSerializer.Serialize(correlationId, typeInfo);
        json.Should().Be("\"corr-12345\"");

        var deserialized = JsonSerializer.Deserialize(json, typeInfo);
        deserialized.Should().Be(correlationId);

        var actInvalid = () => JsonSerializer.Deserialize("999", typeInfo);
        actInvalid.Should().Throw<JsonException>().WithMessage("Expected string value for CorrelationId.");

        var converter = new CorrelationIdJsonConverter();
        var actNullWriter = () => converter.Write(null!, correlationId, options);
        actNullWriter.Should().Throw<ArgumentNullException>().WithParameterName("writer");
    }

    [Fact]
    public void CausationIdConverter_ShouldSerializeAndDeserializeCorrectly()
    {
        var options = ProcessJsonSerializerOptions.Create(TestJsonContext.Default);
        var typeInfo = (JsonTypeInfo<CausationId>)options.GetTypeInfo(typeof(CausationId));
        var causationId = CausationId.From("cause-98765");

        var json = JsonSerializer.Serialize(causationId, typeInfo);
        json.Should().Be("\"cause-98765\"");

        var deserialized = JsonSerializer.Deserialize(json, typeInfo);
        deserialized.Should().Be(causationId);

        var actInvalid = () => JsonSerializer.Deserialize("999", typeInfo);
        actInvalid.Should().Throw<JsonException>().WithMessage("Expected string value for CausationId.");

        var converter = new CausationIdJsonConverter();
        var actNullWriter = () => converter.Write(null!, causationId, options);
        actNullWriter.Should().Throw<ArgumentNullException>().WithParameterName("writer");
    }

    [Fact]
    public void MessageIdConverter_ShouldSerializeAndDeserializeCorrectly()
    {
        var options = ProcessJsonSerializerOptions.Create(TestJsonContext.Default);
        var typeInfo = (JsonTypeInfo<MessageId>)options.GetTypeInfo(typeof(MessageId));
        var messageId = MessageId.From("msg-555");

        var json = JsonSerializer.Serialize(messageId, typeInfo);
        json.Should().Be("\"msg-555\"");

        var deserialized = JsonSerializer.Deserialize(json, typeInfo);
        deserialized.Should().Be(messageId);

        var actInvalid = () => JsonSerializer.Deserialize("999", typeInfo);
        actInvalid.Should().Throw<JsonException>().WithMessage("Expected string value for MessageId.");

        var converter = new MessageIdJsonConverter();
        var actNullWriter = () => converter.Write(null!, messageId, options);
        actNullWriter.Should().Throw<ArgumentNullException>().WithParameterName("writer");
    }

    [Fact]
    public void SystemTextJsonProcessStateSerializer_ShouldSerializeAndDeserializeState()
    {
        var serializer = new SystemTextJsonProcessStateSerializer<SampleProcessState>(
            TestJsonContext.Default.SampleProcessState);

        var state = new SampleProcessState("ORD-777", 450.50m, true);
        var bytes = serializer.Serialize(state);

        bytes.Should().NotBeNullOrEmpty();

        var deserialized = serializer.Deserialize(bytes);
        deserialized.Should().Be(state);
    }

    [Fact]
    public void SystemTextJsonProcessStateSerializer_ShouldThrowOnNullStateOrNullTypeInfo()
    {
        var actNullInfo = () => new SystemTextJsonProcessStateSerializer<SampleProcessState>(null!);
        actNullInfo.Should().Throw<ArgumentNullException>().WithParameterName("jsonTypeInfo");

        var serializer = new SystemTextJsonProcessStateSerializer<SampleProcessState>(
            TestJsonContext.Default.SampleProcessState);

        var actNullState = () => serializer.Serialize(null!);
        actNullState.Should().Throw<ArgumentNullException>().WithParameterName("state");

        var nullJsonBytes = Encoding.UTF8.GetBytes("null");
        var actDeserializeNull = () => serializer.Deserialize(nullJsonBytes);
        actDeserializeNull.Should().Throw<JsonException>()
            .WithMessage($"Failed to deserialize payload into state of type '{nameof(SampleProcessState)}'.");
    }
}
#pragma warning restore IL2026, IL3050





