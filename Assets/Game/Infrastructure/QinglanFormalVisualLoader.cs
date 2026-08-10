using System;
using System.Collections.Generic;
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

        private AsyncOperationHandle<FormalVisualCatalog> handle;
        private bool ownsHandle;
        private readonly List<AsyncOperationHandle<Sprite>> spriteHandles =
            new List<AsyncOperationHandle<Sprite>>(160);
        private readonly List<Sprite> mapTiles = new List<Sprite>(80);
        private readonly List<Sprite> mapProps = new List<Sprite>(80);

        public FormalVisualCatalog Catalog { get; private set; }
        public string LastError { get; private set; } = string.Empty;
        public bool IsLoaded => Catalog != null;
        public IReadOnlyList<Sprite> MapTiles => mapTiles;
        public IReadOnlyList<Sprite> MapProps => mapProps;

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
            if (Catalog != null) return Catalog.TryResolveSprite(stableKey, out sprite);
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

        private void ReleaseHandle()
        {
            if (!ownsHandle) return;
            Addressables.Release(handle);
            ownsHandle = false;
        }
    }
}
