// Copyright © Erickson Lopez. MIT License.
using System.Collections.Generic;
using EricksonLopez.Processes.Abstractions;

namespace EricksonLopez.Processes.Testing.Doubles;

/// <summary>
/// Represents a complex order domain state containing nested objects and collections for storage tests.
/// </summary>
/// <param name="CustomerId">The customer identifier.</param>
/// <param name="BillingAddress">The billing address details.</param>
/// <param name="ShippingAddress">The shipping address details.</param>
/// <param name="Metadata">The key-value metadata dictionary.</param>
/// <param name="Items">The list of line items in the order.</param>
public sealed record ComplexOrderState(
    string CustomerId,
    Address BillingAddress,
    Address ShippingAddress,
    Dictionary<string, string> Metadata,
    List<OrderItem> Items
) : IProcessState;
