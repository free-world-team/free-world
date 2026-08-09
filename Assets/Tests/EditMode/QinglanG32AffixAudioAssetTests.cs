using NUnit.Framework;

namespace Game.Tests.EditMode
{
    public sealed class QinglanG32AffixAudioAssetTests
    {
        private const string Root = "Assets/GameAssets/AI/QinglanDemo/AUDIO-AFFIX-001";
        private static readonly QinglanAudioCueExpectation[] Cues =
        {
            Cue("rampaging-activate", 0.62f), Cue("rampaging-pulse", 0.34f),
            Cue("barrier-activate", 0.86f), Cue("barrier-pulse", 0.58f),
            Cue("splitting-activate", 0.74f), Cue("splitting-pulse", 0.46f),
            Cue("quaking-activate", 0.92f), Cue("quaking-pulse", 0.54f)
        };

        [Test]
        public void EightAffixCuesMeetRuntimeClipPolicy()
        {
            Assert.That(Cues.Length, Is.EqualTo(8));
            QinglanG32AudioAssetTestUtility.AssertClipPolicy(Root, Cues, false);
        }

        [Test]
        public void AffixCuesUseCanonicalReleaseAddresses() =>
            QinglanG32AudioAssetTestUtility.AssertAddressableRoute(Root, Cues, false);

        [Test]
        public void AffixCueProvenancePassesAndMastersAreIsolated() =>
            QinglanG32AudioAssetTestUtility.AssertProvenanceAndSourceIsolation(Root, Cues, false);

        private static QinglanAudioCueExpectation Cue(string name, float duration)
        {
            var split = name.IndexOf('-');
            var address = "qinglan/audio/affix/" + name.Substring(0, split) + "/" + name.Substring(split + 1);
            return new QinglanAudioCueExpectation(name, address, duration);
        }
    }
}
