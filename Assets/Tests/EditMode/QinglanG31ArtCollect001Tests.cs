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
    public sealed class QinglanG31ArtCollect001Tests
    {
        private const string AssetRoot = "Assets/GameAssets/AI/QinglanDemo/ART-COLLECT-001";
        private static readonly string[] Variants = { "illustration", "icon" };

        [Test]
        public void TwelveCollectibleVisualsUseStableIdsCenteredImportsAndReleaseEntries()
        {
            for (var number = 1; number <= 6; number++)
            {
                var numberText = number.ToString("00");
                for (var variantIndex = 0; variantIndex < Variants.Length; variantIndex++)
                {
                    var variant = Variants[variantIndex];
                    var expectedSize = variant == "illustration" ? 1024 : 256;
                    var path = FinalPath(numberText, variant);
                    var texture = AssetDatabase.LoadAssetAtPath<Texture2D>(path);
                    Assert.That(texture, Is.Not.Null, path);
                    Assert.That(texture.width, Is.EqualTo(expectedSize));
                    Assert.That(texture.height, Is.EqualTo(expectedSize));

                    var importer = AssetImporter.GetAtPath(path) as TextureImporter;
                    Assert.That(importer, Is.Not.Null);
                    Assert.That(importer.textureType, Is.EqualTo(TextureImporterType.Sprite));
                    Assert.That(importer.spriteImportMode, Is.EqualTo(SpriteImportMode.Multiple));
                    Assert.That(importer.mipmapEnabled, Is.False);
                    Assert.That(importer.alphaIsTransparency, Is.True);
                    Assert.That(importer.wrapMode, Is.EqualTo(TextureWrapMode.Clamp));
                    Assert.That(importer.maxTextureSize, Is.EqualTo(expectedSize));
                    var standalone = importer.GetPlatformTextureSettings("Standalone");
                    Assert.That(standalone.overridden, Is.True);
                    Assert.That(standalone.maxTextureSize, Is.EqualTo(expectedSize));
                    Assert.That(standalone.format, Is.EqualTo(TextureImporterFormat.BC7));

                    var stableId = "qinglan.presentation.collectible.old_court." + numberText + "." + variant;
                    var sprite = FindSprite(path, stableId);
                    Assert.That(sprite, Is.Not.Null, stableId);
                    Assert.That(sprite.rect.width, Is.EqualTo(expectedSize));
                    Assert.That(sprite.rect.height, Is.EqualTo(expectedSize));
                    Assert.That(sprite.pivot.x, Is.EqualTo(expectedSize / 2f).Within(0.01f));
                    Assert.That(sprite.pivot.y, Is.EqualTo(expectedSize / 2f).Within(0.01f));
                    AssertReleaseEntry(path, "qinglan/collectible/old-court-" + numberText + "/" + variant);
                }
            }
        }

        [Test]
        public void ApprovedSourcesMastersIllustrationsAndIconsMeetVisualBudgets()
        {
            var projectRoot = Directory.GetParent(UnityEngine.Application.dataPath)?.FullName ?? string.Empty;
            var illustrationColorHashes = new HashSet<ulong>();
            var illustrationGrayscaleHashes = new HashSet<ulong>();
            var iconColorHashes = new HashSet<ulong>();
            var iconGrayscaleHashes = new HashSet<ulong>();
            for (var number = 1; number <= 6; number++)
            {
                var numberText = number.ToString("00");
                var minimumCoverage = number == 6 ? 0.13f : 0.18f;
                AssertOpaqueSquare(projectRoot, SourcePath(numberText), 1254);
                AssertTransparentSquare(
                    projectRoot, TransparentPath(numberText), 1254, 0, minimumCoverage, 0.55f, null, null);
                AssertTransparentSquare(
                    projectRoot, MasterPath(numberText), 2048, 127, minimumCoverage, 0.55f, null, null);
                AssertTransparentSquare(
                    projectRoot,
                    FinalPath(numberText, "illustration"),
                    1024,
                    63,
                    minimumCoverage,
                    0.55f,
                    illustrationColorHashes,
                    illustrationGrayscaleHashes);
                AssertTransparentSquare(
                    projectRoot,
                    FinalPath(numberText, "icon"),
                    256,
                    15,
                    minimumCoverage,
                    0.55f,
                    iconColorHashes,
                    iconGrayscaleHashes);
            }

            Assert.That(illustrationColorHashes.Count, Is.EqualTo(6));
            Assert.That(illustrationGrayscaleHashes.Count, Is.EqualTo(6));
            Assert.That(iconColorHashes.Count, Is.EqualTo(6));
            Assert.That(iconGrayscaleHashes.Count, Is.EqualTo(6),
                "Collectible icon silhouettes and value structures must remain distinct without hue.");
        }

        [Test]
        public void OnlyTwelveApprovedFinalCollectibleVisualsAreAddressable()
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
                Assert.That(path, Does.EndWith("-illustration.png").Or.EndWith("-icon.png"));
                count++;
            }

            Assert.That(count, Is.EqualTo(12));
        }

        private static void AssertOpaqueSquare(string projectRoot, string relativePath, int expectedSize)
        {
            var texture = LoadPng(Path.Combine(projectRoot, relativePath));
            try
            {
                Assert.That(texture.width, Is.EqualTo(expectedSize));
                Assert.That(texture.height, Is.EqualTo(expectedSize));
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(texture);
            }
        }

        private static void AssertTransparentSquare(
            string projectRoot,
            string relativePath,
            int expectedSize,
            int minimumMargin,
            float minimumCoverage,
            float maximumCoverage,
            ISet<ulong> colorHashes,
            ISet<ulong> grayscaleHashes)
        {
            var texture = LoadPng(Path.Combine(projectRoot, relativePath));
            try
            {
                Assert.That(texture.width, Is.EqualTo(expectedSize));
                Assert.That(texture.height, Is.EqualTo(expectedSize));
                var pixels = texture.GetPixels32();
                AssertCornersTransparent(pixels, expectedSize);
                Assert.That(CountMagenta(pixels), Is.Zero);
                Assert.That(CountDangerRed(pixels), Is.Zero);
                var metrics = Analyze(pixels, expectedSize);
                Assert.That(metrics.Margin, Is.GreaterThanOrEqualTo(minimumMargin));
                Assert.That(metrics.Coverage, Is.InRange(minimumCoverage, maximumCoverage));
                colorHashes?.Add(HashPixels(pixels));
                grayscaleHashes?.Add(HashGrayscale(pixels));
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(texture);
            }
        }

        private static Sprite FindSprite(string path, string name)
        {
            var assets = AssetDatabase.LoadAllAssetsAtPath(path);
            for (var index = 0; index < assets.Length; index++)
                if (assets[index] is Sprite sprite && string.Equals(sprite.name, name, StringComparison.Ordinal))
                    return sprite;
            return null;
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

        private static void AssertCornersTransparent(Color32[] pixels, int size)
        {
            Assert.That(pixels[0].a, Is.Zero);
            Assert.That(pixels[size - 1].a, Is.Zero);
            Assert.That(pixels[(size - 1) * size].a, Is.Zero);
            Assert.That(pixels[pixels.Length - 1].a, Is.Zero);
        }

        private static int CountMagenta(Color32[] pixels)
        {
            var count = 0;
            for (var index = 0; index < pixels.Length; index++)
            {
                var pixel = pixels[index];
                if (pixel.a >= 16 && pixel.r >= 220 && pixel.b >= 180 && pixel.g <= 70) count++;
            }
            return count;
        }

        private static int CountDangerRed(Color32[] pixels)
        {
            var count = 0;
            for (var index = 0; index < pixels.Length; index++)
            {
                var pixel = pixels[index];
                if (pixel.a >= 16 && pixel.r == 0xE4 && pixel.g == 0x5D && pixel.b == 0x45) count++;
            }
            return count;
        }

        private static OutputMetrics Analyze(Color32[] pixels, int size)
        {
            var minX = size;
            var minY = size;
            var maxX = -1;
            var maxY = -1;
            var covered = 0;
            for (var y = 0; y < size; y++)
            {
                for (var x = 0; x < size; x++)
                {
                    if (pixels[y * size + x].a < 16) continue;
                    covered++;
                    minX = Math.Min(minX, x);
                    minY = Math.Min(minY, y);
                    maxX = Math.Max(maxX, x);
                    maxY = Math.Max(maxY, y);
                }
            }

            Assert.That(covered, Is.GreaterThan(0));
            var margin = Math.Min(Math.Min(minX, minY), Math.Min(size - maxX - 1, size - maxY - 1));
            return new OutputMetrics(margin, covered / (float)(size * size));
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

        private static string SourcePath(string number) => AssetRoot + "/source/old-court-" + number + "-imagegen.png";
        private static string TransparentPath(string number) => AssetRoot + "/working/old-court-" + number + "-transparent.png";
        private static string MasterPath(string number) => AssetRoot + "/working/old-court-" + number + "-source-master.png";
        private static string FinalPath(string number, string variant) => AssetRoot + "/final/old-court-" + number + "-" + variant + ".png";

        private readonly struct OutputMetrics
        {
            public OutputMetrics(int margin, float coverage)
            {
                Margin = margin;
                Coverage = coverage;
            }

            public int Margin { get; }
            public float Coverage { get; }
        }
    }
}
