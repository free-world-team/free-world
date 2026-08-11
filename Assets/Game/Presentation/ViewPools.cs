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

        internal EntityViewPool(
            Transform poolRoot,
            EntityKind entityKind,
            VisualProfileCatalog catalog,
            ProceduralPresentationCatalog proceduralCatalog,
            AccessibilitySettings accessibilitySettings,
            ProceduralVisualLibrary proceduralFallback,
            DirectionalSpriteCatalog directionalSpriteCatalog,
            int prewarm)
        {
            root = poolRoot ?? throw new ArgumentNullException(nameof(poolRoot));
            kind = entityKind;
            profiles = catalog ?? throw new ArgumentNullException(nameof(catalog));
            proceduralProfiles = proceduralCatalog ?? throw new ArgumentNullException(nameof(proceduralCatalog));
            settings = accessibilitySettings ?? throw new ArgumentNullException(nameof(accessibilitySettings));
            fallback = proceduralFallback ?? throw new ArgumentNullException(nameof(proceduralFallback));
            directionalSprites = directionalSpriteCatalog ?? throw new ArgumentNullException(nameof(directionalSpriteCatalog));
            available = new Stack<T>(Math.Max(1, prewarm));
            all = new List<T>(Math.Max(1, prewarm));
            owned = new HashSet<T>();
            for (var index = 0; index < prewarm; index++) available.Push(Create());
        }

        public int CreatedCount => all.Count;
        public int AvailableCount => available.Count;
        public int ActiveCount => all.Count - available.Count;

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
            view.Bind(entity);
            return view;
        }

        internal void RefreshStyle(T view)
        {
            if (view == null || !view.IsBound) return;
            if (profiles.TryResolve(view.ProfileId, kind, out var profile))
            {
                ConfigureFormal(view, profile, view.UsesPlayerStyle);
                return;
            }
            proceduralProfiles.TryResolve(
                view.ProfileId,
                kind,
                view.UsesPlayerStyle,
                settings.ColorVision,
                out var style);
            view.Configure(style, fallback);
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
            view.Configure(
                profile.Sprite != null ? profile.Sprite : fallback.Sprite,
                FormalTint(profile.Color, playerStyle),
                profile.Size,
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
            if (playerStyle || kind == EntityKind.Pickup) return PresentationPriority.Mechanic;
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
