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
    public sealed class QinglanG31ArtHub001Tests
    {
        private const string AssetRoot = "Assets/GameAssets/AI/QinglanDemo/ART-HUB-001";

        private static readonly string[] Facilities =
        {
            "vein-inquiry-platform",
            "scroll-pavilion",
            "hundred-artifact-pavilion",
            "myriad-phenomena-pavilion"
        };

        private static readonly string[] StableNames =
        {
            "vein_inquiry_platform",
            "scroll_pavilion",
            "hundred_artifact_pavilion",
            "myriad_phenomena_pavilion"
        };

        [Test]
        public void FourFacilitiesProvideCenteredPanelAndIconReleaseSprites()
        {
            for (var facilityIndex = 0; facilityIndex < Facilities.Length; facilityIndex++)
            {
                var facility = Facilities[facilityIndex];
                AssertImportedSprite(
                    FinalPath(facility, "panel"),
                    1024,
                    "qinglan.presentation.facility." + StableNames[facilityIndex] + ".panel",
                    "qinglan/hub/facility/" + facility + "/panel");
                AssertImportedSprite(
                    FinalPath(facility, "icon"),
                    256,
                    "qinglan.presentation.facility." + StableNames[facilityIndex] + ".icon",
                    "qinglan/hub/facility/" + facility + "/icon");
            }
        }

        [Test]
        public void ApprovedSourcesPanelsAndIconsMeetGeometryAlphaAndDistinctnessBudgets()
        {
            var projectRoot = Directory.GetParent(UnityEngine.Application.dataPath)?.FullName ?? string.Empty;
            var finalHashes = new HashSet<ulong>();
            for (var facilityIndex = 0; facilityIndex < Facilities.Length; facilityIndex++)
            {
                var facility = Facilities[facilityIndex];
                AssertPngGeometry(projectRoot, SourcePath(facility), 1254);

                var transparent = LoadPng(Path.Combine(projectRoot, TransparentPath(facility)));
                try
                {
                    Assert.That(transparent.width, Is.EqualTo(1254));
                    Assert.That(transparent.height, Is.EqualTo(1254));
                    var pixels = transparent.GetPixels32();
                    AssertCornersTransparent(pixels, transparent.width, transparent.height);
                    Assert.That(CountMagenta(pixels), Is.Zero);
                }
                finally
                {
                    UnityEngine.Object.DestroyImmediate(transparent);
                }

                AssertPngGeometry(projectRoot, SourceMasterPath(facility), 2048);
                AssertFinalPng(projectRoot, facility, "panel", 1024, 63, 0.39f, 0.51f, finalHashes);
                AssertFinalPng(projectRoot, facility, "icon", 256, 15, 0.40f, 0.52f, finalHashes);
            }

            Assert.That(finalHashes.Count, Is.EqualTo(8));
        }

        [Test]
        public void OnlyApprovedFinalFacilitySpritesAreAddressable()
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
                Assert.That(path, Does.EndWith("-panel.png").Or.EndWith("-icon.png"));
                count++;
            }

            Assert.That(count, Is.EqualTo(8));
        }

        private static void AssertImportedSprite(string path, int expectedSize, string spriteName, string address)
        {
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
            Assert.That(importer.maxTextureSize, Is.EqualTo(1024));
            var standalone = importer.GetPlatformTextureSettings("Standalone");
            Assert.That(standalone.overridden, Is.True);
            Assert.That(standalone.format, Is.EqualTo(TextureImporterFormat.BC7));

            var sprites = FindSprites(path);
            Assert.That(sprites.Count, Is.EqualTo(1));
            Assert.That(sprites.ContainsKey(spriteName), Is.True, spriteName);
            var sprite = sprites[spriteName];
            Assert.That(sprite.rect.width, Is.EqualTo(expectedSize));
            Assert.That(sprite.rect.height, Is.EqualTo(expectedSize));
            Assert.That(sprite.pivot.x, Is.EqualTo(expectedSize / 2f).Within(0.01f));
            Assert.That(sprite.pivot.y, Is.EqualTo(expectedSize / 2f).Within(0.01f));
            AssertReleaseEntry(path, address);
        }

        private static void AssertPngGeometry(string projectRoot, string relativePath, int expectedSize)
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

        private static void AssertFinalPng(
            string projectRoot,
            string facility,
            string variant,
            int size,
            int minimumMargin,
            float minimumCoverage,
            float maximumCoverage,
            ISet<ulong> hashes)
        {
            var texture = LoadPng(Path.Combine(projectRoot, FinalPath(facility, variant)));
            try
            {
                Assert.That(texture.width, Is.EqualTo(size));
                Assert.That(texture.height, Is.EqualTo(size));
                var pixels = texture.GetPixels32();
                AssertCornersTransparent(pixels, texture.width, texture.height);
                Assert.That(CountMagenta(pixels), Is.Zero);
                Assert.That(CountDangerRed(pixels), Is.Zero);
                var metrics = Analyze(pixels, size);
                Assert.That(metrics.Margin, Is.GreaterThanOrEqualTo(minimumMargin));
                Assert.That(metrics.Coverage, Is.InRange(minimumCoverage, maximumCoverage));
                hashes.Add(HashPixels(pixels));
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

        private static string JoinIssues(IReadOnlyList<ValidationIssue> issues)
        {
            var value = string.Empty;
            for (var index = 0; index < issues.Count; index++)
                value += (index == 0 ? string.Empty : "\n") + issues[index];
            return value;
        }

        private static string SourcePath(string facility) => AssetRoot + "/source/" + facility + "-imagegen.png";
        private static string TransparentPath(string facility) => AssetRoot + "/working/" + facility + "-transparent.png";
        private static string SourceMasterPath(string facility) => AssetRoot + "/working/" + facility + "-source-master.png";
        private static string FinalPath(string facility, string variant) => AssetRoot + "/final/" + facility + "-" + variant + ".png";

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
