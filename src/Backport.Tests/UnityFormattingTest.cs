#if UNITY_2021_3_OR_NEWER && !NET6_0_OR_GREATER
using System;
using System.Runtime.CompilerServices;
using Backport.Internal;
using NUnit.Framework;
using UnityEngine;

namespace Backport.Tests
{
    public class UnityFormattingTest
    {
        [TestCase(null)]
        [TestCase("")]
        [TestCase("D4")]
        [TestCase("X")]
        public void IntegerVectors_MatchUnityFormatting(string format)
        {
            Assert.That(CustomSpanFormatter.OtherFormatterHelper, Is.Not.Null);
            var vector2 = new Vector2Int(1, -2);
            var vector3 = new Vector3Int(1, -2, 3);
            var bounds = new BoundsInt(1, 2, 3, 4, 5, 6);
            Check(vector2, format, vector2.ToString(format));
            Check(vector3, format, vector3.ToString(format));
            Check(bounds, format, bounds.ToString(format));
        }

        private static void Check<T>(T value, string format, string expected)
        {
            // Ensure this exercises the Unity formatter rather than the ToString fallback.
            Assert.That(CustomSpanFormatter.GetFormatter<T>(), Is.Not.Null);
            var handler = new DefaultInterpolatedStringHandler(0, 1);
            handler.AppendFormatted(value, format);
            Assert.That(handler.ToStringAndClear(), Is.EqualTo(expected));
        }
    }
}
#endif