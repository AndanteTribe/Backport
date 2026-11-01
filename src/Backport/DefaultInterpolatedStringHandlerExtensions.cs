#if NET6_0_OR_GREATER && !NET10_0_OR_GREATER

using System.Diagnostics.CodeAnalysis;
using System.Reflection;

namespace System.Runtime.CompilerServices;

/// <summary>Provides handler members introduced in .NET 10 on .NET 6 through .NET 9.</summary>
public static class DefaultInterpolatedStringHandlerExtensions
{
    /// <summary>Clears the handler and returns its rented buffer to the pool.</summary>
    /// <remarks>Call only as the final operation on the handler. Copies must not be used afterwards.</remarks>
    /// <param name="handler">The handler to clear.</param>
    public static void Clear(this ref DefaultInterpolatedStringHandler handler) => ClearCore(ref handler);

    /// <summary>Extensions for the interpolated string handler.</summary>
    extension(ref DefaultInterpolatedStringHandler handler)
    {
        /// <summary>Gets a view of the characters written to the handler.</summary>
        /// <remarks>The view must not be used after the buffer is returned to the pool or replaced.</remarks>
        public ReadOnlySpan<char> Text => GetText(ref handler);
    }

#if NET8_0_OR_GREATER
    [UnsafeAccessor(UnsafeAccessorKind.Method, Name = "get_Text")]
    private static extern ReadOnlySpan<char> GetText(ref DefaultInterpolatedStringHandler handler);

    [UnsafeAccessor(UnsafeAccessorKind.Method, Name = "Clear")]
    private static extern void ClearCore(ref DefaultInterpolatedStringHandler handler);
#else
    private delegate ReadOnlySpan<char> TextGetter(ref DefaultInterpolatedStringHandler handler);
    private delegate void ClearAction(ref DefaultInterpolatedStringHandler handler);

    private static readonly TextGetter s_getText;
    private static readonly ClearAction s_clear;

    // These members belong to CoreLib, so preserve them explicitly for trimmed applications.
    [DynamicDependency("get_Text", typeof(DefaultInterpolatedStringHandler))]
    [DynamicDependency("Clear", typeof(DefaultInterpolatedStringHandler))]
    static DefaultInterpolatedStringHandlerExtensions()
    {
        const BindingFlags flags = BindingFlags.Instance | BindingFlags.NonPublic;
        s_getText = (TextGetter)typeof(DefaultInterpolatedStringHandler)
            .GetMethod("get_Text", flags)!.CreateDelegate(typeof(TextGetter));
        s_clear = (ClearAction)typeof(DefaultInterpolatedStringHandler)
            .GetMethod("Clear", flags)!.CreateDelegate(typeof(ClearAction));
    }

    private static ReadOnlySpan<char> GetText(ref DefaultInterpolatedStringHandler handler) => s_getText(ref handler);
    private static void ClearCore(ref DefaultInterpolatedStringHandler handler) => s_clear(ref handler);
#endif
}

#endif