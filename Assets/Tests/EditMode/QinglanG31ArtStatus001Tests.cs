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
    public sealed class QinglanG31ArtStatus001Tests
    {
        private const string AssetRoot = "Assets/GameAssets/FirstParty/QinglanDemo/ART-STATUS-001";

        private static readonly StatusVisualContract[] Visuals =
        {
            Status("burning"),
            Status("poisoned"),
            Status("slowed"),
            Status("rooted"),
            Status("armor-broken"),
            Status("marked"),
            Status("damage-immunity"),
            Policy("contact-protection"),
            Policy("boss-hazard", true)
        };

        [Test]
        public void NineStatusAndDamagePolicyTexturesUseCenteredVfxImportAndReleaseEntries()
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
                Assert.That(sprite.pivot.x, Is.EqualTo(256f).Within(0.01f));
                Assert.That(sprite.pivot.y, Is.EqualTo(256f).Within(0.01f));
                AssertReleaseEntry(visual.FinalPath, visual.Address);
            }
        }

        [Test]
        public void SourceAndFinalGeometryMeetStatusReadabilityAndDangerColorBudgets()
        {
            var projectRoot = Directory.GetParent(UnityEngine.Application.dataPath)?.FullName ?? string.Empty;
            var boundsSignatures = new HashSet<string>(StringComparer.Ordinal);
            for (var index = 0; index < Visuals.Length; index++)
            {
                var visual = Visuals[index];
                var source = LoadPng(Path.Combine(projectRoot, visual.SourcePath));
                try
                {
                    Assert.That(source.width, Is.EqualTo(1024));
                    Assert.That(source.height, Is.EqualTo(1024));
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
                    Assert.That(coverage, Is.LessThanOrEqualTo(0.35f));
                    if (visual.UsesDangerRed)
                    {
                        Assert.That(dangerPixels, Is.GreaterThan(1000));
                    }
                    else
                    {
                        Assert.That(deepPixels, Is.GreaterThan(250));
                        Assert.That(dangerPixels, Is.Zero);
                    }
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
        public void OnlyFinalStatusAndDamagePolicyTexturesAreAddressable()
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
                Assert.That(path, Does.EndWith("-visual.png"));
                count++;
            }
            Assert.That(count, Is.EqualTo(9));
        }

        private static StatusVisualContract Status(string name)
        {
            return new StatusVisualContract(
                name,
                "qinglan.presentation.status." + name.Replace('-', '_'),
                "qinglan/status/" + name + "/visual",
                false);
        }

        private static StatusVisualContract Policy(string name, bool usesDangerRed = false)
        {
            return new StatusVisualContract(
                name,
                "qinglan.presentation.damage_policy." + name.Replace('-', '_'),
                "qinglan/damage-policy/" + name + "/visual",
                usesDangerRed);
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

        private sealed class StatusVisualContract
        {
            public StatusVisualContract(string name, string stableId, string address, bool usesDangerRed)
            {
                Name = name;
                StableId = stableId;
                Address = address;
                UsesDangerRed = usesDangerRed;
            }

            public string Name { get; }
            public string StableId { get; }
            public string Address { get; }
            public bool UsesDangerRed { get; }
            public string SourcePath => AssetRoot + "/source/" + Name + "-visual-source.png";
            public string FinalPath => AssetRoot + "/final/" + Name + "-visual.png";
        }
    }
}
