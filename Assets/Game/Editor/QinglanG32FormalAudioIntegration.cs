using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using Game.Presentation;
using UnityEditor;
using UnityEditor.AddressableAssets;
using UnityEditor.AddressableAssets.Build;
using UnityEditor.AddressableAssets.Settings;
using UnityEngine;
using UnityEngine.Audio;

namespace Game.Editor
{
    /// <summary>Builds the G3.2 runtime catalog and mixer from approved audio entries.</summary>
    public static class QinglanG32FormalAudioIntegration
    {
        public const int ExpectedReleaseClipCount = 104;
        public const string CatalogPath =
            "Assets/GameContent/QinglanDemo/Profiles/Audio/G3-2-INTEGRATION/formal-audio-catalog.asset";
        public const string MixerPath =
            "Assets/GameContent/QinglanDemo/Profiles/Audio/G3-2-INTEGRATION/qinglan-demo.mixer";

        public static void Run()
        {
            var exitCode = 0;
            try
            {
                var catalog = BuildCatalog();
                Debug.Log("[Qinglan G3.2 Formal Audio Catalog] PASS: bindings=" +
                          catalog.BindingCount + ", snapshots=" + catalog.SnapshotCount);
            }
            catch (Exception exception)
            {
                Debug.LogException(exception);
                exitCode = 1;
            }
            EditorApplication.Exit(exitCode);
        }

        public static FormalAudioCatalog BuildCatalog()
        {
            var settings = AddressableAssetSettingsDefaultObject.GetSettings(false);
            if (settings == null) throw new InvalidOperationException("Addressables settings are unavailable.");
            var audioGroup = settings.FindGroup(AssetProvenanceValidator.QinglanAudioGroup);
            if (audioGroup == null) throw new InvalidOperationException("Qinglan audio group is unavailable.");

            var directory = Path.GetDirectoryName(CatalogPath);
            if (string.IsNullOrWhiteSpace(directory)) throw new InvalidOperationException("Catalog path is invalid.");
            Directory.CreateDirectory(directory);
            var mixer = EnsureMixer();
            var snapshots = EnsureSnapshots(mixer);
            var groups = mixer.FindMatchingGroups("Master");
            if (groups == null || groups.Length == 0)
                throw new InvalidOperationException("Qinglan audio mixer has no Master group.");

            var catalog = AssetDatabase.LoadAssetAtPath<FormalAudioCatalog>(CatalogPath);
            if (catalog == null)
            {
                catalog = ScriptableObject.CreateInstance<FormalAudioCatalog>();
                AssetDatabase.CreateAsset(catalog, CatalogPath);
            }

            var entries = new List<AddressableAssetEntry>(audioGroup.entries.Count);
            foreach (var entry in audioGroup.entries)
            {
                if (entry == null ||
                    string.Equals(entry.address, Game.Infrastructure.QinglanFormalAudioLoader.CatalogAddress,
                        StringComparison.Ordinal) ||
                    !entry.labels.Contains(AssetProvenanceValidator.AudioReleaseLabel))
                    continue;
                entries.Add(entry);
            }
            entries.Sort((left, right) => string.CompareOrdinal(left.address, right.address));
            if (entries.Count != ExpectedReleaseClipCount)
                throw new InvalidOperationException(
                    "Expected " + ExpectedReleaseClipCount + " approved audio entries but found " + entries.Count + ".");

            var bindings = new List<BindingDraft>(entries.Count);
            for (var index = 0; index < entries.Count; index++)
            {
                var entry = entries[index];
                var path = AssetDatabase.GUIDToAssetPath(entry.guid).Replace('\\', '/');
                var clip = AssetDatabase.LoadAssetAtPath<AudioClip>(path);
                if (clip == null) throw new InvalidOperationException("AudioClip is unavailable: " + path + ".");
                bindings.Add(new BindingDraft(entry.address, entry.address, UsageFor(entry.address), clip));
            }

            WriteCatalog(catalog, groups[0], snapshots, bindings);
            RegisterCatalog(settings, audioGroup);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh(ImportAssetOptions.ForceSynchronousImport);
            return catalog;
        }

        public static void AppendCurrentProjectValidation(
            AddressableAssetSettings settings,
            ValidationReport report)
        {
            if (report == null) throw new ArgumentNullException(nameof(report));
            if (settings == null)
            {
                report.Add("G32-AUDIO-CATALOG", "Addressables settings are unavailable.");
                return;
            }
            var audioGroup = settings.FindGroup(AssetProvenanceValidator.QinglanAudioGroup);
            if (audioGroup == null)
            {
                report.Add("G32-AUDIO-CATALOG", "Qinglan audio group is unavailable.");
                return;
            }

            AddressableAssetEntry catalogEntry = null;
            foreach (var entry in audioGroup.entries)
                if (entry != null && string.Equals(
                        entry.address,
                        Game.Infrastructure.QinglanFormalAudioLoader.CatalogAddress,
                        StringComparison.Ordinal))
                {
                    catalogEntry = entry;
                    break;
                }
            if (catalogEntry == null ||
                !catalogEntry.labels.Contains(AssetProvenanceValidator.QinglanPackLabel))
                report.Add(
                    "G32-AUDIO-CATALOG",
                    "Formal audio catalog must be Addressable in QinglanDemo-Audio with pack.qinglan_demo.");

            var catalog = AssetDatabase.LoadAssetAtPath<FormalAudioCatalog>(CatalogPath);
            if (catalog == null || catalog.SchemaVersion != FormalAudioCatalog.CurrentSchemaVersion)
            {
                report.Add("G32-AUDIO-CATALOG", "Formal audio catalog is missing or has an unsupported schema.");
                return;
            }
            if (catalog.BindingCount != ExpectedReleaseClipCount)
                report.Add("G32-AUDIO-COUNT", "Formal audio catalog must contain exactly 104 clip bindings.");
            if (catalog.SnapshotCount != 4 || catalog.OutputGroup == null)
                report.Add("G32-AUDIO-MIXER", "Formal audio catalog requires Master routing and four snapshots.");

            var releaseCount = 0;
            foreach (var entry in audioGroup.entries)
            {
                if (entry == null || !entry.labels.Contains(AssetProvenanceValidator.AudioReleaseLabel)) continue;
                releaseCount++;
                if (!catalog.ContainsAddress(entry.address))
                    report.Add("G32-AUDIO-ADDRESS", "Formal audio catalog does not map " + entry.address + ".");
            }
            if (releaseCount != ExpectedReleaseClipCount)
                report.Add("G32-AUDIO-COUNT", "Expected 104 approved audio.release entries but found " + releaseCount + ".");

            for (var cue = PresentationAudioCue.Hit; cue <= PresentationAudioCue.UiPauseToggle; cue++)
                if (!catalog.TryResolveCue(cue, out _))
                    report.Add("G32-AUDIO-CUE", "Formal runtime cue is missing: " + cue + ".");
            var bosses = new[] { "qinglan.enemy.boss.zhezhi", "qinglan.enemy.boss.tingfeng" };
            for (var bossIndex = 0; bossIndex < bosses.Length; bossIndex++)
                for (var phase = 0; phase < 3; phase++)
                    if (!catalog.TryResolveBossStem(bosses[bossIndex], phase, out _))
                        report.Add("G32-AUDIO-BOSS", bosses[bossIndex] + " phase " + phase + " is missing.");
        }

        private static AudioMixer EnsureMixer()
        {
            var existing = AssetDatabase.LoadAssetAtPath<AudioMixer>(MixerPath);
            if (existing != null) return existing;
            var controllerType = typeof(EditorApplication).Assembly.GetType(
                "UnityEditor.Audio.AudioMixerController",
                true);
            var create = controllerType.GetMethod(
                "CreateMixerControllerAtPath",
                BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic);
            if (create == null) throw new MissingMethodException(controllerType.FullName, "CreateMixerControllerAtPath");
            var created = create.Invoke(null, new object[] { MixerPath }) as AudioMixer;
            if (created == null) throw new InvalidOperationException("Unity did not create the Qinglan AudioMixer.");
            return created;
        }

        private static AudioMixerSnapshot[] EnsureSnapshots(AudioMixer mixer)
        {
            var names = new[] { "Gameplay", "Paused", "Story", "Boss" };
            var existing = LoadSnapshots();
            if (existing.Count == 1 && Array.IndexOf(names, existing[0].name) < 0)
            {
                existing[0].name = names[0];
                EditorUtility.SetDirty(existing[0]);
                AssetDatabase.SaveAssets();
                existing = LoadSnapshots();
            }

            var controllerType = mixer.GetType();
            MethodInfo create = null;
            MethodInfo clone = null;
            var methods = controllerType.GetMethods(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
            for (var index = 0; index < methods.Length; index++)
            {
                var parameters = methods[index].GetParameters();
                if (methods[index].Name == "CreateSnapshot" && parameters.Length == 1 &&
                    parameters[0].ParameterType == typeof(string))
                {
                    create = methods[index];
                    break;
                }
                if (methods[index].Name == "CloneNewSnapshotFromTarget" && parameters.Length == 1 &&
                    parameters[0].ParameterType == typeof(bool))
                    clone = methods[index];
            }
            for (var nameIndex = 0; nameIndex < names.Length; nameIndex++)
            {
                var found = false;
                for (var index = 0; index < existing.Count; index++)
                    if (string.Equals(existing[index].name, names[nameIndex], StringComparison.Ordinal))
                    {
                        found = true;
                        break;
                    }
                if (found) continue;
                if (create == null && clone == null)
                {
                    var candidates = new List<string>();
                    for (var methodIndex = 0; methodIndex < methods.Length; methodIndex++)
                        if (methods[methodIndex].Name.IndexOf("Snapshot", StringComparison.OrdinalIgnoreCase) >= 0)
                            candidates.Add(methods[methodIndex].ToString());
                    throw new MissingMethodException(
                        controllerType.FullName,
                        "CreateSnapshot; candidates=" + string.Join(" || ", candidates));
                }
                if (create != null)
                    create.Invoke(mixer, new object[] { names[nameIndex] });
                else
                {
                    var priorIds = new HashSet<int>();
                    for (var index = 0; index < existing.Count; index++)
                        priorIds.Add(existing[index].GetInstanceID());
                    clone.Invoke(mixer, new object[] { false });
                    AssetDatabase.SaveAssets();
                    var cloned = LoadSnapshots();
                    AudioMixerSnapshot created = null;
                    for (var index = 0; index < cloned.Count; index++)
                        if (!priorIds.Contains(cloned[index].GetInstanceID()))
                        {
                            created = cloned[index];
                            break;
                        }
                    if (created == null)
                        throw new InvalidOperationException("AudioMixer did not persist a cloned snapshot.");
                    created.name = names[nameIndex];
                    EditorUtility.SetDirty(created);
                }
                AssetDatabase.SaveAssets();
                existing = LoadSnapshots();
            }

            var result = new AudioMixerSnapshot[names.Length];
            for (var nameIndex = 0; nameIndex < names.Length; nameIndex++)
                for (var index = 0; index < existing.Count; index++)
                    if (string.Equals(existing[index].name, names[nameIndex], StringComparison.Ordinal))
                    {
                        result[nameIndex] = existing[index];
                        break;
                    }
            for (var index = 0; index < result.Length; index++)
                if (result[index] == null) throw new InvalidOperationException("Mixer snapshot is missing: " + names[index]);
            return result;
        }

        private static List<AudioMixerSnapshot> LoadSnapshots()
        {
            var values = AssetDatabase.LoadAllAssetsAtPath(MixerPath);
            var result = new List<AudioMixerSnapshot>(4);
            for (var index = 0; index < values.Length; index++)
                if (values[index] is AudioMixerSnapshot snapshot) result.Add(snapshot);
            return result;
        }

        private static FormalAudioUsage UsageFor(string address)
        {
            var tail = address.Substring("qinglan/audio/".Length);
            if (tail.StartsWith("ambience/", StringComparison.Ordinal)) return FormalAudioUsage.Ambience;
            if (tail.StartsWith("music/", StringComparison.Ordinal)) return FormalAudioUsage.Music;
            if (tail.StartsWith("boss/", StringComparison.Ordinal)) return FormalAudioUsage.Boss;
            if (tail.StartsWith("player/", StringComparison.Ordinal)) return FormalAudioUsage.Player;
            if (tail.StartsWith("weapon/", StringComparison.Ordinal)) return FormalAudioUsage.Weapon;
            if (tail.StartsWith("enemy/", StringComparison.Ordinal)) return FormalAudioUsage.Enemy;
            if (tail.StartsWith("affix/", StringComparison.Ordinal)) return FormalAudioUsage.Affix;
            if (tail.StartsWith("map/", StringComparison.Ordinal)) return FormalAudioUsage.Map;
            if (tail.StartsWith("ui/", StringComparison.Ordinal)) return FormalAudioUsage.Ui;
            throw new InvalidOperationException("Unknown formal audio usage: " + address + ".");
        }

        private static void WriteCatalog(
            FormalAudioCatalog catalog,
            AudioMixerGroup outputGroup,
            AudioMixerSnapshot[] snapshots,
            List<BindingDraft> bindings)
        {
            var serialized = new SerializedObject(catalog);
            serialized.FindProperty("schemaVersion").intValue = FormalAudioCatalog.CurrentSchemaVersion;
            serialized.FindProperty("outputGroup").objectReferenceValue = outputGroup;
            serialized.FindProperty("gameplaySnapshot").objectReferenceValue = snapshots[0];
            serialized.FindProperty("pausedSnapshot").objectReferenceValue = snapshots[1];
            serialized.FindProperty("storySnapshot").objectReferenceValue = snapshots[2];
            serialized.FindProperty("bossSnapshot").objectReferenceValue = snapshots[3];
            var array = serialized.FindProperty("bindings");
            array.arraySize = bindings.Count;
            for (var index = 0; index < bindings.Count; index++)
            {
                var element = array.GetArrayElementAtIndex(index);
                element.FindPropertyRelative("stableKey").stringValue = bindings[index].StableKey;
                element.FindPropertyRelative("address").stringValue = bindings[index].Address;
                element.FindPropertyRelative("usage").intValue = (int)bindings[index].Usage;
                element.FindPropertyRelative("clip").objectReferenceValue = bindings[index].Clip;
            }
            serialized.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(catalog);
        }

        private static void RegisterCatalog(
            AddressableAssetSettings settings,
            AddressableAssetGroup audioGroup)
        {
            var guid = AssetDatabase.AssetPathToGUID(CatalogPath);
            if (string.IsNullOrEmpty(guid)) throw new InvalidOperationException("Catalog GUID is unavailable.");
            var entry = settings.CreateOrMoveEntry(guid, audioGroup, false, false);
            entry.address = Game.Infrastructure.QinglanFormalAudioLoader.CatalogAddress;
            entry.SetLabel(AssetProvenanceValidator.QinglanPackLabel, true, false, false);
            entry.SetLabel(AssetProvenanceValidator.ReleaseLabel, false, false, false);
            entry.SetLabel(AssetProvenanceValidator.AudioReleaseLabel, false, false, false);
            entry.SetLabel(PlaceholderAssetGenerator.PlaceholderLabel, false, false, false);
            entry.SetLabel(PlaceholderAssetGenerator.DevelopmentOnlyLabel, false, false, false);
            settings.SetDirty(AddressableAssetSettings.ModificationEvent.BatchModification, entry, true, true);
        }

        private readonly struct BindingDraft
        {
            public BindingDraft(string stableKey, string address, FormalAudioUsage usage, AudioClip clip)
            {
                StableKey = stableKey;
                Address = address;
                Usage = usage;
                Clip = clip;
            }

            public string StableKey { get; }
            public string Address { get; }
            public FormalAudioUsage Usage { get; }
            public AudioClip Clip { get; }
        }
    }

    public static class QinglanG32AddressablesBuildCommand
    {
        public static void Run()
        {
            var exitCode = 0;
            try
            {
                AddressableAssetSettings.BuildPlayerContent(out AddressablesPlayerBuildResult result);
                if (!string.IsNullOrEmpty(result.Error)) throw new InvalidOperationException(result.Error);
                Debug.Log("[Qinglan G3.2 Addressables Build] PASS: output=" + result.OutputPath +
                          ", duration=" + result.Duration + ", locationCount=" + result.LocationCount);
            }
            catch (Exception exception)
            {
                Debug.LogException(exception);
                exitCode = 1;
            }
            finally
            {
                WindowsDevelopmentBuild.RemoveTemporaryAddressablesLinkXml();
            }
            EditorApplication.Exit(exitCode);
        }
    }
}
