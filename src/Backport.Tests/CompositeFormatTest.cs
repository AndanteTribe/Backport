// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Globalization;
using System.Linq;
using System.Text;
using NUnit.Framework;

namespace Backport.Tests
{
    // NUnit port of parsing and MemoryExtensions.TryWrite cases from
    // dotnet/runtime v10.0.14 System/Text/CompositeFormatTests.cs.
    public class CompositeFormatTest
    {
        // Keep formatting expectations explicit rather than deriving them from string.Format.
        // Unity/Mono and modern .NET interpret adjacent escaped braces differently:
        // string.Format("{{{0:D4}}}", 123) produces "{D4}" on Unity/Mono, but "{0123}" on .NET 10.
        // These tests verify .NET 10 CompositeFormat behavior on every runtime.

        [TestCase("", 0)]
        [TestCase("testing 123", 0)]
        [TestCase("testing {{123}}", 0)]
        [TestCase("{0}", 1)]
        [TestCase("{0} {1}", 2)]
        [TestCase("{2}", 3)]
        [TestCase("{2} {0}", 3)]
        [TestCase("{1} {34} {3}", 35)]
        public void MinimumArgumentCount_MatchesExpectedValue(string format, int expected)
        {
            var cf = CompositeFormat.Parse(format);
            Assert.That(cf.Format, Is.SameAs(format));
            Assert.That(cf.MinimumArgumentCount, Is.EqualTo(expected));
            var args = Enumerable.Repeat((object)"arg", expected).ToArray();
            Assert.That(new char[256].AsSpan().TryWrite(null, cf, out _, args), Is.True);
            Assert.That(new char[256].AsSpan().TryWrite(null, cf, out _, (ReadOnlySpan<object>)args), Is.True);
            if (expected != 0)
            {
                args = args.Take(expected - 1).ToArray();
                Assert.Throws<FormatException>(() => new char[256].AsSpan().TryWrite(null, cf, out _, args));
                Assert.Throws<FormatException>(() => new char[256].AsSpan().TryWrite(null, cf, out _, (ReadOnlySpan<object>)args));
            }
        }

        [Test]
        public void NullArgument_Throws()
        {
            Assert.That(Assert.Throws<ArgumentNullException>(() => CompositeFormat.Parse(null)).ParamName, Is.EqualTo("format"));
            Assert.That(Assert.Throws<ArgumentNullException>(() => Span<char>.Empty.TryWrite(null, (CompositeFormat)null, out _, 0)).ParamName, Is.EqualTo("format"));
            Assert.Throws<ArgumentNullException>(() => Span<char>.Empty.TryWrite(null, (CompositeFormat)null, out _, 0, 0));
            Assert.Throws<ArgumentNullException>(() => Span<char>.Empty.TryWrite(null, (CompositeFormat)null, out _, 0, 0, 0));
            Assert.Throws<ArgumentNullException>(() => Span<char>.Empty.TryWrite(null, (CompositeFormat)null, out _, Array.Empty<object>()));
            Assert.Throws<ArgumentNullException>(() => Span<char>.Empty.TryWrite(null, (CompositeFormat)null, out _, ReadOnlySpan<object>.Empty));
            Assert.That(Assert.Throws<ArgumentNullException>(() => Span<char>.Empty.TryWrite(null, CompositeFormat.Parse("abc"), out _, (object[])null)).ParamName, Is.EqualTo("args"));
        }

        [TestCase("{")]
        [TestCase("}")]
        [TestCase("{a}")]
        [TestCase("{-1}")]
        [TestCase("{ 0}")]
        [TestCase("{0")]
        [TestCase("{0,}")]
        [TestCase("{0,+1}")]
        [TestCase("{0,-}")]
        [TestCase("{0:abc{def}")]
        [TestCase("{0:abc")]
        [TestCase("{\u0660}")]
        [TestCase("{0\t}")]
        public void Parse_InvalidFormat_Throws(string format)
        {
            Assert.Throws<FormatException>(() => CompositeFormat.Parse(format));
        }

        [TestCase("{0}", "123")]
        [TestCase("{{{0:D4}}}", "{0123}")]
        [TestCase("{0, 8:N2} {1,-8} {2:X} {3}", "  123,00 abc      2A ")]
        [TestCase("{3} {2} {1} {0} {0}", " 42 abc 123 123")]
        [TestCase("{0   , -8 :D4}", "0123    ")]
        [TestCase("{{literal}}", "{literal}")]
        [TestCase("", "")]
        public void TryWrite_ArrayAndSpan(string format, string expected)
        {
            var cf = CompositeFormat.Parse(format);
            var args = new object[] { 123, "abc", 42, null };
            var provider = CultureInfo.GetCultureInfo("fr-FR");
            var buffer = new char[expected.Length];
            Assert.That(buffer.AsSpan().TryWrite(provider, cf, out var written, args), Is.True);
            Assert.That(written, Is.EqualTo(expected.Length));
            Assert.That(new string(buffer), Is.EqualTo(expected));
            Array.Clear(buffer, 0, buffer.Length);
            Assert.That(buffer.AsSpan().TryWrite(provider, cf, out written, (ReadOnlySpan<object>)args), Is.True);
            Assert.That(written, Is.EqualTo(expected.Length));
            Assert.That(new string(buffer), Is.EqualTo(expected));
            if (expected.Length > 0)
            {
                buffer = new char[expected.Length - 1];
                Assert.That(buffer.AsSpan().TryWrite(provider, cf, out written, args), Is.False);
                Assert.That(written, Is.Zero);
                Assert.That(buffer.AsSpan().TryWrite(provider, cf, out written, (ReadOnlySpan<object>)args), Is.False);
                Assert.That(written, Is.Zero);
            }
        }

        [Test]
        public void TryWrite_GenericOverloadsAndArgumentValidation()
        {
            Span<char> buffer = stackalloc char[32];
            Assert.That(buffer.TryWrite(null, CompositeFormat.Parse("{0:D4}"), out var written, 42), Is.True);
            Assert.That(buffer.Slice(0, written).ToString(), Is.EqualTo("0042"));
            Assert.That(buffer.TryWrite(null, CompositeFormat.Parse("{1}/{0}"), out written, 42, "abc"), Is.True);
            Assert.That(buffer.Slice(0, written).ToString(), Is.EqualTo("abc/42"));
            Assert.That(buffer.TryWrite(null, CompositeFormat.Parse("{2}/{1}/{0}"), out written, 42, "abc", 7), Is.True);
            Assert.That(buffer.Slice(0, written).ToString(), Is.EqualTo("7/abc/42"));
            Assert.Throws<FormatException>(() => Span<char>.Empty.TryWrite(null, CompositeFormat.Parse("{1}"), out _, 0));
            Assert.Throws<FormatException>(() => Span<char>.Empty.TryWrite(null, CompositeFormat.Parse("{2}"), out _, 0, 0));
            Assert.Throws<FormatException>(() => Span<char>.Empty.TryWrite(null, CompositeFormat.Parse("{3}"), out _, 0, 0, 0));
        }

        [TestCase("{0,5:xyz}", "  xyz")]
        [TestCase("{0,-5:xyz}", "xyz  ")]
        [TestCase("{0,5}", "     ")]
        public void TryWrite_UsesCustomFormatter(string format, string expected)
        {
            Span<char> buffer = stackalloc char[5];
            Assert.That(buffer.TryWrite(new CustomFormatter(), CompositeFormat.Parse(format), out var written, 42), Is.True);
            Assert.That(buffer.Slice(0, written).ToString(), Is.EqualTo(expected));
        }

        [Test]
        public void TryWrite_StopsOnFailureAndSkipsFormattingWhenLiteralsDoNotFit()
        {
            var value = new CountingValue();
            Assert.That(Span<char>.Empty.TryWrite(null, CompositeFormat.Parse("literal{0}"), out var written, value), Is.False);
            Assert.That(written, Is.Zero);
            Assert.That(value.Calls, Is.Zero);
            var buffer = new char[1];
            Assert.That(buffer.AsSpan().TryWrite(null, CompositeFormat.Parse("{0}{1}"), out written, "too long", value), Is.False);
            Assert.That(written, Is.Zero);
            Assert.That(value.Calls, Is.Zero);
        }

        [Test]
        public void TryWrite_UsesSpanFormattableWithoutToString()
        {
            Span<char> buffer = stackalloc char[4];
            Assert.That(buffer.TryWrite(null, CompositeFormat.Parse("{0,4}"), out var written, new SpanValue()), Is.True);
            Assert.That(buffer.Slice(0, written).ToString(), Is.EqualTo("  ok"));
            Assert.That(buffer.Slice(0, 1).TryWrite(null, CompositeFormat.Parse("{0}"), out written, new SpanValue()), Is.False);
            Assert.That(written, Is.Zero);
        }

        [Test]
        public void Parse_LargeLiteralsAndRepeatedHoles()
        {
            var literal = new string('x', 1024);
            var format = literal + "{{{0}}}" + literal + "{0}" + literal;
            var cf = CompositeFormat.Parse(format);
            var expected = literal + "{42}" + literal + "42" + literal;
            var buffer = new char[expected.Length];
            Assert.That(buffer.AsSpan().TryWrite(null, cf, out var written, 42), Is.True);
            Assert.That(written, Is.EqualTo(expected.Length));
            Assert.That(new string(buffer), Is.EqualTo(expected));
            Assert.Throws<FormatException>(() => CompositeFormat.Parse(format + "{"));
        }

        [TestCase("{0}", "123")]
        [TestCase("{{{0:D4}}}", "{0123}")]
        [TestCase("{0, 8:N2} {1,-8} {2:X} {3}", "  123,00 abc      2A ")]
        [TestCase("{3} {2} {1} {0} {0}", " 42 abc 123 123")]
        [TestCase("{{literal}}", "{literal}")]
        [TestCase("", "")]
        public void AppendFormat_ArrayAndSpan(string format, string expected)
        {
            // Port of StringBuilderAppendFormat_Valid in CompositeFormatTests.cs.
            var cf = CompositeFormat.Parse(format);
            var args = new object[] { 123, "abc", 42, null };
            var provider = CultureInfo.GetCultureInfo("fr-FR");
            var builder = new StringBuilder("prefix:", 8);
            Assert.That(builder.AppendFormat(provider, cf, args), Is.SameAs(builder));
            Assert.That(builder.ToString(), Is.EqualTo("prefix:" + expected));
            Assert.That(builder.AppendFormat(provider, cf, (ReadOnlySpan<object>)args), Is.SameAs(builder));
            Assert.That(builder.ToString(), Is.EqualTo("prefix:" + expected + expected));
        }

        [Test]
        public void AppendFormat_GenericOverloads()
        {
            var builder = new StringBuilder();
            Assert.That(builder.AppendFormat<int>(null, CompositeFormat.Parse("{0:D4}"), 42), Is.SameAs(builder));
            builder.AppendFormat<int, string>(null, CompositeFormat.Parse("/{1}/{0}"), 42, "abc");
            builder.AppendFormat<int, string, int>(null, CompositeFormat.Parse("/{2}/{1}/{0}"), 42, "abc", 7);
            Assert.That(builder.ToString(), Is.EqualTo("0042/abc/42/7/abc/42"));
            builder.Clear();
            IFormatProvider provider = null;
            builder.AppendFormat(provider, CompositeFormat.Parse("{0:D4}"), 42);
            Assert.That(builder.ToString(), Is.EqualTo("0042"));
        }

        [Test]
        public void AppendFormat_ValidatesArgumentsBeforeAppending()
        {
            IFormatProvider provider = null;
            var builder = new StringBuilder("prefix");
            Assert.That(Assert.Throws<ArgumentNullException>(() => builder.AppendFormat(provider, (CompositeFormat)null, 0)).ParamName, Is.EqualTo("format"));
            Assert.Throws<ArgumentNullException>(() => builder.AppendFormat(provider, (CompositeFormat)null, 0, 0));
            Assert.Throws<ArgumentNullException>(() => builder.AppendFormat(provider, (CompositeFormat)null, 0, 0, 0));
            Assert.Throws<ArgumentNullException>(() => builder.AppendFormat(provider, (CompositeFormat)null, Array.Empty<object>()));
            Assert.Throws<ArgumentNullException>(() => builder.AppendFormat(provider, (CompositeFormat)null, ReadOnlySpan<object>.Empty));
            Assert.That(Assert.Throws<ArgumentNullException>(() => builder.AppendFormat(provider, CompositeFormat.Parse("abc"), (object[])null)).ParamName, Is.EqualTo("args"));
            Assert.Throws<FormatException>(() => builder.AppendFormat(provider, CompositeFormat.Parse("literal{1}"), 0));
            Assert.Throws<FormatException>(() => builder.AppendFormat(provider, CompositeFormat.Parse("literal{2}"), 0, 0));
            Assert.Throws<FormatException>(() => builder.AppendFormat(provider, CompositeFormat.Parse("literal{3}"), 0, 0, 0));
            Assert.Throws<FormatException>(() => builder.AppendFormat(provider, CompositeFormat.Parse("literal{0}"), Array.Empty<object>()));
            Assert.Throws<FormatException>(() => builder.AppendFormat(provider, CompositeFormat.Parse("literal{0}"), ReadOnlySpan<object>.Empty));
            Assert.That(builder.ToString(), Is.EqualTo("prefix"));
        }

        [Test]
        public void AppendFormat_FormatsIntoGrowingBuilder()
        {
            var builder = new StringBuilder(1);
            builder.AppendFormat<SpanValue>(null, CompositeFormat.Parse("{0,4}/{0,-4}"), new SpanValue());
            Assert.That(builder.ToString(), Is.EqualTo("  ok/ok  "));
            var large = new string('x', 1024);
            builder.Clear();
            builder.AppendFormat<string>(null, CompositeFormat.Parse("{0,1030}"), large);
            Assert.That(builder.ToString(), Is.EqualTo(new string(' ', 6) + large));
            builder.Clear();
            builder.AppendFormat(new CustomFormatter(), CompositeFormat.Parse("{0,5:xyz}/{0,-5:xyz}"), 42);
            Assert.That(builder.ToString(), Is.EqualTo("  xyz/xyz  "));
        }

        private sealed class CustomFormatter : IFormatProvider, ICustomFormatter
        {
            public object GetFormat(Type formatType) => formatType == typeof(ICustomFormatter) ? this : null;
            public string Format(string format, object arg, IFormatProvider provider) => format;
        }

        private sealed class CountingValue
        {
            public int Calls { get; private set; }
            public override string ToString()
            {
                Calls++;
                return "value";
            }
        }

        private struct SpanValue : ISpanFormattable
        {
            public string ToString(string format, IFormatProvider provider) => throw new InvalidOperationException();
            public bool TryFormat(Span<char> destination, out int charsWritten, ReadOnlySpan<char> format, IFormatProvider provider)
            {
                if ("ok".AsSpan().TryCopyTo(destination))
                {
                    charsWritten = 2;
                    return true;
                }
                charsWritten = 0;
                return false;
            }
        }
    }
}