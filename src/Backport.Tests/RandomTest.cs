// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#if !NET6_0_OR_GREATER
using System;
using System.Threading;
using System.Threading.Tasks;
using NUnit.Framework;

namespace Backport.Tests
{
    // NUnit port of dotnet/runtime v10.0.14 System.Runtime.Extensions.Tests/System/Random.cs.
    // NextInt64 and NextSingle are omitted because they are not supplied by this library.
    public class RandomTest
    {
        [Test]
        public void Shared_IsSingleton()
        {
            var shared = RandomExtensions.get_Shared();
            Assert.That(shared, Is.Not.Null);
            Assert.That(RandomExtensions.get_Shared(), Is.SameAs(shared));
            Assert.That(Task.Run(() => RandomExtensions.get_Shared()).Result, Is.SameAs(shared));
        }

        [Test]
        public void Shared_ParallelUsage()
        {
            using var barrier = new Barrier(2);
            Parallel.For(0, 2, _ =>
            {
                var buffer = new byte[1000];
                barrier.SignalAndWait();
                for (int i = 0; i < 1000; i++)
                {
                    var shared = RandomExtensions.get_Shared();
                    Assert.That(shared.Next(), Is.InRange(0, int.MaxValue - 1));
                    Assert.That(shared.Next(5), Is.InRange(0, 4));
                    Assert.That(shared.Next(42, 50), Is.InRange(42, 49));
                    Assert.That(shared.NextDouble(), Is.InRange(0.0, 1.0).And.LessThan(1.0));
                    Array.Clear(buffer, 0, buffer.Length);
                    shared.NextBytes(buffer);
                    Assert.That(Array.Exists(buffer, b => b != 0), Is.True);
                    Array.Clear(buffer, 0, buffer.Length);
                    shared.NextBytes(buffer.AsSpan());
                    Assert.That(Array.Exists(buffer, b => b != 0), Is.True);
                }
            });
        }
    }
}
#endif