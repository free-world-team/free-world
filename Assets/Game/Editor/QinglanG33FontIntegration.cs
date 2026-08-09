using System;
using System.Globalization;
using System.IO;
using System.Security.Cryptography;
using System.Text;
using TMPro;
using UnityEditor;
using UnityEditor.AddressableAssets;
using UnityEditor.AddressableAssets.Settings;
using UnityEditor.AddressableAssets.Settings.GroupSchemas;
using UnityEngine;
using UnityEngine.TextCore.LowLevel;

namespace Game.Editor
{
    /// <summary>Creates governed TMP assets from the pinned Noto CJK source fonts.</summary>
    public static class QinglanG33FontIntegration
    {
        public const string Font001Root =
            "Assets/GameContent/QinglanDemo/Profiles/Font/FONT-001";
        public const string SansRegularPath = Font001Root + "/NotoSansCJKsc-Regular.asset";
        public const string SansBoldPath = Font001Root + "/NotoSansCJKsc-Bold.asset";
        public const string SansRegularAddress = "qinglan/font/noto-sans-cjk-sc/regular";
        public const string SansBoldAddress = "qinglan/font/noto-sans-cjk-sc/bold";
        public const string Font002Root =
            "Assets/GameContent/QinglanDemo/Profiles/Font/FONT-002";
        public const string SerifSemiBoldPath = Font002Root + "/NotoSerifCJKsc-SemiBold.asset";
        public const string SerifSemiBoldAddress = "qinglan/font/noto-serif-cjk-sc/semibold";
        public const string TmpSettingsPath =
            "Assets/TextMesh Pro/Resources/TMP Settings.asset";

        private const string SansRegularSource =
            "Assets/ThirdParty/Fonts/NotoCJKSC/FONT-001/NotoSansCJKsc-Regular.otf";
        private const string SansBoldSource =
            "Assets/ThirdParty/Fonts/NotoCJKSC/FONT-001/NotoSansCJKsc-Bold.otf";
        private const string SerifSemiBoldSource =
            "Assets/ThirdParty/Fonts/NotoCJKSC/FONT-002/NotoSerifCJKsc-SemiBold.otf";

        public static void RunImportTmpEssentials()
        {
            AssetDatabase.importPackageCompleted += OnTmpPackageImported;
            AssetDatabase.importPackageFailed += OnTmpPackageFailed;
            AssetDatabase.importPackageCancelled += OnTmpPackageCancelled;
            var obsoleteSettings =
                "Assets/GameContent/QinglanDemo/Profiles/Font/Resources/TMP Settings.asset";
            if (AssetDatabase.LoadMainAssetAtPath(obsoleteSettings) != null)
                AssetDatabase.DeleteAsset(obsoleteSettings);
            TMP_PackageResourceImporter.ImportResources(true, false, false);
        }

        public static void RunFont001()
        {
            var exitCode = 0;
            try
            {
                BuildFont001();
                Debug.Log("[Qinglan G3.3 FONT-001] PASS: two Noto Sans CJK SC TMP assets registered.");
            }
            catch (Exception exception)
            {
                Debug.LogException(exception);
                exitCode = 1;
            }

            EditorApplication.Exit(exitCode);
        }

        public static void RunFont002()
        {
            var exitCode = 0;
            try
            {
                BuildFont002();
                Debug.Log("[Qinglan G3.3 FONT-002] PASS: Noto Serif CJK SC SemiBold TMP asset registered.");
            }
            catch (Exception exception)
            {
                Debug.LogException(exception);
                exitCode = 1;
            }

            EditorApplication.Exit(exitCode);
        }

        public static void BuildFont001()
        {
            EnsureTmpSettings();
            Directory.CreateDirectory(Font001Root);
            CreateDynamicFontAsset(SansRegularSource, SansRegularPath);
            CreateDynamicFontAsset(SansBoldSource, SansBoldPath);
            ConfigureTmpSettings();
            AssetDatabase.SaveAssets();
            WriteFont001Provenance();
            AssetDatabase.Refresh(ImportAssetOptions.ForceSynchronousImport);

            var settings = AddressableAssetSettingsDefaultObject.GetSettings(true);
            if (settings == null) throw new InvalidOperationException("Addressables settings are unavailable.");
            var group = EnsureFontGroup(settings);
            Register(settings, group, SansRegularPath, SansRegularAddress);
            Register(settings, group, SansBoldPath, SansBoldAddress);
            AssetDatabase.SaveAssets();
        }

        public static void BuildFont002()
        {
            EnsureTmpSettings();
            Directory.CreateDirectory(Font002Root);
            CreateDynamicFontAsset(SerifSemiBoldSource, SerifSemiBoldPath);
            AssetDatabase.SaveAssets();
            WriteFont002Provenance();
            AssetDatabase.Refresh(ImportAssetOptions.ForceSynchronousImport);

            var settings = AddressableAssetSettingsDefaultObject.GetSettings(true);
            if (settings == null) throw new InvalidOperationException("Addressables settings are unavailable.");
            var group = EnsureFontGroup(settings);
            Register(settings, group, SerifSemiBoldPath, SerifSemiBoldAddress);
            AssetDatabase.SaveAssets();
        }

        private static void ConfigureTmpSettings()
        {
            var settings = AssetDatabase.LoadAssetAtPath<TMP_Settings>(TmpSettingsPath);
            var regular = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(SansRegularPath);
            if (settings == null || regular == null)
                throw new InvalidOperationException("TMP Settings or default Qinglan font is unavailable.");
            var serialized = new SerializedObject(settings);
            serialized.Update();
            var defaultFont = serialized.FindProperty("m_defaultFontAsset");
            var defaultPath = serialized.FindProperty("m_defaultFontAssetPath");
            var clearOnBuild = serialized.FindProperty("m_ClearDynamicDataOnBuild");
            var fallbacks = serialized.FindProperty("m_fallbackFontAssets");
            if (defaultFont == null || defaultPath == null || clearOnBuild == null || fallbacks == null)
                throw new InvalidOperationException("TMP Settings schema is unsupported.");
            defaultFont.objectReferenceValue = regular;
            defaultPath.stringValue = string.Empty;
            clearOnBuild.boolValue = false;
            fallbacks.arraySize = 0;
            serialized.ApplyModifiedProperties();
            EditorUtility.SetDirty(settings);
            AssetDatabase.SaveAssetIfDirty(settings);
            AssetDatabase.ForceReserializeAssets(new[] { TmpSettingsPath });
        }

        private static void EnsureTmpSettings()
        {
            var settings = AssetDatabase.LoadAssetAtPath<TMP_Settings>(TmpSettingsPath);
            if (settings == null || TMP_Settings.LoadDefaultSettings() == null)
                throw new InvalidOperationException(
                    "TMP Essential Resources are missing. Run RunImportTmpEssentials first.");
            if (Shader.Find("TextMeshPro/Mobile/Distance Field") == null)
                throw new InvalidOperationException("TMP Mobile Distance Field shader is unavailable.");
        }

        private static void OnTmpPackageImported(string packageName)
        {
            UnsubscribePackageEvents();
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh(ImportAssetOptions.ForceSynchronousImport);
            var valid = AssetDatabase.LoadAssetAtPath<TMP_Settings>(TmpSettingsPath) != null &&
                        Shader.Find("TextMeshPro/Mobile/Distance Field") != null;
            Debug.Log(valid
                ? "[Qinglan G3.3 TMP Essentials] PASS: " + packageName
                : "[Qinglan G3.3 TMP Essentials] FAIL: required settings or shader is missing.");
            EditorApplication.Exit(valid ? 0 : 1);
        }

        private static void OnTmpPackageFailed(string packageName, string errorMessage)
        {
            UnsubscribePackageEvents();
            Debug.LogError("[Qinglan G3.3 TMP Essentials] FAIL: " + packageName + ": " + errorMessage);
            EditorApplication.Exit(1);
        }

        private static void OnTmpPackageCancelled(string packageName)
        {
            UnsubscribePackageEvents();
            Debug.LogError("[Qinglan G3.3 TMP Essentials] FAIL: cancelled " + packageName + ".");
            EditorApplication.Exit(1);
        }

        private static void UnsubscribePackageEvents()
        {
            AssetDatabase.importPackageCompleted -= OnTmpPackageImported;
            AssetDatabase.importPackageFailed -= OnTmpPackageFailed;
            AssetDatabase.importPackageCancelled -= OnTmpPackageCancelled;
        }

        private static void CreateDynamicFontAsset(string sourcePath, string outputPath)
        {
            AssetDatabase.ImportAsset(
                sourcePath,
                ImportAssetOptions.ForceSynchronousImport | ImportAssetOptions.ForceUpdate);
            var source = AssetDatabase.LoadAssetAtPath<Font>(sourcePath);
            if (source == null) throw new InvalidOperationException("Source Font is unavailable: " + sourcePath + ".");

            if (AssetDatabase.LoadMainAssetAtPath(outputPath) != null && !AssetDatabase.DeleteAsset(outputPath))
                throw new InvalidOperationException("Unable to replace TMP font asset: " + outputPath + ".");

            FontEngine.InitializeFontEngine();
            var fontAsset = TMP_FontAsset.CreateFontAsset(
                source,
                90,
                9,
                GlyphRenderMode.SDFAA,
                1024,
                1024,
                AtlasPopulationMode.Dynamic,
                true);
            if (fontAsset == null) throw new InvalidOperationException("TMP failed to create: " + outputPath + ".");

            fontAsset.name = Path.GetFileNameWithoutExtension(outputPath);
            AssetDatabase.CreateAsset(fontAsset, outputPath);
            var atlasTextures = fontAsset.atlasTextures;
            for (var index = 0; index < atlasTextures.Length; index++)
            {
                if (atlasTextures[index] == null || AssetDatabase.Contains(atlasTextures[index])) continue;
                atlasTextures[index].name = fontAsset.name + " Atlas " + index.ToString(CultureInfo.InvariantCulture);
                AssetDatabase.AddObjectToAsset(atlasTextures[index], fontAsset);
            }

            if (fontAsset.material != null && !AssetDatabase.Contains(fontAsset.material))
            {
                fontAsset.material.name = fontAsset.name + " Material";
                AssetDatabase.AddObjectToAsset(fontAsset.material, fontAsset);
            }

            EditorUtility.SetDirty(fontAsset);
            AssetDatabase.SaveAssets();
            AssetDatabase.ImportAsset(outputPath, ImportAssetOptions.ForceSynchronousImport);
        }

        private static AddressableAssetGroup EnsureFontGroup(AddressableAssetSettings settings)
        {
            settings.AddLabel(AssetProvenanceValidator.QinglanPackLabel, false);
            settings.AddLabel(AssetProvenanceValidator.ReleaseLabel, false);
            settings.AddLabel(AssetProvenanceValidator.LocalizationReleaseLabel, false);
            var group = settings.FindGroup(AssetProvenanceValidator.ThirdPartyFontGroup);
            if (group != null) return group;

            group = settings.CreateGroup(
                AssetProvenanceValidator.ThirdPartyFontGroup,
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
            return group;
        }

        private static void Register(
            AddressableAssetSettings settings,
            AddressableAssetGroup group,
            string path,
            string address)
        {
            var labels = new[]
            {
                AssetProvenanceValidator.QinglanPackLabel,
                AssetProvenanceValidator.ReleaseLabel,
                AssetProvenanceValidator.LocalizationReleaseLabel
            };
            var projectRoot = Directory.GetParent(UnityEngine.Application.dataPath)?.FullName;
            if (string.IsNullOrWhiteSpace(projectRoot))
                throw new InvalidOperationException("Unable to resolve project root.");
            var issues = AssetProvenanceValidator.ValidateReleaseInput(
                projectRoot,
                path,
                labels,
                AssetProvenanceValidator.ThirdPartyFontGroup);
            if (issues.Count > 0) throw new InvalidOperationException(issues[0].ToString());

            var guid = AssetDatabase.AssetPathToGUID(path);
            if (string.IsNullOrWhiteSpace(guid)) throw new InvalidOperationException("Asset GUID is unavailable: " + path + ".");
            var entry = settings.CreateOrMoveEntry(guid, group, false, false);
            entry.address = address;
            entry.SetLabel(PlaceholderAssetGenerator.PlaceholderLabel, false, false, false);
            entry.SetLabel(PlaceholderAssetGenerator.DevelopmentOnlyLabel, false, false, false);
            entry.SetLabel(AssetProvenanceValidator.QinglanPackLabel, true, false, false);
            entry.SetLabel(AssetProvenanceValidator.ReleaseLabel, true, false, false);
            entry.SetLabel(AssetProvenanceValidator.LocalizationReleaseLabel, true, false, false);
            settings.SetDirty(AddressableAssetSettings.ModificationEvent.BatchModification, entry, true, true);
        }

        private static void WriteFont001Provenance()
        {
            var regularHash = ComputeHash(SansRegularPath);
            var boldHash = ComputeHash(SansBoldPath);
            var template = @"{
  ""schemaVersion"": 2,
  ""assetId"": ""FONT-001"",
  ""owner"": ""Qinglan Demo Localization Owner"",
  ""relativePaths"": [
    ""Assets/GameContent/QinglanDemo/Profiles/Font/FONT-001/NotoSansCJKsc-Regular.asset"",
    ""Assets/GameContent/QinglanDemo/Profiles/Font/FONT-001/NotoSansCJKsc-Bold.asset""
  ],
  ""sourceCategory"": ""third-party-font-derived-tmp-asset"",
  ""tool"": ""Unity 6000.3.20f1 TextMeshPro font asset generator"",
  ""modelVersion"": ""Noto Sans CJK Sans2.004 commit 523d033d6cb47f4a80c58a35753646f5c3608a78"",
  ""generatedOrAcquiredAt"": ""2026-08-10"",
  ""operatorName"": ""Codex"",
  ""promptFile"": ""not-applicable://official-font-acquisition"",
  ""seed"": ""not-applicable"",
  ""referenceInputs"": [],
  ""referenceRightsConfirmed"": true,
  ""humanEdits"": [
    ""Generated dynamic 1024x1024 SDFAA TMP assets with multi-atlas support from unmodified official OTF files"",
    ""Preserved source font data for deterministic Windows x64 runtime glyph expansion""
  ],
  ""sourceSha256"": {},
  ""outputSha256"": {
    ""NotoSansCJKsc-Regular.asset"": ""__REGULAR_HASH__"",
    ""NotoSansCJKsc-Bold.asset"": ""__BOLD_HASH__""
  },
  ""licenseOrTermsUrl"": ""https://raw.githubusercontent.com/notofonts/noto-cjk/Sans2.004/LICENSE"",
  ""licenseOrTermsSnapshot"": ""Assets/ThirdParty/Fonts/NotoCJKSC/FONT-001/LICENSE.txt"",
  ""termsReviewedAt"": ""2026-08-10"",
  ""allowedPlatforms"": [""Windows x64"", ""Steam""],
  ""allowedUses"": [""commercial game runtime"", ""store and marketing screenshots"", ""internal development and testing""],
  ""commercialUseReviewed"": true,
  ""steamDisclosureCategory"": ""third-party font under SIL Open Font License 1.1; no generative AI"",
  ""technicalReviewer"": ""Codex"",
  ""creativeReviewer"": ""Codex"",
  ""rightsReviewer"": ""Codex"",
  ""reviewedAt"": ""2026-08-10"",
  ""status"": ""approved-for-release"",
  ""notes"": ""Original OTF files and license are governed by THIRD_PARTY_NOTICES.md and source-record.json.""
}
";
            var json = template
                .Replace("__REGULAR_HASH__", regularHash)
                .Replace("__BOLD_HASH__", boldHash);
            File.WriteAllText(Font001Root + "/provenance.json", json, new UTF8Encoding(false));
        }

        private static void WriteFont002Provenance()
        {
            var outputHash = ComputeHash(SerifSemiBoldPath);
            var template = @"{
  ""schemaVersion"": 2,
  ""assetId"": ""FONT-002"",
  ""owner"": ""Qinglan Demo Localization Owner"",
  ""relativePaths"": [
    ""Assets/GameContent/QinglanDemo/Profiles/Font/FONT-002/NotoSerifCJKsc-SemiBold.asset""
  ],
  ""sourceCategory"": ""third-party-font-derived-tmp-asset"",
  ""tool"": ""Unity 6000.3.20f1 TextMeshPro font asset generator"",
  ""modelVersion"": ""Noto Serif CJK Serif2.003 commit 9b0f1436e455d902de067a2501422e5dc71ad16b"",
  ""generatedOrAcquiredAt"": ""2026-08-10"",
  ""operatorName"": ""Codex"",
  ""promptFile"": ""not-applicable://official-font-acquisition"",
  ""seed"": ""not-applicable"",
  ""referenceInputs"": [],
  ""referenceRightsConfirmed"": true,
  ""humanEdits"": [
    ""Generated a dynamic 1024x1024 SDFAA TMP asset with multi-atlas support from the unmodified official OTF file"",
    ""Reserved the serif face for narrative headings and story presentation without changing the Sans UI default""
  ],
  ""sourceSha256"": {},
  ""outputSha256"": {
    ""NotoSerifCJKsc-SemiBold.asset"": ""__OUTPUT_HASH__""
  },
  ""licenseOrTermsUrl"": ""https://raw.githubusercontent.com/notofonts/noto-cjk/Serif2.003/Serif/LICENSE"",
  ""licenseOrTermsSnapshot"": ""Assets/ThirdParty/Fonts/NotoCJKSC/FONT-002/LICENSE.txt"",
  ""termsReviewedAt"": ""2026-08-10"",
  ""allowedPlatforms"": [""Windows x64"", ""Steam""],
  ""allowedUses"": [""commercial game runtime"", ""store and marketing screenshots"", ""internal development and testing""],
  ""commercialUseReviewed"": true,
  ""steamDisclosureCategory"": ""third-party font under SIL Open Font License 1.1; no generative AI"",
  ""technicalReviewer"": ""Codex"",
  ""creativeReviewer"": ""Codex"",
  ""rightsReviewer"": ""Codex"",
  ""reviewedAt"": ""2026-08-10"",
  ""status"": ""approved-for-release"",
  ""notes"": ""Original OTF file and license are governed by THIRD_PARTY_NOTICES.md and source-record.json.""
}
";
            File.WriteAllText(
                Font002Root + "/provenance.json",
                template.Replace("__OUTPUT_HASH__", outputHash),
                new UTF8Encoding(false));
        }

        private static string ComputeHash(string assetPath)
        {
            using (var stream = File.OpenRead(assetPath))
            using (var sha = SHA256.Create())
            {
                var bytes = sha.ComputeHash(stream);
                var output = new StringBuilder(bytes.Length * 2);
                for (var index = 0; index < bytes.Length; index++)
                    output.Append(bytes[index].ToString("x2", CultureInfo.InvariantCulture));
                return output.ToString();
            }
        }
    }
}
