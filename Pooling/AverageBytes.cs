using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Jobs;
using Iced.Intel;
using System;
using System.Buffers;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;

namespace Pooling
{
    [SimpleJob(RuntimeMoniker.Net472)]
    [SimpleJob(RuntimeMoniker.Net60)]
    [SimpleJob(RuntimeMoniker.Net70)]
    //[SimpleJob(RuntimeMoniker.NativeAot70)]
    [MemoryDiagnoser]
    [DisassemblyDiagnoser(maxDepth: 0)]
    [HideColumns("Error", "StdDev", "Median", "RatioSD")]
    public class AverageBytes
    {
        private readonly int Count;
        private readonly int Iterations;
        private readonly int Concurrency;
        private readonly Random Rand = new Random();

        public AverageBytes(int count = 2048, int iterations = 100, int concurrency = 4)
        {
            Count = count;
            Iterations = iterations;
            Concurrency = concurrency;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        static double GetAverageByte(byte[] bytes)
        {
            double sum = 0;
            for(int i = 0; i < bytes.Length; i++)
            {
                sum += bytes[i];
            }
            return sum / bytes.Length;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        static double GetAverageByte(Span<byte> bytes)
        {
            double sum = 0;
            for (int i = 0; i < bytes.Length; i++)
            {
                sum += bytes[i];
            }
            return sum / bytes.Length;
        }

        [Benchmark(Baseline = true)]
        public double[] GetAverageBaseline()
        {
            double[] results = new double[Iterations];

            var bag = new ConcurrentBag<int>(Enumerable.Range(0, Iterations));

            Task[] tasks = new Task[Concurrency];

            for(int i = 0; i < Concurrency; ++i)
            {
                tasks[i] = Task.Run(() =>
                {
                    while(bag.TryTake(out var j))
                    {
                        var bytes = new byte[Count];
                        Rand.NextBytes(bytes);

                        results[j] = GetAverageByte(bytes);
                    }
                });
            }

            Task.WhenAll(tasks).Wait();

            return results;
        }

        [Benchmark]
        public double[] SimplePooled()
        {
            var pool = new SimplePool();

            double[] results = new double[Iterations];

            var bag = new ConcurrentBag<int>(Enumerable.Range(0, Iterations));

            Task[] tasks = new Task[Concurrency];

            for (int i = 0; i < Concurrency; ++i)
            {
                tasks[i] = Task.Run(() =>
                {
                    while (bag.TryTake(out var j))
                    {
                        var bytes = pool.Rent(Count);
                        Rand.NextBytes(bytes);

                        results[j] = GetAverageByte(bytes);

                        pool.Return(bytes);
                    }
                });
            }

            Task.WhenAll(tasks).Wait();

            return results;
        }

        [Benchmark]
        public double[] ArrayPooled()
        {
            var pool = ArrayPool<byte>.Shared;

            double[] results = new double[Iterations];

            var bag = new ConcurrentBag<int>(Enumerable.Range(0, Iterations));

            Task[] tasks = new Task[Concurrency];

            for (int i = 0; i < Concurrency; ++i)
            {
                tasks[i] = Task.Run(() =>
                {
                    while (bag.TryTake(out var j))
                    {
                        var bytes = pool.Rent(Count);
                        Rand.NextBytes(bytes);

                        results[j] = GetAverageByte(bytes);

                        pool.Return(bytes);
                    }
                });
            }

            Task.WhenAll(tasks).Wait();

            return results;
        }

        [Benchmark]
        public double[] Stackalloc()
        {
#if NET5_0_OR_GREATER
            double[] results = new double[Iterations];

            var bag = new ConcurrentBag<int>(Enumerable.Range(0, Iterations));

            Task[] tasks = new Task[Concurrency];

            for (int i = 0; i < Concurrency; ++i)
            {
                tasks[i] = Task.Run(() =>
                {
                    while (bag.TryTake(out var j))
                    {
                        //var bytes = pool.Rent(Count);
                        Span<byte> bytes = stackalloc byte[Count];
                        Rand.NextBytes(bytes);

                        results[j] = GetAverageByte(bytes);
                    }
                });
            }

            Task.WhenAll(tasks).Wait();

            return results;
#else
            throw new NotImplementedException();
#endif
        }
    }
}
