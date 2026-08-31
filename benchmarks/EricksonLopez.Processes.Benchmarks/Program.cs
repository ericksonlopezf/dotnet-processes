// Copyright © Erickson Lopez. MIT License.
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using BenchmarkDotNet.Running;

namespace EricksonLopez.Processes.Benchmarks;

public static class Program
{
    public static void Main(string[] args)
    {
        BenchmarkRunner.Run<ProcessBenchmarks>();
    }
}




