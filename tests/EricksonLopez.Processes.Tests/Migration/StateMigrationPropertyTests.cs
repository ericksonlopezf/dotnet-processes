// Copyright © Erickson Lopez. MIT License.
using System;
using EricksonLopez.Processes.Abstractions;
using FsCheck;
using FsCheck.Xunit;
using Xunit;

namespace EricksonLopez.Processes.Tests.Migration;

[Trait("Category", "Property")]
public class StateMigrationPropertyTests
{
    public sealed record SampleV1(string Id, int Amount) : IProcessState;
    public sealed record SampleV2(string Id, int Amount, string Currency) : IProcessState;
    public sealed record SampleV3(string Id, int Amount, string Currency, bool Verified) : IProcessState;

    [Property]
    public bool MigrationPipeline_CompositionLaw_ProducesSameResultAsSequentialStepApplication(NonNull<string> id, int amount)
    {
        var originalId = id.Get;
        var original = new SampleV1(originalId, amount);

        // Define pipeline V1 -> V2 -> V3
        var pipeline = ProcessStateMigrationPipeline
            .Create<SampleV1>(ProcessVersion.From(1))
            .AddStep(ProcessVersion.From(2), v1 => new SampleV2(v1.Id, v1.Amount * 2, "USD"))
            .AddStep(ProcessVersion.From(3), v2 => new SampleV3(v2.Id, v2.Amount + 10, v2.Currency, true))
            .Build<SampleV1>();

        // Step-by-step manual migration
        var manualV2 = new SampleV2(original.Id, original.Amount * 2, "USD");
        var manualV3 = new SampleV3(manualV2.Id, manualV2.Amount + 10, manualV2.Currency, true);

        // Pipeline migration
        var migrated = pipeline.Migrate(original);

        return migrated.Id == manualV3.Id
            && migrated.Amount == manualV3.Amount
            && migrated.Currency == manualV3.Currency
            && migrated.Verified == manualV3.Verified
            && pipeline.FromVersion == ProcessVersion.From(1)
            && pipeline.ToVersion == ProcessVersion.From(3);
    }

    [Property]
    public bool SingleStepPipeline_IsIdentityTransformationWhenStepIsIdempotent(NonNull<string> id, int amount)
    {
        var original = new SampleV1(id.Get, amount);

        var pipeline = ProcessStateMigrationPipeline
            .Create<SampleV1>(ProcessVersion.From(1))
            .AddStep(ProcessVersion.From(2), v1 => new SampleV1(v1.Id, v1.Amount))
            .Build<SampleV1>();

        var migrated = pipeline.Migrate(original);

        return migrated.Id == original.Id
            && migrated.Amount == original.Amount
            && pipeline.FromVersion.Value == 1
            && pipeline.ToVersion.Value == 2;
    }
}
