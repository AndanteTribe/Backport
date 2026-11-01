// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#if !NET8_0_OR_GREATER
using System.Runtime.CompilerServices;

namespace System.Text;

/// <summary>Provides StringBuilder formatting with parsed composite formats.</summary>
public static class StringBuilderExtensions
{
    /// <summary>Appends one argument using a parsed composite format.</summary>
    public static StringBuilder AppendFormat<TArg0>(this StringBuilder builder, IFormatProvider? provider, CompositeFormat format, TArg0 arg0)
    {
        Validate(format, 1);
        return AppendFormatCore(builder, provider, format, arg0, 0, 0, default);
    }

    /// <summary>Appends two arguments using a parsed composite format.</summary>
    public static StringBuilder AppendFormat<TArg0, TArg1>(this StringBuilder builder, IFormatProvider? provider, CompositeFormat format, TArg0 arg0, TArg1 arg1)
    {
        Validate(format, 2);
        return AppendFormatCore(builder, provider, format, arg0, arg1, 0, default);
    }

    /// <summary>Appends three arguments using a parsed composite format.</summary>
    public static StringBuilder AppendFormat<TArg0, TArg1, TArg2>(this StringBuilder builder, IFormatProvider? provider, CompositeFormat format, TArg0 arg0, TArg1 arg1, TArg2 arg2)
    {
        Validate(format, 3);
        return AppendFormatCore(builder, provider, format, arg0, arg1, arg2, default);
    }

    /// <summary>Appends an array of arguments using a parsed composite format.</summary>
    public static StringBuilder AppendFormat(this StringBuilder builder, IFormatProvider? provider, CompositeFormat format, params object?[] args)
    {
        if (builder is null)
        {
            throw new ArgumentNullException(nameof(builder));
        }
        if (format is null)
        {
            throw new ArgumentNullException(nameof(format));
        }
        if (args is null)
        {
            throw new ArgumentNullException(nameof(args));
        }
        return AppendFormat(builder, provider, format, (ReadOnlySpan<object?>)args);
    }

    /// <summary>Appends a span of arguments using a parsed composite format.</summary>
    public static StringBuilder AppendFormat(this StringBuilder builder, IFormatProvider? provider, CompositeFormat format, params ReadOnlySpan<object?> args)
    {
        Validate(format, args.Length);
        return args.Length switch
        {
            0 => AppendFormatCore(builder, provider, format, 0, 0, 0, args),
            1 => AppendFormatCore(builder, provider, format, args[0], 0, 0, args),
            2 => AppendFormatCore(builder, provider, format, args[0], args[1], 0, args),
            _ => AppendFormatCore(builder, provider, format, args[0], args[1], args[2], args),
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

    // Ported from dotnet/runtime v10.0.14 StringBuilder.AppendFormat.
    private static StringBuilder AppendFormatCore<TArg0, TArg1, TArg2>(StringBuilder builder, IFormatProvider? provider, CompositeFormat format, TArg0 arg0, TArg1 arg1, TArg2 arg2, ReadOnlySpan<object?> args)
    {
        if (builder is null)
        {
            throw new ArgumentNullException(nameof(builder));
        }
        foreach (var segment in format._segments)
        {
            if (segment.Literal is not null)
            {
                builder.Append(segment.Literal);
            }
            else
            {
                switch (segment.ArgIndex)
                {
                    case 0:
                        AppendFormatted(builder, provider, arg0, segment.Alignment, segment.Format);
                        break;
                    case 1:
                        AppendFormatted(builder, provider, arg1, segment.Alignment, segment.Format);
                        break;
                    case 2:
                        AppendFormatted(builder, provider, arg2, segment.Alignment, segment.Format);
                        break;
                    default:
                        AppendFormatted(builder, provider, args[segment.ArgIndex], segment.Alignment, segment.Format);
                        break;
                }
            }
        }
        return builder;
    }

    // StringBuilder's writable chunks are internal. Use the .NET 10 handler's
    // temporary-buffer path, backed by DefaultInterpolatedStringHandler.
    private static void AppendFormatted<T>(StringBuilder builder, IFormatProvider? provider, T value, int alignment, string? format)
    {
        var handler = new DefaultInterpolatedStringHandler(0, 0, provider, stackalloc char[256]);
        try
        {
            handler.AppendFormatted(value, format);
            var text = handler.Text;
            var leftAlign = alignment < 0;
            if (leftAlign)
            {
                alignment = -alignment;
            }
            var paddingRequired = alignment - text.Length;
            if (paddingRequired > 0 && !leftAlign)
            {
                builder.Append(' ', paddingRequired);
            }
            builder.Append(text);
            if (paddingRequired > 0 && leftAlign)
            {
                builder.Append(' ', paddingRequired);
            }
        }
        finally
        {
            handler.Clear();
        }
    }
}
#endif