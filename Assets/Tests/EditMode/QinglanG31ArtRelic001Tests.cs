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
    public sealed class QinglanG31ArtRelic001Tests
    {
        private const string AssetRoot = "Assets/GameAssets/AI/QinglanDemo/ART-RELIC-001";

        private static readonly string[] Relics =
        {
            "broken-sword-tassel",
            "wind-vein-copper",
            "herb-garden-seed-pod",
            "listening-wind-core",
            "old-court-bell",
            "blank-sword-trial-token"
        };

        [Test]
        public void SixRelicIconsUseCenteredImportsAndReleaseEntries()
        {
            for (var index = 0; index < Relics.Length; index++)
            {
                var name = Relics[index];
                var path = FinalPath(name);
                var texture = AssetDatabase.LoadAssetAtPath<Texture2D>(path);
                Assert.That(texture, Is.Not.Null, path);
                Assert.That(texture.width, Is.EqualTo(256));
                Assert.That(texture.height, Is.EqualTo(256));

                var importer = AssetImporter.GetAtPath(path) as TextureImporter;
                Assert.That(importer, Is.Not.Null);
                Assert.That(importer.textureType, Is.EqualTo(TextureImporterType.Sprite));
                Assert.That(importer.spriteImportMode, Is.EqualTo(SpriteImportMode.Multiple));
                Assert.That(importer.mipmapEnabled, Is.False);
                Assert.That(importer.alphaIsTransparency, Is.True);
                Assert.That(importer.wrapMode, Is.EqualTo(TextureWrapMode.Clamp));
                Assert.That(importer.maxTextureSize, Is.EqualTo(256));
                var standalone = importer.GetPlatformTextureSettings("Standalone");
                Assert.That(standalone.overridden, Is.True);
                Assert.That(standalone.format, Is.EqualTo(TextureImporterFormat.BC7));

                var stableId = "qinglan.presentation.relic." + name.Replace('-', '_') + ".icon";
                var sprite = FindSprite(path, stableId);
                Assert.That(sprite, Is.Not.Null);
                Assert.That(sprite.pivot.x, Is.EqualTo(128f).Within(0.01f));
                Assert.That(sprite.pivot.y, Is.EqualTo(128f).Within(0.01f));
                AssertReleaseEntry(path, "qinglan/relic/" + name + "/icon");
            }
        }

        [Test]
        public void RelicSourcesAndFinalsMeetAlphaReadabilityAndColorBudgets()
        {
            var projectRoot = Directory.GetParent(UnityEngine.Application.dataPath)?.FullName ?? string.Empty;
            var boundsSignatures = new HashSet<string>(StringComparer.Ordinal);
            for (var index = 0; index < Relics.Length; index++)
            {
                var name = Relics[index];
                var source = LoadPng(Path.Combine(projectRoot, SourcePath(name)));
                try
                {
                    Assert.That(source.width, Is.GreaterThanOrEqualTo(1024));
                    Assert.That(source.height, Is.EqualTo(source.width));
                }
                finally
                {
                    UnityEngine.Object.DestroyImmediate(source);
                }

                var working = LoadPng(Path.Combine(projectRoot, WorkingPath(name)));
                try
                {
                    Assert.That(working.width, Is.GreaterThanOrEqualTo(1024));
                    Assert.That(working.height, Is.EqualTo(working.width));
                    var workingPixels = working.GetPixels32();
                    AssertCornersTransparent(workingPixels, working.width, working.height);
                    Assert.That(CountMagenta(workingPixels), Is.Zero);
                }
                finally
                {
                    UnityEngine.Object.DestroyImmediate(working);
                }

                var final = LoadPng(Path.Combine(projectRoot, FinalPath(name)));
                try
                {
                    Assert.That(final.width, Is.EqualTo(256));
                    Assert.That(final.height, Is.EqualTo(256));
                    var pixels = final.GetPixels32();
                    AssertCornersTransparent(pixels, 256, 256);
                    Assert.That(CountMagenta(pixels), Is.Zero);
                    var bounds = AlphaBounds(pixels, 256, out var covered, out var dangerPixels);
                    Assert.That(bounds.xMin, Is.GreaterThanOrEqualTo(16));
                    Assert.That(bounds.yMin, Is.GreaterThanOrEqualTo(16));
                    Assert.That(256 - bounds.xMax, Is.GreaterThanOrEqualTo(16));
                    Assert.That(256 - bounds.yMax, Is.GreaterThanOrEqualTo(16));
                    var coverage = covered / (256f * 256f);
                    Assert.That(coverage, Is.GreaterThanOrEqualTo(0.35f));
                    Assert.That(coverage, Is.LessThanOrEqualTo(0.60f));
                    Assert.That(dangerPixels, Is.Zero);
                    boundsSignatures.Add(bounds.width + "x" + bounds.height);
                }
                finally
                {
                    UnityEngine.Object.DestroyImmediate(final);
                }
            }

            Assert.That(boundsSignatures.Count, Is.EqualTo(Relics.Length));
        }

        [Test]
        public void OnlyFinalRelicIconsAreAddressable()
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
                Assert.That(path, Does.EndWith("-relic-icon.png"));
                count++;
            }
            Assert.That(count, Is.EqualTo(6));
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

        private static void AssertCornersTransparent(Color32[] pixels, int width, int height)
        {
            Assert.That(pixels[0].a, Is.Zero);
            Assert.That(pixels[width - 1].a, Is.Zero);
            Assert.That(pixels[(height - 1) * width].a, Is.Zero);
            Assert.That(pixels[pixels.Length - 1].a, Is.Zero);
        }

        private static RectInt AlphaBounds(
            Color32[] pixels,
            int width,
            out int covered,
            out int dangerPixels)
        {
            var minimumX = width;
            var minimumY = width;
            var maximumX = -1;
            var maximumY = -1;
            covered = 0;
            dangerPixels = 0;
            for (var y = 0; y < width; y++)
            {
                for (var x = 0; x < width; x++)
                {
                    var pixel = pixels[y * width + x];
                    if (pixel.a < 16) continue;
                    covered++;
                    if (pixel.r == 0xE4 && pixel.g == 0x5D && pixel.b == 0x45) dangerPixels++;
                    minimumX = Math.Min(minimumX, x);
                    minimumY = Math.Min(minimumY, y);
                    maximumX = Math.Max(maximumX, x);
                    maximumY = Math.Max(maximumY, y);
                }
            }
            return new RectInt(minimumX, minimumY, maximumX - minimumX + 1, maximumY - minimumY + 1);
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

        private static string JoinIssues(IReadOnlyList<ValidationIssue> issues)
        {
            var value = string.Empty;
            for (var index = 0; index < issues.Count; index++)
                value += (index == 0 ? string.Empty : "\n") + issues[index];
            return value;
        }

        private static string SourcePath(string name) => AssetRoot + "/source/" + name + "-imagegen.png";
        private static string WorkingPath(string name) => AssetRoot + "/working/" + name + "-transparent.png";
        private static string FinalPath(string name) => AssetRoot + "/final/" + name + "-relic-icon.png";
    }
}
