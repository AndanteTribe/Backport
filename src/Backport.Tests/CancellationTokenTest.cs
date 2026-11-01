// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#if !NET5_0_OR_GREATER
using System;
using System.Threading;
using NUnit.Framework;

namespace Backport.Tests
{
    public class CancellationTokenTest
    {
        // NUnit port of dotnet/runtime v10.0.14 CancellationTokenTests.cs.
        [Test]
        public void Register_ExecutionContextFlowsIfExpected()
        {
            using var cts = new CancellationTokenSource();
            const int iterations = 5;
            int invoked = 0;
            var local = new AsyncLocal<int>();
            for (int i = 1; i <= iterations; i++)
            {
                bool flowExecutionContext = i % 2 == 0;
                local.Value = i;
                Action<object> callback = state =>
                {
                    invoked++;
                    Assert.That(local.Value, Is.EqualTo(flowExecutionContext ? (int)state : 0));
                };

                var token = cts.Token;
                if (flowExecutionContext)
                {
                    token.Register(callback, i);
                }
                else
                {
                    token.UnsafeRegister(callback, i);
                }
            }
            local.Value = 0;
            cts.Cancel();
            Assert.That(invoked, Is.EqualTo(iterations));
        }
    }
}
#endif