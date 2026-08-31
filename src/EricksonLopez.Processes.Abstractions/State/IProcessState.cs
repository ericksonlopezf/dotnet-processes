// Copyright © Erickson Lopez. MIT License.
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace EricksonLopez.Processes.Abstractions;

/// <summary>
/// Defines a marker interface representing the domain state schema of a process instance.
/// </summary>
/// <remarks>
/// State implementations should be immutable C# records or readonly structs.
/// </remarks>
public interface IProcessState
{
}




