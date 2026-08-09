using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using UnityEditor;
using UnityEditor.AddressableAssets;
using UnityEditor.AddressableAssets.Settings;
using UnityEditor.AddressableAssets.Settings.GroupSchemas;
using UnityEngine;

namespace Game.Editor
{
    /// <summary>Imports one approved Qinglan visual file with deterministic texture and Addressables settings.</summary>
    public static class QinglanVisualAssetImporter
    {
        public static void ImportSpriteAtlas(
            string assetPath,
            string address,
            int columns,
            int rows,
            int maxTextureSize,
            string spriteNamePrefix)
        {
            ImportSpriteAtlas(
                assetPath,
                address,
                columns,
                rows,
                maxTextureSize,
                spriteNamePrefix,
                new Vector2(0.5f, 0.046875f));
        }

        public static void ImportSpriteAtlas(
            string assetPath,
            string address,
            int columns,
            int rows,
            int maxTextureSize,
            string spriteNamePrefix,
            Vector2 pivot)
        {
            if (string.IsNullOrWhiteSpace(assetPath) ||
                !assetPath.Replace('\\', '/').Contains("/final/"))
                throw new ArgumentException("Only an explicit final asset path can be imported.", nameof(assetPath));
            if (string.IsNullOrWhiteSpace(address) ||
                !address.StartsWith("qinglan/", StringComparison.Ordinal))
                throw new ArgumentException("A canonical qinglan/ address is required.", nameof(address));
            if (columns <= 0 || rows <= 0) throw new ArgumentOutOfRangeException(nameof(columns));
            if (pivot.x < 0f || pivot.x > 1f || pivot.y < 0f || pivot.y > 1f)
                throw new ArgumentOutOfRangeException(nameof(pivot));

            assetPath = assetPath.Replace('\\', '/');
            AssetDatabase.ImportAsset(
                assetPath,
                ImportAssetOptions.ForceSynchronousImport | ImportAssetOptions.ForceUpdate);
            var texture = AssetDatabase.LoadAssetAtPath<Texture2D>(assetPath);
            if (texture == null) throw new InvalidOperationException(assetPath + " did not import as Texture2D.");
            if (texture.width % columns != 0 || texture.height % rows != 0)
                throw new InvalidOperationException("Texture dimensions are not divisible by the requested grid.");
            if (texture.width > maxTextureSize || texture.height > maxTextureSize)
                throw new InvalidOperationException("Texture exceeds its runtime maximum size.");

            var importer = AssetImporter.GetAtPath(assetPath) as TextureImporter;
            if (importer == null) throw new InvalidOperationException("TextureImporter is unavailable for " + assetPath);
            importer.textureType = TextureImporterType.Sprite;
            importer.spriteImportMode = SpriteImportMode.Multiple;
            importer.spritePixelsPerUnit = 128f;
            importer.mipmapEnabled = false;
            importer.alphaIsTransparency = true;
            importer.wrapMode = TextureWrapMode.Clamp;
            importer.filterMode = FilterMode.Bilinear;
            importer.maxTextureSize = maxTextureSize;
            importer.textureCompression = TextureImporterCompression.CompressedHQ;

            var standalone = importer.GetPlatformTextureSettings("Standalone");
            standalone.overridden = true;
            standalone.maxTextureSize = maxTextureSize;
            standalone.format = TextureImporterFormat.BC7;
            standalone.compressionQuality = 100;
            importer.SetPlatformTextureSettings(standalone);

            var cellWidth = texture.width / columns;
            var cellHeight = texture.height / rows;
            var sprites = new SpriteMetaData[columns * rows];
            for (var row = 0; row < rows; row++)
            {
                for (var column = 0; column < columns; column++)
                {
                    var index = row * columns + column;
                    sprites[index] = new SpriteMetaData
                    {
                        name = BuildSpriteName(spriteNamePrefix, row, column, rows, columns),
                        rect = new Rect(
                            column * cellWidth,
                            texture.height - (row + 1) * cellHeight,
                            cellWidth,
                            cellHeight),
                        alignment = (int)SpriteAlignment.Custom,
                        pivot = pivot,
                        border = Vector4.zero
                    };
                }
            }

#pragma warning disable CS0618
            importer.spritesheet = sprites;
#pragma warning restore CS0618
            importer.SaveAndReimport();

            RegisterApprovedFile(assetPath, address);
        }

        public static void CreateVisualProfileAsset(
            string atlasPath,
            string spriteName,
            string profilePath,
            string stableId)
        {
            CreateVisualProfileAsset(
                atlasPath,
                spriteName,
                profilePath,
                stableId,
                Game.Simulation.EntityKind.Actor,
                Vector2.one);
        }

        public static void CreateVisualProfileAsset(
            string atlasPath,
            string spriteName,
            string profilePath,
            string stableId,
            Game.Simulation.EntityKind entityKind,
            Vector2 size)
        {
            if (!Enum.IsDefined(typeof(Game.Simulation.EntityKind), entityKind))
                throw new ArgumentOutOfRangeException(nameof(entityKind));
            if (size.x <= 0f || size.y <= 0f)
                throw new ArgumentOutOfRangeException(nameof(size));

            atlasPath = atlasPath.Replace('\\', '/');
            profilePath = profilePath.Replace('\\', '/');
            var assets = AssetDatabase.LoadAllAssetsAtPath(atlasPath);
            Sprite sprite = null;
            for (var index = 0; index < assets.Length; index++)
            {
                if (assets[index] is Sprite candidate &&
                    string.Equals(candidate.name, spriteName, StringComparison.Ordinal))
                {
                    sprite = candidate;
                    break;
                }
            }
            if (sprite == null) throw new InvalidOperationException(spriteName + " is missing from " + atlasPath);

            var directory = Path.GetDirectoryName(profilePath);
            if (string.IsNullOrWhiteSpace(directory))
                throw new InvalidOperationException("Visual profile directory is invalid.");
            Directory.CreateDirectory(directory);
            var profile = AssetDatabase.LoadAssetAtPath<Game.Presentation.VisualProfile>(profilePath);
            if (profile == null)
            {
                profile = ScriptableObject.CreateInstance<Game.Presentation.VisualProfile>();
                AssetDatabase.CreateAsset(profile, profilePath);
            }

            var serialized = new SerializedObject(profile);
            serialized.FindProperty("entityKind").intValue = (int)entityKind;
            serialized.FindProperty("stableId").stringValue = stableId;
            serialized.FindProperty("sprite").objectReferenceValue = sprite;
            serialized.FindProperty("color").colorValue = Color.white;
            serialized.FindProperty("size").vector2Value = size;
            serialized.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(profile);
            AssetDatabase.SaveAssets();
        }

        public static void RegisterApprovedFile(string assetPath, string address)
        {
            assetPath = assetPath.Replace('\\', '/');
            if (string.IsNullOrWhiteSpace(address) ||
                !address.StartsWith("qinglan/", StringComparison.Ordinal))
                throw new ArgumentException("A canonical qinglan/ address is required.", nameof(address));

            var projectRoot = Directory.GetParent(UnityEngine.Application.dataPath)?.FullName;
            if (string.IsNullOrWhiteSpace(projectRoot))
                throw new InvalidOperationException("Unable to resolve project root.");
            var releaseLabels = new HashSet<string>
            {
                AssetProvenanceValidator.QinglanPackLabel,
                AssetProvenanceValidator.ReleaseLabel,
                AssetProvenanceValidator.VisualReleaseLabel
            };
            var provenanceIssues = AssetProvenanceValidator.ValidateReleaseInput(
                projectRoot,
                assetPath,
                releaseLabels,
                AssetProvenanceValidator.QinglanVisualGroup);
            if (provenanceIssues.Count > 0)
                throw new InvalidOperationException(provenanceIssues[0].ToString());

            var settings = AddressableAssetSettingsDefaultObject.GetSettings(true);
            if (settings == null) throw new InvalidOperationException("Addressables settings are unavailable.");
            settings.AddLabel(AssetProvenanceValidator.QinglanPackLabel, false);
            settings.AddLabel(AssetProvenanceValidator.ReleaseLabel, false);
            settings.AddLabel(AssetProvenanceValidator.VisualReleaseLabel, false);
            var group = settings.FindGroup(AssetProvenanceValidator.QinglanVisualGroup);
            if (group == null)
            {
                group = settings.CreateGroup(
                    AssetProvenanceValidator.QinglanVisualGroup,
                    false,
                    false,
                    true,
                    null,
                    typeof(ContentUpdateGroupSchema),
                    typeof(BundledAssetGroupSchema));
                var bundleSchema = group.GetSchema<BundledAssetGroupSchema>();
                bundleSchema.BuildPath.SetVariableByName(settings, AddressableAssetSettings.kLocalBuildPath);
                bundleSchema.LoadPath.SetVariableByName(settings, AddressableAssetSettings.kLocalLoadPath);
                bundleSchema.BundleMode = BundledAssetGroupSchema.BundlePackingMode.PackTogether;
            }

            var guid = AssetDatabase.AssetPathToGUID(assetPath);
            if (string.IsNullOrEmpty(guid)) throw new InvalidOperationException("Asset GUID is unavailable.");
            var entry = settings.CreateOrMoveEntry(guid, group, false, false);
            entry.address = address;
            entry.SetLabel(PlaceholderAssetGenerator.PlaceholderLabel, false, false, false);
            entry.SetLabel(PlaceholderAssetGenerator.DevelopmentOnlyLabel, false, false, false);
            entry.SetLabel(AssetProvenanceValidator.QinglanPackLabel, true, false, false);
            entry.SetLabel(AssetProvenanceValidator.ReleaseLabel, true, false, false);
            entry.SetLabel(AssetProvenanceValidator.VisualReleaseLabel, true, false, false);
            settings.SetDirty(AddressableAssetSettings.ModificationEvent.BatchModification, entry, true, true);
            AssetDatabase.SaveAssets();
        }

        private static string BuildSpriteName(
            string prefix,
            int row,
            int column,
            int rows,
            int columns)
        {
            if (rows == 1 && columns == 1) return prefix;

            if (rows == 1 && columns == 3 &&
                prefix.Contains(".landmark.", StringComparison.Ordinal))
            {
                var states = new[] { "undiscovered", "discovered", "claimed" };
                return prefix + "." + states[column];
            }

            if (rows == 1 && columns == 3 &&
                prefix.Contains(".objective.", StringComparison.Ordinal))
            {
                var states = new[] { "idle", "active", "complete" };
                return prefix + "." + states[column];
            }

            if (rows == 1 && columns == 4 &&
                prefix.Contains(".event.", StringComparison.Ordinal))
                return prefix + ".frame-" + column.ToString(CultureInfo.InvariantCulture);

            if (rows == 4 && columns == 6)
            {
                var directions = new[] { "down", "left", "right", "up" };
                var actions = new[] { "idle", "move", "hit", "down", "victory", "imperial-sword" };
                return prefix + "." + directions[row] + "." + actions[column];
            }

            if (rows == 4 && columns == 4)
            {
                var directions = new[] { "down", "left", "right", "up" };
                var actions = new[] { "move", "attack-windup", "hit", "death" };
                return prefix + "." + directions[row] + "." + actions[column];
            }

            if (rows == 4 && columns == 8)
            {
                var directions = new[] { "down", "left", "right", "up" };
                var actions = new[]
                {
                    "move",
                    "hit",
                    "phase-1-windup",
                    "transition-2",
                    "phase-2-windup",
                    "transition-3",
                    "phase-3-windup",
                    "defeated"
                };
                return prefix + "." + directions[row] + "." + actions[column];
            }

            return prefix + ".r" + row.ToString(CultureInfo.InvariantCulture) +
                   ".c" + column.ToString(CultureInfo.InvariantCulture);
        }
    }

    /// <summary>Batchmode entry point for one approved Qinglan visual atlas.</summary>
    public static class QinglanVisualAssetImportCommand
    {
        public static void Run()
        {
            var exitCode = 0;
            try
            {
                QinglanVisualAssetImporter.ImportSpriteAtlas(
                    Required("QINGLAN_VISUAL_ASSET_PATH"),
                    Required("QINGLAN_VISUAL_ADDRESS"),
                    ParsePositive("QINGLAN_VISUAL_COLUMNS", 1),
                    ParsePositive("QINGLAN_VISUAL_ROWS", 1),
                    ParsePositive("QINGLAN_VISUAL_MAX_SIZE", 2048),
                    Required("QINGLAN_VISUAL_SPRITE_PREFIX"),
                    new Vector2(
                        ParseUnitFloat("QINGLAN_VISUAL_PIVOT_X", 0.5f),
                        ParseUnitFloat("QINGLAN_VISUAL_PIVOT_Y", 0.046875f)));
                Debug.Log("[Qinglan Visual Import] PASS");
            }
            catch (Exception exception)
            {
                Debug.LogException(exception);
                exitCode = 1;
            }

            EditorApplication.Exit(exitCode);
        }

        private static string Required(string name)
        {
            var value = Environment.GetEnvironmentVariable(name);
            if (string.IsNullOrWhiteSpace(value))
                throw new InvalidOperationException(name + " is required.");
            return value;
        }

        private static int ParsePositive(string name, int fallback)
        {
            var text = Environment.GetEnvironmentVariable(name);
            if (string.IsNullOrWhiteSpace(text)) return fallback;
            if (!int.TryParse(text, NumberStyles.Integer, CultureInfo.InvariantCulture, out var value) || value <= 0)
                throw new InvalidOperationException(name + " must be a positive integer.");
            return value;
        }

        private static float ParseUnitFloat(string name, float fallback)
        {
            var text = Environment.GetEnvironmentVariable(name);
            if (string.IsNullOrWhiteSpace(text)) return fallback;
            if (!float.TryParse(text, NumberStyles.Float, CultureInfo.InvariantCulture, out var value) ||
                value < 0f || value > 1f)
                throw new InvalidOperationException(name + " must be between zero and one.");
            return value;
        }
    }

    /// <summary>Batchmode entry point that authors a VisualProfile from an imported Sprite subasset.</summary>
    public static class QinglanVisualProfileCreateCommand
    {
        public static void Run()
        {
            var exitCode = 0;
            try
            {
                QinglanVisualAssetImporter.CreateVisualProfileAsset(
                    Required("QINGLAN_VISUAL_ASSET_PATH"),
                    Required("QINGLAN_VISUAL_PROFILE_SPRITE"),
                    Required("QINGLAN_VISUAL_PROFILE_PATH"),
                    Required("QINGLAN_VISUAL_PROFILE_STABLE_ID"),
                    ParseEntityKind(),
                    new Vector2(
                        ParsePositiveFloat("QINGLAN_VISUAL_PROFILE_SIZE_X", 1f),
                        ParsePositiveFloat("QINGLAN_VISUAL_PROFILE_SIZE_Y", 1f)));
                Debug.Log("[Qinglan Visual Profile Create] PASS");
            }
            catch (Exception exception)
            {
                Debug.LogException(exception);
                exitCode = 1;
            }
            EditorApplication.Exit(exitCode);
        }

        private static string Required(string name)
        {
            var value = Environment.GetEnvironmentVariable(name);
            if (string.IsNullOrWhiteSpace(value)) throw new InvalidOperationException(name + " is required.");
            return value;
        }

        private static Game.Simulation.EntityKind ParseEntityKind()
        {
            var text = Environment.GetEnvironmentVariable("QINGLAN_VISUAL_PROFILE_ENTITY_KIND");
            if (string.IsNullOrWhiteSpace(text)) return Game.Simulation.EntityKind.Actor;
            if (!Enum.TryParse(text, true, out Game.Simulation.EntityKind value) ||
                !Enum.IsDefined(typeof(Game.Simulation.EntityKind), value))
                throw new InvalidOperationException("QINGLAN_VISUAL_PROFILE_ENTITY_KIND is invalid.");
            return value;
        }

        private static float ParsePositiveFloat(string name, float fallback)
        {
            var text = Environment.GetEnvironmentVariable(name);
            if (string.IsNullOrWhiteSpace(text)) return fallback;
            if (!float.TryParse(text, NumberStyles.Float, CultureInfo.InvariantCulture, out var value) || value <= 0f)
                throw new InvalidOperationException(name + " must be a positive number.");
            return value;
        }
    }

    /// <summary>Batchmode entry point that registers one already-approved formal profile.</summary>
    public static class QinglanVisualProfileRegisterCommand
    {
        public static void Run()
        {
            var exitCode = 0;
            try
            {
                QinglanVisualAssetImporter.RegisterApprovedFile(
                    Required("QINGLAN_VISUAL_PROFILE_PATH"),
                    Required("QINGLAN_VISUAL_PROFILE_ADDRESS"));
                Debug.Log("[Qinglan Visual Profile Register] PASS");
            }
            catch (Exception exception)
            {
                Debug.LogException(exception);
                exitCode = 1;
            }
            EditorApplication.Exit(exitCode);
        }

        private static string Required(string name)
        {
            var value = Environment.GetEnvironmentVariable(name);
            if (string.IsNullOrWhiteSpace(value)) throw new InvalidOperationException(name + " is required.");
            return value;
        }
    }
}
