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
    public sealed class QinglanG31ArtAffix001Tests
    {
        private const string BatchRoot =
            "Assets/GameAssets/FirstParty/QinglanDemo/ART-AFFIX-001";

        private static readonly AffixContract[] Affixes =
        {
            new AffixContract("frenzy", "qinglan/enemy/affix/frenzy/overlay", new Color32(233, 138, 74, 255), 245),
            new AffixContract("barrier", "qinglan/enemy/affix/barrier/overlay", new Color32(217, 179, 108, 255), 250),
            new AffixContract("splitting", "qinglan/enemy/affix/splitting/overlay", new Color32(199, 125, 139, 255), 245),
            new AffixContract("quake", "qinglan/enemy/affix/quake/overlay", new Color32(228, 93, 69, 255), 199)
        };

        [Test]
        public void FourRuntimeOverlaysHaveApprovedTextureAndReleaseContracts()
        {
            for (var index = 0; index < Affixes.Length; index++)
            {
                var affix = Affixes[index];
                var texture = AssetDatabase.LoadAssetAtPath<Texture2D>(affix.FinalPath);
                Assert.That(texture, Is.Not.Null, affix.FinalPath);
                Assert.That(texture.width, Is.EqualTo(512));
                Assert.That(texture.height, Is.EqualTo(512));

                var importer = AssetImporter.GetAtPath(affix.FinalPath) as TextureImporter;
                Assert.That(importer, Is.Not.Null);
                Assert.That(importer.textureType, Is.EqualTo(TextureImporterType.Sprite));
                Assert.That(importer.spriteImportMode, Is.EqualTo(SpriteImportMode.Multiple));
                Assert.That(importer.mipmapEnabled, Is.False);
                Assert.That(importer.alphaIsTransparency, Is.True);
                Assert.That(importer.maxTextureSize, Is.EqualTo(512));
                var standalone = importer.GetPlatformTextureSettings("Standalone");
                Assert.That(standalone.overridden, Is.True);
                Assert.That(standalone.format, Is.EqualTo(TextureImporterFormat.BC7));

                var sprites = AssetDatabase.LoadAllAssetsAtPath(affix.FinalPath);
                var spriteNames = new HashSet<string>(StringComparer.Ordinal);
                for (var assetIndex = 0; assetIndex < sprites.Length; assetIndex++)
                    if (sprites[assetIndex] is Sprite sprite) spriteNames.Add(sprite.name);
                Assert.That(spriteNames, Does.Contain("qinglan.enemy.affix." + affix.Name + ".overlay.r0.c0"));

                AssertReleaseEntry(affix.FinalPath, affix.Address);
            }
        }

        [Test]
        public void SourceAndFinalGeometryPreserveDistinctReadableMechanics()
        {
            var projectRoot = Directory.GetParent(UnityEngine.Application.dataPath)?.FullName ?? string.Empty;
            var silhouetteSignatures = new HashSet<string>(StringComparer.Ordinal);
            for (var index = 0; index < Affixes.Length; index++)
            {
                var affix = Affixes[index];
                var source = LoadPng(Path.Combine(projectRoot, affix.SourcePath));
                try
                {
                    Assert.That(source.width, Is.EqualTo(1024));
                    Assert.That(source.height, Is.EqualTo(1024));
                }
                finally
                {
                    UnityEngine.Object.DestroyImmediate(source);
                }

                var final = LoadPng(Path.Combine(projectRoot, affix.FinalPath));
                try
                {
                    var pixels = final.GetPixels32();
                    Assert.That(pixels[0].a, Is.Zero);
                    Assert.That(pixels[final.width - 1].a, Is.Zero);
                    Assert.That(pixels[(final.height - 1) * final.width].a, Is.Zero);
                    Assert.That(pixels[pixels.Length - 1].a, Is.Zero);

                    var bounds = AlphaBounds(final, pixels, out var coverage, out var opaqueColors);
                    Assert.That(bounds.xMin, Is.GreaterThanOrEqualTo(40));
                    Assert.That(bounds.yMin, Is.GreaterThanOrEqualTo(40));
                    Assert.That(final.width - bounds.xMax, Is.GreaterThanOrEqualTo(40));
                    Assert.That(final.height - bounds.yMax, Is.GreaterThanOrEqualTo(40));
                    Assert.That(coverage, Is.GreaterThan(final.width * final.height * 0.10));
                    Assert.That(coverage, Is.LessThan(final.width * final.height * 0.21));
                    Assert.That(opaqueColors, Does.Contain(affix.Primary));

                    var centerCoverage = CountCircleCoverage(pixels, final.width, 256, affix.CenterY, 55);
                    Assert.That(centerCoverage, Is.LessThan(950), affix.Name + " obscures the enemy center.");
                    silhouetteSignatures.Add(bounds.width + "x" + bounds.height);
                }
                finally
                {
                    UnityEngine.Object.DestroyImmediate(final);
                }
            }

            Assert.That(silhouetteSignatures.Count, Is.EqualTo(Affixes.Length));
        }

        [Test]
        public void OnlyFinalFilesAreAddressableWithinBatch()
        {
            var settings = AddressableAssetSettingsDefaultObject.GetSettings(false);
            Assert.That(settings, Is.Not.Null);
            var guids = AssetDatabase.FindAssets(string.Empty, new[] { BatchRoot });
            var addressableCount = 0;
            for (var index = 0; index < guids.Length; index++)
            {
                var entry = settings.FindAssetEntry(guids[index]);
                if (entry == null) continue;
                var path = AssetDatabase.GUIDToAssetPath(guids[index]).Replace('\\', '/');
                Assert.That(path, Does.Contain("/final/"));
                addressableCount++;
            }
            Assert.That(addressableCount, Is.EqualTo(4));
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

        private static Texture2D LoadPng(string path)
        {
            var texture = new Texture2D(2, 2, TextureFormat.RGBA32, false);
            Assert.That(ImageConversion.LoadImage(texture, File.ReadAllBytes(path), false), Is.True, path);
            return texture;
        }

        private static RectInt AlphaBounds(
            Texture2D texture,
            Color32[] pixels,
            out int coverage,
            out HashSet<Color32> opaqueColors)
        {
            var minimumX = texture.width;
            var minimumY = texture.height;
            var maximumX = -1;
            var maximumY = -1;
            coverage = 0;
            opaqueColors = new HashSet<Color32>();
            for (var y = 0; y < texture.height; y++)
            {
                for (var x = 0; x < texture.width; x++)
                {
                    var pixel = pixels[y * texture.width + x];
                    if (pixel.a == 255) opaqueColors.Add(pixel);
                    if (pixel.a < 16) continue;
                    coverage++;
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

        private static int CountCircleCoverage(
            Color32[] pixels,
            int width,
            int centerX,
            int centerY,
            int radius)
        {
            var coverage = 0;
            var radiusSquared = radius * radius;
            for (var y = centerY - radius; y <= centerY + radius; y++)
            {
                for (var x = centerX - radius; x <= centerX + radius; x++)
                {
                    var deltaX = x - centerX;
                    var deltaY = y - centerY;
                    if (deltaX * deltaX + deltaY * deltaY > radiusSquared) continue;
                    if (pixels[y * width + x].a >= 16) coverage++;
                }
            }
            return coverage;
        }

        private static string JoinIssues(IReadOnlyList<ValidationIssue> issues)
        {
            var value = string.Empty;
            for (var index = 0; index < issues.Count; index++)
                value += (index == 0 ? string.Empty : "\n") + issues[index];
            return value;
        }

        private sealed class AffixContract
        {
            public AffixContract(string name, string address, Color32 primary, int centerY)
            {
                Name = name;
                Address = address;
                Primary = primary;
                CenterY = centerY;
            }

            public string Name { get; }
            public string Address { get; }
            public Color32 Primary { get; }
            public int CenterY { get; }
            public string SourcePath => BatchRoot + "/source/elite-affix-" + Name + "-overlay-source.png";
            public string FinalPath => BatchRoot + "/final/elite-affix-" + Name + "-overlay.png";
        }
    }
}
