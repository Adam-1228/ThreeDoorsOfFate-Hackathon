using System.IO;
using NUnit.Framework;

namespace ThreeDoorsOfFate.Tests
{
    public sealed class WebGlBuildPolicyTests
    {
        private const string BuilderPath = "Assets/Editor/PlayableGameBuilder.cs";

        [Test]
        public void PlayableBuilderDefinesStaticHostWebGlTarget()
        {
            string source = File.ReadAllText(BuilderPath);
            StringAssert.Contains("public static void BuildWebGLPlayable()", source);
            StringAssert.Contains("BuildTarget.WebGL", source);
            StringAssert.Contains("../Builds/WebGL", source);
            StringAssert.Contains("WebGL.decompressionFallback = true", source);
        }
    }
}
