using System;
using UnityEngine;

namespace Game.Presentation
{
    /// <summary>Immutable presentation-only state frames for one map marker.</summary>
    public sealed class ProceduralMapMarkerSpriteSet
    {
        private readonly Sprite[] states;

        public ProceduralMapMarkerSpriteSet(Sprite[] stateSprites)
        {
            states = stateSprites == null ? Array.Empty<Sprite>() : (Sprite[])stateSprites.Clone();
        }

        public int Count => states.Length;

        public bool TryGet(int index, out Sprite sprite)
        {
            if (index < 0 || index >= states.Length)
            {
                sprite = null;
                return false;
            }
            sprite = states[index];
            return sprite != null;
        }
    }
}
