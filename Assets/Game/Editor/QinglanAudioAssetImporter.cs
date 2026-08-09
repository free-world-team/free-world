using System;
using System.IO;
using UnityEditor;
using UnityEditor.AddressableAssets;
using UnityEditor.AddressableAssets.Settings;
using UnityEditor.AddressableAssets.Settings.GroupSchemas;
using UnityEngine;

namespace Game.Editor
{
    public enum QinglanAudioImportKind : byte
    {
        AmbienceLoop = 0,
        MusicStream = 1,
        EffectClip = 2,
        UiPcm = 3
    }

    /// <summary>Imports an approved Qinglan audio file and registers its exact release route.</summary>
    public static class QinglanAudioAssetImporter
    {
        public static void ImportApprovedClip(
            string assetPath,
            string address,
            QinglanAudioImportKind kind)
        {
            assetPath = (assetPath ?? string.Empty).Replace('\\', '/');
            if (!assetPath.Contains("/final/", StringComparison.Ordinal))
                throw new ArgumentException("Only an explicit final audio path can be imported.", nameof(assetPath));
            if (string.IsNullOrWhiteSpace(address) ||
                !address.StartsWith("qinglan/audio/", StringComparison.Ordinal))
                throw new ArgumentException("A canonical qinglan/audio/ address is required.", nameof(address));
            if (!Enum.IsDefined(typeof(QinglanAudioImportKind), kind))
                throw new ArgumentOutOfRangeException(nameof(kind));

            AssetDatabase.ImportAsset(
                assetPath,
                ImportAssetOptions.ForceSynchronousImport | ImportAssetOptions.ForceUpdate);
            var importer = AssetImporter.GetAtPath(assetPath) as AudioImporter;
            if (importer == null)
                throw new InvalidOperationException("AudioImporter is unavailable for " + assetPath + ".");

            importer.forceToMono = false;
            importer.ambisonic = false;
            importer.loadInBackground = kind == QinglanAudioImportKind.AmbienceLoop ||
                                        kind == QinglanAudioImportKind.MusicStream;
            var settings = importer.defaultSampleSettings;
            settings.sampleRateSetting = AudioSampleRateSetting.PreserveSampleRate;
            settings.preloadAudioData = !importer.loadInBackground;
            switch (kind)
            {
                case QinglanAudioImportKind.AmbienceLoop:
                case QinglanAudioImportKind.MusicStream:
                    settings.loadType = AudioClipLoadType.Streaming;
                    settings.compressionFormat = AudioCompressionFormat.Vorbis;
                    settings.quality = 0.7f;
                    break;
                case QinglanAudioImportKind.UiPcm:
                    settings.loadType = AudioClipLoadType.DecompressOnLoad;
                    settings.compressionFormat = AudioCompressionFormat.PCM;
                    settings.quality = 1f;
                    break;
                default:
                    settings.loadType = AudioClipLoadType.CompressedInMemory;
                    settings.compressionFormat = AudioCompressionFormat.Vorbis;
                    settings.quality = 0.72f;
                    break;
            }
            importer.defaultSampleSettings = settings;
            importer.SaveAndReimport();

            var clip = AssetDatabase.LoadAssetAtPath<AudioClip>(assetPath);
            if (clip == null || clip.frequency != 48000 || clip.length <= 0f)
                throw new InvalidOperationException(assetPath + " must import as a non-empty 48 kHz AudioClip.");
            RegisterApprovedFile(assetPath, address);
        }

        private static void RegisterApprovedFile(string assetPath, string address)
        {
            var projectRoot = Directory.GetParent(UnityEngine.Application.dataPath)?.FullName;
            if (string.IsNullOrWhiteSpace(projectRoot))
                throw new InvalidOperationException("Unable to resolve project root.");
            var labels = new[]
            {
                AssetProvenanceValidator.QinglanPackLabel,
                AssetProvenanceValidator.ReleaseLabel,
                AssetProvenanceValidator.AudioReleaseLabel
            };
            var issues = AssetProvenanceValidator.ValidateReleaseInput(
                projectRoot,
                assetPath,
                labels,
                AssetProvenanceValidator.QinglanAudioGroup);
            if (issues.Count > 0) throw new InvalidOperationException(issues[0].ToString());

            var settings = AddressableAssetSettingsDefaultObject.GetSettings(true);
            if (settings == null) throw new InvalidOperationException("Addressables settings are unavailable.");
            settings.AddLabel(AssetProvenanceValidator.QinglanPackLabel, false);
            settings.AddLabel(AssetProvenanceValidator.ReleaseLabel, false);
            settings.AddLabel(AssetProvenanceValidator.AudioReleaseLabel, false);
            var group = settings.FindGroup(AssetProvenanceValidator.QinglanAudioGroup);
            if (group == null)
            {
                group = settings.CreateGroup(
                    AssetProvenanceValidator.QinglanAudioGroup,
                    false,
                    false,
                    true,
                    null,
                    typeof(ContentUpdateGroupSchema),
                    typeof(BundledAssetGroupSchema));
                var bundleSchema = group.GetSchema<BundledAssetGroupSchema>();
                bundleSchema.BuildPath.SetVariableByName(settings, AddressableAssetSettings.kLocalBuildPath);
                bundleSchema.LoadPath.SetVariableByName(settings, AddressableAssetSettings.kLocalLoadPath);
                bundleSchema.BundleMode = BundledAssetGroupSchema.BundlePackingMode.PackTogether;
            }

            var guid = AssetDatabase.AssetPathToGUID(assetPath);
            if (string.IsNullOrEmpty(guid)) throw new InvalidOperationException("Asset GUID is unavailable.");
            var entry = settings.CreateOrMoveEntry(guid, group, false, false);
            entry.address = address;
            entry.SetLabel(PlaceholderAssetGenerator.PlaceholderLabel, false, false, false);
            entry.SetLabel(PlaceholderAssetGenerator.DevelopmentOnlyLabel, false, false, false);
            entry.SetLabel(AssetProvenanceValidator.QinglanPackLabel, true, false, false);
            entry.SetLabel(AssetProvenanceValidator.ReleaseLabel, true, false, false);
            entry.SetLabel(AssetProvenanceValidator.AudioReleaseLabel, true, false, false);
            settings.SetDirty(AddressableAssetSettings.ModificationEvent.BatchModification, entry, true, true);
            AssetDatabase.SaveAssets();
        }
    }

    public static class QinglanAudioAssetImportCommand
    {
        public static void Run()
        {
            var exitCode = 0;
            try
            {
                QinglanAudioAssetImporter.ImportApprovedClip(
                    Required("QINGLAN_AUDIO_ASSET_PATH"),
                    Required("QINGLAN_AUDIO_ADDRESS"),
                    ParseKind());
                Debug.Log("[Qinglan Audio Import] PASS");
            }
            catch (Exception exception)
            {
                Debug.LogException(exception);
                exitCode = 1;
            }
            EditorApplication.Exit(exitCode);
        }

        private static string Required(string name)
        {
            var value = Environment.GetEnvironmentVariable(name);
            if (string.IsNullOrWhiteSpace(value))
                throw new InvalidOperationException(name + " is required.");
            return value;
        }

        private static QinglanAudioImportKind ParseKind()
        {
            var text = Required("QINGLAN_AUDIO_IMPORT_KIND");
            if (!Enum.TryParse(text, true, out QinglanAudioImportKind kind) ||
                !Enum.IsDefined(typeof(QinglanAudioImportKind), kind))
                throw new InvalidOperationException("QINGLAN_AUDIO_IMPORT_KIND is invalid: " + text + ".");
            return kind;
        }
    }

    /// <summary>Imports one manifest batch in a single headless Unity process.</summary>
    public static class QinglanAudioBatchImportCommand
    {
        public static void Run()
        {
            var exitCode = 0;
            try
            {
                var records = Required("QINGLAN_AUDIO_BATCH_ITEMS")
                    .Split(new[] { ';' }, StringSplitOptions.RemoveEmptyEntries);
                if (records.Length == 0)
                    throw new InvalidOperationException("QINGLAN_AUDIO_BATCH_ITEMS contains no records.");

                for (var index = 0; index < records.Length; index++)
                {
                    var fields = records[index].Split('|');
                    if (fields.Length != 3)
                        throw new InvalidOperationException(
                            "Audio batch record " + index + " must be assetPath|address|kind.");
                    if (!Enum.TryParse(fields[2], true, out QinglanAudioImportKind kind) ||
                        !Enum.IsDefined(typeof(QinglanAudioImportKind), kind))
                        throw new InvalidOperationException(
                            "Audio batch record " + index + " has invalid kind: " + fields[2] + ".");
                    QinglanAudioAssetImporter.ImportApprovedClip(fields[0], fields[1], kind);
                }

                Debug.Log("[Qinglan Audio Batch Import] PASS clips=" + records.Length);
            }
            catch (Exception exception)
            {
                Debug.LogException(exception);
                exitCode = 1;
            }
            EditorApplication.Exit(exitCode);
        }

        private static string Required(string name)
        {
            var value = Environment.GetEnvironmentVariable(name);
            if (string.IsNullOrWhiteSpace(value))
                throw new InvalidOperationException(name + " is required.");
            return value;
        }
    }
}
