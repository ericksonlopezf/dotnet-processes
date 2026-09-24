// Copyright © Erickson Lopez. MIT License.
using EricksonLopez.Processes.Abstractions;

namespace EricksonLopez.Processes.Testing.Doubles;

/// <summary>
/// Represents a sample order domain state for storage integration tests.
/// </summary>
/// <param name="CustomerId">The customer identifier.</param>
/// <param name="TotalAmount">The total monetary amount.</param>
/// <param name="IsDelivered">A value indicating whether the order is delivered.</param>
public sealed record SampleOrderState(string CustomerId, decimal TotalAmount, bool IsDelivered) : IProcessState;
