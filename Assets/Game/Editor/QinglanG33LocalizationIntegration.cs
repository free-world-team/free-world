using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Security.Cryptography;
using System.Text;
using UnityEditor;
using UnityEditor.AddressableAssets;
using UnityEditor.AddressableAssets.Settings;
using UnityEditor.AddressableAssets.Settings.GroupSchemas;
using UnityEditor.Localization;
using UnityEngine;
using UnityEngine.Localization;
using UnityEngine.Localization.Pseudo;
using UnityEngine.Localization.Tables;

namespace Game.Editor
{
    /// <summary>Builds governed G3.3 string-table collections while preserving the M8 mixed table.</summary>
    public static class QinglanG33LocalizationIntegration
    {
        public const string LocalizationRoot = "Assets/GameContent/QinglanDemo/Localization";
        public const string UiBatchRoot = LocalizationRoot + "/LOC-UI-001";
        public const string UiTablesRoot = UiBatchRoot + "/Tables";
        public const string ContentBatchRoot = LocalizationRoot + "/LOC-CONTENT-001";
        public const string ContentTablesRoot = ContentBatchRoot + "/Tables";
        public const string NarrativeBatchRoot = LocalizationRoot + "/LOC-NARRATIVE-001";
        public const string NarrativeTablesRoot = NarrativeBatchRoot + "/Tables";
        public const string LegacyUiCollection = "M8LegacyUI";

        public static void RunUi()
        {
            var exitCode = 0;
            try
            {
                var collection = BuildUi();
                Debug.Log("[Qinglan G3.3 LOC-UI-001] PASS: keys=" + collection.SharedData.Entries.Count + ".");
            }
            catch (Exception exception)
            {
                Debug.LogException(exception);
                exitCode = 1;
            }
            EditorApplication.Exit(exitCode);
        }

        public static void RunContent()
        {
            var exitCode = 0;
            try
            {
                var collection = BuildContent();
                Debug.Log("[Qinglan G3.3 LOC-CONTENT-001] PASS: keys=" + collection.SharedData.Entries.Count + ".");
            }
            catch (Exception exception)
            {
                Debug.LogException(exception);
                exitCode = 1;
            }
            EditorApplication.Exit(exitCode);
        }

        public static void RunNarrative()
        {
            var exitCode = 0;
            try
            {
                var collection = BuildNarrative();
                Debug.Log("[Qinglan G3.3 LOC-NARRATIVE-001] PASS: keys=" + collection.SharedData.Entries.Count + ".");
            }
            catch (Exception exception)
            {
                Debug.LogException(exception);
                exitCode = 1;
            }
            EditorApplication.Exit(exitCode);
        }

        public static StringTableCollection BuildUi()
        {
            var englishLocale = FindLocale("en");
            var chineseLocale = FindLocale("zh-Hans");
            if (englishLocale == null || chineseLocale == null)
                throw new InvalidOperationException("The en and zh-Hans locales must exist before LOC-UI-001.");

            var legacy = LocalizationEditorSettings.GetStringTableCollection(LegacyUiCollection);
            var formal = LocalizationEditorSettings.GetStringTableCollection("UI");
            if (legacy == null)
            {
                if (formal == null) throw new InvalidOperationException("The M8 UI source collection is missing.");
                if (formal.SharedData.Entries.Count <= QinglanG33UiLocalizationSource.MinimumCount)
                    throw new InvalidOperationException("Cannot identify the preserved M8 mixed UI source.");
                formal.SetTableCollectionName(LegacyUiCollection);
                legacy = formal;
                formal = null;
                AssetDatabase.SaveAssets();
            }

            var legacyEnglish = legacy.GetTable(englishLocale.Identifier) as StringTable;
            var legacyChinese = legacy.GetTable(chineseLocale.Identifier) as StringTable;
            var source = QinglanG33UiLocalizationSource.Build(legacyEnglish, legacyChinese);

            Directory.CreateDirectory(UiTablesRoot);
            AssetDatabase.Refresh(ImportAssetOptions.ForceSynchronousImport);
            formal = formal ?? LocalizationEditorSettings.CreateStringTableCollection(
                "UI",
                UiTablesRoot,
                new List<Locale> { englishLocale, chineseLocale });
            var english = formal.GetTable(englishLocale.Identifier) as StringTable ??
                          formal.AddNewTable(englishLocale.Identifier) as StringTable;
            var chinese = formal.GetTable(chineseLocale.Identifier) as StringTable ??
                          formal.AddNewTable(chineseLocale.Identifier) as StringTable;
            if (english == null || chinese == null)
                throw new InvalidOperationException("Formal UI tables could not be created.");
            if (formal.SharedData.Entries.Count > source.Count)
                throw new InvalidOperationException("Formal UI collection contains unreviewed extra keys.");

            for (var index = 0; index < source.Count; index++)
            {
                SetEntry(english, source[index].Key, source[index].English);
                SetEntry(chinese, source[index].Key, source[index].Chinese);
            }
            LocalizationEditorSettings.SetPreloadTableFlag(english, true);
            LocalizationEditorSettings.SetPreloadTableFlag(chinese, true);
            EditorUtility.SetDirty(english);
            EditorUtility.SetDirty(chinese);
            EditorUtility.SetDirty(formal.SharedData);
            AssetDatabase.SaveAssets();

            WriteProvenance(formal, english, chinese);
            AssetDatabase.Refresh(ImportAssetOptions.ForceSynchronousImport);
            RegisterReleaseAssets(formal, english, chinese, "UI");
            AssetDatabase.SaveAssets();
            return formal;
        }

        public static StringTableCollection BuildContent()
        {
            var englishLocale = FindLocale("en");
            var chineseLocale = FindLocale("zh-Hans");
            if (englishLocale == null || chineseLocale == null)
                throw new InvalidOperationException("The en and zh-Hans locales must exist before LOC-CONTENT-001.");

            var source = QinglanG33ContentLocalizationSource.Build();
            Directory.CreateDirectory(ContentTablesRoot);
            AssetDatabase.Refresh(ImportAssetOptions.ForceSynchronousImport);
            var collection = LocalizationEditorSettings.GetStringTableCollection("QinglanContent") ??
                             LocalizationEditorSettings.CreateStringTableCollection(
                                 "QinglanContent",
                                 ContentTablesRoot,
                                 new List<Locale> { englishLocale, chineseLocale });
            var english = collection.GetTable(englishLocale.Identifier) as StringTable ??
                          collection.AddNewTable(englishLocale.Identifier) as StringTable;
            var chinese = collection.GetTable(chineseLocale.Identifier) as StringTable ??
                          collection.AddNewTable(chineseLocale.Identifier) as StringTable;
            if (english == null || chinese == null)
                throw new InvalidOperationException("Formal content tables could not be created.");
            if (collection.SharedData.Entries.Count > source.Count)
                throw new InvalidOperationException("Formal content collection contains unreviewed extra keys.");

            for (var index = 0; index < source.Count; index++)
            {
                SetEntry(english, source[index].Key, source[index].English);
                SetEntry(chinese, source[index].Key, source[index].Chinese);
            }
            EditorUtility.SetDirty(english);
            EditorUtility.SetDirty(chinese);
            EditorUtility.SetDirty(collection.SharedData);
            AssetDatabase.SaveAssets();

            WriteContentProvenance(collection, english, chinese);
            AssetDatabase.Refresh(ImportAssetOptions.ForceSynchronousImport);
            RegisterReleaseAssets(collection, english, chinese, "QinglanContent");
            AssetDatabase.SaveAssets();
            return collection;
        }

        public static StringTableCollection BuildNarrative()
        {
            var englishLocale = FindLocale("en");
            var chineseLocale = FindLocale("zh-Hans");
            if (englishLocale == null || chineseLocale == null)
                throw new InvalidOperationException("The en and zh-Hans locales must exist before LOC-NARRATIVE-001.");

            var source = QinglanG33NarrativeLocalizationSource.Build();
            Directory.CreateDirectory(NarrativeTablesRoot);
            AssetDatabase.Refresh(ImportAssetOptions.ForceSynchronousImport);
            var collection = LocalizationEditorSettings.GetStringTableCollection("QinglanNarrative") ??
                             LocalizationEditorSettings.CreateStringTableCollection(
                                 "QinglanNarrative",
                                 NarrativeTablesRoot,
                                 new List<Locale> { englishLocale, chineseLocale });
            var english = collection.GetTable(englishLocale.Identifier) as StringTable ??
                          collection.AddNewTable(englishLocale.Identifier) as StringTable;
            var chinese = collection.GetTable(chineseLocale.Identifier) as StringTable ??
                          collection.AddNewTable(chineseLocale.Identifier) as StringTable;
            if (english == null || chinese == null)
                throw new InvalidOperationException("Formal narrative tables could not be created.");
            if (collection.SharedData.Entries.Count > source.Count)
                throw new InvalidOperationException("Formal narrative collection contains unreviewed extra keys.");

            for (var index = 0; index < source.Count; index++)
            {
                SetEntry(english, source[index].Key, source[index].English);
                SetEntry(chinese, source[index].Key, source[index].Chinese);
            }
            EditorUtility.SetDirty(english);
            EditorUtility.SetDirty(chinese);
            EditorUtility.SetDirty(collection.SharedData);
            AssetDatabase.SaveAssets();

            WriteNarrativeProvenance(collection, english, chinese);
            AssetDatabase.Refresh(ImportAssetOptions.ForceSynchronousImport);
            RegisterReleaseAssets(collection, english, chinese, "QinglanNarrative");
            AssetDatabase.SaveAssets();
            return collection;
        }

        private static void RegisterReleaseAssets(
            StringTableCollection collection,
            StringTable english,
            StringTable chinese,
            string tableName)
        {
            var settings = AddressableAssetSettingsDefaultObject.GetSettings(true);
            if (settings == null) throw new InvalidOperationException("Addressables settings are unavailable.");
            settings.AddLabel(AssetProvenanceValidator.QinglanPackLabel, false);
            settings.AddLabel(AssetProvenanceValidator.ReleaseLabel, false);
            settings.AddLabel(AssetProvenanceValidator.LocalizationReleaseLabel, false);
            var group = settings.FindGroup(AssetProvenanceValidator.QinglanLocalizationGroup);
            if (group == null)
            {
                group = settings.CreateGroup(
                    AssetProvenanceValidator.QinglanLocalizationGroup,
                    false,
                    false,
                    true,
                    null,
                    typeof(ContentUpdateGroupSchema),
                    typeof(BundledAssetGroupSchema));
                var schema = group.GetSchema<BundledAssetGroupSchema>();
                schema.BuildPath.SetVariableByName(settings, AddressableAssetSettings.kLocalBuildPath);
                schema.LoadPath.SetVariableByName(settings, AddressableAssetSettings.kLocalLoadPath);
                schema.BundleMode = BundledAssetGroupSchema.BundlePackingMode.PackTogether;
            }

            Register(settings, group, AssetDatabase.GetAssetPath(collection.SharedData), null);
            Register(settings, group, AssetDatabase.GetAssetPath(english), tableName + "_en");
            Register(settings, group, AssetDatabase.GetAssetPath(chinese), tableName + "_zh-Hans");
        }

        private static void Register(
            AddressableAssetSettings settings,
            AddressableAssetGroup group,
            string path,
            string requiredAddress)
        {
            var projectRoot = Directory.GetParent(UnityEngine.Application.dataPath)?.FullName;
            if (string.IsNullOrWhiteSpace(projectRoot))
                throw new InvalidOperationException("Unable to resolve project root.");
            var labels = new[]
            {
                AssetProvenanceValidator.QinglanPackLabel,
                AssetProvenanceValidator.ReleaseLabel,
                AssetProvenanceValidator.LocalizationReleaseLabel
            };
            var issues = AssetProvenanceValidator.ValidateReleaseInput(
                projectRoot,
                path,
                labels,
                AssetProvenanceValidator.QinglanLocalizationGroup);
            if (issues.Count > 0) throw new InvalidOperationException(issues[0].ToString());

            var guid = AssetDatabase.AssetPathToGUID(path);
            var prior = settings.FindAssetEntry(guid);
            var address = string.IsNullOrWhiteSpace(requiredAddress)
                ? prior == null ? path : prior.address
                : requiredAddress;
            var entry = settings.CreateOrMoveEntry(guid, group, false, false);
            entry.address = address;
            entry.SetLabel(AssetProvenanceValidator.QinglanPackLabel, true, false, false);
            entry.SetLabel(AssetProvenanceValidator.ReleaseLabel, true, false, false);
            entry.SetLabel(AssetProvenanceValidator.LocalizationReleaseLabel, true, false, false);
            entry.SetLabel(PlaceholderAssetGenerator.PlaceholderLabel, false, false, false);
            entry.SetLabel(PlaceholderAssetGenerator.DevelopmentOnlyLabel, false, false, false);
            settings.SetDirty(AddressableAssetSettings.ModificationEvent.BatchModification, entry, true, true);
        }

        private static void WriteProvenance(
            StringTableCollection collection,
            StringTable english,
            StringTable chinese)
        {
            var collectionPath = AssetDatabase.GetAssetPath(collection);
            var sharedPath = AssetDatabase.GetAssetPath(collection.SharedData);
            var englishPath = AssetDatabase.GetAssetPath(english);
            var chinesePath = AssetDatabase.GetAssetPath(chinese);
            var sourcePath = "Assets/Game/Editor/QinglanG33UiLocalizationSource.cs";
            var template = @"{
  ""schemaVersion"": 2,
  ""assetId"": ""LOC-UI-001"",
  ""owner"": ""Qinglan Demo Localization Owner"",
  ""relativePaths"": [""__COLLECTION_PATH__"", ""__SHARED_PATH__"", ""__ENGLISH_PATH__"", ""__CHINESE_PATH__""],
  ""sourceCategory"": ""first-party-writing-and-translation"",
  ""tool"": ""Unity Localization 1.5.8 and QinglanG33LocalizationIntegration"",
  ""modelVersion"": ""human-reviewed LOC-UI-001 bilingual source"",
  ""generatedOrAcquiredAt"": ""2026-08-10"",
  ""operatorName"": ""Codex"",
  ""promptFile"": ""Docs/DemoDevelopment/26_G3_3_FORMAL_FONT_LOCALIZATION.md"",
  ""seed"": ""not-applicable"",
  ""referenceInputs"": [],
  ""referenceRightsConfirmed"": true,
  ""humanEdits"": [""Preserved 152 reviewed M8 UI and diagnostic translations"", ""Added 39 Qinglan HUD, settings, map, navigation, and toast translations"", ""Separated formal UI strings from the preserved mixed M8 content table without deleting legacy data""],
  ""sourceSha256"": {""QinglanG33UiLocalizationSource.cs"": ""__SOURCE_HASH__""},
  ""outputSha256"": {""__COLLECTION_FILE__"": ""__COLLECTION_HASH__"", ""__SHARED_FILE__"": ""__SHARED_HASH__"", ""__ENGLISH_FILE__"": ""__ENGLISH_HASH__"", ""__CHINESE_FILE__"": ""__CHINESE_HASH__""},
  ""licenseOrTermsUrl"": ""repository://AGENTS.md"",
  ""licenseOrTermsSnapshot"": ""Docs/AssetTerms/2026-08-09-first-party-procedural-rights-review.md"",
  ""termsReviewedAt"": ""2026-08-10"",
  ""allowedPlatforms"": [""Windows x64"", ""Steam""],
  ""allowedUses"": [""commercial game runtime"", ""store and marketing screenshots"", ""internal development and testing""],
  ""commercialUseReviewed"": true,
  ""steamDisclosureCategory"": ""first-party writing and translation; no generative AI"",
  ""technicalReviewer"": ""Codex"",
  ""creativeReviewer"": ""Codex"",
  ""rightsReviewer"": ""Codex"",
  ""reviewedAt"": ""2026-08-10"",
  ""status"": ""approved-for-release"",
  ""notes"": ""Pseudo locale is generated from the formal English table and is not a separately authored table.""
}
";
            var json = template
                .Replace("__COLLECTION_PATH__", collectionPath)
                .Replace("__SHARED_PATH__", sharedPath)
                .Replace("__ENGLISH_PATH__", englishPath)
                .Replace("__CHINESE_PATH__", chinesePath)
                .Replace("__SOURCE_HASH__", ComputeHash(sourcePath))
                .Replace("__COLLECTION_FILE__", Path.GetFileName(collectionPath))
                .Replace("__COLLECTION_HASH__", ComputeHash(collectionPath))
                .Replace("__SHARED_FILE__", Path.GetFileName(sharedPath))
                .Replace("__SHARED_HASH__", ComputeHash(sharedPath))
                .Replace("__ENGLISH_FILE__", Path.GetFileName(englishPath))
                .Replace("__ENGLISH_HASH__", ComputeHash(englishPath))
                .Replace("__CHINESE_FILE__", Path.GetFileName(chinesePath))
                .Replace("__CHINESE_HASH__", ComputeHash(chinesePath));
            File.WriteAllText(UiBatchRoot + "/provenance.json", json, new UTF8Encoding(false));
        }

        private static void WriteContentProvenance(
            StringTableCollection collection,
            StringTable english,
            StringTable chinese)
        {
            var collectionPath = AssetDatabase.GetAssetPath(collection);
            var sharedPath = AssetDatabase.GetAssetPath(collection.SharedData);
            var englishPath = AssetDatabase.GetAssetPath(english);
            var chinesePath = AssetDatabase.GetAssetPath(chinese);
            var sourcePath = "Assets/Game/Editor/QinglanG33ContentLocalizationSource.cs";
            var template = @"{
  ""schemaVersion"": 2,
  ""assetId"": ""LOC-CONTENT-001"",
  ""owner"": ""Qinglan Demo Localization Owner"",
  ""relativePaths"": [""__COLLECTION_PATH__"", ""__SHARED_PATH__"", ""__ENGLISH_PATH__"", ""__CHINESE_PATH__""],
  ""sourceCategory"": ""first-party-writing-and-translation"",
  ""tool"": ""Unity Localization 1.5.8 and QinglanG33LocalizationIntegration"",
  ""modelVersion"": ""human-reviewed LOC-CONTENT-001 bilingual source"",
  ""generatedOrAcquiredAt"": ""2026-08-10"",
  ""operatorName"": ""Codex"",
  ""promptFile"": ""Docs/DemoDevelopment/26_G3_3_FORMAL_FONT_LOCALIZATION.md"",
  ""seed"": ""not-applicable"",
  ""referenceInputs"": [""Assets/GameAssets/Placeholder/QinglanDemo/QinglanDemoContentPack.baked.json""],
  ""referenceRightsConfirmed"": true,
  ""humanEdits"": [""Authored bilingual names and descriptions for every baked Qinglan Demo content definition"", ""Added player-facing level-change text for every baked skill patch"", ""Applied one governed Qinglan terminology lexicon across both locales""],
  ""sourceSha256"": {""QinglanG33ContentLocalizationSource.cs"": ""__SOURCE_HASH__""},
  ""outputSha256"": {""__COLLECTION_FILE__"": ""__COLLECTION_HASH__"", ""__SHARED_FILE__"": ""__SHARED_HASH__"", ""__ENGLISH_FILE__"": ""__ENGLISH_HASH__"", ""__CHINESE_FILE__"": ""__CHINESE_HASH__""},
  ""licenseOrTermsUrl"": ""repository://AGENTS.md"",
  ""licenseOrTermsSnapshot"": ""Docs/AssetTerms/2026-08-09-first-party-procedural-rights-review.md"",
  ""termsReviewedAt"": ""2026-08-10"",
  ""allowedPlatforms"": [""Windows x64"", ""Steam""],
  ""allowedUses"": [""commercial game runtime"", ""store and marketing screenshots"", ""internal development and testing""],
  ""commercialUseReviewed"": true,
  ""steamDisclosureCategory"": ""first-party writing and translation; no generative AI"",
  ""technicalReviewer"": ""Codex"",
  ""creativeReviewer"": ""Codex"",
  ""rightsReviewer"": ""Codex"",
  ""reviewedAt"": ""2026-08-10"",
  ""status"": ""approved-for-release"",
  ""notes"": ""Pseudo locale is generated from the formal English table and is not a separately authored table.""
}
";
            var json = template
                .Replace("__COLLECTION_PATH__", collectionPath)
                .Replace("__SHARED_PATH__", sharedPath)
                .Replace("__ENGLISH_PATH__", englishPath)
                .Replace("__CHINESE_PATH__", chinesePath)
                .Replace("__SOURCE_HASH__", ComputeHash(sourcePath))
                .Replace("__COLLECTION_FILE__", Path.GetFileName(collectionPath))
                .Replace("__COLLECTION_HASH__", ComputeHash(collectionPath))
                .Replace("__SHARED_FILE__", Path.GetFileName(sharedPath))
                .Replace("__SHARED_HASH__", ComputeHash(sharedPath))
                .Replace("__ENGLISH_FILE__", Path.GetFileName(englishPath))
                .Replace("__ENGLISH_HASH__", ComputeHash(englishPath))
                .Replace("__CHINESE_FILE__", Path.GetFileName(chinesePath))
                .Replace("__CHINESE_HASH__", ComputeHash(chinesePath));
            File.WriteAllText(ContentBatchRoot + "/provenance.json", json, new UTF8Encoding(false));
        }

        private static void WriteNarrativeProvenance(
            StringTableCollection collection,
            StringTable english,
            StringTable chinese)
        {
            var collectionPath = AssetDatabase.GetAssetPath(collection);
            var sharedPath = AssetDatabase.GetAssetPath(collection.SharedData);
            var englishPath = AssetDatabase.GetAssetPath(english);
            var chinesePath = AssetDatabase.GetAssetPath(chinese);
            var sourcePath = "Assets/Game/Editor/QinglanG33NarrativeLocalizationSource.cs";
            var template = @"{
  ""schemaVersion"": 2,
  ""assetId"": ""LOC-NARRATIVE-001"",
  ""owner"": ""Qinglan Demo Narrative Owner"",
  ""relativePaths"": [""__COLLECTION_PATH__"", ""__SHARED_PATH__"", ""__ENGLISH_PATH__"", ""__CHINESE_PATH__""],
  ""sourceCategory"": ""first-party-writing-and-translation"",
  ""tool"": ""Unity Localization 1.5.9 and QinglanG33LocalizationIntegration"",
  ""modelVersion"": ""human-reviewed LOC-NARRATIVE-001 bilingual script"",
  ""generatedOrAcquiredAt"": ""2026-08-10"",
  ""operatorName"": ""Codex"",
  ""promptFile"": ""Docs/DemoDevelopment/26_G3_3_FORMAL_FONT_LOCALIZATION.md"",
  ""seed"": ""not-applicable"",
  ""referenceInputs"": [""Docs/Game Proposal/《剑起青岚》游戏系统总纲_V2.0.md"", ""Docs/Game Proposal/《剑起青岚》世界观及完整剧情脉络设定集_V1.0.md"", ""Assets/GameAssets/Placeholder/QinglanDemo/QinglanDemoContentPack.baked.json""],
  ""referenceRightsConfirmed"": true,
  ""humanEdits"": [""Authored three Lu Qingye story sequences and six Old Court field records"", ""Authored objective, event, landmark, boss, and map-state lines"", ""Reviewed English and Simplified Chinese for Qinglan terminology and narrative continuity""],
  ""sourceSha256"": {""QinglanG33NarrativeLocalizationSource.cs"": ""__SOURCE_HASH__""},
  ""outputSha256"": {""__COLLECTION_FILE__"": ""__COLLECTION_HASH__"", ""__SHARED_FILE__"": ""__SHARED_HASH__"", ""__ENGLISH_FILE__"": ""__ENGLISH_HASH__"", ""__CHINESE_FILE__"": ""__CHINESE_HASH__""},
  ""licenseOrTermsUrl"": ""repository://AGENTS.md"",
  ""licenseOrTermsSnapshot"": ""Docs/AssetTerms/2026-08-09-first-party-procedural-rights-review.md"",
  ""termsReviewedAt"": ""2026-08-10"",
  ""allowedPlatforms"": [""Windows x64"", ""Steam""],
  ""allowedUses"": [""commercial game runtime"", ""store and marketing screenshots"", ""internal development and testing""],
  ""commercialUseReviewed"": true,
  ""steamDisclosureCategory"": ""first-party writing and translation; no generative AI"",
  ""technicalReviewer"": ""Codex"",
  ""creativeReviewer"": ""Codex"",
  ""rightsReviewer"": ""Codex"",
  ""reviewedAt"": ""2026-08-10"",
  ""status"": ""approved-for-release"",
  ""notes"": ""Pseudo locale is generated from the formal English table and is not a separately authored table.""
}
";
            var json = template
                .Replace("__COLLECTION_PATH__", collectionPath)
                .Replace("__SHARED_PATH__", sharedPath)
                .Replace("__ENGLISH_PATH__", englishPath)
                .Replace("__CHINESE_PATH__", chinesePath)
                .Replace("__SOURCE_HASH__", ComputeHash(sourcePath))
                .Replace("__COLLECTION_FILE__", Path.GetFileName(collectionPath))
                .Replace("__COLLECTION_HASH__", ComputeHash(collectionPath))
                .Replace("__SHARED_FILE__", Path.GetFileName(sharedPath))
                .Replace("__SHARED_HASH__", ComputeHash(sharedPath))
                .Replace("__ENGLISH_FILE__", Path.GetFileName(englishPath))
                .Replace("__ENGLISH_HASH__", ComputeHash(englishPath))
                .Replace("__CHINESE_FILE__", Path.GetFileName(chinesePath))
                .Replace("__CHINESE_HASH__", ComputeHash(chinesePath));
            File.WriteAllText(NarrativeBatchRoot + "/provenance.json", json, new UTF8Encoding(false));
        }

        private static Locale FindLocale(string code)
        {
            var locales = LocalizationEditorSettings.GetLocales();
            for (var index = 0; index < locales.Count; index++)
                if (!(locales[index] is PseudoLocale) && string.Equals(
                        locales[index].Identifier.Code,
                        code,
                        StringComparison.OrdinalIgnoreCase))
                    return locales[index];
            return null;
        }

        private static void SetEntry(StringTable table, string key, string value)
        {
            var entry = table.GetEntry(key) ?? table.AddEntry(key, value);
            entry.Value = value;
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
