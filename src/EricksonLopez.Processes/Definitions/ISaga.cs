// Copyright © Erickson Lopez. MIT License.
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace EricksonLopez.Processes;

/// <summary>
/// Defines a saga orchestration contract with support for explicit step compensation.
/// </summary>
/// <typeparam name="TState">The domain state type.</typeparam>
public interface ISaga<TState> : IProcess<TState>
    where TState : notnull
{
}




