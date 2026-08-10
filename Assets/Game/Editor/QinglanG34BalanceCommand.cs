using System;
using System.Globalization;
using System.IO;
using System.Numerics;
using Game.Application;
using Game.Content.Authoring;
using Game.Content.Runtime;
using Game.Core;
using Game.Infrastructure;
using Game.Simulation;
using UnityEditor;
using UnityEngine;
using Debug = UnityEngine.Debug;
using Vector2 = System.Numerics.Vector2;

namespace Game.Editor
{
    /// <summary>
    /// Runs the G3.4 balance matrix through the production composition root. The driver
    /// submits player-facing commands only; SimulationWorld access is read-only telemetry.
    /// </summary>
    public static class QinglanG34BalanceCommand
    {
        public const int RouteSword = 0;
        public const int RouteTalisman = 1;
        public const int RouteField = 2;
        public const int MatrixSeedsPerRoute = 5;
        public const int MinimumGoldenTicks = 12 * 60 * SimulationClock.TickRate;
        public const int MaximumRunTicks = MinimumGoldenTicks + (45 * SimulationClock.TickRate);

        public const ulong SwordSeedBase = 0x47333453574F5200UL;
        public const ulong TalismanSeedBase = 0x47333454414C4900UL;
        public const ulong FieldSeedBase = 0x4733344649454C00UL;

        private static readonly ContentVersion GameVersion = new ContentVersion(0, 1, 0);
        private static readonly ContentId GuideObjectiveId = Id("qinglan.objective.wind_altar.guide");
        private static readonly ContentId ListenObjectiveId = Id("qinglan.objective.wind_altar.listen");
        private static readonly ContentId StopObjectiveId = Id("qinglan.objective.wind_altar.stop_balance");

        private static readonly RouteDefinition[] Routes =
        {
            new RouteDefinition(
                RouteSword,
                "moving_sword",
                "qinglan.skill.weapon.yufeng_sword",
                "qinglan.passive.treading_wind",
                "qinglan.evolution.qinglan_flowing_shadow_sword",
                "qinglan.skill.evolved.qinglan_flowing_shadow_sword",
                new[]
                {
                    "qinglan.relic.broken_sword_tassel",
                    "qinglan.relic.wind_vein_copper",
                    "qinglan.relic.listening_wind_core",
                    "qinglan.relic.old_court_bell",
                    "qinglan.relic.blank_sword_trial_token",
                    "qinglan.relic.herb_garden_seed_pod"
                }),
            new RouteDefinition(
                RouteTalisman,
                "talisman_burst",
                "qinglan.skill.weapon.yellow_talisman",
                "qinglan.passive.clear_mind",
                "qinglan.evolution.taiyi_spirit_sealing_array",
                "qinglan.skill.evolved.taiyi_spirit_sealing_array",
                new[]
                {
                    "qinglan.relic.listening_wind_core",
                    "qinglan.relic.blank_sword_trial_token",
                    "qinglan.relic.old_court_bell",
                    "qinglan.relic.broken_sword_tassel",
                    "qinglan.relic.wind_vein_copper",
                    "qinglan.relic.herb_garden_seed_pod"
                }),
            new RouteDefinition(
                RouteField,
                "living_field",
                "qinglan.skill.weapon.spirit_vine_seed",
                "qinglan.passive.spirit_gathering",
                "qinglan.evolution.earth_vein_spring_branch",
                "qinglan.skill.evolved.earth_vein_spring_branch",
                new[]
                {
                    "qinglan.relic.herb_garden_seed_pod",
                    "qinglan.relic.old_court_bell",
                    "qinglan.relic.listening_wind_core",
                    "qinglan.relic.wind_vein_copper",
                    "qinglan.relic.broken_sword_tassel",
                    "qinglan.relic.blank_sword_trial_token"
                })
        };

        private static readonly string[] AllRelicIds =
        {
            "qinglan.relic.broken_sword_tassel",
            "qinglan.relic.wind_vein_copper",
            "qinglan.relic.herb_garden_seed_pod",
            "qinglan.relic.listening_wind_core",
            "qinglan.relic.old_court_bell",
            "qinglan.relic.blank_sword_trial_token"
        };

        [MenuItem("Tools/Free World/Qinglan/G3.4 Run Balance Matrix")]
        public static void Run()
        {
            var exitCode = 0;
            try
            {
                var catalogs = BakeDemoCatalog();
                if (!catalogs.IsSuccess) throw new InvalidOperationException(catalogs.Error.ToString());
                var matrix = new QinglanG34RunSummary[Routes.Length * MatrixSeedsPerRoute];
                var cursor = 0;
                for (var route = 0; route < Routes.Length; route++)
                {
                    for (var seedIndex = 1; seedIndex <= MatrixSeedsPerRoute; seedIndex++)
                    {
                        var seed = GetSeed(route, seedIndex);
                        matrix[cursor++] = Execute(CreateApplication(catalogs.Value), seed, route, false);
                    }
                }

                var replays = new QinglanG34RunSummary[Routes.Length];
                var failureProbes = new QinglanG34RunSummary[Routes.Length];
                for (var route = 0; route < Routes.Length; route++)
                {
                    var seed = GetSeed(route, 1);
                    replays[route] = Execute(CreateApplication(catalogs.Value), seed, route, false);
                    failureProbes[route] = Execute(CreateApplication(catalogs.Value), seed, route, true);
                }

                var report = new QinglanG34BalanceReport
                {
                    schemaVersion = 1,
                    generatedAtUtc = DateTime.UtcNow.ToString("O", CultureInfo.InvariantCulture),
                    unityVersion = UnityEngine.Application.unityVersion,
                    minimumGoldenTicks = MinimumGoldenTicks,
                    maximumRunTicks = MaximumRunTicks,
                    matrix = matrix,
                    goldenReplays = replays,
                    failureProbes = failureProbes,
                    relicCompatibilityCases = Array.Empty<QinglanG34RelicCompatibilitySummary>()
                };
                QinglanG34BalanceRules.Evaluate(report);
                WriteReport(report, ResolveOutputPath());
                if (report.status == "PASS") Debug.Log("[Qinglan G3.4 Balance] PASS: " + ResolveOutputPath());
                else Debug.LogError("[Qinglan G3.4 Balance] FAIL: " + report.failureReason);
                exitCode = report.status == "PASS" ? 0 : 2;
            }
            catch (Exception exception)
            {
                Debug.LogException(exception);
                exitCode = 1;
            }
            EditorApplication.Exit(exitCode);
        }

        public static QinglanG34RunSummary Execute(
            GameApplication application,
            ulong seed,
            int route,
            bool failureProbe)
        {
            if (application == null) throw new ArgumentNullException(nameof(application));
            var definition = GetRoute(route);
            var factory = new QinglanDemoRunFactory(application);
            var descriptor = factory.CreateDescriptor(seed ^ 0x47333452554E4944UL, seed);
            if (!descriptor.IsSuccess) throw new InvalidOperationException(descriptor.Error.ToString());
            var created = factory.Create(descriptor.Value, application.StateMachine);
            if (!created.IsSuccess) throw new InvalidOperationException(created.Error.ToString());
            var handle = created.Value as QinglanDemoRunHandle;
            if (handle == null) throw new InvalidOperationException("Qinglan factory returned an unexpected handle.");
            var session = handle.Session;
            var world = handle.World;
            var player = session.Player.Handle;
            var driver = new CommandOnlyAutoPlayer(application.ContentRegistry, definition, seed, failureProbe);
            var minimumHealth = float.MaxValue;
            var maximumHealth = 0f;
            var movementDistance = 0d;
            var previousPosition = Vector2.Zero;
            var hasPreviousPosition = false;
            var peakEnemies = 0;
            var peakProjectiles = 0;
            var peakAreas = 0;
            var positionsWalkable = true;
            var targetSkillPeakLevel = 0;
            var targetPassivePeakLevel = 0;
            var rewardsSelected = 0;

            try
            {
                while (world.Tick < MaximumRunTicks && !session.HasEnded)
                {
                    driver.SubmitMovement(session, world, player);
                    var advanced = session.Advance(SimulationClock.TickDurationSeconds);
                    if (advanced < 0 || advanced > 1)
                        throw new InvalidOperationException("Balance driver advanced an invalid tick count.");
                    CaptureBuildPeaks(world.Progression.Build, definition,
                        ref targetSkillPeakLevel, ref targetPassivePeakLevel);
                    driver.ResolveChoices(session, ref rewardsSelected);
                    CaptureBuildPeaks(world.Progression.Build, definition,
                        ref targetSkillPeakLevel, ref targetPassivePeakLevel);

                    peakEnemies = Math.Max(peakEnemies, world.Enemies.Count);
                    peakProjectiles = Math.Max(peakProjectiles, world.Projectiles.Count);
                    peakAreas = Math.Max(peakAreas, world.Areas.Count);
                    if (world.Actors.TryReadHealth(player, out var health))
                    {
                        minimumHealth = Math.Min(minimumHealth, health.Current);
                        maximumHealth = Math.Max(maximumHealth, health.Maximum);
                    }
                    if (world.Actors.TryRead(player, out var state))
                    {
                        if (hasPreviousPosition)
                            movementDistance += Vector2.Distance(previousPosition, state.Position);
                        previousPosition = state.Position;
                        hasPreviousPosition = true;
                    }
                    if ((world.Tick % SimulationClock.TickRate) == 0)
                        positionsWalkable &= AllPositionsWalkable(world);
                }

                return CreateSummary(
                    session,
                    world,
                    handle,
                    definition,
                    seed,
                    failureProbe,
                    minimumHealth == float.MaxValue ? 0f : minimumHealth,
                    maximumHealth,
                    movementDistance,
                    peakEnemies,
                    peakProjectiles,
                    peakAreas,
                    positionsWalkable,
                    targetSkillPeakLevel,
                    targetPassivePeakLevel,
                    rewardsSelected);
            }
            finally
            {
                handle.Dispose();
                if (!handle.IsDisposed || handle.ActiveEntityCount != 0)
                    throw new InvalidOperationException("Balance run owner did not release all entities.");
            }
        }

        public static int ChooseOfferIndex(UpgradeOfferSet offers, int route)
        {
            if (offers == null || offers.Count == 0) return -1;
            var definition = GetRoute(route);
            var best = 0;
            var bestScore = ScoreOffer(offers.GetAt(0), definition);
            for (var index = 1; index < offers.Count; index++)
            {
                var score = ScoreOffer(offers.GetAt(index), definition);
                if (score > bestScore ||
                    (score == bestScore && string.CompareOrdinal(
                        offers.GetAt(index).Source.TargetContentId.Value,
                        offers.GetAt(best).Source.TargetContentId.Value) < 0))
                {
                    best = index;
                    bestScore = score;
                }
            }
            return best;
        }

        public static int ChooseRewardIndex(RewardChoice choice, int route)
        {
            if (choice == null || choice.CandidateIds.Count == 0) return -1;
            var definition = GetRoute(route);
            var best = 0;
            var bestScore = ScoreReward(choice.CandidateIds[0], definition);
            for (var index = 1; index < choice.CandidateIds.Count; index++)
            {
                var score = ScoreReward(choice.CandidateIds[index], definition);
                if (score > bestScore ||
                    (score == bestScore && string.CompareOrdinal(
                        choice.CandidateIds[index].Value,
                        choice.CandidateIds[best].Value) < 0))
                {
                    best = index;
                    bestScore = score;
                }
            }
            return best;
        }

        public static ulong GetSeed(int route, int seedIndex)
        {
            if (seedIndex < 1 || seedIndex > MatrixSeedsPerRoute)
                throw new ArgumentOutOfRangeException(nameof(seedIndex));
            if (route == RouteSword) return SwordSeedBase | (uint)seedIndex;
            if (route == RouteTalisman) return TalismanSeedBase | (uint)seedIndex;
            if (route == RouteField) return FieldSeedBase | (uint)seedIndex;
            throw new ArgumentOutOfRangeException(nameof(route));
        }

        public static RouteDefinition GetRoute(int route)
        {
            if (route < 0 || route >= Routes.Length) throw new ArgumentOutOfRangeException(nameof(route));
            return Routes[route];
        }

        public static string[] GetAllRelicIds() => (string[])AllRelicIds.Clone();

        /// <summary>
        /// Bakes only the shippable Demo pack. Editor test packs intentionally share the
        /// project but must never enter a balance candidate or its offer pool.
        /// </summary>
        public static Result<BakedContentCatalog[]> BakeDemoCatalog()
        {
            var pack = AssetDatabase.LoadAssetAtPath<ContentPackAuthoring>(QinglanG12ContentSetup.PackPath);
            if (pack == null)
            {
                return Result<BakedContentCatalog[]>.Failure(new Error(
                    ErrorCode.MissingReference,
                    "Qinglan Demo content pack is missing.",
                    default,
                    default,
                    QinglanG12ContentSetup.PackPath));
            }
            var baked = ContentBakeUtility.Bake(pack);
            return baked.IsSuccess
                ? Result<BakedContentCatalog[]>.Success(new[] { baked.Value })
                : Result<BakedContentCatalog[]>.Failure(baked.Error);
        }

        private static int ScoreOffer(CompiledUpgradeOfferDefinition offer, RouteDefinition route)
        {
            var id = offer.Source.TargetContentId.Value;
            if (id == route.CoreSkillId) return 10_000;
            if (id == route.CorePassiveId) return 9_000;
            if (id == route.EvolutionId) return 8_000;
            if (id == "qinglan.skill.weapon.zhenyue_seal") return 700;
            if (id == "qinglan.passive.long_breath") return 650;
            if (id == "qinglan.skill.weapon.tide_orb") return 500;
            if (id == "qinglan.passive.domain_expansion") return 450;
            if (id == "qinglan.skill.weapon.yellow_talisman") return route.Route == RouteTalisman ? 850 : 350;
            if (id == "qinglan.skill.weapon.spirit_vine_seed") return route.Route == RouteField ? 850 : 300;
            if (id == "qinglan.passive.clear_mind") return route.Route == RouteTalisman ? 800 : 250;
            if (id == "qinglan.passive.spirit_gathering") return route.Route == RouteField ? 800 : 200;
            return offer.TargetKind == UpgradeTargetKind.Passive ? 150 : 100;
        }

        private static int ScoreReward(ContentId id, RouteDefinition route)
        {
            if (id.Value == route.EvolutionId) return 10_000;
            for (var index = 0; index < route.RelicPriority.Length; index++)
                if (id.Value == route.RelicPriority[index]) return 1_000 - (index * 50);
            return 0;
        }

        private static QinglanG34RunSummary CreateSummary(
            RunSession session,
            SimulationWorld world,
            QinglanDemoRunHandle handle,
            RouteDefinition route,
            ulong seed,
            bool failureProbe,
            float minimumHealth,
            float maximumHealth,
            double movementDistance,
            int peakEnemies,
            int peakProjectiles,
            int peakAreas,
            bool positionsWalkable,
            int targetSkillPeakLevel,
            int targetPassivePeakLevel,
            int rewardsSelected)
        {
            var ended = session.HasEnded;
            var result = ended ? session.Result : default;
            var statistics = ended ? result.Statistics : world.Progression.Statistics;
            var ticks = ended ? result.CompletedTicks : world.Tick;
            var skills = ended
                ? InventoryStrings(result.Build.Skills)
                : SkillStrings(world.Progression.Build.Skills);
            var passives = ended
                ? InventoryStrings(result.Build.Passives)
                : PassiveStrings(world.Progression.Build.Passives);
            var relics = ended
                ? InventoryStrings(result.Build.Relics)
                : RelicStrings(world.Qinglan.Rewards.Relics);
            var evolutions = ended
                ? IdStrings(result.Build.Evolutions)
                : EvolutionStrings(world.Progression.Build);
            var spawnChecksum = ended ? result.SpawnChecksum : world.Enemies.SpawnChecksum;
            // Terminal checksums are owned by RunResultBuilder. A timeout is already a
            // failed run, so zero is an explicit "no terminal checksum" sentinel.
            var objectiveChecksum = ended ? result.ObjectiveChecksum : 0UL;
            var bossChecksum = ended ? result.BossChecksum : 0UL;
            var buildChecksum = HashStrings(skills, passives, relics, evolutions);
            var combinedChecksum = Combine(
                spawnChecksum,
                objectiveChecksum,
                bossChecksum,
                statistics.DecisionChecksum,
                buildChecksum,
                ticks,
                route.Route,
                ended ? (int)result.Reason : 0);
            return new QinglanG34RunSummary
            {
                seed = "0x" + seed.ToString("X16", CultureInfo.InvariantCulture),
                route = route.Route,
                routeKey = route.Key,
                failureProbe = failureProbe,
                ended = ended,
                timedOut = !ended && world.Tick >= MaximumRunTicks,
                reason = ended ? result.Reason.ToString() : "Timeout",
                victory = ended && result.IsVictory,
                completedTicks = ticks,
                durationSeconds = ticks * SimulationClock.TickDurationSeconds,
                level = ended ? result.Level : world.Progression.Experience.Level,
                minimumHealth = minimumHealth,
                maximumHealth = maximumHealth,
                targetSkillPeakLevel = targetSkillPeakLevel,
                targetPassivePeakLevel = targetPassivePeakLevel,
                targetEvolutionApplied = Contains(evolutions, route.EvolutionId),
                enemyDefeats = statistics.EnemyDefeats,
                eliteDefeats = statistics.EliteDefeats,
                bossDefeats = statistics.BossDefeats,
                pickupsCollected = statistics.PickupsCollected,
                offersSelected = statistics.OffersSelected,
                rewardsSelected = rewardsSelected,
                completedObjectives = ended ? result.Exploration.CompletedObjectiveIds.Count : CountCompletedObjectives(world),
                completedEvents = ended ? result.Exploration.CompletedEventIds.Count : CountCompletedEvents(world),
                claimedLandmarks = ended ? result.Exploration.ClaimedLandmarkIds.Count : CountClaimedLandmarks(world),
                movementDistance = movementDistance,
                peakEnemies = peakEnemies,
                peakProjectiles = peakProjectiles,
                peakAreas = peakAreas,
                positionsWalkable = positionsWalkable,
                invalidHandleAccesses = world.Diagnostics.InvalidHandleAccesses,
                activeEntitiesBeforeDispose = handle.ActiveEntityCount,
                skills = skills,
                passives = passives,
                relics = relics,
                evolutions = evolutions,
                spawnChecksum = Hex(spawnChecksum),
                objectiveChecksum = Hex(objectiveChecksum),
                bossChecksum = Hex(bossChecksum),
                decisionChecksum = Hex(statistics.DecisionChecksum),
                buildChecksum = Hex(buildChecksum),
                combinedChecksum = Hex(combinedChecksum)
            };
        }

        private static void CaptureBuildPeaks(
            BuildState build,
            RouteDefinition route,
            ref int skillPeak,
            ref int passivePeak)
        {
            for (var index = 0; index < build.Skills.Count; index++)
            {
                var entry = build.Skills.GetAt(index);
                if (entry.ContentId.Value == route.CoreSkillId)
                    skillPeak = Math.Max(skillPeak, entry.Level);
            }
            for (var index = 0; index < build.Passives.Count; index++)
            {
                var entry = build.Passives.GetAt(index);
                if (entry.ContentId.Value == route.CorePassiveId)
                    passivePeak = Math.Max(passivePeak, entry.Level);
            }
        }

        private static bool AllPositionsWalkable(SimulationWorld world)
        {
            if (world.Map == null) return true;
            for (var index = 0; index < world.Actors.Count; index++)
                if (!world.Map.IsWalkable(world.Actors.GetStateAt(index).Position)) return false;
            return true;
        }

        private static int CountCompletedObjectives(SimulationWorld world)
        {
            var count = 0;
            for (var index = 0; index < world.Qinglan.MapObjectives.ObjectiveCount; index++)
                if (world.Qinglan.MapObjectives.GetObjectiveAt(index).State == ObjectiveState.Completed) count++;
            return count;
        }

        private static int CountCompletedEvents(SimulationWorld world)
        {
            var count = 0;
            for (var index = 0; index < world.Qinglan.MapObjectives.EventCount; index++)
                if (world.Qinglan.MapObjectives.GetEventAt(index).State == ObjectiveState.Completed) count++;
            return count;
        }

        private static int CountClaimedLandmarks(SimulationWorld world)
        {
            var count = 0;
            for (var index = 0; index < world.Qinglan.MapObjectives.LandmarkCount; index++)
                if (world.Qinglan.MapObjectives.GetLandmarkAt(index).State == LandmarkState.Claimed) count++;
            return count;
        }

        private static string[] InventoryStrings(System.Collections.Generic.IReadOnlyList<RunInventoryEntry> entries)
        {
            var output = new string[entries.Count];
            for (var index = 0; index < output.Length; index++)
                output[index] = entries[index].ContentId.Value + "@" + entries[index].Level.ToString(CultureInfo.InvariantCulture);
            return output;
        }

        private static string[] SkillStrings(SkillInventory inventory)
        {
            var output = new string[inventory.Count];
            for (var index = 0; index < output.Length; index++)
            {
                var entry = inventory.GetAt(index);
                output[index] = entry.ContentId.Value + "@" + entry.Level.ToString(CultureInfo.InvariantCulture);
            }
            return output;
        }

        private static string[] PassiveStrings(PassiveInventory inventory)
        {
            var output = new string[inventory.Count];
            for (var index = 0; index < output.Length; index++)
            {
                var entry = inventory.GetAt(index);
                output[index] = entry.ContentId.Value + "@" + entry.Level.ToString(CultureInfo.InvariantCulture);
            }
            return output;
        }

        private static string[] RelicStrings(RelicInventory inventory)
        {
            var output = new string[inventory.Count];
            for (var index = 0; index < output.Length; index++)
            {
                var entry = inventory.GetAt(index);
                output[index] = entry.RelicId.Value + "@" + entry.Level.ToString(CultureInfo.InvariantCulture);
            }
            return output;
        }

        private static string[] EvolutionStrings(BuildState build)
        {
            var output = new string[build.AppliedEvolutionCount];
            for (var index = 0; index < output.Length; index++) output[index] = build.GetAppliedEvolutionAt(index).Value;
            return output;
        }

        private static string[] IdStrings(System.Collections.Generic.IReadOnlyList<ContentId> ids)
        {
            var output = new string[ids.Count];
            for (var index = 0; index < output.Length; index++) output[index] = ids[index].Value;
            return output;
        }

        private static bool Contains(string[] values, string id)
        {
            for (var index = 0; index < values.Length; index++)
                if (values[index] == id || values[index].StartsWith(id + "@", StringComparison.Ordinal)) return true;
            return false;
        }

        private static ulong HashStrings(params string[][] groups)
        {
            unchecked
            {
                var hash = 1469598103934665603UL;
                for (var group = 0; group < groups.Length; group++)
                {
                    var values = groups[group] ?? Array.Empty<string>();
                    for (var index = 0; index < values.Length; index++)
                    {
                        var value = values[index] ?? string.Empty;
                        for (var character = 0; character < value.Length; character++)
                            hash = (hash ^ value[character]) * 1099511628211UL;
                        hash = (hash ^ 0xFFUL) * 1099511628211UL;
                    }
                }
                return hash;
            }
        }

        private static ulong Combine(
            ulong spawn,
            ulong objective,
            ulong boss,
            ulong decision,
            ulong build,
            long ticks,
            int route,
            int reason)
        {
            unchecked
            {
                var hash = spawn;
                hash = (hash ^ objective) * 1099511628211UL;
                hash = (hash ^ boss) * 1099511628211UL;
                hash = (hash ^ decision) * 1099511628211UL;
                hash = (hash ^ build) * 1099511628211UL;
                hash = (hash ^ (ulong)ticks) * 1099511628211UL;
                hash = (hash ^ (uint)route) * 1099511628211UL;
                return (hash ^ (uint)reason) * 1099511628211UL;
            }
        }

        private static string Hex(ulong value) => value.ToString("x16", CultureInfo.InvariantCulture);

        private static GameApplication CreateApplication(BakedContentCatalog[] catalogs) =>
            QinglanDemoRunFactory.CreateInitializedApplicationForDiagnostics(catalogs, GameVersion);

        private static string ResolveOutputPath()
        {
            var configured = Environment.GetEnvironmentVariable("QINGLAN_G34_OUTPUT");
            if (string.IsNullOrWhiteSpace(configured))
                configured = "TestResults/QinglanDemo/G3.4/balance-matrix.json";
            return Path.GetFullPath(configured);
        }

        private static void WriteReport(QinglanG34BalanceReport report, string output)
        {
            var directory = Path.GetDirectoryName(output);
            if (!string.IsNullOrEmpty(directory)) Directory.CreateDirectory(directory);
            File.WriteAllText(output, JsonUtility.ToJson(report, true) + "\n");
        }

        private static ContentId Id(string value)
        {
            var result = ContentId.Create(value);
            if (!result.IsSuccess) throw new InvalidOperationException(result.Error.ToString());
            return result.Value;
        }

        public sealed class RouteDefinition
        {
            internal RouteDefinition(
                int route,
                string key,
                string coreSkillId,
                string corePassiveId,
                string evolutionId,
                string evolvedSkillId,
                string[] relicPriority)
            {
                Route = route;
                Key = key;
                CoreSkillId = coreSkillId;
                CorePassiveId = corePassiveId;
                EvolutionId = evolutionId;
                EvolvedSkillId = evolvedSkillId;
                RelicPriority = relicPriority;
            }

            public int Route { get; }
            public string Key { get; }
            public string CoreSkillId { get; }
            public string CorePassiveId { get; }
            public string EvolutionId { get; }
            public string EvolvedSkillId { get; }
            public string[] RelicPriority { get; }
        }

        private enum NavigationTargetKind : byte
        {
            Event = 1,
            Objective = 2,
            Boss = 3,
            Landmark = 4,
            Patrol = 5
        }

        private sealed class CommandOnlyAutoPlayer
        {
            private readonly ContentRegistry content;
            private readonly RouteDefinition route;
            private readonly ulong seed;
            private readonly bool failureProbe;

            public CommandOnlyAutoPlayer(
                ContentRegistry registry,
                RouteDefinition routeDefinition,
                ulong runSeed,
                bool idleFailureProbe)
            {
                content = registry ?? throw new ArgumentNullException(nameof(registry));
                route = routeDefinition ?? throw new ArgumentNullException(nameof(routeDefinition));
                seed = runSeed;
                failureProbe = idleFailureProbe;
            }

            public void SubmitMovement(RunSession session, SimulationWorld world, EntityHandle player)
            {
                if (failureProbe)
                {
                    session.SetMoveDirection(Vector2.Zero);
                    session.SetInteractHeld(false);
                    return;
                }
                if (!world.Actors.TryRead(player, out var state)) return;
                TryChooseTarget(world, out var target, out var kind);
                var direction = ComputeDirection(world, player, state.Position, target, kind);
                session.SetMoveDirection(direction);
                var interaction = (kind == NavigationTargetKind.Event ||
                                   kind == NavigationTargetKind.Objective ||
                                   kind == NavigationTargetKind.Landmark) &&
                                  Vector2.DistanceSquared(state.Position, target) <= 2.2f * 2.2f;
                session.SetInteractHeld(interaction);
            }

            public void ResolveChoices(RunSession session, ref int rewardsSelected)
            {
                if (session.StateMachine.CurrentState == GameState.LevelUpChoice)
                {
                    if (failureProbe)
                    {
                        if (!session.Skip()) throw new InvalidOperationException("Failure probe could not skip an offer.");
                    }
                    else
                    {
                        var index = ChooseOfferIndex(session.CurrentOffers, route.Route);
                        if (index < 0 || !session.SelectAt(index))
                            throw new InvalidOperationException("Balance driver could not select an offer.");
                    }
                }
                if (session.StateMachine.CurrentState == GameState.RewardChoice)
                {
                    var index = failureProbe ? 0 : ChooseRewardIndex(session.CurrentRewardChoice, route.Route);
                    if (index < 0 || !session.SelectRewardAt(index))
                        throw new InvalidOperationException("Balance driver could not select a reward.");
                    rewardsSelected++;
                }
            }

            private void TryChooseTarget(
                SimulationWorld world,
                out Vector2 target,
                out NavigationTargetKind kind)
            {
                var map = world.Qinglan.MapObjectives;
                for (var index = 0; index < map.EventCount; index++)
                {
                    var entry = map.GetEventAt(index);
                    if (entry.State == ObjectiveState.Defending && entry.ActiveAnchorId.IsValid &&
                        world.Map.TryGetAnchor(entry.ActiveAnchorId, out target))
                    {
                        kind = NavigationTargetKind.Event;
                        return;
                    }
                }
                for (var index = 0; index < map.ObjectiveCount; index++)
                {
                    var entry = map.GetObjectiveAt(index);
                    if (!WantsObjective(route.Route, entry.Id) || entry.State == ObjectiveState.Completed) continue;
                    var anchorId = entry.ActiveAnchorId;
                    if (!anchorId.IsValid && content.TryGet(entry.Id, out RuntimeMapObjectiveDefinition definition) &&
                        definition.AnchorIds.Count > 0) anchorId = definition.AnchorIds[0];
                    if (anchorId.IsValid && world.Map.TryGetAnchor(anchorId, out target))
                    {
                        kind = NavigationTargetKind.Objective;
                        return;
                    }
                }
                for (var index = 0; index < world.Actors.Count; index++)
                {
                    var handle = world.Actors.GetHandleAt(index);
                    if (!world.Enemies.TryGetSnapshot(handle, out var enemy) || !enemy.Boss) continue;
                    target = world.Actors.GetStateAt(index).Position;
                    kind = NavigationTargetKind.Boss;
                    return;
                }
                for (var index = 0; index < map.LandmarkCount; index++)
                {
                    var entry = map.GetLandmarkAt(index);
                    if (entry.State == LandmarkState.Claimed ||
                        !world.Map.TryGetAnchor(entry.AnchorId, out target)) continue;
                    kind = NavigationTargetKind.Landmark;
                    return;
                }
                var phase = (world.Tick + (long)(seed & 0xFFFFUL)) *
                            (Math.PI * 2d / (28d * SimulationClock.TickRate));
                target = new Vector2((float)Math.Cos(phase) * 16f, (float)Math.Sin(phase) * 16f);
                kind = NavigationTargetKind.Patrol;
            }

            private Vector2 ComputeDirection(
                SimulationWorld world,
                EntityHandle player,
                Vector2 position,
                Vector2 target,
                NavigationTargetKind kind)
            {
                var offset = target - position;
                var distance = offset.Length();
                var direction = distance > 0.001f ? offset / distance : Vector2.Zero;
                var clockwise = ((seed >> 8) & 1UL) == 0UL ? 1f : -1f;
                if (kind == NavigationTargetKind.Boss)
                {
                    var tangent = new Vector2(-direction.Y, direction.X) * clockwise;
                    var radial = Math.Clamp((distance - 9f) / 3f, -1f, 1f);
                    direction = tangent + (direction * radial);
                }
                else if ((kind == NavigationTargetKind.Event || kind == NavigationTargetKind.Objective ||
                          kind == NavigationTargetKind.Landmark) && distance <= 2f)
                {
                    var tangent = new Vector2(-direction.Y, direction.X) * clockwise;
                    var radial = Math.Clamp((distance - 1.4f) / 0.8f, -1f, 1f);
                    direction = tangent + (direction * radial);
                }
                if (TryFindNearestEnemy(world, player, position, out var enemyPosition, out var enemyDistance) &&
                    enemyDistance < 5.5f)
                {
                    var away = position - enemyPosition;
                    if (away.LengthSquared() > 0.0001f)
                        direction += Vector2.Normalize(away) * ((5.5f - enemyDistance) / 5.5f) * 2.2f;
                }
                if (direction.LengthSquared() > 1f) direction = Vector2.Normalize(direction);
                return direction;
            }

            private static bool TryFindNearestEnemy(
                SimulationWorld world,
                EntityHandle player,
                Vector2 position,
                out Vector2 enemyPosition,
                out float enemyDistance)
            {
                enemyPosition = default;
                enemyDistance = float.MaxValue;
                for (var index = 0; index < world.Actors.Count; index++)
                {
                    var handle = world.Actors.GetHandleAt(index);
                    if (handle == player || !world.Enemies.TryGetSnapshot(handle, out var enemy) || enemy.Boss) continue;
                    var candidate = world.Actors.GetStateAt(index).Position;
                    var distance = Vector2.Distance(position, candidate);
                    if (distance >= enemyDistance) continue;
                    enemyDistance = distance;
                    enemyPosition = candidate;
                }
                return enemyDistance < float.MaxValue;
            }

            private static bool WantsObjective(int route, ContentId id)
            {
                if (route == RouteSword) return id == GuideObjectiveId;
                if (route == RouteTalisman) return id == ListenObjectiveId || id == StopObjectiveId;
                return id == GuideObjectiveId || id == ListenObjectiveId || id == StopObjectiveId;
            }
        }
    }

    public static class QinglanG34BalanceRules
    {
        public static void Evaluate(QinglanG34BalanceReport report)
        {
            if (report == null) throw new ArgumentNullException(nameof(report));
            var failures = string.Empty;
            var totalWins = 0;
            var routeWins = new int[3];
            var matrix = report.matrix ?? Array.Empty<QinglanG34RunSummary>();
            if (matrix.Length != 15) Append(ref failures, "matrix_count");
            for (var index = 0; index < matrix.Length; index++)
            {
                var run = matrix[index];
                if (run == null || !run.ended || run.timedOut || run.invalidHandleAccesses != 0 || !run.positionsWalkable)
                    Append(ref failures, "matrix_runtime_" + index);
                if (run != null && run.victory)
                {
                    totalWins++;
                    if (run.route >= 0 && run.route < routeWins.Length) routeWins[run.route]++;
                }
            }
            report.totalVictories = totalWins;
            report.totalDefeats = matrix.Length - totalWins;
            report.failureRate = matrix.Length == 0 ? 1f : (float)report.totalDefeats / matrix.Length;
            report.routeVictories = routeWins;
            if (totalWins < 10 || totalWins > 13) Append(ref failures, "victory_band");
            for (var route = 0; route < routeWins.Length; route++)
                if (routeWins[route] < 3 || routeWins[route] > 5) Append(ref failures, "route_band_" + route);

            var replays = report.goldenReplays ?? Array.Empty<QinglanG34RunSummary>();
            report.deterministicGoldenReplay = replays.Length == 3;
            for (var route = 0; route < 3; route++)
            {
                var goldenIndex = route * QinglanG34BalanceCommand.MatrixSeedsPerRoute;
                if (goldenIndex >= matrix.Length || route >= replays.Length ||
                    !GoldenValid(matrix[goldenIndex], route) || !Equivalent(matrix[goldenIndex], replays[route]))
                {
                    report.deterministicGoldenReplay = false;
                    Append(ref failures, "golden_" + route);
                }
            }
            if (matrix.Length >= 11 &&
                (matrix[0].decisionChecksum == matrix[5].decisionChecksum ||
                 matrix[0].decisionChecksum == matrix[10].decisionChecksum ||
                 matrix[5].decisionChecksum == matrix[10].decisionChecksum))
                Append(ref failures, "route_decisions_not_distinct");

            var probes = report.failureProbes ?? Array.Empty<QinglanG34RunSummary>();
            report.failureProbesPassed = probes.Length == 3;
            for (var index = 0; index < probes.Length; index++)
            {
                var run = probes[index];
                if (run == null || !run.ended || run.victory || run.reason != RunEndReason.PlayerDefeated.ToString() ||
                    run.bossDefeats >= 2 || run.timedOut)
                {
                    report.failureProbesPassed = false;
                    Append(ref failures, "failure_probe_" + index);
                }
            }

            var covered = new bool[QinglanG34BalanceCommand.GetAllRelicIds().Length];
            var relicIds = QinglanG34BalanceCommand.GetAllRelicIds();
            for (var runIndex = 0; runIndex < matrix.Length; runIndex++)
            {
                var relics = matrix[runIndex]?.relics ?? Array.Empty<string>();
                for (var relic = 0; relic < relicIds.Length; relic++)
                    for (var entry = 0; entry < relics.Length; entry++)
                        if (relics[entry].StartsWith(relicIds[relic] + "@", StringComparison.Ordinal)) covered[relic] = true;
            }
            report.relicsSelectedInMatrix = 0;
            for (var index = 0; index < covered.Length; index++) if (covered[index]) report.relicsSelectedInMatrix++;
            if (report.relicsSelectedInMatrix != relicIds.Length) Append(ref failures, "relic_matrix_coverage");
            var compatibility = report.relicCompatibilityCases ?? Array.Empty<QinglanG34RelicCompatibilitySummary>();
            report.relicCompatibilityPassed = compatibility.Length == 18;
            for (var index = 0; index < compatibility.Length; index++)
                report.relicCompatibilityPassed &= compatibility[index] != null && compatibility[index].passed;
            if (!report.relicCompatibilityPassed) Append(ref failures, "relic_compatibility");

            report.status = failures.Length == 0 ? "PASS" : "FAIL";
            report.failureReason = failures;
        }

        public static bool Equivalent(QinglanG34RunSummary left, QinglanG34RunSummary right)
        {
            return left != null && right != null &&
                   left.completedTicks == right.completedTicks && left.victory == right.victory &&
                   left.reason == right.reason && left.level == right.level &&
                   left.enemyDefeats == right.enemyDefeats && left.eliteDefeats == right.eliteDefeats &&
                   left.bossDefeats == right.bossDefeats && left.pickupsCollected == right.pickupsCollected &&
                   left.offersSelected == right.offersSelected && left.rewardsSelected == right.rewardsSelected &&
                   left.completedObjectives == right.completedObjectives &&
                   left.completedEvents == right.completedEvents && left.claimedLandmarks == right.claimedLandmarks &&
                   left.spawnChecksum == right.spawnChecksum && left.objectiveChecksum == right.objectiveChecksum &&
                   left.bossChecksum == right.bossChecksum && left.decisionChecksum == right.decisionChecksum &&
                   left.buildChecksum == right.buildChecksum && left.combinedChecksum == right.combinedChecksum;
        }

        private static bool GoldenValid(QinglanG34RunSummary run, int route)
        {
            return run != null && run.route == route && run.victory && run.bossDefeats == 2 &&
                   run.completedTicks >= QinglanG34BalanceCommand.MinimumGoldenTicks &&
                   run.completedTicks <= QinglanG34BalanceCommand.MaximumRunTicks &&
                   run.targetSkillPeakLevel >= 8 && run.targetPassivePeakLevel >= 5 &&
                   run.targetEvolutionApplied && run.invalidHandleAccesses == 0;
        }

        private static void Append(ref string failures, string value)
        {
            failures = failures.Length == 0 ? value : failures + "," + value;
        }
    }

    [Serializable]
    public sealed class QinglanG34BalanceReport
    {
        public int schemaVersion;
        public string status;
        public string failureReason;
        public string generatedAtUtc;
        public string unityVersion;
        public int minimumGoldenTicks;
        public int maximumRunTicks;
        public int totalVictories;
        public int totalDefeats;
        public float failureRate;
        public int[] routeVictories;
        public bool deterministicGoldenReplay;
        public bool failureProbesPassed;
        public int relicsSelectedInMatrix;
        public bool relicCompatibilityPassed;
        public QinglanG34RunSummary[] matrix;
        public QinglanG34RunSummary[] goldenReplays;
        public QinglanG34RunSummary[] failureProbes;
        public QinglanG34RelicCompatibilitySummary[] relicCompatibilityCases;
    }

    [Serializable]
    public sealed class QinglanG34RunSummary
    {
        public string seed;
        public int route;
        public string routeKey;
        public bool failureProbe;
        public bool ended;
        public bool timedOut;
        public string reason;
        public bool victory;
        public long completedTicks;
        public double durationSeconds;
        public int level;
        public float minimumHealth;
        public float maximumHealth;
        public int targetSkillPeakLevel;
        public int targetPassivePeakLevel;
        public bool targetEvolutionApplied;
        public long enemyDefeats;
        public long eliteDefeats;
        public long bossDefeats;
        public long pickupsCollected;
        public int offersSelected;
        public int rewardsSelected;
        public int completedObjectives;
        public int completedEvents;
        public int claimedLandmarks;
        public double movementDistance;
        public int peakEnemies;
        public int peakProjectiles;
        public int peakAreas;
        public bool positionsWalkable;
        public long invalidHandleAccesses;
        public int activeEntitiesBeforeDispose;
        public string[] skills;
        public string[] passives;
        public string[] relics;
        public string[] evolutions;
        public string spawnChecksum;
        public string objectiveChecksum;
        public string bossChecksum;
        public string decisionChecksum;
        public string buildChecksum;
        public string combinedChecksum;
    }

    [Serializable]
    public sealed class QinglanG34RelicCompatibilitySummary
    {
        public int route;
        public string relicId;
        public int tickCount;
        public bool outputResolved;
        public bool cleanupPassed;
        public bool passed;
    }
}
