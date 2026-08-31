// Copyright © Erickson Lopez. MIT License.
using System;
using EricksonLopez.Processes.Abstractions;

namespace EricksonLopez.Processes.Testing.Doubles;

/// <summary>
/// Represents a reusable order process domain state for unit and integration testing.
/// </summary>
/// <param name="OrderId">The order identifier string.</param>
/// <param name="Amount">The total order amount.</param>
/// <param name="IsPaid">A value indicating whether payment has been captured.</param>
/// <param name="IsCompleted">A value indicating whether the order lifecycle has completed.</param>
public sealed record TestOrderState(
    string OrderId,
    decimal Amount = 0m,
    bool IsPaid = false,
    bool IsCompleted = false) : IProcessState;
