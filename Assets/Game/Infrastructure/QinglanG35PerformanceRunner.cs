using System;
using System.Collections;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using Game.Core;
using Game.Simulation;
using Unity.Profiling;
using UnityEngine;
using UnityEngine.Profiling;
using Debug = UnityEngine.Debug;

namespace Game.Infrastructure
{
    /// <summary>Opt-in, real-rendering G3.5 Development Player performance gate.</summary>
    internal sealed class QinglanG35PerformanceRunner : MonoBehaviour
    {
        private const string Argument = "-qinglanG35Performance";
        private const string DefaultSeed = "5562308243845666138";
        private const int MemorySampleIntervalTicks = 60 * SimulationClock.TickRate;
        private readonly FrameTiming[] latestFrameTiming = new FrameTiming[1];
        private QinglanDemoRuntimeHost host;
        private M10StressScenario scenario;
        private QinglanG35FormalPresentationProbe presentation;
        private QinglanG35PerformanceReport report;
        private double[] wallFrameSamples;
        private double[] cpuFrameSamples;
        private double[] gpuFrameSamples;
        private double[] tickSamples;
        private QinglanG35MemorySample[] memorySamples;
        private int wallFrameSampleCount;
        private int cpuFrameSampleCount;
        private int gpuFrameSampleCount;
        private int tickSampleCount;
        private int memorySampleCount;
        private int warmupTicks;
        private int measuredTicks;
        private int nextMemorySampleTick;
        private double tickAccumulator;
        private bool initialized;
        private bool measuring;
        private bool measurementWindowStarted;
        private bool finished;
        private bool recordersDisposed;
        private long gc0Start;
        private long gc1Start;
        private long gc2Start;
        private long hotPathManagedAllocationBytes;
        private long renderRecorderSamples;
        private long drawCallsTotal;
        private long setPassTotal;
        private long maximumDrawCalls;
        private long maximumSetPassCalls;
        private long maximumTriangles;
        private long profilerGcSamples;
        private long profilerGcAllocatedBytes;
        private long profilerGcMaximumBytes;
        private int measurementSettlingFrames;
        private ProfilerRecorder gcAllocatedRecorder;
        private ProfilerRecorder drawCallsRecorder;
        private ProfilerRecorder setPassRecorder;
        private ProfilerRecorder trianglesRecorder;

        internal static bool IsRequested()
        {
            var arguments = Environment.GetCommandLineArgs();
            for (var index = 0; index < arguments.Length; index++)
                if (string.Equals(arguments[index], Argument, StringComparison.OrdinalIgnoreCase)) return true;
            return false;
        }

        private IEnumerator Start()
        {
            ConfigurePlayer();
            for (var attempt = 0; attempt < 600 &&
                 (Screen.width != 1920 || Screen.height != 1080); attempt++)
                yield return null;

            try
            {
                InitializeProbe();
            }
            catch (Exception exception)
            {
                FinishWithError(exception.GetType().Name + ": " + exception.Message, 3);
            }
        }

        private void Update()
        {
            if (!initialized || finished) return;
            try
            {
                TickFrame();
            }
            catch (Exception exception)
            {
                FinishWithError(exception.GetType().Name + ": " + exception.Message, 3);
            }
        }

        private void ConfigurePlayer()
        {
            UnityEngine.Application.runInBackground = true;
            UnityEngine.Application.targetFrameRate = 60;
            QualitySettings.vSyncCount = 0;
            var qualityNames = QualitySettings.names;
            for (var index = 0; index < qualityNames.Length; index++)
            {
                if (!string.Equals(qualityNames[index], "Ultra", StringComparison.Ordinal)) continue;
                QualitySettings.SetQualityLevel(index, true);
                break;
            }
            Screen.fullScreen = false;
            Screen.SetResolution(
                1920,
                1080,
                FullScreenMode.Windowed,
                new RefreshRate { numerator = 60, denominator = 1 });
        }

        private void InitializeProbe()
        {
            host = GetComponent<QinglanDemoRuntimeHost>();
            var bootstrap = GetComponent<GameBootstrapper>();
            if (host == null || bootstrap?.Application == null)
                throw new InvalidOperationException("The initialized Qinglan composition root is missing.");
            if (!host.FormalVisualsLoaded || !host.FormalAudioLoaded || !host.FormalFontsLoaded ||
                host.FormalVisualCatalog == null)
                throw new InvalidOperationException("Formal visual, audio, or font content did not load.");

            report = new QinglanG35PerformanceReport();
            report.configuration = ReadConfiguration();
            var enemyId = ContentId.Create("qinglan.enemy.grass_spirit");
            if (!enemyId.IsSuccess) throw new InvalidOperationException(enemyId.Error.ToString());
            var stressConfiguration = new M10StressConfiguration(
                ulong.Parse(report.configuration.seed, CultureInfo.InvariantCulture),
                report.configuration.tickCount,
                report.configuration.enemies,
                report.configuration.projectiles,
                report.configuration.pickups,
                report.configuration.vfx,
                report.configuration.warmupTicks);
            var scenarioResult = M10StressScenario.Create(
                bootstrap.Application.ContentRegistry,
                enemyId.Value,
                stressConfiguration);
            if (!scenarioResult.IsSuccess)
                throw new InvalidOperationException(scenarioResult.Error.ToString());
            scenario = scenarioResult.Value;

            presentation = new QinglanG35FormalPresentationProbe(
                transform,
                host.FormalVisualCatalog,
                report.configuration.enemies,
                report.configuration.projectiles,
                report.configuration.pickups,
                report.configuration.vfx);
            var frameCapacity = checked(report.configuration.tickCount * 3 + 1024);
            wallFrameSamples = new double[frameCapacity];
            cpuFrameSamples = new double[frameCapacity];
            gpuFrameSamples = new double[frameCapacity];
            tickSamples = new double[report.configuration.tickCount];
            memorySamples = new QinglanG35MemorySample[
                (report.configuration.tickCount / MemorySampleIntervalTicks) + 3];
            for (var index = 0; index < memorySamples.Length; index++)
                memorySamples[index] = new QinglanG35MemorySample();

            gcAllocatedRecorder = ProfilerRecorder.StartNew(
                ProfilerCategory.Memory,
                "GC Allocated In Frame",
                1);
            drawCallsRecorder = ProfilerRecorder.StartNew(
                ProfilerCategory.Render,
                "Draw Calls Count",
                1);
            setPassRecorder = ProfilerRecorder.StartNew(
                ProfilerCategory.Render,
                "SetPass Calls Count",
                1);
            trianglesRecorder = ProfilerRecorder.StartNew(
                ProfilerCategory.Render,
                "Triangles Count",
                1);
            host.Presentation.Clear();
            host.enabled = false;
            if (host.Input != null) host.Input.enabled = false;
            FrameTimingManager.CaptureFrameTimings();
            if (report.configuration.warmupTicks == 0) BeginMeasurement();
            initialized = true;
            StartCoroutine(CaptureEndOfFrameMetrics());
        }

        private void TickFrame()
        {
            if (measuring && !measurementWindowStarted)
            {
                tickAccumulator = 0d;
                presentation.Sync(scenario.Snapshot, 0f, Time.frameCount);
                FrameTimingManager.CaptureFrameTimings();
                measurementSettlingFrames--;
                if (measurementSettlingFrames <= 0) StartMeasurementWindow();
                return;
            }
            if (measurementWindowStarted) RecordFrameMetrics();
            FrameTimingManager.CaptureFrameTimings();
            var delta = Time.unscaledDeltaTime;
            tickAccumulator += delta;
            var frameWasMeasuring = measurementWindowStarted;
            var allocationBefore = frameWasMeasuring ? GC.GetAllocatedBytesForCurrentThread() : 0L;
            while (tickAccumulator >= SimulationClock.TickDurationSeconds && !finished)
            {
                tickAccumulator -= SimulationClock.TickDurationSeconds;
                if (!measuring)
                {
                    scenario.AdvanceOneTick();
                    warmupTicks++;
                    if (warmupTicks >= report.configuration.warmupTicks)
                    {
                        BeginMeasurement();
                        tickAccumulator = 0d;
                        break;
                    }
                }
                else
                {
                    var start = Stopwatch.GetTimestamp();
                    scenario.AdvanceOneTick();
                    tickSamples[tickSampleCount++] = ElapsedMilliseconds(start);
                    measuredTicks++;
                }
            }

            var alpha = (float)(tickAccumulator / SimulationClock.TickDurationSeconds);
            presentation.Sync(scenario.Snapshot, alpha, Time.frameCount);
            if (frameWasMeasuring)
            {
                hotPathManagedAllocationBytes +=
                    GC.GetAllocatedBytesForCurrentThread() - allocationBefore;
                if (measuredTicks >= nextMemorySampleTick)
                {
                    CaptureMemory(measuredTicks / (double)SimulationClock.TickRate / 60d);
                    nextMemorySampleTick += MemorySampleIntervalTicks;
                }
                if (measuredTicks >= report.configuration.tickCount) Finish(0);
            }
        }

        private void BeginMeasurement()
        {
            GC.Collect();
            GC.WaitForPendingFinalizers();
            GC.Collect();
            measurementSettlingFrames = 5;
            measuring = true;
        }

        private void StartMeasurementWindow()
        {
            gcAllocatedRecorder.Reset();
            drawCallsRecorder.Reset();
            setPassRecorder.Reset();
            trianglesRecorder.Reset();
            gc0Start = GC.CollectionCount(0);
            gc1Start = GC.CollectionCount(1);
            gc2Start = GC.CollectionCount(2);
            nextMemorySampleTick = MemorySampleIntervalTicks;
            CaptureMemory(0d);
            measurementWindowStarted = true;
        }

        private void RecordFrameMetrics()
        {
            if (wallFrameSampleCount < wallFrameSamples.Length)
                wallFrameSamples[wallFrameSampleCount++] = Time.unscaledDeltaTime * 1000d;
            else report.render.frameSampleOverflow = true;

            var count = FrameTimingManager.GetLatestTimings(1, latestFrameTiming);
            if (count > 0)
            {
                var timing = latestFrameTiming[0];
                if (timing.cpuFrameTime > 0d && cpuFrameSampleCount < cpuFrameSamples.Length)
                    cpuFrameSamples[cpuFrameSampleCount++] = timing.cpuFrameTime;
                if (timing.gpuFrameTime > 0d && gpuFrameSampleCount < gpuFrameSamples.Length)
                    gpuFrameSamples[gpuFrameSampleCount++] = timing.gpuFrameTime;
            }

            if (!UnityEngine.Application.isFocused) report.environment.focusLost = true;
            if (gcAllocatedRecorder.Valid)
            {
                var allocated = Math.Max(0L, gcAllocatedRecorder.LastValue);
                profilerGcSamples++;
                profilerGcAllocatedBytes += allocated;
                if (allocated > profilerGcMaximumBytes) profilerGcMaximumBytes = allocated;
            }
        }

        private IEnumerator CaptureEndOfFrameMetrics()
        {
            var wait = new WaitForEndOfFrame();
            while (!finished)
            {
                yield return wait;
                if (!measurementWindowStarted || finished) continue;
                RecordRenderMetrics();
            }
        }

        private void RecordRenderMetrics()
        {
            if (drawCallsRecorder.Valid && setPassRecorder.Valid && trianglesRecorder.Valid)
            {
                var drawCalls = Math.Max(0L, drawCallsRecorder.CurrentValue);
                var setPass = Math.Max(0L, setPassRecorder.CurrentValue);
                var triangles = Math.Max(0L, trianglesRecorder.CurrentValue);
                renderRecorderSamples++;
                drawCallsTotal += drawCalls;
                setPassTotal += setPass;
                if (drawCalls > maximumDrawCalls) maximumDrawCalls = drawCalls;
                if (setPass > maximumSetPassCalls) maximumSetPassCalls = setPass;
                if (triangles > maximumTriangles) maximumTriangles = triangles;
            }
        }

        private void CaptureMemory(double simulatedMinute)
        {
            if (memorySampleCount >= memorySamples.Length) return;
            var sample = memorySamples[memorySampleCount++];
            sample.simulatedMinute = simulatedMinute;
            sample.managedBytes = Profiler.GetMonoUsedSizeLong();
            sample.nativeBytes = Profiler.GetTotalAllocatedMemoryLong();
            sample.gcHeapBytes = GC.GetTotalMemory(false);
            sample.graphicsDriverBytes = Profiler.GetAllocatedMemoryForGraphicsDriver();
        }

        private void Finish(int exitCode)
        {
            if (finished) return;
            finished = true;
            var measuredGeneration0Collections = (int)(GC.CollectionCount(0) - gc0Start);
            var measuredGeneration1Collections = (int)(GC.CollectionCount(1) - gc1Start);
            var measuredGeneration2Collections = (int)(GC.CollectionCount(2) - gc2Start);
            if (memorySampleCount == 0 ||
                memorySamples[memorySampleCount - 1].simulatedMinute <
                report.configuration.tickCount / (double)SimulationClock.TickRate / 60d)
                CaptureMemory(report.configuration.tickCount / (double)SimulationClock.TickRate / 60d);
            report.generatedAtUtc = DateTime.UtcNow.ToString("O", CultureInfo.InvariantCulture);
            report.wallFrame = QinglanG35PerformanceContract.CalculateTiming(
                wallFrameSamples,
                wallFrameSampleCount);
            report.cpuFrame = QinglanG35PerformanceContract.CalculateTiming(
                cpuFrameSamples,
                cpuFrameSampleCount);
            report.gpuFrame = QinglanG35PerformanceContract.CalculateTiming(
                gpuFrameSamples,
                gpuFrameSampleCount);
            report.simulationTick = QinglanG35PerformanceContract.CalculateTiming(
                tickSamples,
                tickSampleCount);
            report.memory = QinglanG35PerformanceContract.CalculateMemory(memorySamples, memorySampleCount);
            report.gc = new QinglanG35GcMetrics
            {
                generation0Collections = measuredGeneration0Collections,
                generation1Collections = measuredGeneration1Collections,
                generation2Collections = measuredGeneration2Collections,
                hotPathManagedAllocationBytes = hotPathManagedAllocationBytes,
                hotPathManagedBytesPerFrame = wallFrameSampleCount == 0 ? 0d :
                    hotPathManagedAllocationBytes / (double)wallFrameSampleCount,
                profilerSamples = (int)Math.Min(int.MaxValue, profilerGcSamples),
                profilerAllocatedBytes = profilerGcAllocatedBytes,
                profilerMaximumAllocatedBytesPerFrame = profilerGcMaximumBytes
            };
            report.render.drawCallsRecorderValid = drawCallsRecorder.Valid;
            report.render.setPassRecorderValid = setPassRecorder.Valid;
            report.render.trianglesRecorderValid = trianglesRecorder.Valid;
            report.render.samples = (int)Math.Min(int.MaxValue, renderRecorderSamples);
            report.render.averageDrawCalls = renderRecorderSamples == 0 ? 0d :
                drawCallsTotal / (double)renderRecorderSamples;
            report.render.maximumDrawCalls = maximumDrawCalls;
            report.render.averageSetPassCalls = renderRecorderSamples == 0 ? 0d :
                setPassTotal / (double)renderRecorderSamples;
            report.render.maximumSetPassCalls = maximumSetPassCalls;
            report.render.maximumTriangles = maximumTriangles;
            report.pools = presentation.CaptureMetrics();
            report.environment = CaptureEnvironment(report.environment.focusLost);
            report.checksum = scenario.CalculateChecksum().ToString("x16", CultureInfo.InvariantCulture);
            QinglanG35PerformanceContract.Evaluate(report);
            if (!string.Equals(report.status, "PASS", StringComparison.Ordinal)) exitCode = 2;
            WriteResult(report);
            if (exitCode == 0) Debug.Log("[Qinglan G3.5 Performance] PASS");
            else Debug.LogError("[Qinglan G3.5 Performance] FAIL: " + report.failureReason);
            DisposeRecorders();
            UnityEngine.Application.Quit(exitCode);
        }

        private void FinishWithError(string error, int exitCode)
        {
            if (finished) return;
            finished = true;
            if (report == null) report = new QinglanG35PerformanceReport();
            report.status = "FAIL";
            report.failureReason = error;
            report.generatedAtUtc = DateTime.UtcNow.ToString("O", CultureInfo.InvariantCulture);
            try
            {
                WriteResult(report);
            }
            catch (Exception writeException)
            {
                Debug.LogException(writeException);
                exitCode = 4;
            }
            Debug.LogError("[Qinglan G3.5 Performance] FAIL: " + error);
            DisposeRecorders();
            UnityEngine.Application.Quit(exitCode);
        }

        private QinglanG35Environment CaptureEnvironment(bool focusLost)
        {
            var device = SystemInfo.graphicsDeviceName ?? string.Empty;
            return new QinglanG35Environment
            {
                unityVersion = UnityEngine.Application.unityVersion,
                operatingSystem = SystemInfo.operatingSystem,
                processor = SystemInfo.processorType,
                processorCount = SystemInfo.processorCount,
                systemMemoryMegabytes = SystemInfo.systemMemorySize,
                graphicsDevice = device,
                graphicsDeviceType = SystemInfo.graphicsDeviceType.ToString(),
                graphicsDeviceVersion = SystemInfo.graphicsDeviceVersion,
                graphicsMemoryMegabytes = SystemInfo.graphicsMemorySize,
                screenWidth = Screen.width,
                screenHeight = Screen.height,
                quality = QualitySettings.names[QualitySettings.GetQualityLevel()],
                vSyncCount = QualitySettings.vSyncCount,
                batchMode = UnityEngine.Application.isBatchMode,
                frameTimingFeatureEnabled = FrameTimingManager.IsFeatureEnabled(),
                focusLost = focusLost,
                remoteDisplayAdapter = ContainsRemoteAdapterName(device),
                gitSha = Environment.GetEnvironmentVariable("QINGLAN_G35_GIT_SHA") ?? string.Empty,
                packVersion = Environment.GetEnvironmentVariable("QINGLAN_G35_PACK_VERSION") ?? string.Empty,
                packHash = Environment.GetEnvironmentVariable("QINGLAN_G35_PACK_HASH") ?? string.Empty
            };
        }

        private static QinglanG35Configuration ReadConfiguration()
        {
            var profile = (Environment.GetEnvironmentVariable("QINGLAN_G35_PROFILE") ?? "target")
                .Trim().ToLowerInvariant();
            if (profile == "target")
                return CreateConfiguration(profile, 54000, 1200, 900, 500, 200, true);
            if (profile == "extension")
                return CreateConfiguration(profile, 9000, 2000, 900, 500, 200, false);
            if (profile != "quick")
                throw new InvalidOperationException("QINGLAN_G35_PROFILE must be target, extension, or quick.");
            return CreateConfiguration(
                profile,
                ReadPositiveInt("QINGLAN_G35_TICKS", 300),
                ReadPositiveInt("QINGLAN_G35_ENEMIES", 120),
                ReadPositiveInt("QINGLAN_G35_PROJECTILES", 90),
                ReadPositiveInt("QINGLAN_G35_PICKUPS", 50),
                ReadPositiveInt("QINGLAN_G35_VFX", 20),
                false,
                ReadNonNegativeInt("QINGLAN_G35_WARMUP_TICKS", 30));
        }

        private static QinglanG35Configuration CreateConfiguration(
            string profile,
            int ticks,
            int enemies,
            int projectiles,
            int pickups,
            int vfx,
            bool certificationEligible,
            int warmupTicksValue = 300)
        {
            var seed = Environment.GetEnvironmentVariable("QINGLAN_G35_SEED");
            if (string.IsNullOrWhiteSpace(seed)) seed = DefaultSeed;
            if (!ulong.TryParse(seed, NumberStyles.Integer, CultureInfo.InvariantCulture, out _))
                throw new InvalidOperationException("QINGLAN_G35_SEED is not an unsigned integer.");
            return new QinglanG35Configuration
            {
                profile = profile,
                seed = seed,
                warmupTicks = warmupTicksValue,
                tickCount = ticks,
                enemies = enemies,
                projectiles = projectiles,
                pickups = pickups,
                vfx = vfx,
                simulationHz = SimulationClock.TickRate,
                targetFrameRate = 60,
                certificationEligible = certificationEligible
            };
        }

        private static int ReadPositiveInt(string name, int fallback)
        {
            var value = Environment.GetEnvironmentVariable(name);
            if (string.IsNullOrWhiteSpace(value)) return fallback;
            var parsed = int.Parse(value, NumberStyles.Integer, CultureInfo.InvariantCulture);
            if (parsed <= 0) throw new InvalidOperationException(name + " must be positive.");
            return parsed;
        }

        private static int ReadNonNegativeInt(string name, int fallback)
        {
            var value = Environment.GetEnvironmentVariable(name);
            if (string.IsNullOrWhiteSpace(value)) return fallback;
            var parsed = int.Parse(value, NumberStyles.Integer, CultureInfo.InvariantCulture);
            if (parsed < 0) throw new InvalidOperationException(name + " must not be negative.");
            return parsed;
        }

        private static bool ContainsRemoteAdapterName(string value)
        {
            return value.IndexOf("remote", StringComparison.OrdinalIgnoreCase) >= 0 ||
                   value.IndexOf("virtual", StringComparison.OrdinalIgnoreCase) >= 0 ||
                   value.IndexOf("todesk", StringComparison.OrdinalIgnoreCase) >= 0 ||
                   value.IndexOf("oray", StringComparison.OrdinalIgnoreCase) >= 0;
        }

        private static double ElapsedMilliseconds(long startTimestamp) =>
            (Stopwatch.GetTimestamp() - startTimestamp) * 1000d / Stopwatch.Frequency;

        private static void WriteResult(QinglanG35PerformanceReport value)
        {
            var path = Environment.GetEnvironmentVariable("QINGLAN_G35_OUTPUT");
            if (string.IsNullOrWhiteSpace(path))
                path = Path.Combine(
                    UnityEngine.Application.persistentDataPath,
                    "QinglanG35Performance.json");
            path = Path.GetFullPath(path);
            var directory = Path.GetDirectoryName(path);
            if (string.IsNullOrEmpty(directory)) throw new InvalidOperationException("Invalid G3.5 output path.");
            Directory.CreateDirectory(directory);
            File.WriteAllText(path, JsonUtility.ToJson(value, true) + "\n");
        }

        private void DisposeRecorders()
        {
            if (recordersDisposed) return;
            if (gcAllocatedRecorder.Valid) gcAllocatedRecorder.Dispose();
            if (drawCallsRecorder.Valid) drawCallsRecorder.Dispose();
            if (setPassRecorder.Valid) setPassRecorder.Dispose();
            if (trianglesRecorder.Valid) trianglesRecorder.Dispose();
            recordersDisposed = true;
        }

        private void OnDestroy()
        {
            DisposeRecorders();
            presentation?.Dispose();
            presentation = null;
        }
    }
}
