using NUnit.Framework;

namespace Game.Tests.EditMode
{
    public sealed class QinglanG32MapAudioAssetTests
    {
        private const string Root = "Assets/GameAssets/AI/QinglanDemo/AUDIO-MAP-001";
        private static readonly QinglanAudioCueExpectation[] Cues =
        {
            Cue("objective-listen", "objective/listen", 0.82f),
            Cue("objective-guide", "objective/guide", 0.88f),
            Cue("objective-stop-balance", "objective/stop-balance", 0.94f),
            Cue("event-wind-vein-riot", "event/wind-vein-riot", 1.42f),
            Cue("event-herb-garden-revival", "event/herb-garden-revival", 1.28f),
            Cue("event-old-sword-resonance", "event/old-sword-resonance", 1.54f),
            Cue("landmark-wind-vein-stele", "landmark/wind-vein-stele", 0.76f),
            Cue("landmark-sealed-sword-cache", "landmark/sealed-sword-cache", 0.84f),
            Cue("landmark-herb-garden-variant", "landmark/herb-garden-variant", 0.72f),
            Cue("landmark-broken-wall-sword-mark", "landmark/broken-wall-sword-mark", 0.8f),
            Cue("landmark-guest-pavilion-letter", "landmark/guest-pavilion-letter", 0.68f),
            Cue("map-boundary-warning", "system/boundary-warning", 0.46f),
            Cue("objective-complete", "system/objective-complete", 1.12f),
            Cue("landmark-claim", "system/landmark-claim", 0.92f)
        };

        [Test]
        public void FourteenMapCuesMeetRuntimeClipPolicy()
        {
            Assert.That(Cues.Length, Is.EqualTo(14));
            QinglanG32AudioAssetTestUtility.AssertClipPolicy(Root, Cues, false);
        }

        [Test]
        public void MapCuesUseCanonicalReleaseAddresses() =>
            QinglanG32AudioAssetTestUtility.AssertAddressableRoute(Root, Cues, false);

        [Test]
        public void MapCueProvenancePassesAndMastersAreIsolated() =>
            QinglanG32AudioAssetTestUtility.AssertProvenanceAndSourceIsolation(Root, Cues, false);

        private static QinglanAudioCueExpectation Cue(string name, string suffix, float duration) =>
            new QinglanAudioCueExpectation(name, "qinglan/audio/map/" + suffix, duration);
    }
}
