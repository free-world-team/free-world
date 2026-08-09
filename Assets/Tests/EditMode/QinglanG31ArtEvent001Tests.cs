using System;
using System.Collections.Generic;
using System.IO;
using Game.Editor;
using Game.Presentation;
using Game.Simulation;
using NUnit.Framework;
using UnityEditor;
using UnityEditor.AddressableAssets;
using UnityEditor.AddressableAssets.Settings;
using UnityEngine;

namespace Game.Tests.EditMode
{
    public sealed class QinglanG31ArtEvent001Tests
    {
        private const string AssetRoot = "Assets/GameAssets/AI/QinglanDemo/ART-EVENT-001";
        private const string ProfileRoot = "Assets/GameContent/QinglanDemo/Profiles/Visual/ART-EVENT-001";

        private static readonly EventVisualContract[] Events =
        {
            new EventVisualContract("wind-vein-riot", "wind_vein_riot", 3.0f),
            new EventVisualContract("herb-garden-revival", "herb_garden_revival", 3.2f),
            new EventVisualContract("old-sword-resonance", "old_sword_resonance", 3.0f)
        };

        [Test]
        public void ThreeEventAtlasesUseSemanticPhaseImportsAndReleaseEntries()
        {
            for (var eventIndex = 0; eventIndex < Events.Length; eventIndex++)
            {
                var visual = Events[eventIndex];
                var texture = AssetDatabase.LoadAssetAtPath<Texture2D>(visual.FinalPath);
                Assert.That(texture, Is.Not.Null, visual.FinalPath);
                Assert.That(texture.width, Is.EqualTo(1024));
                Assert.That(texture.height, Is.EqualTo(256));

                var importer = AssetImporter.GetAtPath(visual.FinalPath) as TextureImporter;
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

                var sprites = FindSprites(visual.FinalPath);
                Assert.That(sprites.Count, Is.EqualTo(4));
                for (var frame = 0; frame < 4; frame++)
                {
                    var spriteName = visual.PresentationPrefix + ".frame-" + frame;
                    Assert.That(sprites.ContainsKey(spriteName), Is.True, spriteName);
                    var sprite = sprites[spriteName];
                    Assert.That(sprite.rect.width, Is.EqualTo(256f));
                    Assert.That(sprite.rect.height, Is.EqualTo(256f));
                    Assert.That(sprite.pivot.x, Is.EqualTo(128f).Within(0.01f));
                    Assert.That(sprite.pivot.y, Is.EqualTo(128f).Within(0.01f));
                }
                AssertReleaseEntry(visual.FinalPath, visual.TextureAddress);
            }
        }

        [Test]
        public void ThreeEventProfilesUseStableIdsAreaKindsAndApprovedFrameZeroSprites()
        {
            for (var eventIndex = 0; eventIndex < Events.Length; eventIndex++)
            {
                var visual = Events[eventIndex];
                var profile = AssetDatabase.LoadAssetAtPath<VisualProfile>(visual.ProfilePath);
                Assert.That(profile, Is.Not.Null, visual.ProfilePath);
                Assert.That(profile.StableId, Is.EqualTo(visual.StableId));
                Assert.That(profile.EntityKind, Is.EqualTo(EntityKind.Area));
                Assert.That(profile.Size.x, Is.EqualTo(visual.Scale).Within(0.0001f));
                Assert.That(profile.Size.y, Is.EqualTo(visual.Scale).Within(0.0001f));
                Assert.That(profile.Sprite, Is.Not.Null);
                Assert.That(profile.Sprite.name, Is.EqualTo(visual.PresentationPrefix + ".frame-0"));
                AssertReleaseEntry(visual.ProfilePath, visual.ProfileAddress);
            }
        }

        [Test]
        public void EventSourcesWorkingAndFinalsMeetGeometryAlphaAndPhaseBudgets()
        {
            var projectRoot = Directory.GetParent(UnityEngine.Application.dataPath)?.FullName ?? string.Empty;
            var atlasHashes = new HashSet<ulong>();
            for (var eventIndex = 0; eventIndex < Events.Length; eventIndex++)
            {
                var visual = Events[eventIndex];
                AssertSquareAtLeast(projectRoot, visual.SourcePath, 1024);

                var transparent = LoadPng(Path.Combine(projectRoot, visual.TransparentPath));
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

                var sourceMaster = LoadPng(Path.Combine(projectRoot, visual.SourceMasterPath));
                try
                {
                    Assert.That(sourceMaster.width, Is.EqualTo(2048));
                    Assert.That(sourceMaster.height, Is.EqualTo(2048));
                }
                finally
                {
                    UnityEngine.Object.DestroyImmediate(sourceMaster);
                }

                var atlasMaster = LoadPng(Path.Combine(projectRoot, visual.AtlasMasterPath));
                try
                {
                    Assert.That(atlasMaster.width, Is.EqualTo(2048));
                    Assert.That(atlasMaster.height, Is.EqualTo(512));
                    AssertCornersTransparent(atlasMaster.GetPixels32(), atlasMaster.width, atlasMaster.height);
                }
                finally
                {
                    UnityEngine.Object.DestroyImmediate(atlasMaster);
                }

                var final = LoadPng(Path.Combine(projectRoot, visual.FinalPath));
                try
                {
                    Assert.That(final.width, Is.EqualTo(1024));
                    Assert.That(final.height, Is.EqualTo(256));
                    var pixels = final.GetPixels32();
                    AssertCornersTransparent(pixels, final.width, final.height);
                    Assert.That(CountMagenta(pixels), Is.Zero);
                    Assert.That(CountDangerRed(pixels), Is.Zero);
                    var metrics = AnalyzeFrames(pixels, final.width, 256);
                    Assert.That(metrics.UniqueHashes, Is.EqualTo(4));
                    Assert.That(metrics.MinimumMargin, Is.GreaterThanOrEqualTo(14));
                    Assert.That(metrics.MinimumCoverage, Is.GreaterThanOrEqualTo(0.20f));
                    Assert.That(metrics.MaximumCoverage, Is.LessThanOrEqualTo(0.30f));
                    Assert.That(metrics.MinimumPhaseColorPixels, Is.GreaterThan(150));
                    atlasHashes.Add(HashPixels(pixels, 0, pixels.Length, 1469598103934665603UL));
                }
                finally
                {
                    UnityEngine.Object.DestroyImmediate(final);
                }
            }
            Assert.That(atlasHashes.Count, Is.EqualTo(Events.Length));
        }

        [Test]
        public void OnlyEventFinalTexturesAndProfilesAreAddressable()
        {
            var settings = AddressableAssetSettingsDefaultObject.GetSettings(false);
            Assert.That(settings, Is.Not.Null);
            AssertAddressableScope(settings, AssetRoot, "/final/", 3);
            AssertAddressableScope(settings, ProfileRoot, "-event-vfx-profile.asset", 3);
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

        private static void AssertAddressableScope(AddressableAssetSettings settings, string root, string marker, int expected)
        {
            var count = 0;
            var guids = AssetDatabase.FindAssets(string.Empty, new[] { root });
            for (var index = 0; index < guids.Length; index++)
            {
                var entry = settings.FindAssetEntry(guids[index]);
                if (entry == null) continue;
                var path = AssetDatabase.GUIDToAssetPath(guids[index]).Replace('\\', '/');
                Assert.That(path, Does.Contain(marker));
                count++;
            }
            Assert.That(count, Is.EqualTo(expected));
        }

        private static void AssertSquareAtLeast(string projectRoot, string relativePath, int minimum)
        {
            var texture = LoadPng(Path.Combine(projectRoot, relativePath));
            try
            {
                Assert.That(texture.width, Is.GreaterThanOrEqualTo(minimum));
                Assert.That(texture.height, Is.EqualTo(texture.width));
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(texture);
            }
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

        private static FrameMetrics AnalyzeFrames(Color32[] pixels, int atlasWidth, int cellSize)
        {
            var hashes = new HashSet<ulong>();
            var minimumMargin = cellSize;
            var minimumCoverage = 1f;
            var maximumCoverage = 0f;
            var minimumPhaseColorPixels = int.MaxValue;
            for (var frame = 0; frame < 4; frame++)
            {
                var hash = 1469598103934665603UL;
                var minX = cellSize;
                var minY = cellSize;
                var maxX = -1;
                var maxY = -1;
                var covered = 0;
                var phasePixels = 0;
                for (var y = 0; y < cellSize; y++)
                {
                    var start = y * atlasWidth + frame * cellSize;
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
                        if (pixel.g > pixel.r * 1.12f && pixel.g >= 100) phasePixels++;
                    }
                }
                Assert.That(covered, Is.GreaterThan(0));
                hashes.Add(hash);
                minimumMargin = Math.Min(minimumMargin, Math.Min(Math.Min(minX, minY), Math.Min(cellSize - maxX - 1, cellSize - maxY - 1)));
                var coverage = covered / (float)(cellSize * cellSize);
                minimumCoverage = Math.Min(minimumCoverage, coverage);
                maximumCoverage = Math.Max(maximumCoverage, coverage);
                minimumPhaseColorPixels = Math.Min(minimumPhaseColorPixels, phasePixels);
            }
            return new FrameMetrics(hashes.Count, minimumMargin, minimumCoverage, maximumCoverage, minimumPhaseColorPixels);
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

        private readonly struct EventVisualContract
        {
            public EventVisualContract(string fileName, string stableName, float scale)
            {
                FileName = fileName;
                StableName = stableName;
                Scale = scale;
            }

            public string FileName { get; }
            public string StableName { get; }
            public float Scale { get; }
            public string StableId => "qinglan.event." + StableName;
            public string PresentationPrefix => "qinglan.presentation.event.old_court." + StableName;
            public string SourcePath => AssetRoot + "/source/" + FileName + "-core-imagegen.png";
            public string TransparentPath => AssetRoot + "/working/" + FileName + "-core-transparent.png";
            public string SourceMasterPath => AssetRoot + "/working/" + FileName + "-source-master.png";
            public string AtlasMasterPath => AssetRoot + "/working/" + FileName + "-phase-atlas-master.png";
            public string FinalPath => AssetRoot + "/final/" + FileName + "-phase-atlas.png";
            public string ProfilePath => ProfileRoot + "/" + FileName + "-event-vfx-profile.asset";
            public string TextureAddress => "qinglan/event/" + FileName + "/phase-atlas";
            public string ProfileAddress => "qinglan/profile/event/" + FileName;
        }

        private readonly struct FrameMetrics
        {
            public FrameMetrics(int uniqueHashes, int minimumMargin, float minimumCoverage, float maximumCoverage, int minimumPhaseColorPixels)
            {
                UniqueHashes = uniqueHashes;
                MinimumMargin = minimumMargin;
                MinimumCoverage = minimumCoverage;
                MaximumCoverage = maximumCoverage;
                MinimumPhaseColorPixels = minimumPhaseColorPixels;
            }

            public int UniqueHashes { get; }
            public int MinimumMargin { get; }
            public float MinimumCoverage { get; }
            public float MaximumCoverage { get; }
            public int MinimumPhaseColorPixels { get; }
        }
    }
}
