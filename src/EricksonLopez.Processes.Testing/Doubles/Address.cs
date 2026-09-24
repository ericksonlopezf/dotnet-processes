// Copyright © Erickson Lopez. MIT License.

namespace EricksonLopez.Processes.Testing.Doubles;

/// <summary>
/// Represents a nested address model for complex storage tests.
/// </summary>
/// <param name="Street">The street address.</param>
/// <param name="City">The city name.</param>
/// <param name="Country">The country name.</param>
public sealed record Address(string Street, string City, string Country);
