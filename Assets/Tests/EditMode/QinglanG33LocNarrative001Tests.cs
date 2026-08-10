using System;
using System.Collections.Generic;
using System.IO;
using System.Text.RegularExpressions;
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
    public sealed class QinglanG33LocNarrative001Tests
    {
        [Test]
        public void FormalNarrativeContainsOneHundredTwentyThreeReviewedBilingualLines()
        {
            var collection = LocalizationEditorSettings.GetStringTableCollection("QinglanNarrative");
            Assert.That(collection, Is.Not.Null);
            var english = collection.GetTable(new LocaleIdentifier("en")) as StringTable;
            var chinese = collection.GetTable(new LocaleIdentifier("zh-Hans")) as StringTable;
            Assert.That(english, Is.Not.Null);
            Assert.That(chinese, Is.Not.Null);
            Assert.That(collection.SharedData.Entries.Count, Is.EqualTo(123));

            var categoryCounts = new Dictionary<string, int>(StringComparer.Ordinal)
            {
                { "story.qinglan.", 0 },
                { "collectible.qinglan.", 0 },
                { "narrative.qinglan.objective.", 0 },
                { "narrative.qinglan.event.", 0 },
                { "narrative.qinglan.landmark.", 0 },
                { "narrative.qinglan.boss.", 0 },
                { "narrative.qinglan.map.", 0 }
            };
            foreach (var shared in collection.SharedData.Entries)
            {
                var en = english.GetEntry(shared.Id);
                var zh = chinese.GetEntry(shared.Id);
                Assert.That(en, Is.Not.Null, shared.Key + " en");
                Assert.That(zh, Is.Not.Null, shared.Key + " zh-Hans");
                Assert.That(en.Value, Is.Not.Empty, shared.Key + " en");
                Assert.That(zh.Value, Is.Not.Empty, shared.Key + " zh-Hans");
                Assert.That(en.Value, Is.Not.EqualTo(shared.Key));
                Assert.That(zh.Value, Is.Not.EqualTo(shared.Key));
                Assert.That(en.Value, Does.Not.Contain("[Placeholder]"));
                Assert.That(zh.Value, Does.Not.Contain("[占位]"));
                Assert.That(Regex.IsMatch(zh.Value, "[\\u3400-\\u9fff]"), Is.True, shared.Key);
                foreach (var prefix in new List<string>(categoryCounts.Keys))
                    if (shared.Key.StartsWith(prefix, StringComparison.Ordinal))
                        categoryCounts[prefix]++;
            }
            Assert.That(categoryCounts["story.qinglan."], Is.EqualTo(24));
            Assert.That(categoryCounts["collectible.qinglan."], Is.EqualTo(24));
            Assert.That(categoryCounts["narrative.qinglan.objective."], Is.EqualTo(12));
            Assert.That(categoryCounts["narrative.qinglan.event."], Is.EqualTo(12));
            Assert.That(categoryCounts["narrative.qinglan.landmark."], Is.EqualTo(15));
            Assert.That(categoryCounts["narrative.qinglan.boss."], Is.EqualTo(30));
            Assert.That(categoryCounts["narrative.qinglan.map."], Is.EqualTo(6));
        }

        [Test]
        public void EveryRuntimeStoryAndCollectibleNarrativeKeyIsCovered()
        {
            var collection = LocalizationEditorSettings.GetStringTableCollection("QinglanNarrative");
            var dto = JsonUtility.FromJson<BakedContentCatalogDto>(
                File.ReadAllText(QinglanG12ContentSetup.BakedCatalogPath));
            var runtimeKeyCount = 0;
            foreach (var definition in dto.definitions)
            {
                if (definition.kind == RuntimeContentKinds.Story)
                {
                    foreach (var key in definition.qinglanRuntime.localizedSequenceKeys)
                    {
                        Assert.That(collection.SharedData.GetEntry(key), Is.Not.Null, key);
                        runtimeKeyCount++;
                    }
                }
                else if (definition.kind == RuntimeContentKinds.Collectible)
                {
                    Assert.That(collection.SharedData.GetEntry(definition.qinglanRuntime.text0), Is.Not.Null,
                        definition.qinglanRuntime.text0);
                    runtimeKeyCount++;
                }
            }
            Assert.That(runtimeKeyCount, Is.EqualTo(12));
        }

        [Test]
        public void NarrativeContinuitySamplesAreExactInBothLocales()
        {
            var collection = LocalizationEditorSettings.GetStringTableCollection("QinglanNarrative");
            AssertPair(
                collection,
                "story.qinglan.story.lu_qingye.hearing_sword.01",
                "When the wind crossed the ruined stele, Lu Qingye heard a sword chime where no sword remained.",
                "风过残碑时，陆青野听见了一声剑鸣——那里分明早已无剑。");
            AssertPair(
                collection,
                "collectible.qinglan.collectible.old_court.03.body",
                "Senior Shen: the sealed hall breathes after midnight. I heard it behind the western wall.",
                "沈师兄：封殿子时之后会呼吸。我在西墙后听见了。");
            AssertPair(
                collection,
                "narrative.qinglan.boss.tingfeng.18",
                "The bells are quiet at last. Take the records—and leave the door open.",
                "风铃终于安静了。带走藏录——也请让门继续开着。");
            AssertPair(
                collection,
                "narrative.qinglan.map.old_court.06",
                "Not every bell rang, and not every name returned—but the gate remains open.",
                "并非每只铃都再度响起，也并非每个名字都能归来——但山门已经打开。");
        }

        [Test]
        public void FormalNarrativeUsesDedicatedReleaseGroupAndCanonicalAddresses()
        {
            var settings = AddressableAssetSettingsDefaultObject.GetSettings(false);
            var group = settings.FindGroup(AssetProvenanceValidator.QinglanLocalizationGroup);
            Assert.That(group, Is.Not.Null);
            var count = 0;
            var enFound = false;
            var zhFound = false;
            foreach (var entry in group.entries)
            {
                var path = AssetDatabase.GUIDToAssetPath(entry.guid);
                if (!path.StartsWith(QinglanG33LocalizationIntegration.NarrativeTablesRoot, StringComparison.Ordinal))
                    continue;
                count++;
                Assert.That(entry.labels, Does.Contain(AssetProvenanceValidator.LocalizationReleaseLabel));
                Assert.That(entry.labels, Does.Contain(AssetProvenanceValidator.ReleaseLabel));
                Assert.That(entry.labels, Does.Contain(AssetProvenanceValidator.QinglanPackLabel));
                if (entry.address == "QinglanNarrative_en") enFound = true;
                if (entry.address == "QinglanNarrative_zh-Hans") zhFound = true;
            }
            Assert.That(count, Is.EqualTo(3));
            Assert.That(enFound, Is.True);
            Assert.That(zhFound, Is.True);
        }

        [Test]
        public void PseudoLocaleTransformsNarrativeWithoutAuthoredPseudoTable()
        {
            var pseudo = LocalizationEditorSettings.GetPseudoLocales()[0];
            const string sample = "The Old Court receives no visitors.";
            Assert.That(pseudo.GetPseudoString(sample), Is.Not.EqualTo(sample));
            Assert.That(
                LocalizationEditorSettings.GetStringTableCollection("QinglanNarrative")?.GetTable(pseudo.Identifier),
                Is.Null);
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
