using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;

namespace Pooling
{
    internal class SimplePool
    {
        private const int Buckets = 10;
        private const int MaxSize = 8192;

        public SimplePool()
        {
            for(int i = 0; i < Buckets; ++i)
            {
                Pools[i] = new Stack<byte[]>();
            }
        }

        public byte[] Rent(int size)
        {
            var sz = DetermineSize(size);
            var bucket = DetermineBucket(size);

            lock (Pools[bucket])
            {
                if (Pools[bucket].Count != 0)
                {
                    return Pools[bucket].Pop();
                }

                return new byte[sz];
            }
        }

        public void Return(byte[] arr)
        {
            var bucket = DetermineBucket(arr.Length);
            lock (Pools[bucket])
            {
                Pools[bucket].Push(arr);
            }
        }

        // Determine smallest power of two sized array that will satisfy the request
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static int DetermineSize(int size)
        {
            if (size <= 16) return 16;
            if (size <= 32) return 32;
            if (size <= 64) return 64;
            if (size <= 128) return 128;
            if (size <= 256) return 256;
            if (size <= 512) return 512;
            if (size <= 1024) return 1024;
            if (size <= 2048) return 2048;
            if (size <= 4096) return 4096;
            if (size <= 8192) return 8192;
            return size;
        }

        // Which bucket to pull from?
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static int DetermineBucket(int size)
        {
            if (size <= 16) return 0;
            if (size <= 32) return 1;
            if (size <= 64) return 2;
            if (size <= 128) return 3;
            if (size <= 256) return 4;
            if (size <= 512) return 5;
            if (size <= 1024) return 6;
            if (size <= 2048) return 7;
            if (size <= 4096) return 8;
            if (size <= 8192) return 9;
            throw new InvalidOperationException();
        }

        private readonly Stack<byte[]>[] Pools = new Stack<byte[]>[Buckets];
    }
}
