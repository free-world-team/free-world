using System.Collections.Generic;
using System.Reflection;
using Game.Content.Runtime;
using Game.Editor;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;

namespace Game.Tests.EditMode
{
    public sealed class QinglanG36ReleaseContractTests
    {
        [Test]
        public void FormalCatalogIsOneOfficialFrozenDemoPack()
        {
            var catalog = QinglanG36ReleaseCatalog.ValidateOrThrow();
            Assert.That(catalog.Manifest.PackId.Value, Is.EqualTo(QinglanG36ReleaseCatalog.ExpectedPackId));
            Assert.That(catalog.Manifest.Version.ToString(), Is.EqualTo(QinglanG36ReleaseCatalog.ExpectedPackVersion));
            Assert.That(catalog.Manifest.Official, Is.True);
            Assert.That(catalog.Manifest.SourceAssetPath,
                Is.EqualTo(QinglanG36ReleaseCatalog.ReleaseCatalogPath));
            Assert.That(catalog.Definitions.Count, Is.EqualTo(QinglanG36ReleaseCatalog.ExpectedDefinitionCount));
            Assert.That(catalog.ContentHash, Has.Length.EqualTo(64));
        }

        [Test]
        public void PromotionPreservesEveryFrozenRuntimeDefinition()
        {
            var sourceAsset = AssetDatabase.LoadAssetAtPath<TextAsset>(
                QinglanG36ReleaseCatalog.SourceCatalogPath);
            var releaseAsset = AssetDatabase.LoadAssetAtPath<TextAsset>(
                QinglanG36ReleaseCatalog.ReleaseCatalogPath);
            Assert.That(sourceAsset, Is.Not.Null);
            Assert.That(releaseAsset, Is.Not.Null);
            var source = JsonUtility.FromJson<BakedContentCatalogDto>(sourceAsset.text);
            var release = JsonUtility.FromJson<BakedContentCatalogDto>(releaseAsset.text);
            Assert.That(release.definitions.Length, Is.EqualTo(source.definitions.Length));
            for (var index = 0; index < source.definitions.Length; index++)
                Assert.That(
                    JsonUtility.ToJson(release.definitions[index], false),
                    Is.EqualTo(JsonUtility.ToJson(source.definitions[index], false)),
                    "definition index " + index);
        }

        [Test]
        public void ReleaseAddressablesAllowlistExcludesDefaultAndDevelopmentGroups()
        {
            var type = typeof(WindowsReleaseBuild).Assembly.GetType("Game.Editor.ReleaseAddressablesScope");
            var method = type?.GetMethod(
                "IsFormalDemoGroup",
                BindingFlags.Static | BindingFlags.NonPublic);
            Assert.That(method, Is.Not.Null);
            AssertGroup(method, "QinglanDemo-Visual", true);
            AssertGroup(method, "QinglanDemo-Audio", true);
            AssertGroup(method, "QinglanDemo-Localization", true);
            AssertGroup(method, "ThirdParty-Fonts", true);
            AssertGroup(method, "Localization-Locales", true);
            AssertGroup(method, "Default Local Group", false);
            AssertGroup(method, "TestContent", false);
        }

        [Test]
        public void ReleaseSceneInputsAreFormalAndLegacyInputsRemainBlocked()
        {
            var formalCatalogIssue = ReleaseBuildPolicy.ValidateEntry(
                QinglanG36ReleaseCatalog.ReleaseCatalogPath,
                new HashSet<string>());
            var formalInputIssue = ReleaseBuildPolicy.ValidateEntry(
                QinglanG36ReleaseCatalog.ReleaseInputActionsPath,
                new HashSet<string>());
            var legacyInputIssue = ReleaseBuildPolicy.ValidateEntry(
                M7ProjectSetup.LegacyInputAssetPath,
                new HashSet<string>());
            Assert.That(formalCatalogIssue, Is.Null);
            Assert.That(formalInputIssue, Is.Null);
            Assert.That(legacyInputIssue?.Code, Is.EqualTo("M9-RELEASE-PLACEHOLDER"));
        }

        private static void AssertGroup(MethodInfo method, string groupName, bool expected)
        {
            Assert.That(method.Invoke(null, new object[] { groupName }), Is.EqualTo(expected), groupName);
        }
    }
}
