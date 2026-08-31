// Copyright © Erickson Lopez. MIT License.
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace EricksonLopez.Processes.Abstractions;

/// <summary>
/// Specifies that a method handles incoming events within a process manager or saga.
/// </summary>
[AttributeUsage(AttributeTargets.Method, Inherited = false, AllowMultiple = false)]
public sealed class ProcessHandlerAttribute : Attribute
{
    /// <summary>
    /// Gets a value indicating whether the handled event can initiate a new process instance when none exists.
    /// </summary>
    public bool CanInitiate { get; }

    /// <summary>
    /// Initializes a new instance of the <see cref="ProcessHandlerAttribute"/> class with the initiation capability indicator.
    /// </summary>
    /// <param name="canInitiate">A value indicating whether the handled event can initiate a new process instance.</param>
    public ProcessHandlerAttribute(bool canInitiate = false)
    {
        CanInitiate = canInitiate;
    }
}




