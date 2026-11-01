// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#if !NET8_0_OR_GREATER
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;

namespace System.Text;

/// <summary>Represents a parsed composite format string.</summary>
[DebuggerDisplay("{Format}")]
public sealed class CompositeFormat
{
    internal readonly (string? Literal, int ArgIndex, int Alignment, string? Format)[] _segments;
    internal readonly int _literalLength;
    internal readonly int _formattedCount;
    internal readonly int _argsRequired;

    private CompositeFormat(string format, (string? Literal, int ArgIndex, int Alignment, string? Format)[] segments)
    {
        Format = format;
        _segments = segments;
        int literalLength = 0, formattedCount = 0, argsRequired = 0;
        foreach (var segment in segments)
        {
            Debug.Assert((segment.Literal is not null) ^ (segment.ArgIndex >= 0));
            if (segment.Literal is not null)
            {
                literalLength += segment.Literal.Length;
            }
            else if (segment.ArgIndex >= 0)
            {
                formattedCount++;
                argsRequired = Math.Max(argsRequired, segment.ArgIndex + 1);
            }
        }
        _literalLength = literalLength;
        _formattedCount = formattedCount;
        _argsRequired = argsRequired;
    }

    /// <summary>Parses the composite format string.</summary>
    /// <param name="format">The string to parse.</param>
    /// <returns>The parsed format.</returns>
    /// <exception cref="ArgumentNullException">The format is null.</exception>
    /// <exception cref="FormatException">A format item is invalid.</exception>
    public static CompositeFormat Parse([StringSyntax(StringSyntaxAttribute.CompositeFormat)] string format)
    {
        if (format is null)
        {
            throw new ArgumentNullException(nameof(format));
        }
        var segments = new List<(string? Literal, int ArgIndex, int Alignment, string? Format)>();
        var failureOffset = 0;
        var failureReason = string.Empty;
        if (!TryParseLiterals(format, segments, ref failureOffset, ref failureReason))
        {
            throw new FormatException($"Input string was not in a correct format. Failure to parse near offset {failureOffset}. {failureReason}");
        }
        return new CompositeFormat(format, segments.ToArray());
    }

    /// <summary>Gets the original composite format string.</summary>
    public string Format { get; }

    /// <summary>Gets the minimum number of arguments required by this format.</summary>
    public int MinimumArgumentCount => _argsRequired;

    internal void ValidateNumberOfArgs(int numArgs)
    {
        if (numArgs < _argsRequired)
        {
            throw new FormatException("Index (zero based) must be greater than or equal to zero and less than the size of the argument list.");
        }
    }

    // Parsing logic from dotnet/runtime v10.0.14 CompositeFormat.TryParseLiterals.
    // DefaultInterpolatedStringHandler replaces CoreLib's internal ValueStringBuilder; ASCII digit checks
    // and exception resources are expressed using APIs available on netstandard2.1.
    private static bool TryParseLiterals(ReadOnlySpan<char> format, List<(string? Literal, int ArgIndex, int Alignment, string? Format)> segments, ref int failureOffset, ref string failureReason)
    {
        Span<char> initialBuffer = stackalloc char[256];
        var vsb = new DefaultInterpolatedStringHandler(0, 0, null, initialBuffer);
        try
        {
            var pos = 0;
            char ch;
            while (true)
            {
                while (true)
                {
                    var remainder = format.Slice(pos);
                    var countUntilNextBrace = remainder.IndexOfAny('{', '}');
                    if (countUntilNextBrace < 0)
                    {
                        vsb.AppendFormatted(remainder);
                        segments.Add((vsb.ToString(), -1, 0, null));
                        return true;
                    }
                    vsb.AppendFormatted(remainder.Slice(0, countUntilNextBrace));
                    pos += countUntilNextBrace;
                    var brace = format[pos];
                    if (!TryMoveNext(format, ref pos, out ch))
                    {
                        goto FailureUnclosedFormatItem;
                    }
                    if (brace == ch)
                    {
                        vsb.AppendFormatted(format.Slice(pos, 1));
                        pos++;
                        continue;
                    }
                    if (brace != '{')
                    {
                        goto FailureUnexpectedClosingBrace;
                    }
                    segments.Add((vsb.ToString(), -1, 0, null));
                    vsb.Clear();
                    vsb = new DefaultInterpolatedStringHandler(0, 0, null, initialBuffer);
                    break;
                }

                var width = 0;
                string? itemFormat = null;
                var index = ch - '0';
                if ((uint)index >= 10u)
                {
                    goto FailureExpectedAsciiDigit;
                }
                if (!TryMoveNext(format, ref pos, out ch))
                {
                    goto FailureUnclosedFormatItem;
                }
                if (ch != '}')
                {
                    while ((uint)(ch - '0') < 10u)
                    {
                        index = index * 10 + ch - '0';
                        if (!TryMoveNext(format, ref pos, out ch))
                        {
                            goto FailureUnclosedFormatItem;
                        }
                    }
                    while (ch == ' ')
                    {
                        if (!TryMoveNext(format, ref pos, out ch))
                        {
                            goto FailureUnclosedFormatItem;
                        }
                    }
                    if (ch == ',')
                    {
                        do
                        {
                            if (!TryMoveNext(format, ref pos, out ch))
                            {
                                goto FailureUnclosedFormatItem;
                            }
                        }
                        while (ch == ' ');
                        var leftJustify = 1;
                        if (ch == '-')
                        {
                            leftJustify = -1;
                            if (!TryMoveNext(format, ref pos, out ch))
                            {
                                goto FailureUnclosedFormatItem;
                            }
                        }
                        width = ch - '0';
                        if ((uint)width >= 10u)
                        {
                            goto FailureExpectedAsciiDigit;
                        }
                        if (!TryMoveNext(format, ref pos, out ch))
                        {
                            goto FailureUnclosedFormatItem;
                        }
                        while ((uint)(ch - '0') < 10u)
                        {
                            width = width * 10 + ch - '0';
                            if (!TryMoveNext(format, ref pos, out ch))
                            {
                                goto FailureUnclosedFormatItem;
                            }
                        }
                        width *= leftJustify;
                        while (ch == ' ')
                        {
                            if (!TryMoveNext(format, ref pos, out ch))
                            {
                                goto FailureUnclosedFormatItem;
                            }
                        }
                    }
                    if (ch != '}')
                    {
                        if (ch != ':')
                        {
                            goto FailureUnclosedFormatItem;
                        }
                        var startingPos = pos;
                        while (true)
                        {
                            if (!TryMoveNext(format, ref pos, out ch))
                            {
                                goto FailureUnclosedFormatItem;
                            }
                            if (ch == '}')
                            {
                                break;
                            }
                            if (ch == '{')
                            {
                                goto FailureUnclosedFormatItem;
                            }
                        }
                        startingPos++;
                        itemFormat = format.Slice(startingPos, pos - startingPos).ToString();
                    }
                }
                Debug.Assert(format[pos] == '}');
                pos++;
                segments.Add((null, index, width, itemFormat));
            }

        FailureUnexpectedClosingBrace:
            failureReason = "Unexpected closing brace without a corresponding opening brace.";
            failureOffset = pos;
            return false;
        FailureUnclosedFormatItem:
            failureReason = "Format item ends prematurely.";
            failureOffset = pos;
            return false;
        FailureExpectedAsciiDigit:
            failureReason = "Expected an ASCII digit.";
            failureOffset = pos;
            return false;

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            static bool TryMoveNext(ReadOnlySpan<char> format, ref int pos, out char nextChar)
            {
                pos++;
                if ((uint)pos >= (uint)format.Length)
                {
                    nextChar = '\0';
                    return false;
                }
                nextChar = format[pos];
                return true;
            }
        }
        finally
        {
            vsb.Clear();
        }
    }
}
#endif