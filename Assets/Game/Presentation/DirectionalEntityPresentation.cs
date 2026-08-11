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
        private readonly Sprite[] sprites;

        public DirectionalSpriteSet(ContentId profileId, Sprite[] source)
        {
            if (!profileId.IsValid) throw new ArgumentException("Profile ID must be valid.", nameof(profileId));
            if (source == null || source.Length != FacingCount * PoseCount)
                throw new ArgumentException("A directional set requires exactly 24 sprite slots.", nameof(source));
            ProfileId = profileId;
            sprites = (Sprite[])source.Clone();
        }

        public ContentId ProfileId { get; }

        public Sprite Resolve(PresentationFacing facing, PresentationPose pose, Sprite fallback)
        {
            var value = sprites[((int)facing * PoseCount) + (int)pose];
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
