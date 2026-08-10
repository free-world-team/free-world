using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEditor.AddressableAssets.Settings;
using UnityEditor.Localization;
using UnityEditor.Localization.Addressables;
using UnityEngine;
using UnityEngine.Localization;

namespace Game.Editor
{
    /// <summary>
    /// Keeps governed Qinglan string tables in their release-only group after a cold import,
    /// while leaving legacy and package-owned localization assets on Unity's default rules.
    /// </summary>
    [Serializable]
    public sealed class QinglanStringTableGroupResolver : GroupResolver
    {
        public QinglanStringTableGroupResolver()
            : base("Localization-String-Tables-{LocaleName}", "Localization-Assets-Shared")
        {
        }

        public override string GetExpectedGroupName(
            IList<LocaleIdentifier> locales,
            UnityEngine.Object asset,
            AddressableAssetSettings settings)
        {
            var path = AssetDatabase.GetAssetPath(asset);
            return !string.IsNullOrEmpty(path) &&
                   path.StartsWith(QinglanG33LocalizationIntegration.LocalizationRoot, StringComparison.Ordinal)
                ? AssetProvenanceValidator.QinglanLocalizationGroup
                : base.GetExpectedGroupName(locales, asset, settings);
        }
    }

    public static class QinglanG33AddressableGroupRulesCommand
    {
        public const string RulesAssetPath =
            "Assets/GameContent/QinglanDemo/Localization/QinglanDemoAddressableGroupRules.asset";

        public static void Run()
        {
            try
            {
                var rules = AssetDatabase.LoadAssetAtPath<AddressableGroupRules>(RulesAssetPath);
                if (rules == null)
                {
                    rules = ScriptableObject.CreateInstance<AddressableGroupRules>();
                    AssetDatabase.CreateAsset(rules, RulesAssetPath);
                }

                rules.StringTablesResolver = new QinglanStringTableGroupResolver();
                AddressableGroupRules.Instance = rules;
                RefreshFormalCollection("UI");
                RefreshFormalCollection("QinglanContent");
                RefreshFormalCollection("QinglanNarrative");
                AssetDatabase.SaveAssets();
                Debug.Log("[Qinglan G3.3 Addressable Group Rules] PASS");
                EditorApplication.Exit(0);
            }
            catch (Exception exception)
            {
                Debug.LogException(exception);
                EditorApplication.Exit(1);
            }
        }

        private static void RefreshFormalCollection(string collectionName)
        {
            var collection = LocalizationEditorSettings.GetStringTableCollection(collectionName);
            if (collection == null)
                throw new InvalidOperationException("Missing formal collection: " + collectionName);
            collection.RefreshAddressables();
        }
    }
}
