using System;
using Game.Core;
using Game.Presentation;
using Game.Simulation;
using UnityEngine;

namespace Game.Infrastructure
{
    /// <summary>
    /// Prewarmed formal-sprite presentation load used only by the opt-in G3.5 Player gate.
    /// It consumes the production render snapshot but never writes simulation truth.
    /// </summary>
    internal sealed class QinglanG35FormalPresentationProbe : IDisposable
    {
        private const float WorldScale = 0.04f;
        private readonly GameObject root;
        private readonly View player;
        private readonly View[] enemies;
        private readonly View[] projectiles;
        private readonly View[] pickups;
        private readonly View[] vfx;
        private int invalidBindings;
        private int droppedRequests;

        internal QinglanG35FormalPresentationProbe(
            Transform owner,
            FormalVisualCatalog catalog,
            int enemyCount,
            int projectileCount,
            int pickupCount,
            int vfxCount)
        {
            if (owner == null) throw new ArgumentNullException(nameof(owner));
            if (catalog == null) throw new ArgumentNullException(nameof(catalog));
            if (enemyCount <= 0 || projectileCount <= 0 || pickupCount <= 0 || vfxCount <= 0)
                throw new ArgumentOutOfRangeException(nameof(enemyCount));

            root = new GameObject("Qinglan_G3_5_Formal_Performance_Probe");
            root.transform.SetParent(owner, false);
            root.SetActive(false);

            var playerProfile = ResolveProfile(
                catalog,
                "qinglan.character.lu_qingye",
                EntityKind.Actor);
            var enemyProfile = ResolveProfile(
                catalog,
                "qinglan.enemy.grass_spirit",
                EntityKind.Actor);
            var projectileProfile = ResolveProfile(
                catalog,
                "qinglan.presentation.skill.yufeng_sword",
                EntityKind.Projectile);
            var pickupProfile = ResolveProfile(
                catalog,
                "qinglan.pickup.greenwood_dew",
                EntityKind.Pickup);
            if (!catalog.TryResolveSprite("qinglan.status.burning", out var vfxSprite) ||
                vfxSprite == null)
                throw new InvalidOperationException("The formal G3.5 VFX sprite is missing.");

            player = CreateOutlinedView("Player", playerProfile, 30, 0.30f);
            enemies = CreateOutlinedViews("Enemy", enemyCount, enemyProfile, 20, 0.24f);
            projectiles = CreateOutlinedViews("Projectile", projectileCount, projectileProfile, 25, 0.22f);
            pickups = CreateOutlinedViews("Pickup", pickupCount, pickupProfile, 15, 0.20f);
            vfx = CreateViews("Vfx", vfxCount, vfxSprite, Color.white, Vector2.one, 28, 0.20f);
            ExpectedViews = 1 + enemyCount + projectileCount + pickupCount + vfxCount;
            CreatedViews = ExpectedViews;
            ExpectedSpriteRenderers = (1 + enemyCount + projectileCount + pickupCount) * 2 + vfxCount;
            CreatedSpriteRenderers = ExpectedSpriteRenderers;
            ResolvedFormalProfiles = 5;
            root.SetActive(true);
        }

        internal int ExpectedViews { get; }
        internal int CreatedViews { get; }
        internal int ExpectedSpriteRenderers { get; }
        internal int CreatedSpriteRenderers { get; }
        internal int ResolvedFormalProfiles { get; }
        internal int InvalidBindings => invalidBindings;
        internal int DroppedRequests => droppedRequests;

        internal void Sync(RenderSnapshot snapshot, float alpha, long frameIndex)
        {
            if (snapshot == null)
            {
                invalidBindings++;
                return;
            }

            var actorIndex = 0;
            var projectileIndex = 0;
            var pickupIndex = 0;
            for (var index = 0; index < snapshot.Count; index++)
            {
                var item = snapshot.GetAt(index);
                switch (item.Entity.Kind)
                {
                    case EntityKind.Actor:
                        if (actorIndex == 0) SetSnapshot(player, item, alpha);
                        else if (actorIndex - 1 < enemies.Length)
                            SetSnapshot(enemies[actorIndex - 1], item, alpha);
                        else invalidBindings++;
                        actorIndex++;
                        break;
                    case EntityKind.Projectile:
                        if (projectileIndex < projectiles.Length)
                            SetSnapshot(projectiles[projectileIndex], item, alpha);
                        else invalidBindings++;
                        projectileIndex++;
                        break;
                    case EntityKind.Pickup:
                        if (pickupIndex < pickups.Length)
                            SetSnapshot(pickups[pickupIndex], item, alpha);
                        else invalidBindings++;
                        pickupIndex++;
                        break;
                    default:
                        invalidBindings++;
                        break;
                }
            }

            if (actorIndex != enemies.Length + 1 || projectileIndex != projectiles.Length ||
                pickupIndex != pickups.Length) invalidBindings++;
            SyncVfx(frameIndex);
        }

        internal QinglanG35PoolMetrics CaptureMetrics()
        {
            return new QinglanG35PoolMetrics
            {
                expectedViews = ExpectedViews,
                createdViews = CreatedViews,
                expectedSpriteRenderers = ExpectedSpriteRenderers,
                createdSpriteRenderers = CreatedSpriteRenderers,
                peakActiveViews = CreatedViews,
                expansionsAfterWarmup = 0,
                droppedRequests = droppedRequests,
                invalidBindings = invalidBindings,
                resolvedFormalProfiles = ResolvedFormalProfiles
            };
        }

        public void Dispose()
        {
            if (root != null) UnityEngine.Object.Destroy(root);
        }

        private void SyncVfx(long frameIndex)
        {
            var phase = (float)(frameIndex % 3600L) * 0.0025f;
            for (var index = 0; index < vfx.Length; index++)
            {
                var angle = index * 2.39996323f + phase;
                var radius = 0.5f + (index % 20) * 0.18f;
                vfx[index].Transform.localPosition = new Vector3(
                    Mathf.Cos(angle) * radius,
                    Mathf.Sin(angle) * radius,
                    0f);
                vfx[index].Transform.localRotation = Quaternion.Euler(0f, 0f, angle * Mathf.Rad2Deg);
            }
        }

        private static void SetSnapshot(View view, in RenderEntitySnapshot item, float alpha)
        {
            var position = item.InterpolatePosition(alpha);
            var facing = item.InterpolateFacing(alpha);
            view.Transform.localPosition = new Vector3(
                position.X * WorldScale,
                position.Y * WorldScale,
                0f);
            view.Transform.localRotation = Quaternion.Euler(0f, 0f, facing * Mathf.Rad2Deg);
        }

        private View[] CreateOutlinedViews(
            string prefix,
            int count,
            VisualProfile profile,
            int sortingOrder,
            float scale)
        {
            var output = new View[count];
            for (var index = 0; index < count; index++)
                output[index] = CreateOutlinedView(prefix + "_" + index, profile, sortingOrder, scale);
            return output;
        }

        private View CreateOutlinedView(
            string name,
            VisualProfile profile,
            int sortingOrder,
            float scale)
        {
            var view = CreateView(name, profile.Sprite, profile.Color, profile.Size, sortingOrder, scale);
            var outlineObject = new GameObject("Outline");
            outlineObject.transform.SetParent(view.Transform, false);
            outlineObject.transform.localScale = Vector3.one * 1.16f;
            var outline = outlineObject.AddComponent<SpriteRenderer>();
            outline.sprite = profile.Sprite;
            outline.color = new Color(0.03f, 0.04f, 0.05f, 0.82f);
            outline.sortingOrder = sortingOrder - 1;
            return view;
        }

        private View[] CreateViews(
            string prefix,
            int count,
            Sprite sprite,
            Color color,
            Vector2 size,
            int sortingOrder,
            float scale)
        {
            var output = new View[count];
            for (var index = 0; index < count; index++)
                output[index] = CreateView(prefix + "_" + index, sprite, color, size, sortingOrder, scale);
            return output;
        }

        private View CreateView(
            string name,
            Sprite sprite,
            Color color,
            Vector2 size,
            int sortingOrder,
            float scale)
        {
            var value = new GameObject(name);
            value.transform.SetParent(root.transform, false);
            value.transform.localScale = new Vector3(size.x * scale, size.y * scale, 1f);
            var renderer = value.AddComponent<SpriteRenderer>();
            renderer.sprite = sprite;
            renderer.color = color;
            renderer.sortingOrder = sortingOrder;
            return new View(value.transform);
        }

        private static VisualProfile ResolveProfile(
            FormalVisualCatalog catalog,
            string stableId,
            EntityKind kind)
        {
            var id = ContentId.Create(stableId);
            if (!id.IsSuccess || !catalog.TryResolveProfile(id.Value, kind, out var profile) ||
                profile == null || profile.Sprite == null)
                throw new InvalidOperationException("The formal G3.5 profile is missing: " + stableId);
            return profile;
        }

        private readonly struct View
        {
            internal View(Transform transformValue) => Transform = transformValue;
            internal Transform Transform { get; }
        }
    }
}
