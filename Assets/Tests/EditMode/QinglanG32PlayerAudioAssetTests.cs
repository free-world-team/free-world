using NUnit.Framework;

namespace Game.Tests.EditMode
{
    public sealed class QinglanG32PlayerAudioAssetTests
    {
        private const string Root = "Assets/GameAssets/AI/QinglanDemo/AUDIO-PLAYER-001";

        private static readonly QinglanAudioCueExpectation[] Cues =
        {
            Cue("dash-start", 0.34f),
            Cue("dash-ready", 0.28f),
            Cue("riding-wind-tier", 0.72f),
            Cue("sword-intent-gain", 0.24f),
            Cue("sword-intent-full", 0.68f),
            Cue("skill-ready", 0.42f),
            Cue("manifestation-ready", 0.92f),
            Cue("pickup", 0.22f),
            Cue("player-hit", 0.31f),
            Cue("low-health", 0.78f),
            Cue("level-up", 0.84f),
            Cue("player-defeat", 1.2f)
        };

        [Test]
        public void TwelvePlayerCuesMeetRuntimeClipPolicy()
        {
            Assert.That(Cues.Length, Is.EqualTo(12));
            QinglanG32AudioAssetTestUtility.AssertClipPolicy(Root, Cues, false);
        }

        [Test]
        public void PlayerCuesUseCanonicalReleaseAddresses() =>
            QinglanG32AudioAssetTestUtility.AssertAddressableRoute(Root, Cues, false);

        [Test]
        public void PlayerCueProvenancePassesAndMastersAreIsolated() =>
            QinglanG32AudioAssetTestUtility.AssertProvenanceAndSourceIsolation(Root, Cues, false);

        private static QinglanAudioCueExpectation Cue(string name, float duration) =>
            new QinglanAudioCueExpectation(name, "qinglan/audio/player/" + name, duration);
    }
}
