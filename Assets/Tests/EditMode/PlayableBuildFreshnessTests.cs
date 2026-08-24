using System;
using System.IO;
using System.Reflection;
using NUnit.Framework;

namespace ThreeDoorsOfFate.Tests
{
    public sealed class PlayableBuildFreshnessTests
    {
        private const string BuilderTypeName =
            "ThreeDoorsOfFate.Editor.PlayableGameBuilder, Assembly-CSharp-Editor";

        [Test]
        public void StandaloneBuildPreparation_RemovesStaleFilesBeforeRebuild()
        {
            string testRoot = Path.Combine(
                Path.GetTempPath(),
                $"three-doors-build-freshness-{Guid.NewGuid():N}");
            string outputDirectory = Path.Combine(testRoot, "Windows");
            string outputPath = Path.Combine(outputDirectory, "ThreeDoorsOfFate.exe");
            string staleFile = Path.Combine(outputDirectory, "stale-build-marker.txt");

            try
            {
                Directory.CreateDirectory(outputDirectory);
                File.WriteAllText(staleFile, "old build");

                Type builderType = Type.GetType(BuilderTypeName);
                Assert.That(builderType, Is.Not.Null);
                MethodInfo prepare = builderType.GetMethod(
                    "PrepareCleanStandaloneBuildDirectory",
                    BindingFlags.NonPublic | BindingFlags.Static);
                Assert.That(prepare, Is.Not.Null, "Standalone builds need a clean-output guard.");

                prepare.Invoke(null, new object[] { outputPath });

                Assert.That(Directory.Exists(outputDirectory), Is.True);
                Assert.That(File.Exists(staleFile), Is.False);
            }
            finally
            {
                if (Directory.Exists(testRoot))
                {
                    Directory.Delete(testRoot, true);
                }
            }
        }
    }
}
