using System;
using System.Collections.Generic;
using System.IO;
using Game.Editor;
using Game.Presentation;
using Game.Simulation;
using NUnit.Framework;
using UnityEditor;
using UnityEditor.AddressableAssets;
using UnityEngine;

namespace Game.Tests.EditMode
{
    public sealed class QinglanG31ArtSkill002Tests
    {
        private const string AssetRoot = "Assets/GameAssets/FirstParty/QinglanDemo/ART-SKILL-002";
        private const string ProfileRoot = "Assets/GameContent/QinglanDemo/Profiles/Visual/ART-SKILL-002";

        private static readonly EvolutionVisualContract[] Visuals =
        {
            new EvolutionVisualContract("qinglan-flowing-shadow-sword", EntityKind.Area, 0.65f),
            new EvolutionVisualContract("taiyi-spirit-sealing-array", EntityKind.Area, 1.00f),
            new EvolutionVisualContract("chilu-hundred-craft-wheel", EntityKind.Projectile, 0.22f),
            new EvolutionVisualContract("mirror-sea-tide-wheel", EntityKind.Area, 0.85f),
            new EvolutionVisualContract("mountain-boundary-seal", EntityKind.Area, 1.10f),
            new EvolutionVisualContract("earth-vein-spring-branch", EntityKind.Area, 1.00f)
        };

        [Test]
        public void SixEvolutionTexturesUseCenteredVfxImportAndReleaseEntries()
        {
            for (var index = 0; index < Visuals.Length; index++)
            {
                var visual = Visuals[index];
                var texture = AssetDatabase.LoadAssetAtPath<Texture2D>(visual.FinalPath);
                Assert.That(texture, Is.Not.Null, visual.FinalPath);
                Assert.That(texture.width, Is.EqualTo(1024));
                Assert.That(texture.height, Is.EqualTo(1024));
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
                var sprite = FindSprite(visual.FinalPath, visual.StableId);
                Assert.That(sprite, Is.Not.Null);
                Assert.That(sprite.pivot.x, Is.EqualTo(512f).Within(0.01f));
                Assert.That(sprite.pivot.y, Is.EqualTo(512f).Within(0.01f));
                AssertReleaseEntry(visual.FinalPath, visual.TextureAddress);
            }
        }

        [Test]
        public void SixEvolutionProfilesUseStableIdsKindsAndApprovedSprites()
        {
            for (var index = 0; index < Visuals.Length; index++)
            {
                var visual = Visuals[index];
                var profile = AssetDatabase.LoadAssetAtPath<VisualProfile>(visual.ProfilePath);
                Assert.That(profile, Is.Not.Null, visual.ProfilePath);
                Assert.That(profile.StableId, Is.EqualTo(visual.StableId));
                Assert.That(profile.EntityKind, Is.EqualTo(visual.EntityKind));
                Assert.That(profile.Size.x, Is.EqualTo(visual.Scale).Within(0.0001f));
                Assert.That(profile.Size.y, Is.EqualTo(visual.Scale).Within(0.0001f));
                Assert.That(profile.Sprite, Is.Not.Null);
                Assert.That(profile.Sprite.name, Is.EqualTo(visual.StableId));
                AssertReleaseEntry(visual.ProfilePath, visual.ProfileAddress);
            }
        }

        [Test]
        public void SourceAndFinalGeometryMeetEvolutionReadabilityBudget()
        {
            var projectRoot = Directory.GetParent(UnityEngine.Application.dataPath)?.FullName ?? string.Empty;
            var boundsSignatures = new HashSet<string>(StringComparer.Ordinal);
            for (var index = 0; index < Visuals.Length; index++)
            {
                var visual = Visuals[index];
                var source = LoadPng(Path.Combine(projectRoot, visual.SourcePath));
                try
                {
                    Assert.That(source.width, Is.EqualTo(2048));
                    Assert.That(source.height, Is.EqualTo(2048));
                }
                finally
                {
                    UnityEngine.Object.DestroyImmediate(source);
                }

                var final = LoadPng(Path.Combine(projectRoot, visual.FinalPath));
                try
                {
                    var pixels = final.GetPixels32();
                    Assert.That(pixels[0].a, Is.Zero);
                    Assert.That(pixels[1023].a, Is.Zero);
                    Assert.That(pixels[1023 * 1024].a, Is.Zero);
                    Assert.That(pixels[pixels.Length - 1].a, Is.Zero);
                    var bounds = AlphaBounds(pixels, 1024, out var covered, out var deepPixels, out var dangerPixels);
                    Assert.That(bounds.xMin, Is.GreaterThanOrEqualTo(40));
                    Assert.That(bounds.yMin, Is.GreaterThanOrEqualTo(40));
                    Assert.That(1024 - bounds.xMax, Is.GreaterThanOrEqualTo(40));
                    Assert.That(1024 - bounds.yMax, Is.GreaterThanOrEqualTo(40));
                    var coverage = covered / (1024f * 1024f);
                    Assert.That(coverage, Is.GreaterThanOrEqualTo(0.10f));
                    Assert.That(coverage, Is.LessThanOrEqualTo(0.35f));
                    Assert.That(deepPixels, Is.GreaterThan(250));
                    Assert.That(dangerPixels, Is.Zero);
                    boundsSignatures.Add(bounds.width + "x" + bounds.height);
                }
                finally
                {
                    UnityEngine.Object.DestroyImmediate(final);
                }
            }
            Assert.That(boundsSignatures.Count, Is.EqualTo(Visuals.Length));
        }

        [Test]
        public void OnlyEvolutionFinalTexturesAndProfilesAreAddressable()
        {
            var settings = AddressableAssetSettingsDefaultObject.GetSettings(false);
            Assert.That(settings, Is.Not.Null);
            AssertAddressableScope(settings, AssetRoot, "/final/", 6);
            AssertAddressableScope(settings, ProfileRoot, "-visual-profile.asset", 6);
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

        private static void AssertAddressableScope(
            UnityEditor.AddressableAssets.Settings.AddressableAssetSettings settings,
            string root,
            string requiredPathPart,
            int expectedCount)
        {
            var count = 0;
            var guids = AssetDatabase.FindAssets(string.Empty, new[] { root });
            for (var index = 0; index < guids.Length; index++)
            {
                var entry = settings.FindAssetEntry(guids[index]);
                if (entry == null) continue;
                var path = AssetDatabase.GUIDToAssetPath(guids[index]).Replace('\\', '/');
                Assert.That(path, Does.Contain(requiredPathPart));
                count++;
            }
            Assert.That(count, Is.EqualTo(expectedCount));
        }

        private static Texture2D LoadPng(string path)
        {
            var texture = new Texture2D(2, 2, TextureFormat.RGBA32, false);
            Assert.That(ImageConversion.LoadImage(texture, File.ReadAllBytes(path), false), Is.True, path);
            return texture;
        }

        private static RectInt AlphaBounds(
            Color32[] pixels,
            int width,
            out int covered,
            out int deepPixels,
            out int dangerPixels)
        {
            var minimumX = width;
            var minimumY = width;
            var maximumX = -1;
            var maximumY = -1;
            covered = 0;
            deepPixels = 0;
            dangerPixels = 0;
            for (var y = 0; y < width; y++)
            {
                for (var x = 0; x < width; x++)
                {
                    var pixel = pixels[y * width + x];
                    if (pixel.a < 16) continue;
                    covered++;
                    if (pixel.r == 0x16 && pixel.g == 0x3D && pixel.b == 0x45) deepPixels++;
                    if (pixel.r == 0xE4 && pixel.g == 0x5D && pixel.b == 0x45) dangerPixels++;
                    minimumX = Math.Min(minimumX, x);
                    minimumY = Math.Min(minimumY, y);
                    maximumX = Math.Max(maximumX, x);
                    maximumY = Math.Max(maximumY, y);
                }
            }
            return new RectInt(minimumX, minimumY, maximumX - minimumX + 1, maximumY - minimumY + 1);
        }

        private static string JoinIssues(IReadOnlyList<ValidationIssue> issues)
        {
            var value = string.Empty;
            for (var index = 0; index < issues.Count; index++)
                value += (index == 0 ? string.Empty : "\n") + issues[index];
            return value;
        }

        private sealed class EvolutionVisualContract
        {
            public EvolutionVisualContract(string name, EntityKind entityKind, float scale)
            {
                Name = name;
                EntityKind = entityKind;
                Scale = scale;
            }

            public string Name { get; }
            public EntityKind EntityKind { get; }
            public float Scale { get; }
            public string StableId => "qinglan.presentation.skill.evolved." + Name.Replace('-', '_');
            public string SourcePath => AssetRoot + "/source/" + Name + "-visual-source.png";
            public string FinalPath => AssetRoot + "/final/" + Name + "-visual.png";
            public string ProfilePath => ProfileRoot + "/" + Name + "-visual-profile.asset";
            public string TextureAddress => "qinglan/skill/evolved/" + Name + "/vfx";
            public string ProfileAddress => "qinglan/profile/skill/evolved/" + Name;
        }
    }
}
