using System;
using Game.Core;
using UnityEngine;

namespace Game.Presentation
{
    /// <summary>The production readability phases shared by every major skill.</summary>
    public enum PresentationVfxStage : byte
    {
        Anticipation = 0,
        Launch = 1,
        Travel = 2,
        Impact = 3,
        Residue = 4
    }

    /// <summary>
    /// Fixed-capacity, presentation-only sequencer. It emits into the shared VFX pool,
    /// owns no behaviours, and derives a non-colour signature from stable ContentId.
    /// </summary>
    public sealed class StagedPresentationEffectSequencer
    {
        private struct Sequence
        {
            public bool Active;
            public ContentId SourceId;
            public Vector2 Position;
            public ProceduralPresentationStyle BaseStyle;
            public Sprite FormalSprite;
            public float BaseSize;
            public float Rotation;
            public float Remaining;
            public PresentationVfxStage NextStage;
        }

        public const int StageCount = 5;
        public const int DefaultCapacity = 96;

        private readonly VfxRequestPool pool;
        private readonly Sequence[] sequences;
        private readonly long[] spawnedByStage = new long[StageCount];
        private readonly long[] droppedByPriority = new long[4];
        private int activeCount;

        public StagedPresentationEffectSequencer(VfxRequestPool targetPool, int capacity = DefaultCapacity)
        {
            pool = targetPool ?? throw new ArgumentNullException(nameof(targetPool));
            if (capacity <= 0) throw new ArgumentOutOfRangeException(nameof(capacity));
            sequences = new Sequence[capacity];
        }

        public int Capacity => sequences.Length;
        public int ActiveCount => activeCount;
        public long BegunSequenceCount { get; private set; }
        public long CompletedSequenceCount { get; private set; }
        public long DroppedSequenceCount { get; private set; }
        public long EvictedLowerPrioritySequenceCount { get; private set; }
        public long MergedCriticalSequenceCount { get; private set; }
        public long ReducedMotionStageSpawnCount { get; private set; }
        public long GetDroppedCount(PresentationPriority priority) => droppedByPriority[(int)priority];

        public long GetStageSpawnCount(PresentationVfxStage stage) =>
            spawnedByStage[(int)stage];

        /// <summary>A stable non-colour signature used to verify skill readability.</summary>
        public static int BuildNonColorSignature(ContentId sourceId)
        {
            var hash = sourceId.IsValid ? sourceId.StableHash : 0;
            var travel = PositiveModulo(hash, 3);
            var impact = PositiveModulo(hash / 7, 3);
            var residue = PositiveModulo(hash / 29, 3);
            var cadence = PositiveModulo(hash / 113, 4);
            return travel | (impact << 2) | (residue << 4) | (cadence << 6);
        }

        public bool TryBegin(
            ContentId sourceId,
            Vector2 position,
            in ProceduralPresentationStyle baseStyle,
            float baseSize,
            float rotationDegrees,
            Sprite formalSprite,
            bool reducedMotion)
        {
            var index = FindFree();
            var replacing = false;
            if (index < 0)
            {
                index = FindEvictionCandidate(baseStyle.Priority);
                if (index >= 0)
                {
                    EvictedLowerPrioritySequenceCount++;
                    replacing = true;
                }
                else if (baseStyle.Priority == PresentationPriority.CriticalDanger)
                {
                    index = FindCriticalMerge(sourceId);
                    MergedCriticalSequenceCount++;
                    replacing = true;
                }
                else
                {
                    DroppedSequenceCount++;
                    droppedByPriority[(int)baseStyle.Priority]++;
                    return false;
                }
            }

            sequences[index] = new Sequence
            {
                Active = true,
                SourceId = sourceId,
                Position = position,
                BaseStyle = baseStyle,
                FormalSprite = formalSprite,
                BaseSize = Mathf.Max(0.12f, baseSize),
                Rotation = rotationDegrees,
                Remaining = StageDelay(PresentationVfxStage.Anticipation, sourceId, reducedMotion),
                NextStage = PresentationVfxStage.Launch
            };
            if (!replacing) activeCount++;
            BegunSequenceCount++;
            Emit(in sequences[index], PresentationVfxStage.Anticipation, reducedMotion);
            return true;
        }

        public void Tick(float unscaledDeltaTime, bool reducedMotion)
        {
            var delta = Mathf.Max(0f, unscaledDeltaTime);
            for (var index = 0; index < sequences.Length; index++)
            {
                if (!sequences[index].Active) continue;
                var sequence = sequences[index];
                sequence.Remaining -= delta;
                while (sequence.Remaining <= 0f && sequence.Active)
                {
                    var stage = sequence.NextStage;
                    Emit(in sequence, stage, reducedMotion);
                    if (stage == PresentationVfxStage.Residue)
                    {
                        sequence.Active = false;
                        activeCount--;
                        CompletedSequenceCount++;
                        break;
                    }
                    sequence.NextStage = (PresentationVfxStage)((int)stage + 1);
                    sequence.Remaining += StageDelay(stage, sequence.SourceId, reducedMotion);
                }
                sequences[index] = sequence;
            }
        }

        public void Clear()
        {
            for (var index = 0; index < sequences.Length; index++) sequences[index].Active = false;
            activeCount = 0;
        }

        private int FindFree()
        {
            for (var index = 0; index < sequences.Length; index++)
                if (!sequences[index].Active) return index;
            return -1;
        }

        private int FindEvictionCandidate(PresentationPriority incomingPriority)
        {
            var candidate = -1;
            var weakest = incomingPriority;
            for (var index = 0; index < sequences.Length; index++)
            {
                if (!sequences[index].Active || sequences[index].BaseStyle.Priority <= weakest) continue;
                weakest = sequences[index].BaseStyle.Priority;
                candidate = index;
            }
            return candidate;
        }

        private int FindCriticalMerge(ContentId sourceId)
        {
            var fallback = 0;
            for (var index = 0; index < sequences.Length; index++)
            {
                if (!sequences[index].Active ||
                    sequences[index].BaseStyle.Priority != PresentationPriority.CriticalDanger) continue;
                fallback = index;
                if (sequences[index].SourceId == sourceId) return index;
            }
            return fallback;
        }

        private void Emit(in Sequence sequence, PresentationVfxStage stage, bool reducedMotion)
        {
            var signature = BuildNonColorSignature(sequence.SourceId);
            var style = StageStyle(sequence.BaseStyle, stage, signature, reducedMotion);
            var scale = StageScale(stage, signature, reducedMotion);
            var groundAligned = reducedMotion || stage == PresentationVfxStage.Anticipation ||
                                stage == PresentationVfxStage.Impact || stage == PresentationVfxStage.Residue;
            var duration = StageDuration(stage, reducedMotion);
            var rotation = sequence.Rotation + ((signature & 3) * 15f) + ((int)stage * 9f);
            var sprite = stage == PresentationVfxStage.Launch || stage == PresentationVfxStage.Travel
                ? sequence.FormalSprite
                : null;
            pool.TrySpawn(new ProceduralVfxRequest(
                sequence.Position,
                style,
                sequence.BaseSize * scale,
                duration,
                rotation,
                groundAligned), sprite);
            spawnedByStage[(int)stage]++;
            if (reducedMotion) ReducedMotionStageSpawnCount++;
        }

        private static ProceduralPresentationStyle StageStyle(
            in ProceduralPresentationStyle source,
            PresentationVfxStage stage,
            int signature,
            bool reducedMotion)
        {
            var color = source.Color;
            color.a = Mathf.Min(color.a, reducedMotion ? 0.58f : Alpha(stage));
            return new ProceduralPresentationStyle(
                ResolveShape(source.Shape, stage, signature),
                color,
                source.OutlineColor,
                source.Size,
                source.Priority,
                source.AudioCue,
                source.Hostile,
                stage == PresentationVfxStage.Launch || stage == PresentationVfxStage.Travel);
        }

        private static ProceduralShape ResolveShape(
            ProceduralShape baseShape,
            PresentationVfxStage stage,
            int signature)
        {
            if (stage == PresentationVfxStage.Anticipation) return ProceduralShape.Ring;
            if (stage == PresentationVfxStage.Launch) return baseShape;
            if (stage == PresentationVfxStage.Travel)
                return SelectTravelShape(signature & 3);
            if (stage == PresentationVfxStage.Impact)
                return SelectImpactShape((signature >> 2) & 3);
            return SelectResidueShape((signature >> 4) & 3);
        }

        private static ProceduralShape SelectTravelShape(int index) =>
            index == 1 ? ProceduralShape.Diamond :
            index == 2 ? ProceduralShape.Line : ProceduralShape.Chevron;

        private static ProceduralShape SelectImpactShape(int index) =>
            index == 1 ? ProceduralShape.Cross :
            index == 2 ? ProceduralShape.Circle : ProceduralShape.Hexagon;

        private static ProceduralShape SelectResidueShape(int index) =>
            index == 1 ? ProceduralShape.Triangle :
            index == 2 ? ProceduralShape.Square : ProceduralShape.Ring;

        private static float StageScale(PresentationVfxStage stage, int signature, bool reducedMotion)
        {
            var variant = 0.92f + (((signature >> 6) & 3) * 0.08f);
            var scale = stage == PresentationVfxStage.Anticipation ? 1.35f :
                stage == PresentationVfxStage.Launch ? 0.82f :
                stage == PresentationVfxStage.Travel ? 0.68f :
                stage == PresentationVfxStage.Impact ? 1.52f : 1.18f;
            return scale * variant * (reducedMotion ? 0.72f : 1f);
        }

        private static float StageDuration(PresentationVfxStage stage, bool reducedMotion)
        {
            var duration = stage == PresentationVfxStage.Anticipation ? 0.14f :
                stage == PresentationVfxStage.Launch ? 0.12f :
                stage == PresentationVfxStage.Travel ? 0.18f :
                stage == PresentationVfxStage.Impact ? 0.16f : 0.28f;
            return reducedMotion ? duration * 0.72f : duration;
        }

        private static float StageDelay(PresentationVfxStage stage, ContentId sourceId, bool reducedMotion)
        {
            var signature = BuildNonColorSignature(sourceId);
            var cadence = 1f + (((signature >> 6) & 3) * 0.06f);
            var delay = stage == PresentationVfxStage.Anticipation ? 0.045f :
                stage == PresentationVfxStage.Launch ? 0.055f :
                stage == PresentationVfxStage.Travel ? 0.075f : 0.085f;
            return delay * cadence * (reducedMotion ? 0.72f : 1f);
        }

        private static float Alpha(PresentationVfxStage stage) =>
            stage == PresentationVfxStage.Anticipation ? 0.52f :
            stage == PresentationVfxStage.Launch ? 0.9f :
            stage == PresentationVfxStage.Travel ? 0.78f :
            stage == PresentationVfxStage.Impact ? 0.96f : 0.42f;

        private static int PositiveModulo(int value, int divisor)
        {
            var result = value % divisor;
            return result < 0 ? result + divisor : result;
        }
    }
}
