#if UNITY_EDITOR
using System.IO;
using UnityEditor;
using UnityEditor.Build.Player;
#endif
using NUnit.Framework;

namespace Backport.Tests
{
    public class CompileTest
    {
        [Test]
        public void CompileCheck()
        {
#if UNITY_EDITOR
            var buildTarget = EditorUserBuildSettings.activeBuildTarget;
            const string outputPath = "Temp/CompileTest";

            var settings = new ScriptCompilationSettings()
            {
                group = BuildPipeline.GetBuildTargetGroup(buildTarget),
                target = buildTarget,
            };
            try
            {
                var result = PlayerBuildInterface.CompilePlayerScripts(settings, outputPath);
                Assert.That(result.assemblies, Is.Not.Null);
                Assert.That(result.assemblies.Count, Is.GreaterThan(0));
                Assert.That(result.typeDB, Is.Not.Null);
            }
            finally
            {
                if (Directory.Exists(outputPath))
                {
                    Directory.Delete(outputPath, true);
                }
            }
#else
#pragma warning disable NUnit2007
            Assert.That(true, Is.True);
#pragma warning restore NUnit2007
#endif
        }
    }
}