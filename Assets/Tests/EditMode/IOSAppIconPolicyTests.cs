using System;
using System.IO;
using System.Linq;
using System.Reflection;
using NUnit.Framework;
using UnityEditor;
using UnityEditor.Build;
using UnityEngine;

namespace ThreeDoorsOfFate.Tests
{
    public sealed class IOSAppIconPolicyTests
    {
        private const string IconPath =
            "Assets/Art/Branding/AppIcon/three_doors_app_icon_1024.png";
        private const string BuilderTypeName =
            "ThreeDoorsOfFate.Editor.PlayableGameBuilder, Assembly-CSharp-Editor";

        [Test]
        public void BuilderConfiguresOpaqueSourceForEveryIOSAppIconSlot()
        {
            AssetDatabase.ImportAsset(
                IconPath,
                ImportAssetOptions.ForceSynchronousImport | ImportAssetOptions.ForceUpdate);

            Type builderType = Type.GetType(BuilderTypeName);
            Assert.That(builderType, Is.Not.Null, "PlayableGameBuilder must be available.");

            MethodInfo configure = builderType.GetMethod(
                "ConfigureIOSAppIcons",
                BindingFlags.NonPublic | BindingFlags.Static);
            Assert.That(configure, Is.Not.Null, "The iOS build must configure its app icon before export.");
            configure.Invoke(null, null);

            Texture2D icon = AssetDatabase.LoadAssetAtPath<Texture2D>(IconPath);
            Assert.That(icon, Is.Not.Null);
            Assert.That(icon.width, Is.EqualTo(1024));
            Assert.That(icon.height, Is.EqualTo(1024));

            TextureImporter importer = AssetImporter.GetAtPath(IconPath) as TextureImporter;
            Assert.That(importer, Is.Not.Null);
            Assert.That(importer.textureType, Is.EqualTo(TextureImporterType.Default));
            Assert.That(importer.alphaSource, Is.EqualTo(TextureImporterAlphaSource.None));
            Assert.That(importer.mipmapEnabled, Is.False);
            Assert.That(importer.sRGBTexture, Is.True);
            Assert.That(importer.isReadable, Is.False);
            Assert.That(importer.npotScale, Is.EqualTo(TextureImporterNPOTScale.None));
            Assert.That(importer.textureCompression, Is.EqualTo(TextureImporterCompression.Uncompressed));

            PlatformIconKind[] kinds = PlayerSettings.GetSupportedIconKinds(NamedBuildTarget.iOS);
            if (!BuildPipeline.IsBuildTargetSupported(BuildTargetGroup.iOS, BuildTarget.iOS))
            {
                Assert.That(kinds, Is.Empty);
                Assert.Ignore(
                    "iOS Build Support is not installed; importer checks passed, "
                    + "and slot checks remain enabled on an iOS-capable editor.");
            }

            Assert.That(kinds, Is.Not.Empty);
            foreach (PlatformIconKind kind in kinds)
            {
                PlatformIcon[] slots = PlayerSettings.GetPlatformIcons(NamedBuildTarget.iOS, kind);
                Assert.That(slots, Is.Not.Empty, $"iOS icon kind {kind} must expose at least one slot.");
                Assert.That(
                    slots.All(slot => slot.GetTexture(0) == icon),
                    Is.True,
                    $"Every iOS icon slot for {kind} must use the generated Three Doors icon.");
            }

            string projectRoot = Directory.GetParent(Application.dataPath)?.FullName;
            Assert.That(projectRoot, Is.Not.Null);
            byte[] pngBytes = File.ReadAllBytes(Path.Combine(projectRoot, IconPath));
            Texture2D decoded = new(2, 2, TextureFormat.RGBA32, false);
            try
            {
                Assert.That(ImageConversion.LoadImage(decoded, pngBytes, false), Is.True);
                Assert.That(decoded.GetPixels32().All(pixel => pixel.a == byte.MaxValue), Is.True);
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(decoded);
            }
        }
    }
}
