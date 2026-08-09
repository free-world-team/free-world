using System;
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

        private AsyncOperationHandle<FormalVisualCatalog> handle;
        private bool ownsHandle;

        public FormalVisualCatalog Catalog { get; private set; }
        public string LastError { get; private set; } = string.Empty;
        public bool IsLoaded => Catalog != null;

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
                    return true;
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
            ReleaseHandle();
        }

        private void ReleaseHandle()
        {
            if (!ownsHandle) return;
            Addressables.Release(handle);
            ownsHandle = false;
        }
    }
}
