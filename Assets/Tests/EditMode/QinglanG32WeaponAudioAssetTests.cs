using NUnit.Framework;

namespace Game.Tests.EditMode
{
    public sealed class QinglanG32WeaponAudioAssetTests
    {
        private const string Root = "Assets/GameAssets/AI/QinglanDemo/AUDIO-WEAPON-001";

        private static readonly QinglanAudioCueExpectation[] Cues =
        {
            Cue("yufeng-sword-cast", 0.42f), Cue("yellow-talisman-cast", 0.58f),
            Cue("lihuo-wheel-cast", 0.64f), Cue("tide-orb-cast", 0.72f),
            Cue("zhenyue-seal-cast", 0.88f), Cue("spirit-vine-seed-cast", 0.54f),
            Cue("qinglan-flowing-shadow-sword", 0.96f), Cue("taiyi-spirit-sealing-array", 1.24f),
            Cue("chilu-hundred-craft-wheel", 1.08f), Cue("mirror-sea-tide-wheel", 1.34f),
            Cue("mountain-boundary-seal", 1.42f), Cue("earth-vein-spring-branch", 1.16f),
            Cue("yufeng-return", 0.38f), Cue("talisman-detonation", 0.74f),
            Cue("lihuo-return-explosion", 0.82f), Cue("tide-phase-shift", 1.02f),
            Cue("zhenyue-countershock", 0.86f), Cue("vine-propagation", 0.92f)
        };

        [Test]
        public void EighteenWeaponCuesMeetRuntimeClipPolicy()
        {
            Assert.That(Cues.Length, Is.EqualTo(18));
            QinglanG32AudioAssetTestUtility.AssertClipPolicy(Root, Cues, false);
        }

        [Test]
        public void WeaponCuesUseCanonicalReleaseAddresses() =>
            QinglanG32AudioAssetTestUtility.AssertAddressableRoute(Root, Cues, false);

        [Test]
        public void WeaponCueProvenancePassesAndMastersAreIsolated() =>
            QinglanG32AudioAssetTestUtility.AssertProvenanceAndSourceIsolation(Root, Cues, false);

        private static QinglanAudioCueExpectation Cue(string name, float duration) =>
            new QinglanAudioCueExpectation(name, "qinglan/audio/weapon/" + name, duration);
    }
}
