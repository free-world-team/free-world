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
    public sealed class QinglanG31ArtObjective001Tests
    {
        private const string AssetRoot = "Assets/GameAssets/AI/QinglanDemo/ART-OBJECTIVE-001";

        private static readonly string[] Altars =
        {
            "listen-wind-altar",
            "guide-wind-altar",
            "stop-balance-wind-altar"
        };

        private static readonly string[] StableNames = { "listen", "guide", "stop_balance" };
        private static readonly string[] States = { "idle", "active", "complete" };

        [Test]
        public void ThreeWindAltarAtlasesUseSemanticStateImportsAndReleaseEntries()
        {
            for (var altarIndex = 0; altarIndex < Altars.Length; altarIndex++)
            {
                var altar = Altars[altarIndex];
                var stableName = StableNames[altarIndex];
                var path = FinalPath(altar);
                var texture = AssetDatabase.LoadAssetAtPath<Texture2D>(path);
                Assert.That(texture, Is.Not.Null, path);
                Assert.That(texture.width, Is.EqualTo(960));
                Assert.That(texture.height, Is.EqualTo(320));

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

                var prefix = "qinglan.presentation.objective.old_court." + stableName;
                var sprites = FindSprites(path);
                Assert.That(sprites.Count, Is.EqualTo(3));
                for (var stateIndex = 0; stateIndex < States.Length; stateIndex++)
                {
                    var spriteName = prefix + "." + States[stateIndex];
                    Assert.That(sprites.ContainsKey(spriteName), Is.True, spriteName);
                    var sprite = sprites[spriteName];
                    Assert.That(sprite.rect.width, Is.EqualTo(320f));
                    Assert.That(sprite.rect.height, Is.EqualTo(320f));
                    Assert.That(sprite.pivot.x, Is.EqualTo(160f).Within(0.01f));
                    Assert.That(sprite.pivot.y, Is.EqualTo(160f).Within(0.01f));
                }
                AssertReleaseEntry(path, "qinglan/objective/wind-altar/" + stableName.Replace('_', '-') + "/state-atlas");
            }
        }

        [Test]
        public void ApprovedSourcesAndStateAtlasesMeetGeometryAlphaAndProgressionBudgets()
        {
            var projectRoot = Directory.GetParent(UnityEngine.Application.dataPath)?.FullName ?? string.Empty;
            var atlasHashes = new HashSet<ulong>();
            for (var altarIndex = 0; altarIndex < Altars.Length; altarIndex++)
            {
                var altar = Altars[altarIndex];
                var source = LoadPng(Path.Combine(projectRoot, SourcePath(altar)));
                try
                {
                    Assert.That(source.width, Is.GreaterThanOrEqualTo(1024));
                    Assert.That(source.height, Is.EqualTo(source.width));
                }
                finally
                {
                    UnityEngine.Object.DestroyImmediate(source);
                }

                var transparent = LoadPng(Path.Combine(projectRoot, TransparentPath(altar)));
                try
                {
                    Assert.That(transparent.width, Is.GreaterThanOrEqualTo(1024));
                    Assert.That(transparent.height, Is.EqualTo(transparent.width));
                    var pixels = transparent.GetPixels32();
                    AssertCornersTransparent(pixels, transparent.width, transparent.height);
                    Assert.That(CountMagenta(pixels), Is.Zero);
                }
                finally
                {
                    UnityEngine.Object.DestroyImmediate(transparent);
                }

                var sourceMaster = LoadPng(Path.Combine(projectRoot, SourceMasterPath(altar)));
                try
                {
                    Assert.That(sourceMaster.width, Is.EqualTo(2048));
                    Assert.That(sourceMaster.height, Is.EqualTo(2048));
                }
                finally
                {
                    UnityEngine.Object.DestroyImmediate(sourceMaster);
                }

                var atlasMaster = LoadPng(Path.Combine(projectRoot, AtlasMasterPath(altar)));
                try
                {
                    Assert.That(atlasMaster.width, Is.EqualTo(1920));
                    Assert.That(atlasMaster.height, Is.EqualTo(640));
                    AssertCornersTransparent(atlasMaster.GetPixels32(), atlasMaster.width, atlasMaster.height);
                }
                finally
                {
                    UnityEngine.Object.DestroyImmediate(atlasMaster);
                }

                var final = LoadPng(Path.Combine(projectRoot, FinalPath(altar)));
                try
                {
                    Assert.That(final.width, Is.EqualTo(960));
                    Assert.That(final.height, Is.EqualTo(320));
                    var pixels = final.GetPixels32();
                    AssertCornersTransparent(pixels, final.width, final.height);
                    Assert.That(CountMagenta(pixels), Is.Zero);
                    Assert.That(CountDangerRed(pixels), Is.Zero);
                    var metrics = AnalyzeStates(pixels, final.width, 320);
                    Assert.That(metrics.UniqueHashes, Is.EqualTo(3));
                    Assert.That(metrics.MinimumMargin, Is.GreaterThanOrEqualTo(19));
                    Assert.That(metrics.MinimumCoverage, Is.GreaterThanOrEqualTo(0.40f));
                    Assert.That(metrics.MaximumCoverage, Is.LessThanOrEqualTo(0.65f));
                    Assert.That(metrics.TealPixels[1], Is.GreaterThan(metrics.TealPixels[0]));
                    Assert.That(metrics.TealPixels[2], Is.GreaterThan(metrics.TealPixels[0]));
                    atlasHashes.Add(HashPixels(pixels, 0, pixels.Length, 1469598103934665603UL));
                }
                finally
                {
                    UnityEngine.Object.DestroyImmediate(final);
                }
            }

            Assert.That(atlasHashes.Count, Is.EqualTo(Altars.Length));
        }

        [Test]
        public void OnlyApprovedFinalStateAtlasesAreAddressable()
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
                Assert.That(path, Does.EndWith("-state-atlas.png"));
                Assert.That(path, Does.Not.Contain("/rejected/"));
                count++;
            }
            Assert.That(count, Is.EqualTo(3));
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

        private static StateMetrics AnalyzeStates(Color32[] pixels, int atlasWidth, int cellSize)
        {
            var hashes = new HashSet<ulong>();
            var minimumMargin = cellSize;
            var minimumCoverage = 1f;
            var maximumCoverage = 0f;
            var tealPixels = new int[3];
            for (var state = 0; state < 3; state++)
            {
                var hash = 1469598103934665603UL;
                var minX = cellSize;
                var minY = cellSize;
                var maxX = -1;
                var maxY = -1;
                var covered = 0;
                for (var y = 0; y < cellSize; y++)
                {
                    var start = y * atlasWidth + state * cellSize;
                    hash = HashPixels(pixels, start, cellSize, hash);
                    for (var x = 0; x < cellSize; x++)
                    {
                        var pixel = pixels[start + x];
                        if (pixel.a < 16) continue;
                        covered++;
                        minX = Math.Min(minX, x);
                        minY = Math.Min(minY, y);
                        maxX = Math.Max(maxX, x);
                        maxY = Math.Max(maxY, y);
                        if (pixel.g > pixel.r * 1.15f && pixel.b > pixel.r * 1.15f && pixel.g >= 100)
                            tealPixels[state]++;
                    }
                }
                Assert.That(covered, Is.GreaterThan(0));
                hashes.Add(hash);
                minimumMargin = Math.Min(minimumMargin, Math.Min(Math.Min(minX, minY), Math.Min(cellSize - maxX - 1, cellSize - maxY - 1)));
                var coverage = covered / (float)(cellSize * cellSize);
                minimumCoverage = Math.Min(minimumCoverage, coverage);
                maximumCoverage = Math.Max(maximumCoverage, coverage);
            }
            return new StateMetrics(hashes.Count, minimumMargin, minimumCoverage, maximumCoverage, tealPixels);
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

        private static string SourcePath(string altar) => AssetRoot + "/source/" + altar + "-imagegen.png";
        private static string TransparentPath(string altar) => AssetRoot + "/working/" + altar + "-transparent.png";
        private static string SourceMasterPath(string altar) => AssetRoot + "/working/" + altar + "-source-master.png";
        private static string AtlasMasterPath(string altar) => AssetRoot + "/working/" + altar + "-state-atlas-master.png";
        private static string FinalPath(string altar) => AssetRoot + "/final/" + altar + "-state-atlas.png";

        private readonly struct StateMetrics
        {
            public StateMetrics(int uniqueHashes, int minimumMargin, float minimumCoverage, float maximumCoverage, int[] tealPixels)
            {
                UniqueHashes = uniqueHashes;
                MinimumMargin = minimumMargin;
                MinimumCoverage = minimumCoverage;
                MaximumCoverage = maximumCoverage;
                TealPixels = tealPixels;
            }

            public int UniqueHashes { get; }
            public int MinimumMargin { get; }
            public float MinimumCoverage { get; }
            public float MaximumCoverage { get; }
            public int[] TealPixels { get; }
        }
    }
}
