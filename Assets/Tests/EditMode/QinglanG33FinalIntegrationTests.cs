using System;
using System.IO;
using Game.Editor;
using Game.Infrastructure;
using Game.UI;
using NUnit.Framework;
using TMPro;
using UnityEditor;
using UnityEditor.Localization;
using UnityEngine.Localization;
using UnityEngine.Localization.Pseudo;
using UnityEngine.Localization.Tables;

namespace Game.Tests.EditMode
{
    public sealed class QinglanG33FinalIntegrationTests
    {
        [Test]
        public void StableKeyNamespacesRouteToTheirGovernedRuntimeTables()
        {
            Assert.That(UnityLocalizationService.TableForKey("ui.qinglan.title.subtitle"),
                Is.EqualTo(UnityLocalizationService.UiTableName));
            Assert.That(UnityLocalizationService.TableForKey("content.qinglan.skill.weapon.yufeng_sword.name"),
                Is.EqualTo(UnityLocalizationService.ContentTableName));
            Assert.That(UnityLocalizationService.TableForKey("story.qinglan.story.lu_qingye.hearing_sword.01"),
                Is.EqualTo(UnityLocalizationService.NarrativeTableName));
            Assert.That(UnityLocalizationService.TableForKey("collectible.qinglan.collectible.old_court.03.body"),
                Is.EqualTo(UnityLocalizationService.NarrativeTableName));
            Assert.That(UnityLocalizationService.TableForKey("narrative.qinglan.objective.wind_altar.listen.start"),
                Is.EqualTo(UnityLocalizationService.NarrativeTableName));
        }

        [Test]
        public void EveryFormalBilingualTableIsPreloaded()
        {
            AssertPreloaded(UnityLocalizationService.UiTableName);
            AssertPreloaded(UnityLocalizationService.ContentTableName);
            AssertPreloaded(UnityLocalizationService.NarrativeTableName);
        }

        [Test]
        public void FontSafePseudoLocaleTransformsTextAndUsesOnlyPrewarmedGlyphs()
        {
            var pseudoLocales = LocalizationEditorSettings.GetPseudoLocales();
            Assert.That(pseudoLocales, Has.Count.EqualTo(1));
            var pseudo = pseudoLocales[0];
            Assert.That(pseudo.Methods, Has.Count.EqualTo(4));
            var hasAccenter = false;
            var hasEncapsulator = false;
            for (var index = 0; index < pseudo.Methods.Count; index++)
            {
                if (pseudo.Methods[index] is Accenter) hasAccenter = true;
                if (pseudo.Methods[index] is Encapsulator) hasEncapsulator = true;
            }
            Assert.That(hasAccenter, Is.True);
            Assert.That(hasEncapsulator, Is.True);

            const string source = "Ride the wind!";
            var transformed = pseudo.GetPseudoString(source);
            Assert.That(transformed, Is.Not.EqualTo(source));
            Assert.That(transformed, Does.StartWith("【").And.EndWith("】"));
            Assert.That(transformed, Does.Contain("Ｒｉｄｅ"));

            var font = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(QinglanG33FontIntegration.SansRegularPath);
            Assert.That(font, Is.Not.Null);
            for (var index = 0; index < transformed.Length; index++)
                Assert.That(font.HasCharacter(transformed[index], false, false), Is.True,
                    "Pseudo glyph U+" + ((int)transformed[index]).ToString("X4") + " is not prewarmed.");
        }

        [Test]
        public void FormalTmpAssetsCoverAllGovernedTextAndRuntimeHasNoSystemFontFallback()
        {
            var sets = QinglanG33FinalIntegration.CollectCurrentGlyphSets();
            AssertFont(QinglanG33FontIntegration.SansRegularPath, sets.Regular);
            AssertFont(QinglanG33FontIntegration.SansBoldPath, sets.Bold);
            AssertFont(QinglanG33FontIntegration.SerifSemiBoldPath, sets.Narrative);

            var settings = AssetDatabase.LoadAssetAtPath<TMP_Settings>(QinglanG33FontIntegration.TmpSettingsPath);
            var regular = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(QinglanG33FontIntegration.SansRegularPath);
            Assert.That(settings, Is.Not.Null);
            Assert.That(TMP_Settings.defaultFontAsset, Is.SameAs(regular));
            Assert.That(QinglanFormalFontLoader.RegularAddress, Is.EqualTo(QinglanG33FontIntegration.SansRegularAddress));
            Assert.That(QinglanFormalFontLoader.BoldAddress, Is.EqualTo(QinglanG33FontIntegration.SansBoldAddress));
            Assert.That(QinglanFormalFontLoader.NarrativeAddress, Is.EqualTo(QinglanG33FontIntegration.SerifSemiBoldAddress));

            var source = File.ReadAllText("Assets/Game/UI/QinglanRuntimeUiRoot.cs") +
                         File.ReadAllText("Assets/Game/UI/RuntimeUiRoot.cs") +
                         File.ReadAllText("Assets/Game/Presentation/PresentationEffects.cs");
            Assert.That(source, Does.Not.Contain("CreateDynamicFontFromOSFont"));
            Assert.That(source, Does.Not.Contain("LegacyRuntime.ttf"));
            Assert.That(source, Does.Contain("TextMeshProUGUI"));
        }

        private static void AssertPreloaded(string collectionName)
        {
            var collection = LocalizationEditorSettings.GetStringTableCollection(collectionName);
            Assert.That(collection, Is.Not.Null, collectionName);
            var english = collection.GetTable(new LocaleIdentifier("en")) as StringTable;
            var chinese = collection.GetTable(new LocaleIdentifier("zh-Hans")) as StringTable;
            Assert.That(english, Is.Not.Null, collectionName + " en");
            Assert.That(chinese, Is.Not.Null, collectionName + " zh-Hans");
            Assert.That(LocalizationEditorSettings.GetPreloadTableFlag(english), Is.True, collectionName + " en");
            Assert.That(LocalizationEditorSettings.GetPreloadTableFlag(chinese), Is.True, collectionName + " zh-Hans");
        }

        private static void AssertFont(string path, string characters)
        {
            var font = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(path);
            Assert.That(font, Is.Not.Null, path);
            Assert.That(font.atlasTextures.Length, Is.GreaterThan(1), path + " must persist its prewarmed atlas pages.");
            for (var index = 0; index < characters.Length; index++)
                Assert.That(font.HasCharacter(characters[index], false, false), Is.True,
                    path + " is missing U+" + ((int)characters[index]).ToString("X4") + ".");
        }
    }
}
