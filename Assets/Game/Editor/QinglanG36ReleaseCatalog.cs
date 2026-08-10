using System;
using System.IO;
using Game.Content.Runtime;
using UnityEditor;
using UnityEngine;

namespace Game.Editor
{
    /// <summary>Promotes the frozen Qinglan bake into the single formal Release runtime catalog.</summary>
    public static class QinglanG36ReleaseCatalog
    {
        public const string SourceCatalogPath =
            "Assets/GameAssets/Placeholder/QinglanDemo/QinglanDemoContentPack.baked.json";
        public const string ReleaseFolder = "Assets/GameContent/QinglanDemo/Runtime";
        public const string ReleaseCatalogPath = ReleaseFolder + "/QinglanDemoContentPack.release.json";
        public const string ReleaseInputActionsPath = ReleaseFolder + "/QinglanInputActions.asset";
        public const string ExpectedPackId = "qinglan.pack.demo";
        public const string ExpectedPackVersion = "0.10.0";
        public const int ExpectedDefinitionCount = 193;

        [MenuItem("Tools/Free World/Qinglan/G3.6 Promote Release Catalog")]
        public static void Promote()
        {
            EnsureFolder("Assets/GameContent/QinglanDemo", "Runtime");
            var sourceAsset = AssetDatabase.LoadAssetAtPath<TextAsset>(SourceCatalogPath);
            if (sourceAsset == null) throw new InvalidOperationException("Frozen Qinglan catalog is missing.");
            var sourceDto = JsonUtility.FromJson<BakedContentCatalogDto>(sourceAsset.text);
            if (sourceDto == null) throw new InvalidOperationException("Frozen Qinglan catalog has no DTO.");
            var sourceResult = sourceDto.ToCatalog();
            if (!sourceResult.IsSuccess)
                throw new InvalidOperationException("Frozen Qinglan catalog is invalid: " +
                                                    sourceResult.Error);
            var source = sourceResult.Value;
            if (!string.Equals(source.Manifest.PackId.Value, ExpectedPackId, StringComparison.Ordinal) ||
                !string.Equals(source.Manifest.Version.ToString(), ExpectedPackVersion, StringComparison.Ordinal) ||
                source.Definitions.Count != ExpectedDefinitionCount)
                throw new InvalidOperationException("Frozen Qinglan catalog identity does not match G3.6.");

            var dependencies = new ContentPackDependency[source.Manifest.Dependencies.Count];
            for (var index = 0; index < dependencies.Length; index++)
                dependencies[index] = source.Manifest.Dependencies[index];
            var definitions = new RuntimeContentDefinition[source.Definitions.Count];
            for (var index = 0; index < definitions.Length; index++) definitions[index] = source.Definitions[index];
            var manifest = new ContentPackManifest(
                source.Manifest.PackId,
                source.Manifest.Version,
                source.Manifest.SchemaVersion,
                source.Manifest.MinimumGameVersion,
                source.Manifest.MaximumGameVersion,
                dependencies,
                source.Manifest.CatalogAddress,
                source.Manifest.AssetLabel,
                true,
                ReleaseCatalogPath);
            var promoted = BakedContentCatalog.Create(manifest, definitions);
            File.WriteAllText(
                Path.GetFullPath(ReleaseCatalogPath),
                JsonUtility.ToJson(promoted.ToDto(), true) + "\n");
            AssetDatabase.ImportAsset(ReleaseCatalogPath, ImportAssetOptions.ForceSynchronousImport);

            if (AssetDatabase.LoadAssetAtPath<UnityEngine.InputSystem.InputActionAsset>(
                    ReleaseInputActionsPath) == null)
            {
                if (!AssetDatabase.CopyAsset(M7ProjectSetup.LegacyInputAssetPath, ReleaseInputActionsPath))
                    throw new InvalidOperationException("Unable to create the formal Qinglan input asset.");
                AssetDatabase.ImportAsset(ReleaseInputActionsPath, ImportAssetOptions.ForceSynchronousImport);
            }

            AssetDatabase.SaveAssets();
            ValidateOrThrow();
            Debug.Log("[Qinglan G3.6 Release Catalog] PASS: pack=" + ExpectedPackId +
                      ", version=" + ExpectedPackVersion + ", definitions=" +
                      ExpectedDefinitionCount + ", hash=" + promoted.ContentHash + ".");
        }

        public static BakedContentCatalog ValidateOrThrow()
        {
            var releaseAsset = AssetDatabase.LoadAssetAtPath<TextAsset>(ReleaseCatalogPath);
            if (releaseAsset == null) throw new InvalidOperationException("Formal Qinglan Release catalog is missing.");
            var dto = JsonUtility.FromJson<BakedContentCatalogDto>(releaseAsset.text);
            if (dto == null) throw new InvalidOperationException("Formal Qinglan Release catalog has no DTO.");
            var result = dto.ToCatalog();
            if (!result.IsSuccess)
                throw new InvalidOperationException("Formal Qinglan Release catalog is invalid: " +
                                                    result.Error);
            var catalog = result.Value;
            if (!string.Equals(catalog.Manifest.PackId.Value, ExpectedPackId, StringComparison.Ordinal) ||
                !string.Equals(catalog.Manifest.Version.ToString(), ExpectedPackVersion, StringComparison.Ordinal) ||
                !catalog.Manifest.Official ||
                !string.Equals(catalog.Manifest.SourceAssetPath, ReleaseCatalogPath, StringComparison.Ordinal) ||
                catalog.Definitions.Count != ExpectedDefinitionCount)
                throw new InvalidOperationException("Formal Qinglan Release catalog contract is not satisfied.");
            if (AssetDatabase.LoadAssetAtPath<UnityEngine.InputSystem.InputActionAsset>(
                    ReleaseInputActionsPath) == null)
                throw new InvalidOperationException("Formal Qinglan input asset is missing.");
            return catalog;
        }

        public static void RunFromCommandLine()
        {
            var exitCode = 0;
            try { Promote(); }
            catch (Exception exception) { Debug.LogException(exception); exitCode = 1; }
            EditorApplication.Exit(exitCode);
        }

        private static void EnsureFolder(string parent, string name)
        {
            if (!AssetDatabase.IsValidFolder(parent + "/" + name)) AssetDatabase.CreateFolder(parent, name);
        }
    }
}
