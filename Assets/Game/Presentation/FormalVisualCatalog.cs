using System;
using Game.Core;
using Game.Simulation;
using UnityEngine;

namespace Game.Presentation
{
    public enum FormalVisualUsage : byte
    {
        CatalogOnly = 0,
        RuntimeSprite = 1,
        UiBackground = 2,
        UiChrome = 3,
        StatusEffect = 4
    }

    [Serializable]
    public sealed class FormalVisualBinding
    {
        [SerializeField] private string stableKey;
        [SerializeField] private string address;
        [SerializeField] private FormalVisualUsage usage;
        [SerializeField] private Sprite sprite;

        public string StableKey => stableKey ?? string.Empty;
        public string Address => address ?? string.Empty;
        public FormalVisualUsage Usage => usage;
        public Sprite Sprite => sprite;
    }

    /// <summary>Addressable metadata boundary for formal visual content.</summary>
    [CreateAssetMenu(menuName = "Free World/Presentation/Formal Visual Catalog")]
    public sealed class FormalVisualCatalog : ScriptableObject
    {
        public const int CurrentSchemaVersion = 1;

        [SerializeField] private int schemaVersion = CurrentSchemaVersion;
        [SerializeField] private VisualProfile[] entityProfiles = Array.Empty<VisualProfile>();
        [SerializeField] private FormalVisualBinding[] bindings = Array.Empty<FormalVisualBinding>();

        public int SchemaVersion => schemaVersion;
        public int EntityProfileCount => entityProfiles == null ? 0 : entityProfiles.Length;
        public int BindingCount => bindings == null ? 0 : bindings.Length;
        public VisualProfileCatalog CreateEntityCatalog() => new VisualProfileCatalog(entityProfiles);

        public bool TryResolveSprite(string stableKey, out Sprite sprite)
        {
            if (!string.IsNullOrEmpty(stableKey) && bindings != null)
            {
                for (var index = 0; index < bindings.Length; index++)
                {
                    var binding = bindings[index];
                    if (binding != null && binding.Sprite != null &&
                        string.Equals(binding.StableKey, stableKey, StringComparison.Ordinal))
                    {
                        sprite = binding.Sprite;
                        return true;
                    }
                }
            }

            sprite = null;
            return false;
        }

        public bool TryResolveSprite(ContentId stableId, out Sprite sprite) =>
            TryResolveSprite(stableId.IsValid ? stableId.Value : string.Empty, out sprite);

        public bool TryResolveAddress(string stableKey, out string address)
        {
            if (!string.IsNullOrEmpty(stableKey) && bindings != null)
            {
                for (var index = 0; index < bindings.Length; index++)
                {
                    var binding = bindings[index];
                    if (binding != null && !string.IsNullOrEmpty(binding.Address) &&
                        string.Equals(binding.StableKey, stableKey, StringComparison.Ordinal))
                    {
                        address = binding.Address;
                        return true;
                    }
                }
            }

            address = string.Empty;
            return false;
        }

        public bool ContainsAddress(string address)
        {
            if (string.IsNullOrEmpty(address) || bindings == null) return false;
            for (var index = 0; index < bindings.Length; index++)
            {
                var binding = bindings[index];
                if (binding != null && string.Equals(binding.Address, address, StringComparison.Ordinal))
                    return true;
            }
            return false;
        }

        public bool TryResolveProfile(ContentId stableId, EntityKind kind, out VisualProfile profile)
        {
            if (stableId.IsValid && entityProfiles != null)
            {
                for (var index = 0; index < entityProfiles.Length; index++)
                {
                    var candidate = entityProfiles[index];
                    if (candidate != null && candidate.EntityKind == kind &&
                        string.Equals(candidate.StableId, stableId.Value, StringComparison.Ordinal))
                    {
                        profile = candidate;
                        return true;
                    }
                }
            }

            profile = null;
            return false;
        }
    }
}
