using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using Game.Content.Runtime;
using UnityEditor;
using UnityEditor.AddressableAssets;
using UnityEngine;

namespace Game.Editor
{
    /// <summary>Exports the machine-readable compliance state of the formal Demo Release inputs.</summary>
    public static class QinglanG36ComplianceCommand
    {
        public static void Run()
        {
            var exitCode = 0;
            var result = new ComplianceResult
            {
                schemaVersion = 1,
                generatedAtUtc = DateTime.UtcNow.ToString("O", CultureInfo.InvariantCulture),
                status = "FAIL",
                issues = Array.Empty<string>()
            };
            try
            {
                var validation = ProjectGovernanceValidator.ValidateCurrentProject();
                var issues = new List<string>(validation.Issues.Count);
                for (var index = 0; index < validation.Issues.Count; index++)
                    issues.Add(validation.Issues[index].ToString());
                result.issueCount = issues.Count;
                result.issues = issues.ToArray();

                var catalog = QinglanG36ReleaseCatalog.ValidateOrThrow();
                result.packId = catalog.Manifest.PackId.Value;
                result.packVersion = catalog.Manifest.Version.ToString();
                result.packOfficial = catalog.Manifest.Official;
                result.contentHash = catalog.ContentHash;
                result.definitionCount = catalog.Definitions.Count;
                result.catalogSha256 = BuildManifestWriter.HashFile(
                    Path.GetFullPath(QinglanG36ReleaseCatalog.ReleaseCatalogPath));

                using (var scope = ReleaseAddressablesScope.IncludeOnlyFormalDemoGroups())
                {
                    result.includedAddressablesGroupCount = scope.IncludedGroupCount;
                    result.excludedAddressablesGroupCount = scope.ExcludedGroupCount;
                    result.placeholderCount = ReleaseBuildGateValidator.CountIncludedPlaceholderEntries();
                    result.releaseAddressableEntryCount = CountReleaseEntries();
                }

                result.thirdPartyFileCount = CountRuntimeFiles(Path.GetFullPath("Assets/ThirdParty"));
                result.formalAssetFileCount =
                    CountRuntimeFiles(Path.GetFullPath("Assets/GameAssets/AI")) +
                    CountRuntimeFiles(Path.GetFullPath("Assets/GameAssets/FirstParty"));
                result.thirdPartyNoticesSha256 = BuildManifestWriter.HashFile(
                    Path.GetFullPath("THIRD_PARTY_NOTICES.md"));
                result.assetProvenanceSha256 = BuildManifestWriter.HashFile(
                    Path.GetFullPath("ASSET_PROVENANCE.csv"));
                result.publicApiFreezeSha256 = BuildManifestWriter.HashFile(
                    Path.GetFullPath("Docs/PUBLIC_API_FREEZE.md"));
                result.status = validation.IsValid &&
                                result.placeholderCount == 0 &&
                                result.releaseAddressableEntryCount > 0 &&
                                result.packOfficial &&
                                string.Equals(result.packId, QinglanG36ReleaseCatalog.ExpectedPackId,
                                    StringComparison.Ordinal) &&
                                string.Equals(result.packVersion, QinglanG36ReleaseCatalog.ExpectedPackVersion,
                                    StringComparison.Ordinal) &&
                                result.definitionCount == QinglanG36ReleaseCatalog.ExpectedDefinitionCount
                    ? "PASS"
                    : "FAIL";
                if (!string.Equals(result.status, "PASS", StringComparison.Ordinal)) exitCode = 1;
            }
            catch (Exception exception)
            {
                Debug.LogException(exception);
                result.issues = new[] { exception.Message };
                result.issueCount = 1;
                exitCode = 1;
            }

            try
            {
                Write(result);
                if (exitCode == 0) Debug.Log("[Qinglan G3.6 Compliance] PASS");
                else Debug.LogError("[Qinglan G3.6 Compliance] FAIL");
            }
            catch (Exception exception)
            {
                Debug.LogException(exception);
                exitCode = 1;
            }
            EditorApplication.Exit(exitCode);
        }

        private static int CountReleaseEntries()
        {
            var settings = AddressableAssetSettingsDefaultObject.GetSettings(false);
            if (settings == null) return 0;
            var count = 0;
            for (var groupIndex = 0; groupIndex < settings.groups.Count; groupIndex++)
            {
                var group = settings.groups[groupIndex];
                if (group == null || !ReleaseAddressablesScope.IsFormalDemoGroup(group.Name)) continue;
                foreach (var entry in group.entries)
                    if (entry.labels.Contains(AssetProvenanceValidator.ReleaseLabel)) count++;
            }
            return count;
        }

        private static int CountRuntimeFiles(string root)
        {
            if (!Directory.Exists(root)) return 0;
            var files = Directory.GetFiles(root, "*", SearchOption.AllDirectories);
            var count = 0;
            for (var index = 0; index < files.Length; index++)
            {
                var name = Path.GetFileName(files[index]);
                if (name.EndsWith(".meta", StringComparison.OrdinalIgnoreCase) ||
                    string.Equals(name, "provenance.json", StringComparison.OrdinalIgnoreCase) ||
                    string.Equals(name, ".gitkeep", StringComparison.OrdinalIgnoreCase)) continue;
                count++;
            }
            return count;
        }

        private static void Write(ComplianceResult result)
        {
            var path = Environment.GetEnvironmentVariable("QINGLAN_G36_COMPLIANCE_RESULT");
            if (string.IsNullOrWhiteSpace(path))
                path = "TestResults/QinglanDemo/G3.6/compliance.json";
            path = Path.GetFullPath(path);
            var directory = Path.GetDirectoryName(path);
            if (string.IsNullOrEmpty(directory)) throw new InvalidOperationException("Invalid compliance path.");
            Directory.CreateDirectory(directory);
            File.WriteAllText(path, JsonUtility.ToJson(result, true) + "\n");
        }

        [Serializable]
        private sealed class ComplianceResult
        {
            public int schemaVersion;
            public string generatedAtUtc;
            public string status;
            public int issueCount;
            public string[] issues;
            public string packId;
            public string packVersion;
            public bool packOfficial;
            public string contentHash;
            public string catalogSha256;
            public int definitionCount;
            public int includedAddressablesGroupCount;
            public int excludedAddressablesGroupCount;
            public int releaseAddressableEntryCount;
            public int placeholderCount;
            public int thirdPartyFileCount;
            public int formalAssetFileCount;
            public string thirdPartyNoticesSha256;
            public string assetProvenanceSha256;
            public string publicApiFreezeSha256;
        }
    }
}
