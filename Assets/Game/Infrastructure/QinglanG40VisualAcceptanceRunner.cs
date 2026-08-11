using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using Game.Application;
using Game.Simulation;
using Game.UI;
using UnityEngine;
using UnityEngine.Profiling;
using UnityEngine.UI;

namespace Game.Infrastructure
{
    /// <summary>
    /// Opt-in visible Player driver for the G4.0 sixty-second visual acceptance gate.
    /// It exercises one real card click, the production flow, simulation and presentation,
    /// and captures five rendered frames without introducing a second gameplay state path.
    /// </summary>
    internal sealed class QinglanG40VisualAcceptanceRunner : MonoBehaviour
    {
        private const string Argument = "-qinglanG40VisualAcceptance";
        private const string ResultVariable = "QINGLAN_G40_VISUAL_RESULT";
        private const string ScreenshotVariable = "QINGLAN_G40_SCREENSHOT_DIR";
        private const double RequiredWallClockSeconds = 60d;
        private const double AcceptanceSimulationScale = 3.25d;
        private static readonly double[] ScreenshotTimes = { 0.5d, 15d, 30d, 45d, 60d };
        private static readonly string[] ScreenshotNames =
        {
            "00-enter-combat", "15-seconds", "30-seconds", "45-seconds", "60-seconds"
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

        internal static bool IsRequested()
        {
            var arguments = Environment.GetCommandLineArgs();
            for (var index = 0; index < arguments.Length; index++)
                if (string.Equals(arguments[index], Argument, StringComparison.OrdinalIgnoreCase)) return true;
            return false;
        }

        private IEnumerator Start()
        {
            yield return null;
            var host = GetComponent<QinglanDemoRuntimeHost>();
            var result = new QinglanG40VisualAcceptanceResult
            {
                schemaVersion = 1,
                generatedAtUtc = DateTime.UtcNow.ToString("O", CultureInfo.InvariantCulture),
                status = "FAIL",
                requiredWallClockSeconds = RequiredWallClockSeconds,
                acceptanceSimulationScale = AcceptanceSimulationScale,
                screenWidth = Screen.width,
                screenHeight = Screen.height
            };
            if (host == null)
            {
                Finish(result, "The Qinglan runtime host is missing.");
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
                !host.Flow.Execute(QinglanUiCommand.BeginRun, "begin", 0))
            {
                Finish(result, "The title card or production run preparation flow failed.");
                yield break;
            }

            host.TickRuntime(0d);
            host.TickRuntime(SimulationClock.TickDurationSeconds);
            if (host.Flow.Stage != DemoFlowStage.Active || host.Flow.Session == null)
            {
                Finish(result, "The active run did not start.");
                yield break;
            }

            host.enabled = false;
            var enemyProfiles = new HashSet<string>(StringComparer.Ordinal);
            var observedViews = new HashSet<SpatialEntity>();
            var wallFrameSamples = new double[8192];
            var gpuFrameSamples = new double[8192];
            var frameTiming = new FrameTiming[1];
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
            var drivenSimulationSeconds = 0d;
            var waypointIndex = 0;
            var nextScreenshot = 0;
            while (true)
            {
                var now = Time.realtimeSinceStartupAsDouble;
                var elapsed = now - startedAt;
                DriveActiveRun(host, elapsed, ref drivenSimulationSeconds, ref waypointIndex);
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

                while (nextScreenshot < ScreenshotTimes.Length &&
                       elapsed >= ScreenshotTimes[nextScreenshot])
                {
                    yield return CaptureScreenshot(result, ScreenshotNames[nextScreenshot]);
                    nextScreenshot++;
                }

                if (elapsed >= RequiredWallClockSeconds) break;
                yield return null;
            }

            host.ClearVisualAcceptanceMovement();
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
            result.gpuFrameAverageMilliseconds = gpuFrameSampleCount == 0
                ? 0d
                : gpuFrameTotalMilliseconds / gpuFrameSampleCount;
            result.gpuFrameP99Milliseconds = Percentile99(gpuFrameSamples, gpuFrameSampleCount);
            result.wallFrameSampleCount = wallFrameSampleCount;
            result.gpuFrameSampleCount = gpuFrameSampleCount;
            result.finalMonoUsedBytes = Profiler.GetMonoUsedSizeLong();
            result.finalTotalAllocatedMemoryBytes = Profiler.GetTotalAllocatedMemoryLong();
            result.generation0Collections = GC.CollectionCount(0) - generation0Start;
            result.generation1Collections = GC.CollectionCount(1) - generation1Start;
            result.generation2Collections = GC.CollectionCount(2) - generation2Start;
            result.usesTiltedOrthographicCamera = CaptureCameraMetrics(result);
            result.usesXzGroundPlane = host.Presentation.UsesXzGroundPlane;
            result.raisedMapGeometryCount = host.Presentation.RaisedMapGeometryCount;
            result.mapGroundShadowCount = host.Presentation.MapGroundShadowCount;
            result.formalMapGroundTileCount = host.Presentation.FormalMapGroundTileCount;
            result.formalMapPropCount = host.Presentation.FormalMapPropCount;
            result.projectileTrailSpawnCount = host.Presentation.ProjectileTrailSpawnCount;
            result.directionalAnimationFrameChangeCount =
                host.Presentation.DirectionalAnimationFrameChangeCount;
            result.totalHitRequestCount = host.Presentation.TotalHitRequestCount;
            result.totalDeathRequestCount = host.Presentation.TotalDeathRequestCount;
            result.totalStatusRequestCount = host.Presentation.TotalStatusRequestCount;
            result.formalVfxSpawnCount = host.Presentation.FormalVfxSpawnCount;
            result.createdVfxCount = host.Presentation.CreatedVfxCount;
            result.missingProfileFallbackCount = host.Presentation.MissingProfileFallbackCount;
            result.passedAutomaticGate = result.formalVisualsLoaded && result.formalAudioLoaded &&
                                         result.formalFontsLoaded && result.realCardClicks > 0 &&
                                         result.wallClockSeconds >= RequiredWallClockSeconds &&
                                         result.screenshotCount == ScreenshotTimes.Length &&
                                         result.distinctEnemyProfileCount >= 3 &&
                                         result.totalObservedViews >= result.maxActiveViews &&
                                         result.maxActorViews >= 4 && result.maxProjectileViews > 0 &&
                                         result.maxHeldWeaponViews > 0 &&
                                         result.projectileTrailSpawnCount > 0 &&
                                         result.directionalAnimationFrameChangeCount > 0 &&
                                         result.totalHitRequestCount > 0 &&
                                         result.totalDeathRequestCount > 0 &&
                                         result.formalVfxSpawnCount > 0 &&
                                         result.usesTiltedOrthographicCamera && result.usesXzGroundPlane &&
                                         result.raisedMapGeometryCount > 0 &&
                                         result.mapGroundShadowCount > 0 &&
                                         result.formalMapGroundTileCount > 0 && result.formalMapPropCount > 0;
            result.status = result.passedAutomaticGate ? "PASS" : "FAIL";
            result.error = result.passedAutomaticGate
                ? string.Empty
                : "One or more G4.0 sixty-second visual acceptance assertions failed.";
            WriteAndQuit(result, result.passedAutomaticGate ? 0 : 2);
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
            ref double drivenSimulationSeconds,
            ref int waypointIndex)
        {
            var targetSimulationSeconds = elapsed * AcceptanceSimulationScale;
            var safety = 0;
            while (drivenSimulationSeconds + 0.000001d < targetSimulationSeconds && safety++ < 64)
            {
                switch (host.Flow.Stage)
                {
                    case DemoFlowStage.Active:
                        host.SetVisualAcceptanceMovement(ResolveWaypointMovement(host, ref waypointIndex));
                        var delta = Math.Min(
                            SimulationClock.TickDurationSeconds * 4d,
                            targetSimulationSeconds - drivenSimulationSeconds);
                        host.TickRuntime(delta);
                        drivenSimulationSeconds += delta;
                        break;
                    case DemoFlowStage.UpgradePaused:
                        host.Flow.Execute(QinglanUiCommand.SelectUpgrade, "upgrade", 0);
                        host.TickRuntime(0d);
                        break;
                    case DemoFlowStage.RewardPaused:
                        host.Flow.Execute(QinglanUiCommand.SelectReward, "reward", 0);
                        host.TickRuntime(0d);
                        break;
                    default:
                        host.ClearVisualAcceptanceMovement();
                        host.TickRuntime(0d);
                        return;
                }
            }
        }

        private static Vector2 ResolveWaypointMovement(QinglanDemoRuntimeHost host, ref int waypointIndex)
        {
            var session = host.Flow.Session;
            if (session == null || !host.Presentation.TryGetView(session.Player, out var playerView))
                return Vector2.right;
            var position = Game.Presentation.PresentationSpace.ToSimulation(playerView.transform.position);
            for (var attempts = 0; attempts < AcceptanceWaypoints.Length; attempts++)
            {
                var delta = AcceptanceWaypoints[waypointIndex] - position;
                if (delta.sqrMagnitude > 2.25f) return delta.normalized;
                waypointIndex = (waypointIndex + 1) % AcceptanceWaypoints.Length;
            }
            return Vector2.right;
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
            var session = host.Flow.Session;
            if (session == null) return;
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

        private static double Percentile99(double[] values, int count)
        {
            if (values == null || count <= 0) return 0d;
            Array.Sort(values, 0, count);
            var index = Math.Min(count - 1, Math.Max(0, (int)Math.Ceiling(count * 0.99d) - 1));
            return values[index];
        }

        private static IEnumerator CaptureScreenshot(QinglanG40VisualAcceptanceResult result, string name)
        {
            var directory = Environment.GetEnvironmentVariable(ScreenshotVariable);
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
                    result.screenshotCount++;
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

        private static void Finish(QinglanG40VisualAcceptanceResult result, string error)
        {
            result.status = "FAIL";
            result.error = error;
            WriteAndQuit(result, 2);
        }

        private static void WriteAndQuit(QinglanG40VisualAcceptanceResult result, int exitCode)
        {
            try
            {
                var path = Environment.GetEnvironmentVariable(ResultVariable);
                if (string.IsNullOrWhiteSpace(path))
                    path = Path.Combine(UnityEngine.Application.persistentDataPath, "QinglanG40VisualAcceptance.json");
                path = Path.GetFullPath(path);
                var directory = Path.GetDirectoryName(path);
                if (string.IsNullOrEmpty(directory)) throw new InvalidOperationException("Invalid G4.0 result path.");
                Directory.CreateDirectory(directory);
                File.WriteAllText(path, JsonUtility.ToJson(result, true) + "\n");
                if (exitCode == 0) Debug.Log("[Qinglan G4.0 Visual Acceptance] PASS: " + path);
                else Debug.LogError("[Qinglan G4.0 Visual Acceptance] FAIL: " + result.error);
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
            public string status;
            public string error;
            public string generatedAtUtc;
            public bool passedAutomaticGate;
            public double requiredWallClockSeconds;
            public double acceptanceSimulationScale;
            public double wallClockSeconds;
            public double simulationSeconds;
            public int screenWidth;
            public int screenHeight;
            public bool formalVisualsLoaded;
            public bool formalAudioLoaded;
            public bool formalFontsLoaded;
            public int realCardClicks;
            public int screenshotCount;
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
            public long projectileTrailSpawnCount;
            public long directionalAnimationFrameChangeCount;
            public long totalHitRequestCount;
            public long totalDeathRequestCount;
            public long totalStatusRequestCount;
            public long formalVfxSpawnCount;
            public int createdVfxCount;
            public int missingProfileFallbackCount;
            public bool usesTiltedOrthographicCamera;
            public bool cameraOrthographic;
            public float cameraTiltDegrees;
            public bool usesXzGroundPlane;
            public int raisedMapGeometryCount;
            public int mapGroundShadowCount;
            public int formalMapGroundTileCount;
            public int formalMapPropCount;
            public int wallFrameSampleCount;
            public int gpuFrameSampleCount;
            public double wallFrameAverageMilliseconds;
            public double wallFrameP99Milliseconds;
            public double gpuFrameAverageMilliseconds;
            public double gpuFrameP99Milliseconds;
            public long initialMonoUsedBytes;
            public long peakMonoUsedBytes;
            public long finalMonoUsedBytes;
            public long initialTotalAllocatedMemoryBytes;
            public long peakTotalAllocatedMemoryBytes;
            public long finalTotalAllocatedMemoryBytes;
            public int generation0Collections;
            public int generation1Collections;
            public int generation2Collections;
        }
    }
}
