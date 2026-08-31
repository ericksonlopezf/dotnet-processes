// Copyright © Erickson Lopez. MIT License.
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace EricksonLopez.Processes.Abstractions;

/// <summary>
/// Specifies the lifecycle execution status of a process or saga instance.
/// </summary>
public enum ProcessStatus
{
    /// <summary>
    /// Specifies that the process instance is created and initialized, but has not yet processed triggers.
    /// </summary>
    Initialized = 0,

    /// <summary>
    /// Specifies that the process is actively executing forward workflow steps.
    /// </summary>
    Running = 1,

    /// <summary>
    /// Specifies that the process is suspended, awaiting external input, manual approval, or a timer.
    /// </summary>
    Suspended = 2,

    /// <summary>
    /// Specifies that the process has successfully finished all steps and reached a terminal state.
    /// </summary>
    Completed = 3,

    /// <summary>
    /// Specifies that an error occurred and compensating actions are actively executing.
    /// </summary>
    Compensating = 4,

    /// <summary>
    /// Specifies that all compensating actions completed successfully in a terminal state.
    /// </summary>
    Compensated = 5,

    /// <summary>
    /// Specifies that the process encountered an unrecoverable failure requiring intervention.
    /// </summary>
    Failed = 6
}




