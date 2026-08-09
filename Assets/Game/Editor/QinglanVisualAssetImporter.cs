using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using UnityEditor;
using UnityEditor.AddressableAssets;
using UnityEditor.AddressableAssets.Settings;
using UnityEditor.AddressableAssets.Settings.GroupSchemas;
using UnityEditor.U2D.Sprites;
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
            var sourceSize = ReadSourceDimensions(assetPath);
            var sourceWidth = sourceSize.x;
            var sourceHeight = sourceSize.y;
            if (sourceWidth % columns != 0 || sourceHeight % rows != 0)
                throw new InvalidOperationException("Texture dimensions are not divisible by the requested grid.");
            var useDownsampledSingleSprite =
                columns == 1 && rows == 1 &&
                (sourceWidth > maxTextureSize || sourceHeight > maxTextureSize);
            if (!useDownsampledSingleSprite &&
                (sourceWidth > maxTextureSize || sourceHeight > maxTextureSize))
                throw new InvalidOperationException("Multi-sprite source exceeds its runtime maximum size.");

            var importer = AssetImporter.GetAtPath(assetPath) as TextureImporter;
            if (importer == null) throw new InvalidOperationException("TextureImporter is unavailable for " + assetPath);
            importer.textureType = TextureImporterType.Sprite;
            importer.spriteImportMode = useDownsampledSingleSprite
                ? SpriteImportMode.Single
                : SpriteImportMode.Multiple;
            importer.spritePixelsPerUnit = 128f;
            importer.mipmapEnabled = false;
            importer.alphaIsTransparency = true;
            importer.wrapMode = TextureWrapMode.Clamp;
            importer.filterMode = FilterMode.Bilinear;
            importer.maxTextureSize = maxTextureSize;
            var runtimeCursor = string.Equals(
                address,
                "qinglan/ui/input-glyph/cursor/pointer",
                StringComparison.Ordinal);
            importer.isReadable = runtimeCursor;
            importer.textureCompression = runtimeCursor ?
                TextureImporterCompression.Uncompressed : TextureImporterCompression.CompressedHQ;

            var standalone = importer.GetPlatformTextureSettings("Standalone");
            standalone.overridden = true;
            standalone.maxTextureSize = maxTextureSize;
            standalone.format = runtimeCursor ? TextureImporterFormat.RGBA32 : TextureImporterFormat.BC7;
            standalone.compressionQuality = 100;
            importer.SetPlatformTextureSettings(standalone);
            if (useDownsampledSingleSprite)
            {
                var textureSettings = new TextureImporterSettings();
                importer.ReadTextureSettings(textureSettings);
                textureSettings.spriteAlignment = (int)SpriteAlignment.Custom;
                textureSettings.spritePivot = pivot;
                importer.SetTextureSettings(textureSettings);
            }
            importer.SaveAndReimport();

            var importedTexture = AssetDatabase.LoadAssetAtPath<Texture2D>(assetPath);
            if (importedTexture == null)
                throw new InvalidOperationException(assetPath + " did not import as Texture2D.");
            if (importedTexture.width > maxTextureSize || importedTexture.height > maxTextureSize)
                throw new InvalidOperationException("Imported texture exceeds its runtime maximum size.");
            if (useDownsampledSingleSprite)
            {
                RegisterApprovedFile(assetPath, address);
                return;
            }
            importer = AssetImporter.GetAtPath(assetPath) as TextureImporter;
            if (importer == null)
                throw new InvalidOperationException("TextureImporter was unavailable after reimport for " + assetPath);

            var factory = new SpriteDataProviderFactories();
            factory.Init();
            var dataProvider = factory.GetSpriteEditorDataProviderFromObject(importer);
            if (dataProvider == null)
                throw new InvalidOperationException("Sprite data provider is unavailable for " + assetPath);
            dataProvider.InitSpriteEditorDataProvider();
            var existingSpriteIds = new Dictionary<string, GUID>(StringComparer.Ordinal);
            var existingSpriteRects = dataProvider.GetSpriteRects();
            for (var index = 0; index < existingSpriteRects.Length; index++)
                existingSpriteIds[existingSpriteRects[index].name] = existingSpriteRects[index].spriteID;

            var cellWidth = importedTexture.width / columns;
            var cellHeight = importedTexture.height / rows;
            var spriteRects = new SpriteRect[columns * rows];
            var nameFileIdPairs = new SpriteNameFileIdPair[columns * rows];
            for (var row = 0; row < rows; row++)
            {
                for (var column = 0; column < columns; column++)
                {
                    var index = row * columns + column;
                    var spriteName = BuildSpriteName(spriteNamePrefix, row, column, rows, columns);
                    var spriteId = existingSpriteIds.TryGetValue(spriteName, out var existingSpriteId)
                        ? existingSpriteId
                        : GUID.Generate();
                    spriteRects[index] = new SpriteRect
                    {
                        name = spriteName,
                        rect = new Rect(
                            column * cellWidth,
                            importedTexture.height - (row + 1) * cellHeight,
                            cellWidth,
                            cellHeight),
                        alignment = SpriteAlignment.Custom,
                        pivot = pivot,
                        border = BuildSpriteBorder(spriteNamePrefix, row, rows, columns)
                    };
                    spriteRects[index].spriteID = spriteId;
                    nameFileIdPairs[index] = new SpriteNameFileIdPair(spriteName, spriteId);
                }
            }

            dataProvider.SetSpriteRects(spriteRects);
            var nameFileIdProvider = dataProvider.GetDataProvider<ISpriteNameFileIdDataProvider>();
            nameFileIdProvider?.SetNameFileIdPairs(nameFileIdPairs);
            dataProvider.Apply();
            importer.SaveAndReimport();

            RegisterApprovedFile(assetPath, address);
        }

        private static Vector2Int ReadSourceDimensions(string assetPath)
        {
            var projectRoot = Directory.GetParent(UnityEngine.Application.dataPath)?.FullName;
            if (string.IsNullOrWhiteSpace(projectRoot))
                throw new InvalidOperationException("Unable to resolve project root.");
            var sourcePath = Path.Combine(projectRoot, assetPath.Replace('/', Path.DirectorySeparatorChar));
            var source = new Texture2D(2, 2, TextureFormat.RGBA32, false);
            try
            {
                if (!ImageConversion.LoadImage(source, File.ReadAllBytes(sourcePath), false))
                    throw new InvalidOperationException(assetPath + " could not be decoded for source dimensions.");
                return new Vector2Int(source.width, source.height);
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(source);
            }
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

            if (rows == 4 && columns == 4 &&
                prefix.EndsWith(".ui.framework", StringComparison.Ordinal))
            {
                var names = new[]
                {
                    "frame.standard", "frame.focused", "frame.disabled", "frame.danger",
                    "panel.solid", "panel.translucent", "panel.card", "panel.tooltip",
                    "icon.health", "icon.shield", "icon.experience", "icon.level",
                    "icon.time", "icon.objective", "icon.map", "icon.lock"
                };
                return prefix + "." + names[row * columns + column];
            }

            if (rows == 4 && columns == 4 &&
                prefix.EndsWith(".ui.telegraph-accessibility", StringComparison.Ordinal))
            {
                var names = new[]
                {
                    "shape.area-circle", "shape.directional-line", "shape.fan-cone", "shape.point-impact",
                    "shape.cross-lanes", "shape.hazard-ring", "shape.sweep-arc", "shape.direction-arrow",
                    "texture.diagonal-stripes", "texture.crosshatch", "texture.dots", "texture.chevrons",
                    "texture.radial-spokes", "texture.grid", "texture.broken-bars", "texture.concentric"
                };
                return prefix + "." + names[row * columns + column];
            }

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

        private static Vector4 BuildSpriteBorder(string prefix, int row, int rows, int columns)
        {
            if (rows == 4 && columns == 4 && row < 2 &&
                prefix.EndsWith(".ui.framework", StringComparison.Ordinal))
                return new Vector4(64f, 64f, 64f, 64f);
            return Vector4.zero;
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

    /// <summary>Batchmode entry point for the fixed ART-UI-005 cursor/focus/input glyph set.</summary>
    public static class QinglanUiGlyphSetImportCommand
    {
        private const string Root = "Assets/GameAssets/FirstParty/QinglanDemo/ART-UI-005/final/";

        private static readonly string[] Names =
        {
            "cursor.pointer", "focus.ring", "keyboard.wasd", "keyboard.arrows",
            "keyboard.enter", "keyboard.escape", "keyboard.key-e", "keyboard.key-m",
            "keyboard.key-q", "keyboard.page-axis", "mouse.left-button", "mouse.right-button",
            "mouse.scroll", "gamepad.left-stick", "gamepad.dpad", "gamepad.button-south",
            "gamepad.button-east", "gamepad.button-north", "gamepad.start", "gamepad.select",
            "gamepad.left-shoulder", "gamepad.right-shoulder", "gamepad.left-trigger", "gamepad.right-trigger"
        };

        public static void Run()
        {
            var exitCode = 0;
            try
            {
                for (var index = 0; index < Names.Length; index++)
                {
                    var name = Names[index];
                    QinglanVisualAssetImporter.ImportSpriteAtlas(
                        Root + name.Replace('.', '-') + ".png",
                        "qinglan/ui/input-glyph/" + name.Replace('.', '/'),
                        1,
                        1,
                        128,
                        "qinglan.presentation.ui.input-glyph." + name,
                        new Vector2(0.5f, 0.5f));
                }
                Debug.Log("[Qinglan UI Glyph Set Import] PASS glyphs=" + Names.Length);
            }
            catch (Exception exception)
            {
                Debug.LogException(exception);
                exitCode = 1;
            }
            EditorApplication.Exit(exitCode);
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
