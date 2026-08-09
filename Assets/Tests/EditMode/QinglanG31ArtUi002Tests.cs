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
    public sealed class QinglanG31ArtUi002Tests
    {
        private const string AssetRoot = "Assets/GameAssets/FirstParty/QinglanDemo/ART-UI-002";
        private const string AtlasPath = AssetRoot + "/final/ui-framework-atlas.png";
        private const string Prefix = "qinglan.presentation.ui.framework.";

        private static readonly string[] Names =
        {
            "frame.standard", "frame.focused", "frame.disabled", "frame.danger",
            "panel.solid", "panel.translucent", "panel.card", "panel.tooltip",
            "icon.health", "icon.shield", "icon.experience", "icon.level",
            "icon.time", "icon.objective", "icon.map", "icon.lock"
        };

        [Test]
        public void AtlasUsesSixteenSemanticSpritesNineSliceBordersAndReleaseEntry()
        {
            var texture = AssetDatabase.LoadAssetAtPath<Texture2D>(AtlasPath);
            Assert.That(texture, Is.Not.Null);
            Assert.That(texture.width, Is.EqualTo(2048));
            Assert.That(texture.height, Is.EqualTo(2048));

            var importer = AssetImporter.GetAtPath(AtlasPath) as TextureImporter;
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

            var sprites = FindSprites(AtlasPath);
            Assert.That(sprites.Count, Is.EqualTo(16));
            for (var index = 0; index < Names.Length; index++)
            {
                var name = Prefix + Names[index];
                Assert.That(sprites.ContainsKey(name), Is.True, name);
                var sprite = sprites[name];
                var row = index / 4;
                var column = index % 4;
                Assert.That(sprite.rect, Is.EqualTo(new Rect(column * 512, (3 - row) * 512, 512, 512)));
                Assert.That(sprite.pivot, Is.EqualTo(new Vector2(256f, 256f)));
                Assert.That(sprite.border, Is.EqualTo(row < 2 ? new Vector4(64f, 64f, 64f, 64f) : Vector4.zero));
            }

            AssertReleaseEntry(AtlasPath, "qinglan/ui/framework/atlas");
        }

        [Test]
        public void SourceAndFinalMeetAlphaGeometryDangerColorAndDistinctnessBudgets()
        {
            var projectRoot = Directory.GetParent(UnityEngine.Application.dataPath)?.FullName ?? string.Empty;
            AssertAtlas(
                Path.Combine(projectRoot, AssetRoot + "/source/ui-framework-atlas-source.png"),
                4096,
                56,
                160);
            AssertAtlas(Path.Combine(projectRoot, AtlasPath), 2048, 28, 80);
        }

        [Test]
        public void OnlyApprovedFinalAtlasIsAddressable()
        {
            var settings = AddressableAssetSettingsDefaultObject.GetSettings(false);
            Assert.That(settings, Is.Not.Null);
            var count = 0;
            var guids = AssetDatabase.FindAssets(string.Empty, new[] { AssetRoot });
            for (var index = 0; index < guids.Length; index++)
            {
                var entry = settings.FindAssetEntry(guids[index]);
                if (entry == null) continue;
                Assert.That(AssetDatabase.GUIDToAssetPath(guids[index]).Replace('\\', '/'), Is.EqualTo(AtlasPath));
                count++;
            }
            Assert.That(count, Is.EqualTo(1));
        }

        private static void AssertAtlas(string path, int size, int presentationMargin, int iconMargin)
        {
            var texture = LoadPng(path);
            try
            {
                Assert.That(texture.width, Is.EqualTo(size));
                Assert.That(texture.height, Is.EqualTo(size));
                var pixels = texture.GetPixels32();
                var cellSize = size / 4;
                var colorHashes = new HashSet<ulong>();
                var grayscaleHashes = new HashSet<ulong>();
                for (var index = 0; index < Names.Length; index++)
                {
                    var row = index / 4;
                    var column = index % 4;
                    var minimumMargin = row >= 2 ? iconMargin : presentationMargin;
                    var metrics = AnalyzeCell(pixels, size, cellSize, row, column);
                    Assert.That(metrics.Margin, Is.GreaterThanOrEqualTo(minimumMargin), Names[index]);
                    Assert.That(metrics.Coverage, Is.InRange(
                        row == 0 ? 0.04f : row == 1 ? 0.58f : 0.05f,
                        row == 0 ? 0.36f : row == 1 ? 0.88f : 0.42f), Names[index]);
                    Assert.That(metrics.MagentaPixels, Is.Zero, Names[index]);
                    Assert.That(metrics.DangerPixels, index == 3 ? Is.GreaterThan(0) : Is.Zero, Names[index]);
                    Assert.That(metrics.TopLeftAlpha, Is.Zero, Names[index]);
                    Assert.That(metrics.BottomRightAlpha, Is.Zero, Names[index]);
                    if (row == 0)
                        Assert.That(metrics.CenterAlpha, Is.Zero, Names[index]);
                    else if (row == 1)
                        Assert.That(metrics.CenterAlpha, Is.GreaterThanOrEqualTo(180), Names[index]);
                    colorHashes.Add(metrics.ColorHash);
                    grayscaleHashes.Add(metrics.GrayscaleHash);
                }
                Assert.That(colorHashes.Count, Is.EqualTo(16));
                Assert.That(grayscaleHashes.Count, Is.EqualTo(16));
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(texture);
            }
        }

        private static CellMetrics AnalyzeCell(Color32[] pixels, int atlasSize, int cellSize, int row, int column)
        {
            var minX = cellSize;
            var minY = cellSize;
            var maxX = -1;
            var maxY = -1;
            var covered = 0;
            var magenta = 0;
            var danger = 0;
            var colorHash = 1469598103934665603UL;
            var grayscaleHash = 1469598103934665603UL;
            var originX = column * cellSize;
            var originY = (3 - row) * cellSize;
            for (var y = 0; y < cellSize; y++)
            {
                for (var x = 0; x < cellSize; x++)
                {
                    var pixel = pixels[(originY + y) * atlasSize + originX + x];
                    if (pixel.a >= 16)
                    {
                        covered++;
                        minX = Math.Min(minX, x);
                        minY = Math.Min(minY, y);
                        maxX = Math.Max(maxX, x);
                        maxY = Math.Max(maxY, y);
                        if (pixel.r >= 220 && pixel.b >= 180 && pixel.g <= 70) magenta++;
                        if (pixel.r == 0xE4 && pixel.g == 0x5D && pixel.b == 0x45) danger++;
                    }
                    colorHash = Hash(colorHash, pixel.r, pixel.g, pixel.b, pixel.a);
                    var luma = (byte)((pixel.r * 54 + pixel.g * 183 + pixel.b * 19) >> 8);
                    grayscaleHash = Hash(grayscaleHash, luma, pixel.a);
                }
            }
            Assert.That(covered, Is.GreaterThan(0), Names[row * 4 + column]);
            var margin = Math.Min(Math.Min(minX, minY), Math.Min(cellSize - maxX - 1, cellSize - maxY - 1));
            return new CellMetrics(
                margin,
                covered / (float)(cellSize * cellSize),
                magenta,
                danger,
                pixels[originY * atlasSize + originX].a,
                pixels[(originY + cellSize - 1) * atlasSize + originX + cellSize - 1].a,
                pixels[(originY + cellSize / 2) * atlasSize + originX + cellSize / 2].a,
                colorHash,
                grayscaleHash);
        }

        private static ulong Hash(ulong hash, params byte[] values)
        {
            for (var index = 0; index < values.Length; index++)
                hash = (hash ^ values[index]) * 1099511628211UL;
            return hash;
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
            Assert.That(entry, Is.Not.Null);
            Assert.That(entry.parentGroup.Name, Is.EqualTo(AssetProvenanceValidator.QinglanVisualGroup));
            Assert.That(entry.address, Is.EqualTo(address));
            Assert.That(entry.labels, Does.Contain(AssetProvenanceValidator.QinglanPackLabel));
            Assert.That(entry.labels, Does.Contain(AssetProvenanceValidator.ReleaseLabel));
            Assert.That(entry.labels, Does.Contain(AssetProvenanceValidator.VisualReleaseLabel));
            var projectRoot = Directory.GetParent(UnityEngine.Application.dataPath)?.FullName;
            var issues = AssetProvenanceValidator.ValidateReleaseInput(projectRoot, path, entry.labels, entry.parentGroup.Name);
            Assert.That(issues, Is.Empty, JoinIssues(issues));
        }

        private static string JoinIssues(IReadOnlyList<ValidationIssue> issues)
        {
            var value = string.Empty;
            for (var index = 0; index < issues.Count; index++)
                value += (index == 0 ? string.Empty : "\n") + issues[index];
            return value;
        }

        private static Texture2D LoadPng(string path)
        {
            var texture = new Texture2D(2, 2, TextureFormat.RGBA32, false);
            Assert.That(ImageConversion.LoadImage(texture, File.ReadAllBytes(path), false), Is.True, path);
            return texture;
        }

        private readonly struct CellMetrics
        {
            public CellMetrics(int margin, float coverage, int magentaPixels, int dangerPixels,
                byte topLeftAlpha, byte bottomRightAlpha, byte centerAlpha, ulong colorHash, ulong grayscaleHash)
            {
                Margin = margin;
                Coverage = coverage;
                MagentaPixels = magentaPixels;
                DangerPixels = dangerPixels;
                TopLeftAlpha = topLeftAlpha;
                BottomRightAlpha = bottomRightAlpha;
                CenterAlpha = centerAlpha;
                ColorHash = colorHash;
                GrayscaleHash = grayscaleHash;
            }

            public int Margin { get; }
            public float Coverage { get; }
            public int MagentaPixels { get; }
            public int DangerPixels { get; }
            public byte TopLeftAlpha { get; }
            public byte BottomRightAlpha { get; }
            public byte CenterAlpha { get; }
            public ulong ColorHash { get; }
            public ulong GrayscaleHash { get; }
        }
    }
}
