// Copyright © Erickson Lopez. MIT License.
using System;

namespace EricksonLopez.Processes.Testing.Doubles;

/// <summary>
/// Represents a domain event for payment completion of a test order.
/// </summary>
/// <param name="OrderId">The process and order identifier.</param>
public sealed record TestOrderPaidEvent(Guid OrderId);
