#if !NET6_0_OR_GREATER
using System;
using System.Runtime.CompilerServices;
using System.Threading;
using NUnit.Framework;

namespace Backport.Tests
{
    public class CompatibilityTest
    {
        [Test]
        public void Handler_UsesRuntimeSpanFormattable()
        {
            Check<object>(new SpanValue());
            Check<IFormattable>(new SpanValue());
            Check<SpanValue?>(new SpanValue());
            Check<ISpanFormattable>(new SpanValue());
        }

        private static void Check<T>(T value)
        {
            var handler = new DefaultInterpolatedStringHandler(0, 1, null, Span<char>.Empty);
            handler.AppendFormatted(value);
            handler.AppendFormatted(value, "G");
            Assert.That(handler.Text.ToString(), Is.EqualTo("okok"));
            handler.Clear();
            Assert.That(handler.Text.IsEmpty, Is.True);
        }

        private struct SpanValue : ISpanFormattable
        {
            public string ToString(string format, IFormatProvider provider) => throw new InvalidOperationException("Unexpected string fallback");

            public bool TryFormat(Span<char> destination, out int charsWritten, ReadOnlySpan<char> format, IFormatProvider provider)
            {
                charsWritten = 0;
                if (!"ok".AsSpan().TryCopyTo(destination))
                    return false;
                charsWritten = 2;
                return true;
            }
        }

#if !NET5_0_OR_GREATER
        [TestCase(false)]
        [TestCase(true)]
        public void UnsafeRegister_PassesState(bool alreadyCanceled)
        {
            using var source = new CancellationTokenSource();
            if (alreadyCanceled)
                source.Cancel();
            var state = new object();
            int calls = 0;
            using var registration = source.Token.UnsafeRegister(actualState =>
            {
                Assert.That(actualState, Is.SameAs(state));
                Assert.That(source.Token.IsCancellationRequested, Is.True);
                calls++;
            }, state);
            if (!alreadyCanceled)
                source.Cancel();
            Assert.That(calls, Is.EqualTo(1));
        }

        [Test]
        public void UnsafeRegister_ValidatesCallbackAndSupportsDisposal()
        {
            Assert.Throws<ArgumentNullException>(() => CancellationToken.None.UnsafeRegister((Action<object>)null, null));
            using var source = new CancellationTokenSource();
            bool called = false;
            var registration = source.Token.UnsafeRegister(_ => called = true, null);
            registration.Dispose();
            source.Cancel();
            Assert.That(called, Is.False);
        }

#endif

        [Test]
        public void SharedRandom_ValidatesArguments()
        {
            var random = RandomExtensions.get_Shared();
            Assert.That(RandomExtensions.get_Shared(), Is.SameAs(random));
            Assert.That(random.Next(0), Is.Zero);
            Assert.That(random.Next(42, 42), Is.EqualTo(42));
            Assert.Throws<ArgumentOutOfRangeException>(() => random.Next(-1));
            Assert.Throws<ArgumentOutOfRangeException>(() => random.Next(2, 1));
            Assert.Throws<ArgumentNullException>(() => random.NextBytes((byte[])null));
            random.NextBytes(Span<byte>.Empty);
        }

#if !NET5_0_OR_GREATER
        [Test]
        public void CollectionsMarshal_AsSpanRejectsInvalidSize()
        {
            var list = new System.Collections.Generic.List<int> { 1 };
            var size = list.GetType().GetField("_size", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic);
            try
            {
                size.SetValue(list, list.Capacity + 1);
                Assert.Throws<InvalidOperationException>(() => System.Runtime.InteropServices.CollectionsMarshal.AsSpan(list));
            }
            finally
            {
                size.SetValue(list, 1);
            }
        }

        [Test]
        public void IsExternalInit_IsStaticAndHidden()
        {
            var type = typeof(IsExternalInit);
            Assert.That(type.IsAbstract && type.IsSealed, Is.True);
            var attribute = (System.ComponentModel.EditorBrowsableAttribute)Attribute.GetCustomAttribute(type, typeof(System.ComponentModel.EditorBrowsableAttribute));
            Assert.That(attribute.State, Is.EqualTo(System.ComponentModel.EditorBrowsableState.Never));
        }
#endif
    }
}
#endif