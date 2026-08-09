using System.Collections;
using Game.Infrastructure;
using Game.Presentation;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using Object = UnityEngine.Object;

namespace Game.Tests.PlayMode
{
    public sealed class QinglanG32FormalAudioPlayModeTests
    {
        [UnityTest]
        public IEnumerator FormalAddressableAudioSchedulesStemsAndRoutesCriticalCueInPlayerLifecycle()
        {
            var loader = new QinglanFormalAudioLoader();
            var root = new GameObject("G32FormalAudioPlayMode");
            AudioRequestRouter router = null;
            try
            {
                Assert.That(loader.LoadForStartup(), Is.True, loader.LastError);
                router = new AudioRequestRouter(root.transform, loader.Catalog);
                Assert.That(router.SourceCapacity, Is.EqualTo(32));
                Assert.That(router.StemCapacity, Is.EqualTo(8));
                Assert.That(router.StemScheduleBatchCount, Is.EqualTo(1));
                Assert.That(root.GetComponentsInChildren<AudioSource>(true).Length, Is.EqualTo(16));
                Assert.That(router.Route(PresentationAudioCue.Danger,
                    PresentationPriority.CriticalDanger, 1f), Is.True);
                router.SetStemState(700d, true, "qinglan.enemy.boss.tingfeng", 2);
                router.SetMix(1f, 1f, 1f, 1f, PresentationMixState.Boss);
                router.Tick(0.016f);
                yield return null;
                Assert.That(router.ActiveCount, Is.EqualTo(1));
                Assert.That(router.CurrentBossPhase, Is.EqualTo(2));
                Assert.That(router.SnapshotTransitionCount, Is.GreaterThanOrEqualTo(2));
            }
            finally
            {
                router?.Dispose();
                loader.Dispose();
                Object.Destroy(root);
            }
            yield return null;
            Assert.That(loader.IsLoaded, Is.False);
        }
    }
}
