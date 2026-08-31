// Copyright © Erickson Lopez. MIT License.
using System;
using System.Collections.Generic;
using System.Text.Json.Serialization;
using EricksonLopez.Processes.Abstractions;

namespace EricksonLopez.Processes.Testing.Doubles;

/// <summary>
/// Represents a sample order domain state for storage integration tests.
/// </summary>
/// <param name="CustomerId">The customer identifier.</param>
/// <param name="TotalAmount">The total monetary amount.</param>
/// <param name="IsDelivered">A value indicating whether the order is delivered.</param>
public sealed record SampleOrderState(string CustomerId, decimal TotalAmount, bool IsDelivered) : IProcessState;

/// <summary>
/// Represents a nested address model for complex storage tests.
/// </summary>
/// <param name="Street">The street address.</param>
/// <param name="City">The city name.</param>
/// <param name="Country">The country name.</param>
public sealed record Address(string Street, string City, string Country);

/// <summary>
/// Represents a nested order line item for storage tests.
/// </summary>
/// <param name="Sku">The product SKU identifier.</param>
/// <param name="Quantity">The ordered quantity.</param>
/// <param name="UnitPrice">The unit price amount.</param>
public sealed record OrderItem(string Sku, int Quantity, decimal UnitPrice);

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

/// <summary>
/// Provides JSON serialization metadata for storage test models.
/// </summary>
[JsonSerializable(typeof(SampleOrderState))]
[JsonSerializable(typeof(Address))]
[JsonSerializable(typeof(OrderItem))]
[JsonSerializable(typeof(ComplexOrderState))]
[JsonSerializable(typeof(ProcessId))]
[JsonSerializable(typeof(CorrelationId))]
[JsonSerializable(typeof(ProcessVersion))]
[JsonSerializable(typeof(Revision))]
public sealed partial class SharedStorageTestJsonContext : JsonSerializerContext
{
}
