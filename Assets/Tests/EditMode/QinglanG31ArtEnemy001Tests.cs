using System;
using System.Collections.Generic;
using System.IO;
using Game.Editor;
using Game.Presentation;
using NUnit.Framework;
using UnityEditor;
using UnityEditor.AddressableAssets;
using UnityEngine;

namespace Game.Tests.EditMode
{
    public sealed class QinglanG31ArtEnemy001Tests
    {
        private const string AssetRoot =
            "Assets/GameAssets/AI/QinglanDemo/ART-ENEMY-001";
        private const string ProfileRoot =
            "Assets/GameContent/QinglanDemo/Profiles/Visual/ART-ENEMY-001";

        private static readonly EnemyContract[] Enemies =
        {
            new EnemyContract("grass-spirit", "qinglan.enemy.grass_spirit"),
            new EnemyContract("paper-crane-spirit", "qinglan.enemy.paper_crane_spirit"),
            new EnemyContract("wooden-sword-puppet", "qinglan.enemy.wooden_sword_puppet"),
            new EnemyContract("stone-lantern-guard", "qinglan.enemy.stone_lantern_guard"),
            new EnemyContract("wind-bell-spirit", "qinglan.enemy.wind_bell_spirit"),
            new EnemyContract("explosive-seed-pod", "qinglan.enemy.explosive_seed_pod")
        };

        private static readonly string[] Directions = { "down", "left", "right", "up" };
        private static readonly string[] Actions = { "move", "attack-windup", "hit", "death" };

        [Test]
        public void SixAtlasesExposeSemanticFourDirectionFourStateSprites()
        {
            for (var enemyIndex = 0; enemyIndex < Enemies.Length; enemyIndex++)
            {
                var enemy = Enemies[enemyIndex];
                var atlasPath = enemy.AtlasPath;
                var texture = AssetDatabase.LoadAssetAtPath<Texture2D>(atlasPath);
                Assert.That(texture, Is.Not.Null, atlasPath);
                Assert.That(texture.width, Is.EqualTo(1024));
                Assert.That(texture.height, Is.EqualTo(1024));

                var importer = AssetImporter.GetAtPath(atlasPath) as TextureImporter;
                Assert.That(importer, Is.Not.Null);
                Assert.That(importer.textureType, Is.EqualTo(TextureImporterType.Sprite));
                Assert.That(importer.spriteImportMode, Is.EqualTo(SpriteImportMode.Multiple));
                Assert.That(importer.mipmapEnabled, Is.False);
                Assert.That(importer.alphaIsTransparency, Is.True);
                Assert.That(importer.maxTextureSize, Is.EqualTo(1024));
                var standalone = importer.GetPlatformTextureSettings("Standalone");
                Assert.That(standalone.overridden, Is.True);
                Assert.That(standalone.format, Is.EqualTo(TextureImporterFormat.BC7));

                var subassets = AssetDatabase.LoadAllAssetsAtPath(atlasPath);
                var names = new HashSet<string>(StringComparer.Ordinal);
                for (var index = 0; index < subassets.Length; index++)
                    if (subassets[index] is Sprite sprite) names.Add(sprite.name);
                Assert.That(names.Count, Is.EqualTo(16));
                for (var row = 0; row < Directions.Length; row++)
                {
                    for (var column = 0; column < Actions.Length; column++)
                    {
                        Assert.That(
                            names,
                            Does.Contain(
                                "qinglan.enemy." + enemy.Name + "." +
                                Directions[row] + "." + Actions[column]));
                    }
                }

                AssertReleaseEntry(atlasPath, enemy.AtlasAddress);
            }
        }

        [Test]
        public void SixProfilesUseStableEnemyIdsAndApprovedDownMoveSprites()
        {
            for (var index = 0; index < Enemies.Length; index++)
            {
                var enemy = Enemies[index];
                var profile = AssetDatabase.LoadAssetAtPath<VisualProfile>(enemy.ProfilePath);
                Assert.That(profile, Is.Not.Null, enemy.ProfilePath);
                Assert.That(profile.StableId, Is.EqualTo(enemy.StableId));
                Assert.That(profile.Sprite, Is.Not.Null);
                Assert.That(
                    profile.Sprite.name,
                    Is.EqualTo("qinglan.enemy." + enemy.Name + ".down.move"));
                AssertReleaseEntry(enemy.ProfilePath, enemy.ProfileAddress);
            }
        }

        [Test]
        public void FinalCellsHaveSafeAlphaAndDistinctOpeningSilhouettes()
        {
            var projectRoot = Directory.GetParent(UnityEngine.Application.dataPath)?.FullName ?? string.Empty;
            var openingSignatures = new HashSet<string>(StringComparer.Ordinal);
            for (var enemyIndex = 0; enemyIndex < Enemies.Length; enemyIndex++)
            {
                var enemy = Enemies[enemyIndex];
                var absolute = Path.Combine(projectRoot, enemy.AtlasPath);
                var texture = LoadPng(absolute);
                try
                {
                    Assert.That(texture.width, Is.EqualTo(1024));
                    Assert.That(texture.height, Is.EqualTo(1024));
                    var pixels = texture.GetPixels32();
                    Assert.That(pixels[0].a, Is.Zero);
                    Assert.That(pixels[texture.width - 1].a, Is.Zero);
                    Assert.That(pixels[(texture.height - 1) * texture.width].a, Is.Zero);
                    Assert.That(pixels[pixels.Length - 1].a, Is.Zero);

                    for (var row = 0; row < 4; row++)
                    {
                        for (var column = 0; column < 4; column++)
                        {
                            var bounds = CellAlphaBounds(pixels, texture.width, row, column, out var magentaPixels);
                            Assert.That(bounds.width, Is.GreaterThan(0));
                            Assert.That(bounds.height, Is.GreaterThan(0));
                            Assert.That(bounds.xMin, Is.GreaterThanOrEqualTo(12));
                            Assert.That(bounds.yMin, Is.GreaterThanOrEqualTo(12));
                            Assert.That(256 - bounds.xMax, Is.GreaterThanOrEqualTo(12));
                            Assert.That(256 - bounds.yMax, Is.GreaterThanOrEqualTo(12));
                            Assert.That(magentaPixels, Is.Zero);
                            if (row == 0 && column == 0)
                                openingSignatures.Add(bounds.width + "x" + bounds.height);
                        }
                    }
                }
                finally
                {
                    UnityEngine.Object.DestroyImmediate(texture);
                }
            }
            Assert.That(openingSignatures.Count, Is.EqualTo(Enemies.Length));
        }

        [Test]
        public void OnlyFinalAtlasesAndProfilesAreAddressable()
        {
            var settings = AddressableAssetSettingsDefaultObject.GetSettings(false);
            Assert.That(settings, Is.Not.Null);
            AssertAddressableScope(settings, AssetRoot, "/final/", 6);
            AssertAddressableScope(settings, ProfileRoot, "-visual-profile.asset", 6);
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

        private static RectInt CellAlphaBounds(
            Color32[] pixels,
            int atlasWidth,
            int row,
            int column,
            out int magentaPixels)
        {
            var minimumX = 256;
            var minimumY = 256;
            var maximumX = -1;
            var maximumY = -1;
            magentaPixels = 0;
            for (var y = 0; y < 256; y++)
            {
                for (var x = 0; x < 256; x++)
                {
                    var atlasX = column * 256 + x;
                    var atlasY = (3 - row) * 256 + y;
                    var pixel = pixels[atlasY * atlasWidth + atlasX];
                    if (pixel.a < 16) continue;
                    if (pixel.r >= 210 && pixel.b >= 180 && pixel.g <= 90) magentaPixels++;
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

        private sealed class EnemyContract
        {
            public EnemyContract(string name, string stableId)
            {
                Name = name;
                StableId = stableId;
            }

            public string Name { get; }
            public string StableId { get; }
            public string AtlasPath =>
                AssetRoot + "/final/" + Name + "-directional-animation-atlas.png";
            public string AtlasAddress =>
                "qinglan/enemy/" + Name + "/directional-animation-atlas";
            public string ProfilePath =>
                ProfileRoot + "/" + Name + "-visual-profile.asset";
            public string ProfileAddress => "qinglan/profile/enemy/" + Name;
        }
    }
}
