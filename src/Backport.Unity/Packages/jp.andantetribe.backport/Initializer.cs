#if !NET6_0_OR_GREATER
#nullable enable

using Backport.Internal;
using UnityEngine;

[assembly: UnityEngine.Scripting.AlwaysLinkAssembly]

namespace Backport.Unity
{
#if UNITY_EDITOR
    [UnityEditor.InitializeOnLoad]
#endif
    internal static class Initializer
    {
        static Initializer()
        {
            CustomSpanFormatter.OtherFormatterHelper = new UnityCustomSpanFormatter();
        }
    }
}

#endif