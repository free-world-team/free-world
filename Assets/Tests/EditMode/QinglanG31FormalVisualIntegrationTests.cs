using System;
using Game.Core;
using Game.Editor;
using Game.Infrastructure;
using Game.Presentation;
using Game.Simulation;
using Game.UI;
using NUnit.Framework;
using UnityEditor;
using UnityEditor.AddressableAssets;
using UnityEngine;
using Object = UnityEngine.Object;

namespace Game.Tests.EditMode
{
    public sealed class QinglanG31FormalVisualIntegrationTests
    {
        private GameObject root;

        [TearDown]
        public void TearDown()
        {
            if (root != null) Object.DestroyImmediate(root);
        }

        [Test]
        public void CatalogMapsEveryApprovedVisualAddressAndRequiredRuntimeAliases()
        {
            var catalog = LoadCatalog();
            var settings = AddressableAssetSettingsDefaultObject.GetSettings(false);
            var group = settings.FindGroup(AssetProvenanceValidator.QinglanVisualGroup);
            var releaseCount = 0;
            foreach (var entry in group.entries)
            {
                if (!entry.labels.Contains(AssetProvenanceValidator.VisualReleaseLabel)) continue;
                releaseCount++;
                Assert.That(catalog.ContainsAddress(entry.address), Is.True, entry.address);
            }

            Assert.That(releaseCount, Is.EqualTo(181));
            Assert.That(catalog.EntityProfileCount, Is.EqualTo(34));
            Assert.That(catalog.BindingCount, Is.EqualTo(199));
            Assert.That(catalog.TryResolveSprite("ui.page.title", out _), Is.True);
            Assert.That(catalog.TryResolveSprite("ui.page.choice", out _), Is.True);
            Assert.That(catalog.TryResolveSprite("ui.panel", out _), Is.True);
            Assert.That(catalog.TryResolveSprite("ui.focus", out _), Is.True);
            Assert.That(catalog.TryResolveSprite("ui.cursor.pointer", out _), Is.True);
            Assert.That(catalog.TryResolveSprite("qinglan.status.burning", out _), Is.True);
        }

        [Test]
        public void FormalProfilesCoverPlayerEnemyBossSkillPickupAndAffixKinds()
        {
            var catalog = LoadCatalog();
            AssertProfile(catalog, "qinglan.character.lu_qingye", EntityKind.Actor);
            AssertProfile(catalog, "qinglan.enemy.grass_spirit", EntityKind.Actor);
            AssertProfile(catalog, "qinglan.enemy.boss.tingfeng", EntityKind.Actor);
            AssertProfile(catalog, "qinglan.presentation.skill.yufeng_sword", EntityKind.Projectile);
            AssertProfile(catalog, "qinglan.presentation.skill.zhenyue_seal", EntityKind.Area);
            AssertProfile(catalog, "qinglan.pickup.greenwood_dew", EntityKind.Pickup);
            AssertProfile(catalog, "qinglan.affix.rampaging", EntityKind.Actor);
        }

        [Test]
        public void UiConsumesFormalBackgroundChromeAndFocusWithoutOwningAddressableHandles()
        {
            var catalog = LoadCatalog();
            root = new GameObject("G31FormalUi");
            var ui = root.AddComponent<QinglanRuntimeUiRoot>();
            ui.Initialize(new EchoLocalization(), _ => string.Empty, new CatalogAdapter(catalog));
            var page = new QinglanPageViewModel();
            page.Reset(QinglanUiPageId.CharacterSelect, "ui.qinglan.character_select.title");
            page.Add(new QinglanUiOption("character", "content.qinglan.character.name", "",
                QinglanUiCommand.Continue));
            ui.ShowPage(page);

            Assert.That(ui.FormalBackgroundApplied, Is.True);
            Assert.That(ui.FormalVisualMissCount, Is.Zero);
            Assert.That(root.transform.Find("Qinglan_FormalBackground").GetComponent<UnityEngine.UI.Image>().sprite,
                Is.Not.Null);
            Assert.That(root.transform.Find("Qinglan_FormalFocus").GetComponent<UnityEngine.UI.Image>().sprite,
                Is.Not.Null);
        }

        [Test]
        public void AddressableLoaderOwnsAndReleasesTheFormalCatalogHandle()
        {
            var loader = new QinglanFormalVisualLoader();
            try
            {
                Assert.That(loader.LoadForStartup(), Is.True, loader.LastError);
                Assert.That(loader.IsLoaded, Is.True);
                Assert.That(loader.Catalog.EntityProfileCount, Is.EqualTo(34));
                Assert.That(loader.DirectionalSprites.Count, Is.EqualTo(9));
                Assert.That(loader.TryResolveSprite("ui.page.title", out var sprite), Is.True);
                Assert.That(sprite, Is.Not.Null);
            }
            finally
            {
                loader.Dispose();
            }
            Assert.That(loader.IsLoaded, Is.False);
        }

        private static FormalVisualCatalog LoadCatalog()
        {
            var catalog = AssetDatabase.LoadAssetAtPath<FormalVisualCatalog>(
                QinglanG31FormalVisualIntegration.CatalogPath);
            Assert.That(catalog, Is.Not.Null);
            Assert.That(catalog.SchemaVersion, Is.EqualTo(FormalVisualCatalog.CurrentSchemaVersion));
            return catalog;
        }

        private static void AssertProfile(FormalVisualCatalog catalog, string value, EntityKind kind)
        {
            var id = ContentId.Create(value);
            Assert.That(id.IsSuccess, Is.True, value);
            Assert.That(catalog.TryResolveProfile(id.Value, kind, out var profile), Is.True, value);
            Assert.That(profile.Sprite, Is.Not.Null, value);
        }

        private sealed class CatalogAdapter : IQinglanUiVisualCatalog
        {
            private readonly FormalVisualCatalog catalog;
            public CatalogAdapter(FormalVisualCatalog value) => catalog = value;
            public bool TryResolveSprite(string stableKey, out Sprite sprite) =>
                catalog.TryResolveSprite(stableKey, out sprite);
        }

        private sealed class EchoLocalization : ILocalizationService
        {
            public string SelectedLocaleCode => "en";
            public string Resolve(string localizationKey) => localizationKey;
            public bool SelectLocale(string localeCode) => true;
            public bool SelectNextLocale() => true;
        }
    }
}
