using System;
using Game.Infrastructure;
using Game.Presentation;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.InputSystem;

namespace Game.Editor
{
    /// <summary>Wires checked-in development catalogs, camera, and the reviewed input maps.</summary>
    public static class M7ProjectSetup
    {
        public const string LegacyInputAssetPath = "Assets/GameAssets/Placeholder/M7InputActions.asset";
        public const string InputAssetPath = QinglanG36ReleaseCatalog.ReleaseInputActionsPath;

        private static readonly string[] AdditionalCatalogPaths =
        {
            "Assets/GameAssets/Placeholder/TestSkillContent/TestM4SkillContentPack.baked.json",
            "Assets/GameAssets/Placeholder/TestM5Content/TestM5ContentPack.baked.json",
            "Assets/GameAssets/Placeholder/TestBuildContent/TestM6BuildContentPack.baked.json"
        };

        [MenuItem("Tools/Free World/M7/Configure Presentation UI Input")]
        public static void Configure()
        {
            if (!AssetDatabase.IsValidFolder(QinglanG36ReleaseCatalog.ReleaseFolder))
                AssetDatabase.CreateFolder("Assets/GameContent/QinglanDemo", "Runtime");
            var input = AssetDatabase.LoadAssetAtPath<InputActionAsset>(InputAssetPath);
            if (input == null)
            {
                if (AssetDatabase.LoadAssetAtPath<InputActionAsset>(LegacyInputAssetPath) != null)
                {
                    if (!AssetDatabase.CopyAsset(LegacyInputAssetPath, InputAssetPath))
                        throw new UnityException("Unable to copy the reviewed Qinglan input asset.");
                }
                else
                {
                    input = M7InputRouter.CreateDefaultActions();
                    input.name = "QinglanInputActions";
                    AssetDatabase.CreateAsset(input, InputAssetPath);
                }
                AssetDatabase.SaveAssets();
                AssetDatabase.ImportAsset(InputAssetPath, ImportAssetOptions.ForceSynchronousImport);
                input = AssetDatabase.LoadAssetAtPath<InputActionAsset>(InputAssetPath);
                if (input == null) throw new UnityException("Unable to reload the M7 input asset.");
            }

            var scene = EditorSceneManager.OpenScene(M0ProjectSetup.BootstrapScenePath, OpenSceneMode.Single);
            GameBootstrapper bootstrapper = null;
            Camera camera = null;
            var roots = scene.GetRootGameObjects();
            for (var index = 0; index < roots.Length; index++)
            {
                if (bootstrapper == null) bootstrapper = roots[index].GetComponentInChildren<GameBootstrapper>(true);
                if (camera == null) camera = roots[index].GetComponentInChildren<Camera>(true);
            }
            if (bootstrapper == null) throw new UnityException("Bootstrap scene has no GameBootstrapper.");
            if (camera == null) throw new UnityException("Bootstrap scene has no presentation camera.");

            var catalogs = new TextAsset[AdditionalCatalogPaths.Length];
            for (var index = 0; index < AdditionalCatalogPaths.Length; index++)
            {
                var catalog = AssetDatabase.LoadAssetAtPath<TextAsset>(AdditionalCatalogPaths[index]);
                if (catalog == null) throw new UnityException("Missing M7 catalog: " + AdditionalCatalogPaths[index]);
                catalogs[index] = catalog;
            }
            bootstrapper.ConfigureM7Assets(catalogs, camera, input);
            EditorUtility.SetDirty(bootstrapper);
            AssetDatabase.SaveAssets();
            if (!EditorSceneManager.SaveScene(scene, M0ProjectSetup.BootstrapScenePath, false))
                throw new UnityException("Unable to save M7 Bootstrap scene.");
            Debug.Log("[M7 Setup] Presentation, UI, input maps, and four Placeholder catalogs configured.");
        }

        public static void RunFromCommandLine()
        {
            try
            {
                Configure();
                EditorApplication.Exit(0);
            }
            catch (Exception exception)
            {
                Debug.LogException(exception);
                EditorApplication.Exit(1);
            }
        }
    }
}
