using Game.Presentation;
using Game.UI;
using NUnit.Framework;
using UnityEngine;

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
    }
}
