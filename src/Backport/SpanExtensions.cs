// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#if !NET8_0_OR_GREATER
using System.Text;
#if NET6_0_OR_GREATER
using TryWriteInterpolatedStringHandler = System.MemoryExtensions.TryWriteInterpolatedStringHandler;
#else
using TryWriteInterpolatedStringHandler = Backport.Internal.CompositeFormatTryWriteHandler;
#endif

namespace System;

/// <summary>Provides span formatting with parsed composite formats.</summary>
public static class SpanExtensions
{
    /// <summary>Writes one argument using a parsed composite format.</summary>
    public static bool TryWrite<TArg0>(this Span<char> destination, IFormatProvider? provider, CompositeFormat format, out int charsWritten, TArg0 arg0)
    {
        Validate(format, 1);
        return TryWriteCore(destination, provider, format, out charsWritten, arg0, 0, 0, default);
    }

    /// <summary>Writes two arguments using a parsed composite format.</summary>
    public static bool TryWrite<TArg0, TArg1>(this Span<char> destination, IFormatProvider? provider, CompositeFormat format, out int charsWritten, TArg0 arg0, TArg1 arg1)
    {
        Validate(format, 2);
        return TryWriteCore(destination, provider, format, out charsWritten, arg0, arg1, 0, default);
    }

    /// <summary>Writes three arguments using a parsed composite format.</summary>
    public static bool TryWrite<TArg0, TArg1, TArg2>(this Span<char> destination, IFormatProvider? provider, CompositeFormat format, out int charsWritten, TArg0 arg0, TArg1 arg1, TArg2 arg2)
    {
        Validate(format, 3);
        return TryWriteCore(destination, provider, format, out charsWritten, arg0, arg1, arg2, default);
    }

    /// <summary>Writes an array of arguments using a parsed composite format.</summary>
    public static bool TryWrite(this Span<char> destination, IFormatProvider? provider, CompositeFormat format, out int charsWritten, params object?[] args)
    {
        if (format is null)
        {
            throw new ArgumentNullException(nameof(format));
        }
        if (args is null)
        {
            throw new ArgumentNullException(nameof(args));
        }
        return TryWrite(destination, provider, format, out charsWritten, (ReadOnlySpan<object?>)args);
    }

    /// <summary>Writes a span of arguments using a parsed composite format.</summary>
    public static bool TryWrite(this Span<char> destination, IFormatProvider? provider, CompositeFormat format, out int charsWritten, params ReadOnlySpan<object?> args)
    {
        Validate(format, args.Length);
        return args.Length switch
        {
            0 => TryWriteCore(destination, provider, format, out charsWritten, 0, 0, 0, args),
            1 => TryWriteCore(destination, provider, format, out charsWritten, args[0], 0, 0, args),
            2 => TryWriteCore(destination, provider, format, out charsWritten, args[0], args[1], 0, args),
            _ => TryWriteCore(destination, provider, format, out charsWritten, args[0], args[1], args[2], args),
        };
    }

    private static void Validate(CompositeFormat format, int count)
    {
        if (format is null)
        {
            throw new ArgumentNullException(nameof(format));
        }
        format.ValidateNumberOfArgs(count);
    }

    // Ported from dotnet/runtime v10.0.14 MemoryExtensions.TryWrite.
    private static bool TryWriteCore<TArg0, TArg1, TArg2>(Span<char> destination, IFormatProvider? provider, CompositeFormat format, out int charsWritten, TArg0 arg0, TArg1 arg1, TArg2 arg2, ReadOnlySpan<object?> args)
    {
        var handler = new TryWriteInterpolatedStringHandler(format._literalLength, format._formattedCount, destination, provider, out var shouldAppend);
        if (shouldAppend)
        {
            foreach (var segment in format._segments)
            {
                bool appended;
                if (segment.Literal is not null)
                {
                    appended = handler.AppendLiteral(segment.Literal);
                }
                else
                {
                    appended = segment.ArgIndex switch
                    {
                        0 => handler.AppendFormatted(arg0, segment.Alignment, segment.Format),
                        1 => handler.AppendFormatted(arg1, segment.Alignment, segment.Format),
                        2 => handler.AppendFormatted(arg2, segment.Alignment, segment.Format),
                        _ => handler.AppendFormatted(args[segment.ArgIndex], segment.Alignment, segment.Format),
                    };
                }
                if (!appended)
                {
                    break;
                }
            }
        }
#if NET6_0_OR_GREATER
        return MemoryExtensions.TryWrite(destination, provider, ref handler, out charsWritten);
#else
        charsWritten = handler._success ? handler._pos : 0;
        return handler._success;
#endif
    }
}
#endif