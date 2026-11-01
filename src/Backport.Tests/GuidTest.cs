// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Globalization;
using NUnit.Framework;
#if NET9_0_OR_GREATER
using GuidFactory = System.Guid;
#else
using GuidFactory = System.GuidExtensions;
#endif

namespace Backport.Tests
{
    public class GuidTest
    {
        // NUnit port of dotnet/runtime v10.0.14 GuidTests.CreateVersion7,
        // extended with timestamp boundaries and offsets.
        [TestCase(0L)]
        [TestCase(1L)]
        [TestCase(65535L)]
        [TestCase(65536L)]
        [TestCase(1731974400123L)]
        [TestCase(253402300799999L)]
        public void CreateVersion7_EncodesTimestampVersionAndVariant(long milliseconds)
        {
            var timestamp = DateTimeOffset.FromUnixTimeMilliseconds(milliseconds);
            var guid = GuidFactory.CreateVersion7(timestamp).ToString("D");
            var hex = milliseconds.ToString("x12", CultureInfo.InvariantCulture);
            Assert.That(guid.Substring(0, 8), Is.EqualTo(hex.Substring(0, 8)));
            Assert.That(guid.Substring(9, 4), Is.EqualTo(hex.Substring(8)));
            Assert.That(guid[14], Is.EqualTo('7'));
            Assert.That(guid[19], Is.EqualTo('8').Or.EqualTo('9').Or.EqualTo('a').Or.EqualTo('b'));
        }

        [Test]
        public void CreateVersion7_ThrowsForPreUnixEpoch()
        {
            foreach (var timestamp in new[] { DateTimeOffset.UnixEpoch.AddTicks(-1), DateTimeOffset.MinValue })
            {
                Assert.That(Assert.Throws<ArgumentOutOfRangeException>(() => GuidFactory.CreateVersion7(timestamp)).ParamName, Is.EqualTo("timestamp"));
            }
        }

        [Test]
        public void CreateVersion7_UsesUtcAndRandomBits()
        {
            var timestamp = DateTimeOffset.FromUnixTimeMilliseconds(1731974400123).ToOffset(TimeSpan.FromHours(9));
            var ids = new HashSet<Guid>();
            for (var i = 0; i < 100; i++)
            {
                var id = GuidFactory.CreateVersion7(timestamp);
                Assert.That(ids.Add(id), Is.True);
                Assert.That(id.ToString("N").Substring(0, 12), Is.EqualTo(timestamp.ToUnixTimeMilliseconds().ToString("x12", CultureInfo.InvariantCulture)));
            }
        }

        [Test]
        public void CreateVersion7_DefaultUsesCurrentTime()
        {
            var before = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
            var id = GuidFactory.CreateVersion7();
            var after = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
            var milliseconds = long.Parse(id.ToString("N").Substring(0, 12), NumberStyles.HexNumber, CultureInfo.InvariantCulture);
            Assert.That(milliseconds, Is.InRange(before, after));
        }

#if !UNITY_2021_3_OR_NEWER
        [Test]
        public void CreateVersion7_StaticExtensionSyntax()
        {
            Assert.That(Guid.CreateVersion7(DateTimeOffset.UnixEpoch).ToString("N").Substring(0, 12), Is.EqualTo("000000000000"));
            Assert.That(Guid.CreateVersion7(), Is.Not.EqualTo(Guid.Empty));
        }
#endif
    }
}