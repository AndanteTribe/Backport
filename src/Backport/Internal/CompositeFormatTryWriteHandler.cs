// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#if !NET6_0_OR_GREATER
using System.Globalization;

namespace Backport.Internal;

// The subset of .NET 10 MemoryExtensions.TryWriteInterpolatedStringHandler used
// by CompositeFormat. Writes directly to the destination and never grows it.
internal ref struct CompositeFormatTryWriteHandler
{
    private readonly Span<char> _destination;
    private readonly IFormatProvider? _provider;
    private readonly bool _hasCustomFormatter;
    internal int _pos;
    internal bool _success;

    public CompositeFormatTryWriteHandler(int literalLength, int formattedCount, Span<char> destination, IFormatProvider? provider, out bool shouldAppend)
    {
        _destination = destination;
        _provider = provider;
        _pos = 0;
        _success = shouldAppend = destination.Length >= literalLength;
        _hasCustomFormatter = provider is not null && provider.GetType() != typeof(CultureInfo) && provider.GetFormat(typeof(ICustomFormatter)) is not null;
    }

    public bool AppendLiteral(string value)
    {
        if (value.AsSpan().TryCopyTo(_destination.Slice(_pos)))
        {
            _pos += value.Length;
            return true;
        }
        return Fail();
    }

    private bool AppendFormatted<T>(T value, string? format)
    {
        if (_hasCustomFormatter)
        {
            var formatter = (ICustomFormatter?)_provider!.GetFormat(typeof(ICustomFormatter));
            return formatter?.Format(format, value, _provider) is not string text || AppendLiteral(text);
        }
        if (value is null)
        {
            return true;
        }
        if (value is ISpanFormattable)
        {
            if (((ISpanFormattable)value).TryFormat(_destination.Slice(_pos), out var written, format, _provider))
            {
                _pos += written;
                return true;
            }
            return Fail();
        }
        // Older BCL types do not implement our backported ISpanFormattable.
        // Use the same adapters as DefaultInterpolatedStringHandler (including Unity).
        if (CustomSpanFormatter.GetFormatter<T>() is { } tryFormat)
        {
            if (tryFormat(value, _destination.Slice(_pos), out var written, format.AsSpan(), _provider))
            {
                _pos += written;
                return true;
            }
            return Fail();
        }
        var s = value is IFormattable ? ((IFormattable)value).ToString(format, _provider) : value.ToString();
        return s is null || AppendLiteral(s);
    }

    public bool AppendFormatted<T>(T value, int alignment, string? format)
    {
        var startingPos = _pos;
        if (!AppendFormatted(value, format))
        {
            return Fail();
        }
        if (alignment == 0)
        {
            return true;
        }
        var charsWritten = _pos - startingPos;
        var leftAlign = alignment < 0;
        if (leftAlign)
        {
            alignment = -alignment;
        }
        var paddingNeeded = alignment - charsWritten;
        if (paddingNeeded <= 0)
        {
            return true;
        }
        if (paddingNeeded <= _destination.Length - _pos)
        {
            if (leftAlign)
            {
                _destination.Slice(_pos, paddingNeeded).Fill(' ');
            }
            else
            {
                _destination.Slice(startingPos, charsWritten).CopyTo(_destination.Slice(startingPos + paddingNeeded));
                _destination.Slice(startingPos, paddingNeeded).Fill(' ');
            }
            _pos += paddingNeeded;
            return true;
        }
        return Fail();
    }

    private bool Fail()
    {
        _success = false;
        return false;
    }
}
#endif