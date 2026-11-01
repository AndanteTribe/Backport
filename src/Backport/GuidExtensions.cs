// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#if !NET9_0_OR_GREATER
using System.Runtime.CompilerServices;

namespace System;

/// <summary>Provides UUID version 7 generation on runtimes before .NET 9.</summary>
public static class GuidExtensions
{
    private const byte Variant10XxMask = 0xC0;
    private const byte Variant10XxValue = 0x80;
    private const ushort VersionMask = 0xF000;
    private const ushort Version7Value = 0x7000;

    /// <summary>Extensions for <see cref="Guid"/>.</summary>
    extension(Guid)
    {
        /// <summary>Creates a new UUID version 7 using the current UTC timestamp.</summary>
        public static Guid CreateVersion7() => CreateVersion7(DateTimeOffset.UtcNow);

        /// <summary>Creates a new UUID version 7 using the specified timestamp.</summary>
        /// <param name="timestamp">The timestamp used for the Unix Epoch milliseconds.</param>
        /// <exception cref="ArgumentOutOfRangeException">The timestamp precedes the Unix Epoch.</exception>
        public static Guid CreateVersion7(DateTimeOffset timestamp)
        {
            // Ported from dotnet/runtime v10.0.14 Guid.CreateVersion7.
            // NewGuid supplies the random bits; its version/variant bits are replaced below.
            var result = Guid.NewGuid();
            var unixTimestampMilliseconds = timestamp.ToUnixTimeMilliseconds();
            if (unixTimestampMilliseconds < 0)
            {
                throw new ArgumentOutOfRangeException(nameof(timestamp), unixTimestampMilliseconds, "The timestamp must not precede the Unix Epoch.");
            }

            // Guid's sequential layout is int _a, short _b, short _c, byte _d, ... .
            // Access the fields in their native types, independent of byte order.
            // See also https://neue.cc/2024/11/19_cysharp_oss.html.
            Unsafe.As<Guid, int>(ref result) = (int)(unixTimestampMilliseconds >> 16);
            Unsafe.Add(ref Unsafe.As<Guid, short>(ref result), 2) = (short)unixTimestampMilliseconds;
            ref var c = ref Unsafe.Add(ref Unsafe.As<Guid, short>(ref result), 3);
            c = (short)((c & ~VersionMask) | Version7Value);
            ref var d = ref Unsafe.Add(ref Unsafe.As<Guid, byte>(ref result), 8);
            d = (byte)((d & ~Variant10XxMask) | Variant10XxValue);
            return result;
        }
    }
}
#endif