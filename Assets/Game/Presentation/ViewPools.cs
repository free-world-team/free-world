using System;
using System.Collections.Generic;
using Game.Application;
using Game.Core;
using Game.Simulation;
using UnityEngine;

namespace Game.Presentation
{
    /// <summary>Reusable pool for one high-frequency entity view type.</summary>
    public sealed class EntityViewPool<T> : IDisposable where T : EntityView
    {
        private readonly Stack<T> available;
        private readonly List<T> all;
        private readonly HashSet<T> owned;
        private readonly Transform root;
        private readonly EntityKind kind;
        private readonly VisualProfileCatalog profiles;
        private readonly ProceduralPresentationCatalog proceduralProfiles;
        private readonly AccessibilitySettings settings;
        private readonly ProceduralVisualLibrary fallback;
        private readonly DirectionalSpriteCatalog directionalSprites;
        private readonly Sprite heldWeaponSprite;

        internal EntityViewPool(
            Transform poolRoot,
            EntityKind entityKind,
            VisualProfileCatalog catalog,
            ProceduralPresentationCatalog proceduralCatalog,
            AccessibilitySettings accessibilitySettings,
            ProceduralVisualLibrary proceduralFallback,
            DirectionalSpriteCatalog directionalSpriteCatalog,
            int prewarm,
            Sprite defaultHeldWeaponSprite = null)
        {
            root = poolRoot ?? throw new ArgumentNullException(nameof(poolRoot));
            kind = entityKind;
            profiles = catalog ?? throw new ArgumentNullException(nameof(catalog));
            proceduralProfiles = proceduralCatalog ?? throw new ArgumentNullException(nameof(proceduralCatalog));
            settings = accessibilitySettings ?? throw new ArgumentNullException(nameof(accessibilitySettings));
            fallback = proceduralFallback ?? throw new ArgumentNullException(nameof(proceduralFallback));
            directionalSprites = directionalSpriteCatalog ?? throw new ArgumentNullException(nameof(directionalSpriteCatalog));
            heldWeaponSprite = defaultHeldWeaponSprite;
            available = new Stack<T>(Math.Max(1, prewarm));
            all = new List<T>(Math.Max(1, prewarm));
            owned = new HashSet<T>();
            for (var index = 0; index < prewarm; index++) available.Push(Create());
        }

        public int CreatedCount => all.Count;
        public int AvailableCount => available.Count;
        public int ActiveCount => all.Count - available.Count;
        public long AnimationFrameChangeCount
        {
            get
            {
                long count = 0;
                for (var index = 0; index < all.Count; index++) count += all[index].AnimationFrameChangeCount;
                return count;
            }
        }

        public T Acquire(
            SpatialEntity entity,
            ContentId visualProfileId,
            bool playerStyle,
            out bool usedFallback)
        {
            if (entity.Kind != kind) throw new ArgumentException("Entity kind does not match this pool.", nameof(entity));
            var view = available.Count > 0 ? available.Pop() : Create();
            if (profiles.TryResolve(visualProfileId, kind, out var profile))
            {
                ConfigureFormal(view, profile, playerStyle);
                usedFallback = profile.Sprite == null;
            }
            else
            {
                usedFallback = !proceduralProfiles.TryResolve(
                    visualProfileId,
                    kind,
                    playerStyle,
                    settings.ColorVision,
                    out var style);
                view.Configure(style, fallback);
            }
            view.SetStyleIdentity(visualProfileId, playerStyle);
            view.ConfigureAnimation(
                directionalSprites.TryResolve(visualProfileId, out var spriteSet) ? spriteSet : null);
            view.ConfigureHeldWeapon(playerStyle ? heldWeaponSprite : null);
            if (kind == EntityKind.Projectile) view.ConfigureProjectileTrail(fallback.TrailMaterial);
            view.Bind(entity);
            view.ConfigureQingciReadability(
                kind,
                playerStyle,
                settings.ColorVision == ColorVisionMode.HighContrast,
                fallback);
            return view;
        }

        internal void RefreshStyle(T view)
        {
            if (view == null || !view.IsBound) return;
            if (profiles.TryResolve(view.ProfileId, kind, out var profile))
            {
                ConfigureFormal(view, profile, view.UsesPlayerStyle);
            }
            else
            {
                proceduralProfiles.TryResolve(
                    view.ProfileId,
                    kind,
                    view.UsesPlayerStyle,
                    settings.ColorVision,
                    out var style);
                view.Configure(style, fallback);
            }
            view.ConfigureQingciReadability(
                kind,
                view.UsesPlayerStyle,
                settings.ColorVision == ColorVisionMode.HighContrast,
                fallback);
        }

        internal bool ApplyOverlay(T view, int index, ContentId overlayId)
        {
            if (view == null || !view.IsBound || !overlayId.IsValid) return false;
            if (profiles.TryResolve(overlayId, kind, out var formal) && formal.Sprite != null)
            {
                view.SetOverlay(
                    index,
                    formal.Sprite,
                    FormalTint(formal.Color, false),
                    PresentationPriority.Mechanic);
                return true;
            }
            if (!proceduralProfiles.TryResolve(
                    overlayId,
                    kind,
                    false,
                    settings.ColorVision,
                    out var style))
                return false;
            view.SetOverlay(index, style, fallback);
            return true;
        }

        private void ConfigureFormal(T view, VisualProfile profile, bool playerStyle)
        {
            var outline = settings.ColorVision == ColorVisionMode.HighContrast ?
                (playerStyle ? Color.white : Color.black) : new Color(0.04f, 0.05f, 0.05f, 0.78f);
            var size = profile.Size;
            var defaultExperiencePickup = kind == EntityKind.Pickup &&
                string.Equals(
                    profile.StableId,
                    "qinglan.pickup.riding_wind_feather",
                    StringComparison.Ordinal);
            if (kind == EntityKind.Actor)
            {
                var scale = playerStyle ? 1.65f :
                    profile.StableId.IndexOf(".boss.", StringComparison.Ordinal) >= 0 ? 2.2f : 1.4f;
                size *= scale;
            }
            else if (defaultExperiencePickup)
                size *= 0.52f;
            var tint = FormalTint(profile.Color, playerStyle);
            if (defaultExperiencePickup) tint.a = Mathf.Min(tint.a, 0.84f);
            view.Configure(
                profile.Sprite != null ? profile.Sprite : fallback.Sprite,
                tint,
                size,
                FormalPriority(profile, playerStyle),
                FormalShape(profile, playerStyle),
                outline,
                true);
        }

        private Color FormalTint(Color source, bool playerStyle)
        {
            if (settings.ColorVision == ColorVisionMode.Standard) return source;
            var accessibility = ProceduralPresentationCatalog.ApplyColorVision(
                ProceduralPresentationCatalog.Fallback(kind, playerStyle),
                settings.ColorVision);
            return new Color(
                source.r * accessibility.Color.r,
                source.g * accessibility.Color.g,
                source.b * accessibility.Color.b,
                source.a);
        }

        private PresentationPriority FormalPriority(VisualProfile profile, bool playerStyle)
        {
            if (playerStyle) return PresentationPriority.Mechanic;
            if (kind == EntityKind.Pickup) return PresentationPriority.Decoration;
            if (kind == EntityKind.Area ||
                (kind == EntityKind.Actor && profile.StableId.IndexOf(".boss.", StringComparison.Ordinal) >= 0))
                return PresentationPriority.CriticalDanger;
            return kind == EntityKind.Actor || kind == EntityKind.Projectile ?
                PresentationPriority.Combat : PresentationPriority.Decoration;
        }

        private ProceduralShape FormalShape(VisualProfile profile, bool playerStyle)
        {
            if (playerStyle) return ProceduralShape.Triangle;
            if (kind == EntityKind.Actor && profile.StableId.IndexOf(".boss.", StringComparison.Ordinal) >= 0)
                return ProceduralShape.Hexagon;
            switch (kind)
            {
                case EntityKind.Actor: return ProceduralShape.Circle;
                case EntityKind.Projectile: return ProceduralShape.Diamond;
                case EntityKind.Area: return ProceduralShape.Ring;
                case EntityKind.Pickup: return ProceduralShape.Cross;
                default: return ProceduralShape.Square;
            }
        }

        public bool Release(T view)
        {
            if (view == null || !view.IsBound || !owned.Contains(view)) return false;
            view.Unbind();
            available.Push(view);
            return true;
        }

        public void Dispose()
        {
            for (var index = all.Count - 1; index >= 0; index--)
                if (all[index] != null) UnityObjectLifetime.Destroy(all[index].gameObject);
            all.Clear();
            owned.Clear();
            available.Clear();
        }

        private T Create()
        {
            var instance = new GameObject(typeof(T).Name + "_Pooled").AddComponent<T>();
            instance.transform.SetParent(root, false);
            instance.Configure(fallback.Sprite, ProceduralVisualLibrary.ColorFor(kind), ProceduralVisualLibrary.SizeFor(kind));
            instance.Unbind();
            all.Add(instance);
            owned.Add(instance);
            return instance;
        }
    }
}
