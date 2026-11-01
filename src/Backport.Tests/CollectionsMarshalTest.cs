// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#if !NET5_0_OR_GREATER
using System;
using System.Collections.Generic;
using NUnit.Framework;

namespace Backport.Tests
{
    // NUnit port of dotnet/runtime v10.0.14 CollectionsMarshalTests.cs (AsSpan cases).
    public class CollectionsMarshalTest
    {
        [Test]
        public void NullListAsSpan()
        {
            Assert.That(System.Runtime.InteropServices.CollectionsMarshal.AsSpan<int>(null).Length, Is.Zero);
            Assert.That(System.Runtime.InteropServices.CollectionsMarshal.AsSpan<object>(null).Length, Is.Zero);
        }

        [Test]
        public void ListAsSpanValueType()
        {
            ValidateLists(i => i);
        }

        [Test]
        public void ListAsSpanClass()
        {
            ValidateLists(i => new IntAsObject { Value = i });
        }

        private static void ValidateLists<T>(Func<int, T> create)
        {
            var list = new List<T>();
            for (int length = 0; length < 36; length++)
            {
                list.Clear();
                ValidateContentEquality(list);
                for (int i = 0; i < length; i++) list.Add(create(i));
                ValidateContentEquality(list);
                list.TrimExcess();
                ValidateContentEquality(list);
                list.Add(create(length + 1));
                ValidateContentEquality(list);
            }
        }

        private static void ValidateContentEquality<T>(List<T> list)
        {
            var span = System.Runtime.InteropServices.CollectionsMarshal.AsSpan(list);
            Assert.That(span.Length, Is.EqualTo(list.Count));
            for (int i = 0; i < span.Length; i++) Assert.That(span[i], Is.EqualTo(list[i]));
        }

        [Test]
        public void ListAsSpanLinkBreaksOnResize()
        {
            var list = new List<int>(10);
            for (int i = 0; i < 10; i++) list.Add(i);
            list.TrimExcess();
            var span = System.Runtime.InteropServices.CollectionsMarshal.AsSpan(list);
            int startCapacity = list.Capacity;
            int startCount = list.Count;
            Assert.That(startCapacity, Is.EqualTo(startCount));
            Assert.That(span.Length, Is.EqualTo(startCount));
            for (int i = 0; i < span.Length; i++)
            {
                span[i]++;
                Assert.That(span[i], Is.EqualTo(list[i]));
                list[i]++;
                Assert.That(span[i], Is.EqualTo(list[i]));
            }
            list.Add(11);
            Assert.That(list.Capacity, Is.Not.EqualTo(startCapacity));
            Assert.That(list.Count, Is.Not.EqualTo(startCount));
            Assert.That(span.Length, Is.EqualTo(startCount));
            for (int i = 0; i < span.Length; i++)
            {
                span[i] += 2;
                Assert.That(span[i], Is.Not.EqualTo(list[i]));
                list[i] += 3;
                Assert.That(span[i], Is.Not.EqualTo(list[i]));
            }
        }

        private sealed class IntAsObject
        {
            public int Value;
        }
    }
}
#endif