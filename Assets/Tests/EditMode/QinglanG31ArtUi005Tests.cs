using System;
using System.Collections.Generic;
using System.IO;
using Game.Editor;
using Game.Presentation;
using NUnit.Framework;
using UnityEditor;
using UnityEditor.AddressableAssets;
using UnityEngine;
using UnityEngine.InputSystem;

namespace Game.Tests.EditMode
{
    public sealed class QinglanG31ArtUi005Tests
    {
        private const string AssetRoot = "Assets/GameAssets/FirstParty/QinglanDemo/ART-UI-005";
        private const string Prefix = "qinglan.presentation.ui.input-glyph.";

        private static readonly string[] Names =
        {
            "cursor.pointer", "focus.ring", "keyboard.wasd", "keyboard.arrows",
            "keyboard.enter", "keyboard.escape", "keyboard.key-e", "keyboard.key-m",
            "keyboard.key-q", "keyboard.page-axis", "mouse.left-button", "mouse.right-button",
            "mouse.scroll", "gamepad.left-stick", "gamepad.dpad", "gamepad.button-south",
            "gamepad.button-east", "gamepad.button-north", "gamepad.start", "gamepad.select",
            "gamepad.left-shoulder", "gamepad.right-shoulder", "gamepad.left-trigger", "gamepad.right-trigger"
        };

        [Test]
        public void TwentyFourGlyphsUseReleaseImportBudgetAndStableAddresses()
        {
            for (var index = 0; index < Names.Length; index++)
            {
                var name = Names[index];
                var path = FinalPath(name);
                var texture = AssetDatabase.LoadAssetAtPath<Texture2D>(path);
                Assert.That(texture, Is.Not.Null, name);
                Assert.That(texture.width, Is.EqualTo(128), name);
                Assert.That(texture.height, Is.EqualTo(128), name);

                var importer = AssetImporter.GetAtPath(path) as TextureImporter;
                Assert.That(importer, Is.Not.Null, name);
                Assert.That(importer.textureType, Is.EqualTo(TextureImporterType.Sprite), name);
                Assert.That(importer.spriteImportMode, Is.EqualTo(SpriteImportMode.Multiple), name);
                Assert.That(importer.mipmapEnabled, Is.False, name);
                Assert.That(importer.alphaIsTransparency, Is.True, name);
                Assert.That(importer.wrapMode, Is.EqualTo(TextureWrapMode.Clamp), name);
                Assert.That(importer.maxTextureSize, Is.EqualTo(128), name);
                var standalone = importer.GetPlatformTextureSettings("Standalone");
                Assert.That(standalone.overridden, Is.True, name);
                Assert.That(standalone.maxTextureSize, Is.EqualTo(128), name);
                Assert.That(standalone.format, Is.EqualTo(TextureImporterFormat.BC7), name);

                var sprites = FindSprites(path);
                Assert.That(sprites.Count, Is.EqualTo(1), name);
                Assert.That(sprites.ContainsKey(Prefix + name), Is.True, name);
                var sprite = sprites[Prefix + name];
                Assert.That(sprite.rect, Is.EqualTo(new Rect(0f, 0f, 128f, 128f)), name);
                Assert.That(sprite.pivot, Is.EqualTo(new Vector2(64f, 64f)), name);
                Assert.That(sprite.border, Is.EqualTo(Vector4.zero), name);

                AssertReleaseEntry(path, "qinglan/ui/input-glyph/" + name.Replace('.', '/'));
            }
        }

        [Test]
        public void SourceAndFinalMeetGeometryForbiddenColorAndDistinctnessBudgets()
        {
            var projectRoot = Directory.GetParent(UnityEngine.Application.dataPath)?.FullName ?? string.Empty;
            var sourceColorHashes = new HashSet<ulong>();
            var sourceGrayHashes = new HashSet<ulong>();
            var finalColorHashes = new HashSet<ulong>();
            var finalGrayHashes = new HashSet<ulong>();
            for (var index = 0; index < Names.Length; index++)
            {
                var filename = FileName(Names[index]);
                AssertGlyph(
                    Path.Combine(projectRoot, AssetRoot, "source", filename),
                    512,
                    32,
                    sourceColorHashes,
                    sourceGrayHashes,
                    Names[index]);
                AssertGlyph(
                    Path.Combine(projectRoot, AssetRoot, "final", filename),
                    128,
                    8,
                    finalColorHashes,
                    finalGrayHashes,
                    Names[index]);
            }
            Assert.That(sourceColorHashes.Count, Is.EqualTo(24));
            Assert.That(sourceGrayHashes.Count, Is.EqualTo(24));
            Assert.That(finalColorHashes.Count, Is.EqualTo(24));
            Assert.That(finalGrayHashes.Count, Is.EqualTo(24));
        }

        [Test]
        public void ReleaseGlyphsCoverPrimaryGameplayAndUiBindingContractWithoutDebugGlyphs()
        {
            var mapped = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
            {
                ["<Keyboard>/w"] = "keyboard.wasd",
                ["<Keyboard>/a"] = "keyboard.wasd",
                ["<Keyboard>/s"] = "keyboard.wasd",
                ["<Keyboard>/d"] = "keyboard.wasd",
                ["<Keyboard>/upArrow"] = "keyboard.arrows",
                ["<Keyboard>/downArrow"] = "keyboard.arrows",
                ["<Keyboard>/leftArrow"] = "keyboard.arrows",
                ["<Keyboard>/rightArrow"] = "keyboard.arrows",
                ["<Keyboard>/enter"] = "keyboard.enter",
                ["<Keyboard>/escape"] = "keyboard.escape",
                ["<Keyboard>/e"] = "keyboard.key-e",
                ["<Keyboard>/m"] = "keyboard.key-m",
                ["<Keyboard>/q"] = "keyboard.key-q",
                ["<Keyboard>/pageUp"] = "keyboard.page-axis",
                ["<Keyboard>/pageDown"] = "keyboard.page-axis",
                ["<Mouse>/leftButton"] = "mouse.left-button",
                ["<Mouse>/rightButton"] = "mouse.right-button",
                ["<Mouse>/scroll"] = "mouse.scroll",
                ["<Gamepad>/leftStick"] = "gamepad.left-stick",
                ["<Gamepad>/dpad"] = "gamepad.dpad",
                ["<Gamepad>/buttonSouth"] = "gamepad.button-south",
                ["<Gamepad>/buttonEast"] = "gamepad.button-east",
                ["<Gamepad>/buttonNorth"] = "gamepad.button-north",
                ["<Gamepad>/start"] = "gamepad.start",
                ["<Gamepad>/select"] = "gamepad.select",
                ["<Gamepad>/leftShoulder"] = "gamepad.left-shoulder",
                ["<Gamepad>/rightShoulder"] = "gamepad.right-shoulder",
                ["<Gamepad>/leftTrigger"] = "gamepad.left-trigger",
                ["<Gamepad>/rightTrigger"] = "gamepad.right-trigger"
            };
            var glyphs = new HashSet<string>(Names, StringComparer.Ordinal);
            foreach (var pair in mapped)
                Assert.That(glyphs.Contains(pair.Value), Is.True, pair.Key);

            var actions = M7InputRouter.CreateDefaultActions();
            try
            {
                var requiredActions = new[]
                {
                    "Gameplay/Move", "Gameplay/Pause", "Gameplay/Map", "Gameplay/Interact",
                    "UI/Navigate", "UI/Submit", "UI/Cancel", "UI/Tab", "UI/Page"
                };
                for (var actionIndex = 0; actionIndex < requiredActions.Length; actionIndex++)
                {
                    var action = actions.FindAction(requiredActions[actionIndex], true);
                    var mappedBindingCount = 0;
                    for (var bindingIndex = 0; bindingIndex < action.bindings.Count; bindingIndex++)
                    {
                        var binding = action.bindings[bindingIndex];
                        if (binding.isComposite || string.IsNullOrWhiteSpace(binding.path)) continue;
                        if (mapped.ContainsKey(binding.path)) mappedBindingCount++;
                    }
                    Assert.That(mappedBindingCount, Is.GreaterThanOrEqualTo(2), requiredActions[actionIndex]);
                }
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(actions);
            }

            Assert.That(Array.Exists(Names, name => name.Contains("f2", StringComparison.OrdinalIgnoreCase)), Is.False);
            Assert.That(Array.Exists(Names, name => name.Contains("f3", StringComparison.OrdinalIgnoreCase)), Is.False);

            var settings = AddressableAssetSettingsDefaultObject.GetSettings(false);
            var addressableCount = 0;
            var guids = AssetDatabase.FindAssets(string.Empty, new[] { AssetRoot });
            for (var index = 0; index < guids.Length; index++)
            {
                var entry = settings?.FindAssetEntry(guids[index]);
                if (entry == null) continue;
                Assert.That(AssetDatabase.GUIDToAssetPath(guids[index]).Replace('\\', '/'), Does.StartWith(AssetRoot + "/final/"));
                addressableCount++;
            }
            Assert.That(addressableCount, Is.EqualTo(24));
        }

        private static void AssertGlyph(
            string path,
            int size,
            int minimumMargin,
            ISet<ulong> colorHashes,
            ISet<ulong> grayscaleHashes,
            string name)
        {
            var texture = LoadPng(path);
            try
            {
                Assert.That(texture.width, Is.EqualTo(size), name);
                Assert.That(texture.height, Is.EqualTo(size), name);
                var pixels = texture.GetPixels32();
                var minX = size;
                var minY = size;
                var maxX = -1;
                var maxY = -1;
                var covered = 0;
                var magenta = 0;
                var danger = 0;
                var colorHash = 1469598103934665603UL;
                var grayscaleHash = 1469598103934665603UL;
                for (var y = 0; y < size; y++)
                {
                    for (var x = 0; x < size; x++)
                    {
                        var pixel = pixels[y * size + x];
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
                Assert.That(covered, Is.GreaterThan(0), name);
                var margin = Math.Min(Math.Min(minX, minY), Math.Min(size - maxX - 1, size - maxY - 1));
                Assert.That(margin, Is.GreaterThanOrEqualTo(minimumMargin), name);
                Assert.That(covered / (float)(size * size), Is.InRange(0.035f, 0.62f), name);
                Assert.That(magenta, Is.Zero, name);
                Assert.That(danger, Is.Zero, name);
                Assert.That(pixels[0].a, Is.Zero, name);
                Assert.That(pixels[pixels.Length - 1].a, Is.Zero, name);
                colorHashes.Add(colorHash);
                grayscaleHashes.Add(grayscaleHash);
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(texture);
            }
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

        private static string FinalPath(string name) => AssetRoot + "/final/" + FileName(name);
        private static string FileName(string name) => name.Replace('.', '-') + ".png";
    }
}
