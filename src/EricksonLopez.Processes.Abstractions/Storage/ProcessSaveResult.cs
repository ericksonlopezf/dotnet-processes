// Copyright © Erickson Lopez. MIT License.
using System;

namespace EricksonLopez.Processes.Abstractions;

/// <summary>
/// Specifies the outcome of saving a process instance to durable storage.
/// </summary>
public enum ProcessSaveResult
{
    /// <summary>
    /// Specifies that the process instance was successfully created or updated with a matching revision token.
    /// </summary>
    Success = 0,

    /// <summary>
    /// Specifies that an optimistic concurrency conflict occurred because the stored revision did not match the expected revision.
    /// </summary>
    ConcurrencyConflict = 1,

    /// <summary>
    /// Specifies that the target process instance was not found in storage.
    /// </summary>
    NotFound = 2,

    /// <summary>
    /// Specifies that a storage or network infrastructure error occurred during persistence.
    /// </summary>
    PersistenceError = 3
}
