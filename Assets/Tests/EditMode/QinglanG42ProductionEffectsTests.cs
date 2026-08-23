using System.Collections.Generic;
using Game.Core;
using Game.Presentation;
using NUnit.Framework;
using UnityEngine;
using Object = UnityEngine.Object;

namespace Game.Tests.EditMode
{
    public sealed class QinglanG42ProductionEffectsTests
    {
        private GameObject root;
        private Texture2D texture;
        private Sprite sprite;

        [TearDown]
        public void TearDown()
        {
            if (root != null) Object.DestroyImmediate(root);
            if (sprite != null) Object.DestroyImmediate(sprite);
            if (texture != null) Object.DestroyImmediate(texture);
        }

        [Test]
        public void MajorSkillsHaveDistinctStableNonColorVfxSignatures()
        {
            var ids = new[]
            {
                Id("qinglan.presentation.skill.yufeng_sword"),
                Id("qinglan.presentation.skill.yellow_talisman"),
                Id("qinglan.presentation.skill.lihuo_wheel"),
                Id("qinglan.presentation.skill.tide_orb"),
                Id("qinglan.presentation.skill.zhenyue_seal"),
                Id("qinglan.presentation.skill.spirit_vine_seed")
            };
            var signatures = new HashSet<int>();
            for (var index = 0; index < ids.Length; index++)
                Assert.That(
                    signatures.Add(StagedPresentationEffectSequencer.BuildNonColorSignature(ids[index])),
                    Is.True,
                    $"major skill {ids[index].Value} reused another non-colour VFX signature");
        }

        [Test]
        public void SequencerEmitsAllFiveStagesAndKeepsReducedMotionSemanticAlternative()
        {
            root = new GameObject("G42EStagedVfx");
            texture = new Texture2D(1, 1);
            sprite = Sprite.Create(texture, new Rect(0f, 0f, 1f, 1f), Vector2.one * 0.5f);
            var pool = new VfxRequestPool(root.transform, sprite, 16);
            var sequencer = new StagedPresentationEffectSequencer(pool, 4);
            var style = new ProceduralPresentationStyle(
                ProceduralShape.Diamond,
                new Color(0.2f, 0.9f, 0.82f, 1f),
                Color.white,
                Vector2.one,
                PresentationPriority.Mechanic,
                PresentationAudioCue.Hit,
                false,
                true);

            Assert.That(sequencer.TryBegin(
                Id("qinglan.presentation.skill.yufeng_sword"),
                Vector2.zero,
                style,
                1f,
                0f,
                null,
                false), Is.True);
            sequencer.Tick(1f, false);

            Assert.That(sequencer.CompletedSequenceCount, Is.EqualTo(1));
            for (var stage = 0; stage < StagedPresentationEffectSequencer.StageCount; stage++)
                Assert.That(
                    sequencer.GetStageSpawnCount((PresentationVfxStage)stage),
                    Is.EqualTo(1),
                    ((PresentationVfxStage)stage).ToString());

            Assert.That(sequencer.TryBegin(
                Id("qinglan.presentation.skill.tide_orb"),
                Vector2.one,
                style,
                1f,
                0f,
                null,
                true), Is.True);
            sequencer.Tick(1f, true);

            Assert.That(sequencer.CompletedSequenceCount, Is.EqualTo(2));
            Assert.That(sequencer.ReducedMotionStageSpawnCount, Is.EqualTo(5));
            Assert.That(sequencer.DroppedSequenceCount, Is.Zero);
            pool.Dispose();
        }

        [Test]
        public void CriticalStageSequenceEvictsDecorationThenMergesWithoutDrop()
        {
            root = new GameObject("G42ECriticalSequence");
            texture = new Texture2D(1, 1);
            sprite = Sprite.Create(texture, new Rect(0f, 0f, 1f, 1f), Vector2.one * 0.5f);
            var pool = new VfxRequestPool(root.transform, sprite, 8);
            var sequencer = new StagedPresentationEffectSequencer(pool, 1);
            var decoration = new ProceduralPresentationStyle(
                ProceduralShape.Circle, Color.gray, Color.black, Vector2.one,
                PresentationPriority.Decoration, PresentationAudioCue.None, false);
            var danger = new ProceduralPresentationStyle(
                ProceduralShape.Line, Color.red, Color.black, Vector2.one,
                PresentationPriority.CriticalDanger, PresentationAudioCue.Danger, true, true);

            Assert.That(sequencer.TryBegin(Id("test.presentation.decoration"), Vector2.zero,
                decoration, 1f, 0f, null, false), Is.True);
            Assert.That(sequencer.TryBegin(Id("test.presentation.danger.one"), Vector2.one,
                danger, 1f, 0f, null, false), Is.True);
            Assert.That(sequencer.TryBegin(Id("test.presentation.danger.two"), Vector2.up,
                danger, 1f, 0f, null, false), Is.True);

            Assert.That(sequencer.EvictedLowerPrioritySequenceCount, Is.EqualTo(1));
            Assert.That(sequencer.MergedCriticalSequenceCount, Is.EqualTo(1));
            Assert.That(sequencer.GetDroppedCount(PresentationPriority.CriticalDanger), Is.Zero);
            Assert.That(sequencer.DroppedSequenceCount, Is.Zero);
            pool.Dispose();
        }

        private static ContentId Id(string value) => ContentId.Create(value).Value;
    }
}
