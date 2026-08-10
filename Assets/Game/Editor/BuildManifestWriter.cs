using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Security.Cryptography;
using System.Text;
using System.Xml;
using Game.Application;
using Game.Content.Authoring;
using Game.Content.Runtime;
using UnityEditor;
using UnityEditor.Build.Reporting;
using UnityEngine;

namespace Game.Editor
{
    [Serializable]
    internal sealed class BuildSourceState
    {
        public string commit;
        public string branch;
        public string tag;
        public bool workingTreeClean;
    }

    /// <summary>Writes the auditable manifest shared by Development and formal Release builds.</summary>
    internal static class BuildManifestWriter
    {
        public static BuildSourceState CaptureSourceState()
        {
            return new BuildSourceState
            {
                commit = RunGit("rev-parse", "HEAD"),
                branch = RunGit("rev-parse", "--abbrev-ref", "HEAD"),
                tag = FirstLine(RunGit("tag", "--points-at", "HEAD")),
                workingTreeClean = GitSucceeds("diff", "--quiet") &&
                                   GitSucceeds("diff", "--cached", "--quiet") &&
                                   string.IsNullOrWhiteSpace(
                                       RunGit("ls-files", "--others", "--exclude-standard"))
            };
        }

        public static string Write(
            string outputDirectory,
            string outputPath,
            BuildReport report,
            string configuration,
            bool development,
            BuildSourceState sourceState)
        {
            if (sourceState == null) throw new ArgumentNullException(nameof(sourceState));
            var evidenceRoot = Environment.GetEnvironmentVariable("M10_EVIDENCE_ROOT");
            if (string.IsNullOrWhiteSpace(evidenceRoot)) evidenceRoot = "TestResults/M10Final";
            evidenceRoot = Path.GetFullPath(evidenceRoot);
            var contentPacks = CollectContentPacks(development);
            var manifest = new M10BuildManifest
            {
                schemaVersion = 2,
                productName = PlayerSettings.productName,
                gameVersion = PlayerSettings.bundleVersion,
                buildNumber = ParseBuildNumber(),
                buildConfiguration = configuration,
                targetPlatform = "Windows x64",
                unityVersion = UnityEngine.Application.unityVersion,
                buildTarget = BuildTarget.StandaloneWindows64.ToString(),
                development = development,
                executable = outputPath.Replace('\\', '/'),
                result = report.summary.result.ToString(),
                git = sourceState,
                builtAtUtc = DateTime.UtcNow.ToString("O", CultureInfo.InvariantCulture),
                generatedAtUtc = DateTime.UtcNow.ToString("O", CultureInfo.InvariantCulture),
                contentSchemaVersion = ContentPackTopology.LatestSupportedSchemaVersion,
                saveSchemaVersion = SaveSchema.ProfileCurrentVersion,
                settingsSaveSchemaVersion = SaveSchema.SettingsCurrentVersion,
                profileSaveSchemaVersion = SaveSchema.ProfileCurrentVersion,
                runRecoverySaveSchemaVersion = SaveSchema.RunRecoveryCurrentVersion,
                contentPacks = contentPacks,
                packagesLockSha256 = HashFile(Path.GetFullPath("Packages/packages-lock.json")),
                packagesManifestSha256 = HashFile(Path.GetFullPath("Packages/manifest.json")),
                projectVersionSha256 = HashFile(Path.GetFullPath("ProjectSettings/ProjectVersion.txt")),
                addressablesSettingsSha256 = HashFile(Path.GetFullPath(
                    "Assets/AddressableAssetsData/AddressableAssetSettings.asset")),
                publicApiFreezeSha256 = HashFile(Path.GetFullPath("Docs/PUBLIC_API_FREEZE.md")),
                thirdPartyNoticesSha256 = HashFile(Path.GetFullPath("THIRD_PARTY_NOTICES.md")),
                assetProvenanceSha256 = HashFile(Path.GetFullPath("ASSET_PROVENANCE.csv")),
                addressablesBuildHash = HashAddressablesOutput(outputDirectory),
                placeholderCount = ReleaseBuildGateValidator.CountIncludedPlaceholderEntries(),
                unapprovedAssetCount = 0,
                formalContentPackCount = CountIncludedPacks(contentPacks),
                releaseValidator = development ? "NOT_RUN" : "PASS",
                platformBackend = "NullPlatformFacade",
                offlineRequired = true,
                tests = ReadEvidence(evidenceRoot),
                artifacts = new[]
                {
                    new BuildArtifactDto
                    {
                        path = outputPath.Replace('\\', '/'),
                        sha256 = HashFile(outputPath)
                    }
                }
            };
            var manifestPath = Path.Combine(outputDirectory, "BuildManifest.json");
            File.WriteAllText(manifestPath, JsonUtility.ToJson(manifest, true) + "\n");
            return manifestPath;
        }

        private static ContentPackManifestDto[] CollectContentPacks(bool development)
        {
            if (!development)
            {
                var catalog = QinglanG36ReleaseCatalog.ValidateOrThrow();
                return new[]
                {
                    new ContentPackManifestDto
                    {
                        packId = catalog.Manifest.PackId.Value,
                        version = catalog.Manifest.Version.ToString(),
                        contentHash = catalog.ContentHash,
                        catalogHash = HashText(JsonUtility.ToJson(catalog.ToDto(), false)),
                        catalogSha256 = HashFile(Path.GetFullPath(
                            QinglanG36ReleaseCatalog.ReleaseCatalogPath)),
                        includedInPlayer = true,
                        placeholder = false,
                        official = catalog.Manifest.Official,
                        sourceAssetPath = catalog.Manifest.SourceAssetPath
                    }
                };
            }

            var guids = AssetDatabase.FindAssets("t:ContentPackAuthoring");
            var paths = new string[guids.Length];
            for (var index = 0; index < guids.Length; index++)
                paths[index] = AssetDatabase.GUIDToAssetPath(guids[index]);
            Array.Sort(paths, StringComparer.Ordinal);
            var output = new List<ContentPackManifestDto>(paths.Length);
            for (var index = 0; index < paths.Length; index++)
            {
                var pack = AssetDatabase.LoadAssetAtPath<ContentPackAuthoring>(paths[index]);
                if (pack == null) continue;
                var baked = ContentBakeUtility.Bake(pack);
                if (!baked.IsSuccess)
                    throw new InvalidOperationException("Content pack audit failed: " + baked.Error);
                var catalogJson = JsonUtility.ToJson(baked.Value.ToDto(), false);
                var placeholder = paths[index].StartsWith(
                    PlaceholderAssetGenerator.OutputFolder + "/",
                    StringComparison.OrdinalIgnoreCase);
                output.Add(new ContentPackManifestDto
                {
                    packId = baked.Value.Manifest.PackId.Value,
                    version = baked.Value.Manifest.Version.ToString(),
                    contentHash = baked.Value.ContentHash,
                    catalogHash = HashText(catalogJson),
                    catalogSha256 = string.Empty,
                    includedInPlayer = development || !placeholder,
                    placeholder = placeholder,
                    official = baked.Value.Manifest.Official,
                    sourceAssetPath = baked.Value.Manifest.SourceAssetPath
                });
            }

            return output.ToArray();
        }

        private static BuildEvidenceDto ReadEvidence(string root)
        {
            var editMode = Path.Combine(root, "editmode.xml");
            var playMode = Path.Combine(root, "playmode.xml");
            var validation = Path.Combine(root, "validation.log");
            var performance = FindPerformanceEvidence(root);
            return new BuildEvidenceDto
            {
                editMode = ReadTestXml(editMode),
                playMode = ReadTestXml(playMode),
                contentValidation = Contains(validation, "[Project Validation] PASS") ? "PASS" : "NOT_RUN",
                soak = ReadPerformanceStatus(performance),
                editModeSha256 = HashFileIfPresent(editMode),
                playModeSha256 = HashFileIfPresent(playMode),
                contentValidationSha256 = HashFileIfPresent(validation),
                performanceSha256 = HashFileIfPresent(performance)
            };
        }

        private static string ReadTestXml(string path)
        {
            if (!File.Exists(path)) return "NOT_RUN";
            try
            {
                var document = new XmlDocument();
                document.Load(path);
                var root = document.DocumentElement;
                return root != null &&
                       string.Equals(root.GetAttribute("result"), "Passed", StringComparison.Ordinal) &&
                       string.Equals(root.GetAttribute("failed"), "0", StringComparison.Ordinal)
                    ? "PASS"
                    : "FAIL";
            }
            catch
            {
                return "FAIL";
            }
        }

        private static string ReadPerformanceStatus(string path)
        {
            if (string.IsNullOrWhiteSpace(path) || !File.Exists(path)) return "NOT_RUN";
            try
            {
                var value = JsonUtility.FromJson<PerformanceStatusDto>(File.ReadAllText(path));
                return string.Equals(value?.status, "PASS", StringComparison.Ordinal)
                    ? "PASS"
                    : "FAIL";
            }
            catch
            {
                return "FAIL";
            }
        }

        private static string FindPerformanceEvidence(string root)
        {
            var candidates = new[]
            {
                "target-player.json",
                "gpu-target.json",
                "performance.json",
                "cpu-target.json"
            };
            for (var index = 0; index < candidates.Length; index++)
            {
                var path = Path.Combine(root, candidates[index]);
                if (File.Exists(path)) return path;
            }
            return string.Empty;
        }

        private static string HashFileIfPresent(string path) =>
            !string.IsNullOrWhiteSpace(path) && File.Exists(path) ? HashFile(path) : string.Empty;

        private static int CountIncludedPacks(ContentPackManifestDto[] packs)
        {
            var count = 0;
            for (var index = 0; index < packs.Length; index++)
                if (packs[index].includedInPlayer) count++;
            return count;
        }

        private static bool Contains(string path, string marker) =>
            File.Exists(path) && File.ReadAllText(path).Contains(marker);

        private static int ParseBuildNumber()
        {
            var value = Environment.GetEnvironmentVariable("BUILD_NUMBER");
            return int.TryParse(value, NumberStyles.Integer, CultureInfo.InvariantCulture, out var parsed)
                ? parsed
                : 1;
        }

        private static string HashAddressablesOutput(string outputDirectory)
        {
            var directories = Directory.Exists(outputDirectory)
                ? Directory.GetDirectories(outputDirectory, "aa", SearchOption.AllDirectories)
                : Array.Empty<string>();
            Array.Sort(directories, StringComparer.OrdinalIgnoreCase);
            return directories.Length == 0 ? HashText(string.Empty) : HashDirectory(directories[0]);
        }

        private static string HashDirectory(string root)
        {
            var files = Directory.GetFiles(root, "*", SearchOption.AllDirectories);
            Array.Sort(files, StringComparer.OrdinalIgnoreCase);
            var builder = new StringBuilder(files.Length * 128);
            for (var index = 0; index < files.Length; index++)
            {
                builder.Append(files[index].Substring(root.Length).Replace('\\', '/'));
                builder.Append(':');
                builder.Append(HashFile(files[index]));
                builder.Append('\n');
            }

            return HashText(builder.ToString());
        }

        internal static string HashFile(string path)
        {
            using (var stream = File.OpenRead(ForFileSystemAccess(path)))
            using (var sha = SHA256.Create())
            {
                return ToHex(sha.ComputeHash(stream));
            }
        }

        private static string ForFileSystemAccess(string path)
        {
            var fullPath = Path.GetFullPath(path);
            if (Path.DirectorySeparatorChar != '\\' ||
                fullPath.StartsWith("\\\\?\\", StringComparison.Ordinal))
                return fullPath;
            return fullPath.StartsWith("\\\\", StringComparison.Ordinal)
                ? "\\\\?\\UNC\\" + fullPath.Substring(2)
                : "\\\\?\\" + fullPath;
        }

        private static string HashText(string text)
        {
            using (var sha = SHA256.Create())
            {
                return ToHex(sha.ComputeHash(Encoding.UTF8.GetBytes(text ?? string.Empty)));
            }
        }

        private static string ToHex(byte[] value)
        {
            var builder = new StringBuilder(value.Length * 2);
            for (var index = 0; index < value.Length; index++)
                builder.Append(value[index].ToString("x2", CultureInfo.InvariantCulture));
            return builder.ToString();
        }

        private static string RunGit(params string[] arguments)
        {
            var startInfo = CreateGitStartInfo(arguments);
            using (var process = Process.Start(startInfo))
            {
                if (process == null) throw new InvalidOperationException("Unable to launch git.");
                var output = process.StandardOutput.ReadToEnd();
                var error = process.StandardError.ReadToEnd();
                process.WaitForExit();
                if (process.ExitCode != 0)
                    throw new InvalidOperationException("git failed: " + error.Trim());
                return output.Trim();
            }
        }

        private static bool GitSucceeds(params string[] arguments)
        {
            var startInfo = CreateGitStartInfo(arguments);
            using (var process = Process.Start(startInfo))
            {
                if (process == null) return false;
                process.StandardOutput.ReadToEnd();
                process.StandardError.ReadToEnd();
                process.WaitForExit();
                return process.ExitCode == 0;
            }
        }

        private static ProcessStartInfo CreateGitStartInfo(string[] arguments)
        {
            var startInfo = new ProcessStartInfo
            {
                FileName = "git",
                WorkingDirectory = Path.GetFullPath("."),
                UseShellExecute = false,
                CreateNoWindow = true,
                RedirectStandardOutput = true,
                RedirectStandardError = true
            };
            for (var index = 0; index < arguments.Length; index++)
                startInfo.ArgumentList.Add(arguments[index]);
            return startInfo;
        }

        private static string FirstLine(string value)
        {
            if (string.IsNullOrWhiteSpace(value)) return string.Empty;
            var newline = value.IndexOfAny(new[] { '\r', '\n' });
            return newline < 0 ? value.Trim() : value.Substring(0, newline).Trim();
        }

        [Serializable]
        private sealed class M10BuildManifest
        {
            public int schemaVersion;
            public string productName;
            public string gameVersion;
            public int buildNumber;
            public string buildConfiguration;
            public string targetPlatform;
            public string unityVersion;
            public string buildTarget;
            public bool development;
            public string executable;
            public string result;
            public BuildSourceState git;
            public string builtAtUtc;
            public string generatedAtUtc;
            public int contentSchemaVersion;
            public int saveSchemaVersion;
            public int settingsSaveSchemaVersion;
            public int profileSaveSchemaVersion;
            public int runRecoverySaveSchemaVersion;
            public ContentPackManifestDto[] contentPacks;
            public string packagesLockSha256;
            public string packagesManifestSha256;
            public string projectVersionSha256;
            public string addressablesSettingsSha256;
            public string publicApiFreezeSha256;
            public string thirdPartyNoticesSha256;
            public string assetProvenanceSha256;
            public string addressablesBuildHash;
            public int placeholderCount;
            public int unapprovedAssetCount;
            public int formalContentPackCount;
            public string releaseValidator;
            public string platformBackend;
            public bool offlineRequired;
            public BuildEvidenceDto tests;
            public BuildArtifactDto[] artifacts;
        }

        [Serializable]
        private sealed class ContentPackManifestDto
        {
            public string packId;
            public string version;
            public string contentHash;
            public string catalogHash;
            public string catalogSha256;
            public bool includedInPlayer;
            public bool placeholder;
            public bool official;
            public string sourceAssetPath;
        }

        [Serializable]
        private sealed class BuildEvidenceDto
        {
            public string editMode;
            public string playMode;
            public string contentValidation;
            public string soak;
            public string editModeSha256;
            public string playModeSha256;
            public string contentValidationSha256;
            public string performanceSha256;
        }

        [Serializable]
        private sealed class BuildArtifactDto
        {
            public string path;
            public string sha256;
        }

        [Serializable]
        private sealed class PerformanceStatusDto
        {
            public string status;
        }
    }
}
