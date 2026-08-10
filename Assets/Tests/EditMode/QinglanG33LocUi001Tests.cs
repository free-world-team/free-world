using System;
using System.IO;
using Game.Editor;
using NUnit.Framework;
using UnityEditor;
using UnityEditor.AddressableAssets;
using UnityEditor.Localization;
using UnityEngine.Localization;
using UnityEngine.Localization.Pseudo;
using UnityEngine.Localization.Tables;

namespace Game.Tests.EditMode
{
    public sealed class QinglanG33LocUi001Tests
    {
        [Test]
        public void FormalUiContainsAtLeastOneHundredEightyCompleteBilingualKeys()
        {
            var collection = LocalizationEditorSettings.GetStringTableCollection("UI");
            Assert.That(collection, Is.Not.Null);
            var english = collection.GetTable(new LocaleIdentifier("en")) as StringTable;
            var chinese = collection.GetTable(new LocaleIdentifier("zh-Hans")) as StringTable;
            Assert.That(english, Is.Not.Null);
            Assert.That(chinese, Is.Not.Null);
            Assert.That(collection.SharedData.Entries.Count, Is.GreaterThanOrEqualTo(180));
            foreach (var shared in collection.SharedData.Entries)
            {
                var en = english.GetEntry(shared.Id);
                var zh = chinese.GetEntry(shared.Id);
                Assert.That(en, Is.Not.Null, shared.Key + " en");
                Assert.That(zh, Is.Not.Null, shared.Key + " zh-Hans");
                Assert.That(en.Value, Is.Not.Empty, shared.Key + " en");
                Assert.That(zh.Value, Is.Not.Empty, shared.Key + " zh-Hans");
                Assert.That(en.Value, Does.Not.Contain("[Placeholder]"), shared.Key);
                Assert.That(zh.Value, Does.Not.Contain("[占位]"), shared.Key);
            }
        }

        [Test]
        public void RequiredQinglanUiTermsAreHumanReadableInBothLocales()
        {
            var collection = LocalizationEditorSettings.GetStringTableCollection("UI");
            AssertPair(collection, "ui.qinglan.title.name", "Sword Rises in Qinglan", "剑起青岚");
            AssertPair(collection, "ui.qinglan.hud.experience", "Sword Insight", "剑悟");
            AssertPair(collection, "ui.qinglan.map.legend.boss", "Oath Keeper", "古誓守主");
            AssertPair(collection, "ui.qinglan.settings.font_scale.150", "150%", "150%");
            AssertPair(collection, "save.error.invalid_run_result",
                "The run result is invalid and was not saved.", "本局结果无效，未执行保存。");
        }

        [Test]
        public void ExpandedPseudoLocaleTransformsFormalEnglishWithoutAuthoredPseudoTable()
        {
            var pseudoLocales = LocalizationEditorSettings.GetPseudoLocales();
            Assert.That(pseudoLocales.Count, Is.GreaterThan(0));
            var pseudo = pseudoLocales[0];
            Assert.That(pseudo, Is.TypeOf<PseudoLocale>());
            Assert.That(pseudo.Methods.Count, Is.GreaterThanOrEqualTo(4));
            const string sample = "The old court waits for the wind to return.";
            var transformed = pseudo.GetPseudoString(sample);
            Assert.That(transformed, Is.Not.EqualTo(sample));
            Assert.That(transformed.Length, Is.GreaterThan(sample.Length));
            Assert.That(LocalizationEditorSettings.GetStringTableCollection("UI")?.GetTable(pseudo.Identifier), Is.Null);
        }

        [Test]
        public void FormalUiUsesDedicatedReleaseGroupAndCanonicalAddresses()
        {
            var settings = AddressableAssetSettingsDefaultObject.GetSettings(false);
            Assert.That(settings, Is.Not.Null);
            var group = settings.FindGroup(AssetProvenanceValidator.QinglanLocalizationGroup);
            Assert.That(group, Is.Not.Null);
            var uiReleaseCount = 0;
            var enFound = false;
            var zhFound = false;
            foreach (var entry in group.entries)
            {
                if (!entry.labels.Contains(AssetProvenanceValidator.LocalizationReleaseLabel)) continue;
                Assert.That(entry.labels, Does.Contain(AssetProvenanceValidator.ReleaseLabel));
                Assert.That(entry.labels, Does.Contain(AssetProvenanceValidator.QinglanPackLabel));
                var path = AssetDatabase.GUIDToAssetPath(entry.guid);
                if (!path.StartsWith(QinglanG33LocalizationIntegration.UiTablesRoot, StringComparison.Ordinal))
                    continue;
                uiReleaseCount++;
                if (entry.address == "UI_en") enFound = true;
                if (entry.address == "UI_zh-Hans") zhFound = true;
                StringAssert.StartsWith(QinglanG33LocalizationIntegration.UiTablesRoot, path);
            }
            Assert.That(uiReleaseCount, Is.EqualTo(3));
            Assert.That(enFound, Is.True);
            Assert.That(zhFound, Is.True);
        }

        [Test]
        public void MixedM8CollectionRemainsPreservedAsNonReleaseLegacySource()
        {
            var legacy = LocalizationEditorSettings.GetStringTableCollection(
                QinglanG33LocalizationIntegration.LegacyUiCollection);
            var formal = LocalizationEditorSettings.GetStringTableCollection("UI");
            Assert.That(legacy, Is.Not.Null);
            Assert.That(formal, Is.Not.Null);
            Assert.That(legacy.SharedData.Entries.Count, Is.GreaterThan(formal.SharedData.Entries.Count));
            Assert.That(legacy.SharedData.GetEntry("content.test.passive.force.name"), Is.Not.Null);

            var settings = AddressableAssetSettingsDefaultObject.GetSettings(false);
            var legacyEntry = settings.FindAssetEntry(
                AssetDatabase.AssetPathToGUID(AssetDatabase.GetAssetPath(legacy.SharedData)));
            Assert.That(legacyEntry, Is.Not.Null);
            Assert.That(legacyEntry.labels, Does.Not.Contain(AssetProvenanceValidator.ReleaseLabel));
        }

        private static void AssertPair(
            UnityEditor.Localization.StringTableCollection collection,
            string key,
            string english,
            string chinese)
        {
            Assert.That((collection.GetTable(new LocaleIdentifier("en")) as StringTable)?.GetEntry(key)?.Value,
                Is.EqualTo(english));
            Assert.That((collection.GetTable(new LocaleIdentifier("zh-Hans")) as StringTable)?.GetEntry(key)?.Value,
                Is.EqualTo(chinese));
        }
    }
}
