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
    public sealed class QinglanG31ArtUi003Tests
    {
        private const string AssetRoot = "Assets/GameAssets/AI/QinglanDemo/ART-UI-003";
        private static readonly BackgroundSpec[] Backgrounds =
        {
            new BackgroundSpec("character-select", "character_select"),
            new BackgroundSpec("map-select", "map_select"),
            new BackgroundSpec("loadout", "loadout"),
            new BackgroundSpec("choice", "choice"),
            new BackgroundSpec("hub", "hub"),
            new BackgroundSpec("story-result", "story_result")
        };

        [Test]
        public void SixPageBackgroundsUseCenteredBudgetedImportsAndReleaseEntries()
        {
            for (var index = 0; index < Backgrounds.Length; index++)
            {
                var spec = Backgrounds[index];
                var path = FinalPath(spec.FileName);
                var texture = AssetDatabase.LoadAssetAtPath<Texture2D>(path);
                Assert.That(texture, Is.Not.Null, path);
                Assert.That(texture.width, Is.EqualTo(1920));
                Assert.That(texture.height, Is.EqualTo(1080));

                var importer = AssetImporter.GetAtPath(path) as TextureImporter;
                Assert.That(importer, Is.Not.Null);
                Assert.That(importer.textureType, Is.EqualTo(TextureImporterType.Sprite));
                Assert.That(importer.spriteImportMode, Is.EqualTo(SpriteImportMode.Multiple));
                Assert.That(importer.mipmapEnabled, Is.False);
                Assert.That(importer.alphaIsTransparency, Is.True);
                Assert.That(importer.wrapMode, Is.EqualTo(TextureWrapMode.Clamp));
                Assert.That(importer.maxTextureSize, Is.EqualTo(2048));
                var standalone = importer.GetPlatformTextureSettings("Standalone");
                Assert.That(standalone.overridden, Is.True);
                Assert.That(standalone.maxTextureSize, Is.EqualTo(2048));
                Assert.That(standalone.format, Is.EqualTo(TextureImporterFormat.BC7));

                var sprites = FindSprites(path);
                var spriteName = "qinglan.presentation.ui.page_background." + spec.SpriteSuffix;
                Assert.That(sprites.Count, Is.EqualTo(1));
                Assert.That(sprites.ContainsKey(spriteName), Is.True, spriteName);
                var sprite = sprites[spriteName];
                Assert.That(sprite.rect, Is.EqualTo(new Rect(0f, 0f, 1920f, 1080f)));
                Assert.That(sprite.pivot, Is.EqualTo(new Vector2(960f, 540f)));
                Assert.That(sprite.border, Is.EqualTo(Vector4.zero));
                AssertReleaseEntry(path, "qinglan/ui/page-background/" + spec.FileName);
            }
        }

        [Test]
        public void SourcesMastersAndFinalsMeetSafeRegionTonalAndDistinctnessBudgets()
        {
            var projectRoot = Directory.GetParent(UnityEngine.Application.dataPath)?.FullName ?? string.Empty;
            var colorHashes = new HashSet<ulong>();
            var grayscaleHashes = new HashSet<ulong>();
            for (var index = 0; index < Backgrounds.Length; index++)
            {
                var name = Backgrounds[index].FileName;
                AssertDimensions(projectRoot, SourcePath(name), 1672, 941);
                AssertDimensions(projectRoot, MasterPath(name), 2560, 1440);
                var final = LoadPng(Path.Combine(projectRoot, FinalPath(name)));
                try
                {
                    Assert.That(final.width, Is.EqualTo(1920));
                    Assert.That(final.height, Is.EqualTo(1080));
                    var pixels = final.GetPixels32();
                    Assert.That(CountNonOpaque(pixels), Is.Zero, name);
                    Assert.That(CountMagenta(pixels), Is.Zero, name);
                    Assert.That(CountDangerRed(pixels), Is.Zero, name);
                    var safe = AnalyzeSafeRegion(pixels, final.width, final.height);
                    Assert.That(safe.Mean, Is.InRange(170f, 245f), name);
                    Assert.That(safe.StandardDeviation, Is.LessThanOrEqualTo(34f), name);
                    Assert.That(safe.EdgeRatio, Is.LessThanOrEqualTo(0.04f), name);
                    var tonal = AnalyzeLuma(pixels);
                    Assert.That(tonal.Minimum, Is.LessThanOrEqualTo(96), name);
                    Assert.That(tonal.Maximum, Is.GreaterThanOrEqualTo(220), name);
                    Assert.That(tonal.StandardDeviation, Is.GreaterThanOrEqualTo(24f), name);
                    colorHashes.Add(HashPixels(pixels));
                    grayscaleHashes.Add(HashGrayscale(pixels));
                }
                finally
                {
                    UnityEngine.Object.DestroyImmediate(final);
                }
            }

            Assert.That(colorHashes.Count, Is.EqualTo(6));
            Assert.That(grayscaleHashes.Count, Is.EqualTo(6));
            AssertDimensions(
                projectRoot,
                AssetRoot + "/source/map-select-rejected-six-regions.png",
                1672,
                941);
        }

        [Test]
        public void OnlySixApprovedFinalPageBackgroundsAreAddressable()
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
                Assert.That(path, Does.EndWith("-background.png"));
                count++;
            }
            Assert.That(count, Is.EqualTo(6));
        }

        private static SafeMetrics AnalyzeSafeRegion(Color32[] pixels, int width, int height)
        {
            var left = Mathf.RoundToInt(0.04f * width);
            var top = Mathf.RoundToInt(0.06f * height);
            var right = Mathf.RoundToInt(0.58f * width);
            var bottom = Mathf.RoundToInt(0.94f * height);
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
            return new SafeMetrics((float)mean, (float)Math.Sqrt(Math.Max(0, variance)), edgeCount / (float)sampleCount);
        }

        private static LumaMetrics AnalyzeLuma(Color32[] pixels)
        {
            var minimum = 255;
            var maximum = 0;
            double sum = 0;
            double squareSum = 0;
            for (var index = 0; index < pixels.Length; index++)
            {
                var luma = Luma(pixels[index]);
                minimum = Math.Min(minimum, luma);
                maximum = Math.Max(maximum, luma);
                sum += luma;
                squareSum += luma * luma;
            }
            var mean = sum / pixels.Length;
            var variance = squareSum / pixels.Length - mean * mean;
            return new LumaMetrics(minimum, maximum, (float)Math.Sqrt(Math.Max(0, variance)));
        }

        private static int Luma(Color32 color) => (color.r * 54 + color.g * 183 + color.b * 19) >> 8;

        private static void AssertDimensions(string projectRoot, string relativePath, int width, int height)
        {
            var texture = LoadPng(Path.Combine(projectRoot, relativePath));
            try
            {
                Assert.That(texture.width, Is.EqualTo(width), relativePath);
                Assert.That(texture.height, Is.EqualTo(height), relativePath);
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(texture);
            }
        }

        private static Dictionary<string, Sprite> FindSprites(string path)
        {
            var result = new Dictionary<string, Sprite>(StringComparer.Ordinal);
            var assets = AssetDatabase.LoadAllAssetsAtPath(path);
            for (var index = 0; index < assets.Length; index++)
                if (assets[index] is Sprite sprite)
                    result.Add(sprite.name, sprite);
            return result;
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
                var luma = (byte)Luma(pixels[index]);
                hash = (hash ^ luma) * 1099511628211UL;
                hash = (hash ^ pixels[index].a) * 1099511628211UL;
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
        private static string FinalPath(string name) => AssetRoot + "/final/" + name + "-background.png";

        private readonly struct BackgroundSpec
        {
            public BackgroundSpec(string fileName, string spriteSuffix)
            {
                FileName = fileName;
                SpriteSuffix = spriteSuffix;
            }
            public string FileName { get; }
            public string SpriteSuffix { get; }
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

        private readonly struct LumaMetrics
        {
            public LumaMetrics(int minimum, int maximum, float standardDeviation)
            {
                Minimum = minimum;
                Maximum = maximum;
                StandardDeviation = standardDeviation;
            }
            public int Minimum { get; }
            public int Maximum { get; }
            public float StandardDeviation { get; }
        }
    }
}
