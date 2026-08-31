// Copyright © Erickson Lopez. MIT License.
using System;
using EricksonLopez.Processes.Abstractions;

namespace EricksonLopez.Processes.Testing.Doubles;

/// <summary>
/// Represents a numeric counter process state for concurrency and optimistic concurrency testing.
/// </summary>
/// <param name="Count">The current counter value.</param>
public sealed record TestCounterState(int Count = 0) : IProcessState;
