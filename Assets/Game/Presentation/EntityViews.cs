using System;
using Game.Core;
using Game.Simulation;
using UnityEngine;

namespace Game.Presentation
{
    /// <summary>Optional presentation-only profile resolved outside simulation truth.</summary>
    [CreateAssetMenu(menuName = "Free World/Presentation/Visual Profile")]
    public sealed class VisualProfile : ScriptableObject
    {
        [SerializeField] private EntityKind entityKind = EntityKind.Actor;
        [SerializeField] private string stableId;
        [SerializeField] private Sprite sprite;
        [SerializeField] private Color color = Color.white;
        [SerializeField] private Vector2 size = Vector2.one;

        public EntityKind EntityKind => entityKind;
        public string StableId => stableId ?? string.Empty;
        public Sprite Sprite => sprite;
        public Color Color => color;
        public Vector2 Size => size;
    }

    /// <summary>Presentation catalog whose miss path always returns a procedural fallback.</summary>
    public sealed class VisualProfileCatalog
    {
        private readonly VisualProfile[] profiles;

        public VisualProfileCatalog(VisualProfile[] source = null)
        {
            profiles = source == null ? Array.Empty<VisualProfile>() : (VisualProfile[])source.Clone();
        }

        public int Count => profiles.Length;

        public bool TryResolve(ContentId id, EntityKind kind, out VisualProfile profile)
        {
            for (var index = 0; index < profiles.Length; index++)
            {
                if (profiles[index] != null &&
                    profiles[index].EntityKind == kind &&
                    id.IsValid &&
                    string.Equals(profiles[index].StableId, id.Value, StringComparison.Ordinal))
                {
                    profile = profiles[index];
                    return true;
                }
            }

            profile = null;
            return false;
        }
    }

    /// <summary>Base binding that consumes snapshots and never owns gameplay truth.</summary>
    public abstract class EntityView : MonoBehaviour
    {
        private SpriteRenderer spriteRenderer;
        private SpriteRenderer outlineRenderer;
        private SpriteRenderer shadowRenderer;
        private SpriteRenderer heldWeaponRenderer;
        private TrailRenderer projectileTrail;
        private SpriteRenderer[] overlayRenderers;
        private int baseSortingOrder;
        private DirectionalSpriteSet animationSet;
        private Vector2 baseScale = Vector2.one;
        private Color baseColor = Color.white;
        private float hitReactionRemaining;
        private float attackReactionRemaining;

        public SpatialEntity Binding { get; private set; }
        public bool IsBound => Binding.IsValid;
        public long LastSnapshotTick { get; private set; }
        public PresentationPriority Priority { get; private set; } = PresentationPriority.Decoration;
        public ProceduralShape Shape { get; private set; } = ProceduralShape.Square;
        public Color DisplayColor => spriteRenderer == null ? Color.clear : spriteRenderer.color;
        public int ActiveOverlayCount { get; private set; }
        internal ContentId ProfileId { get; private set; }
        internal bool UsesPlayerStyle { get; private set; }
        public bool DirectionalAnimationActive => animationSet != null;
        public PresentationFacing CurrentFacing { get; private set; } = PresentationFacing.Down;
        public PresentationPose CurrentPose { get; private set; } = PresentationPose.Idle;
        public bool HitReactionActive => hitReactionRemaining > 0f;
        public bool HeldWeaponVisible => heldWeaponRenderer != null && heldWeaponRenderer.gameObject.activeSelf;
        public bool ProjectileTrailActive => projectileTrail != null && projectileTrail.emitting;
        public long AnimationFrameChangeCount { get; private set; }

        internal void Configure(Sprite sprite, Color color, Vector2 size)
        {
            Configure(
                sprite,
                color,
                size,
                PresentationPriority.Decoration,
                ProceduralShape.Square,
                Color.clear,
                false);
        }

        internal void Configure(
            Sprite sprite,
            Color color,
            Vector2 size,
            PresentationPriority priority,
            ProceduralShape shape,
            Color outlineColor,
            bool showOutline)
        {
            if (spriteRenderer == null) spriteRenderer = gameObject.AddComponent<SpriteRenderer>();
            spriteRenderer.sprite = sprite;
            spriteRenderer.color = color;
            baseColor = color;
            baseScale = size;
            baseSortingOrder = PresentationSpace.PriorityBand(priority);
            spriteRenderer.sortingOrder = baseSortingOrder;
            transform.localScale = new Vector3(size.x, size.y, 1f);
            if (showOutline)
            {
                var outline = EnsureOutline();
                outline.sprite = sprite;
                outline.color = outlineColor;
                outline.sortingOrder = spriteRenderer.sortingOrder - 1;
                outline.transform.localScale = Vector3.one * 1.12f;
                outline.gameObject.SetActive(true);
            }
            else if (outlineRenderer != null) outlineRenderer.gameObject.SetActive(false);
            Priority = priority;
            Shape = shape;
            ClearOverlays();
        }

        internal void Configure(
            in ProceduralPresentationStyle style,
            ProceduralVisualLibrary library)
        {
            if (library == null) throw new ArgumentNullException(nameof(library));
            if (spriteRenderer == null) spriteRenderer = gameObject.AddComponent<SpriteRenderer>();
            spriteRenderer.sprite = library.GetSprite(style.Shape);
            spriteRenderer.color = style.Color;
            baseColor = style.Color;
            baseScale = style.Size;
            baseSortingOrder = PresentationSpace.PriorityBand(style.Priority);
            spriteRenderer.sortingOrder = baseSortingOrder;
            transform.localScale = new Vector3(style.Size.x, style.Size.y, 1f);
            var outline = EnsureOutline();
            outline.sprite = library.GetSprite(style.Shape);
            var outlineColor = style.OutlineColor;
            if (style.Shape == ProceduralShape.Ring && style.Size.x >= 1.5f)
                outlineColor.a = Mathf.Min(outlineColor.a, 0.48f);
            outline.color = outlineColor;
            outline.sortingOrder = spriteRenderer.sortingOrder - 1;
            outline.transform.localScale = Vector3.one * 1.18f;
            outline.gameObject.SetActive(true);
            Priority = style.Priority;
            Shape = style.Shape;
        }

        internal void SetOverlay(
            int index,
            in ProceduralPresentationStyle style,
            ProceduralVisualLibrary library)
        {
            if (index < 0 || index >= 2) throw new ArgumentOutOfRangeException(nameof(index));
            if (library == null) throw new ArgumentNullException(nameof(library));
            EnsureOverlays();
            var renderer = overlayRenderers[index];
            renderer.sprite = library.GetSprite(style.Shape);
            renderer.color = style.Color;
            renderer.sortingOrder = Math.Max(
                spriteRenderer.sortingOrder + 1 + index,
                PresentationSpace.PriorityBand(style.Priority) + index);
            renderer.transform.localScale = Vector3.one * (1.28f + (index * 0.18f));
            renderer.gameObject.SetActive(true);
            if (index + 1 > ActiveOverlayCount) ActiveOverlayCount = index + 1;
        }

        internal void SetOverlay(
            int index,
            Sprite sprite,
            Color color,
            PresentationPriority priority)
        {
            if (index < 0 || index >= 2) throw new ArgumentOutOfRangeException(nameof(index));
            if (sprite == null) throw new ArgumentNullException(nameof(sprite));
            EnsureOverlays();
            var renderer = overlayRenderers[index];
            renderer.sprite = sprite;
            renderer.color = color;
            renderer.sortingOrder = Math.Max(
                spriteRenderer.sortingOrder + 1 + index,
                PresentationSpace.PriorityBand(priority) + index);
            renderer.transform.localScale = Vector3.one * (1.28f + (index * 0.18f));
            renderer.gameObject.SetActive(true);
            if (index + 1 > ActiveOverlayCount) ActiveOverlayCount = index + 1;
        }

        internal void ClearOverlays()
        {
            if (overlayRenderers != null)
                for (var index = 0; index < overlayRenderers.Length; index++)
                    if (overlayRenderers[index] != null) overlayRenderers[index].gameObject.SetActive(false);
            ActiveOverlayCount = 0;
        }

        internal void SetStyleIdentity(ContentId profileId, bool playerStyle)
        {
            ProfileId = profileId;
            UsesPlayerStyle = playerStyle;
        }

        internal void ConfigureAnimation(DirectionalSpriteSet value)
        {
            animationSet = value;
            CurrentFacing = PresentationFacing.Down;
            CurrentPose = PresentationPose.Idle;
        }

        internal void ConfigureHeldWeapon(Sprite sprite)
        {
            if (sprite == null)
            {
                if (heldWeaponRenderer != null) heldWeaponRenderer.gameObject.SetActive(false);
                return;
            }
            if (heldWeaponRenderer == null)
            {
                var child = new GameObject("HeldWeapon_YufengSword");
                child.transform.SetParent(transform, false);
                heldWeaponRenderer = child.AddComponent<SpriteRenderer>();
            }
            heldWeaponRenderer.sprite = sprite;
            heldWeaponRenderer.color = Color.white;
            heldWeaponRenderer.transform.localScale = Vector3.one * 0.32f;
            heldWeaponRenderer.gameObject.SetActive(true);
        }

        internal void ConfigureProjectileTrail(Material material)
        {
            if (material == null || Binding.Kind == EntityKind.Area)
            {
                if (projectileTrail != null) projectileTrail.emitting = false;
                return;
            }
            if (projectileTrail == null)
            {
                projectileTrail = gameObject.AddComponent<TrailRenderer>();
                projectileTrail.time = 0.18f;
                projectileTrail.minVertexDistance = 0.06f;
                projectileTrail.startWidth = 0.18f;
                projectileTrail.endWidth = 0f;
                projectileTrail.alignment = LineAlignment.View;
                projectileTrail.textureMode = LineTextureMode.Stretch;
                projectileTrail.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
                projectileTrail.receiveShadows = false;
                projectileTrail.sortingOrder = 1900;
            }
            projectileTrail.sharedMaterial = material;
            projectileTrail.startColor = new Color(0.42f, 1f, 0.96f, 0.86f);
            projectileTrail.endColor = new Color(0.08f, 0.42f, 0.58f, 0f);
            projectileTrail.Clear();
        }

        public void Bind(SpatialEntity entity)
        {
            if (!entity.IsValid) throw new ArgumentException("A view requires a valid entity.", nameof(entity));
            Binding = entity;
            LastSnapshotTick = -1;
            ConfigureShadow(entity.Kind);
            if (projectileTrail != null)
            {
                projectileTrail.Clear();
                projectileTrail.emitting = entity.Kind == EntityKind.Projectile;
            }
            gameObject.SetActive(true);
        }

        public bool Apply(in RenderEntitySnapshot snapshot, float alpha, long snapshotTick)
        {
            if (!IsBound || snapshot.Entity != Binding) return false;
            var position = snapshot.InterpolatePosition(alpha);
            var facing = snapshot.InterpolateFacing(alpha);
            transform.SetPositionAndRotation(
                PresentationSpace.ToEntity(Binding.Kind, position.X, position.Y),
                Binding.Kind == EntityKind.Area
                    ? PresentationSpace.GroundRotation
                    : Binding.Kind == EntityKind.Projectile
                        ? Quaternion.Euler(0f, 0f, facing * Mathf.Rad2Deg)
                        : Quaternion.identity);
            ApplyPose(snapshot.CurrentStateFlags, facing);
            ApplyMotion(snapshot.CurrentStateFlags);
            UpdateDepthSort(position.Y);
            UpdateHeldWeapon(facing);
            if (shadowRenderer != null)
                shadowRenderer.transform.localPosition = new Vector3(
                    0f,
                    -transform.position.y + PresentationSpace.GroundDecalHeight,
                    0f);
            gameObject.SetActive((snapshot.CurrentStateFlags & SimulationStateFlags.Hidden) == 0);
            LastSnapshotTick = snapshotTick;
            return true;
        }

        public void Unbind()
        {
            Binding = default;
            LastSnapshotTick = -1;
            ProfileId = default;
            UsesPlayerStyle = false;
            animationSet = null;
            hitReactionRemaining = 0f;
            attackReactionRemaining = 0f;
            CurrentPose = PresentationPose.Idle;
            if (spriteRenderer != null)
            {
                spriteRenderer.color = baseColor;
                spriteRenderer.transform.localPosition = Vector3.zero;
            }
            transform.localScale = new Vector3(baseScale.x, baseScale.y, 1f);
            if (heldWeaponRenderer != null) heldWeaponRenderer.gameObject.SetActive(false);
            if (projectileTrail != null)
            {
                projectileTrail.emitting = false;
                projectileTrail.Clear();
            }
            ClearOverlays();
            if (shadowRenderer != null) shadowRenderer.gameObject.SetActive(false);
            gameObject.SetActive(false);
        }

        private SpriteRenderer EnsureOutline()
        {
            if (outlineRenderer != null) return outlineRenderer;
            var child = new GameObject("ProceduralOutline");
            child.transform.SetParent(transform, false);
            outlineRenderer = child.AddComponent<SpriteRenderer>();
            return outlineRenderer;
        }

        private void ConfigureShadow(EntityKind kind)
        {
            if (kind == EntityKind.Area)
            {
                if (shadowRenderer != null) shadowRenderer.gameObject.SetActive(false);
                return;
            }

            if (shadowRenderer == null)
            {
                var child = new GameObject("GroundShadow");
                child.transform.SetParent(transform, false);
                shadowRenderer = child.AddComponent<SpriteRenderer>();
            }
            shadowRenderer.sprite = spriteRenderer == null ? null : spriteRenderer.sprite;
            shadowRenderer.color = new Color(0.015f, 0.02f, 0.025f, kind == EntityKind.Projectile ? 0.12f : 0.28f);
            shadowRenderer.transform.localRotation = PresentationSpace.GroundRotation;
            shadowRenderer.transform.localScale = kind == EntityKind.Projectile
                ? new Vector3(0.62f, 0.12f, 1f)
                : new Vector3(0.78f, 0.22f, 1f);
            shadowRenderer.sortingOrder = -100;
            shadowRenderer.gameObject.SetActive(true);
        }

        internal void PlayHitReaction(float duration = 0.14f)
        {
            hitReactionRemaining = Mathf.Max(hitReactionRemaining, Mathf.Max(0.01f, duration));
        }

        internal void PlayAttackReaction(float duration = 0.16f)
        {
            attackReactionRemaining = Mathf.Max(attackReactionRemaining, Mathf.Max(0.01f, duration));
        }

        internal Sprite ResolvePoseSprite(PresentationPose pose)
        {
            return animationSet == null || spriteRenderer == null
                ? null
                : animationSet.Resolve(CurrentFacing, pose, spriteRenderer.sprite);
        }

        internal void TickVisual(float unscaledDeltaTime)
        {
            if (hitReactionRemaining > 0f)
                hitReactionRemaining = Mathf.Max(0f, hitReactionRemaining - Mathf.Max(0f, unscaledDeltaTime));
            if (attackReactionRemaining > 0f)
                attackReactionRemaining = Mathf.Max(0f, attackReactionRemaining - Mathf.Max(0f, unscaledDeltaTime));
            if (spriteRenderer == null) return;
            if (hitReactionRemaining > 0f)
            {
                var pulse = 0.55f + (Mathf.Sin(Time.unscaledTime * 90f) * 0.45f);
                spriteRenderer.color = Color.Lerp(baseColor, Color.white, pulse);
            }
            else spriteRenderer.color = baseColor;
        }

        private void ApplyPose(SimulationStateFlags flags, float facingRadians)
        {
            if (spriteRenderer == null || Binding.Kind == EntityKind.Area) return;
            CurrentFacing = DirectionalSpriteCatalog.FacingFromRadians(facingRadians);
            CurrentPose = hitReactionRemaining > 0f
                ? PresentationPose.Hit
                : attackReactionRemaining > 0f
                    ? PresentationPose.Attack
                : (flags & SimulationStateFlags.Moving) != 0
                    ? PresentationPose.Move
                    : PresentationPose.Idle;
            if (animationSet != null)
            {
                var nextSprite = animationSet.Resolve(CurrentFacing, CurrentPose, spriteRenderer.sprite);
                if (nextSprite != null && nextSprite != spriteRenderer.sprite) AnimationFrameChangeCount++;
                spriteRenderer.sprite = nextSprite;
                spriteRenderer.flipX = false;
            }
            else if (Binding.Kind != EntityKind.Projectile)
                spriteRenderer.flipX = Mathf.Cos(facingRadians) < 0f;
        }

        private void ApplyMotion(SimulationStateFlags flags)
        {
            if (spriteRenderer == null) return;
            if (Binding.Kind == EntityKind.Area)
            {
                var areaWave = 0.94f + (Mathf.Sin((Time.unscaledTime * 4.2f) +
                    (Binding.Handle.Index * 0.31f)) * 0.06f);
                transform.localScale = new Vector3(baseScale.x * areaWave, baseScale.y * areaWave, 1f);
                return;
            }
            var moving = (flags & SimulationStateFlags.Moving) != 0;
            var boss = ProfileId.IsValid && ProfileId.Value.IndexOf(".boss.", StringComparison.Ordinal) >= 0;
            var phase = (Time.unscaledTime * (moving ? 8.5f : 3.1f)) + (Binding.Handle.Index * 0.37f);
            var wave = Mathf.Sin(phase);
            var bob = wave * (moving ? 0.055f : boss ? 0.036f : 0.022f);
            if (spriteRenderer.transform == transform)
            {
                // The renderer currently lives on the pooled entity root. Preserve the XZ
                // position written from simulation truth and apply bob only on world height.
                var rootPosition = transform.position;
                transform.position = new Vector3(rootPosition.x, rootPosition.y + bob, rootPosition.z);
            }
            else
                spriteRenderer.transform.localPosition = new Vector3(0f, bob, 0f);
            var squash = wave * (moving ? 0.035f : boss ? 0.022f : 0.012f);
            transform.localScale = new Vector3(
                baseScale.x * (1f + squash),
                baseScale.y * (1f - squash),
                1f);
        }

        private void UpdateHeldWeapon(float facingRadians)
        {
            if (heldWeaponRenderer == null || !heldWeaponRenderer.gameObject.activeSelf || spriteRenderer == null) return;
            var swing = attackReactionRemaining > 0f
                ? Mathf.Sin((1f - (attackReactionRemaining / 0.16f)) * Mathf.PI) * 54f
                : Mathf.Sin(Time.unscaledTime * 3.5f) * 4f;
            switch (CurrentFacing)
            {
                case PresentationFacing.Left:
                    heldWeaponRenderer.transform.localPosition = new Vector3(-0.5f, 0.02f, 0f);
                    heldWeaponRenderer.transform.localRotation = Quaternion.Euler(0f, 0f, 195f - swing);
                    heldWeaponRenderer.sortingOrder = spriteRenderer.sortingOrder + 2;
                    break;
                case PresentationFacing.Up:
                    heldWeaponRenderer.transform.localPosition = new Vector3(0.34f, 0.22f, 0f);
                    heldWeaponRenderer.transform.localRotation = Quaternion.Euler(0f, 0f, 78f + swing);
                    heldWeaponRenderer.sortingOrder = spriteRenderer.sortingOrder - 2;
                    break;
                case PresentationFacing.Down:
                    heldWeaponRenderer.transform.localPosition = new Vector3(-0.34f, -0.06f, 0f);
                    heldWeaponRenderer.transform.localRotation = Quaternion.Euler(0f, 0f, -72f - swing);
                    heldWeaponRenderer.sortingOrder = spriteRenderer.sortingOrder + 2;
                    break;
                default:
                    heldWeaponRenderer.transform.localPosition = new Vector3(0.5f, 0.02f, 0f);
                    heldWeaponRenderer.transform.localRotation = Quaternion.Euler(0f, 0f, -15f + swing);
                    heldWeaponRenderer.sortingOrder = spriteRenderer.sortingOrder + 2;
                    break;
            }
        }

        private void UpdateDepthSort(float simulationY)
        {
            var order = Binding.Kind == EntityKind.Area
                ? -20 + PresentationSpace.DepthOffset(simulationY)
                : baseSortingOrder + PresentationSpace.DepthOffset(simulationY);
            if (spriteRenderer != null) spriteRenderer.sortingOrder = order;
            if (outlineRenderer != null) outlineRenderer.sortingOrder = order - 1;
            if (shadowRenderer != null)
                shadowRenderer.sortingOrder = -100 + PresentationSpace.DepthOffset(simulationY);
            if (overlayRenderers == null) return;
            for (var index = 0; index < overlayRenderers.Length; index++)
                if (overlayRenderers[index] != null) overlayRenderers[index].sortingOrder = order + 1 + index;
        }

        private void EnsureOverlays()
        {
            if (overlayRenderers != null) return;
            overlayRenderers = new SpriteRenderer[2];
            for (var index = 0; index < overlayRenderers.Length; index++)
            {
                var child = new GameObject("ProceduralOverlay_" + index);
                child.transform.SetParent(transform, false);
                overlayRenderers[index] = child.AddComponent<SpriteRenderer>();
                child.SetActive(false);
            }
        }
    }

    public sealed class ActorView : EntityView { }
    public sealed class ProjectileView : EntityView { }
    public sealed class AreaView : EntityView { }
    public sealed class PickupView : EntityView { }

    internal sealed class ProceduralVisualLibrary : IDisposable
    {
        private readonly Texture2D[] textures;
        private readonly Sprite[] sprites;
        private readonly Material trailMaterial;

        public ProceduralVisualLibrary()
        {
            var count = Enum.GetValues(typeof(ProceduralShape)).Length;
            textures = new Texture2D[count];
            sprites = new Sprite[count];
            for (var index = 0; index < count; index++) Create((ProceduralShape)index);
            var shader = Shader.Find("Universal Render Pipeline/Particles/Unlit");
            if (shader == null) shader = Shader.Find("Sprites/Default");
            if (shader != null)
            {
                trailMaterial = new Material(shader)
                {
                    name = "G4_0_QinglanSwordTrail",
                    hideFlags = HideFlags.DontSave
                };
            }
        }

        public Sprite Sprite => GetSprite(ProceduralShape.Square);
        public Material TrailMaterial => trailMaterial;

        public Sprite GetSprite(ProceduralShape shape)
        {
            var index = (int)shape;
            return index >= 0 && index < sprites.Length ? sprites[index] : sprites[0];
        }

        public void Dispose()
        {
            for (var index = 0; index < sprites.Length; index++)
            {
                UnityObjectLifetime.Destroy(sprites[index]);
                UnityObjectLifetime.Destroy(textures[index]);
            }
            UnityObjectLifetime.Destroy(trailMaterial);
        }

        public static Color ColorFor(EntityKind kind)
        {
            switch (kind)
            {
                case EntityKind.Actor: return new Color(0.2f, 0.8f, 1f, 1f);
                case EntityKind.Projectile: return new Color(1f, 0.85f, 0.2f, 1f);
                case EntityKind.Area: return new Color(0.7f, 0.25f, 1f, 0.45f);
                case EntityKind.Pickup: return new Color(0.25f, 1f, 0.4f, 1f);
                default: return Color.magenta;
            }
        }

        public static Vector2 SizeFor(EntityKind kind)
        {
            switch (kind)
            {
                case EntityKind.Actor: return Vector2.one;
                case EntityKind.Projectile: return Vector2.one * 0.35f;
                case EntityKind.Area: return Vector2.one * 2f;
                case EntityKind.Pickup: return Vector2.one * 0.45f;
                default: return Vector2.one;
            }
        }

        private void Create(ProceduralShape shape)
        {
            const int size = 32;
            var texture = new Texture2D(size, size, TextureFormat.RGBA32, false)
            {
                name = "G27_Procedural_" + shape,
                filterMode = FilterMode.Bilinear,
                wrapMode = TextureWrapMode.Clamp,
                hideFlags = HideFlags.DontSave
            };
            var pixels = new Color32[size * size];
            for (var y = 0; y < size; y++)
            for (var x = 0; x < size; x++)
            {
                var nx = ((x + 0.5f) / size * 2f) - 1f;
                var ny = ((y + 0.5f) / size * 2f) - 1f;
                pixels[(y * size) + x] = Contains(shape, nx, ny) ?
                    new Color32(255, 255, 255, 255) : new Color32(255, 255, 255, 0);
            }
            texture.SetPixels32(pixels);
            texture.Apply(false, true);
            var sprite = UnityEngine.Sprite.Create(
                texture,
                new Rect(0f, 0f, size, size),
                new Vector2(0.5f, 0.5f),
                size);
            sprite.name = texture.name + "_Sprite";
            sprite.hideFlags = HideFlags.DontSave;
            textures[(int)shape] = texture;
            sprites[(int)shape] = sprite;
        }

        private static bool Contains(ProceduralShape shape, float x, float y)
        {
            var ax = Mathf.Abs(x);
            var ay = Mathf.Abs(y);
            switch (shape)
            {
                case ProceduralShape.Circle: return (x * x) + (y * y) <= 0.72f;
                case ProceduralShape.Diamond: return ax + ay <= 0.92f;
                case ProceduralShape.Triangle: return y >= -0.78f && y <= 0.78f && ax <= (0.82f - (y * 0.52f));
                case ProceduralShape.Ring:
                    var radius = (x * x) + (y * y);
                    return radius >= 0.34f && radius <= 0.76f;
                case ProceduralShape.Cross: return (ax <= 0.2f && ay <= 0.78f) || (ay <= 0.2f && ax <= 0.78f);
                case ProceduralShape.Chevron:
                    return ay <= 0.8f && Mathf.Abs(ax - ((y + 0.8f) * 0.48f)) <= 0.13f;
                case ProceduralShape.Hexagon: return ax <= 0.78f && ay <= 0.68f && (ax + (ay * 0.58f)) <= 0.92f;
                case ProceduralShape.Line: return ax <= 0.88f && ay <= 0.13f;
                default: return ax <= 0.72f && ay <= 0.72f;
            }
        }
    }

    internal static class UnityObjectLifetime
    {
        public static void Destroy(UnityEngine.Object value)
        {
            if (value == null) return;
            if (UnityEngine.Application.isPlaying) UnityEngine.Object.Destroy(value);
            else UnityEngine.Object.DestroyImmediate(value);
        }
    }
}
