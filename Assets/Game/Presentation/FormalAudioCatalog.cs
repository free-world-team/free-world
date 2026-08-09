using System;
using UnityEngine;
using UnityEngine.Audio;

namespace Game.Presentation
{
    public enum FormalAudioUsage : byte
    {
        Ambience = 0,
        Music = 1,
        Boss = 2,
        Player = 3,
        Weapon = 4,
        Enemy = 5,
        Affix = 6,
        Map = 7,
        Ui = 8
    }

    [Serializable]
    public sealed class FormalAudioBinding
    {
        [SerializeField] private string stableKey;
        [SerializeField] private string address;
        [SerializeField] private FormalAudioUsage usage;
        [SerializeField] private AudioClip clip;

        public string StableKey => stableKey ?? string.Empty;
        public string Address => address ?? string.Empty;
        public FormalAudioUsage Usage => usage;
        public AudioClip Clip => clip;
    }

    /// <summary>Addressable metadata and clip boundary for the Qinglan release audio set.</summary>
    [CreateAssetMenu(menuName = "Free World/Presentation/Formal Audio Catalog")]
    public sealed class FormalAudioCatalog : ScriptableObject
    {
        public const int CurrentSchemaVersion = 1;

        [SerializeField] private int schemaVersion = CurrentSchemaVersion;
        [SerializeField] private FormalAudioBinding[] bindings = Array.Empty<FormalAudioBinding>();
        [SerializeField] private AudioMixerGroup outputGroup;
        [SerializeField] private AudioMixerSnapshot gameplaySnapshot;
        [SerializeField] private AudioMixerSnapshot pausedSnapshot;
        [SerializeField] private AudioMixerSnapshot storySnapshot;
        [SerializeField] private AudioMixerSnapshot bossSnapshot;

        public int SchemaVersion => schemaVersion;
        public int BindingCount => bindings == null ? 0 : bindings.Length;
        public AudioMixerGroup OutputGroup => outputGroup;
        public int SnapshotCount =>
            (gameplaySnapshot == null ? 0 : 1) +
            (pausedSnapshot == null ? 0 : 1) +
            (storySnapshot == null ? 0 : 1) +
            (bossSnapshot == null ? 0 : 1);

        public bool TryResolveClip(string stableKey, out AudioClip clip)
        {
            if (!string.IsNullOrEmpty(stableKey) && bindings != null)
            {
                for (var index = 0; index < bindings.Length; index++)
                {
                    var binding = bindings[index];
                    if (binding != null && binding.Clip != null &&
                        string.Equals(binding.StableKey, stableKey, StringComparison.Ordinal))
                    {
                        clip = binding.Clip;
                        return true;
                    }
                }
            }

            clip = null;
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

        public bool TryResolveCue(PresentationAudioCue cue, out AudioClip clip)
        {
            switch (cue)
            {
                case PresentationAudioCue.Hit:
                    return TryResolveClip("qinglan/audio/player/player-hit", out clip);
                case PresentationAudioCue.Death:
                    return TryResolveClip("qinglan/audio/enemy/grass-spirit/death", out clip);
                case PresentationAudioCue.Pickup:
                    return TryResolveClip("qinglan/audio/player/pickup", out clip);
                case PresentationAudioCue.MechanicRise:
                    return TryResolveClip("qinglan/audio/player/riding-wind-tier", out clip);
                case PresentationAudioCue.Objective:
                    return TryResolveClip("qinglan/audio/map/system/objective-complete", out clip);
                case PresentationAudioCue.Danger:
                    return TryResolveClip("qinglan/audio/map/system/boundary-warning", out clip);
                case PresentationAudioCue.BossPhase:
                    return TryResolveClip("qinglan/audio/enemy/tingfeng/spawn", out clip);
                case PresentationAudioCue.Confirm:
                    return TryResolveClip("qinglan/audio/ui/confirm", out clip);
                case PresentationAudioCue.UiNavigate:
                    return TryResolveClip("qinglan/audio/ui/navigate", out clip);
                case PresentationAudioCue.UiCancel:
                    return TryResolveClip("qinglan/audio/ui/cancel", out clip);
                case PresentationAudioCue.UiPageOpen:
                    return TryResolveClip("qinglan/audio/ui/page-open", out clip);
                case PresentationAudioCue.UiTabChange:
                    return TryResolveClip("qinglan/audio/ui/tab-change", out clip);
                case PresentationAudioCue.UiChoiceSelect:
                    return TryResolveClip("qinglan/audio/ui/choice-select", out clip);
                case PresentationAudioCue.UiLocked:
                    return TryResolveClip("qinglan/audio/ui/locked", out clip);
                case PresentationAudioCue.UiNotification:
                    return TryResolveClip("qinglan/audio/ui/notification", out clip);
                case PresentationAudioCue.UiPauseToggle:
                    return TryResolveClip("qinglan/audio/ui/pause-toggle", out clip);
                default:
                    clip = null;
                    return false;
            }
        }

        public bool TryResolveBossStem(string bossId, int phase, out AudioClip clip)
        {
            var slug = !string.IsNullOrEmpty(bossId) &&
                       bossId.IndexOf("zhezhi", StringComparison.OrdinalIgnoreCase) >= 0
                ? "zhezhi"
                : "tingfeng";
            var suffix = Mathf.Clamp(phase, 0, 2) == 0 ? "phase-one" :
                Mathf.Clamp(phase, 0, 2) == 1 ? "phase-two" : "phase-three";
            return TryResolveClip("qinglan/audio/boss/" + slug + "/" + suffix, out clip);
        }

        public AudioMixerSnapshot GetSnapshot(PresentationMixState state)
        {
            switch (state)
            {
                case PresentationMixState.Paused: return pausedSnapshot;
                case PresentationMixState.Story: return storySnapshot;
                case PresentationMixState.Boss: return bossSnapshot;
                default: return gameplaySnapshot;
            }
        }
    }
}
