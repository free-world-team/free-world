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
    public sealed class QinglanG31ArtPickup001Tests
    {
        private const string AssetRoot = "Assets/GameAssets/AI/QinglanDemo/ART-PICKUP-001";

        private static readonly string[] Pickups =
        {
            "greenwood-dew",
            "boundary-talisman",
            "thunder-jade",
            "spirit-gourd",
            "heart-guard-jade",
            "riding-wind-feather"
        };

        [Test]
        public void TwelvePickupTexturesUseCenteredImportsAndReleaseEntries()
        {
            for (var pickupIndex = 0; pickupIndex < Pickups.Length; pickupIndex++)
            {
                AssertImported(Pickups[pickupIndex], "sprite", 256);
                AssertImported(Pickups[pickupIndex], "icon", 128);
            }
        }

        [Test]
        public void PickupSourcesAndFinalsMeetAlphaReadabilityBudgets()
        {
            var projectRoot = Directory.GetParent(UnityEngine.Application.dataPath)?.FullName ?? string.Empty;
            var spriteBounds = new HashSet<string>(StringComparer.Ordinal);
            for (var pickupIndex = 0; pickupIndex < Pickups.Length; pickupIndex++)
            {
                var name = Pickups[pickupIndex];
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
                    Assert.That(CountMagenta(workingPixels), Is.Zero);
                    Assert.That(workingPixels[0].a, Is.Zero);
                    Assert.That(workingPixels[working.width - 1].a, Is.Zero);
                    Assert.That(workingPixels[(working.height - 1) * working.width].a, Is.Zero);
                    Assert.That(workingPixels[workingPixels.Length - 1].a, Is.Zero);
                }
                finally
                {
                    UnityEngine.Object.DestroyImmediate(working);
                }

                AssertFinal(projectRoot, name, "sprite", 256, 16, spriteBounds);
                AssertFinal(projectRoot, name, "icon", 128, 10, null);
            }

            Assert.That(spriteBounds.Count, Is.EqualTo(Pickups.Length));
        }

        [Test]
        public void OnlyFinalPickupSpritesAndIconsAreAddressable()
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
                Assert.That(path, Does.Match("-pickup-(sprite|icon)\\.png$"));
                count++;
            }
            Assert.That(count, Is.EqualTo(12));
        }

        private static void AssertImported(string name, string kind, int size)
        {
            var path = FinalPath(name, kind);
            var texture = AssetDatabase.LoadAssetAtPath<Texture2D>(path);
            Assert.That(texture, Is.Not.Null, path);
            Assert.That(texture.width, Is.EqualTo(size));
            Assert.That(texture.height, Is.EqualTo(size));

            var importer = AssetImporter.GetAtPath(path) as TextureImporter;
            Assert.That(importer, Is.Not.Null);
            Assert.That(importer.textureType, Is.EqualTo(TextureImporterType.Sprite));
            Assert.That(importer.spriteImportMode, Is.EqualTo(SpriteImportMode.Multiple));
            Assert.That(importer.mipmapEnabled, Is.False);
            Assert.That(importer.alphaIsTransparency, Is.True);
            Assert.That(importer.wrapMode, Is.EqualTo(TextureWrapMode.Clamp));
            Assert.That(importer.maxTextureSize, Is.EqualTo(size));
            var standalone = importer.GetPlatformTextureSettings("Standalone");
            Assert.That(standalone.overridden, Is.True);
            Assert.That(standalone.format, Is.EqualTo(TextureImporterFormat.BC7));

            var stableId = "qinglan.presentation.pickup." + name.Replace('-', '_') + "." + kind;
            var sprite = FindSprite(path, stableId);
            Assert.That(sprite, Is.Not.Null);
            Assert.That(sprite.pivot.x, Is.EqualTo(size * 0.5f).Within(0.01f));
            Assert.That(sprite.pivot.y, Is.EqualTo(size * 0.5f).Within(0.01f));
            AssertReleaseEntry(path, "qinglan/pickup/" + name + "/" + kind);
        }

        private static void AssertFinal(
            string projectRoot,
            string name,
            string kind,
            int size,
            int minimumMargin,
            ISet<string> boundsSignatures)
        {
            var texture = LoadPng(Path.Combine(projectRoot, FinalPath(name, kind)));
            try
            {
                Assert.That(texture.width, Is.EqualTo(size));
                Assert.That(texture.height, Is.EqualTo(size));
                var pixels = texture.GetPixels32();
                Assert.That(pixels[0].a, Is.Zero);
                Assert.That(pixels[size - 1].a, Is.Zero);
                Assert.That(pixels[(size - 1) * size].a, Is.Zero);
                Assert.That(pixels[pixels.Length - 1].a, Is.Zero);
                Assert.That(CountMagenta(pixels), Is.Zero);
                var bounds = AlphaBounds(pixels, size, out var covered);
                Assert.That(bounds.xMin, Is.GreaterThanOrEqualTo(minimumMargin));
                Assert.That(bounds.yMin, Is.GreaterThanOrEqualTo(minimumMargin));
                Assert.That(size - bounds.xMax, Is.GreaterThanOrEqualTo(minimumMargin));
                Assert.That(size - bounds.yMax, Is.GreaterThanOrEqualTo(minimumMargin));
                var ratio = covered / (float)(size * size);
                Assert.That(ratio, Is.GreaterThanOrEqualTo(0.25f));
                Assert.That(ratio, Is.LessThanOrEqualTo(0.70f));
                boundsSignatures?.Add(bounds.width + "x" + bounds.height);
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

        private static RectInt AlphaBounds(Color32[] pixels, int width, out int covered)
        {
            var minimumX = width;
            var minimumY = width;
            var maximumX = -1;
            var maximumY = -1;
            covered = 0;
            for (var y = 0; y < width; y++)
            {
                for (var x = 0; x < width; x++)
                {
                    var pixel = pixels[y * width + x];
                    if (pixel.a < 16) continue;
                    covered++;
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
        private static string FinalPath(string name, string kind) => AssetRoot + "/final/" + name + "-pickup-" + kind + ".png";
    }
}
