using BenchmarkDotNet.Running;
using System;

namespace Pooling
{
    internal class Program
    {
        static void Main(string[] args)
        {
            BenchmarkRunner.Run<AverageBytes>();

            Console.WriteLine("Hello, World!");
        }
    }
}