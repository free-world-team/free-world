using System;
using System.Collections.Generic;
using System.IO;
using Game.Infrastructure;
using UnityEditor;
using UnityEditor.AddressableAssets;
using UnityEditor.AddressableAssets.Build;
using UnityEditor.AddressableAssets.Settings;
using UnityEditor.AddressableAssets.Settings.GroupSchemas;
using UnityEditor.Build;
using UnityEditor.Build.Reporting;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;

namespace Game.Editor
{
    /// <summary>Builds the formal Qinglan Demo Windows Release candidate.</summary>
    public static class WindowsReleaseBuild
    {
        public const string DefaultOutputPath = "Builds/WindowsRelease/AzureSword.exe";
        internal const string TemporaryScenePath = "Assets/__QinglanG36Release.generated.unity";

        /// <summary>Builds the formal Demo Release candidate from the Editor menu.</summary>
        [MenuItem("Tools/Free World/Qinglan/G3.6 Build Windows Release Candidate")]
        public static void BuildFromMenu()
        {
            Build(DefaultOutputPath);
        }

        /// <summary>Builds the formal Demo Release candidate and exits the Editor.</summary>
        public static void BuildFromCommandLine()
        {
            var exitCode = 0;
            try
            {
                var output = Environment.GetEnvironmentVariable("BUILD_OUTPUT");
                Build(string.IsNullOrWhiteSpace(output) ? DefaultOutputPath : output);
            }
            catch (Exception exception)
            {
                Debug.LogException(exception);
                exitCode = 1;
            }
            finally
            {
                try
                {
                    DeleteTemporaryScene();
                    WindowsDevelopmentBuild.RemoveTemporaryAddressablesLinkXml();
                }
                catch (Exception exception)
                {
                    Debug.LogException(exception);
                    exitCode = 1;
                }
            }

            EditorApplication.Exit(exitCode);
        }

        /// <summary>Builds a placeholder-free formal Demo player at the requested path.</summary>
        public static BuildReport Build(string outputPath)
        {
            if (string.IsNullOrWhiteSpace(outputPath))
                throw new ArgumentException("Build output path is required.", nameof(outputPath));
            QinglanG36ReleaseCatalog.ValidateOrThrow();
            var validation = ProjectGovernanceValidator.ValidateCurrentProject();
            if (!validation.IsValid) throw new BuildFailedException(validation.Issues[0].ToString());
            var sourceState = BuildManifestWriter.CaptureSourceState();
            var absoluteOutput = Path.GetFullPath(outputPath);
            var outputDirectory = Path.GetDirectoryName(absoluteOutput);
            if (string.IsNullOrEmpty(outputDirectory))
                throw new BuildFailedException("Unable to resolve Release output directory.");
            Directory.CreateDirectory(outputDirectory);

            try
            {
                BuildReport report;
                using (var addressables = ReleaseAddressablesScope.IncludeOnlyFormalDemoGroups())
                {
                    var releaseValidation = ProjectGovernanceValidator.ValidateCurrentProject();
                    ReleaseBuildGateValidator.AppendCurrentProject(releaseValidation);
                    CreateTemporaryScene();
                    ReleaseBuildGateValidator.AppendSceneDependencies(
                        releaseValidation,
                        TemporaryScenePath);
                    if (!releaseValidation.IsValid)
                        throw new BuildFailedException(releaseValidation.Issues[0].ToString());
                    if (ReleaseBuildGateValidator.CountIncludedPlaceholderEntries() != 0)
                        throw new BuildFailedException("Release input still includes Placeholder Addressables.");

                    var settings = AddressableAssetSettingsDefaultObject.GetSettings(false);
                    if (settings == null) throw new BuildFailedException("Addressables settings are unavailable.");
                    AddressableAssetSettings.BuildPlayerContent(out AddressablesPlayerBuildResult contentResult);
                    if (!string.IsNullOrEmpty(contentResult.Error))
                        throw new BuildFailedException("Formal Addressables build failed: " + contentResult.Error);
                    var previousBuildOption = settings.BuildAddressablesWithPlayerBuild;
                    try
                    {
                        settings.BuildAddressablesWithPlayerBuild =
                            AddressableAssetSettings.PlayerBuildOption.DoNotBuildWithPlayer;
                        report = BuildPipeline.BuildPlayer(new BuildPlayerOptions
                        {
                            scenes = new[] { TemporaryScenePath },
                            locationPathName = absoluteOutput,
                            target = BuildTarget.StandaloneWindows64,
                            options = BuildOptions.None
                        });
                    }
                    finally
                    {
                        settings.BuildAddressablesWithPlayerBuild = previousBuildOption;
                    }

                    if (report.summary.result != BuildResult.Succeeded)
                        throw new BuildFailedException(
                            "Windows Release Build failed with result " + report.summary.result + ".");
                    if (ReleaseBuildGateValidator.CountIncludedPlaceholderEntries() != 0)
                        throw new BuildFailedException("Release output still includes Placeholder Addressables.");
                    BuildManifestWriter.Write(
                        outputDirectory,
                        absoluteOutput,
                        report,
                        "WindowsReleaseCandidate",
                        false,
                        sourceState);
                    Debug.Log("[Qinglan G3.6 Release Build] PASS: " + absoluteOutput +
                              "; includedGroups=" + addressables.IncludedGroupCount +
                              "; excludedGroups=" + addressables.ExcludedGroupCount + ".");
                }

                return report;
            }
            finally
            {
                DeleteTemporaryScene();
            }
        }

        private static void CreateTemporaryScene()
        {
            DeleteTemporaryScene();
            var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            var catalog = AssetDatabase.LoadAssetAtPath<TextAsset>(
                QinglanG36ReleaseCatalog.ReleaseCatalogPath);
            var input = AssetDatabase.LoadAssetAtPath<InputActionAsset>(
                QinglanG36ReleaseCatalog.ReleaseInputActionsPath);
            if (catalog == null || input == null)
                throw new BuildFailedException("Formal Qinglan catalog or input asset is missing.");

            var cameraObject = new GameObject("Main Camera");
            cameraObject.tag = "MainCamera";
            var camera = cameraObject.AddComponent<Camera>();
            camera.clearFlags = CameraClearFlags.SolidColor;
            camera.backgroundColor = new Color(0.025f, 0.045f, 0.055f, 1f);
            camera.orthographic = true;
            cameraObject.transform.position = new Vector3(0f, 0f, -10f);

            var owner = new GameObject("QinglanReleaseBootstrapper");
            var bootstrapper = owner.AddComponent<GameBootstrapper>();
            var serialized = new SerializedObject(bootstrapper);
            serialized.FindProperty("bakedTestCatalog").objectReferenceValue = catalog;
            serialized.FindProperty("additionalBakedCatalogs").arraySize = 0;
            serialized.FindProperty("presentationCamera").objectReferenceValue = camera;
            serialized.FindProperty("inputActions").objectReferenceValue = input;
            serialized.ApplyModifiedPropertiesWithoutUndo();
            if (!EditorSceneManager.SaveScene(scene, TemporaryScenePath))
                throw new BuildFailedException("Unable to save the generated Qinglan Release scene.");
            AssetDatabase.ImportAsset(TemporaryScenePath, ImportAssetOptions.ForceSynchronousImport);
        }

        private static void DeleteTemporaryScene()
        {
            if (!File.Exists(Path.GetFullPath(TemporaryScenePath))) return;
            EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            AssetDatabase.DeleteAsset(TemporaryScenePath);
            AssetDatabase.Refresh(ImportAssetOptions.ForceSynchronousImport);
        }
    }

    internal sealed class ReleaseAddressablesScope : IDisposable
    {
        private sealed class GroupState
        {
            public BundledAssetGroupSchema Schema;
            public bool Included;
        }

        private readonly List<GroupState> states = new List<GroupState>();

        private ReleaseAddressablesScope()
        {
        }

        public int ExcludedGroupCount { get; private set; }
        public int IncludedGroupCount { get; private set; }

        public static ReleaseAddressablesScope IncludeOnlyFormalDemoGroups()
        {
            var scope = new ReleaseAddressablesScope();
            var settings = AddressableAssetSettingsDefaultObject.GetSettings(false);
            if (settings == null) throw new BuildFailedException("Addressables settings are unavailable.");
            for (var groupIndex = 0; groupIndex < settings.groups.Count; groupIndex++)
            {
                var group = settings.groups[groupIndex];
                var schema = group?.GetSchema<BundledAssetGroupSchema>();
                if (group == null) continue;
                if (schema == null && group.entries.Count > 0)
                    throw new BuildFailedException("Addressables group has no bundled schema: " + group.Name);
                if (schema == null) continue;
                scope.states.Add(new GroupState { Schema = schema, Included = schema.IncludeInBuild });
                var include = IsFormalDemoGroup(group.Name);
                schema.IncludeInBuild = include;
                if (include) scope.IncludedGroupCount++;
                else scope.ExcludedGroupCount++;
            }

            if (scope.IncludedGroupCount < 4)
                throw new BuildFailedException("Formal Qinglan Addressables groups are incomplete.");

            return scope;
        }

        public void Dispose()
        {
            for (var index = states.Count - 1; index >= 0; index--)
                states[index].Schema.IncludeInBuild = states[index].Included;
            states.Clear();
        }

        internal static bool IsFormalDemoGroup(string groupName)
        {
            return string.Equals(groupName, AssetProvenanceValidator.QinglanVisualGroup, StringComparison.Ordinal) ||
                   string.Equals(groupName, AssetProvenanceValidator.QinglanAudioGroup, StringComparison.Ordinal) ||
                   string.Equals(groupName, AssetProvenanceValidator.QinglanLocalizationGroup, StringComparison.Ordinal) ||
                   string.Equals(groupName, AssetProvenanceValidator.ThirdPartyFontGroup, StringComparison.Ordinal) ||
                   (groupName ?? string.Empty).StartsWith("Localization-", StringComparison.Ordinal);
        }
    }
}
