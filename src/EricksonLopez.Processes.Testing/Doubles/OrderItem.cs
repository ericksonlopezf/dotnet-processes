// Copyright © Erickson Lopez. MIT License.

namespace EricksonLopez.Processes.Testing.Doubles;

/// <summary>
/// Represents a nested order line item for storage tests.
/// </summary>
/// <param name="Sku">The product SKU identifier.</param>
/// <param name="Quantity">The ordered quantity.</param>
/// <param name="UnitPrice">The unit price amount.</param>
public sealed record OrderItem(string Sku, int Quantity, decimal UnitPrice);
