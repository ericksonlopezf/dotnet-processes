// Copyright © Erickson Lopez. MIT License.
using System;

namespace EricksonLopez.Processes.Testing.Doubles;

/// <summary>
/// Represents a domain event for the creation of a test order.
/// </summary>
/// <param name="OrderId">The process and order identifier.</param>
/// <param name="Amount">The initial order amount.</param>
public sealed record TestOrderCreatedEvent(Guid OrderId, decimal Amount);
