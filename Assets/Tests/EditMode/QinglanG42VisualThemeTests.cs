using Game.Application;
using Game.Presentation;
using Game.UI;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.UI;

namespace Game.Tests.EditMode
{
    public sealed class QinglanG42VisualThemeTests
    {
        [Test]
        public void UiAndPresentationPalettesMatchTheStyleBible()
        {
            AssertToken(QinglanUiTheme.Ink950, QinglanPresentationTheme.Ink950, 16, 42, 45);
            AssertToken(QinglanUiTheme.Ink800, QinglanPresentationTheme.Ink800, 28, 66, 67);
            AssertToken(QinglanUiTheme.Jade500, QinglanPresentationTheme.Jade500, 66, 184, 173);
            AssertToken(QinglanUiTheme.Jade200, QinglanPresentationTheme.Jade200, 169, 229, 216);
            AssertToken(QinglanUiTheme.Rice100, QinglanPresentationTheme.Rice100, 242, 235, 216);
            AssertToken(QinglanUiTheme.Gold400, QinglanPresentationTheme.Gold400, 215, 181, 90);
            AssertToken(QinglanUiTheme.Cinnabar500, QinglanPresentationTheme.Cinnabar500, 228, 87, 61);
            AssertToken(QinglanUiTheme.Cinnabar300, QinglanPresentationTheme.Cinnabar300, 255, 139, 98);
            AssertToken(QinglanUiTheme.Void700, QinglanPresentationTheme.Void700, 86, 60, 118);
            Assert.That(QinglanUiTheme.MinimumBodyFontSize1080p, Is.GreaterThanOrEqualTo(18));
        }

        [Test]
        public void RuntimeUiUsesThreePrimaryChoiceCardsAndKeepsDangerLegendOutOfCombat()
        {
            var root = new GameObject("G42UiLayout");
            try
            {
                var ui = root.AddComponent<QinglanRuntimeUiRoot>();
                ui.Initialize(new EchoLocalization(), id => "content." + id + ".name");
                var page = new QinglanPageViewModel(5);
                page.Reset(QinglanUiPageId.LevelUpChoice, "ui.qinglan.level_up.title");
                for (var index = 0; index < 3; index++)
                    page.Add(new QinglanUiOption(
                        "choice." + index,
                        "content.choice." + index + ".name",
                        "content.choice." + index + ".description",
                        QinglanUiCommand.SelectUpgrade,
                        true,
                        "150%",
                        "ui.qinglan.card.tag.skill"));
                page.Add(new QinglanUiOption("reroll", "ui.qinglan.level_up.reroll", "",
                    QinglanUiCommand.RerollUpgrade));
                page.Add(new QinglanUiOption("skip", "ui.qinglan.level_up.skip", "",
                    QinglanUiCommand.SkipUpgrade));
                ui.ShowPage(page);

                Assert.That(ui.UsesChoiceCardLayout, Is.True);
                Assert.That(ui.PermanentDangerLegendVisible, Is.False);
                for (var index = 0; index < 3; index++)
                {
                    var card = root.transform.Find(
                        "Qinglan_PageLayer/Qinglan_OptionViewport/Qinglan_OptionCards/Qinglan_OptionCard_" + index);
                    Assert.That(card, Is.Not.Null);
                    Assert.That(card.GetComponent<LayoutElement>().ignoreLayout, Is.True);
                    var rect = (RectTransform)card;
                    Assert.That(rect.anchorMin.y, Is.GreaterThanOrEqualTo(0.24f));
                    Assert.That(rect.anchorMax.y, Is.EqualTo(0.97f).Within(0.001f));
                }

                page.Reset(QinglanUiPageId.Settings, "ui.qinglan.settings.title");
                page.Add(new QinglanUiOption("font_scale", "ui.qinglan.settings.font_scale", "",
                    QinglanUiCommand.CycleSetting, true, "150%"));
                ui.ShowPage(page);
                var settings = new AccessibilitySettings();
                settings.SetFontScale(1.5f);
                settings.SetColorVision(ColorVisionMode.HighContrast);
                ui.ApplyAccessibility(settings);
                Assert.That(ui.UsesChoiceCardLayout, Is.False);
                Assert.That(ui.SettingsPreviewVisible, Is.True);
                Assert.That(ui.PermanentDangerLegendVisible, Is.False);
            }
            finally
            {
                Object.DestroyImmediate(root);
            }
        }

        private static void AssertToken(
            Color32 ui,
            Color32 presentation,
            byte red,
            byte green,
            byte blue)
        {
            Assert.That(ui, Is.EqualTo(new Color32(red, green, blue, 255)));
            Assert.That(presentation, Is.EqualTo(ui));
        }

        private sealed class EchoLocalization : ILocalizationService
        {
            public string SelectedLocaleCode => "en";
            public string Resolve(string localizationKey) => "loc:" + localizationKey;
            public bool SelectLocale(string localeCode) => true;
            public bool SelectNextLocale() => true;
        }
    }
}
