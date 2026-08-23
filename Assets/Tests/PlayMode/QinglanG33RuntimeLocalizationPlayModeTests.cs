using System.Collections;
using System.Linq;
using Game.Application;
using Game.Infrastructure;
using Game.UI;
using NUnit.Framework;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using UnityEngine.UI;
using Object = UnityEngine.Object;

namespace Game.Tests.PlayMode
{
    public sealed class QinglanG33RuntimeLocalizationPlayModeTests
    {
        [UnitySetUp]
        public IEnumerator SetUp()
        {
            DestroyBootstrapInstances();
            yield return null;
            var operation = SceneManager.LoadSceneAsync("Bootstrap", LoadSceneMode.Single);
            while (!operation.isDone) yield return null;
            yield return null;
        }

        [UnityTearDown]
        public IEnumerator TearDown()
        {
            DestroyBootstrapInstances();
            yield return null;
        }

        [UnityTest]
        public IEnumerator BootstrapUsesFormalTmpFontsAndResolvesAllThreeTablesInEveryLocale()
        {
            var host = Object.FindFirstObjectByType<QinglanDemoRuntimeHost>();
            Assert.That(host, Is.Not.Null);
            Assert.That(host.FormalFontsLoaded, Is.True);
            Assert.That(host.Ui.UsesFormalTmpFonts, Is.True);
            var tmpTexts = host.Ui.GetComponentsInChildren<TMP_Text>(true);
            Assert.That(tmpTexts.Count(text => text.name.StartsWith("Qinglan_")), Is.EqualTo(3));
            Assert.That(tmpTexts.Count(text => text.name == "M7_DamageNumber"), Is.EqualTo(16));
            Assert.That(host.Ui.GetComponentsInChildren<Text>(true), Is.Empty);

            Assert.That(host.Localization.SelectLocale("en"), Is.True);
            Assert.That(host.Localization.Resolve("ui.qinglan.title.subtitle"),
                Is.EqualTo("The old court waits for the wind to return."));
            Assert.That(host.Localization.Resolve("content.qinglan.skill.weapon.yufeng_sword.name"), Is.EqualTo("Yufeng Sword"));
            Assert.That(host.Localization.Resolve("story.qinglan.story.lu_qingye.hearing_sword.01"),
                Does.StartWith("When the wind crossed"));

            Assert.That(host.Localization.SelectLocale("zh-Hans"), Is.True);
            Assert.That(host.Localization.Resolve("content.qinglan.skill.weapon.yufeng_sword.name"), Is.EqualTo("御风剑"));
            Assert.That(host.Localization.Resolve("story.qinglan.story.lu_qingye.hearing_sword.01"),
                Does.StartWith("风过残碑时"));

            Assert.That(host.Localization.SelectLocale("pseudo"), Is.True);
            Assert.That(host.Localization.Resolve("ui.qinglan.title.subtitle"), Does.StartWith("【"));
            Assert.That(host.Localization.Resolve("content.qinglan.skill.weapon.yufeng_sword.name"), Does.Contain("Ｙｕｆｅｎｇ"));
            Assert.That(host.Localization.Resolve("story.qinglan.story.lu_qingye.hearing_sword.01"), Does.StartWith("【"));
            yield return null;
        }

        [UnityTest]
        public IEnumerator FormalPagesRemainReadableAtSupportedLocalesAndOneHundredFiftyPercentScale()
        {
            var host = Object.FindFirstObjectByType<QinglanDemoRuntimeHost>();
            Assert.That(host, Is.Not.Null);
            var page = new QinglanPageViewModel(6);
            page.Reset(
                QinglanUiPageId.StoryOverlay,
                "content.qinglan.skill.weapon.yufeng_sword.name",
                "story.qinglan.story.lu_qingye.hearing_sword.01",
                "ui.qinglan.story.page",
                "1/8");
            page.Add(new QinglanUiOption("continue", "ui.common.continue",
                "story.qinglan.story.lu_qingye.hearing_sword.02", QinglanUiCommand.Continue));
            page.RestoreSelection(0);

            var settings = new AccessibilitySettings();
            settings.SetFontScale(1.5f);
            foreach (var locale in new[] { "en", "zh-Hans", "pseudo" })
            {
                Assert.That(host.Localization.SelectLocale(locale), Is.True, locale);
                host.Ui.ApplyAccessibility(settings);
                host.Ui.ShowPage(page);
                Canvas.ForceUpdateCanvases();
                yield return null;
                var overflowDiagnostic = string.Join(" | ", host.Ui.GetComponentsInChildren<TMP_Text>(true)
                    .Where(text => text.gameObject.activeInHierarchy)
                    .Select(text => text.name + ":rect=" + text.rectTransform.rect.height.ToString("0.0") +
                                    ",preferred=" + text.preferredHeight.ToString("0.0") +
                                    ",overflow=" + text.isTextOverflowing));
                Assert.That(host.Ui.HasAnyTextOverflow, Is.False,
                    locale + " overflowed at 150% font scale. " + overflowDiagnostic);
                Assert.That(host.Ui.SupportsCharacter('剑'), Is.True, locale);
            }

            var pageText = host.Ui.transform.Find("Qinglan_PageLayer/Qinglan_PageText")?.GetComponent<TMP_Text>();
            Assert.That(pageText, Is.Not.Null);
            Assert.That(pageText.font.name, Does.Contain("NotoSerifCJKsc-SemiBold"));
        }

        [UnityTest]
        public IEnumerator SettingsPageRemainsReadableAtTwoHundredPercentScale()
        {
            var host = Object.FindFirstObjectByType<QinglanDemoRuntimeHost>();
            Assert.That(host, Is.Not.Null);
            var page = new QinglanPageViewModel(20);
            page.Reset(QinglanUiPageId.Settings, "ui.qinglan.settings.title", "ui.qinglan.settings.description");
            var keys = new[]
            {
                "ui.settings.rebind", "ui.settings.language", "ui.settings.deadzone", "ui.settings.vibration",
                "ui.settings.screen_shake", "ui.settings.flash_intensity", "ui.settings.damage_numbers",
                "ui.settings.auto_aim", "ui.qinglan.settings.font_scale", "ui.qinglan.settings.color_vision",
                "ui.qinglan.settings.master_volume", "ui.qinglan.settings.music_volume",
                "ui.qinglan.settings.ambience_volume", "ui.qinglan.settings.effects_volume",
                "ui.qinglan.settings.subtitles", "ui.common.back"
            };
            for (var index = 0; index < keys.Length; index++)
                page.Add(new QinglanUiOption("setting." + index, keys[index], "", QinglanUiCommand.CycleSetting, true, "200%"));
            page.RestoreSelection(0);
            var settings = new AccessibilitySettings();
            settings.SetFontScale(2f);

            foreach (var locale in new[] { "en", "zh-Hans", "pseudo" })
            {
                Assert.That(host.Localization.SelectLocale(locale), Is.True, locale);
                host.Ui.ShowPage(page);
                host.Ui.ApplyAccessibility(settings);
                Canvas.ForceUpdateCanvases();
                yield return null;
                var overflowDiagnostic = string.Join(" | ", host.Ui.GetComponentsInChildren<TMP_Text>(true)
                    .Where(text => text.gameObject.activeInHierarchy)
                    .Select(text => text.name + ":rect=" + text.rectTransform.rect.height.ToString("0.0") +
                                    ",preferred=" + text.preferredHeight.ToString("0.0") +
                                    ",overflow=" + text.isTextOverflowing));
                Assert.That(host.Ui.HasAnyTextOverflow, Is.False,
                    locale + " overflowed at 200% font scale. " + overflowDiagnostic);
            }
        }
        private static void DestroyBootstrapInstances()
        {
            var instances = Object.FindObjectsByType<GameBootstrapper>(
                FindObjectsInactive.Include,
                FindObjectsSortMode.None);
            for (var index = 0; index < instances.Length; index++) Object.Destroy(instances[index].gameObject);
        }
    }
}
