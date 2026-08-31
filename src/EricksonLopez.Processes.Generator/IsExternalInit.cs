// Copyright © Erickson Lopez. MIT License.
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

#if NETSTANDARD2_0
namespace System.Runtime.CompilerServices;

[System.Diagnostics.CodeAnalysis.SuppressMessage("Minor Code Smell", "S2094:Classes should not be empty", Justification = "Polyfill for init-only properties in netstandard2.0")]
internal static class IsExternalInit
{
}
#endif




