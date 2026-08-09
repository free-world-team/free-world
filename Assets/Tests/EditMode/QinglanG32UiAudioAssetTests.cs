using NUnit.Framework;

namespace Game.Tests.EditMode
{
    public sealed class QinglanG32UiAudioAssetTests
    {
        private const string Root = "Assets/GameAssets/AI/QinglanDemo/AUDIO-UI-001";
        private static readonly QinglanAudioCueExpectation[] Cues =
        {
            Cue("navigate", 0.08f),
            Cue("confirm", 0.18f),
            Cue("cancel", 0.16f),
            Cue("error", 0.28f),
            Cue("page-open", 0.32f),
            Cue("page-close", 0.26f),
            Cue("tab-change", 0.12f),
            Cue("choice-focus", 0.10f),
            Cue("choice-select", 0.22f),
            Cue("locked", 0.30f),
            Cue("notification", 0.42f),
            Cue("pause-toggle", 0.20f)
        };

        [Test]
        public void TwelveUiCuesMeetRuntimePcmPolicy()
        {
            Assert.That(Cues.Length, Is.EqualTo(12));
            QinglanG32AudioAssetTestUtility.AssertClipPolicy(Root, Cues, true);
        }

        [Test]
        public void UiCuesUseCanonicalReleaseAddresses() =>
            QinglanG32AudioAssetTestUtility.AssertAddressableRoute(Root, Cues, true);

        [Test]
        public void UiCueProvenancePassesAndMastersAreIsolated() =>
            QinglanG32AudioAssetTestUtility.AssertProvenanceAndSourceIsolation(Root, Cues, true);

        private static QinglanAudioCueExpectation Cue(string name, float duration) =>
            new QinglanAudioCueExpectation(name, "qinglan/audio/ui/" + name, duration);
    }
}
