// Copyright © Erickson Lopez. MIT License.
using EricksonLopez.Processes.Abstractions;

namespace EricksonLopez.Processes.Testing.Doubles;

/// <summary>
/// Represents a domain event for incrementing a test counter process.
/// </summary>
/// <param name="TargetId">The target process identifier.</param>
/// <param name="Delta">The amount to increment.</param>
public sealed record TestIncrementEvent(ProcessId TargetId, int Delta = 1);
