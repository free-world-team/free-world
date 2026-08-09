using Game.Editor;
using Game.Infrastructure;
using Game.Presentation;
using NUnit.Framework;
using UnityEditor;
using UnityEditor.AddressableAssets;
using UnityEngine;
using Object = UnityEngine.Object;

namespace Game.Tests.EditMode
{
    public sealed class QinglanG32FormalAudioIntegrationTests
    {
        private GameObject root;

        [TearDown]
        public void TearDown()
        {
            if (root != null) Object.DestroyImmediate(root);
        }

        [Test]
        public void CatalogMapsAllReleaseClipsCuesBossStemsAndMixerSnapshots()
        {
            var catalog = LoadCatalog();
            var settings = AddressableAssetSettingsDefaultObject.GetSettings(false);
            var group = settings.FindGroup(AssetProvenanceValidator.QinglanAudioGroup);
            var releaseCount = 0;
            foreach (var entry in group.entries)
            {
                if (!entry.labels.Contains(AssetProvenanceValidator.AudioReleaseLabel)) continue;
                releaseCount++;
                Assert.That(catalog.ContainsAddress(entry.address), Is.True, entry.address);
            }

            Assert.That(releaseCount, Is.EqualTo(QinglanG32FormalAudioIntegration.ExpectedReleaseClipCount));
            Assert.That(catalog.BindingCount, Is.EqualTo(releaseCount));
            Assert.That(catalog.SnapshotCount, Is.EqualTo(4));
            Assert.That(catalog.OutputGroup, Is.Not.Null);
            for (var cue = PresentationAudioCue.Hit; cue <= PresentationAudioCue.UiPauseToggle; cue++)
            {
                Assert.That(catalog.TryResolveCue(cue, out var clip), Is.True, cue.ToString());
                Assert.That(clip, Is.Not.Null, cue.ToString());
            }
            AssertBoss(catalog, "qinglan.enemy.boss.zhezhi");
            AssertBoss(catalog, "qinglan.enemy.boss.tingfeng");
        }

        [Test]
        public void AddressableLoaderOwnsAndReleasesFormalAudioCatalogHandle()
        {
            var loader = new QinglanFormalAudioLoader();
            try
            {
                Assert.That(loader.LoadForStartup(), Is.True, loader.LastError);
                Assert.That(loader.IsLoaded, Is.True);
                Assert.That(loader.Catalog.BindingCount,
                    Is.EqualTo(QinglanG32FormalAudioIntegration.ExpectedReleaseClipCount));
            }
            finally
            {
                loader.Dispose();
            }
            Assert.That(loader.IsLoaded, Is.False);
        }

        [Test]
        public void ProductionRouterUsesFormalClipsAndFixedSourceBudgets()
        {
            root = new GameObject("G32FormalAudioRouter");
            var router = new AudioRequestRouter(root.transform, LoadCatalog());
            try
            {
                Assert.That(router.FormalCatalogLoaded, Is.True);
                Assert.That(router.UsingTestToneFallback, Is.False);
                Assert.That(router.SourceCapacity, Is.EqualTo(32));
                Assert.That(router.StemCapacity, Is.EqualTo(8));
                Assert.That(router.ReservedCriticalCapacity, Is.EqualTo(8));
                Assert.That(router.ConfiguredStemCount, Is.EqualTo(8));
                Assert.That(router.Route(PresentationAudioCue.Confirm, PresentationPriority.Mechanic, 1f), Is.True);
                Assert.That(router.TryGetActiveCueVolume(PresentationAudioCue.Confirm, out var volume), Is.True);
                Assert.That(volume, Is.GreaterThan(0f));
            }
            finally
            {
                router.Dispose();
            }
        }

        [Test]
        public void RouterAppliesExactDuckStorySnapshotPressureAndBossPhaseStem()
        {
            root = new GameObject("G32FormalMixRouter");
            var router = new AudioRequestRouter(root.transform, LoadCatalog());
            try
            {
                router.SetMix(1f, 1f, 1f, 1f, PresentationMixState.Gameplay);
                Assert.That(router.Route(PresentationAudioCue.Hit, PresentationPriority.Combat, 1f), Is.True);
                Assert.That(router.TryGetActiveCueVolume(PresentationAudioCue.Hit, out var normal), Is.True);
                Assert.That(normal, Is.EqualTo(1f).Within(0.0001f));
                Assert.That(router.Route(PresentationAudioCue.Danger,
                    PresentationPriority.CriticalDanger, 1f), Is.True);
                Assert.That(router.TryGetActiveCueVolume(PresentationAudioCue.Hit, out var ducked), Is.True);
                Assert.That(ducked, Is.EqualTo(AudioRequestRouter.OrdinaryDuckLinear).Within(0.0001f));

                Assert.That(router.TryGetStemVolume(0, out var gameplayAmbience), Is.True);
                router.SetMix(1f, 1f, 1f, 1f, PresentationMixState.Story);
                Assert.That(router.TryGetStemVolume(0, out var storyAmbience), Is.True);
                Assert.That(storyAmbience / gameplayAmbience,
                    Is.EqualTo(AudioRequestRouter.StoryEnvironmentDuckLinear).Within(0.0001f));

                router.SetStemState(630d, false, string.Empty, 0);
                router.SetMix(1f, 1f, 1f, 1f, PresentationMixState.Gameplay);
                Assert.That(router.HighPressureStemActive, Is.True);
                Assert.That(router.TryGetStemVolume(4, out var pressure), Is.True);
                Assert.That(pressure, Is.GreaterThan(0f));

                router.SetStemState(700d, true, "qinglan.enemy.boss.zhezhi", 1);
                router.SetMix(1f, 1f, 1f, 1f, PresentationMixState.Boss);
                Assert.That(router.CurrentBossPhase, Is.EqualTo(1));
                Assert.That(router.TryGetStemVolume(6, out var phaseTwo), Is.True);
                Assert.That(phaseTwo, Is.GreaterThan(0f));
                Assert.That(router.TryGetStemVolume(5, out var phaseOne), Is.True);
                Assert.That(phaseOne, Is.Zero);
                Assert.That(router.SnapshotTransitionCount, Is.GreaterThanOrEqualTo(3));
            }
            finally
            {
                router.Dispose();
            }
        }

        [Test]
        public void OrdinaryCueCooldownIsInsideFortyToOneHundredTwentyMilliseconds()
        {
            root = new GameObject("G32CooldownRouter");
            var router = new AudioRequestRouter(root.transform, LoadCatalog());
            try
            {
                Assert.That(router.Route(PresentationAudioCue.Hit, PresentationPriority.Combat, 1f), Is.True);
                router.Tick(0.039f);
                Assert.That(router.Route(PresentationAudioCue.Hit, PresentationPriority.Combat, 1f), Is.False);
                router.Tick(0.002f);
                Assert.That(router.Route(PresentationAudioCue.Hit, PresentationPriority.Combat, 1f), Is.True);
            }
            finally
            {
                router.Dispose();
            }
        }

        private static FormalAudioCatalog LoadCatalog()
        {
            var catalog = AssetDatabase.LoadAssetAtPath<FormalAudioCatalog>(
                QinglanG32FormalAudioIntegration.CatalogPath);
            Assert.That(catalog, Is.Not.Null);
            Assert.That(catalog.SchemaVersion, Is.EqualTo(FormalAudioCatalog.CurrentSchemaVersion));
            return catalog;
        }

        private static void AssertBoss(FormalAudioCatalog catalog, string boss)
        {
            for (var phase = 0; phase < 3; phase++)
            {
                Assert.That(catalog.TryResolveBossStem(boss, phase, out var clip), Is.True, boss + " " + phase);
                Assert.That(clip, Is.Not.Null, boss + " " + phase);
            }
        }
    }
}
