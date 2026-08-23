using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Text;
using Game.Application;
using Game.Core;
using Game.Presentation;
using Game.Simulation;
using Game.UI;
using TMPro;
using Unity.Profiling;
using UnityEngine;
using UnityEngine.Profiling;
using UnityEngine.UI;

namespace Game.Infrastructure
{
    /// <summary>
    /// Opt-in visible Player driver for the G4.0 sixty-second and G4.2 production
    /// visual acceptance gates.
    /// It exercises one real card click, the production flow, simulation and presentation,
    /// and captures five rendered frames without introducing a second gameplay state path.
    /// </summary>
    internal sealed class QinglanG40VisualAcceptanceRunner : MonoBehaviour
    {
        private const string G40Argument = "-qinglanG40VisualAcceptance";
        private const string G42Argument = "-qinglanG42VisualAcceptance";
        private const string G42FinalArgument = "-qinglanG42FinalAcceptance";
        private const string G40ResultVariable = "QINGLAN_G40_VISUAL_RESULT";
        private const string G42ResultVariable = "QINGLAN_G42_VISUAL_RESULT";
        private const string G42FinalResultVariable = "QINGLAN_G42_FINAL_RESULT";
        private const string G40ScreenshotVariable = "QINGLAN_G40_SCREENSHOT_DIR";
        private const string G42ScreenshotVariable = "QINGLAN_G42_SCREENSHOT_DIR";
        private const string G42FinalScreenshotVariable = "QINGLAN_G42_FINAL_SCREENSHOT_DIR";
        private const double G40RequiredWallClockSeconds = 60d;
        private const double G42RequiredWallClockSeconds = 90d;
        private const double G42FinalRequiredWallClockSeconds = 720d;
        private const double AcceptanceSimulationScale = 3.25d;
        private const double FinalAcceptanceSimulationScale = 1d;
        private const ulong G42RunSeed = 0x4734324156495355UL;
        private const ulong G42RewardSeed = 0x514C414E47523432UL;
        private static readonly double[] G40ScreenshotTimes = { 0.5d, 15d, 30d, 45d, 60d };
        private static readonly string[] G40ScreenshotNames =
        {
            "00-enter-combat", "15-seconds", "30-seconds", "45-seconds", "60-seconds"
        };
        private static readonly double[] G42ScreenshotTimes = { 0.5d, 15d, 30d, 45d, 60d, 90d };
        private static readonly string[] G42ScreenshotNames =
        {
            "00-enter-combat", "15-seconds", "30-seconds", "45-seconds", "60-seconds", "90-seconds"
        };
        private static readonly double[] G42FinalScreenshotTimes =
        {
            0.5d, 15d, 30d, 60d, 180d, 360d, 540d, 720d
        };
        private static readonly string[] G42FinalScreenshotNames =
        {
            "00-enter-combat", "15-seconds", "30-seconds", "60-seconds",
            "03-minutes", "06-minutes", "09-minutes", "12-minutes"
        };
        private static readonly Vector2[] AcceptanceWaypoints =
        {
            new Vector2(10f, -10f),
            new Vector2(17f, -11f),
            new Vector2(25f, -11f),
            new Vector2(32f, -4f),
            new Vector2(34f, 8f),
            new Vector2(25f, -11f),
            new Vector2(17f, -11f),
            new Vector2(10f, -8f),
            new Vector2(12f, 8f),
            new Vector2(4f, 11f)
        };
        private static readonly Vector2[] G42AcceptanceWaypoints =
        {
            new Vector2(10f, -10f),
            new Vector2(18f, -11f),
            new Vector2(27f, -11f),
            new Vector2(34f, -4f),
            new Vector2(34f, 10f)
        };
        private static readonly string[] G42UpgradePriority =
        {
            "qinglan.skill.weapon.spirit_vine_seed",
            "qinglan.skill.weapon.yufeng_sword",
            "qinglan.skill.weapon.zhenyue_seal",
            "qinglan.skill.weapon.tide_orb",
            "qinglan.skill.weapon.lihuo_wheel",
            "qinglan.skill.weapon.yellow_talisman"
        };
        private static readonly string[] G42MajorPresentationProfiles =
        {
            "qinglan.presentation.skill.yufeng_sword",
            "qinglan.presentation.skill.yellow_talisman",
            "qinglan.presentation.skill.lihuo_wheel",
            "qinglan.presentation.skill.tide_orb",
            "qinglan.presentation.skill.zhenyue_seal",
            "qinglan.presentation.skill.spirit_vine_seed"
        };

        private static int CountDistinctMajorSkillSignatures()
        {
            var signatures = new HashSet<int>();
            for (var index = 0; index < G42MajorPresentationProfiles.Length; index++)
            {
                var id = ContentId.Create(G42MajorPresentationProfiles[index]);
                if (id.IsSuccess)
                    signatures.Add(StagedPresentationEffectSequencer.BuildNonColorSignature(id.Value));
            }
            return signatures.Count;
        }

        internal static bool IsRequested()
        {
            return HasArgument(G40Argument) || HasArgument(G42Argument) || HasArgument(G42FinalArgument);
        }

        private IEnumerator Start()
        {
            yield return null;
            var finalAcceptance = HasArgument(G42FinalArgument);
            var g42 = finalAcceptance || HasArgument(G42Argument);
            var requiredWallClockSeconds = finalAcceptance
                ? G42FinalRequiredWallClockSeconds
                : g42 ? G42RequiredWallClockSeconds : G40RequiredWallClockSeconds;
            var acceptanceSimulationScale = finalAcceptance
                ? FinalAcceptanceSimulationScale
                : AcceptanceSimulationScale;
            var screenshotTimes = finalAcceptance
                ? G42FinalScreenshotTimes
                : g42 ? G42ScreenshotTimes : G40ScreenshotTimes;
            var screenshotNames = finalAcceptance
                ? G42FinalScreenshotNames
                : g42 ? G42ScreenshotNames : G40ScreenshotNames;
            var resultVariable = finalAcceptance
                ? G42FinalResultVariable
                : g42 ? G42ResultVariable : G40ResultVariable;
            var screenshotVariable = finalAcceptance
                ? G42FinalScreenshotVariable
                : g42 ? G42ScreenshotVariable : G40ScreenshotVariable;
            var host = GetComponent<QinglanDemoRuntimeHost>();
            var result = new QinglanG40VisualAcceptanceResult
            {
                schemaVersion = finalAcceptance ? 4 : g42 ? 3 : 1,
                milestone = finalAcceptance ? "G4.2-F" : g42 ? "G4.2-E" : "G4.0",
                generatedAtUtc = DateTime.UtcNow.ToString("O", CultureInfo.InvariantCulture),
                status = "FAIL",
                requiredWallClockSeconds = requiredWallClockSeconds,
                acceptanceSimulationScale = acceptanceSimulationScale,
                screenWidth = Screen.width,
                screenHeight = Screen.height,
                graphicsDeviceName = SystemInfo.graphicsDeviceName,
                graphicsDeviceType = SystemInfo.graphicsDeviceType.ToString(),
                graphicsMemorySizeMegabytes = SystemInfo.graphicsMemorySize,
                processorType = SystemInfo.processorType,
                processorCount = SystemInfo.processorCount,
                systemMemorySizeMegabytes = SystemInfo.systemMemorySize,
                operatingSystem = SystemInfo.operatingSystem,
                qualityLevel = QualitySettings.names[QualitySettings.GetQualityLevel()],
                vSyncCount = QualitySettings.vSyncCount,
                humanVisualSignoff = false,
                runSeed = g42 ? G42RunSeed.ToString("X16", CultureInfo.InvariantCulture) : string.Empty,
                rewardSeed = g42 ? G42RewardSeed.ToString("X16", CultureInfo.InvariantCulture) : string.Empty,
                majorSkillNonColorSignatureCount = g42 ? CountDistinctMajorSkillSignatures() : 0
            };
            if (host == null)
            {
                Finish(result, "The Qinglan runtime host is missing.", resultVariable, g42);
                yield break;
            }

            result.formalVisualsLoaded = host.FormalVisualsLoaded;
            result.formalAudioLoaded = host.FormalAudioLoaded;
            result.formalFontsLoaded = host.FormalFontsLoaded;
            result.realCardClicks = ClickFirstInteractableCard(host.Ui);
            yield return null;
            if (host.Flow.Stage != DemoFlowStage.CharacterSelect ||
                !host.Flow.Execute(QinglanUiCommand.Continue, "character", 0) ||
                !host.Flow.Execute(QinglanUiCommand.OpenLoadout, "map", 0) ||
                !(g42
                    ? host.Flow.BeginVisualAcceptanceRun(G42RunSeed, G42RewardSeed)
                    : host.Flow.Execute(QinglanUiCommand.BeginRun, "begin", 0)))
            {
                Finish(result, "The title card or production run preparation flow failed.", resultVariable, g42);
                yield break;
            }

            host.TickRuntime(0d);
            host.TickRuntime(SimulationClock.TickDurationSeconds);
            if (host.Flow.Stage != DemoFlowStage.Active || host.Flow.Session == null)
            {
                Finish(result, "The active run did not start.", resultVariable, g42);
                yield break;
            }

            host.enabled = false;
            var enemyProfiles = new HashSet<string>(StringComparer.Ordinal);
            var observedViews = new HashSet<SpatialEntity>();
            var frameSampleCapacity = finalAcceptance ? 65536 : 8192;
            var wallFrameSamples = new double[frameSampleCapacity];
            var gpuFrameSamples = new double[frameSampleCapacity];
            var frameTiming = new FrameTiming[1];
            var gcAllocatedRecorder = ProfilerRecorder.StartNew(
                ProfilerCategory.Memory, "GC Allocated In Frame", 1);
            var drawCallsRecorder = ProfilerRecorder.StartNew(
                ProfilerCategory.Render, "Draw Calls Count", 1);
            var setPassRecorder = ProfilerRecorder.StartNew(
                ProfilerCategory.Render, "SetPass Calls Count", 1);
            var trianglesRecorder = ProfilerRecorder.StartNew(
                ProfilerCategory.Render, "Triangles Count", 1);
            var wallFrameSampleCount = 0;
            var gpuFrameSampleCount = 0;
            var wallFrameTotalMilliseconds = 0d;
            var gpuFrameTotalMilliseconds = 0d;
            result.initialMonoUsedBytes = Profiler.GetMonoUsedSizeLong();
            result.peakMonoUsedBytes = result.initialMonoUsedBytes;
            result.initialTotalAllocatedMemoryBytes = Profiler.GetTotalAllocatedMemoryLong();
            result.peakTotalAllocatedMemoryBytes = result.initialTotalAllocatedMemoryBytes;
            var generation0Start = GC.CollectionCount(0);
            var generation1Start = GC.CollectionCount(1);
            var generation2Start = GC.CollectionCount(2);
            FrameTimingManager.CaptureFrameTimings();
            var startedAt = Time.realtimeSinceStartupAsDouble;
            var drivenSimulationTicks = 0L;
            var waypointIndex = 0;
            var nextScreenshot = 0;
            while (true)
            {
                var now = Time.realtimeSinceStartupAsDouble;
                var elapsed = now - startedAt;
                DriveActiveRun(
                    host,
                    elapsed,
                    acceptanceSimulationScale,
                    g42,
                    ref drivenSimulationTicks,
                    ref waypointIndex,
                    result,
                    enemyProfiles,
                    observedViews);
                CollectMetrics(host, result, enemyProfiles, observedViews);
                RecordPerformanceSample(
                    result,
                    wallFrameSamples,
                    ref wallFrameSampleCount,
                    ref wallFrameTotalMilliseconds,
                    gpuFrameSamples,
                    ref gpuFrameSampleCount,
                    ref gpuFrameTotalMilliseconds,
                    frameTiming);
                RecordRenderPerformanceSample(
                    result,
                    drawCallsRecorder,
                    setPassRecorder,
                    trianglesRecorder,
                    gcAllocatedRecorder);

                while (nextScreenshot < screenshotTimes.Length &&
                       elapsed >= screenshotTimes[nextScreenshot])
                {
                    yield return CaptureScreenshot(
                        result,
                        screenshotNames[nextScreenshot],
                        screenshotVariable,
                        false,
                        false);
                    nextScreenshot++;
                }

                if (elapsed >= requiredWallClockSeconds) break;
                yield return null;
            }

            host.ClearVisualAcceptanceMovement();
            if (g42)
            {
                yield return CaptureG42AccessibilityEvidence(host, result, screenshotVariable);
                yield return CaptureGrayscaleReview(result, screenshotVariable);
            }
            result.wallClockSeconds = Time.realtimeSinceStartupAsDouble - startedAt;
            var uiSnapshot = new RunUiSnapshot();
            if (host.Flow.Session != null && host.Flow.Session.CaptureUiSnapshot(uiSnapshot))
                result.simulationSeconds = uiSnapshot.DurationSeconds;
            result.distinctEnemyProfileIds = new string[enemyProfiles.Count];
            enemyProfiles.CopyTo(result.distinctEnemyProfileIds);
            Array.Sort(result.distinctEnemyProfileIds, StringComparer.Ordinal);
            result.distinctEnemyProfileCount = result.distinctEnemyProfileIds.Length;
            result.totalObservedViews = observedViews.Count;
            result.wallFrameAverageMilliseconds = wallFrameSampleCount == 0
                ? 0d
                : wallFrameTotalMilliseconds / wallFrameSampleCount;
            result.wallFrameP99Milliseconds = Percentile99(wallFrameSamples, wallFrameSampleCount);
            result.averageFps = result.wallFrameAverageMilliseconds <= 0d
                ? 0d
                : 1000d / result.wallFrameAverageMilliseconds;
            result.onePercentLowFps = result.wallFrameP99Milliseconds <= 0d
                ? 0d
                : 1000d / result.wallFrameP99Milliseconds;
            result.gpuFrameAverageMilliseconds = gpuFrameSampleCount == 0
                ? 0d
                : gpuFrameTotalMilliseconds / gpuFrameSampleCount;
            result.gpuFrameP99Milliseconds = Percentile99(gpuFrameSamples, gpuFrameSampleCount);
            result.wallFrameSampleCount = wallFrameSampleCount;
            result.gpuFrameSampleCount = gpuFrameSampleCount;
            result.finalMonoUsedBytes = Profiler.GetMonoUsedSizeLong();
            result.finalTotalAllocatedMemoryBytes = Profiler.GetTotalAllocatedMemoryLong();
            result.managedMemoryGrowthBytes = result.finalMonoUsedBytes - result.initialMonoUsedBytes;
            result.totalAllocatedMemoryGrowthBytes =
                result.finalTotalAllocatedMemoryBytes - result.initialTotalAllocatedMemoryBytes;
            result.averageDrawCalls = result.renderRecorderSamples == 0
                ? 0d
                : result.drawCallsTotal / (double)result.renderRecorderSamples;
            result.averageSetPassCalls = result.renderRecorderSamples == 0
                ? 0d
                : result.setPassCallsTotal / (double)result.renderRecorderSamples;
            result.renderRecordersAvailable = result.renderRecorderSamples > 0 &&
                                              result.maximumDrawCalls > 0 &&
                                              result.maximumTriangles > 0 &&
                                              result.drawCallsRecorderValid &&
                                              result.setPassRecorderValid &&
                                              result.trianglesRecorderValid;
            result.generation0Collections = GC.CollectionCount(0) - generation0Start;
            result.generation1Collections = GC.CollectionCount(1) - generation1Start;
            result.generation2Collections = GC.CollectionCount(2) - generation2Start;
            result.usesTiltedOrthographicCamera = CaptureCameraMetrics(result);
            result.usesXzGroundPlane = host.Presentation.UsesXzGroundPlane;
            result.raisedMapGeometryCount = host.Presentation.RaisedMapGeometryCount;
            result.mapGroundShadowCount = host.Presentation.MapGroundShadowCount;
            result.formalMapGroundTileCount = host.Presentation.FormalMapGroundTileCount;
            result.formalMapPropCount = host.Presentation.FormalMapPropCount;
            result.centralArenaTransitionCount = host.Presentation.MapCentralArenaTransitionCount;
            result.regionTransitionDecalCount = host.Presentation.MapRegionTransitionDecalCount;
            result.regionIdentityClusterCount = host.Presentation.MapRegionIdentityClusterCount;
            result.formalMapMarkerCount = host.Presentation.FormalMapMarkerCount;
            result.formalMapMarkerStateSpriteCount = host.Presentation.FormalMapMarkerStateSpriteCount;
            result.projectileTrailSpawnCount = host.Presentation.ProjectileTrailSpawnCount;
            result.directionalAnimationFrameChangeCount =
                host.Presentation.DirectionalAnimationFrameChangeCount;
            result.directionalSpriteSetCount = host.Presentation.DirectionalSpriteSetCount;
            result.bossPhaseSpriteSetCount = host.Presentation.BossPhaseSpriteSetCount;
            result.bossPhaseStateSpriteCount = host.Presentation.BossPhaseStateSpriteCount;
            result.createdActorViewCount = host.Presentation.CreatedActorViewCount;
            result.actorViewAcquireCount = host.Presentation.ActorViewAcquireCount;
            result.actorViewPoolHitCount = host.Presentation.ActorViewPoolHitCount;
            result.actorViewPoolExpansionCount = host.Presentation.ActorViewPoolExpansionCount;
            result.totalHitRequestCount = host.Presentation.TotalHitRequestCount;
            result.totalDeathRequestCount = host.Presentation.TotalDeathRequestCount;
            result.totalStatusRequestCount = host.Presentation.TotalStatusRequestCount;
            result.totalPickupRequestCount = host.Presentation.TotalPickupRequestCount;
            result.formalVfxSpawnCount = host.Presentation.FormalVfxSpawnCount;
            result.createdVfxCount = host.Presentation.CreatedVfxCount;
            result.peakActiveVfx = host.Presentation.PeakActiveVfxCount;
            result.droppedVfxRequestCount = host.Presentation.DroppedVfxRequestCount;
            result.droppedCriticalVfxRequestCount = host.Presentation.DroppedCriticalVfxRequestCount;
            result.evictedLowerPriorityVfxCount = host.Presentation.EvictedLowerPriorityVfxCount;
            result.mergedCriticalVfxCount = host.Presentation.MergedCriticalVfxCount;
            result.begunStagedVfxSequenceCount = host.Presentation.BegunStagedVfxSequenceCount;
            result.completedStagedVfxSequenceCount = host.Presentation.CompletedStagedVfxSequenceCount;
            result.droppedStagedVfxSequenceCount = host.Presentation.DroppedStagedVfxSequenceCount;
            result.droppedCriticalStagedVfxSequenceCount = host.Presentation.DroppedCriticalStagedVfxSequenceCount;
            result.evictedLowerPriorityStagedVfxSequenceCount = host.Presentation.EvictedLowerPriorityStagedVfxSequenceCount;
            result.mergedCriticalStagedVfxSequenceCount = host.Presentation.MergedCriticalStagedVfxSequenceCount;
            result.reducedMotionStageSpawnCount = host.Presentation.ReducedMotionStageSpawnCount;
            result.anticipationVfxStageCount = host.Presentation.AnticipationVfxStageCount;
            result.launchVfxStageCount = host.Presentation.LaunchVfxStageCount;
            result.travelVfxStageCount = host.Presentation.TravelVfxStageCount;
            result.impactVfxStageCount = host.Presentation.ImpactVfxStageCount;
            result.residueVfxStageCount = host.Presentation.ResidueVfxStageCount;
            result.presentationHitStopCount = host.Presentation.PresentationHitStopCount;
            result.cameraImpulseRequestCount = host.Presentation.CameraImpulseRequestCount;
            result.peakActiveAudio = host.Presentation.PeakActiveAudioCount;
            result.droppedAudioRequestCount = host.Presentation.DroppedAudioRequestCount;
            result.droppedCriticalAudioRequestCount = host.Presentation.DroppedCriticalAudioRequestCount;
            result.suppressedAudioCooldownCount = host.Presentation.SuppressedAudioCooldownCount;
            result.evictedLowerPriorityAudioCount = host.Presentation.EvictedLowerPriorityAudioCount;
            result.mergedCriticalAudioCount = host.Presentation.MergedCriticalAudioCount;
            result.missingProfileFallbackCount = host.Presentation.MissingProfileFallbackCount;
            gcAllocatedRecorder.Dispose();
            drawCallsRecorder.Dispose();
            setPassRecorder.Dispose();
            trianglesRecorder.Dispose();
            var sharedAutomaticGate = result.formalVisualsLoaded && result.formalAudioLoaded &&
                                         result.formalFontsLoaded && result.realCardClicks > 0 &&
                                         result.wallClockSeconds >= requiredWallClockSeconds &&
                                         result.screenshotCount == screenshotTimes.Length &&
                                         result.distinctEnemyProfileCount >= 3 &&
                                         result.totalObservedViews >= result.maxActiveViews &&
                                         result.maxActorViews >= 4 && result.maxProjectileViews > 0 &&
                                         result.maxHeldWeaponViews > 0 &&
                                         result.projectileTrailSpawnCount > 0 &&
                                         result.directionalAnimationFrameChangeCount > 0 &&
                                         result.directionalSpriteSetCount == 9 &&
                                         result.bossPhaseSpriteSetCount == 2 &&
                                         result.bossPhaseStateSpriteCount == 24 &&
                                         result.maxHeldWeaponAttackTrailViews > 0 &&
                                         result.actorViewPoolHitCount > 0 &&
                                         result.actorViewAcquireCount > result.createdActorViewCount &&
                                         result.totalHitRequestCount > 0 &&
                                         result.totalDeathRequestCount > 0 &&
                                         result.totalPickupRequestCount > 0 &&
                                         result.formalVfxSpawnCount > 0 &&
                                         result.usesTiltedOrthographicCamera && result.usesXzGroundPlane &&
                                         result.raisedMapGeometryCount > 0 &&
                                         result.mapGroundShadowCount > 0 &&
                                         result.formalMapGroundTileCount > 0 && result.formalMapPropCount > 0;
            result.passedAutomaticGate = sharedAutomaticGate && (!g42 ||
                                         result.centralArenaTransitionCount == 14 &&
                                         result.regionTransitionDecalCount == 32 &&
                                         result.regionIdentityClusterCount == 5 &&
                                         result.formalMapMarkerCount == 8 &&
                                         result.formalMapMarkerStateSpriteCount == 24 &&
                                         result.maxVisibleFormalMapMarkers > 0 &&
                                         result.playerOutlineObserved && result.playerRimObserved &&
                                         result.densePickupPresentationObserved &&
                                         result.maxDensityGroupedPickupViews > 0 &&
                                         result.maxDensityEmphasisPickupViews > 0 &&
                                         result.maxActorViews >= 103 && result.maxPickupViews >= 268 &&
                                         result.maxActiveVfx >= 42 &&
                                         result.majorSkillNonColorSignatureCount == 6 &&
                                         result.begunStagedVfxSequenceCount > 0 &&
                                         result.completedStagedVfxSequenceCount > 0 &&
                                         result.anticipationVfxStageCount > 0 &&
                                         result.launchVfxStageCount > 0 &&
                                         result.travelVfxStageCount > 0 &&
                                         result.impactVfxStageCount > 0 &&
                                         result.residueVfxStageCount > 0 &&
                                         result.droppedCriticalVfxRequestCount == 0 &&
                                         result.droppedCriticalStagedVfxSequenceCount == 0 &&
                                         result.droppedCriticalAudioRequestCount == 0 &&
                                         result.presentationHitStopCount > 0 &&
                                         result.cameraImpulseRequestCount > 0 &&
                                         result.peakActiveAudio > 0 &&
                                         result.reducedMotionAlternativeObserved &&
                                         (finalAcceptance || result.reducedMotionStageSpawnCount > 0) &&
                                         result.accessibilityScreenshotCount == 8 &&
                                         !result.accessibilityTextOverflowObserved &&
                                         result.grayscaleReviewScreenshotCount == 1) &&
                                         (!finalAcceptance ||
                                          Math.Abs(result.acceptanceSimulationScale - 1d) < 0.0001d &&
                                          result.simulationSeconds >= 720d &&
                                          result.maxBossPhaseViews > 0 &&
                                          result.maximumObservedBossPhase >= 2 &&
                                          result.bossPhaseObservationSamples > 0 &&
                                          result.renderRecordersAvailable &&
                                          result.wallFrameSampleCount >= 10000 &&
                                          result.averageFps >= 30d &&
                                          result.onePercentLowFps >= 20d &&
                                          result.gpuFrameSampleCount > 0 &&
                                          result.gpuFrameP99Milliseconds <= 33.34d &&
                                          result.managedMemoryGrowthBytes <= 268435456L &&
                                          result.finalTotalAllocatedMemoryBytes <= 1610612736L &&
                                          result.generation2Collections < 120);
            result.status = result.passedAutomaticGate ? "PASS" : "FAIL";
            result.error = result.passedAutomaticGate
                ? string.Empty
                : "One or more " + result.milestone + " visual acceptance assertions failed.";
            WriteAndQuit(result, result.passedAutomaticGate ? 0 : 2, resultVariable, g42);
        }

        private static bool HasArgument(string expected)
        {
            var arguments = Environment.GetCommandLineArgs();
            for (var index = 0; index < arguments.Length; index++)
                if (string.Equals(arguments[index], expected, StringComparison.OrdinalIgnoreCase)) return true;
            return false;
        }

        private static int ClickFirstInteractableCard(QinglanRuntimeUiRoot ui)
        {
            if (ui == null) return 0;
            var buttons = ui.GetComponentsInChildren<Button>(false);
            for (var index = 0; index < buttons.Length; index++)
            {
                if (!buttons[index].interactable) continue;
                buttons[index].onClick.Invoke();
                return 1;
            }
            return 0;
        }

        private static void DriveActiveRun(
            QinglanDemoRuntimeHost host,
            double elapsed,
            double simulationScale,
            bool g42,
            ref long drivenSimulationTicks,
            ref int waypointIndex,
            QinglanG40VisualAcceptanceResult result,
            ISet<string> enemyProfiles,
            ISet<SpatialEntity> observedViews)
        {
            var targetSimulationTicks = CalculateTargetSimulationTickCount(elapsed, simulationScale);
            var safety = 0;
            while (drivenSimulationTicks < targetSimulationTicks && safety++ < 64)
            {
                switch (host.Flow.Stage)
                {
                    case DemoFlowStage.Active:
                        host.SetVisualAcceptanceMovement(
                            ResolveWaypointMovement(host, g42, ref waypointIndex));
                        host.TickRuntime(SimulationClock.TickDurationSeconds);
                        drivenSimulationTicks++;
                        CollectMetrics(host, result, enemyProfiles, observedViews);
                        break;
                    case DemoFlowStage.UpgradePaused:
                        host.Flow.Execute(
                            QinglanUiCommand.SelectUpgrade,
                            "upgrade",
                            ChooseStableUpgradeIndex(host.Flow.Session));
                        host.TickRuntime(0d);
                        break;
                    case DemoFlowStage.RewardPaused:
                        host.Flow.Execute(
                            QinglanUiCommand.SelectReward,
                            "reward",
                            ChooseStableRewardIndex(host.Flow.Session));
                        host.TickRuntime(0d);
                        break;
                    default:
                        host.ClearVisualAcceptanceMovement();
                        host.TickRuntime(0d);
                        return;
                }
            }
        }

        internal static long CalculateTargetSimulationTickCount(double elapsed) =>
            CalculateTargetSimulationTickCount(elapsed, AcceptanceSimulationScale);

        internal static long CalculateTargetSimulationTickCount(double elapsed, double simulationScale)
        {
            if (elapsed <= 0d || simulationScale <= 0d) return 0L;
            return (long)Math.Floor(
                (elapsed * simulationScale / SimulationClock.TickDurationSeconds) + 0.000000001d);
        }

        internal static int ChooseStableUpgradeIndex(RunSession session)
        {
            var offers = session?.CurrentOffers;
            if (offers == null || offers.Count == 0) return 0;
            var selectedIndex = 0;
            var selectedId = offers.GetAt(0).Source.TargetContentId.Value;
            var selectedRank = UpgradePriorityRank(selectedId);
            for (var index = 1; index < offers.Count; index++)
            {
                var candidateId = offers.GetAt(index).Source.TargetContentId.Value;
                var candidateRank = UpgradePriorityRank(candidateId);
                if (candidateRank > selectedRank ||
                    (candidateRank == selectedRank && string.CompareOrdinal(candidateId, selectedId) >= 0))
                    continue;
                selectedId = candidateId;
                selectedRank = candidateRank;
                selectedIndex = index;
            }
            return selectedIndex;
        }

        private static int UpgradePriorityRank(string contentId)
        {
            for (var index = 0; index < G42UpgradePriority.Length; index++)
                if (string.Equals(contentId, G42UpgradePriority[index], StringComparison.Ordinal)) return index;
            return G42UpgradePriority.Length;
        }

        internal static int ChooseStableRewardIndex(RunSession session)
        {
            var choice = session?.CurrentRewardChoice;
            if (choice == null || choice.CandidateIds.Count == 0) return 0;
            var selectedIndex = 0;
            var selectedId = choice.CandidateIds[0].Value;
            for (var index = 1; index < choice.CandidateIds.Count; index++)
            {
                var candidateId = choice.CandidateIds[index].Value;
                if (string.CompareOrdinal(candidateId, selectedId) >= 0) continue;
                selectedId = candidateId;
                selectedIndex = index;
            }
            return selectedIndex;
        }

        private static Vector2 ResolveWaypointMovement(
            QinglanDemoRuntimeHost host,
            bool g42,
            ref int waypointIndex)
        {
            var session = host.Flow.Session;
            if (session == null || !session.RenderSnapshot.TryGet(session.Player, out var playerSnapshot))
                return Vector2.right;
            var position = new Vector2(
                playerSnapshot.CurrentPosition.X,
                playerSnapshot.CurrentPosition.Y);
            return ResolveWaypointMovement(position, g42, ref waypointIndex);
        }

        internal static Vector2 ResolveWaypointMovement(
            Vector2 position,
            bool g42,
            ref int waypointIndex)
        {
            var waypoints = g42 ? G42AcceptanceWaypoints : AcceptanceWaypoints;
            for (var attempts = 0; attempts < waypoints.Length; attempts++)
            {
                var delta = waypoints[waypointIndex] - position;
                if (delta.sqrMagnitude > 2.25f) return delta.normalized;
                if (g42 && waypointIndex == waypoints.Length - 1) return Vector2.zero;
                waypointIndex = (waypointIndex + 1) % waypoints.Length;
            }
            return g42 ? Vector2.zero : Vector2.right;
        }

        private static void CollectMetrics(
            QinglanDemoRuntimeHost host,
            QinglanG40VisualAcceptanceResult result,
            ISet<string> enemyProfiles,
            ISet<SpatialEntity> observedViews)
        {
            result.maxActiveViews = Math.Max(result.maxActiveViews, host.Presentation.ActiveViewCount);
            result.maxActorViews = Math.Max(result.maxActorViews, host.Presentation.ActiveActorViewCount);
            result.maxProjectileViews = Math.Max(result.maxProjectileViews, host.Presentation.ActiveProjectileViewCount);
            result.maxAreaViews = Math.Max(result.maxAreaViews, host.Presentation.ActiveAreaViewCount);
            result.maxPickupViews = Math.Max(result.maxPickupViews, host.Presentation.ActivePickupViewCount);
            result.maxActiveVfx = Math.Max(result.maxActiveVfx, host.Presentation.ActiveVfxCount);
            result.maxHeldWeaponViews = Math.Max(result.maxHeldWeaponViews, host.Presentation.HeldWeaponViewCount);
            var bossPhaseViews = host.Presentation.BossPhaseViewCount;
            result.maxBossPhaseViews = Math.Max(result.maxBossPhaseViews, bossPhaseViews);
            result.maximumObservedBossPhase = Math.Max(
                result.maximumObservedBossPhase,
                host.Presentation.MaximumAppliedBossPhase);
            if (bossPhaseViews > 0) result.bossPhaseObservationSamples++;
            result.maxHeldWeaponAttackTrailViews = Math.Max(
                result.maxHeldWeaponAttackTrailViews,
                host.Presentation.HeldWeaponAttackTrailViewCount);
            result.maxVisibleFormalMapMarkers = Math.Max(
                result.maxVisibleFormalMapMarkers,
                host.Presentation.VisibleFormalMapMarkerCount);
            result.maxCompletedFormalMapMarkers = Math.Max(
                result.maxCompletedFormalMapMarkers,
                host.Presentation.CompletedFormalMapMarkerCount);
            result.densePickupPresentationObserved |= host.Presentation.DensePickupPresentationActive;
            result.maxDensityGroupedPickupViews = Math.Max(
                result.maxDensityGroupedPickupViews,
                host.Presentation.DensityGroupedPickupViewCount);
            result.maxDensityEmphasisPickupViews = Math.Max(
                result.maxDensityEmphasisPickupViews,
                host.Presentation.DensityEmphasisPickupViewCount);
            var session = host.Flow.Session;
            if (session == null) return;
            if (host.Presentation.TryGetView(session.Player, out var playerView))
            {
                result.playerOutlineObserved |= playerView.QingciOutlineActive;
                result.playerRimObserved |= playerView.PlayerRimActive;
            }
            var snapshot = session.RenderSnapshot;
            for (var index = 0; index < snapshot.Count; index++)
            {
                var entity = snapshot.GetAt(index).Entity;
                observedViews.Add(entity);
                if (entity.Kind != EntityKind.Actor || entity == session.Player) continue;
                if (session.TryGetVisualProfileId(entity, out var profileId) && profileId.IsValid)
                    enemyProfiles.Add(profileId.Value);
            }
        }

        private static void RecordPerformanceSample(
            QinglanG40VisualAcceptanceResult result,
            double[] wallFrameSamples,
            ref int wallFrameSampleCount,
            ref double wallFrameTotalMilliseconds,
            double[] gpuFrameSamples,
            ref int gpuFrameSampleCount,
            ref double gpuFrameTotalMilliseconds,
            FrameTiming[] frameTiming)
        {
            var wallMilliseconds = Time.unscaledDeltaTime * 1000d;
            if (wallMilliseconds > 0d && wallFrameSampleCount < wallFrameSamples.Length)
            {
                wallFrameSamples[wallFrameSampleCount++] = wallMilliseconds;
                wallFrameTotalMilliseconds += wallMilliseconds;
            }
            if (FrameTimingManager.GetLatestTimings(1, frameTiming) > 0 &&
                frameTiming[0].gpuFrameTime > 0d && gpuFrameSampleCount < gpuFrameSamples.Length)
            {
                gpuFrameSamples[gpuFrameSampleCount++] = frameTiming[0].gpuFrameTime;
                gpuFrameTotalMilliseconds += frameTiming[0].gpuFrameTime;
            }
            FrameTimingManager.CaptureFrameTimings();
            result.peakMonoUsedBytes = Math.Max(result.peakMonoUsedBytes, Profiler.GetMonoUsedSizeLong());
            result.peakTotalAllocatedMemoryBytes = Math.Max(
                result.peakTotalAllocatedMemoryBytes,
                Profiler.GetTotalAllocatedMemoryLong());
        }

        private static void RecordRenderPerformanceSample(
            QinglanG40VisualAcceptanceResult result,
            ProfilerRecorder drawCallsRecorder,
            ProfilerRecorder setPassRecorder,
            ProfilerRecorder trianglesRecorder,
            ProfilerRecorder gcAllocatedRecorder)
        {
            result.drawCallsRecorderValid |= drawCallsRecorder.Valid;
            result.setPassRecorderValid |= setPassRecorder.Valid;
            result.trianglesRecorderValid |= trianglesRecorder.Valid;
            result.gcAllocatedRecorderValid |= gcAllocatedRecorder.Valid;
            if (drawCallsRecorder.Valid && setPassRecorder.Valid && trianglesRecorder.Valid)
            {
                var drawCalls = Math.Max(0L, drawCallsRecorder.CurrentValue);
                var setPass = Math.Max(0L, setPassRecorder.CurrentValue);
                var triangles = Math.Max(0L, trianglesRecorder.CurrentValue);
                result.renderRecorderSamples++;
                result.drawCallsTotal += drawCalls;
                result.setPassCallsTotal += setPass;
                result.maximumDrawCalls = Math.Max(result.maximumDrawCalls, drawCalls);
                result.maximumSetPassCalls = Math.Max(result.maximumSetPassCalls, setPass);
                result.maximumTriangles = Math.Max(result.maximumTriangles, triangles);
            }
            if (!gcAllocatedRecorder.Valid) return;
            var allocated = Math.Max(0L, gcAllocatedRecorder.CurrentValue);
            result.gcAllocatedRecorderSamples++;
            result.gcAllocatedInFrameBytes += allocated;
            result.maximumGcAllocatedInFrameBytes = Math.Max(
                result.maximumGcAllocatedInFrameBytes,
                allocated);
        }

        private static double Percentile99(double[] values, int count)
        {
            if (values == null || count <= 0) return 0d;
            Array.Sort(values, 0, count);
            var index = Math.Min(count - 1, Math.Max(0, (int)Math.Ceiling(count * 0.99d) - 1));
            return values[index];
        }

        private static IEnumerator CaptureG42AccessibilityEvidence(
            QinglanDemoRuntimeHost host,
            QinglanG40VisualAcceptanceResult result,
            string screenshotVariable)
        {
            var settings = host.Flow.Settings;
            if (host.Flow.Stage == DemoFlowStage.Active)
            {
                host.Flow.TogglePause();
                host.Flow.Execute(QinglanUiCommand.OpenSettings, "settings", 0);
                host.TickRuntime(0d);
            }
            settings.SetFontScale(1.5f);
            host.Ui.ApplyAccessibility(settings);
            host.TickRuntime(0d);
            SampleAccessibilityOverflow(host, result, "150-font");
            yield return CaptureScreenshot(result, "accessibility-150-font", screenshotVariable, true, false);

            settings.SetFontScale(QinglanUiTheme.MaximumFontScale);
            host.Ui.ApplyAccessibility(settings);
            host.TickRuntime(0d);
            SampleAccessibilityOverflow(host, result, "200-font");
            yield return CaptureScreenshot(result, "accessibility-200-font", screenshotVariable, true, false);

            if (host.Flow.Stage == DemoFlowStage.UserPaused)
            {
                host.Flow.Cancel();
                host.Flow.TogglePause();
                host.TickRuntime(0d);
            }

            var modes = new[]
            {
                ColorVisionMode.Protanopia,
                ColorVisionMode.Deuteranopia,
                ColorVisionMode.Tritanopia,
                ColorVisionMode.HighContrast
            };
            var names = new[]
            {
                "accessibility-protanopia",
                "accessibility-deuteranopia",
                "accessibility-tritanopia",
                "accessibility-high-contrast"
            };
            settings.SetFontScale(1f);
            for (var index = 0; index < modes.Length; index++)
            {
                settings.SetColorVision(modes[index]);
                host.Ui.ApplyAccessibility(settings);
                host.TickRuntime(0d);
                SampleAccessibilityOverflow(host, result, names[index]);
                yield return CaptureScreenshot(result, names[index], screenshotVariable, true, false);
            }

            settings.SetColorVision(ColorVisionMode.Standard);
            settings.SetScreenShakeEnabled(false);
            settings.SetFlashIntensity(0f);
            host.Ui.ApplyAccessibility(settings);
            host.TickRuntime(0d);
            for (var tick = 0; tick < 120; tick++)
            {
                host.TickRuntime(SimulationClock.TickDurationSeconds);
                yield return null;
            }
            EntityView reducedMotionPlayerView = null;
            var reducedMotionViewReady = host.Flow.Session != null &&
                                         host.Presentation.TryGetView(host.Flow.Session.Player, out reducedMotionPlayerView);
            result.reducedMotionAlternativeObserved =
                host.Presentation.ReducedMotionActive && reducedMotionViewReady &&
                reducedMotionPlayerView.ReducedMotionActive &&
                !reducedMotionPlayerView.HeldWeaponAttackTrailActive;
            SampleAccessibilityOverflow(host, result, "reduce-motion");
            yield return CaptureScreenshot(result, "accessibility-reduce-motion", screenshotVariable, true, false);

            settings.SetDamageNumbersEnabled(false);
            host.Ui.ApplyAccessibility(settings);
            host.TickRuntime(0d);
            SampleAccessibilityOverflow(host, result, "no-damage-numbers");
            yield return CaptureScreenshot(result, "accessibility-no-damage-numbers", screenshotVariable, true, false);
        }

        private static void SampleAccessibilityOverflow(
            QinglanDemoRuntimeHost host,
            QinglanG40VisualAcceptanceResult result,
            string stateName)
        {
            Canvas.ForceUpdateCanvases();
            if (!host.Ui.HasAnyTextOverflow) return;
            result.accessibilityTextOverflowObserved = true;
            if (!string.IsNullOrEmpty(result.accessibilityOverflowDiagnostic)) return;
            var builder = new StringBuilder(2048);
            builder.Append(stateName);
            var texts = host.Ui.GetComponentsInChildren<TMP_Text>(true);
            for (var index = 0; index < texts.Length; index++)
            {
                var text = texts[index];
                if (!text.gameObject.activeInHierarchy) continue;
                text.ForceMeshUpdate();
                builder.Append(" | ").Append(text.name)
                    .Append(":rect=").Append(text.rectTransform.rect.height.ToString("0.0", CultureInfo.InvariantCulture))
                    .Append(",preferred=").Append(text.preferredHeight.ToString("0.0", CultureInfo.InvariantCulture))
                    .Append(",overflow=").Append(text.isTextOverflowing);
            }
            result.accessibilityOverflowDiagnostic = builder.ToString();
        }
        private static IEnumerator CaptureGrayscaleReview(
            QinglanG40VisualAcceptanceResult result,
            string screenshotVariable)
        {
            var directory = Environment.GetEnvironmentVariable(screenshotVariable);
            if (string.IsNullOrWhiteSpace(directory)) yield break;
            directory = Path.GetFullPath(directory);
            Directory.CreateDirectory(directory);
            var path = Path.Combine(directory, "review-grayscale.png");
            if (File.Exists(path)) File.Delete(path);
            Canvas.ForceUpdateCanvases();
            yield return new WaitForEndOfFrame();
            var texture = new Texture2D(Screen.width, Screen.height, TextureFormat.RGB24, false);
            try
            {
                texture.ReadPixels(new Rect(0f, 0f, Screen.width, Screen.height), 0, 0, false);
                texture.Apply(false, false);
                var pixels = texture.GetPixels32();
                for (var index = 0; index < pixels.Length; index++)
                {
                    var source = pixels[index];
                    var luminance = (byte)Mathf.Clamp(
                        Mathf.RoundToInt((source.r * 0.2126f) + (source.g * 0.7152f) + (source.b * 0.0722f)),
                        0,
                        255);
                    pixels[index] = new Color32(luminance, luminance, luminance, source.a);
                }
                texture.SetPixels32(pixels);
                texture.Apply(false, false);
                File.WriteAllBytes(path, texture.EncodeToPNG());
                if (File.Exists(path) && new FileInfo(path).Length > 0)
                    result.grayscaleReviewScreenshotCount++;
            }
            finally
            {
                UnityEngine.Object.Destroy(texture);
            }
        }

        private static IEnumerator CaptureScreenshot(
            QinglanG40VisualAcceptanceResult result,
            string name,
            string screenshotVariable,
            bool accessibility,
            bool grayscale)
        {
            var directory = Environment.GetEnvironmentVariable(screenshotVariable);
            if (string.IsNullOrWhiteSpace(directory)) yield break;
            directory = Path.GetFullPath(directory);
            Directory.CreateDirectory(directory);
            var path = Path.Combine(directory, name + ".png");
            if (File.Exists(path)) File.Delete(path);
            Canvas.ForceUpdateCanvases();
            yield return new WaitForEndOfFrame();
            ScreenCapture.CaptureScreenshot(path);
            for (var frame = 0; frame < 180; frame++)
            {
                if (File.Exists(path) && new FileInfo(path).Length > 0)
                {
                    if (grayscale) result.grayscaleReviewScreenshotCount++;
                    else if (accessibility) result.accessibilityScreenshotCount++;
                    else result.screenshotCount++;
                    yield break;
                }
                yield return null;
            }
        }

        private static bool CaptureCameraMetrics(QinglanG40VisualAcceptanceResult result)
        {
            var camera = Camera.main;
            if (camera == null) camera = FindFirstObjectByType<Camera>();
            if (camera == null) return false;
            result.cameraOrthographic = camera.orthographic;
            result.cameraTiltDegrees = camera.transform.eulerAngles.x;
            return camera.orthographic && result.cameraTiltDegrees > 20f && result.cameraTiltDegrees < 80f;
        }

        private static void Finish(
            QinglanG40VisualAcceptanceResult result,
            string error,
            string resultVariable,
            bool g42)
        {
            result.status = "FAIL";
            result.error = error;
            WriteAndQuit(result, 2, resultVariable, g42);
        }

        private static void WriteAndQuit(
            QinglanG40VisualAcceptanceResult result,
            int exitCode,
            string resultVariable,
            bool g42)
        {
            try
            {
                var path = Environment.GetEnvironmentVariable(resultVariable);
                if (string.IsNullOrWhiteSpace(path))
                    path = Path.Combine(
                        UnityEngine.Application.persistentDataPath,
                        result.milestone == "G4.2-F"
                            ? "QinglanG42FinalAcceptance.json"
                            : g42 ? "QinglanG42VisualAcceptance.json" : "QinglanG40VisualAcceptance.json");
                path = Path.GetFullPath(path);
                var directory = Path.GetDirectoryName(path);
                if (string.IsNullOrEmpty(directory))
                    throw new InvalidOperationException("Invalid " + result.milestone + " result path.");
                Directory.CreateDirectory(directory);
                File.WriteAllText(path, JsonUtility.ToJson(result, true) + "\n");
                var marker = result.milestone == "G4.2-F"
                    ? "[Qinglan G4.2-F Final Acceptance]"
                    : g42 ? "[Qinglan G4.2-A Visual Acceptance]" : "[Qinglan G4.0 Visual Acceptance]";
                if (exitCode == 0) Debug.Log(marker + " PASS: " + path);
                else Debug.LogError(marker + " FAIL: " + result.error);
            }
            catch (Exception exception)
            {
                Debug.LogException(exception);
                exitCode = 3;
            }
            UnityEngine.Application.Quit(exitCode);
        }

        [Serializable]
        private sealed class QinglanG40VisualAcceptanceResult
        {
            public int schemaVersion;
            public string milestone;
            public string status;
            public string error;
            public string generatedAtUtc;
            public bool passedAutomaticGate;
            public bool humanVisualSignoff;
            public string runSeed;
            public string rewardSeed;
            public double requiredWallClockSeconds;
            public double acceptanceSimulationScale;
            public double wallClockSeconds;
            public double simulationSeconds;
            public int screenWidth;
            public int screenHeight;
            public string graphicsDeviceName;
            public string graphicsDeviceType;
            public int graphicsMemorySizeMegabytes;
            public string processorType;
            public int processorCount;
            public int systemMemorySizeMegabytes;
            public string operatingSystem;
            public string qualityLevel;
            public int vSyncCount;
            public bool formalVisualsLoaded;
            public bool formalAudioLoaded;
            public bool formalFontsLoaded;
            public int realCardClicks;
            public int screenshotCount;
            public int accessibilityScreenshotCount;
            public bool accessibilityTextOverflowObserved;
            public string accessibilityOverflowDiagnostic;
            public int grayscaleReviewScreenshotCount;
            public string[] distinctEnemyProfileIds;
            public int distinctEnemyProfileCount;
            public int totalObservedViews;
            public int maxActiveViews;
            public int maxActorViews;
            public int maxProjectileViews;
            public int maxAreaViews;
            public int maxPickupViews;
            public int maxActiveVfx;
            public int maxHeldWeaponViews;
            public int maxHeldWeaponAttackTrailViews;
            public int directionalSpriteSetCount;
            public int bossPhaseSpriteSetCount;
            public int bossPhaseStateSpriteCount;
            public int maxBossPhaseViews;
            public int maximumObservedBossPhase = -1;
            public int bossPhaseObservationSamples;
            public int createdActorViewCount;
            public long actorViewAcquireCount;
            public long actorViewPoolHitCount;
            public long actorViewPoolExpansionCount;
            public bool playerOutlineObserved;
            public bool playerRimObserved;
            public bool densePickupPresentationObserved;
            public int maxDensityGroupedPickupViews;
            public int maxDensityEmphasisPickupViews;
            public long projectileTrailSpawnCount;
            public long directionalAnimationFrameChangeCount;
            public long totalHitRequestCount;
            public long totalDeathRequestCount;
            public long totalStatusRequestCount;
            public long totalPickupRequestCount;
            public long formalVfxSpawnCount;
            public int createdVfxCount;
            public int peakActiveVfx;
            public long droppedVfxRequestCount;
            public long droppedCriticalVfxRequestCount;
            public long evictedLowerPriorityVfxCount;
            public long mergedCriticalVfxCount;
            public int majorSkillNonColorSignatureCount;
            public long begunStagedVfxSequenceCount;
            public long completedStagedVfxSequenceCount;
            public long droppedStagedVfxSequenceCount;
            public long droppedCriticalStagedVfxSequenceCount;
            public long evictedLowerPriorityStagedVfxSequenceCount;
            public long mergedCriticalStagedVfxSequenceCount;
            public long reducedMotionStageSpawnCount;
            public long anticipationVfxStageCount;
            public long launchVfxStageCount;
            public long travelVfxStageCount;
            public long impactVfxStageCount;
            public long residueVfxStageCount;
            public bool reducedMotionAlternativeObserved;
            public long presentationHitStopCount;
            public long cameraImpulseRequestCount;
            public int peakActiveAudio;
            public long droppedAudioRequestCount;
            public long droppedCriticalAudioRequestCount;
            public long suppressedAudioCooldownCount;
            public long evictedLowerPriorityAudioCount;
            public long mergedCriticalAudioCount;
            public int missingProfileFallbackCount;
            public bool usesTiltedOrthographicCamera;
            public bool cameraOrthographic;
            public float cameraTiltDegrees;
            public bool usesXzGroundPlane;
            public int raisedMapGeometryCount;
            public int mapGroundShadowCount;
            public int formalMapGroundTileCount;
            public int formalMapPropCount;
            public int centralArenaTransitionCount;
            public int regionTransitionDecalCount;
            public int regionIdentityClusterCount;
            public int formalMapMarkerCount;
            public int formalMapMarkerStateSpriteCount;
            public int maxVisibleFormalMapMarkers;
            public int maxCompletedFormalMapMarkers;
            public int wallFrameSampleCount;
            public int gpuFrameSampleCount;
            public double wallFrameAverageMilliseconds;
            public double wallFrameP99Milliseconds;
            public double averageFps;
            public double onePercentLowFps;
            public double gpuFrameAverageMilliseconds;
            public double gpuFrameP99Milliseconds;
            public bool drawCallsRecorderValid;
            public bool setPassRecorderValid;
            public bool trianglesRecorderValid;
            public bool gcAllocatedRecorderValid;
            public bool renderRecordersAvailable;
            public long renderRecorderSamples;
            public long drawCallsTotal;
            public long setPassCallsTotal;
            public double averageDrawCalls;
            public long maximumDrawCalls;
            public double averageSetPassCalls;
            public long maximumSetPassCalls;
            public long maximumTriangles;
            public long gcAllocatedRecorderSamples;
            public long gcAllocatedInFrameBytes;
            public long maximumGcAllocatedInFrameBytes;
            public long initialMonoUsedBytes;
            public long peakMonoUsedBytes;
            public long finalMonoUsedBytes;
            public long managedMemoryGrowthBytes;
            public long initialTotalAllocatedMemoryBytes;
            public long peakTotalAllocatedMemoryBytes;
            public long finalTotalAllocatedMemoryBytes;
            public long totalAllocatedMemoryGrowthBytes;
            public int generation0Collections;
            public int generation1Collections;
            public int generation2Collections;
        }
    }
}
