// Copyright © Erickson Lopez. MIT License.
using System;

namespace EricksonLopez.Processes.Abstractions;

/// <summary>
/// Represents a flat, schema-agnostic storage record of a process instance for database engines.
/// </summary>
public sealed record ProcessStateRecord
{
    /// <summary>
    /// Gets the unique process instance identifier as a string.
    /// </summary>
    public required string ProcessId { get; init; }

    /// <summary>
    /// Gets the logical process type name or identifier.
    /// </summary>
    public required string ProcessType { get; init; }

    /// <summary>
    /// Gets the schema or definition version string.
    /// </summary>
    public required string Version { get; init; }

    /// <summary>
    /// Gets the integer representation of the lifecycle execution status.
    /// </summary>
    public required int Status { get; init; }

    /// <summary>
    /// Gets the optimistic concurrency revision integer token.
    /// </summary>
    public required long Revision { get; init; }

    /// <summary>
    /// Gets the business correlation identifier.
    /// </summary>
    public required string CorrelationId { get; init; }

    /// <summary>
    /// Gets the serialized state payload string.
    /// </summary>
    public required string StatePayload { get; init; }

    /// <summary>
    /// Gets the UTC timestamp when the process instance was created.
    /// </summary>
    public required DateTimeOffset CreatedAt { get; init; }

    /// <summary>
    /// Gets the UTC timestamp when the process instance was last updated.
    /// </summary>
    public required DateTimeOffset UpdatedAt { get; init; }

    /// <summary>
    /// Gets the UTC timestamp when the process reached a terminal status, if any.
    /// </summary>
    public DateTimeOffset? CompletedAt { get; init; }
}
