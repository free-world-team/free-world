using System;
using Game.Core;
using UnityEngine;

namespace Game.Presentation
{
    public enum PresentationFacing : byte
    {
        Down = 0,
        Left = 1,
        Right = 2,
        Up = 3
    }

    public enum PresentationPose : byte
    {
        Idle = 0,
        Move = 1,
        Attack = 2,
        Hit = 3,
        Death = 4,
        Victory = 5
    }

    /// <summary>Four-direction, state-aware formal sprite set loaded outside simulation.</summary>
    public sealed class DirectionalSpriteSet
    {
        public const int FacingCount = 4;
        public const int PoseCount = 6;
        public const int BossPhaseCount = 3;
        private readonly Sprite[] sprites;
        private readonly Sprite[] bossPhaseSprites;

        public DirectionalSpriteSet(ContentId profileId, Sprite[] source)
            : this(profileId, source, null)
        {
        }

        public DirectionalSpriteSet(ContentId profileId, Sprite[] source, Sprite[] phaseSprites)
        {
            if (!profileId.IsValid) throw new ArgumentException("Profile ID must be valid.", nameof(profileId));
            if (source == null || source.Length != FacingCount * PoseCount)
                throw new ArgumentException("A directional set requires exactly 24 sprite slots.", nameof(source));
            if (phaseSprites != null && phaseSprites.Length != FacingCount * BossPhaseCount)
                throw new ArgumentException("Boss phase animation requires exactly 12 sprite slots.", nameof(phaseSprites));
            ProfileId = profileId;
            sprites = (Sprite[])source.Clone();
            bossPhaseSprites = phaseSprites == null ? Array.Empty<Sprite>() : (Sprite[])phaseSprites.Clone();
        }

        public ContentId ProfileId { get; }
        public bool HasBossPhaseFrames => bossPhaseSprites.Length == FacingCount * BossPhaseCount;
        public int BossPhaseSpriteCount => bossPhaseSprites.Length;

        public Sprite Resolve(PresentationFacing facing, PresentationPose pose, Sprite fallback)
        {
            var value = sprites[((int)facing * PoseCount) + (int)pose];
            return value != null ? value : fallback;
        }

        public Sprite ResolveBossPhase(PresentationFacing facing, int phase, Sprite fallback)
        {
            if (!HasBossPhaseFrames) return fallback;
            var clamped = Mathf.Clamp(phase, 0, BossPhaseCount - 1);
            var value = bossPhaseSprites[((int)facing * BossPhaseCount) + clamped];
            return value != null ? value : fallback;
        }
    }

    /// <summary>Small startup catalog; entity pools resolve it only when a view is acquired.</summary>
    public sealed class DirectionalSpriteCatalog
    {
        private readonly DirectionalSpriteSet[] sets;

        public DirectionalSpriteCatalog(DirectionalSpriteSet[] source = null)
        {
            sets = source == null ? Array.Empty<DirectionalSpriteSet>() : (DirectionalSpriteSet[])source.Clone();
        }

        public int Count => sets.Length;
        public int BossPhaseSetCount
        {
            get
            {
                var count = 0;
                for (var index = 0; index < sets.Length; index++)
                    if (sets[index] != null && sets[index].HasBossPhaseFrames) count++;
                return count;
            }
        }
        public int BossPhaseSpriteCount
        {
            get
            {
                var count = 0;
                for (var index = 0; index < sets.Length; index++)
                    if (sets[index] != null) count += sets[index].BossPhaseSpriteCount;
                return count;
            }
        }

        public bool TryResolve(ContentId profileId, out DirectionalSpriteSet set)
        {
            if (profileId.IsValid)
            {
                for (var index = 0; index < sets.Length; index++)
                {
                    var candidate = sets[index];
                    if (candidate != null && candidate.ProfileId == profileId)
                    {
                        set = candidate;
                        return true;
                    }
                }
            }

            set = null;
            return false;
        }

        public static PresentationFacing FacingFromRadians(float radians)
        {
            var x = Mathf.Cos(radians);
            var y = Mathf.Sin(radians);
            if (Mathf.Abs(x) > Mathf.Abs(y)) return x < 0f ? PresentationFacing.Left : PresentationFacing.Right;
            return y < 0f ? PresentationFacing.Down : PresentationFacing.Up;
        }
    }
}
