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
    public sealed class QinglanG31ArtSkill001Tests
    {
        private const string AssetRoot =
            "Assets/GameAssets/FirstParty/QinglanDemo/ART-SKILL-001";
        private const string ProfileRoot =
            "Assets/GameContent/QinglanDemo/Profiles/Visual/ART-SKILL-001";

        private static readonly SkillVisualContract[] Visuals =
        {
            new SkillVisualContract("yufeng-sword", EntityKind.Projectile, 0.36f),
            new SkillVisualContract("yellow-talisman", EntityKind.Projectile, 0.25f),
            new SkillVisualContract("lihuo-wheel", EntityKind.Projectile, 0.30f),
            new SkillVisualContract("tide-orb", EntityKind.Area, 0.80f),
            new SkillVisualContract("zhenyue-seal", EntityKind.Area, 1.25f),
            new SkillVisualContract("spirit-vine-seed", EntityKind.Area, 1.10f)
        };

        [Test]
        public void SixFinalTexturesUseCenteredVfxImportAndReleaseEntries()
        {
            for (var index = 0; index < Visuals.Length; index++)
            {
                var visual = Visuals[index];
                var texture = AssetDatabase.LoadAssetAtPath<Texture2D>(visual.FinalPath);
                Assert.That(texture, Is.Not.Null, visual.FinalPath);
                Assert.That(texture.width, Is.EqualTo(512));
                Assert.That(texture.height, Is.EqualTo(512));

                var importer = AssetImporter.GetAtPath(visual.FinalPath) as TextureImporter;
                Assert.That(importer, Is.Not.Null);
                Assert.That(importer.textureType, Is.EqualTo(TextureImporterType.Sprite));
                Assert.That(importer.spriteImportMode, Is.EqualTo(SpriteImportMode.Multiple));
                Assert.That(importer.mipmapEnabled, Is.False);
                Assert.That(importer.alphaIsTransparency, Is.True);
                Assert.That(importer.wrapMode, Is.EqualTo(TextureWrapMode.Clamp));
                Assert.That(importer.maxTextureSize, Is.EqualTo(512));
                var standalone = importer.GetPlatformTextureSettings("Standalone");
                Assert.That(standalone.overridden, Is.True);
                Assert.That(standalone.format, Is.EqualTo(TextureImporterFormat.BC7));

                var sprite = FindSprite(visual.FinalPath, visual.StableId);
                Assert.That(sprite, Is.Not.Null);
                Assert.That(sprite.rect.width, Is.EqualTo(512f));
                Assert.That(sprite.rect.height, Is.EqualTo(512f));
                Assert.That(sprite.pivot.x, Is.EqualTo(256f).Within(0.01f));
                Assert.That(sprite.pivot.y, Is.EqualTo(256f).Within(0.01f));
                AssertReleaseEntry(visual.FinalPath, visual.TextureAddress);
            }
        }

        [Test]
        public void SixProfilesUseStableIdsEntityKindsAndApprovedSprites()
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
        public void SourceAndFinalGeometryMeetBaseWeaponReadabilityBudget()
        {
            var projectRoot = Directory.GetParent(UnityEngine.Application.dataPath)?.FullName ?? string.Empty;
            var boundsSignatures = new HashSet<string>(StringComparer.Ordinal);
            for (var index = 0; index < Visuals.Length; index++)
            {
                var visual = Visuals[index];
                using (var source = new TextureScope(LoadPng(Path.Combine(projectRoot, visual.SourcePath))))
                {
                    Assert.That(source.Texture.width, Is.EqualTo(1024));
                    Assert.That(source.Texture.height, Is.EqualTo(1024));
                }

                using (var final = new TextureScope(LoadPng(Path.Combine(projectRoot, visual.FinalPath))))
                {
                    var pixels = final.Texture.GetPixels32();
                    Assert.That(pixels[0].a, Is.Zero);
                    Assert.That(pixels[511].a, Is.Zero);
                    Assert.That(pixels[511 * 512].a, Is.Zero);
                    Assert.That(pixels[pixels.Length - 1].a, Is.Zero);
                    var bounds = AlphaBounds(pixels, 512, out var covered, out var deepPixels, out var dangerPixels);
                    Assert.That(bounds.xMin, Is.GreaterThanOrEqualTo(24));
                    Assert.That(bounds.yMin, Is.GreaterThanOrEqualTo(24));
                    Assert.That(512 - bounds.xMax, Is.GreaterThanOrEqualTo(24));
                    Assert.That(512 - bounds.yMax, Is.GreaterThanOrEqualTo(24));
                    var coverage = covered / (512f * 512f);
                    Assert.That(coverage, Is.GreaterThanOrEqualTo(0.08f));
                    Assert.That(coverage, Is.LessThanOrEqualTo(0.32f));
                    Assert.That(deepPixels, Is.GreaterThan(100));
                    Assert.That(dangerPixels, Is.Zero, "Friendly VFX must not use the exact P0 danger core.");
                    boundsSignatures.Add(bounds.width + "x" + bounds.height);
                }
            }

            Assert.That(boundsSignatures.Count, Is.EqualTo(Visuals.Length));
        }

        [Test]
        public void OnlyFinalTexturesAndFormalProfilesAreAddressable()
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
            var issues = AssetProvenanceValidator.ValidateReleaseInput(
                projectRoot,
                path,
                entry.labels,
                entry.parentGroup.Name);
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
            return new RectInt(
                minimumX,
                minimumY,
                maximumX - minimumX + 1,
                maximumY - minimumY + 1);
        }

        private static string JoinIssues(IReadOnlyList<ValidationIssue> issues)
        {
            var value = string.Empty;
            for (var index = 0; index < issues.Count; index++)
                value += (index == 0 ? string.Empty : "\n") + issues[index];
            return value;
        }

        private sealed class TextureScope : IDisposable
        {
            public TextureScope(Texture2D texture) => Texture = texture;
            public Texture2D Texture { get; }
            public void Dispose() => UnityEngine.Object.DestroyImmediate(Texture);
        }

        private sealed class SkillVisualContract
        {
            public SkillVisualContract(string name, EntityKind entityKind, float scale)
            {
                Name = name;
                EntityKind = entityKind;
                Scale = scale;
            }

            public string Name { get; }
            public EntityKind EntityKind { get; }
            public float Scale { get; }
            public string StableId => "qinglan.presentation.skill." + Name.Replace('-', '_');
            public string SourcePath => AssetRoot + "/source/" + Name + "-visual-source.png";
            public string FinalPath => AssetRoot + "/final/" + Name + "-visual.png";
            public string ProfilePath => ProfileRoot + "/" + Name + "-visual-profile.asset";
            public string TextureAddress => "qinglan/skill/base/" + Name + "/vfx";
            public string ProfileAddress => "qinglan/profile/skill/base/" + Name;
        }
    }
}
