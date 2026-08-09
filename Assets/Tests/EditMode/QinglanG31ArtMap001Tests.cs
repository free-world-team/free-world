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
    public sealed class QinglanG31ArtMap001Tests
    {
        private const string AssetRoot = "Assets/GameAssets/AI/QinglanDemo/ART-MAP-001";

        private static readonly string[] Regions =
        {
            "central-training-ground",
            "west-herb-garden",
            "east-sword-gallery",
            "north-old-gate",
            "south-guest-court"
        };

        [Test]
        public void FiveRegionAtlasesUseSixteenTileImportsAndReleaseEntries()
        {
            for (var regionIndex = 0; regionIndex < Regions.Length; regionIndex++)
            {
                var region = Regions[regionIndex];
                var path = FinalPath(region);
                var texture = AssetDatabase.LoadAssetAtPath<Texture2D>(path);
                Assert.That(texture, Is.Not.Null, path);
                Assert.That(texture.width, Is.EqualTo(2048));
                Assert.That(texture.height, Is.EqualTo(512));

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
                Assert.That(standalone.format, Is.EqualTo(TextureImporterFormat.BC7));

                var prefix = "qinglan.presentation.map.old_court.region." + region.Replace('-', '_') + ".tile";
                var sprites = FindSprites(path);
                Assert.That(sprites.Count, Is.EqualTo(16));
                for (var row = 0; row < 2; row++)
                {
                    for (var column = 0; column < 8; column++)
                    {
                        var spriteName = prefix + ".r" + row + ".c" + column;
                        Assert.That(sprites.ContainsKey(spriteName), Is.True, spriteName);
                        var sprite = sprites[spriteName];
                        Assert.That(sprite.rect.width, Is.EqualTo(256f));
                        Assert.That(sprite.rect.height, Is.EqualTo(256f));
                        Assert.That(sprite.pivot.x, Is.EqualTo(128f).Within(0.01f));
                        Assert.That(sprite.pivot.y, Is.EqualTo(128f).Within(0.01f));
                    }
                }
                AssertReleaseEntry(path, "qinglan/map/old-court/region/" + region + "/tile-kit");
            }
        }

        [Test]
        public void RegionSourcesMastersAndFinalsMeetGeometryAndPixelBudgets()
        {
            var projectRoot = Directory.GetParent(UnityEngine.Application.dataPath)?.FullName ?? string.Empty;
            var atlasSignatures = new HashSet<ulong>();
            for (var regionIndex = 0; regionIndex < Regions.Length; regionIndex++)
            {
                var region = Regions[regionIndex];
                var source = LoadPng(Path.Combine(projectRoot, SourcePath(region)));
                try
                {
                    Assert.That(source.width, Is.GreaterThanOrEqualTo(1024));
                    Assert.That(source.height, Is.EqualTo(source.width));
                }
                finally
                {
                    UnityEngine.Object.DestroyImmediate(source);
                }

                var working = LoadPng(Path.Combine(projectRoot, WorkingPath(region)));
                try
                {
                    Assert.That(working.width, Is.EqualTo(4096));
                    Assert.That(working.height, Is.EqualTo(1024));
                    AssertOpaqueAndSafe(working.GetPixels32());
                }
                finally
                {
                    UnityEngine.Object.DestroyImmediate(working);
                }

                var final = LoadPng(Path.Combine(projectRoot, FinalPath(region)));
                try
                {
                    Assert.That(final.width, Is.EqualTo(2048));
                    Assert.That(final.height, Is.EqualTo(512));
                    var pixels = final.GetPixels32();
                    AssertOpaqueAndSafe(pixels);
                    var tileSignatures = TileSignatures(pixels, final.width, 256, 8, 2);
                    Assert.That(tileSignatures.Count, Is.EqualTo(16), region);
                    atlasSignatures.Add(HashPixels(pixels, 0, pixels.Length, 1));
                }
                finally
                {
                    UnityEngine.Object.DestroyImmediate(final);
                }
            }

            Assert.That(atlasSignatures.Count, Is.EqualTo(Regions.Length));
        }

        [Test]
        public void OnlyFinalRegionAtlasesAreAddressable()
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
                Assert.That(path, Does.EndWith("-tile-kit-atlas.png"));
                count++;
            }
            Assert.That(count, Is.EqualTo(5));
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

        private static void AssertOpaqueAndSafe(Color32[] pixels)
        {
            var nonOpaquePixels = 0;
            var dangerPixels = 0;
            for (var index = 0; index < pixels.Length; index++)
            {
                var pixel = pixels[index];
                if (pixel.a != byte.MaxValue) nonOpaquePixels++;
                if (pixel.r == 0xE4 && pixel.g == 0x5D && pixel.b == 0x45) dangerPixels++;
            }
            Assert.That(nonOpaquePixels, Is.Zero);
            Assert.That(dangerPixels, Is.Zero);
        }

        private static HashSet<ulong> TileSignatures(
            Color32[] pixels,
            int atlasWidth,
            int tileSize,
            int columns,
            int rows)
        {
            var signatures = new HashSet<ulong>();
            for (var row = 0; row < rows; row++)
            {
                for (var column = 0; column < columns; column++)
                {
                    var hash = 1469598103934665603UL;
                    for (var y = 0; y < tileSize; y++)
                    {
                        var start = (row * tileSize + y) * atlasWidth + column * tileSize;
                        hash = HashPixels(pixels, start, tileSize, hash);
                    }
                    signatures.Add(hash);
                }
            }
            return signatures;
        }

        private static ulong HashPixels(Color32[] pixels, int start, int count, ulong seed)
        {
            var hash = seed;
            for (var index = start; index < start + count; index++)
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

        private static string SourcePath(string region) => AssetRoot + "/source/" + region + "-imagegen.png";
        private static string WorkingPath(string region) => AssetRoot + "/working/" + region + "-tile-kit-master.png";
        private static string FinalPath(string region) => AssetRoot + "/final/" + region + "-tile-kit-atlas.png";
    }
}
