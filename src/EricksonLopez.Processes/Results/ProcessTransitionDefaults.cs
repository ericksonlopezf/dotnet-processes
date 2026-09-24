// Copyright © Erickson Lopez. MIT License.
using System;
using System.Collections.Generic;
using EricksonLopez.Processes.Abstractions;

namespace EricksonLopez.Processes;

internal static class ProcessTransitionDefaults
{
    internal static readonly IReadOnlyList<ProcessEffect> EmptyEffects = Array.Empty<ProcessEffect>();
    internal static readonly IReadOnlyList<CompensationStep> EmptyCompensations = Array.Empty<CompensationStep>();
}
