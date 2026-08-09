using System;
using System.Collections.Generic;
using System.IO;
using Game.Editor;
using NUnit.Framework;
using UnityEditor;
using UnityEditor.AddressableAssets;
using UnityEngine;

namespace Game.Tests.EditMode
{
    public sealed class QinglanG31ArtUi001Tests
    {
        private const string AssetRoot = "Assets/GameAssets/AI/QinglanDemo/ART-UI-001";

        private static readonly AssetSpec[] Assets =
        {
            new AssetSpec(
                "title-key-art",
                "qinglan/ui/title/key-art",
                0.04f, 0.05f, 0.55f, 0.64f),
            new AssetSpec(
                "logo-safe-background",
                "qinglan/ui/title/logo-safe-background",
                0.20f, 0.06f, 0.80f, 0.62f)
        };

        [Test]
        public void TwoTitleBackgroundsUseCenteredBudgetedImportsAndReleaseEntries()
        {
            for (var index = 0; index < Assets.Length; index++)
            {
                var spec = Assets[index];
                var path = FinalPath(spec.Name);
                var texture = AssetDatabase.LoadAssetAtPath<Texture2D>(path);
                Assert.That(texture, Is.Not.Null, path);
                Assert.That(texture.width, Is.EqualTo(2048));
                Assert.That(texture.height, Is.EqualTo(1152));

                var importer = AssetImporter.GetAtPath(path) as TextureImporter;
                Assert.That(importer, Is.Not.Null);
                Assert.That(importer.textureType, Is.EqualTo(TextureImporterType.Sprite));
                Assert.That(importer.spriteImportMode, Is.EqualTo(SpriteImportMode.Single));
                Assert.That(importer.mipmapEnabled, Is.False);
                Assert.That(importer.wrapMode, Is.EqualTo(TextureWrapMode.Clamp));
                Assert.That(importer.maxTextureSize, Is.EqualTo(2048));
                var standalone = importer.GetPlatformTextureSettings("Standalone");
                Assert.That(standalone.overridden, Is.True);
                Assert.That(standalone.maxTextureSize, Is.EqualTo(2048));
                Assert.That(standalone.format, Is.EqualTo(TextureImporterFormat.BC7));

                var sprite = AssetDatabase.LoadAssetAtPath<Sprite>(path);
                Assert.That(sprite, Is.Not.Null, path);
                Assert.That(sprite.rect.width, Is.EqualTo(2048));
                Assert.That(sprite.rect.height, Is.EqualTo(1152));
                Assert.That(sprite.pivot.x, Is.EqualTo(1024f).Within(0.01f));
                Assert.That(sprite.pivot.y, Is.EqualTo(576f).Within(0.01f));
                AssertReleaseEntry(path, spec.Address);
            }
        }

        [Test]
        public void SourcesMastersAndFinalsMeetFramingContrastSafeAreaAndDistinctnessBudgets()
        {
            var projectRoot = Directory.GetParent(UnityEngine.Application.dataPath)?.FullName ?? string.Empty;
            var colorHashes = new HashSet<ulong>();
            var grayscaleHashes = new HashSet<ulong>();
            for (var index = 0; index < Assets.Length; index++)
            {
                var spec = Assets[index];
                AssertDimensions(projectRoot, SourcePath(spec.Name), 1672, 941);
                AssertDimensions(projectRoot, MasterPath(spec.Name), 4096, 2304);

                var final = LoadPng(Path.Combine(projectRoot, FinalPath(spec.Name)));
                try
                {
                    Assert.That(final.width, Is.EqualTo(2560));
                    Assert.That(final.height, Is.EqualTo(1440));
                    var pixels = final.GetPixels32();
                    Assert.That(CountNonOpaque(pixels), Is.Zero);
                    Assert.That(CountMagenta(pixels), Is.Zero);
                    Assert.That(CountDangerRed(pixels), Is.Zero);
                    var metrics = AnalyzeLuma(pixels);
                    Assert.That(metrics.Minimum, Is.LessThanOrEqualTo(48));
                    Assert.That(metrics.Maximum, Is.GreaterThanOrEqualTo(224));
                    Assert.That(metrics.Mean, Is.InRange(65f, 225f));
                    Assert.That(metrics.StandardDeviation, Is.GreaterThanOrEqualTo(32f));

                    var safe = AnalyzeSafeRegion(pixels, final.width, final.height, spec);
                    Assert.That(safe.Mean, Is.GreaterThanOrEqualTo(215f));
                    Assert.That(safe.StandardDeviation, Is.LessThanOrEqualTo(20f));
                    Assert.That(safe.EdgeRatio, Is.LessThanOrEqualTo(0.03f));
                    colorHashes.Add(HashPixels(pixels));
                    grayscaleHashes.Add(HashGrayscale(pixels));
                }
                finally
                {
                    UnityEngine.Object.DestroyImmediate(final);
                }
            }

            Assert.That(colorHashes.Count, Is.EqualTo(Assets.Length));
            Assert.That(grayscaleHashes.Count, Is.EqualTo(Assets.Length),
                "Title presentation assets must remain distinct after hue information is discarded.");
        }

        [Test]
        public void OnlyTwoApprovedFinalTitleBackgroundsAreAddressable()
        {
            var settings = AddressableAssetSettingsDefaultObject.GetSettings(false);
            Assert.That(settings, Is.Not.Null);
            var count = 0;
            var guids = AssetDatabase.FindAssets(string.Empty, new[] { AssetRoot });
            for (var index = 0; index < guids.Length; index++)
            {
                var entry = settings.FindAssetEntry(guids[index]);
                if (entry == null) continue;
                var path = AssetDatabase.GUIDToAssetPath(guids[index]).Replace('\\', '/');
                Assert.That(path, Does.Contain("/final/"));
                Assert.That(path, Does.EndWith(".png"));
                count++;
            }

            Assert.That(count, Is.EqualTo(2));
        }

        private static SafeMetrics AnalyzeSafeRegion(
            Color32[] pixels,
            int width,
            int height,
            AssetSpec spec)
        {
            var left = Mathf.RoundToInt(spec.Left * width);
            var top = Mathf.RoundToInt(spec.Top * height);
            var right = Mathf.RoundToInt(spec.Right * width);
            var bottom = Mathf.RoundToInt(spec.Bottom * height);
            double sum = 0;
            double squareSum = 0;
            var edgeCount = 0;
            var sampleCount = 0;
            for (var yTop = top; yTop < bottom; yTop++)
            {
                var y = height - 1 - yTop;
                for (var x = left; x < right; x++)
                {
                    var pixelIndex = y * width + x;
                    var luma = Luma(pixels[pixelIndex]);
                    sum += luma;
                    squareSum += luma * luma;
                    if (x > left && yTop > top)
                    {
                        var horizontal = Math.Abs(luma - Luma(pixels[pixelIndex - 1]));
                        var vertical = Math.Abs(luma - Luma(pixels[pixelIndex + width]));
                        if (Math.Max(horizontal, vertical) >= 36) edgeCount++;
                    }
                    sampleCount++;
                }
            }

            var mean = sum / sampleCount;
            var variance = squareSum / sampleCount - mean * mean;
            return new SafeMetrics(
                (float)mean,
                (float)Math.Sqrt(Math.Max(0, variance)),
                (float)edgeCount / sampleCount);
        }

        private static int Luma(Color32 color)
        {
            return (color.r * 54 + color.g * 183 + color.b * 19) >> 8;
        }

        private static void AssertDimensions(
            string projectRoot,
            string relativePath,
            int expectedWidth,
            int expectedHeight)
        {
            var texture = LoadPng(Path.Combine(projectRoot, relativePath));
            try
            {
                Assert.That(texture.width, Is.EqualTo(expectedWidth));
                Assert.That(texture.height, Is.EqualTo(expectedHeight));
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(texture);
            }
        }

        private static void AssertReleaseEntry(string path, string address)
        {
            var settings = AddressableAssetSettingsDefaultObject.GetSettings(false);
            var entry = settings?.FindAssetEntry(AssetDatabase.AssetPathToGUID(path));
            Assert.That(entry, Is.Not.Null, path);
            Assert.That(entry.parentGroup.Name, Is.EqualTo(AssetProvenanceValidator.QinglanVisualGroup));
            Assert.That(entry.address, Is.EqualTo(address));
            Assert.That(entry.labels, Does.Contain(AssetProvenanceValidator.QinglanPackLabel));
            Assert.That(entry.labels, Does.Contain(AssetProvenanceValidator.ReleaseLabel));
            Assert.That(entry.labels, Does.Contain(AssetProvenanceValidator.VisualReleaseLabel));
            var projectRoot = Directory.GetParent(UnityEngine.Application.dataPath)?.FullName;
            var issues = AssetProvenanceValidator.ValidateReleaseInput(projectRoot, path, entry.labels, entry.parentGroup.Name);
            Assert.That(issues, Is.Empty, JoinIssues(issues));
        }

        private static Texture2D LoadPng(string path)
        {
            var texture = new Texture2D(2, 2, TextureFormat.RGBA32, false);
            Assert.That(ImageConversion.LoadImage(texture, File.ReadAllBytes(path), false), Is.True, path);
            return texture;
        }

        private static int CountNonOpaque(Color32[] pixels)
        {
            var count = 0;
            for (var index = 0; index < pixels.Length; index++)
                if (pixels[index].a != 255) count++;
            return count;
        }

        private static int CountMagenta(Color32[] pixels)
        {
            var count = 0;
            for (var index = 0; index < pixels.Length; index++)
            {
                var pixel = pixels[index];
                if (pixel.r >= 220 && pixel.b >= 180 && pixel.g <= 70) count++;
            }
            return count;
        }

        private static int CountDangerRed(Color32[] pixels)
        {
            var count = 0;
            for (var index = 0; index < pixels.Length; index++)
            {
                var pixel = pixels[index];
                if (pixel.r == 0xE4 && pixel.g == 0x5D && pixel.b == 0x45) count++;
            }
            return count;
        }

        private static LumaMetrics AnalyzeLuma(Color32[] pixels)
        {
            var minimum = 255;
            var maximum = 0;
            double sum = 0;
            double squareSum = 0;
            for (var index = 0; index < pixels.Length; index++)
            {
                var pixel = pixels[index];
                var luma = (pixel.r * 54 + pixel.g * 183 + pixel.b * 19) >> 8;
                minimum = Math.Min(minimum, luma);
                maximum = Math.Max(maximum, luma);
                sum += luma;
                squareSum += luma * luma;
            }

            var mean = sum / pixels.Length;
            var variance = squareSum / pixels.Length - mean * mean;
            return new LumaMetrics(minimum, maximum, (float)mean, (float)Math.Sqrt(Math.Max(0, variance)));
        }

        private static ulong HashPixels(Color32[] pixels)
        {
            var hash = 1469598103934665603UL;
            for (var index = 0; index < pixels.Length; index++)
            {
                var pixel = pixels[index];
                hash = (hash ^ pixel.r) * 1099511628211UL;
                hash = (hash ^ pixel.g) * 1099511628211UL;
                hash = (hash ^ pixel.b) * 1099511628211UL;
                hash = (hash ^ pixel.a) * 1099511628211UL;
            }
            return hash;
        }

        private static ulong HashGrayscale(Color32[] pixels)
        {
            var hash = 1469598103934665603UL;
            for (var index = 0; index < pixels.Length; index++)
            {
                var pixel = pixels[index];
                var luma = (byte)((pixel.r * 54 + pixel.g * 183 + pixel.b * 19) >> 8);
                hash = (hash ^ luma) * 1099511628211UL;
                hash = (hash ^ pixel.a) * 1099511628211UL;
            }
            return hash;
        }

        private static string JoinIssues(IReadOnlyList<ValidationIssue> issues)
        {
            var value = string.Empty;
            for (var index = 0; index < issues.Count; index++)
                value += (index == 0 ? string.Empty : "\n") + issues[index];
            return value;
        }

        private static string SourcePath(string name) => AssetRoot + "/source/" + name + "-imagegen.png";
        private static string MasterPath(string name) => AssetRoot + "/working/" + name + "-source-master.png";
        private static string FinalPath(string name) => AssetRoot + "/final/" + name + ".png";

        private readonly struct AssetSpec
        {
            public AssetSpec(
                string name,
                string address,
                float left,
                float top,
                float right,
                float bottom)
            {
                Name = name;
                Address = address;
                Left = left;
                Top = top;
                Right = right;
                Bottom = bottom;
            }

            public string Name { get; }
            public string Address { get; }
            public float Left { get; }
            public float Top { get; }
            public float Right { get; }
            public float Bottom { get; }
        }

        private readonly struct LumaMetrics
        {
            public LumaMetrics(int minimum, int maximum, float mean, float standardDeviation)
            {
                Minimum = minimum;
                Maximum = maximum;
                Mean = mean;
                StandardDeviation = standardDeviation;
            }

            public int Minimum { get; }
            public int Maximum { get; }
            public float Mean { get; }
            public float StandardDeviation { get; }
        }

        private readonly struct SafeMetrics
        {
            public SafeMetrics(float mean, float standardDeviation, float edgeRatio)
            {
                Mean = mean;
                StandardDeviation = standardDeviation;
                EdgeRatio = edgeRatio;
            }

            public float Mean { get; }
            public float StandardDeviation { get; }
            public float EdgeRatio { get; }
        }
    }
}
