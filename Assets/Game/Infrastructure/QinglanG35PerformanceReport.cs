using System;
using System.Text;

namespace Game.Infrastructure
{
    [Serializable]
    internal sealed class QinglanG35PerformanceReport
    {
        public int schemaVersion = 1;
        public string status = "FAIL";
        public string failureReason = string.Empty;
        public string generatedAtUtc = string.Empty;
        public QinglanG35Configuration configuration = new QinglanG35Configuration();
        public QinglanG35Environment environment = new QinglanG35Environment();
        public QinglanG35TimingMetrics wallFrame = new QinglanG35TimingMetrics();
        public QinglanG35TimingMetrics cpuFrame = new QinglanG35TimingMetrics();
        public QinglanG35TimingMetrics gpuFrame = new QinglanG35TimingMetrics();
        public QinglanG35TimingMetrics simulationTick = new QinglanG35TimingMetrics();
        public QinglanG35MemoryMetrics memory = new QinglanG35MemoryMetrics();
        public QinglanG35GcMetrics gc = new QinglanG35GcMetrics();
        public QinglanG35RenderMetrics render = new QinglanG35RenderMetrics();
        public QinglanG35PoolMetrics pools = new QinglanG35PoolMetrics();
        public QinglanG35BudgetResult budgets = new QinglanG35BudgetResult();
        public string checksum = string.Empty;
    }

    [Serializable]
    internal sealed class QinglanG35Configuration
    {
        public string profile = string.Empty;
        public string seed = string.Empty;
        public int warmupTicks;
        public int tickCount;
        public int enemies;
        public int projectiles;
        public int pickups;
        public int vfx;
        public int simulationHz;
        public int targetFrameRate;
        public bool certificationEligible;
    }

    [Serializable]
    internal sealed class QinglanG35Environment
    {
        public string unityVersion = string.Empty;
        public string operatingSystem = string.Empty;
        public string processor = string.Empty;
        public int processorCount;
        public int systemMemoryMegabytes;
        public string graphicsDevice = string.Empty;
        public string graphicsDeviceType = string.Empty;
        public string graphicsDeviceVersion = string.Empty;
        public int graphicsMemoryMegabytes;
        public int screenWidth;
        public int screenHeight;
        public string quality = string.Empty;
        public int vSyncCount;
        public bool batchMode;
        public bool frameTimingFeatureEnabled;
        public bool focusLost;
        public bool remoteDisplayAdapter;
        public string gitSha = string.Empty;
        public string packVersion = string.Empty;
        public string packHash = string.Empty;
    }

    [Serializable]
    internal sealed class QinglanG35TimingMetrics
    {
        public int samples;
        public double averageMilliseconds;
        public double p95Milliseconds;
        public double p99Milliseconds;
        public double maximumMilliseconds;
        public double averageFps;
        public double onePercentLowFps;
    }

    [Serializable]
    internal sealed class QinglanG35MemorySample
    {
        public double simulatedMinute;
        public long managedBytes;
        public long nativeBytes;
        public long gcHeapBytes;
        public long graphicsDriverBytes;
    }

    [Serializable]
    internal sealed class QinglanG35MemoryMetrics
    {
        public QinglanG35MemorySample[] samples = Array.Empty<QinglanG35MemorySample>();
        public long peakManagedBytes;
        public long peakNativeBytes;
        public long peakGcHeapBytes;
        public long peakGraphicsDriverBytes;
        public long managedGrowthBytes;
        public long nativeGrowthBytes;
        public bool managedSustainedGrowth;
        public bool nativeSustainedGrowth;
    }

    [Serializable]
    internal sealed class QinglanG35GcMetrics
    {
        public int generation0Collections;
        public int generation1Collections;
        public int generation2Collections;
        public long hotPathManagedAllocationBytes;
        public double hotPathManagedBytesPerFrame;
        public int profilerSamples;
        public long profilerAllocatedBytes;
        public long profilerMaximumAllocatedBytesPerFrame;
    }

    [Serializable]
    internal sealed class QinglanG35RenderMetrics
    {
        public int samples;
        public bool drawCallsRecorderValid;
        public bool setPassRecorderValid;
        public bool trianglesRecorderValid;
        public double averageDrawCalls;
        public long maximumDrawCalls;
        public double averageSetPassCalls;
        public long maximumSetPassCalls;
        public long maximumTriangles;
        public bool frameSampleOverflow;
    }

    [Serializable]
    internal sealed class QinglanG35PoolMetrics
    {
        public int expectedViews;
        public int createdViews;
        public int expectedSpriteRenderers;
        public int createdSpriteRenderers;
        public int peakActiveViews;
        public int expansionsAfterWarmup;
        public int droppedRequests;
        public int invalidBindings;
        public int resolvedFormalProfiles;
    }

    [Serializable]
    internal sealed class QinglanG35BudgetResult
    {
        public double averageFpsMinimum = 59d;
        public double onePercentLowFpsMinimum = 45d;
        public double gpuP99BudgetMilliseconds = 16.67d;
        public double tickP99BudgetMilliseconds = 33.33d;
        public bool configurationMatchesProfile;
        public bool exactEntityCounts;
        public bool formalAssetsResolved;
        public bool averageFpsWithinBudget;
        public bool onePercentLowWithinBudget;
        public bool gpuP99WithinBudget;
        public bool tickP99WithinBudget;
        public bool gpuSamplesAvailable;
        public bool zeroHotPathManagedAllocation;
        public bool zeroProfilerGcAllocation;
        public bool noGcCollections;
        public bool noSustainedMemoryGrowth;
        public bool renderRecordersAvailable;
        public bool noPoolExpansionOrDrops;
        public bool correctResolution;
        public bool correctGraphicsApi;
        public bool correctQuality;
        public bool uninterruptedFocus;
        public bool physicalDisplayAdapter;
        public bool provenanceRecorded;
        public bool noFrameSampleOverflow;
    }

    internal static class QinglanG35PerformanceContract
    {
        internal static QinglanG35TimingMetrics CalculateTiming(double[] values, int count)
        {
            if (values == null) throw new ArgumentNullException(nameof(values));
            if (count <= 0 || count > values.Length) return new QinglanG35TimingMetrics();
            var total = 0d;
            var maximum = 0d;
            for (var index = 0; index < count; index++)
            {
                var value = values[index];
                total += value;
                if (value > maximum) maximum = value;
            }

            Array.Sort(values, 0, count);
            var average = total / count;
            var p99 = Percentile(values, count, 0.99d);
            return new QinglanG35TimingMetrics
            {
                samples = count,
                averageMilliseconds = average,
                p95Milliseconds = Percentile(values, count, 0.95d),
                p99Milliseconds = p99,
                maximumMilliseconds = maximum,
                averageFps = average <= 0d ? 0d : 1000d / average,
                onePercentLowFps = p99 <= 0d ? 0d : 1000d / p99
            };
        }

        internal static void Evaluate(QinglanG35PerformanceReport report)
        {
            if (report == null) throw new ArgumentNullException(nameof(report));
            var budgets = report.budgets ?? (report.budgets = new QinglanG35BudgetResult());
            budgets.configurationMatchesProfile = ConfigurationMatches(report.configuration);
            budgets.exactEntityCounts = report.pools.createdViews == report.pools.expectedViews &&
                                        report.pools.peakActiveViews == report.pools.expectedViews;
            budgets.formalAssetsResolved = report.pools.resolvedFormalProfiles >= 5 &&
                                           report.pools.createdSpriteRenderers ==
                                           report.pools.expectedSpriteRenderers;
            budgets.averageFpsWithinBudget = report.wallFrame.averageFps >= budgets.averageFpsMinimum;
            budgets.onePercentLowWithinBudget =
                report.wallFrame.onePercentLowFps >= budgets.onePercentLowFpsMinimum;
            budgets.gpuSamplesAvailable = report.environment.frameTimingFeatureEnabled &&
                                          report.gpuFrame.samples > 0;
            budgets.gpuP99WithinBudget = budgets.gpuSamplesAvailable &&
                                         report.gpuFrame.p99Milliseconds <=
                                         budgets.gpuP99BudgetMilliseconds;
            budgets.tickP99WithinBudget = report.simulationTick.samples == report.configuration.tickCount &&
                                          report.simulationTick.p99Milliseconds <=
                                          budgets.tickP99BudgetMilliseconds;
            budgets.zeroHotPathManagedAllocation = report.gc.hotPathManagedAllocationBytes == 0;
            budgets.zeroProfilerGcAllocation = report.gc.profilerSamples > 0 &&
                                               report.gc.profilerAllocatedBytes == 0;
            budgets.noGcCollections = report.gc.generation0Collections == 0 &&
                                      report.gc.generation1Collections == 0 &&
                                      report.gc.generation2Collections == 0;
            budgets.noSustainedMemoryGrowth = !report.memory.managedSustainedGrowth &&
                                              !report.memory.nativeSustainedGrowth;
            budgets.renderRecordersAvailable = report.render.samples > 0 &&
                                               report.render.maximumDrawCalls > 0 &&
                                               report.render.maximumTriangles > 0 &&
                                               report.render.drawCallsRecorderValid &&
                                               report.render.setPassRecorderValid &&
                                               report.render.trianglesRecorderValid;
            budgets.noPoolExpansionOrDrops = report.pools.expansionsAfterWarmup == 0 &&
                                             report.pools.droppedRequests == 0 &&
                                             report.pools.invalidBindings == 0;
            budgets.correctResolution = report.environment.screenWidth == 1920 &&
                                        report.environment.screenHeight == 1080;
            budgets.correctGraphicsApi = string.Equals(
                report.environment.graphicsDeviceType,
                "Direct3D11",
                StringComparison.Ordinal);
            budgets.correctQuality = string.Equals(
                report.environment.quality,
                "Ultra",
                StringComparison.Ordinal) && report.environment.vSyncCount == 0;
            budgets.uninterruptedFocus = !report.environment.focusLost && !report.environment.batchMode;
            budgets.physicalDisplayAdapter = !report.environment.remoteDisplayAdapter;
            budgets.provenanceRecorded = HasValue(report.environment.gitSha) &&
                                         HasValue(report.environment.packVersion) &&
                                         HasValue(report.environment.packHash);
            budgets.noFrameSampleOverflow = !report.render.frameSampleOverflow;

            var passed = budgets.configurationMatchesProfile && budgets.exactEntityCounts &&
                         budgets.formalAssetsResolved && budgets.averageFpsWithinBudget &&
                         budgets.onePercentLowWithinBudget && budgets.gpuP99WithinBudget &&
                         budgets.tickP99WithinBudget && budgets.zeroHotPathManagedAllocation &&
                         budgets.zeroProfilerGcAllocation && budgets.noGcCollections &&
                         budgets.noSustainedMemoryGrowth && budgets.renderRecordersAvailable &&
                         budgets.noPoolExpansionOrDrops && budgets.correctResolution &&
                         budgets.correctGraphicsApi && budgets.correctQuality &&
                         budgets.uninterruptedFocus && budgets.physicalDisplayAdapter &&
                         budgets.provenanceRecorded && budgets.noFrameSampleOverflow;
            report.status = passed ? "PASS" : "FAIL";
            report.failureReason = passed ? string.Empty : DescribeFailures(budgets);
        }

        internal static QinglanG35MemoryMetrics CalculateMemory(
            QinglanG35MemorySample[] source,
            int count)
        {
            if (source == null) throw new ArgumentNullException(nameof(source));
            var output = new QinglanG35MemoryMetrics();
            if (count <= 0 || count > source.Length) return output;
            output.samples = new QinglanG35MemorySample[count];
            Array.Copy(source, output.samples, count);
            for (var index = 0; index < count; index++)
            {
                var sample = source[index];
                if (sample.managedBytes > output.peakManagedBytes) output.peakManagedBytes = sample.managedBytes;
                if (sample.nativeBytes > output.peakNativeBytes) output.peakNativeBytes = sample.nativeBytes;
                if (sample.gcHeapBytes > output.peakGcHeapBytes) output.peakGcHeapBytes = sample.gcHeapBytes;
                if (sample.graphicsDriverBytes > output.peakGraphicsDriverBytes)
                    output.peakGraphicsDriverBytes = sample.graphicsDriverBytes;
            }

            var first = source[0];
            var last = source[count - 1];
            output.managedGrowthBytes = last.managedBytes - first.managedBytes;
            output.nativeGrowthBytes = last.nativeBytes - first.nativeBytes;
            output.managedSustainedGrowth = count >= 3 &&
                                            output.managedGrowthBytes > 32L * 1024L * 1024L &&
                                            MonotonicTail(source, count, true);
            output.nativeSustainedGrowth = count >= 3 &&
                                           output.nativeGrowthBytes > 128L * 1024L * 1024L &&
                                           MonotonicTail(source, count, false);
            return output;
        }

        private static bool ConfigurationMatches(QinglanG35Configuration value)
        {
            if (value == null || value.simulationHz != 30 || value.targetFrameRate != 60)
                return false;
            if (string.Equals(value.profile, "quick", StringComparison.Ordinal))
                return !value.certificationEligible && value.tickCount > 0 && value.enemies > 0 &&
                       value.projectiles > 0 && value.pickups > 0 && value.vfx > 0 &&
                       value.warmupTicks >= 0;
            if (value.warmupTicks != 300 ||
                value.targetFrameRate != 60 || value.projectiles != 900 ||
                value.pickups != 500 || value.vfx != 200) return false;
            if (string.Equals(value.profile, "target", StringComparison.Ordinal))
                return value.certificationEligible && value.tickCount == 54000 && value.enemies == 1200;
            if (string.Equals(value.profile, "extension", StringComparison.Ordinal))
                return !value.certificationEligible && value.tickCount == 9000 && value.enemies == 2000;
            return false;
        }

        private static bool MonotonicTail(QinglanG35MemorySample[] values, int count, bool managed)
        {
            var start = Math.Max(1, count - 3);
            for (var index = start; index < count; index++)
            {
                var previous = managed ? values[index - 1].managedBytes : values[index - 1].nativeBytes;
                var current = managed ? values[index].managedBytes : values[index].nativeBytes;
                if (current < previous) return false;
            }
            return true;
        }

        private static double Percentile(double[] values, int count, double percentile)
        {
            var index = (int)Math.Ceiling(percentile * count) - 1;
            return values[Math.Max(0, Math.Min(count - 1, index))];
        }

        private static bool HasValue(string value) => !string.IsNullOrWhiteSpace(value);

        private static string DescribeFailures(QinglanG35BudgetResult value)
        {
            var output = new StringBuilder(256);
            Append(output, value.configurationMatchesProfile, "configuration");
            Append(output, value.exactEntityCounts, "entity-counts");
            Append(output, value.formalAssetsResolved, "formal-assets");
            Append(output, value.averageFpsWithinBudget, "average-fps");
            Append(output, value.onePercentLowWithinBudget, "one-percent-low");
            Append(output, value.gpuP99WithinBudget, "gpu-p99");
            Append(output, value.tickP99WithinBudget, "tick-p99");
            Append(output, value.zeroHotPathManagedAllocation, "hot-path-allocation");
            Append(output, value.zeroProfilerGcAllocation, "frame-gc-allocation");
            Append(output, value.noGcCollections, "gc-collections");
            Append(output, value.noSustainedMemoryGrowth, "memory-growth");
            Append(output, value.renderRecordersAvailable, "render-recorders");
            Append(output, value.noPoolExpansionOrDrops, "pool-capacity");
            Append(output, value.correctResolution, "resolution");
            Append(output, value.correctGraphicsApi, "graphics-api");
            Append(output, value.correctQuality, "quality");
            Append(output, value.uninterruptedFocus, "focus");
            Append(output, value.physicalDisplayAdapter, "display-adapter");
            Append(output, value.provenanceRecorded, "provenance");
            Append(output, value.noFrameSampleOverflow, "sample-capacity");
            return output.ToString();
        }

        private static void Append(StringBuilder output, bool passed, string label)
        {
            if (passed) return;
            if (output.Length > 0) output.Append(", ");
            output.Append(label);
        }
    }
}
