using System;
using System.IO;
using Game.Content.Runtime;
using Game.Editor;
using NUnit.Framework;
using UnityEditor;
using UnityEditor.AddressableAssets;
using UnityEditor.Localization;
using UnityEngine;
using UnityEngine.Localization;
using UnityEngine.Localization.Tables;

namespace Game.Tests.EditMode
{
    public sealed class QinglanG33LocContent001Tests
    {
        [Test]
        public void FormalContentContainsEveryBakedNameDescriptionAndLevelChange()
        {
            var collection = LocalizationEditorSettings.GetStringTableCollection("QinglanContent");
            Assert.That(collection, Is.Not.Null);
            var english = collection.GetTable(new LocaleIdentifier("en")) as StringTable;
            var chinese = collection.GetTable(new LocaleIdentifier("zh-Hans")) as StringTable;
            Assert.That(english, Is.Not.Null);
            Assert.That(chinese, Is.Not.Null);
            Assert.That(collection.SharedData.Entries.Count, Is.EqualTo(492));

            var dto = JsonUtility.FromJson<BakedContentCatalogDto>(
                File.ReadAllText(QinglanG12ContentSetup.BakedCatalogPath));
            Assert.That(dto.definitions, Has.Length.EqualTo(193));
            var expectedLevelChanges = 0;
            foreach (var definition in dto.definitions)
            {
                AssertCompletePair(collection, english, chinese, definition.localizedNameKey);
                AssertCompletePair(collection, english, chinese, definition.localizedDescriptionKey);
                var patches = definition.levelPatches ?? Array.Empty<SkillLevelPatchDto>();
                var root = definition.localizedDescriptionKey.Substring(
                    0,
                    definition.localizedDescriptionKey.Length - ".description".Length);
                for (var index = 0; index < patches.Length; index++)
                {
                    var key = root + ".level." + patches[index].level + ".change." + (index + 1);
                    AssertCompletePair(collection, english, chinese, key);
                    expectedLevelChanges++;
                }
            }
            Assert.That(expectedLevelChanges, Is.EqualTo(106));
        }

        [Test]
        public void GovernedQinglanTerminologyAndLevelChangesAreExact()
        {
            var collection = LocalizationEditorSettings.GetStringTableCollection("QinglanContent");
            AssertPair(collection, "content.qinglan.skill.weapon.yufeng_sword.name", "Yufeng Sword", "御风剑");
            AssertPair(collection, "content.qinglan.map.old_court.name", "Old Court", "旧庭");
            AssertPair(collection, "content.qinglan.enemy.boss.tingfeng.name", "Tingfeng", "听风");
            AssertPair(
                collection,
                "content.qinglan.skill.evolved.qinglan_flowing_shadow_sword.name",
                "Qinglan Flowing-Shadow Sword",
                "青岚流影剑");
            AssertPair(
                collection,
                "content.qinglan.skill.weapon.yufeng_sword.level.2.change.1",
                "Level 2: Effect strength +4.",
                "等级2：效果强度+4。");
        }

        [Test]
        public void FormalContentUsesDedicatedReleaseGroupAndCanonicalAddresses()
        {
            var settings = AddressableAssetSettingsDefaultObject.GetSettings(false);
            Assert.That(settings, Is.Not.Null);
            var group = settings.FindGroup(AssetProvenanceValidator.QinglanLocalizationGroup);
            Assert.That(group, Is.Not.Null);
            var count = 0;
            var enFound = false;
            var zhFound = false;
            foreach (var entry in group.entries)
            {
                var path = AssetDatabase.GUIDToAssetPath(entry.guid);
                if (!path.StartsWith(QinglanG33LocalizationIntegration.ContentTablesRoot, StringComparison.Ordinal))
                    continue;
                count++;
                Assert.That(entry.labels, Does.Contain(AssetProvenanceValidator.LocalizationReleaseLabel));
                Assert.That(entry.labels, Does.Contain(AssetProvenanceValidator.ReleaseLabel));
                Assert.That(entry.labels, Does.Contain(AssetProvenanceValidator.QinglanPackLabel));
                if (entry.address == "QinglanContent_en") enFound = true;
                if (entry.address == "QinglanContent_zh-Hans") zhFound = true;
            }
            Assert.That(count, Is.EqualTo(3));
            Assert.That(enFound, Is.True);
            Assert.That(zhFound, Is.True);
        }

        [Test]
        public void PseudoLocaleTransformsContentWithoutAuthoredPseudoTable()
        {
            var pseudo = LocalizationEditorSettings.GetPseudoLocales()[0];
            const string sample = "Yufeng Sword joins the automatic attack cycle.";
            Assert.That(pseudo.GetPseudoString(sample), Is.Not.EqualTo(sample));
            Assert.That(
                LocalizationEditorSettings.GetStringTableCollection("QinglanContent")?.GetTable(pseudo.Identifier),
                Is.Null);
        }

        private static void AssertCompletePair(
            UnityEditor.Localization.StringTableCollection collection,
            StringTable english,
            StringTable chinese,
            string key)
        {
            var shared = collection.SharedData.GetEntry(key);
            Assert.That(shared, Is.Not.Null, key);
            var en = english.GetEntry(shared.Id);
            var zh = chinese.GetEntry(shared.Id);
            Assert.That(en, Is.Not.Null, key + " en");
            Assert.That(zh, Is.Not.Null, key + " zh-Hans");
            Assert.That(en.Value, Is.Not.Empty, key + " en");
            Assert.That(zh.Value, Is.Not.Empty, key + " zh-Hans");
            Assert.That(en.Value, Is.Not.EqualTo(key));
            Assert.That(zh.Value, Is.Not.EqualTo(key));
            Assert.That(en.Value, Does.Not.Contain("[Placeholder]"));
            Assert.That(zh.Value, Does.Not.Contain("[占位]"));
            Assert.That(en.Value, Does.Not.Contain("Unavailable"));
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
