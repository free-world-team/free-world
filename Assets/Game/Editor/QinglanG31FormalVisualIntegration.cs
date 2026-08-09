using System;
using System.Collections.Generic;
using System.IO;
using Game.Presentation;
using Game.Simulation;
using UnityEditor;
using UnityEditor.AddressableAssets;
using UnityEditor.AddressableAssets.Settings;
using UnityEditor.AddressableAssets.Build;
using UnityEngine;

namespace Game.Editor
{
    /// <summary>Authors the indirect G3.1 runtime catalog from approved visual entries.</summary>
    public static class QinglanG31FormalVisualIntegration
    {
        public const string CatalogPath =
            "Assets/GameContent/QinglanDemo/Profiles/Visual/G3-1-INTEGRATION/formal-visual-catalog.asset";

        public static void Run()
        {
            var exitCode = 0;
            try
            {
                var catalog = BuildCatalog();
                Debug.Log("[Qinglan G3.1 Formal Visual Catalog] PASS: profiles=" +
                          catalog.EntityProfileCount + ", bindings=" + catalog.BindingCount);
            }
            catch (Exception exception)
            {
                Debug.LogException(exception);
                exitCode = 1;
            }
            EditorApplication.Exit(exitCode);
        }

        public static FormalVisualCatalog BuildCatalog()
        {
            var settings = AddressableAssetSettingsDefaultObject.GetSettings(false);
            if (settings == null) throw new InvalidOperationException("Addressables settings are unavailable.");
            var visualGroup = settings.FindGroup(AssetProvenanceValidator.QinglanVisualGroup);
            if (visualGroup == null) throw new InvalidOperationException("Qinglan visual group is unavailable.");

            var directory = Path.GetDirectoryName(CatalogPath);
            if (string.IsNullOrWhiteSpace(directory)) throw new InvalidOperationException("Catalog path is invalid.");
            Directory.CreateDirectory(directory);
            var catalog = AssetDatabase.LoadAssetAtPath<FormalVisualCatalog>(CatalogPath);
            if (catalog == null)
            {
                catalog = ScriptableObject.CreateInstance<FormalVisualCatalog>();
                AssetDatabase.CreateAsset(catalog, CatalogPath);
            }
            RemoveGeneratedProfiles(catalog);

            var profiles = FindExternalProfiles();
            var bindings = new List<BindingDraft>(visualGroup.entries.Count + 48);
            var entries = new List<AddressableAssetEntry>();
            foreach (var entry in visualGroup.entries)
            {
                if (entry == null ||
                    string.Equals(entry.address, Game.Infrastructure.QinglanFormalVisualLoader.CatalogAddress,
                        StringComparison.Ordinal))
                    continue;
                if (!entry.labels.Contains(AssetProvenanceValidator.VisualReleaseLabel)) continue;
                entries.Add(entry);
            }
            entries.Sort((left, right) => string.CompareOrdinal(left.address, right.address));

            for (var index = 0; index < entries.Count; index++)
            {
                var entry = entries[index];
                var path = AssetDatabase.GUIDToAssetPath(entry.guid).Replace('\\', '/');
                var sprite = ShouldPreload(entry.address) ? FindSingleSprite(path) : null;
                bindings.Add(new BindingDraft(entry.address, entry.address, UsageFor(entry.address), sprite));
                AddAliases(entry.address, path, sprite, bindings);
            }

            AddGeneratedProfiles(catalog, bindings, profiles);
            WriteCatalog(catalog, profiles, bindings);
            RegisterCatalog(settings, visualGroup);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh(ImportAssetOptions.ForceSynchronousImport);
            return catalog;
        }

        public static void AppendCurrentProjectValidation(
            AddressableAssetSettings settings,
            ValidationReport report)
        {
            if (report == null) throw new ArgumentNullException(nameof(report));
            if (settings == null)
            {
                report.Add("G31-VISUAL-CATALOG", "Addressables settings are unavailable.");
                return;
            }
            var visualGroup = settings.FindGroup(AssetProvenanceValidator.QinglanVisualGroup);
            if (visualGroup == null)
            {
                report.Add("G31-VISUAL-CATALOG", "Qinglan visual group is unavailable.");
                return;
            }

            AddressableAssetEntry catalogEntry = null;
            foreach (var entry in visualGroup.entries)
            {
                if (entry != null && string.Equals(
                        entry.address,
                        Game.Infrastructure.QinglanFormalVisualLoader.CatalogAddress,
                        StringComparison.Ordinal))
                {
                    catalogEntry = entry;
                    break;
                }
            }
            if (catalogEntry == null ||
                !catalogEntry.labels.Contains(AssetProvenanceValidator.QinglanPackLabel))
            {
                report.Add(
                    "G31-VISUAL-CATALOG",
                    "Formal visual catalog must be Addressable in QinglanDemo-Visual with pack.qinglan_demo.");
                return;
            }

            var catalog = AssetDatabase.LoadAssetAtPath<FormalVisualCatalog>(CatalogPath);
            if (catalog == null || catalog.SchemaVersion != FormalVisualCatalog.CurrentSchemaVersion)
            {
                report.Add("G31-VISUAL-CATALOG", "Formal visual catalog is missing or has an unsupported schema.");
                return;
            }
            if (catalog.EntityProfileCount < 34)
                report.Add("G31-VISUAL-PROFILES", "Formal visual catalog requires at least 34 entity profiles.");

            var releaseAddressCount = 0;
            foreach (var entry in visualGroup.entries)
            {
                if (entry == null || !entry.labels.Contains(AssetProvenanceValidator.VisualReleaseLabel)) continue;
                releaseAddressCount++;
                if (!catalog.ContainsAddress(entry.address))
                    report.Add("G31-VISUAL-ADDRESS", "Formal visual catalog does not map " + entry.address + ".");
            }
            if (releaseAddressCount != 181)
                report.Add(
                    "G31-VISUAL-COUNT",
                    "Expected 181 approved visual.release entries but found " + releaseAddressCount + ".");

            var requiredSprites = new[]
            {
                "ui.page.title", "ui.page.title_safe", "ui.page.character_select", "ui.page.map_select",
                "ui.page.loadout", "ui.page.choice", "ui.page.hub", "ui.page.story_result", "ui.panel",
                "ui.focus", "ui.cursor.pointer", "qinglan.status.burning", "qinglan.status.poisoned"
            };
            for (var index = 0; index < requiredSprites.Length; index++)
                if (!catalog.TryResolveSprite(requiredSprites[index], out _))
                    report.Add("G31-VISUAL-RUNTIME", "Required runtime sprite is missing: " + requiredSprites[index] + ".");

            AssertProfile(catalog, report, "qinglan.character.lu_qingye", EntityKind.Actor);
            AssertProfile(catalog, report, "qinglan.enemy.boss.tingfeng", EntityKind.Actor);
            AssertProfile(catalog, report, "qinglan.pickup.greenwood_dew", EntityKind.Pickup);
            AssertProfile(catalog, report, "qinglan.affix.rampaging", EntityKind.Actor);
        }

        private static void AssertProfile(
            FormalVisualCatalog catalog,
            ValidationReport report,
            string stableId,
            EntityKind kind)
        {
            var id = Game.Core.ContentId.Create(stableId);
            if (!id.IsSuccess || !catalog.TryResolveProfile(id.Value, kind, out var profile) || profile.Sprite == null)
                report.Add("G31-VISUAL-PROFILES", "Required formal profile is missing: " + stableId + ".");
        }

        private static List<VisualProfile> FindExternalProfiles()
        {
            var result = new List<VisualProfile>();
            var guids = AssetDatabase.FindAssets(
                "t:VisualProfile",
                new[] { "Assets/GameContent/QinglanDemo/Profiles/Visual" });
            Array.Sort(guids, StringComparer.Ordinal);
            for (var index = 0; index < guids.Length; index++)
            {
                var path = AssetDatabase.GUIDToAssetPath(guids[index]);
                if (string.Equals(path, CatalogPath, StringComparison.Ordinal)) continue;
                var profile = AssetDatabase.LoadAssetAtPath<VisualProfile>(path);
                if (profile != null && !AssetDatabase.IsSubAsset(profile)) result.Add(profile);
            }
            return result;
        }

        private static void RemoveGeneratedProfiles(FormalVisualCatalog catalog)
        {
            var values = AssetDatabase.LoadAllAssetsAtPath(CatalogPath);
            for (var index = values.Length - 1; index >= 0; index--)
                if (values[index] is VisualProfile && values[index] != catalog)
                    UnityEngine.Object.DestroyImmediate(values[index], true);
        }

        private static void AddGeneratedProfiles(
            FormalVisualCatalog catalog,
            List<BindingDraft> bindings,
            List<VisualProfile> profiles)
        {
            for (var index = 0; index < bindings.Count; index++)
            {
                var binding = bindings[index];
                if (binding.Sprite == null) continue;
                if (binding.Address.StartsWith("qinglan/pickup/", StringComparison.Ordinal) &&
                    binding.Address.EndsWith("/sprite", StringComparison.Ordinal))
                {
                    var slug = Between(binding.Address, "qinglan/pickup/", "/sprite");
                    profiles.Add(CreateGeneratedProfile(
                        catalog,
                        "qinglan.pickup." + slug.Replace('-', '_'),
                        EntityKind.Pickup,
                        binding.Sprite,
                        new Vector2(0.58f, 0.58f)));
                }
            }

            AddAffixProfile(catalog, bindings, profiles, "splitting", "qinglan.affix.splitting");
            AddAffixProfile(catalog, bindings, profiles, "barrier", "qinglan.affix.barrier");
            AddAffixProfile(catalog, bindings, profiles, "quake", "qinglan.affix.quaking");
            AddAffixProfile(catalog, bindings, profiles, "frenzy", "qinglan.affix.rampaging");
        }

        private static void AddAffixProfile(
            FormalVisualCatalog catalog,
            List<BindingDraft> bindings,
            List<VisualProfile> profiles,
            string slug,
            string stableId)
        {
            var address = "qinglan/enemy/affix/" + slug + "/overlay";
            for (var index = 0; index < bindings.Count; index++)
            {
                if (!string.Equals(bindings[index].Address, address, StringComparison.Ordinal) ||
                    bindings[index].Sprite == null) continue;
                profiles.Add(CreateGeneratedProfile(
                    catalog,
                    stableId,
                    EntityKind.Actor,
                    bindings[index].Sprite,
                    new Vector2(1.28f, 1.28f)));
                return;
            }
            throw new InvalidOperationException("Affix runtime sprite is missing: " + address);
        }

        private static VisualProfile CreateGeneratedProfile(
            FormalVisualCatalog catalog,
            string stableId,
            EntityKind kind,
            Sprite sprite,
            Vector2 size)
        {
            var profile = ScriptableObject.CreateInstance<VisualProfile>();
            profile.name = "runtime-profile-" + stableId.Replace('.', '-');
            var serialized = new SerializedObject(profile);
            serialized.FindProperty("entityKind").intValue = (int)kind;
            serialized.FindProperty("stableId").stringValue = stableId;
            serialized.FindProperty("sprite").objectReferenceValue = sprite;
            serialized.FindProperty("color").colorValue = Color.white;
            serialized.FindProperty("size").vector2Value = size;
            serialized.ApplyModifiedPropertiesWithoutUndo();
            AssetDatabase.AddObjectToAsset(profile, catalog);
            return profile;
        }

        private static void AddAliases(
            string address,
            string path,
            Sprite primary,
            List<BindingDraft> bindings)
        {
            if (string.Equals(address, "qinglan/ui/title/key-art", StringComparison.Ordinal))
                bindings.Add(new BindingDraft("ui.page.title", address, FormalVisualUsage.UiBackground, primary));
            else if (string.Equals(address, "qinglan/ui/title/logo-safe-background", StringComparison.Ordinal))
                bindings.Add(new BindingDraft("ui.page.title_safe", address, FormalVisualUsage.UiBackground, primary));
            else if (address.StartsWith("qinglan/ui/page-background/", StringComparison.Ordinal))
            {
                var slug = address.Substring("qinglan/ui/page-background/".Length).Replace('-', '_');
                bindings.Add(new BindingDraft("ui.page." + slug, address, FormalVisualUsage.UiBackground, primary));
            }
            else if (string.Equals(address, "qinglan/ui/input-glyph/focus/ring", StringComparison.Ordinal))
                bindings.Add(new BindingDraft("ui.focus", address, FormalVisualUsage.UiChrome, primary));
            else if (string.Equals(address, "qinglan/ui/input-glyph/cursor/pointer", StringComparison.Ordinal))
                bindings.Add(new BindingDraft("ui.cursor.pointer", address, FormalVisualUsage.UiChrome, primary));
            else if (string.Equals(address, "qinglan/ui/framework/atlas", StringComparison.Ordinal))
            {
                var panel = FindSprite(path, "qinglan.presentation.ui.framework.panel.translucent");
                bindings.Add(new BindingDraft("ui.panel", address, FormalVisualUsage.UiChrome, panel));
            }
            else if (address.StartsWith("qinglan/status/", StringComparison.Ordinal) &&
                     address.EndsWith("/visual", StringComparison.Ordinal))
            {
                var slug = Between(address, "qinglan/status/", "/visual");
                bindings.Add(new BindingDraft(
                    "qinglan.status." + slug.Replace('-', '_'),
                    address,
                    FormalVisualUsage.StatusEffect,
                    primary));
            }
        }

        private static bool ShouldPreload(string address)
        {
            return address.StartsWith("qinglan/ui/", StringComparison.Ordinal) ||
                   address.StartsWith("qinglan/pickup/", StringComparison.Ordinal) ||
                   address.StartsWith("qinglan/enemy/affix/", StringComparison.Ordinal) ||
                   address.StartsWith("qinglan/status/", StringComparison.Ordinal);
        }

        private static FormalVisualUsage UsageFor(string address)
        {
            if (address.StartsWith("qinglan/ui/page-background/", StringComparison.Ordinal) ||
                address.StartsWith("qinglan/ui/title/", StringComparison.Ordinal))
                return FormalVisualUsage.UiBackground;
            if (address.StartsWith("qinglan/ui/", StringComparison.Ordinal)) return FormalVisualUsage.UiChrome;
            if (address.StartsWith("qinglan/status/", StringComparison.Ordinal)) return FormalVisualUsage.StatusEffect;
            if (ShouldPreload(address)) return FormalVisualUsage.RuntimeSprite;
            return FormalVisualUsage.CatalogOnly;
        }

        private static Sprite FindSingleSprite(string path)
        {
            Sprite result = null;
            var values = AssetDatabase.LoadAllAssetsAtPath(path);
            for (var index = 0; index < values.Length; index++)
            {
                if (!(values[index] is Sprite sprite)) continue;
                if (result != null) return null;
                result = sprite;
            }
            return result;
        }

        private static Sprite FindSprite(string path, string name)
        {
            var values = AssetDatabase.LoadAllAssetsAtPath(path);
            for (var index = 0; index < values.Length; index++)
                if (values[index] is Sprite sprite && string.Equals(sprite.name, name, StringComparison.Ordinal))
                    return sprite;
            throw new InvalidOperationException(name + " is missing from " + path);
        }

        private static string Between(string value, string prefix, string suffix) =>
            value.Substring(prefix.Length, value.Length - prefix.Length - suffix.Length);

        private static void WriteCatalog(
            FormalVisualCatalog catalog,
            List<VisualProfile> profiles,
            List<BindingDraft> bindings)
        {
            bindings.Sort((left, right) =>
            {
                var key = string.CompareOrdinal(left.StableKey, right.StableKey);
                return key != 0 ? key : string.CompareOrdinal(left.Address, right.Address);
            });
            var serialized = new SerializedObject(catalog);
            serialized.FindProperty("schemaVersion").intValue = FormalVisualCatalog.CurrentSchemaVersion;
            var profileArray = serialized.FindProperty("entityProfiles");
            profileArray.arraySize = profiles.Count;
            for (var index = 0; index < profiles.Count; index++)
                profileArray.GetArrayElementAtIndex(index).objectReferenceValue = profiles[index];
            var bindingArray = serialized.FindProperty("bindings");
            bindingArray.arraySize = bindings.Count;
            for (var index = 0; index < bindings.Count; index++)
            {
                var element = bindingArray.GetArrayElementAtIndex(index);
                element.FindPropertyRelative("stableKey").stringValue = bindings[index].StableKey;
                element.FindPropertyRelative("address").stringValue = bindings[index].Address;
                element.FindPropertyRelative("usage").intValue = (int)bindings[index].Usage;
                element.FindPropertyRelative("sprite").objectReferenceValue = bindings[index].Sprite;
            }
            serialized.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(catalog);
        }

        private static void RegisterCatalog(
            AddressableAssetSettings settings,
            AddressableAssetGroup visualGroup)
        {
            var guid = AssetDatabase.AssetPathToGUID(CatalogPath);
            if (string.IsNullOrEmpty(guid)) throw new InvalidOperationException("Catalog GUID is unavailable.");
            var entry = settings.CreateOrMoveEntry(guid, visualGroup, false, false);
            entry.address = Game.Infrastructure.QinglanFormalVisualLoader.CatalogAddress;
            entry.SetLabel(AssetProvenanceValidator.QinglanPackLabel, true, false, false);
            entry.SetLabel(AssetProvenanceValidator.ReleaseLabel, false, false, false);
            entry.SetLabel(AssetProvenanceValidator.VisualReleaseLabel, false, false, false);
            entry.SetLabel(PlaceholderAssetGenerator.PlaceholderLabel, false, false, false);
            entry.SetLabel(PlaceholderAssetGenerator.DevelopmentOnlyLabel, false, false, false);
            settings.SetDirty(AddressableAssetSettings.ModificationEvent.BatchModification, entry, true, true);
        }

        private readonly struct BindingDraft
        {
            public BindingDraft(string stableKey, string address, FormalVisualUsage usage, Sprite sprite)
            {
                StableKey = stableKey;
                Address = address;
                Usage = usage;
                Sprite = sprite;
            }

            public string StableKey { get; }
            public string Address { get; }
            public FormalVisualUsage Usage { get; }
            public Sprite Sprite { get; }
        }
    }

    public static class QinglanG31AddressablesBuildCommand
    {
        public static void Run()
        {
            var exitCode = 0;
            try
            {
                AddressableAssetSettings.BuildPlayerContent(out AddressablesPlayerBuildResult result);
                if (!string.IsNullOrEmpty(result.Error))
                    throw new InvalidOperationException(result.Error);
                Debug.Log("[Qinglan G3.1 Addressables Build] PASS: output=" + result.OutputPath +
                          ", duration=" + result.Duration + ", locationCount=" + result.LocationCount);
            }
            catch (Exception exception)
            {
                Debug.LogException(exception);
                exitCode = 1;
            }
            finally
            {
                WindowsDevelopmentBuild.RemoveTemporaryAddressablesLinkXml();
            }
            EditorApplication.Exit(exitCode);
        }
    }
}
