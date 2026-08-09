using System.Collections.Generic;
using NUnit.Framework;

namespace Game.Tests.EditMode
{
    public sealed class QinglanG32EnemyAudioAssetTests
    {
        private const string Root = "Assets/GameAssets/AI/QinglanDemo/AUDIO-ENEMY-001";
        private static readonly string[] Identities =
        {
            "grass-spirit", "paper-crane", "wooden-puppet", "stone-lantern",
            "wind-bell-spirit", "explosive-seed", "zhezhi", "tingfeng"
        };
        private static readonly QinglanAudioCueExpectation[] Cues = BuildCues();

        [Test]
        public void TwentyFourEnemyAndBossCuesMeetRuntimeClipPolicy()
        {
            Assert.That(Cues.Length, Is.EqualTo(24));
            QinglanG32AudioAssetTestUtility.AssertClipPolicy(Root, Cues, false);
        }

        [Test]
        public void EnemyCuesUseCanonicalReleaseAddresses() =>
            QinglanG32AudioAssetTestUtility.AssertAddressableRoute(Root, Cues, false);

        [Test]
        public void EnemyCueProvenancePassesAndMastersAreIsolated() =>
            QinglanG32AudioAssetTestUtility.AssertProvenanceAndSourceIsolation(Root, Cues, false);

        private static QinglanAudioCueExpectation[] BuildCues()
        {
            var cues = new List<QinglanAudioCueExpectation>(24);
            for (var index = 0; index < Identities.Length; index++)
            {
                var identity = Identities[index];
                var boss = index >= 6;
                cues.Add(Cue(identity, "spawn", boss ? 1.35f : 0.62f));
                cues.Add(Cue(identity, "attack", boss ? 0.82f : 0.38f));
                cues.Add(Cue(identity, "death", boss ? 1.8f : 0.74f));
            }
            return cues.ToArray();
        }

        private static QinglanAudioCueExpectation Cue(string identity, string action, float duration) =>
            new QinglanAudioCueExpectation(
                identity + "-" + action,
                "qinglan/audio/enemy/" + identity + "/" + action,
                duration);
    }
}
