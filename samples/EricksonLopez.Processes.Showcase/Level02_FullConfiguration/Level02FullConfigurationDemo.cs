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
        var result = await coordinator.ExecuteAsync(
            handler: process,
            correlation: correlation,
            eventMessage: new VerifyAccountEvent(accountId),
            initialStateFactory: e => AccountVerificationState.Initial(e.AccountId.ToString(), DateTimeOffset.UtcNow),
            canInitiate: true);

        Console.WriteLine($"Executed Process via DI: ID '{result.Instance.Id}', Status: '{result.Instance.Status}'");

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

        // 7. Value object Parse / TryParse / operator overloads
        Console.WriteLine("\n[Section 7] Value Object Parse(), TryParse(), and operator overloads");

        // ProcessId
        var rawGuid = Guid.NewGuid();
        var parsedId = ProcessId.Parse(rawGuid.ToString(), CultureInfo.InvariantCulture);
        Console.WriteLine($"  ProcessId.Parse(guid):        {parsedId}");
        Console.WriteLine($"  ProcessId == ProcessId:       {parsedId == ProcessId.From(rawGuid)}");
        Console.WriteLine($"  ProcessId < ProcessId:        {ProcessId.Empty < parsedId}");

        // ProcessVersion
        var v1 = ProcessVersion.From(1);
        var v2 = ProcessVersion.From(2);
        Console.WriteLine($"  ProcessVersion.Initial:       {ProcessVersion.Initial.Value}");
        Console.WriteLine($"  v1 < v2:                      {v1 < v2}");
        Console.WriteLine($"  v2 > v1:                      {v2 > v1}");
        Console.WriteLine($"  (int)v2:                      {(int)v2}");

        // Revision
        var rev = Revision.From(3);
        Console.WriteLine($"  Revision.Initial:             {Revision.Initial.Value}");
        Console.WriteLine($"  Revision.None:                {Revision.None.Value}");
        Console.WriteLine($"  Revision.From(3).Next():      {rev.Next().Value}");
        Console.WriteLine($"  Revision.From(3).ToInt64():   {rev.ToInt64()}");
        Console.WriteLine($"  (long)rev:                    {(long)rev}");

        // CorrelationId
        var corrParsed = CorrelationId.From("MY-CORRELATION-123");
        Console.WriteLine($"  CorrelationId.From():         {corrParsed.Value}");

        // CausationId
        var causeId = CausationId.NewId();
        Console.WriteLine($"  CausationId.NewId():          {causeId.Value}");

        // MessageId
        var msgId = MessageId.NewId();
        Console.WriteLine($"  MessageId.NewId():            {msgId.Value}");

        // ProcessType — comparison operators
        var typeA = ProcessType.From("order.fulfillment");
        var typeB = ProcessType.From("payment.workflow");
        Console.WriteLine($"  ProcessType == ProcessType:   {typeA == ProcessType.From("order.fulfillment")}");
        Console.WriteLine($"  (string)typeA:                {(string)typeA}");

        Console.ForegroundColor = ConsoleColor.Green;
        Console.WriteLine("\n✔ Level 02 Full Configuration completed successfully.");
        Console.ResetColor();
    }
}
