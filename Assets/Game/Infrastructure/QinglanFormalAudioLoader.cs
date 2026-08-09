using System;
using Game.Presentation;
using UnityEngine.AddressableAssets;
using UnityEngine.ResourceManagement.AsyncOperations;

namespace Game.Infrastructure
{
    /// <summary>Startup Addressables owner for the complete Qinglan formal audio catalog.</summary>
    public sealed class QinglanFormalAudioLoader : IDisposable
    {
        public const string CatalogAddress = "qinglan/runtime/formal-audio-catalog";

        private AsyncOperationHandle<FormalAudioCatalog> handle;
        private bool ownsHandle;

        public FormalAudioCatalog Catalog { get; private set; }
        public string LastError { get; private set; } = string.Empty;
        public bool IsLoaded => Catalog != null;

        public bool LoadForStartup()
        {
            if (Catalog != null) return true;
            try
            {
                handle = Addressables.LoadAssetAsync<FormalAudioCatalog>(CatalogAddress);
                ownsHandle = true;
                Catalog = handle.WaitForCompletion();
                if (handle.Status == AsyncOperationStatus.Succeeded && Catalog != null &&
                    Catalog.SchemaVersion == FormalAudioCatalog.CurrentSchemaVersion)
                    return true;
                LastError = "Formal audio catalog failed or has an unsupported schema.";
            }
            catch (Exception exception)
            {
                LastError = exception.GetType().Name + ": " + exception.Message;
            }

            ReleaseHandle();
            Catalog = null;
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
