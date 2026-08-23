using System;
using System.Collections.Generic;
using Game.Application;
using Game.Core;
using UnityEngine;
using UnityEngine.Rendering;

namespace Game.Presentation
{
    public readonly struct ProceduralMapObstacle
    {
        public ProceduralMapObstacle(Vector2 minimum, Vector2 maximum)
        {
            Minimum = minimum;
            Maximum = maximum;
        }

        public Vector2 Minimum { get; }
        public Vector2 Maximum { get; }
    }

    public readonly struct ProceduralMapMarker
    {
        public ProceduralMapMarker(ContentId stateId, byte kind, Vector2 position)
            : this(stateId, kind, position, null)
        {
        }

        public ProceduralMapMarker(
            ContentId stateId,
            byte kind,
            Vector2 position,
            ProceduralMapMarkerSpriteSet formalStates)
        {
            StateId = stateId;
            Kind = kind;
            Position = position;
            FormalStates = formalStates;
        }

        public ContentId StateId { get; }
        public byte Kind { get; }
        public Vector2 Position { get; }
        public ProceduralMapMarkerSpriteSet FormalStates { get; }
    }

    /// <summary>Pure DTO built outside Presentation from a runtime map definition.</summary>
    public sealed class ProceduralMapConfiguration
    {
        public ProceduralMapConfiguration(
            Vector2 minimum,
            Vector2 maximum,
            float chunkSize,
            ProceduralMapObstacle[] obstacles,
            Vector2[] zones,
            ProceduralMapMarker[] markers,
            Sprite[] groundSprites = null,
            Sprite[] propSprites = null)
        {
            Minimum = minimum;
            Maximum = maximum;
            ChunkSize = Mathf.Max(1f, chunkSize);
            Obstacles = obstacles == null ? Array.Empty<ProceduralMapObstacle>() :
                (ProceduralMapObstacle[])obstacles.Clone();
            Zones = zones == null ? Array.Empty<Vector2>() : (Vector2[])zones.Clone();
            Markers = markers == null ? Array.Empty<ProceduralMapMarker>() :
                (ProceduralMapMarker[])markers.Clone();
            GroundSprites = groundSprites == null ? Array.Empty<Sprite>() : (Sprite[])groundSprites.Clone();
            PropSprites = propSprites == null ? Array.Empty<Sprite>() : (Sprite[])propSprites.Clone();
        }

        public Vector2 Minimum { get; }
        public Vector2 Maximum { get; }
        public float ChunkSize { get; }
        public IReadOnlyList<ProceduralMapObstacle> Obstacles { get; }
        public IReadOnlyList<Vector2> Zones { get; }
        public IReadOnlyList<ProceduralMapMarker> Markers { get; }
        public IReadOnlyList<Sprite> GroundSprites { get; }
        public IReadOnlyList<Sprite> PropSprites { get; }
    }

    internal sealed class ProceduralMapMarkerView
    {
        public ProceduralMapMarker Definition;
        public SpriteRenderer Renderer;
        public SpriteRenderer Aura;
        public Color BaseColor;
        public Vector3 BaseScale;
        public bool Formal;
    }

    /// <summary>Fixed, programmatic map layer; it owns no gameplay or map state.</summary>
    public sealed class ProceduralMapPresentation : IDisposable
    {
        private readonly GameObject root;
        private readonly List<ProceduralMapMarkerView> markers;
        private readonly ProceduralPresentationCatalog profiles;
        private readonly ProceduralVisualLibrary library;
        private readonly Material raisedSurfaceMaterial;
        private ColorVisionMode colorVision;
        private long lastSnapshotTick = long.MinValue;

        internal ProceduralMapPresentation(
            Transform owner,
            ProceduralMapConfiguration configuration,
            ProceduralPresentationCatalog profileCatalog,
            ProceduralVisualLibrary visualLibrary,
            ColorVisionMode initialColorVision)
        {
            if (owner == null) throw new ArgumentNullException(nameof(owner));
            if (configuration == null) throw new ArgumentNullException(nameof(configuration));
            profiles = profileCatalog ?? throw new ArgumentNullException(nameof(profileCatalog));
            library = visualLibrary ?? throw new ArgumentNullException(nameof(visualLibrary));
            colorVision = initialColorVision;
            root = new GameObject("G2_7_ProceduralMap");
            root.transform.SetParent(owner, false);
            markers = new List<ProceduralMapMarkerView>(configuration.Markers.Count);
            raisedSurfaceMaterial = CreateRaisedSurfaceMaterial();
            BuildGround(configuration);
            BuildRegionTransitions(configuration);
            BuildCentralArenaTransition(configuration);
            BuildBounds(configuration);
            BuildZones(configuration);
            BuildObstacles(configuration);
            BuildProps(configuration);
            BuildMarkers(configuration);
        }

        public int MarkerCount => markers.Count;
        public int GroundTileCount { get; private set; }
        public int FormalGroundTileCount { get; private set; }
        public int FormalPropCount { get; private set; }
        public int RaisedGeometryCount { get; private set; }
        public int GroundShadowCount { get; private set; }
        public int CentralArenaTransitionCount { get; private set; }
        public int RegionTransitionDecalCount { get; private set; }
        public int RegionIdentityClusterCount { get; private set; }
        public int FormalMarkerCount { get; private set; }
        public int FormalMarkerStateSpriteCount { get; private set; }
        public int VisibleFormalMarkerCount { get; private set; }
        public int CompletedFormalMarkerCount { get; private set; }
        public bool UsesXzGroundPlane => true;

        public void Sync(RunUiSnapshot snapshot, ColorVisionMode mode)
        {
            var styleChanged = false;
            if (mode != colorVision)
            {
                colorVision = mode;
                RefreshMarkerStyles();
                styleChanged = true;
            }
            if (snapshot == null) return;
            if (!styleChanged && snapshot.Tick == lastSnapshotTick) return;
            lastSnapshotTick = snapshot.Tick;
            VisibleFormalMarkerCount = 0;
            CompletedFormalMarkerCount = 0;
            for (var markerIndex = 0; markerIndex < markers.Count; markerIndex++)
            {
                var marker = markers[markerIndex];
                var found = false;
                for (var stateIndex = 0; stateIndex < snapshot.MapCount; stateIndex++)
                {
                    var state = snapshot.GetMapAt(stateIndex);
                    if (!string.Equals(state.ContentId, marker.Definition.StateId.Value, StringComparison.Ordinal))
                        continue;
                    found = true;
                    var visible = state.State != 1;
                    marker.Renderer.enabled = visible;
                    if (marker.Aura != null) marker.Aura.enabled = visible;
                    if (!visible) break;

                    var completed = state.Progress >= 0.999f || state.State == 6;
                    if (marker.Formal)
                    {
                        if (marker.Definition.FormalStates.TryGet(
                            ResolveFormalMarkerFrame(marker.Definition.Kind, state.State, completed),
                            out var formalSprite))
                            marker.Renderer.sprite = formalSprite;
                        marker.Renderer.color = colorVision == ColorVisionMode.HighContrast
                            ? Color.white
                            : new Color(0.94f, 0.98f, 0.94f, 1f);
                        marker.Renderer.transform.localScale = marker.BaseScale *
                            (completed ? 0.94f : 1f + (Mathf.Clamp01(state.Progress) * 0.08f));
                        VisibleFormalMarkerCount++;
                        if (completed) CompletedFormalMarkerCount++;
                    }
                    else
                    {
                        var color = completed ? new Color(0.52f, 0.58f, 0.54f, 0.55f) : marker.BaseColor;
                        marker.Renderer.color = color;
                        marker.Renderer.transform.localScale = marker.BaseScale *
                            (completed ? 0.8f : 1f + (Mathf.Clamp01(state.Progress) * 0.25f));
                    }

                    if (marker.Aura != null)
                        marker.Aura.color = MarkerAuraColor(marker.Definition.Kind, completed, colorVision);
                    break;
                }
                if (found) continue;
                marker.Renderer.enabled = false;
                if (marker.Aura != null) marker.Aura.enabled = false;
            }
        }

        public void Dispose()
        {
            UnityObjectLifetime.Destroy(root);
            UnityObjectLifetime.Destroy(raisedSurfaceMaterial);
        }

        private void BuildGround(ProceduralMapConfiguration configuration)
        {
            const int formalRegionCount = 5;
            const float tileSize = 4f;
            var minimum = configuration.Minimum;
            var maximum = configuration.Maximum;
            var columns = Mathf.Max(1, Mathf.CeilToInt((maximum.x - minimum.x) / tileSize));
            var rows = Mathf.Max(1, Mathf.CeilToInt((maximum.y - minimum.y) / tileSize));
            var formal = configuration.GroundSprites.Count > 0;
            for (var row = 0; row < rows; row++)
            {
                for (var column = 0; column < columns; column++)
                {
                    var position = new Vector2(
                        minimum.x + ((column + 0.5f) * tileSize),
                        minimum.y + ((row + 0.5f) * tileSize));
                    var renderer = CreateRenderer("Ground_" + row + "_" + column, -30);
                    if (formal)
                    {
                        var region = ResolveRegion(position, minimum, maximum);
                        var spritesPerRegion = Mathf.Max(1, configuration.GroundSprites.Count / formalRegionCount);
                        var regionStart = Mathf.Min(region * spritesPerRegion,
                            configuration.GroundSprites.Count - 1);
                        var regionLength = region == formalRegionCount - 1
                            ? configuration.GroundSprites.Count - regionStart
                            : Mathf.Min(spritesPerRegion, configuration.GroundSprites.Count - regionStart);
                        // Each formal kit is authored as an 8x2 contiguous atlas. Preserve that
                        // topology so seams reconstruct the source environment instead of looking
                        // like shuffled rectangular puzzle pieces.
                        var variation = ((row % 2) * 8) + (column % 8);
                        variation %= Mathf.Max(1, regionLength);
                        var sprite = configuration.GroundSprites[regionStart + variation];
                        renderer.sprite = sprite;
                        renderer.color = RegionTint(region);
                        var bounds = sprite.bounds.size;
                        renderer.transform.localScale = new Vector3(
                            bounds.x <= 0f ? 1f : tileSize / bounds.x,
                            bounds.y <= 0f ? 1f : tileSize / bounds.y,
                            1f);
                        FormalGroundTileCount++;
                    }
                    else
                    {
                        renderer.sprite = library.GetSprite(ProceduralShape.Square);
                        renderer.color = ((row + column) & 1) == 0
                            ? new Color(0.14f, 0.19f, 0.17f, 1f)
                            : new Color(0.12f, 0.17f, 0.15f, 1f);
                        renderer.transform.localScale = new Vector3(tileSize, tileSize, 1f);
                    }
                    renderer.transform.SetPositionAndRotation(
                        PresentationSpace.ToGround(position.x, position.y),
                        PresentationSpace.GroundRotation);
                    GroundTileCount++;
                }
            }
        }

        private void BuildRegionTransitions(ProceduralMapConfiguration configuration)
        {
            var center = (configuration.Minimum + configuration.Maximum) * 0.5f;
            BuildRegionTransitionBand(
                "West",
                center + new Vector2(-20f, 0f),
                new Vector2(2.6f, 32f),
                false,
                RegionTint(1));
            BuildRegionTransitionBand(
                "East",
                center + new Vector2(20f, 0f),
                new Vector2(2.6f, 32f),
                false,
                RegionTint(2));
            BuildRegionTransitionBand(
                "North",
                center + new Vector2(0f, 16f),
                new Vector2(40f, 2.6f),
                true,
                RegionTint(3));
            BuildRegionTransitionBand(
                "South",
                center + new Vector2(0f, -16f),
                new Vector2(40f, 2.6f),
                true,
                RegionTint(4));
        }

        private void BuildRegionTransitionBand(
            string name,
            Vector2 center,
            Vector2 size,
            bool horizontal,
            Color tint)
        {
            CreateRect(
                "G42C_RegionTransition_" + name,
                center,
                size,
                new Color(tint.r, tint.g, tint.b, 0.075f),
                -28);
            RegionTransitionDecalCount++;
            var length = horizontal ? size.x : size.y;
            for (var index = 0; index < 7; index++)
            {
                var along = (-length * 0.5f) + ((index + 0.5f) * (length / 7f));
                var across = ((index % 3) - 1) * 0.64f;
                var position = center + (horizontal
                    ? new Vector2(along, across)
                    : new Vector2(across, along));
                var renderer = CreateRenderer("G42C_TransitionBloom_" + name + "_" + index, -27);
                renderer.sprite = library.GetSprite(index % 3 == 0
                    ? ProceduralShape.Diamond
                    : ProceduralShape.Circle);
                renderer.color = new Color(tint.r, tint.g, tint.b, 0.045f + ((index % 3) * 0.012f));
                renderer.transform.SetPositionAndRotation(
                    PresentationSpace.ToGround(
                        position.x,
                        position.y,
                        PresentationSpace.GroundDecalHeight * 0.55f),
                    PresentationSpace.GroundRotation);
                var diameter = 1.7f + ((index % 4) * 0.43f);
                renderer.transform.localScale = new Vector3(
                    horizontal ? diameter : diameter * 0.68f,
                    horizontal ? diameter * 0.68f : diameter,
                    1f);
                RegionTransitionDecalCount++;
            }
        }
        private void BuildCentralArenaTransition(ProceduralMapConfiguration configuration)
        {
            var center = (configuration.Minimum + configuration.Maximum) * 0.5f;
            var maximumDiameter = Mathf.Min(
                QinglanPresentationTheme.CentralArenaSampleDiameter,
                Mathf.Min(
                    configuration.Maximum.x - configuration.Minimum.x,
                    configuration.Maximum.y - configuration.Minimum.y));
            if (maximumDiameter < 4f) return;

            // Five translucent, differently sized washes replace a single opaque decal edge
            // with a 2.5 m celadon falloff. The source tiles remain untouched and authoritative.
            for (var layer = 0; layer < 5; layer++)
            {
                var renderer = CreateRenderer("G42_CentralArenaWash_" + layer, -29 + layer);
                renderer.sprite = library.GetSprite(ProceduralShape.Circle);
                renderer.color = QinglanPresentationTheme.WithAlpha(
                    layer < 2 ? QinglanPresentationTheme.Ink800 : QinglanPresentationTheme.Jade200,
                    0.018f + (layer * 0.006f));
                var diameter = maximumDiameter - (layer * 1.25f);
                renderer.transform.SetPositionAndRotation(
                    PresentationSpace.ToGround(
                        center.x,
                        center.y,
                        PresentationSpace.GroundDecalHeight * (0.2f + (layer * 0.04f))),
                    PresentationSpace.GroundRotation);
                renderer.transform.localScale = new Vector3(diameter, diameter, 1f);
                CentralArenaTransitionCount++;
            }

            // Small deterministic moss/ink blooms break the last circular contour so the
            // sample boundary reads as weathered ground, not another perfect gameplay ring.
            for (var bloom = 0; bloom < 9; bloom++)
            {
                var angle = (bloom * 40f + ((bloom & 1) * 13f)) * Mathf.Deg2Rad;
                var radius = (maximumDiameter * 0.5f) - 0.7f + ((bloom % 3) * 0.32f);
                var position = center + new Vector2(Mathf.Cos(angle), Mathf.Sin(angle)) * radius;
                var renderer = CreateRenderer("G42_CentralArenaEdgeBloom_" + bloom, -24);
                renderer.sprite = library.GetSprite(ProceduralShape.Circle);
                renderer.color = QinglanPresentationTheme.WithAlpha(
                    (bloom & 1) == 0 ? QinglanPresentationTheme.Jade500 : QinglanPresentationTheme.Ink800,
                    0.035f + ((bloom % 3) * 0.008f));
                renderer.transform.SetPositionAndRotation(
                    PresentationSpace.ToGround(
                        position.x,
                        position.y,
                        PresentationSpace.GroundDecalHeight * 0.45f),
                    PresentationSpace.GroundRotation);
                var diameter = 2.2f + ((bloom % 4) * 0.52f);
                renderer.transform.localScale = new Vector3(diameter, diameter * 0.72f, 1f);
                CentralArenaTransitionCount++;
            }
        }

        private void BuildProps(ProceduralMapConfiguration configuration)
        {
            if (configuration.PropSprites.Count == 0) return;
            const int formalRegionCount = 5;
            var propsPerRegion = Mathf.Max(1, configuration.PropSprites.Count / formalRegionCount);
            for (var zoneIndex = 0; zoneIndex < configuration.Zones.Count; zoneIndex++)
            {
                var center = configuration.Zones[zoneIndex];
                var region = ResolveRegion(center, configuration.Minimum, configuration.Maximum);
                var regionStart = Mathf.Min(region * propsPerRegion, configuration.PropSprites.Count - 1);
                var regionLength = region == formalRegionCount - 1
                    ? configuration.PropSprites.Count - regionStart
                    : Mathf.Min(propsPerRegion, configuration.PropSprites.Count - regionStart);
                var tint = RegionPropTint(region);
                for (var offsetIndex = 0; offsetIndex < 3; offsetIndex++)
                {
                    var sprite = configuration.PropSprites[
                        regionStart + ((zoneIndex * 5 + offsetIndex * 7) % Mathf.Max(1, regionLength))];
                    if (sprite == null) continue;
                    var renderer = CreateRenderer("FormalProp_" + zoneIndex + "_" + offsetIndex, -9);
                    renderer.sprite = sprite;
                    renderer.color = tint;
                    var angle = (zoneIndex * 71f + offsetIndex * 119f + region * 23f) * Mathf.Deg2Rad;
                    var radius = 6.2f + (offsetIndex * 1.65f);
                    var propX = center.x + Mathf.Cos(angle) * radius;
                    var propY = center.y + Mathf.Sin(angle) * radius * 0.78f;
                    var bounds = sprite.bounds.size;
                    var targetHeight = offsetIndex == 0 ? 5.0f : offsetIndex == 1 ? 3.45f : 2.35f;
                    targetHeight += (region % 3) * 0.18f;
                    var scale = bounds.y <= 0f ? 1f : targetHeight / bounds.y;
                    renderer.transform.position = PresentationSpace.ToGround(propX, propY, targetHeight * 0.48f);
                    renderer.transform.localScale = Vector3.one * scale;
                    renderer.sortingOrder = 200 + PresentationSpace.DepthOffset(propY);
                    CreateGroundShadow(
                        "PropShadow_" + zoneIndex + "_" + offsetIndex,
                        propX,
                        propY,
                        targetHeight * (offsetIndex == 0 ? 0.82f : 0.66f));
                    FormalPropCount++;
                }
            }
        }

        private void BuildBounds(ProceduralMapConfiguration configuration)
        {
            var minimum = configuration.Minimum;
            var maximum = configuration.Maximum;
            var width = maximum.x - minimum.x;
            var height = maximum.y - minimum.y;
            var center = (minimum + maximum) * 0.5f;
            var color = new Color(0.28f, 0.36f, 0.34f, 1f);
            CreateRaisedBlock("Boundary_North", new Vector2(center.x, maximum.y), new Vector2(width, 0.22f), 0.26f, color);
            CreateRaisedBlock("Boundary_South", new Vector2(center.x, minimum.y), new Vector2(width, 0.22f), 0.26f, color);
            CreateRaisedBlock("Boundary_West", new Vector2(minimum.x, center.y), new Vector2(0.22f, height), 0.26f, color);
            CreateRaisedBlock("Boundary_East", new Vector2(maximum.x, center.y), new Vector2(0.22f, height), 0.26f, color);
        }

        private void BuildZones(ProceduralMapConfiguration configuration)
        {
            var size = Mathf.Max(6f, configuration.ChunkSize * 0.72f);
            for (var index = 0; index < configuration.Zones.Count; index++)
            {
                var center = configuration.Zones[index];
                var region = ResolveRegion(center, configuration.Minimum, configuration.Maximum);
                var tint = RegionTint(region);
                for (var layer = 0; layer < 3; layer++)
                {
                    var renderer = CreateRenderer("G42C_RegionIdentity_" + index + "_" + layer, -21 + layer);
                    renderer.sprite = library.GetSprite(layer == 1
                        ? ProceduralShape.Diamond
                        : ProceduralShape.Circle);
                    renderer.color = new Color(
                        tint.r,
                        tint.g,
                        tint.b,
                        0.025f + (layer * 0.018f));
                    renderer.transform.SetPositionAndRotation(
                        PresentationSpace.ToGround(
                            center.x + ((layer - 1) * 0.55f),
                            center.y + (((index + layer) % 2 == 0 ? 1f : -1f) * 0.4f),
                            PresentationSpace.GroundDecalHeight * (0.7f + layer * 0.08f)),
                        PresentationSpace.GroundRotation);
                    var diameter = size - (layer * 1.8f);
                    renderer.transform.localScale = new Vector3(
                        diameter,
                        diameter * (0.72f + ((region & 1) * 0.08f)),
                        1f);
                }
                RegionIdentityClusterCount++;
            }
        }

        private void BuildObstacles(ProceduralMapConfiguration configuration)
        {
            for (var index = 0; index < configuration.Obstacles.Count; index++)
            {
                var item = configuration.Obstacles[index];
                var authoredFootprint = item.Maximum - item.Minimum;
                var footprint = authoredFootprint;
                // Collision walls are authored two metres thick. A literal cube at that
                // footprint occupies an excessive part of a tilted orthographic frame. The
                // mesh is therefore only a curb; formal region props below communicate the
                // full height and authored blocking line.
                if (footprint.x <= 2.1f && footprint.y > 4f) footprint.x = 0.22f;
                if (footprint.y <= 2.1f && footprint.x > 4f) footprint.y = 0.22f;
                CreateRaisedBlock(
                    "Obstacle_" + index,
                    (item.Minimum + item.Maximum) * 0.5f,
                    footprint,
                    0.12f,
                    new Color(0.24f, 0.31f, 0.3f, 1f));
                BuildObstacleProps(
                    configuration,
                    index,
                    (item.Minimum + item.Maximum) * 0.5f,
                    authoredFootprint);
            }
        }

        private void BuildObstacleProps(
            ProceduralMapConfiguration configuration,
            int obstacleIndex,
            Vector2 center,
            Vector2 footprint)
        {
            if (configuration.PropSprites.Count == 0) return;
            const int formalRegionCount = 5;
            var propsPerRegion = Mathf.Max(1, configuration.PropSprites.Count / formalRegionCount);
            var region = ResolveRegion(center, configuration.Minimum, configuration.Maximum);
            var regionStart = Mathf.Min(region * propsPerRegion, configuration.PropSprites.Count - 1);
            var regionLength = region == formalRegionCount - 1
                ? configuration.PropSprites.Count - regionStart
                : Mathf.Min(propsPerRegion, configuration.PropSprites.Count - regionStart);
            var horizontal = footprint.x >= footprint.y;
            var length = Mathf.Max(footprint.x, footprint.y);
            var count = Mathf.Max(1, Mathf.CeilToInt(length / 3.8f));
            for (var itemIndex = 0; itemIndex < count; itemIndex++)
            {
                // Atlas slots 6 and 2 are region-authored fence/stone silhouettes. Alternate
                // occasional posts so long simulation walls read as courtyard architecture.
                var localSpriteIndex = itemIndex % 4 == 0 ? 2 : 6;
                localSpriteIndex %= Mathf.Max(1, regionLength);
                var sprite = configuration.PropSprites[regionStart + localSpriteIndex];
                if (sprite == null) continue;
                var distance = (-length * 0.5f) + ((itemIndex + 0.5f) * (length / count));
                var x = center.x + (horizontal ? distance : 0f);
                var y = center.y + (horizontal ? 0f : distance);
                var targetHeight = itemIndex % 4 == 0 ? 2.45f : 2.05f;
                var bounds = sprite.bounds.size;
                var scale = bounds.y <= 0f ? 1f : targetHeight / bounds.y;
                var renderer = CreateRenderer(
                    "ObstacleProp_" + obstacleIndex + "_" + itemIndex,
                    200 + PresentationSpace.DepthOffset(y));
                renderer.sprite = sprite;
                renderer.color = Color.white;
                renderer.transform.position = PresentationSpace.ToGround(x, y, targetHeight * 0.48f);
                renderer.transform.localScale = Vector3.one * scale;
                CreateGroundShadow(
                    "ObstaclePropShadow_" + obstacleIndex + "_" + itemIndex,
                    x,
                    y,
                    targetHeight * 0.62f);
                FormalPropCount++;
            }
        }

        private void BuildMarkers(ProceduralMapConfiguration configuration)
        {
            for (var index = 0; index < configuration.Markers.Count; index++)
            {
                var definition = configuration.Markers[index];
                profiles.TryResolveEffect(definition.StateId, colorVision, out var style);
                var aura = CreateRenderer("MapMarkerAura_" + definition.Kind + "_" + index, -3);
                aura.transform.SetPositionAndRotation(
                    PresentationSpace.ToGround(
                        definition.Position.x,
                        definition.Position.y,
                        PresentationSpace.GroundDecalHeight * 3f),
                    PresentationSpace.GroundRotation);
                aura.sprite = library.GetSprite(ShapeFor(definition.Kind, style.Shape));
                aura.color = MarkerAuraColor(definition.Kind, false, colorVision);
                aura.transform.localScale = Vector3.one * (definition.Kind == 1 ? 2.8f : 2.15f);
                aura.enabled = false;

                Sprite initialSprite = null;
                var formal = definition.FormalStates != null &&
                             definition.FormalStates.Count >= 3 &&
                             definition.FormalStates.TryGet(0, out initialSprite);
                var renderer = CreateRenderer(
                    (formal ? "FormalMapMarker_" : "MapMarker_") + definition.Kind + "_" + index,
                    formal ? 260 + PresentationSpace.DepthOffset(definition.Position.y) : -2);
                if (formal)
                {
                    renderer.sprite = initialSprite;
                    renderer.color = Color.white;
                    var targetHeight = definition.Kind == 1 ? 4.8f : 3.9f;
                    var spriteHeight = initialSprite.bounds.size.y;
                    var scale = spriteHeight <= 0f ? 1f : targetHeight / spriteHeight;
                    renderer.transform.position = PresentationSpace.ToGround(
                        definition.Position.x,
                        definition.Position.y,
                        targetHeight * 0.48f);
                    renderer.transform.localScale = Vector3.one * scale;
                    CreateGroundShadow(
                        "FormalMapMarkerShadow_" + definition.Kind + "_" + index,
                        definition.Position.x,
                        definition.Position.y,
                        targetHeight * 0.82f);
                    FormalMarkerCount++;
                    FormalMarkerStateSpriteCount += definition.FormalStates.Count;
                }
                else
                {
                    renderer.transform.SetPositionAndRotation(
                        PresentationSpace.ToGround(
                            definition.Position.x,
                            definition.Position.y,
                            PresentationSpace.GroundDecalHeight * 3.2f),
                        PresentationSpace.GroundRotation);
                    renderer.sprite = library.GetSprite(ShapeFor(definition.Kind, style.Shape));
                    renderer.color = style.Color;
                    renderer.transform.localScale = Vector3.one * (definition.Kind == 1 ? 1.45f : 1.05f);
                }
                renderer.enabled = false;
                markers.Add(new ProceduralMapMarkerView
                {
                    Definition = definition,
                    Renderer = renderer,
                    Aura = aura,
                    BaseColor = style.Color,
                    BaseScale = renderer.transform.localScale,
                    Formal = formal
                });
            }
        }

        private void RefreshMarkerStyles()
        {
            for (var index = 0; index < markers.Count; index++)
            {
                var marker = markers[index];
                profiles.TryResolveEffect(marker.Definition.StateId, colorVision, out var style);
                marker.BaseColor = style.Color;
                marker.Renderer.color = style.Color;
            }
        }

        private SpriteRenderer CreateRect(string name, Vector2 position, Vector2 size, Color color, int order)
        {
            var renderer = CreateRenderer(name, order);
            renderer.sprite = library.GetSprite(ProceduralShape.Square);
            renderer.color = color;
            renderer.transform.SetPositionAndRotation(
                PresentationSpace.ToGround(position.x, position.y, PresentationSpace.GroundDecalHeight),
                PresentationSpace.GroundRotation);
            renderer.transform.localScale = new Vector3(Mathf.Max(0.05f, size.x), Mathf.Max(0.05f, size.y), 1f);
            return renderer;
        }

        private void CreateRaisedBlock(
            string name,
            Vector2 position,
            Vector2 footprint,
            float height,
            Color color)
        {
            var value = GameObject.CreatePrimitive(PrimitiveType.Cube);
            value.name = name;
            value.transform.SetParent(root.transform, false);
            value.transform.position = PresentationSpace.ToGround(position.x, position.y, height * 0.5f);
            value.transform.localScale = new Vector3(
                Mathf.Max(0.08f, footprint.x),
                Mathf.Max(0.08f, height),
                Mathf.Max(0.08f, footprint.y));
            var collider = value.GetComponent<Collider>();
            if (collider != null) UnityObjectLifetime.Destroy(collider);
            var renderer = value.GetComponent<MeshRenderer>();
            if (renderer != null)
            {
                renderer.sharedMaterial = raisedSurfaceMaterial;
                renderer.shadowCastingMode = ShadowCastingMode.On;
                renderer.receiveShadows = true;
                var block = new MaterialPropertyBlock();
                block.SetColor("_BaseColor", color);
                block.SetColor("_Color", color);
                renderer.SetPropertyBlock(block);
            }
            RaisedGeometryCount++;
        }

        private void CreateGroundShadow(string name, float x, float y, float diameter)
        {
            var shadow = CreateRenderer(name, -100 + PresentationSpace.DepthOffset(y));
            shadow.sprite = library.GetSprite(ProceduralShape.Circle);
            shadow.color = new Color(0.01f, 0.015f, 0.02f, 0.25f);
            shadow.transform.SetPositionAndRotation(
                PresentationSpace.ToGround(x, y, PresentationSpace.GroundDecalHeight * 2f),
                PresentationSpace.GroundRotation);
            shadow.transform.localScale = new Vector3(diameter, diameter * 0.34f, 1f);
            GroundShadowCount++;
        }

        private static Material CreateRaisedSurfaceMaterial()
        {
            // Unlit sides keep the low courtyard rails legible under every runtime light
            // configuration; depth and contact shadows still come from the raised mesh and
            // the formal vertical props around each region.
            var shader = Shader.Find("Universal Render Pipeline/Unlit");
            if (shader == null) shader = Shader.Find("Universal Render Pipeline/Simple Lit");
            if (shader == null) shader = Shader.Find("Sprites/Default");
            return shader == null ? null : new Material(shader)
            {
                name = "G4_0_RaisedMapSurface",
                hideFlags = HideFlags.DontSave
            };
        }

        private SpriteRenderer CreateRenderer(string name, int order)
        {
            var value = new GameObject(name);
            value.transform.SetParent(root.transform, false);
            var renderer = value.AddComponent<SpriteRenderer>();
            renderer.sortingOrder = order;
            return renderer;
        }

        private static int ResolveFormalMarkerFrame(byte kind, byte state, bool completed)
        {
            if (completed) return 2;
            if (kind == 3) return state >= 2 ? 1 : 0;
            return state >= 4 ? 1 : 0;
        }

        private static Color MarkerAuraColor(byte kind, bool completed, ColorVisionMode mode)
        {
            if (mode == ColorVisionMode.HighContrast) return new Color(1f, 1f, 1f, completed ? 0.42f : 0.30f);
            if (completed) return QinglanPresentationTheme.WithAlpha(QinglanPresentationTheme.Gold400, 0.34f);
            return kind == 3
                ? QinglanPresentationTheme.WithAlpha(QinglanPresentationTheme.Void700, 0.30f)
                : QinglanPresentationTheme.WithAlpha(QinglanPresentationTheme.Jade200, 0.32f);
        }

        private static Color RegionPropTint(int region)
        {
            var tint = RegionTint(region);
            return Color.Lerp(Color.white, tint, 0.24f);
        }
        private static ProceduralShape ShapeFor(byte kind, ProceduralShape fallback)
        {
            if (kind == 1) return ProceduralShape.Ring;
            if (kind == 2) return ProceduralShape.Chevron;
            if (kind == 3) return ProceduralShape.Diamond;
            return fallback;
        }

        private static int ResolveRegion(Vector2 position, Vector2 minimum, Vector2 maximum)
        {
            var center = (minimum + maximum) * 0.5f;
            var localX = position.x - center.x;
            var localY = position.y - center.y;

            // Old Court is five authored courtyards separated by the real simulation walls
            // at x=+/-20 and y=+/-16. Aligning material changes to those rails makes the
            // zones read as intentional spaces rather than arbitrary rectangular patches.
            if (Mathf.Abs(localX) < 20f && Mathf.Abs(localY) < 16f) return 0;
            if (localY >= 16f) return 3;
            if (localY <= -16f) return 4;
            return localX < 0f ? 1 : 2;
        }

        private static Color RegionTint(int region)
        {
            switch (region)
            {
                case 1: return new Color(0.9f, 0.96f, 0.88f, 1f);
                case 2: return new Color(0.88f, 0.93f, 0.98f, 1f);
                case 3: return new Color(0.84f, 0.9f, 0.94f, 1f);
                case 4: return new Color(0.98f, 0.93f, 0.84f, 1f);
                default: return new Color(0.94f, 0.96f, 0.93f, 1f);
            }
        }
    }
}
