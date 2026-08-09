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
    public sealed class QinglanG31ArtMeta001Tests
    {
        private const string AssetRoot = "Assets/GameAssets/FirstParty/QinglanDemo/ART-META-001";
        private static readonly string[] Branches = { "innate", "movement", "mind" };

        [Test]
        public void TwelveMetaNodeIconsUseStableIdsCenteredImportsAndReleaseEntries()
        {
            for (var branchIndex = 0; branchIndex < Branches.Length; branchIndex++)
            {
                var branch = Branches[branchIndex];
                for (var number = 1; number <= 4; number++)
                {
                    var numberText = number.ToString("00");
                    var path = FinalPath(branch, numberText);
                    var texture = AssetDatabase.LoadAssetAtPath<Texture2D>(path);
                    Assert.That(texture, Is.Not.Null, path);
                    Assert.That(texture.width, Is.EqualTo(128));
                    Assert.That(texture.height, Is.EqualTo(128));

                    var importer = AssetImporter.GetAtPath(path) as TextureImporter;
                    Assert.That(importer, Is.Not.Null);
                    Assert.That(importer.textureType, Is.EqualTo(TextureImporterType.Sprite));
                    Assert.That(importer.spriteImportMode, Is.EqualTo(SpriteImportMode.Multiple));
                    Assert.That(importer.mipmapEnabled, Is.False);
                    Assert.That(importer.alphaIsTransparency, Is.True);
                    Assert.That(importer.wrapMode, Is.EqualTo(TextureWrapMode.Clamp));
                    Assert.That(importer.maxTextureSize, Is.EqualTo(128));
                    var standalone = importer.GetPlatformTextureSettings("Standalone");
                    Assert.That(standalone.overridden, Is.True);
                    Assert.That(standalone.maxTextureSize, Is.EqualTo(128));
                    Assert.That(standalone.format, Is.EqualTo(TextureImporterFormat.BC7));

                    var spriteName = "qinglan.presentation.meta.lu_qingye." + branch + "." + numberText;
                    var sprites = FindSprites(path);
                    Assert.That(sprites.Count, Is.EqualTo(1));
                    Assert.That(sprites.ContainsKey(spriteName), Is.True, spriteName);
                    var sprite = sprites[spriteName];
                    Assert.That(sprite.rect.width, Is.EqualTo(128));
                    Assert.That(sprite.rect.height, Is.EqualTo(128));
                    Assert.That(sprite.pivot.x, Is.EqualTo(64f).Within(0.01f));
                    Assert.That(sprite.pivot.y, Is.EqualTo(64f).Within(0.01f));
                    AssertReleaseEntry(path, "qinglan/hub/meta-node/" + branch + "/" + numberText + "/icon");
                }
            }
        }

        [Test]
        public void SourcesAndFinalsMeetVectorRasterGeometryAlphaAndGrayscaleDistinctnessBudgets()
        {
            var projectRoot = Directory.GetParent(UnityEngine.Application.dataPath)?.FullName ?? string.Empty;
            var sourceHashes = new HashSet<ulong>();
            var finalHashes = new HashSet<ulong>();
            var grayscaleHashes = new HashSet<ulong>();
            for (var branchIndex = 0; branchIndex < Branches.Length; branchIndex++)
            {
                var branch = Branches[branchIndex];
                for (var number = 1; number <= 4; number++)
                {
                    var numberText = number.ToString("00");
                    AssertIcon(
                        projectRoot,
                        SourcePath(branch, numberText),
                        1024,
                        80,
                        0.30f,
                        0.66f,
                        sourceHashes,
                        null);
                    AssertIcon(
                        projectRoot,
                        FinalPath(branch, numberText),
                        128,
                        10,
                        0.30f,
                        0.66f,
                        finalHashes,
                        grayscaleHashes);
                }
            }

            Assert.That(sourceHashes.Count, Is.EqualTo(12));
            Assert.That(finalHashes.Count, Is.EqualTo(12));
            Assert.That(grayscaleHashes.Count, Is.EqualTo(12),
                "Node motifs must remain distinct after hue information is discarded.");
        }

        [Test]
        public void OnlyApprovedFinalMetaNodeIconsAreAddressable()
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
                Assert.That(path, Does.EndWith("-icon.png"));
                count++;
            }

            Assert.That(count, Is.EqualTo(12));
        }

        private static void AssertIcon(
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
                AssertCornersTransparent(pixels, texture.width, texture.height);
                Assert.That(CountMagenta(pixels), Is.Zero);
                Assert.That(CountDangerRed(pixels), Is.Zero);
                var metrics = Analyze(pixels, expectedSize);
                Assert.That(metrics.Margin, Is.GreaterThanOrEqualTo(minimumMargin));
                Assert.That(metrics.Coverage, Is.InRange(minimumCoverage, maximumCoverage));
                colorHashes.Add(HashPixels(pixels));
                grayscaleHashes?.Add(HashGrayscale(pixels));
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

        private static void AssertCornersTransparent(Color32[] pixels, int width, int height)
        {
            Assert.That(pixels[0].a, Is.Zero);
            Assert.That(pixels[width - 1].a, Is.Zero);
            Assert.That(pixels[(height - 1) * width].a, Is.Zero);
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
                var luminance = (byte)((pixel.r * 54 + pixel.g * 183 + pixel.b * 19) >> 8);
                hash = (hash ^ luminance) * 1099511628211UL;
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

        private static string SourcePath(string branch, string number) => AssetRoot + "/source/" + branch + "-" + number + "-source.png";
        private static string FinalPath(string branch, string number) => AssetRoot + "/final/" + branch + "-" + number + "-icon.png";

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
