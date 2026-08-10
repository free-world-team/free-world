using System;
using System.IO;
using Game.Editor;
using NUnit.Framework;

namespace Game.Tests.EditMode
{
    public sealed class QinglanG34BalanceContractTests
    {
        [Test]
        public void MatrixSeedsAreFrozenUniqueAndPartitionedByRoute()
        {
            var seen = new System.Collections.Generic.HashSet<ulong>();
            for (var route = 0; route < 3; route++)
            {
                for (var index = 1; index <= QinglanG34BalanceCommand.MatrixSeedsPerRoute; index++)
                {
                    var seed = QinglanG34BalanceCommand.GetSeed(route, index);
                    Assert.That(seen.Add(seed), Is.True, $"duplicate seed 0x{seed:X16}");
                    Assert.That(seed & 0xFFUL, Is.EqualTo((ulong)index));
                }
            }
            Assert.That(QinglanG34BalanceCommand.GetSeed(0, 1), Is.EqualTo(0x47333453574F5201UL));
            Assert.That(QinglanG34BalanceCommand.GetSeed(1, 1), Is.EqualTo(0x47333454414C4901UL));
            Assert.That(QinglanG34BalanceCommand.GetSeed(2, 1), Is.EqualTo(0x4733344649454C01UL));
        }

        [Test]
        public void RouteDefinitionsFreezeThreeDistinctCoreBuildsAndSixRelics()
        {
            var ids = new System.Collections.Generic.HashSet<string>(StringComparer.Ordinal);
            for (var route = 0; route < 3; route++)
            {
                var definition = QinglanG34BalanceCommand.GetRoute(route);
                Assert.That(definition.Route, Is.EqualTo(route));
                Assert.That(ids.Add(definition.CoreSkillId), Is.True);
                Assert.That(ids.Add(definition.CorePassiveId), Is.True);
                Assert.That(ids.Add(definition.EvolutionId), Is.True);
                Assert.That(definition.RelicPriority, Has.Length.EqualTo(6));
            }
            Assert.That(QinglanG34BalanceCommand.GetAllRelicIds(), Has.Length.EqualTo(6));
        }

        [Test]
        public void BalanceCatalogContainsOnlyTheShippableQinglanPack()
        {
            var catalogs = QinglanG34BalanceCommand.BakeDemoCatalog();
            Assert.That(catalogs.IsSuccess, Is.True, catalogs.Error.ToString());
            Assert.That(catalogs.Value, Has.Length.EqualTo(1));
            Assert.That(catalogs.Value[0].Manifest.PackId.Value, Is.EqualTo("qinglan.pack.demo"));
            Assert.That(catalogs.Value[0].Manifest.Version.ToString(),
                Is.EqualTo(QinglanG34BalanceContentSetup.FrozenPackVersion));
            for (var index = 0; index < catalogs.Value[0].Definitions.Count; index++)
                Assert.That(catalogs.Value[0].Definitions[index].Id.Value, Does.Not.StartWith("test."));
        }

        [Test]
        public void CommandOnlyDriverSourceRejectsCombatAndStoreMutationApis()
        {
            var source = File.ReadAllText(Path.GetFullPath(
                "Assets/Game/Editor/QinglanG34BalanceCommand.cs"));
            var driverStart = source.IndexOf("private sealed class CommandOnlyAutoPlayer", StringComparison.Ordinal);
            var driverEnd = source.IndexOf("public static class QinglanG34BalanceRules", StringComparison.Ordinal);
            Assert.That(driverStart, Is.GreaterThanOrEqualTo(0));
            Assert.That(driverEnd, Is.GreaterThan(driverStart));
            var driver = source.Substring(driverStart, driverEnd - driverStart);
            Assert.That(driver, Does.Not.Contain("TryApplyHealing"));
            Assert.That(driver, Does.Not.Contain("QueueDamage"));
            Assert.That(driver, Does.Not.Contain("TryAddModifier"));
            Assert.That(driver, Does.Not.Contain("TryWrite"));
            Assert.That(driver, Does.Not.Contain("GrantDebugExperience"));
            Assert.That(driver, Does.Not.Contain(".Position ="));
            Assert.That(driver, Does.Contain("SetMoveDirection"));
            Assert.That(driver, Does.Contain("SetInteractHeld"));
        }

        [Test]
        public void RuleEvaluatorFailsIncompleteEvidenceInsteadOfPromotingNotRun()
        {
            var report = new QinglanG34BalanceReport
            {
                matrix = Array.Empty<QinglanG34RunSummary>(),
                goldenReplays = Array.Empty<QinglanG34RunSummary>(),
                failureProbes = Array.Empty<QinglanG34RunSummary>(),
                relicCompatibilityCases = Array.Empty<QinglanG34RelicCompatibilitySummary>()
            };
            QinglanG34BalanceRules.Evaluate(report);
            Assert.That(report.status, Is.EqualTo("FAIL"));
            Assert.That(report.failureReason, Does.Contain("matrix_count"));
            Assert.That(report.failureReason, Does.Contain("relic_compatibility"));
            Assert.That(report.deterministicGoldenReplay, Is.False);
            Assert.That(report.failureProbesPassed, Is.False);
        }

        [Test]
        public void GoldenEquivalenceIncludesEveryFrozenDeterministicChecksum()
        {
            var left = EquivalentFixture();
            var right = EquivalentFixture();
            Assert.That(QinglanG34BalanceRules.Equivalent(left, right), Is.True);
            right.buildChecksum = "changed";
            Assert.That(QinglanG34BalanceRules.Equivalent(left, right), Is.False);
            right = EquivalentFixture();
            right.completedTicks++;
            Assert.That(QinglanG34BalanceRules.Equivalent(left, right), Is.False);
        }

        private static QinglanG34RunSummary EquivalentFixture()
        {
            return new QinglanG34RunSummary
            {
                completedTicks = 21_700,
                victory = true,
                reason = "Completed",
                level = 30,
                enemyDefeats = 500,
                eliteDefeats = 4,
                bossDefeats = 2,
                pickupsCollected = 400,
                offersSelected = 29,
                rewardsSelected = 3,
                completedObjectives = 1,
                completedEvents = 3,
                claimedLandmarks = 5,
                spawnChecksum = "spawn",
                objectiveChecksum = "objective",
                bossChecksum = "boss",
                decisionChecksum = "decision",
                buildChecksum = "build",
                combinedChecksum = "combined"
            };
        }
    }
}
