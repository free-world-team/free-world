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
    public sealed class QinglanG31ArtBoss002Tests
    {
        private const string BatchRoot = "Assets/GameAssets/FirstParty/QinglanDemo/ART-BOSS-002";

        private static readonly OverlayContract[] Overlays =
        {
            new OverlayContract("zhezhi-phase-1-horizontal-trial", "qinglan/boss/zhezhi/phase-1-overlay"),
            new OverlayContract("zhezhi-phase-2-falling-wood", "qinglan/boss/zhezhi/phase-2-overlay"),
            new OverlayContract("zhezhi-phase-3-training-formation", "qinglan/boss/zhezhi/phase-3-overlay"),
            new OverlayContract("tingfeng-phase-1-gate-charge", "qinglan/boss/tingfeng/phase-1-overlay"),
            new OverlayContract("tingfeng-phase-2-listening-wind", "qinglan/boss/tingfeng/phase-2-overlay"),
            new OverlayContract("tingfeng-phase-3-undying-oath", "qinglan/boss/tingfeng/phase-3-overlay")
        };

        [Test]
        public void SixRuntimeOverlaysHaveApprovedTextureAndReleaseContracts()
        {
            for (var index = 0; index < Overlays.Length; index++)
            {
                var overlay = Overlays[index];
                var texture = AssetDatabase.LoadAssetAtPath<Texture2D>(overlay.FinalPath);
                Assert.That(texture, Is.Not.Null, overlay.FinalPath);
                Assert.That(texture.width, Is.EqualTo(1024));
                Assert.That(texture.height, Is.EqualTo(1024));

                var importer = AssetImporter.GetAtPath(overlay.FinalPath) as TextureImporter;
                Assert.That(importer, Is.Not.Null);
                Assert.That(importer.textureType, Is.EqualTo(TextureImporterType.Sprite));
                Assert.That(importer.spriteImportMode, Is.EqualTo(SpriteImportMode.Multiple));
                Assert.That(importer.mipmapEnabled, Is.False);
                Assert.That(importer.alphaIsTransparency, Is.True);
                Assert.That(importer.maxTextureSize, Is.EqualTo(1024));
                var standalone = importer.GetPlatformTextureSettings("Standalone");
                Assert.That(standalone.overridden, Is.True);
                Assert.That(standalone.format, Is.EqualTo(TextureImporterFormat.BC7));
                AssertReleaseEntry(overlay.FinalPath, overlay.Address);
            }
        }

        [Test]
        public void SourceAndFinalGeometryPreserveSixDistinctP0Patterns()
        {
            var projectRoot = Directory.GetParent(UnityEngine.Application.dataPath)?.FullName ?? string.Empty;
            var signatures = new HashSet<string>(StringComparer.Ordinal);
            var danger = new Color32(228, 93, 69, 255);
            var warning = new Color32(255, 208, 163, 255);
            for (var index = 0; index < Overlays.Length; index++)
            {
                var overlay = Overlays[index];
                var source = LoadPng(Path.Combine(projectRoot, overlay.SourcePath));
                try
                {
                    Assert.That(source.width, Is.EqualTo(2048));
                    Assert.That(source.height, Is.EqualTo(2048));
                }
                finally
                {
                    UnityEngine.Object.DestroyImmediate(source);
                }

                var final = LoadPng(Path.Combine(projectRoot, overlay.FinalPath));
                try
                {
                    var pixels = final.GetPixels32();
                    Assert.That(pixels[0].a, Is.Zero);
                    Assert.That(pixels[final.width - 1].a, Is.Zero);
                    Assert.That(pixels[(final.height - 1) * final.width].a, Is.Zero);
                    Assert.That(pixels[pixels.Length - 1].a, Is.Zero);
                    var bounds = AlphaBounds(final, pixels, out var coverage, out var opaqueColors);
                    Assert.That(bounds.xMin, Is.GreaterThanOrEqualTo(60));
                    Assert.That(bounds.yMin, Is.GreaterThanOrEqualTo(60));
                    Assert.That(final.width - bounds.xMax, Is.GreaterThanOrEqualTo(60));
                    Assert.That(final.height - bounds.yMax, Is.GreaterThanOrEqualTo(60));
                    Assert.That(coverage, Is.GreaterThan(final.width * final.height * 0.10));
                    Assert.That(coverage, Is.LessThan(final.width * final.height * 0.24));
                    Assert.That(opaqueColors, Does.Contain(danger));
                    Assert.That(opaqueColors, Does.Contain(warning));
                    signatures.Add(bounds.width + "x" + bounds.height);
                }
                finally
                {
                    UnityEngine.Object.DestroyImmediate(final);
                }
            }

            Assert.That(signatures.Count, Is.EqualTo(Overlays.Length));
        }

        [Test]
        public void OnlyFinalFilesAreAddressableWithinBatch()
        {
            var settings = AddressableAssetSettingsDefaultObject.GetSettings(false);
            Assert.That(settings, Is.Not.Null);
            var guids = AssetDatabase.FindAssets(string.Empty, new[] { BatchRoot });
            var count = 0;
            for (var index = 0; index < guids.Length; index++)
            {
                var entry = settings.FindAssetEntry(guids[index]);
                if (entry == null) continue;
                var path = AssetDatabase.GUIDToAssetPath(guids[index]).Replace('\\', '/');
                Assert.That(path, Does.Contain("/final/"));
                count++;
            }
            Assert.That(count, Is.EqualTo(6));
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
            return new RectInt(minimumX, minimumY, maximumX - minimumX + 1, maximumY - minimumY + 1);
        }

        private static string JoinIssues(IReadOnlyList<ValidationIssue> issues)
        {
            var value = string.Empty;
            for (var index = 0; index < issues.Count; index++)
                value += (index == 0 ? string.Empty : "\n") + issues[index];
            return value;
        }

        private sealed class OverlayContract
        {
            public OverlayContract(string name, string address)
            {
                Name = name;
                Address = address;
            }

            public string Name { get; }
            public string Address { get; }
            public string FinalPath => BatchRoot + "/final/" + Name + ".png";
            public string SourcePath => BatchRoot + "/source/" + Name + "-source.png";
        }
    }
}
