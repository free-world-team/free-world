using System;
using System.Collections.Generic;
using Game.Application;
using Game.Simulation;
using UnityEngine;
using UnityEngine.UI;

namespace Game.Presentation
{
    /// <summary>
    /// Single presentation owner that reconciles snapshots, translates transient
    /// events, and ticks pooled effects. It never writes simulation state.
    /// </summary>
    public sealed class PresentationCoordinator : MonoBehaviour
    {
        private readonly Dictionary<SpatialEntity, EntityView> views =
            new Dictionary<SpatialEntity, EntityView>(256);
        private readonly HashSet<SpatialEntity> visible = new HashSet<SpatialEntity>();
        private readonly List<SpatialEntity> releaseBuffer = new List<SpatialEntity>(64);
        private readonly PresentationRequestBuffer requests = new PresentationRequestBuffer(64);

        private ProceduralVisualLibrary fallback;
        private EntityViewPool<ActorView> actors;
        private EntityViewPool<ProjectileView> projectiles;
        private EntityViewPool<AreaView> areas;
        private EntityViewPool<PickupView> pickups;
        private VfxRequestPool vfx;
        private StagedPresentationEffectSequencer stagedVfx;
        private DamageNumberPool damageNumbers;
        private AudioRequestRouter audioRouter;
        private ProceduralMapPresentation mapPresentation;
        private AccessibilitySettings settings;
        private ProceduralPresentationCatalog proceduralProfiles;
        private FormalVisualCatalog formalVisuals;
        private FormalAudioCatalog formalAudio;
        private DirectionalSpriteCatalog directionalSprites;
        private Game.Core.ContentId defaultFormalPickupProfileId;
        private ColorVisionMode lastColorVision;
        private bool lastScreenShakeEnabled;
        private float lastFlashIntensity;
        private PresentationMixState mixState;
        private long consumedTick = -1;
        private int lastMechanicTier = -1;
        private int lastBossPhase = -1;
        private bool lastHadBoss;
        private float pendingCameraImpulseAmplitude;
        private float pendingCameraImpulseDuration;
        private float cameraImpulseCooldown;
        private bool initialized;

        public int ActiveViewCount => views.Count;
        public int InvalidHandleRejections { get; private set; }
        public int LastHitRequestCount { get; private set; }
        public int LastDeathRequestCount { get; private set; }
        public int LastStatusRequestCount { get; private set; }
        public int LastPickupRequestCount { get; private set; }
        public int MissingProfileFallbackCount { get; private set; }
        public int ActiveVfxCount => vfx?.ActiveCount ?? 0;
        public int ActiveDamageNumberCount => damageNumbers?.ActiveCount ?? 0;
        public int ActiveAudioCount => audioRouter?.ActiveCount ?? 0;
        public int CreatedVfxCount => vfx?.CreatedCount ?? 0;
        public int CreatedAudioSourceCount => audioRouter?.CreatedSourceCount ?? 0;
        public int AudioSourceCapacity => audioRouter?.SourceCapacity ?? 0;
        public int AudioStemCapacity => audioRouter?.StemCapacity ?? 0;
        public int AudioReservedCriticalCapacity => audioRouter?.ReservedCriticalCapacity ?? 0;
        public bool FormalAudioLoaded => audioRouter?.FormalCatalogLoaded == true;
        public long DroppedVfxRequestCount => vfx?.DroppedRequestCount ?? 0;
        public long DroppedCriticalVfxRequestCount => vfx?.GetDroppedCount(PresentationPriority.CriticalDanger) ?? 0;
        public int PeakActiveVfxCount => vfx?.PeakActiveCount ?? 0;
        public long EvictedLowerPriorityVfxCount => vfx?.EvictedLowerPriorityCount ?? 0;
        public long MergedCriticalVfxCount => vfx?.MergedCriticalCount ?? 0;
        public long DroppedAudioRequestCount => audioRouter?.DroppedRequestCount ?? 0;
        public long DroppedCriticalAudioRequestCount => audioRouter?.GetDroppedCount(PresentationPriority.CriticalDanger) ?? 0;
        public int PeakActiveAudioCount => audioRouter?.PeakActiveCount ?? 0;
        public long SuppressedAudioCooldownCount => audioRouter?.SuppressedCooldownCount ?? 0;
        public long EvictedLowerPriorityAudioCount => audioRouter?.EvictedLowerPriorityCount ?? 0;
        public long MergedCriticalAudioCount => audioRouter?.MergedCriticalCount ?? 0;
        public int MapMarkerCount => mapPresentation?.MarkerCount ?? 0;
        public int MapGroundTileCount => mapPresentation?.GroundTileCount ?? 0;
        public int FormalMapGroundTileCount => mapPresentation?.FormalGroundTileCount ?? 0;
        public int FormalMapPropCount => mapPresentation?.FormalPropCount ?? 0;
        public int RaisedMapGeometryCount => mapPresentation?.RaisedGeometryCount ?? 0;
        public int MapGroundShadowCount => mapPresentation?.GroundShadowCount ?? 0;
        public int MapCentralArenaTransitionCount => mapPresentation?.CentralArenaTransitionCount ?? 0;
        public int MapRegionTransitionDecalCount => mapPresentation?.RegionTransitionDecalCount ?? 0;
        public int MapRegionIdentityClusterCount => mapPresentation?.RegionIdentityClusterCount ?? 0;
        public int FormalMapMarkerCount => mapPresentation?.FormalMarkerCount ?? 0;
        public int FormalMapMarkerStateSpriteCount => mapPresentation?.FormalMarkerStateSpriteCount ?? 0;
        public int VisibleFormalMapMarkerCount => mapPresentation?.VisibleFormalMarkerCount ?? 0;
        public int CompletedFormalMapMarkerCount => mapPresentation?.CompletedFormalMarkerCount ?? 0;
        public bool UsesXzGroundPlane => mapPresentation?.UsesXzGroundPlane == true;
        public long ProjectileTrailSpawnCount { get; private set; }
        public long TotalHitRequestCount { get; private set; }
        public long TotalDeathRequestCount { get; private set; }
        public long TotalStatusRequestCount { get; private set; }
        public long TotalPickupRequestCount { get; private set; }
        public long FormalVfxSpawnCount { get; private set; }
        public int ActiveStagedVfxSequenceCount => stagedVfx?.ActiveCount ?? 0;
        public long BegunStagedVfxSequenceCount => stagedVfx?.BegunSequenceCount ?? 0;
        public long CompletedStagedVfxSequenceCount => stagedVfx?.CompletedSequenceCount ?? 0;
        public long DroppedStagedVfxSequenceCount => stagedVfx?.DroppedSequenceCount ?? 0;
        public long DroppedCriticalStagedVfxSequenceCount => stagedVfx?.GetDroppedCount(PresentationPriority.CriticalDanger) ?? 0;
        public long EvictedLowerPriorityStagedVfxSequenceCount => stagedVfx?.EvictedLowerPrioritySequenceCount ?? 0;
        public long MergedCriticalStagedVfxSequenceCount => stagedVfx?.MergedCriticalSequenceCount ?? 0;
        public long ReducedMotionStageSpawnCount => stagedVfx?.ReducedMotionStageSpawnCount ?? 0;
        public long AnticipationVfxStageCount => stagedVfx?.GetStageSpawnCount(PresentationVfxStage.Anticipation) ?? 0;
        public long LaunchVfxStageCount => stagedVfx?.GetStageSpawnCount(PresentationVfxStage.Launch) ?? 0;
        public long TravelVfxStageCount => stagedVfx?.GetStageSpawnCount(PresentationVfxStage.Travel) ?? 0;
        public long ImpactVfxStageCount => stagedVfx?.GetStageSpawnCount(PresentationVfxStage.Impact) ?? 0;
        public long ResidueVfxStageCount => stagedVfx?.GetStageSpawnCount(PresentationVfxStage.Residue) ?? 0;
        public bool ReducedMotionActive => initialized && !settings.ScreenShakeEnabled;
        public long PresentationHitStopCount { get; private set; }
        public long CameraImpulseRequestCount { get; private set; }
        public long DirectionalAnimationFrameChangeCount => actors?.AnimationFrameChangeCount ?? 0;
        public int DirectionalSpriteSetCount => directionalSprites?.Count ?? 0;
        public int BossPhaseSpriteSetCount => directionalSprites?.BossPhaseSetCount ?? 0;
        public int BossPhaseStateSpriteCount => directionalSprites?.BossPhaseSpriteCount ?? 0;
        public int CreatedActorViewCount => actors?.CreatedCount ?? 0;
        public long ActorViewAcquireCount => actors?.AcquireCount ?? 0;
        public long ActorViewPoolHitCount => actors?.PoolHitCount ?? 0;
        public long ActorViewPoolExpansionCount => actors?.ExpansionCount ?? 0;
        public int ActiveActorViewCount => CountViews(EntityKind.Actor);
        public int ActiveProjectileViewCount => CountViews(EntityKind.Projectile);
        public int ActiveAreaViewCount => CountViews(EntityKind.Area);
        public int ActivePickupViewCount => CountViews(EntityKind.Pickup);
        public bool DensePickupPresentationActive { get; private set; }
        public int DensityGroupedPickupViewCount { get; private set; }
        public int DensityEmphasisPickupViewCount { get; private set; }
        public int HeldWeaponViewCount
        {
            get
            {
                var count = 0;
                foreach (var pair in views) if (pair.Value.HeldWeaponVisible) count++;
                return count;
            }
        }
        public int HeldWeaponAttackTrailViewCount
        {
            get
            {
                var count = 0;
                foreach (var pair in views) if (pair.Value.HeldWeaponAttackTrailActive) count++;
                return count;
            }
        }
        public int BossPhaseViewCount
        {
            get
            {
                var count = 0;
                foreach (var pair in views) if (pair.Value.BossPhaseFrameActive) count++;
                return count;
            }
        }
        public int MaximumAppliedBossPhase
        {
            get
            {
                var phase = -1;
                foreach (var pair in views)
                    if (pair.Value.AppliedBossPhase > phase) phase = pair.Value.AppliedBossPhase;
                return phase;
            }
        }

        public void Initialize(
            Canvas sharedCanvas,
            AccessibilitySettings accessibilitySettings,
            VisualProfileCatalog profileCatalog = null,
            ProceduralPresentationCatalog proceduralCatalog = null,
            FormalVisualCatalog formalCatalog = null,
            FormalAudioCatalog formalAudioCatalog = null,
            DirectionalSpriteCatalog directionalSpriteCatalog = null)
        {
            if (initialized) throw new InvalidOperationException("PresentationCoordinator is already initialized.");
            settings = accessibilitySettings ?? throw new ArgumentNullException(nameof(accessibilitySettings));
            fallback = new ProceduralVisualLibrary();
            var catalog = profileCatalog ?? new VisualProfileCatalog();
            proceduralProfiles = proceduralCatalog ?? new ProceduralPresentationCatalog();
            formalVisuals = formalCatalog;
            formalAudio = formalAudioCatalog;
            directionalSprites = directionalSpriteCatalog ?? new DirectionalSpriteCatalog();
            Sprite defaultHeldWeapon = null;
            var weaponProfileId = Game.Core.ContentId.Create("qinglan.presentation.skill.yufeng_sword");
            if (formalVisuals != null && weaponProfileId.IsSuccess &&
                formalVisuals.TryResolveProfile(weaponProfileId.Value, EntityKind.Projectile, out var weaponProfile))
                defaultHeldWeapon = weaponProfile.Sprite;
            var pickupProfileId = Game.Core.ContentId.Create("qinglan.pickup.riding_wind_feather");
            if (formalVisuals != null && pickupProfileId.IsSuccess &&
                formalVisuals.TryResolveProfile(pickupProfileId.Value, EntityKind.Pickup, out _))
                defaultFormalPickupProfileId = pickupProfileId.Value;
            actors = new EntityViewPool<ActorView>(transform, EntityKind.Actor, catalog, proceduralProfiles, settings, fallback, directionalSprites, 8, defaultHeldWeapon);
            projectiles = new EntityViewPool<ProjectileView>(transform, EntityKind.Projectile, catalog, proceduralProfiles, settings, fallback, directionalSprites, 16);
            areas = new EntityViewPool<AreaView>(transform, EntityKind.Area, catalog, proceduralProfiles, settings, fallback, directionalSprites, 8);
            pickups = new EntityViewPool<PickupView>(transform, EntityKind.Pickup, catalog, proceduralProfiles, settings, fallback, directionalSprites, 16);
            vfx = new VfxRequestPool(transform, fallback, 200, 32);
            stagedVfx = new StagedPresentationEffectSequencer(vfx);
            damageNumbers = new DamageNumberPool(sharedCanvas);
            audioRouter = new AudioRequestRouter(transform, formalAudio);
            lastColorVision = settings.ColorVision;
            lastScreenShakeEnabled = settings.ScreenShakeEnabled;
            lastFlashIntensity = settings.FlashIntensity;
            initialized = true;
        }

        public void Sync(RenderSnapshot snapshot, float interpolationAlpha, RunSession session = null)
        {
            if (!initialized) throw new InvalidOperationException("PresentationCoordinator must be initialized.");
            if (snapshot == null) throw new ArgumentNullException(nameof(snapshot));
            if (lastColorVision != settings.ColorVision ||
                lastScreenShakeEnabled != settings.ScreenShakeEnabled ||
                !Mathf.Approximately(lastFlashIntensity, settings.FlashIntensity))
            {
                lastColorVision = settings.ColorVision;
                lastScreenShakeEnabled = settings.ScreenShakeEnabled;
                lastFlashIntensity = settings.FlashIntensity;
                RefreshAllStyles(session);
            }
            var activePickupCount = 0;
            var playerX = 0f;
            var playerY = 0f;
            var hasPlayerPosition = false;
            for (var index = 0; index < snapshot.Count; index++)
            {
                var candidate = snapshot.GetAt(index);
                if (candidate.Entity.Kind == EntityKind.Pickup) activePickupCount++;
                if (session == null || candidate.Entity != session.Player) continue;
                playerX = candidate.CurrentPosition.X;
                playerY = candidate.CurrentPosition.Y;
                hasPlayerPosition = true;
            }
            DensePickupPresentationActive =
                activePickupCount > QinglanPresentationTheme.PickupGroupingThreshold;
            DensityGroupedPickupViewCount = 0;
            DensityEmphasisPickupViewCount = 0;
            visible.Clear();
            for (var index = 0; index < snapshot.Count; index++)
            {
                var entry = snapshot.GetAt(index);
                if (!entry.Entity.IsValid)
                {
                    InvalidHandleRejections++;
                    continue;
                }

                visible.Add(entry.Entity);
                if (!views.TryGetValue(entry.Entity, out var view))
                {
                    var visualProfileId = default(Game.Core.ContentId);
                    session?.TryGetVisualProfileId(entry.Entity, out visualProfileId);
                    visualProfileId = ResolveRenderableProfileId(visualProfileId, entry.Entity.Kind);
                    var playerStyle = session != null && entry.Entity == session.Player;
                    view = Acquire(entry.Entity, visualProfileId, playerStyle, out var usedFallback);
                    if (usedFallback) MissingProfileFallbackCount++;
                    ApplyOverlays(view, entry.Entity, session);
                    views.Add(entry.Entity, view);
                    if (entry.Entity.Kind == EntityKind.Projectile)
                    {
                        ProjectileTrailSpawnCount++;
                        TriggerNearestActorAttack(entry.CurrentPosition.X, entry.CurrentPosition.Y);
                        proceduralProfiles.TryResolveEffect(visualProfileId, settings.ColorVision, out var castStyle);
                        var castColor = castStyle.Color;
                        castColor.a = Mathf.Max(0.32f, settings.FlashIntensity * 0.65f);
                        castStyle = castStyle.WithColor(castColor, castStyle.OutlineColor);
                        TryResolveFormalEffectSprite(visualProfileId, entry.Entity.Kind, out var castSprite);
                        if (stagedVfx.TryBegin(
                            visualProfileId,
                            new Vector2(entry.CurrentPosition.X, entry.CurrentPosition.Y),
                            castStyle,
                            0.42f,
                            0f,
                            castSprite,
                            ReducedMotionActive) && castSprite != null)
                            FormalVfxSpawnCount++;
                    }
                    else if (entry.Entity.Kind == EntityKind.Area)
                        TriggerNearestActorAttack(entry.CurrentPosition.X, entry.CurrentPosition.Y);
                }

                if (!view.Apply(entry, interpolationAlpha, snapshot.Tick)) InvalidHandleRejections++;
                if (entry.Entity.Kind == EntityKind.Pickup)
                {
                    var deltaX = entry.CurrentPosition.X - (hasPlayerPosition ? playerX : 0f);
                    var deltaY = entry.CurrentPosition.Y - (hasPlayerPosition ? playerY : 0f);
                    view.ApplyPickupDensity(
                        activePickupCount,
                        (deltaX * deltaX) + (deltaY * deltaY),
                        (entry.CurrentStateFlags & SimulationStateFlags.Moving) != 0);
                    if (view.PickupDensityGrouped) DensityGroupedPickupViewCount++;
                    if (view.PickupClusterEmphasis) DensityEmphasisPickupViewCount++;
                }
            }

            releaseBuffer.Clear();
            foreach (var pair in views)
                if (!visible.Contains(pair.Key)) releaseBuffer.Add(pair.Key);
            for (var index = 0; index < releaseBuffer.Count; index++) Release(releaseBuffer[index]);
        }

        public bool TryGetView(SpatialEntity entity, out EntityView view)
        {
            return views.TryGetValue(entity, out view);
        }

        public bool Release(SpatialEntity entity)
        {
            if (!entity.IsValid || !views.TryGetValue(entity, out var view))
            {
                InvalidHandleRejections++;
                return false;
            }

            views.Remove(entity);
            switch (entity.Kind)
            {
                case EntityKind.Actor: return actors.Release((ActorView)view);
                case EntityKind.Projectile: return projectiles.Release((ProjectileView)view);
                case EntityKind.Area: return areas.Release((AreaView)view);
                case EntityKind.Pickup: return pickups.Release((PickupView)view);
                default:
                    InvalidHandleRejections++;
                    return false;
            }
        }

        public void ConsumeLatestEvents(
            long snapshotTick,
            SimulationEventBuffer simulationEvents,
            CombatEventBuffer combatEvents)
        {
            if (snapshotTick == consumedTick) return;
            consumedTick = snapshotTick;
            requests.Clear();
            LastHitRequestCount = 0;
            LastDeathRequestCount = 0;
            LastStatusRequestCount = 0;
            LastPickupRequestCount = 0;

            releaseBuffer.Clear();
            if (simulationEvents != null)
            {
                for (var index = 0; index < simulationEvents.Count; index++)
                {
                    var item = simulationEvents.GetAt(index);
                    var removed = new SpatialEntity(item.EntityKind, item.Handle);
                    if (item.Type != SimulationEventType.Removed || !views.ContainsKey(removed)) continue;
                    if (item.EntityKind == EntityKind.Pickup)
                    {
                        var playerPosition = FindPlayerPosition();
                        var deltaX = item.Position.X - playerPosition.x;
                        var deltaY = item.Position.Y - playerPosition.y;
                        if ((deltaX * deltaX) + (deltaY * deltaY) <= 4f)
                            requests.Add(new PresentationRequest(
                                PresentationRequestType.Pickup,
                                removed,
                                item.Position,
                                1f,
                                true,
                                defaultFormalPickupProfileId));
                    }
                    releaseBuffer.Add(removed);
                }
            }

            if (combatEvents != null)
            {
                for (var index = 0; index < combatEvents.DamageAppliedCount; index++)
                {
                    var item = combatEvents.GetDamageAppliedAt(index).Context;
                    requests.Add(new PresentationRequest(
                        PresentationRequestType.Hit,
                        item.Packet.Target,
                        item.Packet.Position,
                        item.FinalDamage,
                        item.WasCritical,
                        item.Packet.SourceContentId));
                }

                for (var index = 0; index < combatEvents.EntityDiedCount; index++)
                {
                    var item = combatEvents.GetEntityDiedAt(index);
                    requests.Add(new PresentationRequest(
                        PresentationRequestType.Death,
                        item.Target,
                        item.Position,
                        1f,
                        true,
                        item.SourceContentId));
                }

                for (var index = 0; index < combatEvents.StatusAppliedCount; index++)
                {
                    var item = combatEvents.GetStatusAppliedAt(index);
                    var position = System.Numerics.Vector2.Zero;
                    if (views.TryGetValue(item.Target, out var targetView))
                        position = new System.Numerics.Vector2(targetView.transform.position.x, targetView.transform.position.z);
                    requests.Add(new PresentationRequest(
                        PresentationRequestType.Status,
                        item.Target,
                        position,
                        item.Stacks,
                        item.Outcome == StatusApplicationOutcome.Replaced,
                        item.StatusId));
                }
            }

            RouteRequests();
            for (var index = 0; index < releaseBuffer.Count; index++) Release(releaseBuffer[index]);
        }

        public void TickEffects(float unscaledDeltaTime)
        {
            if (!initialized) return;
            audioRouter.SetMix(
                settings.MasterVolume,
                settings.MusicVolume,
                settings.AmbienceVolume,
                settings.EffectsVolume,
                mixState);
            foreach (var pair in views) pair.Value.TickVisual(unscaledDeltaTime);
            stagedVfx.Tick(unscaledDeltaTime, ReducedMotionActive);
            vfx.Tick(unscaledDeltaTime);
            damageNumbers.Tick(unscaledDeltaTime);
            audioRouter.Tick(unscaledDeltaTime);
            cameraImpulseCooldown = Mathf.Max(0f, cameraImpulseCooldown - Mathf.Max(0f, unscaledDeltaTime));
        }

        public void SetMixState(PresentationMixState value) => mixState = value;

        public bool RouteUiCue(PresentationAudioCue cue) =>
            initialized && audioRouter.Route(cue, PresentationPriority.Mechanic, 0.72f);

        /// <summary>Consumes the strongest queued presentation-only camera impulse.</summary>
        public bool TryConsumeCameraImpulse(out float amplitude, out float duration)
        {
            amplitude = pendingCameraImpulseAmplitude;
            duration = pendingCameraImpulseDuration;
            pendingCameraImpulseAmplitude = 0f;
            pendingCameraImpulseDuration = 0f;
            return amplitude > 0f && duration > 0f;
        }

        public void SetMap(ProceduralMapConfiguration configuration)
        {
            mapPresentation?.Dispose();
            mapPresentation = configuration == null ? null : new ProceduralMapPresentation(
                transform,
                configuration,
                proceduralProfiles,
                fallback,
                settings.ColorVision);
        }

        /// <summary>Converts low-frequency run-state transitions into readable presentation signals.</summary>
        public void SyncRunState(RunUiSnapshot snapshot)
        {
            if (!initialized || snapshot == null) return;
            var appliedBossPhase = snapshot.HasBoss ? snapshot.BossPhase : -1;
            foreach (var pair in views)
                if (pair.Key.Kind == EntityKind.Actor) pair.Value.SetBossPhase(appliedBossPhase);
            audioRouter.SetStemState(snapshot.DurationSeconds, snapshot.HasBoss, snapshot.BossId, snapshot.BossPhase);
            if (lastMechanicTier < 0) lastMechanicTier = snapshot.MechanicTier;
            else if (snapshot.MechanicTier > lastMechanicTier)
            {
                var position = FindPlayerPosition();
                var style = new ProceduralPresentationStyle(
                    ProceduralShape.Ring,
                    new Color(0.25f, 0.9f, 0.82f, Mathf.Max(0.35f, settings.FlashIntensity)),
                    Color.white,
                    Vector2.one * 1.5f,
                    PresentationPriority.Mechanic,
                    PresentationAudioCue.MechanicRise,
                    false,
                    true);
                vfx.TrySpawn(new ProceduralVfxRequest(position, style, 1.8f, 0.4f));
                audioRouter.Route(style.AudioCue, style.Priority, 0.55f);
            }
            lastMechanicTier = snapshot.MechanicTier;

            if (snapshot.HasBoss && (!lastHadBoss || snapshot.BossPhase != lastBossPhase))
            {
                var position = FindCriticalDangerPosition();
                var style = new ProceduralPresentationStyle(
                    ProceduralShape.Ring,
                    new Color(1f, 0.22f, 0.12f, Mathf.Max(0.45f, settings.FlashIntensity)),
                    Color.black,
                    Vector2.one * 2.5f,
                    PresentationPriority.CriticalDanger,
                    PresentationAudioCue.BossPhase,
                    true,
                    true);
                vfx.TrySpawn(new ProceduralVfxRequest(position, style, 3.2f, 0.55f));
                audioRouter.Route(style.AudioCue, style.Priority, 0.8f);
                QueueCameraImpulse(0.12f, 0.18f);
            }
            lastHadBoss = snapshot.HasBoss;
            lastBossPhase = snapshot.HasBoss ? snapshot.BossPhase : -1;
            mapPresentation?.Sync(snapshot, settings.ColorVision);
        }

        public void Clear()
        {
            if (!initialized) return;
            releaseBuffer.Clear();
            foreach (var pair in views) releaseBuffer.Add(pair.Key);
            for (var index = 0; index < releaseBuffer.Count; index++) Release(releaseBuffer[index]);
            requests.Clear();
            stagedVfx.Clear();
            consumedTick = -1;
            lastMechanicTier = -1;
            lastBossPhase = -1;
            lastHadBoss = false;
            pendingCameraImpulseAmplitude = 0f;
            pendingCameraImpulseDuration = 0f;
            cameraImpulseCooldown = 0f;
            DensePickupPresentationActive = false;
            DensityGroupedPickupViewCount = 0;
            DensityEmphasisPickupViewCount = 0;
        }

        private EntityView Acquire(
            SpatialEntity entity,
            Game.Core.ContentId visualProfileId,
            bool playerStyle,
            out bool usedFallback)
        {
            switch (entity.Kind)
            {
                case EntityKind.Actor: return actors.Acquire(entity, visualProfileId, playerStyle, out usedFallback);
                case EntityKind.Projectile: return projectiles.Acquire(entity, visualProfileId, playerStyle, out usedFallback);
                case EntityKind.Area: return areas.Acquire(entity, visualProfileId, playerStyle, out usedFallback);
                case EntityKind.Pickup: return pickups.Acquire(entity, visualProfileId, playerStyle, out usedFallback);
                default: throw new ArgumentOutOfRangeException(nameof(entity));
            }
        }

        private void RefreshAllStyles(RunSession session)
        {
            foreach (var pair in views)
            {
                var view = pair.Value;
                view.ClearOverlays();
                switch (pair.Key.Kind)
                {
                    case EntityKind.Actor: actors.RefreshStyle((ActorView)view); break;
                    case EntityKind.Projectile: projectiles.RefreshStyle((ProjectileView)view); break;
                    case EntityKind.Area: areas.RefreshStyle((AreaView)view); break;
                    case EntityKind.Pickup: pickups.RefreshStyle((PickupView)view); break;
                }
                ApplyOverlays(view, pair.Key, session);
            }
        }

        private void ApplyOverlays(EntityView view, SpatialEntity entity, RunSession session)
        {
            if (view == null || session == null || entity.Kind != EntityKind.Actor) return;
            for (var index = 0; index < 2; index++)
            {
                if (!session.TryGetVisualOverlayId(entity, index, out var overlayId)) break;
                actors.ApplyOverlay(
                    (ActorView)view,
                    index,
                    ResolveRenderableProfileId(overlayId, EntityKind.Actor));
            }
        }

        private void TriggerNearestActorAttack(float simulationX, float simulationY)
        {
            EntityView nearest = null;
            var nearestDistance = 16f;
            foreach (var pair in views)
            {
                if (pair.Key.Kind != EntityKind.Actor) continue;
                var position = pair.Value.transform.position;
                var deltaX = position.x - simulationX;
                var deltaY = position.z - simulationY;
                var distance = (deltaX * deltaX) + (deltaY * deltaY);
                if (distance >= nearestDistance) continue;
                nearestDistance = distance;
                nearest = pair.Value;
            }
            nearest?.PlayAttackReaction();
        }

        private void QueueCameraImpulse(float amplitude, float duration)
        {
            if (!settings.ScreenShakeEnabled) return;
            var boundedAmplitude = Mathf.Clamp(amplitude * Mathf.Max(0.25f, settings.FlashIntensity), 0f, 0.12f);
            if (cameraImpulseCooldown > 0f && boundedAmplitude < 0.07f) return;
            if (boundedAmplitude <= pendingCameraImpulseAmplitude) return;
            pendingCameraImpulseAmplitude = boundedAmplitude;
            pendingCameraImpulseDuration = Mathf.Clamp(duration, 0.04f, 0.2f);
            cameraImpulseCooldown = boundedAmplitude >= 0.07f ? 0.12f : 0.08f;
            CameraImpulseRequestCount++;
        }

        private void RouteRequests()
        {
            for (var index = 0; index < requests.Count; index++)
            {
                var request = requests.GetAt(index);
                var position = new Vector2(request.Position.X, request.Position.Y);
                proceduralProfiles.TryResolveEffect(request.ContentId, settings.ColorVision, out var style);
                switch (request.Type)
                {
                    case PresentationRequestType.Hit:
                        LastHitRequestCount++;
                        TotalHitRequestCount++;
                        if (views.TryGetValue(request.Target, out var hitView))
                        {
                            hitView.PlayHitReaction();
                            PresentationHitStopCount++;
                        }
                        QueueCameraImpulse(request.Emphasized ? 0.045f : 0.025f, 0.06f);
                        if (settings.FlashIntensity > 0f || style.Priority == PresentationPriority.CriticalDanger)
                        {
                            var color = style.Color;
                            color.a = style.Priority == PresentationPriority.CriticalDanger ?
                                Mathf.Max(0.35f, settings.FlashIntensity) : settings.FlashIntensity;
                            style = style.WithColor(color, style.OutlineColor);
                            var hitRequest = new ProceduralVfxRequest(position, style, 0.45f, 0.12f);
                            if (TryResolveFormalEffectSprite(request.ContentId, EntityKind.Projectile, out var hitSprite))
                            {
                                if (vfx.TrySpawn(hitRequest, hitSprite)) FormalVfxSpawnCount++;
                            }
                            else vfx.TrySpawn(hitRequest);
                        }
                        if (settings.DamageNumbersEnabled)
                            damageNumbers.Spawn(position, request.Magnitude, request.Emphasized);
                        audioRouter.Route(
                            style.AudioCue == PresentationAudioCue.None ? PresentationAudioCue.Hit : style.AudioCue,
                            style.Priority,
                            0.35f);
                        break;
                    case PresentationRequestType.Death:
                        LastDeathRequestCount++;
                        TotalDeathRequestCount++;
                        var deathColor = style.Color;
                        deathColor.a = Mathf.Max(0.55f, settings.FlashIntensity);
                        style = style.WithColor(deathColor, style.OutlineColor);
                        Sprite deathSprite = null;
                        if (views.TryGetValue(request.Target, out var deathView))
                            deathSprite = deathView.ResolvePoseSprite(PresentationPose.Death);
                        var deathSpawned = vfx.TrySpawn(
                            new ProceduralVfxRequest(position, style, 1.4f, 0.34f, 0f, deathSprite == null),
                            deathSprite);
                        if (deathSpawned && deathSprite != null) FormalVfxSpawnCount++;
                        audioRouter.Route(PresentationAudioCue.Death, style.Priority, 0.5f);
                        QueueCameraImpulse(0.075f, 0.12f);
                        break;
                    case PresentationRequestType.Pickup:
                        LastPickupRequestCount++;
                        TotalPickupRequestCount++;
                        var pickupColor = style.Color;
                        pickupColor.a = Mathf.Max(0.55f, settings.FlashIntensity * 0.75f);
                        style = style.WithColor(pickupColor, style.OutlineColor);
                        var pickupEffect = new ProceduralVfxRequest(position, style, 0.68f, 0.24f, 45f, false);
                        if (TryResolveFormalEffectSprite(request.ContentId, EntityKind.Pickup, out var pickupSprite))
                        {
                            if (vfx.TrySpawn(pickupEffect, pickupSprite)) FormalVfxSpawnCount++;
                        }
                        else vfx.TrySpawn(pickupEffect);
                        audioRouter.Route(PresentationAudioCue.Pickup, PresentationPriority.Mechanic, 0.42f);
                        break;
                    case PresentationRequestType.Status:
                        LastStatusRequestCount++;
                        TotalStatusRequestCount++;
                        if (settings.FlashIntensity > 0f)
                        {
                            var statusColor = style.Color;
                            statusColor.a = settings.FlashIntensity * 0.7f;
                            style = style.WithColor(statusColor, style.OutlineColor);
                            var effect = new ProceduralVfxRequest(position, style, 0.7f, 0.2f);
                            if (TryResolveFormalEffectSprite(request.ContentId, EntityKind.Area, out var sprite))
                            {
                                if (vfx.TrySpawn(effect, sprite)) FormalVfxSpawnCount++;
                            }
                            else
                                vfx.TrySpawn(effect);
                        }
                        break;
                }
            }
        }

        private Game.Core.ContentId ResolveRenderableProfileId(
            Game.Core.ContentId authoredId,
            EntityKind kind)
        {
            if (formalVisuals == null) return authoredId;
            if (!authoredId.IsValid)
                return kind == EntityKind.Pickup && defaultFormalPickupProfileId.IsValid
                    ? defaultFormalPickupProfileId
                    : authoredId;
            var normalized = FormalPresentationIdResolver.NormalizeProfileId(authoredId);
            return formalVisuals.TryResolveProfile(normalized, kind, out _) ? normalized : authoredId;
        }

        private bool TryResolveFormalEffectSprite(
            Game.Core.ContentId sourceId,
            EntityKind kind,
            out Sprite sprite)
        {
            if (formalVisuals != null)
            {
                var normalized = FormalPresentationIdResolver.NormalizeProfileId(sourceId);
                if (formalVisuals.TryResolveProfile(normalized, kind, out var profile) && profile.Sprite != null)
                {
                    sprite = profile.Sprite;
                    return true;
                }
                if (formalVisuals.TryResolveSprite(normalized, out sprite)) return true;
                if (FormalPresentationIdResolver.TryGetSkillVfxKey(sourceId, out var skillVfxKey) &&
                    formalVisuals.TryResolveSprite(skillVfxKey, out sprite)) return true;
            }

            sprite = null;
            return false;
        }

        private Vector2 FindPlayerPosition()
        {
            foreach (var pair in views)
                if (pair.Value.UsesPlayerStyle)
                    return PresentationSpace.ToSimulation(pair.Value.transform.position);
            return Vector2.zero;
        }

        private int CountViews(EntityKind kind)
        {
            var count = 0;
            foreach (var pair in views) if (pair.Key.Kind == kind) count++;
            return count;
        }

        private Vector2 FindCriticalDangerPosition()
        {
            EntityView selected = null;
            var largest = -1f;
            foreach (var pair in views)
            {
                var view = pair.Value;
                if (view.Priority != PresentationPriority.CriticalDanger) continue;
                var size = view.transform.localScale.sqrMagnitude;
                if (size <= largest) continue;
                largest = size;
                selected = view;
            }
            return selected == null ? Vector2.zero : PresentationSpace.ToSimulation(selected.transform.position);
        }

        public void Shutdown()
        {
            if (!initialized) return;
            Clear();
            actors.Dispose();
            projectiles.Dispose();
            areas.Dispose();
            pickups.Dispose();
            vfx.Dispose();
            damageNumbers.Dispose();
            audioRouter.Dispose();
            mapPresentation?.Dispose();
            mapPresentation = null;
            fallback.Dispose();
            formalAudio = null;
            initialized = false;
        }

        private void OnDestroy() => Shutdown();
    }
}
