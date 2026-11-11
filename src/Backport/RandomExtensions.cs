#if !NET6_0_OR_GREATER

using System.Buffers.Binary;
using System.Runtime.CompilerServices;
using System.Security.Cryptography;

namespace System;

public static class RandomExtensions
{
    private static readonly Random s_shared = new ThreadSafeRandom();

    /// <summary>
    /// <see cref="Random"/> extensions.
    /// </summary>
    extension(Random)
    {
        /// <summary>
        /// Provides a thread-safe <see cref="Random"/> instance that may be used concurrently from any thread.
        /// </summary>
        public static Random Shared
        {
            get => s_shared;
        }
    }

    private sealed class ThreadSafeRandom : Random
    {
        [ThreadStatic]
        private static XoshiroState? s_random;

        private static XoshiroState LocalRandom => s_random ??= new XoshiroState();

        public override int Next()
        {
            uint result;
            do
            {
                result = LocalRandom.NextUInt32() >> 1;
            }
            while (result == int.MaxValue);
            return (int)result;
        }

        public override int Next(int maxValue)
        {
            if (maxValue < 0)
            {
                throw new ArgumentOutOfRangeException(nameof(maxValue));
            }
            return (int)LocalRandom.NextBelow((uint)maxValue);
        }

        public override int Next(int minValue, int maxValue)
        {
            if (minValue > maxValue)
            {
                throw new ArgumentOutOfRangeException(nameof(minValue));
            }
            return (int)((long)LocalRandom.NextBelow((uint)((long)maxValue - minValue)) + minValue);
        }

        public override void NextBytes(Span<byte> buffer)
        {
            var random = LocalRandom;
            while (buffer.Length >= sizeof(ulong))
            {
                BinaryPrimitives.WriteUInt64LittleEndian(buffer, random.NextUInt64());
                buffer = buffer.Slice(sizeof(ulong));
            }
            if (!buffer.IsEmpty)
            {
                ulong remaining = random.NextUInt64();
                for (int i = 0; i < buffer.Length; i++)
                {
                    buffer[i] = (byte)remaining;
                    remaining >>= 8;
                }
            }
        }

        public override void NextBytes(byte[] buffer)
        {
            if (buffer is null)
            {
                throw new ArgumentNullException(nameof(buffer));
            }
            NextBytes(buffer.AsSpan());
        }

        public override double NextDouble()
        {
            return (LocalRandom.NextUInt64() >> 11) * (1.0 / (1UL << 53));
        }

        private sealed class XoshiroState
        {
            private ulong _s0, _s1, _s2, _s3;

            internal XoshiroState()
            {
                Span<byte> seed = stackalloc byte[32];
                do
                {
                    RandomNumberGenerator.Fill(seed);
                    _s0 = BinaryPrimitives.ReadUInt64LittleEndian(seed);
                    _s1 = BinaryPrimitives.ReadUInt64LittleEndian(seed.Slice(8));
                    _s2 = BinaryPrimitives.ReadUInt64LittleEndian(seed.Slice(16));
                    _s3 = BinaryPrimitives.ReadUInt64LittleEndian(seed.Slice(24));
                }
                while ((_s0 | _s1 | _s2 | _s3) == 0);
            }

            // xoshiro256**, as used by the 64-bit .NET 10 Random implementation.
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            internal ulong NextUInt64()
            {
                unchecked
                {
                    ulong s0 = _s0, s1 = _s1, s2 = _s2, s3 = _s3;

                    ulong result = RotateLeft(s1 * 5, 7) * 9;
                    ulong t = s1 << 17;

                    s2 ^= s0;
                    s3 ^= s1;
                    s1 ^= s2;
                    s0 ^= s3;

                    s2 ^= t;
                    s3 = RotateLeft(s3, 45);

                    _s0 = s0;
                    _s1 = s1;
                    _s2 = s2;
                    _s3 = s3;

                    return result;
                }
            }

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            internal uint NextUInt32() => (uint)(NextUInt64() >> 32);

            internal uint NextBelow(uint maxValue)
            {
                ulong product = (ulong)maxValue * NextUInt32();
                uint low = (uint)product;
                if (low < maxValue)
                {
                    uint threshold = unchecked(0u - maxValue) % maxValue;
                    while (low < threshold)
                    {
                        product = (ulong)maxValue * NextUInt32();
                        low = (uint)product;
                    }
                }
                return (uint)(product >> 32);
            }

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            private static ulong RotateLeft(ulong value, int offset) =>
                (value << offset) | (value >> (64 - offset));
        }
    }
}

#endif