using Game.Infrastructure;
using NUnit.Framework;

namespace Game.Tests.EditMode
{
    public sealed class QinglanG35PerformanceContractTests
    {
        [Test]
        public void TimingUsesSortedNearestRankAndDerivesOnePercentLow()
        {
            var values = new double[100];
            for (var index = 0; index < values.Length; index++) values[index] = 100 - index;

            var result = QinglanG35PerformanceContract.CalculateTiming(values, values.Length);

            Assert.That(result.samples, Is.EqualTo(100));
            Assert.That(result.averageMilliseconds, Is.EqualTo(50.5d).Within(0.0001d));
            Assert.That(result.p95Milliseconds, Is.EqualTo(95d));
            Assert.That(result.p99Milliseconds, Is.EqualTo(99d));
            Assert.That(result.maximumMilliseconds, Is.EqualTo(100d));
            Assert.That(result.onePercentLowFps, Is.EqualTo(1000d / 99d).Within(0.0001d));
        }

        [Test]
        public void CompleteQuickProbePassesEveryRuntimeBudget()
        {
            var report = CreatePassingReport();

            QinglanG35PerformanceContract.Evaluate(report);

            Assert.That(report.status, Is.EqualTo("PASS"), report.failureReason);
            Assert.That(report.failureReason, Is.Empty);
            Assert.That(report.budgets.gpuSamplesAvailable, Is.True);
            Assert.That(report.budgets.zeroProfilerGcAllocation, Is.True);
            Assert.That(report.budgets.uninterruptedFocus, Is.True);
        }

        [Test]
        public void MissingGpuSamplesCannotBeReportedAsPass()
        {
            var report = CreatePassingReport();
            report.gpuFrame.samples = 0;

            QinglanG35PerformanceContract.Evaluate(report);

            Assert.That(report.status, Is.EqualTo("FAIL"));
            Assert.That(report.budgets.gpuSamplesAvailable, Is.False);
            Assert.That(report.failureReason, Does.Contain("gpu-p99"));
        }

        [Test]
        public void FocusLossOrRemoteAdapterCannotBeReportedAsPass()
        {
            var report = CreatePassingReport();
            report.environment.focusLost = true;
            report.environment.remoteDisplayAdapter = true;

            QinglanG35PerformanceContract.Evaluate(report);

            Assert.That(report.status, Is.EqualTo("FAIL"));
            Assert.That(report.failureReason, Does.Contain("focus"));
            Assert.That(report.failureReason, Does.Contain("display-adapter"));
        }

        [Test]
        public void SustainedMemoryGrowthRequiresThresholdAndMonotonicTail()
        {
            var megabyte = 1024L * 1024L;
            var samples = new[]
            {
                Sample(0d, 100 * megabyte, 500 * megabyte),
                Sample(1d, 120 * megabyte, 550 * megabyte),
                Sample(2d, 140 * megabyte, 650 * megabyte),
                Sample(3d, 150 * megabyte, 700 * megabyte)
            };

            var result = QinglanG35PerformanceContract.CalculateMemory(samples, samples.Length);

            Assert.That(result.managedSustainedGrowth, Is.True);
            Assert.That(result.nativeSustainedGrowth, Is.True);
            Assert.That(result.samples, Has.Length.EqualTo(4));
            Assert.That(result.peakManagedBytes, Is.EqualTo(150 * megabyte));
        }

        private static QinglanG35PerformanceReport CreatePassingReport()
        {
            return new QinglanG35PerformanceReport
            {
                configuration = new QinglanG35Configuration
                {
                    profile = "quick",
                    seed = "17",
                    warmupTicks = 1,
                    tickCount = 3,
                    enemies = 2,
                    projectiles = 2,
                    pickups = 2,
                    vfx = 1,
                    simulationHz = 30,
                    targetFrameRate = 60,
                    certificationEligible = false
                },
                environment = new QinglanG35Environment
                {
                    graphicsDeviceType = "Direct3D11",
                    screenWidth = 1920,
                    screenHeight = 1080,
                    quality = "Ultra",
                    vSyncCount = 0,
                    batchMode = false,
                    frameTimingFeatureEnabled = true,
                    focusLost = false,
                    remoteDisplayAdapter = false,
                    gitSha = "0123456789012345678901234567890123456789",
                    packVersion = "0.10.0",
                    packHash = "hash"
                },
                wallFrame = new QinglanG35TimingMetrics
                {
                    samples = 6,
                    averageMilliseconds = 16d,
                    p99Milliseconds = 16.5d,
                    averageFps = 62.5d,
                    onePercentLowFps = 60.6d
                },
                gpuFrame = new QinglanG35TimingMetrics
                {
                    samples = 6,
                    p99Milliseconds = 12d
                },
                simulationTick = new QinglanG35TimingMetrics
                {
                    samples = 3,
                    p99Milliseconds = 4d
                },
                memory = new QinglanG35MemoryMetrics(),
                gc = new QinglanG35GcMetrics
                {
                    profilerSamples = 6
                },
                render = new QinglanG35RenderMetrics
                {
                    samples = 6,
                    drawCallsRecorderValid = true,
                    setPassRecorderValid = true,
                    trianglesRecorderValid = true,
                    maximumDrawCalls = 8,
                    maximumTriangles = 16
                },
                pools = new QinglanG35PoolMetrics
                {
                    expectedViews = 8,
                    createdViews = 8,
                    peakActiveViews = 8,
                    expectedSpriteRenderers = 15,
                    createdSpriteRenderers = 15,
                    resolvedFormalProfiles = 5
                }
            };
        }

        private static QinglanG35MemorySample Sample(
            double minute,
            long managed,
            long native)
        {
            return new QinglanG35MemorySample
            {
                simulatedMinute = minute,
                managedBytes = managed,
                nativeBytes = native,
                gcHeapBytes = managed,
                graphicsDriverBytes = native / 2
            };
        }
    }
}
