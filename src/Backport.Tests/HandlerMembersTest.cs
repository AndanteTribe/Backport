using System;
using System.Runtime.CompilerServices;
using NUnit.Framework;

namespace Backport.Tests
{
    public class HandlerMembersTest
    {
        [TestCase(false)]
        [TestCase(true)]
        public void TextAndClear_WorkWithRentedAndStackBuffers(bool useStackBuffer)
        {
            Span<char> buffer = stackalloc char[32];
            var handler = useStackBuffer
                ? new DefaultInterpolatedStringHandler(0, 1, null, buffer)
                : new DefaultInterpolatedStringHandler(0, 1);

            handler.AppendLiteral("hello");
            Assert.That(handler.Text.ToString(), Is.EqualTo("hello"));
            if (useStackBuffer)
            {
                buffer[0] = 'H';
                Assert.That(handler.Text[0], Is.EqualTo('H'));
            }

            handler.Clear();
            // Observe the cleared original, detecting an accidental by-value extension receiver.
            Assert.That(handler.Text.IsEmpty, Is.True);
        }

        [Test]
        public void Text_ReflectsBufferGrowth()
        {
            var handler = new DefaultInterpolatedStringHandler(0, 1, null, stackalloc char[1]);
            var expected = new string('x', 1024);
            handler.AppendLiteral(expected);
            Assert.That(handler.Text.ToString(), Is.EqualTo(expected));
            Assert.That(handler.ToStringAndClear(), Is.EqualTo(expected));
        }
    }
}