using System;
using Game.UI;
using TMPro;
using UnityEngine.AddressableAssets;
using UnityEngine.ResourceManagement.AsyncOperations;

namespace Game.Infrastructure
{
    /// <summary>Startup Addressables owner for the three governed Qinglan TMP assets.</summary>
    public sealed class QinglanFormalFontLoader : IDisposable, IQinglanUiFontCatalog
    {
        public const string RegularAddress = "qinglan/font/noto-sans-cjk-sc/regular";
        public const string BoldAddress = "qinglan/font/noto-sans-cjk-sc/bold";
        public const string NarrativeAddress = "qinglan/font/noto-serif-cjk-sc/semibold";

        private AsyncOperationHandle<TMP_FontAsset> regularHandle;
        private AsyncOperationHandle<TMP_FontAsset> boldHandle;
        private AsyncOperationHandle<TMP_FontAsset> narrativeHandle;
        private bool ownsHandles;

        public TMP_FontAsset Regular { get; private set; }
        public TMP_FontAsset Bold { get; private set; }
        public TMP_FontAsset Narrative { get; private set; }
        public string LastError { get; private set; } = string.Empty;
        public bool IsLoaded => Regular != null && Bold != null && Narrative != null;

        public bool LoadForStartup()
        {
            if (IsLoaded) return true;
            try
            {
                regularHandle = Addressables.LoadAssetAsync<TMP_FontAsset>(RegularAddress);
                boldHandle = Addressables.LoadAssetAsync<TMP_FontAsset>(BoldAddress);
                narrativeHandle = Addressables.LoadAssetAsync<TMP_FontAsset>(NarrativeAddress);
                ownsHandles = true;
                Regular = regularHandle.WaitForCompletion();
                Bold = boldHandle.WaitForCompletion();
                Narrative = narrativeHandle.WaitForCompletion();
                if (regularHandle.Status == AsyncOperationStatus.Succeeded &&
                    boldHandle.Status == AsyncOperationStatus.Succeeded &&
                    narrativeHandle.Status == AsyncOperationStatus.Succeeded && IsLoaded)
                    return true;
                LastError = "One or more formal TMP font assets failed to load.";
            }
            catch (Exception exception)
            {
                LastError = exception.GetType().Name + ": " + exception.Message;
            }

            ReleaseHandles();
            Regular = null;
            Bold = null;
            Narrative = null;
            return false;
        }

        public void Dispose()
        {
            Regular = null;
            Bold = null;
            Narrative = null;
            ReleaseHandles();
        }

        private void ReleaseHandles()
        {
            if (!ownsHandles) return;
            Addressables.Release(regularHandle);
            Addressables.Release(boldHandle);
            Addressables.Release(narrativeHandle);
            ownsHandles = false;
        }
    }
}
