// Copyright © Erickson Lopez. MIT License.
using System;
using System.Globalization;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading.Tasks;
using EricksonLopez.Processes;
using EricksonLopez.Processes.Abstractions;
using EricksonLopez.Processes.DependencyInjection;
using EricksonLopez.Processes.SystemTextJson;
using EricksonLopez.Processes.Testing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace EricksonLopez.Processes.Showcase.Level02_FullConfiguration;

public sealed record AccountVerificationState(
    string AccountId,
    string Status,
    DateTimeOffset CreatedAt) : IProcessState
{
    public static AccountVerificationState Initial(string accountId, DateTimeOffset now) =>
        new(accountId, "PendingVerification", now);
}

public sealed record VerifyAccountEvent(Guid AccountId);

public sealed record DemoSerializedPayload(
    ProcessId ProcessId,
    CorrelationId CorrelationId,
    Revision Revision);

[JsonSerializable(typeof(AccountVerificationState))]
[JsonSerializable(typeof(DemoSerializedPayload))]
[JsonSerializable(typeof(ProcessId))]
[JsonSerializable(typeof(CorrelationId))]
[JsonSerializable(typeof(Revision))]
internal sealed partial class Level02JsonContext : JsonSerializerContext
{
}

public sealed class AccountVerificationProcess :
    IProcess<AccountVerificationState>,
    IProcessHandler<AccountVerificationState, VerifyAccountEvent>
{
    public ProcessType Type => ProcessType.From("account.verification");
    public ProcessVersion Version => ProcessVersion.Initial;

    public ValueTask<ProcessTransitionResult<AccountVerificationState>> HandleAsync(
        AccountVerificationState state,
        VerifyAccountEvent eventMessage,
        ProcessContext context)
    {
        var updated = state with { Status = "Verified" };
        var effect = ProcessEffect.CreateEvent(new { eventMessage.AccountId, Status = "Verified" });

        return ValueTask.FromResult(ProcessTransitionResult<AccountVerificationState>.Complete(
            updated,
            effects: [effect]));
    }
}

public sealed class VerifyAccountCorrelation : IProcessCorrelation<VerifyAccountEvent>
{
    public ProcessId ExtractProcessId(VerifyAccountEvent @event) => ProcessId.From(@event.AccountId);
    public CorrelationId ExtractCorrelationId(VerifyAccountEvent @event) => CorrelationId.From(@event.AccountId.ToString());
}

/// <summary>
/// Level 2: Full Configuration
/// Demonstrates Dependency Injection, ProcessCoordinatorOptions, TimeProvider, and System.Text.Json configuration.
/// </summary>
public static class Level02FullConfigurationDemo
{
    public static async Task RunAsync()
    {
        Console.ForegroundColor = ConsoleColor.Cyan;
        Console.WriteLine("================================================================================");
        Console.WriteLine(" LEVEL 02: FULL CONFIGURATION (DI, OPTIONS, TIMEPROVIDER & JSON)");
        Console.WriteLine("================================================================================");
        Console.ResetColor();

        // 1. Setup DI Container with options and services
        var services = new ServiceCollection();

        // Core process infrastructure (ProcessRegistry, TimeProvider)
        services.AddProcesses();

        // Register custom store
        services.AddSingleton<IProcessStore<AccountVerificationState>, InMemoryProcessStore<AccountVerificationState>>();

        // Register Process Coordinator with custom CAS retry options
        services.AddProcessCoordinator<AccountVerificationState>(options =>
        {
            options.MaxConcurrencyRetries = 5;
            options.InitialBackoffDelay = TimeSpan.FromMilliseconds(25);
        });

        // Register handlers and correlation extractors
        services.AddTransient<AccountVerificationProcess>();
        services.AddTransient<VerifyAccountCorrelation>();

        var serviceProvider = services.BuildServiceProvider();

        // 2. Resolve configured coordinator from DI
        var coordinator = serviceProvider.GetRequiredService<ProcessCoordinator<AccountVerificationState>>();
        var process = serviceProvider.GetRequiredService<AccountVerificationProcess>();
        var correlation = serviceProvider.GetRequiredService<VerifyAccountCorrelation>();

        var accountId = Guid.NewGuid();
        // ProcessExecutionResult<TState> — the return type of ExecuteAsync and CompensateAsync.
        // Members: Instance, Effects, SaveResult (ProcessSaveResult), RecordedCompensations, IsSuccess.
        ProcessExecutionResult<AccountVerificationState> result = await coordinator.ExecuteAsync(
            handler: process,
            correlation: correlation,
            eventMessage: new VerifyAccountEvent(accountId),
            initialStateFactory: e => AccountVerificationState.Initial(e.AccountId.ToString(), DateTimeOffset.UtcNow),
            canInitiate: true);

        Console.WriteLine($"Executed Process via DI: ID '{result.Instance.Id}', Status: '{result.Instance.Status}'");
        Console.WriteLine($"  ProcessExecutionResult.IsSuccess:            {result.IsSuccess}");
        Console.WriteLine($"  ProcessExecutionResult.SaveResult:           {result.SaveResult}");
        Console.WriteLine($"  ProcessExecutionResult.Effects.Count:        {result.Effects.Count}");
        Console.WriteLine($"  ProcessExecutionResult.RecordedCompensations:{result.RecordedCompensations?.Count ?? 0}");

        // 3. Demonstrate ProcessJsonSerializerOptions & SystemTextJson Converters (AOT-safe)
        var serializer = new SystemTextJsonProcessStateSerializer<AccountVerificationState>(
            Level02JsonContext.Default.AccountVerificationState);

        var bytes = serializer.Serialize(result.Instance.State);
        var restored = serializer.Deserialize(bytes);
        Console.WriteLine($"\nAOT State Serializer roundtrip: AccountId='{restored.AccountId}', Status='{restored.Status}'");

        // 4. ProcessJsonSerializerOptions.Create() — build options with all converters pre-registered
        Console.WriteLine("\n[Section 4] ProcessJsonSerializerOptions.Create() & Configure()");
        var jsonOptions = ProcessJsonSerializerOptions.Create();
        Console.WriteLine($"  ProcessJsonSerializerOptions.Create() registered {jsonOptions.Converters.Count} converters");
        Console.WriteLine("  Converters: ProcessId, ProcessType, ProcessVersion, Revision, CorrelationId, CausationId, MessageId");

        var customOptions = ProcessJsonSerializerOptions.Configure(
            new System.Text.Json.JsonSerializerOptions { WriteIndented = true });
        Console.WriteLine($"  ProcessJsonSerializerOptions.Configure() applied to existing options: {customOptions.Converters.Count} converters");

        // 5. ProcessRegistry.Register() — explicit manual registration
        Console.WriteLine("\n[Section 5] ProcessRegistry — manual Register() and IsRegistered()");
        var registry = new ProcessRegistry();
        var processType = ProcessType.From("account.verification");
        var processVersion = ProcessVersion.Initial;

        registry.Register(processType, processVersion);
        Console.WriteLine($"  Registered: '{processType.Value}' v{processVersion.Value}");
        Console.WriteLine($"  IsRegistered: {registry.IsRegistered(processType, processVersion)}");
        Console.WriteLine($"  RegisteredProcesses count: {registry.RegisteredProcesses.Count}");

        // IProcessRegistry interface resolution from DI (registered by AddProcesses())
        var diRegistry = serviceProvider.GetRequiredService<IProcessRegistry>();
        Console.WriteLine($"  IProcessRegistry from DI type: {diRegistry.GetType().Name}");

        // 6. ProcessContext.Create() — static factory with auto-generated IDs
        Console.WriteLine("\n[Section 6] ProcessContext.Create() — static factory method");
        var procId = result.Instance.Id;
        var corrId = result.Instance.CorrelationId;

        var context = ProcessContext.Create(
            processId: procId,
            correlationId: corrId,
            timeProvider: TimeProvider.System);

        Console.WriteLine($"  ProcessContext.ProcessId:      {context.ProcessId}");
        Console.WriteLine($"  ProcessContext.CorrelationId:  {context.CorrelationId.Value}");
        Console.WriteLine($"  ProcessContext.CausationId:    {context.CausationId.Value}");
        Console.WriteLine($"  ProcessContext.MessageId:      {context.MessageId.Value}");
        Console.WriteLine($"  ProcessContext.Now:            {context.Now:O}");
        Console.WriteLine($"  ProcessContext.Items count:    {context.Items.Count}");

        // 7. Value object Parse / TryParse / operator overloads / factory methods
        Console.WriteLine("\n[Section 7] Value Object Parse(), TryParse(), factory overloads & DefaultBackoffStrategy");

        // ProcessId
        var rawGuid = Guid.NewGuid();
        var parsedId = ProcessId.Parse(rawGuid.ToString(), CultureInfo.InvariantCulture);
        var idFromGuid = ProcessId.FromGuid(rawGuid);
        var guidExtracted = idFromGuid.ToGuid();
        var idParsedOk = ProcessId.TryParse(rawGuid.ToString(), CultureInfo.InvariantCulture, out var tryParsedId);
        Console.WriteLine($"  ProcessId.Parse(guid):        {parsedId}");
        Console.WriteLine($"  ProcessId.FromGuid(guid):     {idFromGuid} (ToGuid == raw: {guidExtracted == rawGuid})");
        Console.WriteLine($"  ProcessId.TryParse():         {idParsedOk} -> {tryParsedId}");
        Console.WriteLine($"  ProcessId == ProcessId:       {parsedId == ProcessId.From(rawGuid)}");
        Console.WriteLine($"  ProcessId < ProcessId:        {ProcessId.Empty < parsedId}");

        // ProcessVersion
        var v1 = ProcessVersion.From(1);
        var v2 = ProcessVersion.FromInt32(2);
        var vInt = v2.ToInt32();
        var vParsedOk = ProcessVersion.TryParse("3", CultureInfo.InvariantCulture, out var v3);
        Console.WriteLine($"  ProcessVersion.Initial:       {ProcessVersion.Initial.Value}");
        Console.WriteLine($"  ProcessVersion.FromInt32(2):  {v2.Value} (ToInt32: {vInt})");
        Console.WriteLine($"  ProcessVersion.TryParse('3'): {vParsedOk} -> {v3.Value}");
        Console.WriteLine($"  v1 < v2:                      {v1 < v2}");
        Console.WriteLine($"  v2 > v1:                      {v2 > v1}");
        Console.WriteLine($"  (int)v2:                      {(int)v2}");

        // Revision
        var rev = Revision.From(3);
        var revFromInt64 = Revision.FromInt64(4L);
        var revParsedOk = Revision.TryParse("5", CultureInfo.InvariantCulture, out var rev5);
        Console.WriteLine($"  Revision.Initial:             {Revision.Initial.Value}");
        Console.WriteLine($"  Revision.None:                {Revision.None.Value}");
        Console.WriteLine($"  Revision.From(3).Next():      {rev.Next().Value}");
        Console.WriteLine($"  Revision.FromInt64(4):        {revFromInt64.Value}");
        Console.WriteLine($"  Revision.From(3).ToInt64():   {rev.ToInt64()}");
        Console.WriteLine($"  Revision.TryParse('5'):       {revParsedOk} -> {rev5.Value}");
        Console.WriteLine($"  (long)rev:                    {(long)rev}");

        // CorrelationId
        var corrParsed = CorrelationId.From("MY-CORRELATION-123");
        var corrFromStr = CorrelationId.FromString("MY-CORRELATION-456");
        var corrFromGuid = CorrelationId.FromGuid(rawGuid);
        var corrParsedOk = CorrelationId.TryParse("MY-CORRELATION-789", null, out var corrTry);
        Console.WriteLine($"  CorrelationId.From():         {corrParsed.Value}");
        Console.WriteLine($"  CorrelationId.FromString():   {corrFromStr.Value}");
        Console.WriteLine($"  CorrelationId.FromGuid():     {corrFromGuid.Value}");
        Console.WriteLine($"  CorrelationId.TryParse():     {corrParsedOk} -> {corrTry.Value}");

        // CausationId
        var causeId = CausationId.NewId();
        var causeFromStr = CausationId.FromString("CAUSE-CMD-101");
        var causeFromGuid = CausationId.FromGuid(rawGuid);
        var causeParsedOk = CausationId.TryParse("CAUSE-CMD-202", null, out var causeTry);
        Console.WriteLine($"  CausationId.NewId():          {causeId.Value}");
        Console.WriteLine($"  CausationId.FromString():     {causeFromStr.Value}");
        Console.WriteLine($"  CausationId.FromGuid():       {causeFromGuid.Value}");
        Console.WriteLine($"  CausationId.TryParse():       {causeParsedOk} -> {causeTry.Value}");

        // MessageId
        var msgId = MessageId.NewId();
        var msgFromStr = MessageId.FromString("MSG-ID-101");
        var msgFromGuid = MessageId.FromGuid(rawGuid);
        var msgParsedOk = MessageId.TryParse("MSG-ID-202", null, out var msgTry);
        Console.WriteLine($"  MessageId.NewId():            {msgId.Value}");
        Console.WriteLine($"  MessageId.FromString():       {msgFromStr.Value}");
        Console.WriteLine($"  MessageId.FromGuid():         {msgFromGuid.Value}");
        Console.WriteLine($"  MessageId.TryParse():         {msgParsedOk} -> {msgTry.Value}");

        // ProcessType — comparison operators and TryParse
        var typeA = ProcessType.From("order.fulfillment");
        var typeFromStr = ProcessType.FromString("payment.workflow");
        var typeParsedOk = ProcessType.TryParse("inventory.check", null, out var typeTry);
        Console.WriteLine($"  ProcessType == ProcessType:   {typeA == ProcessType.From("order.fulfillment")}");
        Console.WriteLine($"  ProcessType.FromString():     {typeFromStr.Value}");
        Console.WriteLine($"  ProcessType.TryParse():       {typeParsedOk} -> {typeTry.Value}");
        Console.WriteLine($"  (string)typeA:                {(string)typeA}");

        // ProcessCoordinator.DefaultBackoffStrategy
        var defaultBackoff = ProcessCoordinator<AccountVerificationState>.DefaultBackoffStrategy(1);
        Console.WriteLine($"  ProcessCoordinator.DefaultBackoffStrategy(attempt 1): {defaultBackoff.TotalMilliseconds}ms");

        Console.ForegroundColor = ConsoleColor.Green;
        Console.WriteLine("\n✔ Level 02 Full Configuration completed successfully.");
        Console.ResetColor();
    }
}
