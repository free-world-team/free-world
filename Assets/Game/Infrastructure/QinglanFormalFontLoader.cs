using System;
using Game.UI;
using TMPro;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.ResourceManagement.AsyncOperations;
using UnityEngine.TextCore.LowLevel;
using Object = UnityEngine.Object;

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
                var regularSource = regularHandle.WaitForCompletion();
                var boldSource = boldHandle.WaitForCompletion();
                var narrativeSource = narrativeHandle.WaitForCompletion();
                if (regularHandle.Status == AsyncOperationStatus.Succeeded && regularSource != null &&
                    boldHandle.Status == AsyncOperationStatus.Succeeded && boldSource != null &&
                    narrativeHandle.Status == AsyncOperationStatus.Succeeded && narrativeSource != null)
                {
                    Regular = CreateRuntimeCopy(regularSource);
                    Bold = CreateRuntimeCopy(boldSource);
                    Narrative = CreateRuntimeCopy(narrativeSource);
                    return true;
                }
                LastError = "One or more formal TMP font assets failed to load.";
            }
            catch (Exception exception)
            {
                LastError = exception.GetType().Name + ": " + exception.Message;
            }

            Dispose();
            return false;
        }

        public void Dispose()
        {
            DestroyRuntimeCopy(Regular);
            DestroyRuntimeCopy(Bold);
            DestroyRuntimeCopy(Narrative);
            Regular = null;
            Bold = null;
            Narrative = null;
            ReleaseHandles();
        }

        private static TMP_FontAsset CreateRuntimeCopy(TMP_FontAsset source)
        {
            if (source.sourceFontFile == null)
                throw new InvalidOperationException("Formal TMP source font file is unavailable for " + source.name + ".");
            var copy = TMP_FontAsset.CreateFontAsset(
                source.sourceFontFile,
                Mathf.RoundToInt(source.faceInfo.pointSize),
                source.atlasPadding,
                source.atlasRenderMode,
                source.atlasWidth,
                source.atlasHeight,
                AtlasPopulationMode.Dynamic,
                true);
            if (copy == null)
                throw new InvalidOperationException("Failed to create runtime TMP font copy for " + source.name + ".");
            copy.name = source.name + " Runtime";
            copy.hideFlags = HideFlags.DontSave;
            return copy;
        }

        private static void DestroyRuntimeCopy(TMP_FontAsset font)
        {
            if (font == null) return;
            var material = font.material;
            var atlases = font.atlasTextures;
            DestroyRuntimeObject(font);
            DestroyRuntimeObject(material);
            if (atlases == null) return;
            for (var index = 0; index < atlases.Length; index++) DestroyRuntimeObject(atlases[index]);
        }

        private static void DestroyRuntimeObject(Object value)
        {
            if (value == null) return;
            if (UnityEngine.Application.isPlaying) Object.Destroy(value);
            else Object.DestroyImmediate(value);
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
