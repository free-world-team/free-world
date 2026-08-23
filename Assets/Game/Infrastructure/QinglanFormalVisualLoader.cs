using System;
using System.Collections.Generic;
using Game.Core;
using Game.Presentation;
using Game.UI;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.ResourceManagement.AsyncOperations;

namespace Game.Infrastructure
{
    /// <summary>Startup-only Addressables owner for the Qinglan formal visual catalog.</summary>
    public sealed class QinglanFormalVisualLoader : IDisposable, IQinglanUiVisualCatalog
    {
        public const string CatalogAddress = "qinglan/runtime/formal-visual-catalog";

        private static readonly string[] MapTileAddresses =
        {
            "qinglan/map/old-court/region/central-training-ground/tile-kit",
            "qinglan/map/old-court/region/west-herb-garden/tile-kit",
            "qinglan/map/old-court/region/east-sword-gallery/tile-kit",
            "qinglan/map/old-court/region/north-old-gate/tile-kit",
            "qinglan/map/old-court/region/south-guest-court/tile-kit"
        };

        private static readonly string[] MapPropAddresses =
        {
            "qinglan/map/old-court/region/central-training-ground/prop-set",
            "qinglan/map/old-court/region/west-herb-garden/prop-set",
            "qinglan/map/old-court/region/east-sword-gallery/prop-set",
            "qinglan/map/old-court/region/north-old-gate/prop-set",
            "qinglan/map/old-court/region/south-guest-court/prop-set"
        };

        private static readonly string[] MapRegionKeys =
        {
            "central_training_ground",
            "west_herb_garden",
            "east_sword_gallery",
            "north_old_gate",
            "south_guest_court"
        };

        private static readonly string[] MapStateContentIds =
        {
            "qinglan.objective.wind_altar.listen",
            "qinglan.objective.wind_altar.guide",
            "qinglan.objective.wind_altar.stop_balance",
            "qinglan.landmark.wind_vein_stele",
            "qinglan.landmark.sealed_sword_cache",
            "qinglan.landmark.herb_garden_variant",
            "qinglan.landmark.broken_wall_sword_mark",
            "qinglan.landmark.guest_pavilion_letter"
        };

        private static readonly string[] MapStateAddresses =
        {
            "qinglan/objective/wind-altar/listen/state-atlas",
            "qinglan/objective/wind-altar/guide/state-atlas",
            "qinglan/objective/wind-altar/stop-balance/state-atlas",
            "qinglan/landmark/wind-vein-stele/state-atlas",
            "qinglan/landmark/sealed-sword-cache/state-atlas",
            "qinglan/landmark/herb-garden-variant/state-atlas",
            "qinglan/landmark/broken-wall-sword-mark/state-atlas",
            "qinglan/landmark/guest-pavilion-letter/state-atlas"
        };

        private static readonly string[] MapStateSpritePrefixes =
        {
            "qinglan.presentation.objective.old_court.listen",
            "qinglan.presentation.objective.old_court.guide",
            "qinglan.presentation.objective.old_court.stop_balance",
            "qinglan.presentation.landmark.old_court.wind_vein_stele",
            "qinglan.presentation.landmark.old_court.sealed_sword_cache",
            "qinglan.presentation.landmark.old_court.herb_garden_variant",
            "qinglan.presentation.landmark.old_court.broken_wall_sword_mark",
            "qinglan.presentation.landmark.old_court.guest_pavilion_letter"
        };

        private AsyncOperationHandle<FormalVisualCatalog> handle;
        private bool ownsHandle;
        private readonly List<AsyncOperationHandle<Sprite>> spriteHandles =
            new List<AsyncOperationHandle<Sprite>>(160);
        private readonly List<Sprite> mapTiles = new List<Sprite>(80);
        private readonly List<Sprite> mapProps = new List<Sprite>(80);
        private readonly Dictionary<string, Sprite[]> mapStateSprites =
            new Dictionary<string, Sprite[]>(StringComparer.Ordinal);
        private readonly Dictionary<string, Sprite> resolvedSprites =
            new Dictionary<string, Sprite>(StringComparer.Ordinal);
        private readonly HashSet<string> failedSpriteKeys =
            new HashSet<string>(StringComparer.Ordinal);

        public FormalVisualCatalog Catalog { get; private set; }
        public string LastError { get; private set; } = string.Empty;
        public bool IsLoaded => Catalog != null;
        public IReadOnlyList<Sprite> MapTiles => mapTiles;
        public IReadOnlyList<Sprite> MapProps => mapProps;
        public IReadOnlyDictionary<string, Sprite[]> MapStateSprites => mapStateSprites;
        public DirectionalSpriteCatalog DirectionalSprites { get; private set; } = new DirectionalSpriteCatalog();

        public bool LoadForStartup()
        {
            if (Catalog != null) return true;
            try
            {
                handle = Addressables.LoadAssetAsync<FormalVisualCatalog>(CatalogAddress);
                ownsHandle = true;
                Catalog = handle.WaitForCompletion();
                if (handle.Status == AsyncOperationStatus.Succeeded && Catalog != null &&
                    Catalog.SchemaVersion == FormalVisualCatalog.CurrentSchemaVersion)
                {
                    LoadMapSpriteSets();
                    LoadMapStateSpriteSets();
                    LoadDirectionalSpriteSets();
                    return true;
                }
                LastError = "Formal visual catalog failed or has an unsupported schema.";
            }
            catch (Exception exception)
            {
                LastError = exception.GetType().Name + ": " + exception.Message;
            }

            ReleaseHandle();
            Catalog = null;
            return false;
        }

        public bool TryResolveSprite(string stableKey, out Sprite sprite)
        {
            if (Catalog == null || string.IsNullOrEmpty(stableKey))
            {
                sprite = null;
                return false;
            }
            if (Catalog.TryResolveSprite(stableKey, out sprite)) return true;
            if (resolvedSprites.TryGetValue(stableKey, out sprite)) return sprite != null;
            if (failedSpriteKeys.Contains(stableKey) ||
                !Catalog.TryResolveAddress(stableKey, out var address) || string.IsNullOrEmpty(address))
            {
                sprite = null;
                return false;
            }

            // CatalogOnly entries intentionally keep the catalog asset light. UI icons and
            // short-lived formal VFX resolve their local Addressable once on first use, then
            // stay cached under this startup owner instead of falling back to ui.focus.
            try
            {
                var operation = Addressables.LoadAssetAsync<Sprite>(address);
                spriteHandles.Add(operation);
                sprite = operation.WaitForCompletion();
                if (operation.Status == AsyncOperationStatus.Succeeded && sprite != null)
                {
                    resolvedSprites.Add(stableKey, sprite);
                    return true;
                }
            }
            catch (Exception)
            {
                // The stable miss is cached below so a malformed catalog entry cannot turn
                // into repeated Addressables work on a presentation hot path.
            }
            failedSpriteKeys.Add(stableKey);
            sprite = null;
            return false;
        }

        public void Dispose()
        {
            Catalog = null;
            for (var index = spriteHandles.Count - 1; index >= 0; index--)
                Addressables.Release(spriteHandles[index]);
            spriteHandles.Clear();
            mapTiles.Clear();
            mapProps.Clear();
            mapStateSprites.Clear();
            resolvedSprites.Clear();
            failedSpriteKeys.Clear();
            DirectionalSprites = new DirectionalSpriteCatalog();
            ReleaseHandle();
        }

        private void LoadMapSpriteSets()
        {
            for (var index = 0; index < MapTileAddresses.Length; index++)
                LoadSpriteGrid(MapTileAddresses[index], MapRegionKeys[index], "tile", mapTiles);
            for (var index = 0; index < MapPropAddresses.Length; index++)
                LoadSpriteGrid(MapPropAddresses[index], MapRegionKeys[index], "prop", mapProps);
            Debug.Log("[Qinglan Formal Visuals] Map tiles=" + mapTiles.Count +
                      ", props=" + mapProps.Count + ".");
        }

        private void LoadSpriteGrid(string address, string regionKey, string kind, List<Sprite> target)
        {
            for (var row = 0; row < 2; row++)
            {
                for (var column = 0; column < 8; column++)
                {
                    var spriteName = "qinglan.presentation.map.old_court.region." + regionKey +
                                     "." + kind + ".r" + row + ".c" + column;
                    var operation = Addressables.LoadAssetAsync<Sprite>(address + "[" + spriteName + "]");
                    spriteHandles.Add(operation);
                    var sprite = operation.WaitForCompletion();
                    if (operation.Status == AsyncOperationStatus.Succeeded && sprite != null)
                        target.Add(sprite);
                }
            }
        }

        private void LoadMapStateSpriteSets()
        {
            var objectiveStates = new[] { "idle", "active", "complete" };
            var landmarkStates = new[] { "undiscovered", "discovered", "claimed" };
            for (var setIndex = 0; setIndex < MapStateContentIds.Length; setIndex++)
            {
                var states = setIndex < 3 ? objectiveStates : landmarkStates;
                var sprites = new Sprite[states.Length];
                var complete = true;
                for (var stateIndex = 0; stateIndex < states.Length; stateIndex++)
                {
                    var spriteName = MapStateSpritePrefixes[setIndex] + "." + states[stateIndex];
                    var operation = Addressables.LoadAssetAsync<Sprite>(
                        MapStateAddresses[setIndex] + "[" + spriteName + "]");
                    spriteHandles.Add(operation);
                    var sprite = operation.WaitForCompletion();
                    sprites[stateIndex] = sprite;
                    complete &= operation.Status == AsyncOperationStatus.Succeeded && sprite != null;
                }
                if (complete) mapStateSprites.Add(MapStateContentIds[setIndex], sprites);
            }
            Debug.Log("[Qinglan Formal Visuals] Map state sets=" + mapStateSprites.Count + ".");
        }
        private void LoadDirectionalSpriteSets()
        {
            var sets = new List<DirectionalSpriteSet>(9);
            LoadDirectionalSet(
                sets,
                "qinglan.character.lu_qingye",
                "qinglan/character/lu-qingye/directional-animation-atlas",
                "lu-qingye",
                "idle", "move", "imperial-sword", "hit", "down", "victory");
            LoadDirectionalSet(sets, "qinglan.enemy.grass_spirit", "qinglan/enemy/grass-spirit/directional-animation-atlas", "qinglan.enemy.grass-spirit", "move", "move", "attack-windup", "hit", "death", "move");
            LoadDirectionalSet(sets, "qinglan.enemy.paper_crane_spirit", "qinglan/enemy/paper-crane-spirit/directional-animation-atlas", "qinglan.enemy.paper-crane-spirit", "move", "move", "attack-windup", "hit", "death", "move");
            LoadDirectionalSet(sets, "qinglan.enemy.wooden_sword_puppet", "qinglan/enemy/wooden-sword-puppet/directional-animation-atlas", "qinglan.enemy.wooden-sword-puppet", "move", "move", "attack-windup", "hit", "death", "move");
            LoadDirectionalSet(sets, "qinglan.enemy.stone_lantern_guard", "qinglan/enemy/stone-lantern-guard/directional-animation-atlas", "qinglan.enemy.stone-lantern-guard", "move", "move", "attack-windup", "hit", "death", "move");
            LoadDirectionalSet(sets, "qinglan.enemy.wind_bell_spirit", "qinglan/enemy/wind-bell-spirit/directional-animation-atlas", "qinglan.enemy.wind-bell-spirit", "move", "move", "attack-windup", "hit", "death", "move");
            LoadDirectionalSet(sets, "qinglan.enemy.explosive_seed_pod", "qinglan/enemy/explosive-seed-pod/directional-animation-atlas", "qinglan.enemy.explosive-seed-pod", "move", "move", "attack-windup", "hit", "death", "move");
            LoadDirectionalSet(sets, "qinglan.enemy.boss.tingfeng", "qinglan/boss/tingfeng/animation-atlas", "qinglan.boss.tingfeng", "move", "move", "phase-1-windup", "hit", "defeated", "transition-3", "phase-1-windup", "phase-2-windup", "phase-3-windup");
            LoadDirectionalSet(sets, "qinglan.enemy.boss.zhezhi", "qinglan/boss/zhezhi/animation-atlas", "qinglan.boss.zhezhi", "move", "move", "phase-1-windup", "hit", "defeated", "transition-3", "phase-1-windup", "phase-2-windup", "phase-3-windup");
            DirectionalSprites = new DirectionalSpriteCatalog(sets.ToArray());
            Debug.Log("[Qinglan Formal Visuals] Directional sprite sets=" + DirectionalSprites.Count +
                      ", boss phase sets=" + DirectionalSprites.BossPhaseSetCount +
                      ", boss phase sprites=" + DirectionalSprites.BossPhaseSpriteCount + ".");
        }

        private void LoadDirectionalSet(
            List<DirectionalSpriteSet> target,
            string profileId,
            string address,
            string spritePrefix,
            string idle,
            string move,
            string attack,
            string hit,
            string death,
            string victory,
            string bossPhase1 = null,
            string bossPhase2 = null,
            string bossPhase3 = null)
        {
            var id = ContentId.Create(profileId);
            if (!id.IsSuccess) return;
            var states = new[] { idle, move, attack, hit, death, victory };
            var directions = new[] { "down", "left", "right", "up" };
            var sprites = new Sprite[DirectionalSpriteSet.FacingCount * DirectionalSpriteSet.PoseCount];
            for (var directionIndex = 0; directionIndex < directions.Length; directionIndex++)
            {
                for (var poseIndex = 0; poseIndex < states.Length; poseIndex++)
                {
                    var duplicate = -1;
                    for (var earlier = 0; earlier < poseIndex; earlier++)
                        if (string.Equals(states[earlier], states[poseIndex], StringComparison.Ordinal))
                        {
                            duplicate = earlier;
                            break;
                        }
                    if (duplicate >= 0)
                    {
                        sprites[(directionIndex * DirectionalSpriteSet.PoseCount) + poseIndex] =
                            sprites[(directionIndex * DirectionalSpriteSet.PoseCount) + duplicate];
                        continue;
                    }

                    var spriteName = spritePrefix + "." + directions[directionIndex] + "." + states[poseIndex];
                    var operation = Addressables.LoadAssetAsync<Sprite>(address + "[" + spriteName + "]");
                    spriteHandles.Add(operation);
                    var sprite = operation.WaitForCompletion();
                    if (operation.Status == AsyncOperationStatus.Succeeded)
                        sprites[(directionIndex * DirectionalSpriteSet.PoseCount) + poseIndex] = sprite;
                }
            }
            Sprite[] phaseSprites = null;
            if (!string.IsNullOrEmpty(bossPhase1) &&
                !string.IsNullOrEmpty(bossPhase2) &&
                !string.IsNullOrEmpty(bossPhase3))
            {
                var phases = new[] { bossPhase1, bossPhase2, bossPhase3 };
                phaseSprites = new Sprite[DirectionalSpriteSet.FacingCount * DirectionalSpriteSet.BossPhaseCount];
                for (var directionIndex = 0; directionIndex < directions.Length; directionIndex++)
                {
                    for (var phaseIndex = 0; phaseIndex < phases.Length; phaseIndex++)
                    {
                        var spriteName = spritePrefix + "." + directions[directionIndex] + "." + phases[phaseIndex];
                        var operation = Addressables.LoadAssetAsync<Sprite>(address + "[" + spriteName + "]");
                        spriteHandles.Add(operation);
                        var sprite = operation.WaitForCompletion();
                        if (operation.Status == AsyncOperationStatus.Succeeded)
                            phaseSprites[(directionIndex * DirectionalSpriteSet.BossPhaseCount) + phaseIndex] = sprite;
                    }
                }
            }
            target.Add(new DirectionalSpriteSet(id.Value, sprites, phaseSprites));
        }

        private void ReleaseHandle()
        {
            if (!ownsHandle) return;
            Addressables.Release(handle);
            ownsHandle = false;
        }
    }
}
