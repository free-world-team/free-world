using System;
using System.Collections.Generic;
using System.Globalization;
using Game.Application;
using Game.Simulation;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Game.Presentation
{
    internal sealed class PooledVfx
    {
        public GameObject Object;
        public SpriteRenderer Renderer;
        public float Remaining;
        public PresentationPriority Priority;
        public ProceduralShape Shape;
        public float InitialDuration;
        public float InitialSize;
        public Color InitialColor;
        public bool GroundAligned;
    }

    /// <summary>Single-owner VFX request pool with no per-effect Update methods.</summary>
    public sealed class VfxRequestPool : IDisposable
    {
        private readonly List<PooledVfx> all = new List<PooledVfx>(16);
        private readonly Stack<PooledVfx> available = new Stack<PooledVfx>(16);
        private readonly List<PooledVfx> active = new List<PooledVfx>(16);
        private readonly Transform root;
        private readonly Sprite sprite;
        private readonly ProceduralVisualLibrary library;
        private readonly int maximumCapacity;
        private readonly long[] droppedByPriority = new long[4];

        /// <summary>Creates the bounded production default while preserving the original constructor.</summary>
        public VfxRequestPool(Transform owner, Sprite fallbackSprite)
            : this(owner, fallbackSprite, 200)
        {
        }

        /// <summary>Creates a pool with an explicit simultaneous-effect capacity.</summary>
        public VfxRequestPool(Transform owner, Sprite fallbackSprite, int maximumCapacity)
        {
            root = owner ?? throw new ArgumentNullException(nameof(owner));
            sprite = fallbackSprite ?? throw new ArgumentNullException(nameof(fallbackSprite));
            if (maximumCapacity <= 0) throw new ArgumentOutOfRangeException(nameof(maximumCapacity));
            this.maximumCapacity = maximumCapacity;
        }

        internal VfxRequestPool(
            Transform owner,
            ProceduralVisualLibrary proceduralLibrary,
            int maximumCapacity,
            int prewarm)
        {
            root = owner ?? throw new ArgumentNullException(nameof(owner));
            library = proceduralLibrary ?? throw new ArgumentNullException(nameof(proceduralLibrary));
            sprite = library.Sprite;
            if (maximumCapacity <= 0) throw new ArgumentOutOfRangeException(nameof(maximumCapacity));
            if (prewarm < 0 || prewarm > maximumCapacity) throw new ArgumentOutOfRangeException(nameof(prewarm));
            this.maximumCapacity = maximumCapacity;
            for (var index = 0; index < prewarm; index++) available.Push(Create());
        }

        /// <summary>Gets the number of effects currently leased from the pool.</summary>
        public int ActiveCount => active.Count;
        /// <summary>Gets the number of effect objects created by this pool.</summary>
        public int CreatedCount => all.Count;
        /// <summary>Gets the highest simultaneous active count observed.</summary>
        public int PeakActiveCount { get; private set; }
        /// <summary>Gets the number of acquisitions served by an available object.</summary>
        public long HitCount { get; private set; }
        /// <summary>Gets the number of objects created to expand the pool.</summary>
        public long ExpansionCount { get; private set; }
        /// <summary>Gets the number of acquisitions rejected at capacity.</summary>
        public long FailedAcquireCount { get; private set; }
        /// <summary>Gets the number of VFX requests dropped at capacity.</summary>
        public long DroppedRequestCount { get; private set; }
        public long EvictedLowerPriorityCount { get; private set; }
        public long MergedCriticalCount { get; private set; }

        public long GetDroppedCount(PresentationPriority priority) => droppedByPriority[(int)priority];

        /// <summary>Spawns an effect using the original fire-and-forget contract.</summary>
        public void Spawn(Vector2 position, Color color, float size, float duration)
        {
            TrySpawn(position, color, size, duration);
        }

        /// <summary>Tries to spawn an effect and returns false when the bounded pool is full.</summary>
        public bool TrySpawn(Vector2 position, Color color, float size, float duration)
        {
            var style = new ProceduralPresentationStyle(
                ProceduralShape.Circle,
                color,
                Color.clear,
                Vector2.one,
                PresentationPriority.Combat,
                PresentationAudioCue.None,
                false);
            return TrySpawn(new ProceduralVfxRequest(position, style, size, duration));
        }

        public bool TrySpawn(in ProceduralVfxRequest request)
        {
            return TrySpawn(request, null);
        }

        public bool TrySpawn(in ProceduralVfxRequest request, Sprite formalSprite)
        {
            PooledVfx effect = null;
            if (available.Count > 0)
            {
                effect = available.Pop();
                HitCount++;
            }
            else if (all.Count < maximumCapacity)
            {
                effect = Create();
                ExpansionCount++;
            }
            else
            {
                var candidate = FindEvictionCandidate(request.Style.Priority);
                if (candidate >= 0)
                {
                    effect = active[candidate];
                    active.RemoveAt(candidate);
                    EvictedLowerPriorityCount++;
                }
                else if (request.Style.Priority == PresentationPriority.CriticalDanger && active.Count > 0)
                {
                    var merge = FindCriticalMerge(request.Style.Shape);
                    merge.Remaining = Mathf.Max(merge.Remaining, request.Duration);
                    merge.Object.transform.localScale = Vector3.Max(
                        merge.Object.transform.localScale,
                        Vector3.one * request.Size);
                    MergedCriticalCount++;
                    return true;
                }
                else
                {
                    FailedAcquireCount++;
                    DroppedRequestCount++;
                    droppedByPriority[(int)request.Style.Priority]++;
                    return false;
                }
            }

            effect.Object.transform.SetPositionAndRotation(
                request.GroundAligned
                    ? PresentationSpace.ToGround(
                        request.Position.x,
                        request.Position.y,
                        PresentationSpace.GroundDecalHeight * 4f)
                    : PresentationSpace.ToEntity(EntityKind.Actor, request.Position.x, request.Position.y),
                request.GroundAligned
                    ? PresentationSpace.GroundRotation * Quaternion.Euler(0f, 0f, request.RotationDegrees)
                    : Quaternion.Euler(0f, 0f, request.RotationDegrees));
            effect.Object.transform.localScale = Vector3.one * request.Size;
            effect.Renderer.sprite = formalSprite != null ? formalSprite :
                library == null ? sprite : library.GetSprite(request.Style.Shape);
            effect.Renderer.color = request.Style.Color;
            effect.Renderer.sortingOrder = request.GroundAligned
                ? 100 + PresentationSpace.DepthOffset(request.Position.y)
                : PresentationSpace.PriorityBand(request.Style.Priority) +
                  PresentationSpace.DepthOffset(request.Position.y);
            effect.Remaining = request.Duration;
            effect.InitialDuration = request.Duration;
            effect.InitialSize = request.Size;
            effect.InitialColor = request.Style.Color;
            effect.GroundAligned = request.GroundAligned;
            effect.Priority = request.Style.Priority;
            effect.Shape = request.Style.Shape;
            effect.Object.SetActive(true);
            active.Add(effect);
            if (active.Count > PeakActiveCount) PeakActiveCount = active.Count;
            return true;
        }

        public void Tick(float unscaledDeltaTime)
        {
            for (var index = active.Count - 1; index >= 0; index--)
            {
                var effect = active[index];
                effect.Remaining -= unscaledDeltaTime;
                var progress = effect.InitialDuration <= 0f
                    ? 1f
                    : 1f - Mathf.Clamp01(effect.Remaining / effect.InitialDuration);
                var eased = 1f - ((1f - progress) * (1f - progress));
                if (effect.GroundAligned)
                    effect.Object.transform.localScale = Vector3.one *
                        (effect.InitialSize * Mathf.Lerp(0.55f, 1.4f, eased));
                else
                {
                    effect.Object.transform.localScale = Vector3.one *
                        (effect.InitialSize * Mathf.Lerp(1f, 0.84f, progress));
                    effect.Object.transform.position += Vector3.up * (0.34f * unscaledDeltaTime);
                }
                var color = effect.InitialColor;
                color.a *= 1f - (progress * progress);
                effect.Renderer.color = color;
                if (effect.Remaining > 0f) continue;
                effect.Object.SetActive(false);
                active.RemoveAt(index);
                available.Push(effect);
            }
        }

        public void Dispose()
        {
            for (var index = all.Count - 1; index >= 0; index--)
                UnityObjectLifetime.Destroy(all[index].Object);
            all.Clear();
            active.Clear();
            available.Clear();
        }

        private PooledVfx Create()
        {
            var value = new PooledVfx();
            value.Object = new GameObject("M7_PooledVfx");
            value.Object.transform.SetParent(root, false);
            value.Renderer = value.Object.AddComponent<SpriteRenderer>();
            value.Renderer.sprite = sprite;
            value.Object.SetActive(false);
            all.Add(value);
            return value;
        }

        private int FindEvictionCandidate(PresentationPriority incoming)
        {
            var candidate = -1;
            var lowest = incoming;
            for (var index = 0; index < active.Count; index++)
            {
                if (active[index].Priority <= incoming || active[index].Priority <= lowest) continue;
                lowest = active[index].Priority;
                candidate = index;
            }
            return candidate;
        }

        private PooledVfx FindCriticalMerge(ProceduralShape shape)
        {
            for (var index = 0; index < active.Count; index++)
                if (active[index].Priority == PresentationPriority.CriticalDanger && active[index].Shape == shape)
                    return active[index];
            return active[0];
        }
    }

    internal sealed class DamageNumberEntry
    {
        public TMP_Text Text;
        public float Remaining;
    }

    /// <summary>Damage-number pool sharing the existing UI Canvas.</summary>
    public sealed class DamageNumberPool : IDisposable
    {
        private readonly List<DamageNumberEntry> all = new List<DamageNumberEntry>(16);
        private readonly Stack<DamageNumberEntry> available = new Stack<DamageNumberEntry>(16);
        private readonly List<DamageNumberEntry> active = new List<DamageNumberEntry>(16);
        private readonly RectTransform root;
        private readonly TMP_FontAsset font;
        private readonly int maximumCapacity;

        public DamageNumberPool(Canvas sharedCanvas)
            : this(sharedCanvas, 96, 16)
        {
        }

        public DamageNumberPool(Canvas sharedCanvas, int maximumCapacity, int prewarm)
        {
            if (sharedCanvas == null) throw new ArgumentNullException(nameof(sharedCanvas));
            if (maximumCapacity <= 0) throw new ArgumentOutOfRangeException(nameof(maximumCapacity));
            if (prewarm < 0 || prewarm > maximumCapacity) throw new ArgumentOutOfRangeException(nameof(prewarm));
            this.maximumCapacity = maximumCapacity;
            var rootObject = new GameObject("M7_DamageNumbers", typeof(RectTransform));
            rootObject.transform.SetParent(sharedCanvas.transform, false);
            root = (RectTransform)rootObject.transform;
            root.anchorMin = Vector2.zero;
            root.anchorMax = Vector2.one;
            root.offsetMin = Vector2.zero;
            root.offsetMax = Vector2.zero;
            font = TMP_Settings.defaultFontAsset;
            if (font == null) throw new InvalidOperationException("The governed TMP default font is unavailable.");
            for (var index = 0; index < prewarm; index++) available.Push(Create());
        }

        public int ActiveCount => active.Count;
        public int CreatedCount => all.Count;
        public long AggregatedCount { get; private set; }

        public void Spawn(Vector2 worldPosition, float value, bool critical)
        {
            DamageNumberEntry entry;
            if (available.Count > 0) entry = available.Pop();
            else if (all.Count < maximumCapacity) entry = Create();
            else
            {
                AggregatedCount++;
                if (active.Count > 0 && critical)
                {
                    entry = active[active.Count - 1];
                    entry.Text.color = Color.yellow;
                    entry.Remaining = Mathf.Max(entry.Remaining, 0.65f);
                }
                return;
            }
            entry.Text.rectTransform.anchoredPosition = worldPosition * 18f;
            entry.Text.text = value.ToString("0", CultureInfo.InvariantCulture);
            entry.Text.color = critical ? Color.yellow : Color.white;
            entry.Remaining = 0.65f;
            entry.Text.gameObject.SetActive(true);
            active.Add(entry);
        }

        public void Tick(float unscaledDeltaTime)
        {
            for (var index = active.Count - 1; index >= 0; index--)
            {
                var entry = active[index];
                entry.Remaining -= unscaledDeltaTime;
                entry.Text.rectTransform.anchoredPosition += Vector2.up * (24f * unscaledDeltaTime);
                if (entry.Remaining > 0f) continue;
                entry.Text.gameObject.SetActive(false);
                active.RemoveAt(index);
                available.Push(entry);
            }
        }

        public void Dispose()
        {
            UnityObjectLifetime.Destroy(root.gameObject);
            all.Clear();
            active.Clear();
            available.Clear();
        }

        private DamageNumberEntry Create()
        {
            var objectValue = new GameObject("M7_DamageNumber", typeof(RectTransform), typeof(TextMeshProUGUI));
            objectValue.transform.SetParent(root, false);
            var text = objectValue.GetComponent<TMP_Text>();
            text.font = font;
            text.fontSize = 18;
            text.alignment = TextAlignmentOptions.Center;
            text.raycastTarget = false;
            text.rectTransform.sizeDelta = new Vector2(120f, 32f);
            objectValue.SetActive(false);
            var entry = new DamageNumberEntry { Text = text };
            all.Add(entry);
            return entry;
        }
    }

    internal sealed class RoutedAudioSource
    {
        public AudioSource Source;
        public float Remaining;
        public float BaseVolume;
        public PresentationPriority Priority;
        public PresentationAudioCue Cue;
    }

    /// <summary>Presentation-only state mapped to the formal mixer snapshots.</summary>
    public enum PresentationMixState : byte
    {
        Gameplay = 0,
        Paused = 1,
        Story = 2,
        Boss = 3
    }

    /// <summary>Bounded priority router for formal clips with a Development-only test-tone fallback.</summary>
    public sealed class AudioRequestRouter : IDisposable
    {
        public const int ProductionSourceCapacity = 32;
        public const int ProductionReservedCriticalCapacity = 8;
        public const int ProductionStemCapacity = 8;
        public const float OrdinaryDuckLinear = 0.5011872f;
        public const float StoryEnvironmentDuckLinear = 0.6309574f;

        private readonly List<RoutedAudioSource> all;
        private readonly Stack<RoutedAudioSource> available;
        private readonly List<RoutedAudioSource> active;
        private readonly Transform root;
        private readonly AudioClip[] cueClips;
        private readonly float[] cooldowns;
        private readonly long[] droppedByPriority = new long[4];
        private readonly int transientCapacity;
        private readonly int reservedCriticalCapacity;
        private readonly AudioSource[] stemSources;
        private readonly FormalAudioCatalog formalCatalog;
        private readonly bool ownsGeneratedClips;
        private float masterVolume = 1f;
        private float musicVolume = 1f;
        private float ambienceVolume = 1f;
        private float effectsVolume = 1f;
        private float effectsMix = 1f;
        private float duckRemaining;
        private double runDurationSeconds;
        private bool bossActive;
        private string bossId = string.Empty;
        private int bossPhase;
        private bool hasMixState;
        private PresentationMixState mixState;

        public AudioRequestRouter(Transform owner)
            : this(owner, null)
        {
        }

        public AudioRequestRouter(Transform owner, FormalAudioCatalog catalog)
            : this(
                owner,
                ProductionSourceCapacity,
                ProductionReservedCriticalCapacity,
                8,
                catalog,
                catalog == null ? 2 : ProductionStemCapacity)
        {
        }

        public AudioRequestRouter(Transform owner, int maximumCapacity, int reservedCriticalCapacity, int prewarm)
            : this(owner, maximumCapacity, reservedCriticalCapacity, prewarm, null, 2)
        {
        }

        private AudioRequestRouter(
            Transform owner,
            int maximumCapacity,
            int reservedCriticalCapacity,
            int prewarm,
            FormalAudioCatalog catalog,
            int stemCapacity)
        {
            root = owner ?? throw new ArgumentNullException(nameof(owner));
            if (maximumCapacity <= stemCapacity) throw new ArgumentOutOfRangeException(nameof(maximumCapacity));
            transientCapacity = maximumCapacity - stemCapacity;
            if (reservedCriticalCapacity < 0 || reservedCriticalCapacity >= transientCapacity)
                throw new ArgumentOutOfRangeException(nameof(reservedCriticalCapacity));
            if (prewarm < 0 || prewarm > transientCapacity) throw new ArgumentOutOfRangeException(nameof(prewarm));
            this.reservedCriticalCapacity = reservedCriticalCapacity;
            formalCatalog = catalog;
            ownsGeneratedClips = catalog == null;
            all = new List<RoutedAudioSource>(transientCapacity);
            available = new Stack<RoutedAudioSource>(transientCapacity);
            active = new List<RoutedAudioSource>(transientCapacity);
            cueClips = new AudioClip[Enum.GetValues(typeof(PresentationAudioCue)).Length];
            cooldowns = new float[cueClips.Length];
            stemSources = new AudioSource[stemCapacity];
            if (formalCatalog == null) ConfigureFallbackClips();
            else ConfigureFormalClips();
            StartConfiguredStems();
            for (var index = 0; index < prewarm; index++) available.Push(Create());
            SetMix(1f, 1f, 1f, 1f, PresentationMixState.Gameplay);
        }

        public int ActiveCount => active.Count;
        public int CreatedSourceCount => all.Count + stemSources.Length;
        public int SourceCapacity => transientCapacity + stemSources.Length;
        public int StemCapacity => stemSources.Length;
        public int ReservedCriticalCapacity => reservedCriticalCapacity;
        public bool FormalCatalogLoaded => formalCatalog != null;
        public bool UsingTestToneFallback => ownsGeneratedClips;
        public bool HighPressureStemActive => runDurationSeconds >= 630d && !bossActive;
        public int CurrentBossPhase => bossActive ? bossPhase : -1;
        public PresentationMixState CurrentMixState => mixState;
        public int SnapshotTransitionCount { get; private set; }
        public int StemScheduleBatchCount { get; private set; }
        public int ConfiguredStemCount
        {
            get
            {
                var count = 0;
                for (var index = 0; index < stemSources.Length; index++)
                    if (stemSources[index] != null && stemSources[index].clip != null) count++;
                return count;
            }
        }
        public int PeakActiveCount { get; private set; }
        public long DroppedRequestCount { get; private set; }
        public long SuppressedCooldownCount { get; private set; }
        public long EvictedLowerPriorityCount { get; private set; }
        public long MergedCriticalCount { get; private set; }
        public long GetDroppedCount(PresentationPriority priority) => droppedByPriority[(int)priority];

        public void Route(PresentationRequestType type, float volume)
        {
            var cue = type == PresentationRequestType.Death ? PresentationAudioCue.Death :
                type == PresentationRequestType.Hit ? PresentationAudioCue.Hit : PresentationAudioCue.None;
            Route(cue, PresentationPriority.Combat, volume);
        }

        public bool Route(PresentationAudioCue cue, PresentationPriority priority, float volume)
        {
            if (cue == PresentationAudioCue.None || volume <= 0f) return false;
            var cueIndex = (int)cue;
            if (cueIndex < 0 || cueIndex >= cueClips.Length || cueClips[cueIndex] == null) return false;
            if (priority != PresentationPriority.CriticalDanger && cooldowns[cueIndex] > 0f)
            {
                SuppressedCooldownCount++;
                return false;
            }

            var ordinaryLimit = transientCapacity - reservedCriticalCapacity;
            RoutedAudioSource item = null;
            if (priority != PresentationPriority.CriticalDanger && active.Count >= ordinaryLimit)
            {
                DroppedRequestCount++;
                droppedByPriority[(int)priority]++;
                return false;
            }
            if (available.Count > 0) item = available.Pop();
            else if (all.Count < transientCapacity) item = Create();
            else
            {
                var candidate = FindEvictionCandidate(priority);
                if (candidate >= 0)
                {
                    item = active[candidate];
                    item.Source.Stop();
                    active.RemoveAt(candidate);
                    EvictedLowerPriorityCount++;
                }
                else if (priority == PresentationPriority.CriticalDanger && active.Count > 0)
                {
                    var merge = FindCriticalMerge(cue);
                    merge.Remaining = Mathf.Max(merge.Remaining, cueClips[cueIndex].length);
                    merge.BaseVolume = Mathf.Max(merge.BaseVolume, Mathf.Clamp01(volume));
                    MergedCriticalCount++;
                    duckRemaining = Mathf.Max(duckRemaining, 0.35f);
                    ApplyEffectVolumes();
                    return true;
                }
                else
                {
                    DroppedRequestCount++;
                    droppedByPriority[(int)priority]++;
                    return false;
                }
            }

            item.Source.clip = cueClips[cueIndex];
            item.BaseVolume = Mathf.Clamp01(volume);
            item.Priority = priority;
            item.Cue = cue;
            item.Remaining = item.Source.clip.length;
            item.Source.volume = ResolveEffectVolume(item);
            if (UnityEngine.Application.isPlaying) item.Source.Play();
            active.Add(item);
            if (active.Count > PeakActiveCount) PeakActiveCount = active.Count;
            cooldowns[cueIndex] = CooldownFor(cue);
            if (priority == PresentationPriority.CriticalDanger)
                duckRemaining = Mathf.Max(duckRemaining, 0.35f);
            ApplyEffectVolumes();
            return true;
        }

        public void SetMix(
            float master,
            float music,
            float ambience,
            float effects,
            PresentationMixState state)
        {
            masterVolume = Mathf.Clamp01(master);
            musicVolume = Mathf.Clamp01(music);
            ambienceVolume = Mathf.Clamp01(ambience);
            effectsVolume = Mathf.Clamp01(effects);
            if (!hasMixState || mixState != state)
            {
                mixState = state;
                hasMixState = true;
                var snapshot = formalCatalog == null ? null : formalCatalog.GetSnapshot(state);
                if (snapshot != null)
                {
                    snapshot.TransitionTo(0.08f);
                    SnapshotTransitionCount++;
                }
            }
            float musicMix;
            float ambienceMix;
            switch (state)
            {
                case PresentationMixState.Paused:
                    musicMix = 0.12f;
                    ambienceMix = 0.1f;
                    effectsMix = 0.6f;
                    break;
                case PresentationMixState.Story:
                    musicMix = 0.2f;
                    ambienceMix = 0.2f * StoryEnvironmentDuckLinear;
                    effectsMix = 0.65f;
                    break;
                case PresentationMixState.Boss:
                    musicMix = 0.34f;
                    ambienceMix = 0.16f;
                    effectsMix = 1f;
                    break;
                default:
                    musicMix = 0.28f;
                    ambienceMix = 0.2f;
                    effectsMix = 1f;
                    break;
            }
            ApplyStemVolumes(musicMix, ambienceMix);
            ApplyEffectVolumes();
        }

        public void SetStemState(double durationSeconds, bool hasBoss, string activeBossId, int activeBossPhase)
        {
            runDurationSeconds = Math.Max(0d, durationSeconds);
            var normalizedBossId = activeBossId ?? string.Empty;
            var normalizedPhase = Mathf.Clamp(activeBossPhase, 0, 2);
            var identityChanged = !string.Equals(bossId, normalizedBossId, StringComparison.Ordinal);
            bossActive = hasBoss;
            bossId = normalizedBossId;
            bossPhase = normalizedPhase;
            if (formalCatalog != null && identityChanged) ConfigureBossStems();
        }

        public bool TryGetActiveCueVolume(PresentationAudioCue cue, out float volume)
        {
            for (var index = 0; index < active.Count; index++)
            {
                if (active[index].Cue != cue) continue;
                volume = active[index].Source.volume;
                return true;
            }
            volume = 0f;
            return false;
        }

        public bool TryGetStemVolume(int index, out float volume)
        {
            if (index >= 0 && index < stemSources.Length && stemSources[index] != null)
            {
                volume = stemSources[index].volume;
                return true;
            }
            volume = 0f;
            return false;
        }

        public void Tick(float unscaledDeltaTime)
        {
            for (var index = 0; index < cooldowns.Length; index++)
                cooldowns[index] = Mathf.Max(0f, cooldowns[index] - unscaledDeltaTime);
            duckRemaining = Mathf.Max(0f, duckRemaining - unscaledDeltaTime);
            for (var index = active.Count - 1; index >= 0; index--)
            {
                var item = active[index];
                item.Remaining -= unscaledDeltaTime;
                if (item.Remaining > 0f) continue;
                item.Source.Stop();
                item.Source.clip = null;
                active.RemoveAt(index);
                available.Push(item);
            }
            ApplyEffectVolumes();
        }

        public void Dispose()
        {
            for (var index = all.Count - 1; index >= 0; index--)
                UnityObjectLifetime.Destroy(all[index].Source.gameObject);
            if (ownsGeneratedClips)
                for (var index = 0; index < cueClips.Length; index++)
                    if (cueClips[index] != null) UnityObjectLifetime.Destroy(cueClips[index]);
            for (var index = 0; index < stemSources.Length; index++)
            {
                if (stemSources[index] == null) continue;
                var clip = stemSources[index].clip;
                UnityObjectLifetime.Destroy(stemSources[index].gameObject);
                if (ownsGeneratedClips && clip != null) UnityObjectLifetime.Destroy(clip);
            }
            all.Clear();
            active.Clear();
            available.Clear();
        }

        private RoutedAudioSource Create()
        {
            var objectValue = new GameObject("M7_PooledAudio");
            objectValue.transform.SetParent(root, false);
            var value = new RoutedAudioSource { Source = objectValue.AddComponent<AudioSource>() };
            value.Source.playOnAwake = false;
            value.Source.outputAudioMixerGroup = formalCatalog == null ? null : formalCatalog.OutputGroup;
            all.Add(value);
            return value;
        }

        private AudioSource CreateLoopSource(string name, AudioClip clip)
        {
            var objectValue = new GameObject(name);
            objectValue.transform.SetParent(root, false);
            var source = objectValue.AddComponent<AudioSource>();
            source.playOnAwake = false;
            source.loop = true;
            source.clip = clip;
            source.outputAudioMixerGroup = formalCatalog == null ? null : formalCatalog.OutputGroup;
            return source;
        }

        private void ConfigureFallbackClips()
        {
            cueClips[(int)PresentationAudioCue.Hit] = CreateTestTone("G2_7_HitTone", 660f, 0.05f);
            cueClips[(int)PresentationAudioCue.Death] = CreateTestTone("G2_7_DeathTone", 220f, 0.11f);
            cueClips[(int)PresentationAudioCue.Pickup] = CreateTestTone("G2_7_PickupTone", 880f, 0.08f);
            cueClips[(int)PresentationAudioCue.MechanicRise] = CreateTestTone("G2_7_MechanicTone", 520f, 0.16f);
            cueClips[(int)PresentationAudioCue.Objective] = CreateTestTone("G2_7_ObjectiveTone", 740f, 0.14f);
            cueClips[(int)PresentationAudioCue.Danger] = CreateTestTone("G2_7_DangerTone", 150f, 0.18f);
            cueClips[(int)PresentationAudioCue.BossPhase] = CreateTestTone("G2_7_BossTone", 110f, 0.28f);
            cueClips[(int)PresentationAudioCue.Confirm] = CreateTestTone("G2_7_ConfirmTone", 780f, 0.07f);
            cueClips[(int)PresentationAudioCue.UiNavigate] = CreateTestTone("G3_2_UiNavigateTone", 880f, 0.05f);
            cueClips[(int)PresentationAudioCue.UiCancel] = CreateTestTone("G3_2_UiCancelTone", 420f, 0.08f);
            cueClips[(int)PresentationAudioCue.UiPageOpen] = CreateTestTone("G3_2_UiPageTone", 560f, 0.1f);
            cueClips[(int)PresentationAudioCue.UiTabChange] = CreateTestTone("G3_2_UiTabTone", 720f, 0.06f);
            cueClips[(int)PresentationAudioCue.UiChoiceSelect] = CreateTestTone("G3_2_UiChoiceTone", 820f, 0.09f);
            cueClips[(int)PresentationAudioCue.UiLocked] = CreateTestTone("G3_2_UiLockedTone", 260f, 0.12f);
            cueClips[(int)PresentationAudioCue.UiNotification] = CreateTestTone("G3_2_UiNoticeTone", 940f, 0.14f);
            cueClips[(int)PresentationAudioCue.UiPauseToggle] = CreateTestTone("G3_2_UiPauseTone", 480f, 0.09f);
            stemSources[0] = CreateLoopSource("G2_7_GeneratedAmbience", CreateLoop("G2_7_AmbienceLoop", 55f, 82.5f));
            stemSources[1] = CreateLoopSource("G2_7_GeneratedMusic", CreateLoop("G2_7_MusicLoop", 110f, 165f));
        }

        private void ConfigureFormalClips()
        {
            for (var index = 1; index < cueClips.Length; index++)
                formalCatalog.TryResolveCue((PresentationAudioCue)index, out cueClips[index]);

            SetStemClip(0, "Qinglan_Ambience", "qinglan/audio/ambience/courtyard-wind");
            SetStemClip(1, "Qinglan_Music_Air", "qinglan/audio/music/exploration-air");
            SetStemClip(2, "Qinglan_Music_Strings", "qinglan/audio/music/exploration-strings");
            SetStemClip(3, "Qinglan_Music_Combat", "qinglan/audio/music/combat-rhythm");
            SetStemClip(4, "Qinglan_Music_Pressure", "qinglan/audio/music/high-pressure-drive");
            ConfigureBossStems();
        }

        private void ConfigureBossStems()
        {
            if (stemSources.Length < ProductionStemCapacity) return;
            for (var phase = 0; phase < 3; phase++)
            {
                formalCatalog.TryResolveBossStem(bossId, phase, out var clip);
                SetStemClip(phase + 5, "Qinglan_Boss_Phase_" + (phase + 1), clip);
            }
            if (UnityEngine.Application.isPlaying && hasMixState) ScheduleStems(5, 8);
        }

        private void SetStemClip(int index, string name, string address)
        {
            formalCatalog.TryResolveClip(address, out var clip);
            SetStemClip(index, name, clip);
        }

        private void SetStemClip(int index, string name, AudioClip clip)
        {
            if (index < 0 || index >= stemSources.Length) return;
            if (stemSources[index] == null)
            {
                stemSources[index] = CreateLoopSource(name, clip);
                return;
            }
            if (stemSources[index].clip == clip) return;
            stemSources[index].Stop();
            stemSources[index].clip = clip;
        }

        private void StartConfiguredStems()
        {
            if (UnityEngine.Application.isPlaying) ScheduleStems(0, stemSources.Length);
        }

        private void ScheduleStems(int first, int exclusiveLast)
        {
            var dspStart = AudioSettings.dspTime + 0.08d;
            for (var index = first; index < exclusiveLast; index++)
            {
                if (stemSources[index] == null || stemSources[index].clip == null) continue;
                stemSources[index].Stop();
                stemSources[index].PlayScheduled(dspStart);
            }
            StemScheduleBatchCount++;
        }

        private void ApplyStemVolumes(float musicMix, float ambienceMix)
        {
            if (stemSources.Length == 2)
            {
                stemSources[0].volume = masterVolume * ambienceVolume * ambienceMix;
                stemSources[1].volume = masterVolume * musicVolume * musicMix;
                return;
            }

            var ambience = masterVolume * ambienceVolume * ambienceMix;
            var music = masterVolume * musicVolume * musicMix;
            stemSources[0].volume = ambience;
            stemSources[1].volume = music * (bossActive ? 0.18f : 0.82f);
            stemSources[2].volume = music * (bossActive ? 0.12f : 0.62f);
            stemSources[3].volume = music * (!bossActive && runDurationSeconds >= 120d ? 0.72f : 0f);
            stemSources[4].volume = music * (!bossActive && runDurationSeconds >= 630d ? 0.76f : 0f);
            for (var phase = 0; phase < 3; phase++)
                stemSources[phase + 5].volume = music * (bossActive && bossPhase == phase ? 0.92f : 0f);
        }

        private int FindEvictionCandidate(PresentationPriority incoming)
        {
            var candidate = -1;
            var leastImportant = incoming;
            for (var index = 0; index < active.Count; index++)
            {
                if (active[index].Priority <= incoming || active[index].Priority <= leastImportant) continue;
                leastImportant = active[index].Priority;
                candidate = index;
            }
            return candidate;
        }

        private RoutedAudioSource FindCriticalMerge(PresentationAudioCue cue)
        {
            for (var index = 0; index < active.Count; index++)
                if (active[index].Priority == PresentationPriority.CriticalDanger && active[index].Cue == cue)
                    return active[index];
            return active[0];
        }

        private void ApplyEffectVolumes()
        {
            for (var index = 0; index < active.Count; index++)
                active[index].Source.volume = ResolveEffectVolume(active[index]);
        }

        private float ResolveEffectVolume(RoutedAudioSource item)
        {
            var duck = duckRemaining > 0f && item.Priority != PresentationPriority.CriticalDanger ?
                OrdinaryDuckLinear : 1f;
            return item.BaseVolume * masterVolume * effectsVolume * effectsMix * duck;
        }

        private static float CooldownFor(PresentationAudioCue cue)
        {
            switch (cue)
            {
                case PresentationAudioCue.Hit: return 0.04f;
                case PresentationAudioCue.Pickup: return 0.08f;
                case PresentationAudioCue.Danger: return 0.12f;
                case PresentationAudioCue.UiNavigate: return 0.04f;
                case PresentationAudioCue.UiTabChange: return 0.06f;
                default: return 0.05f;
            }
        }

        private static AudioClip CreateTestTone(string name, float frequency, float seconds)
        {
            const int sampleRate = 22050;
            var count = Mathf.CeilToInt(sampleRate * seconds);
            var samples = new float[count];
            for (var index = 0; index < count; index++)
            {
                var envelope = 1f - ((float)index / count);
                samples[index] = Mathf.Sin(2f * Mathf.PI * frequency * index / sampleRate) * envelope * 0.08f;
            }
            var clip = AudioClip.Create(name, count, 1, sampleRate, false);
            clip.SetData(samples, 0);
            return clip;
        }

        private static AudioClip CreateLoop(string name, float firstFrequency, float secondFrequency)
        {
            const int sampleRate = 22050;
            const int count = sampleRate;
            var samples = new float[count];
            for (var index = 0; index < count; index++)
            {
                var first = Mathf.Sin(2f * Mathf.PI * firstFrequency * index / sampleRate);
                var second = Mathf.Sin(2f * Mathf.PI * secondFrequency * index / sampleRate);
                samples[index] = ((first * 0.7f) + (second * 0.3f)) * 0.025f;
            }
            var clip = AudioClip.Create(name, count, 1, sampleRate, false);
            clip.SetData(samples, 0);
            return clip;
        }
    }
}
