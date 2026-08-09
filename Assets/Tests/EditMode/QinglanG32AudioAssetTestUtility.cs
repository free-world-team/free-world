using System.IO;
using Game.Editor;
using NUnit.Framework;
using UnityEditor;
using UnityEditor.AddressableAssets;
using UnityEngine;

namespace Game.Tests.EditMode
{
    internal static class QinglanG32AudioAssetTestUtility
    {
        public static void AssertClipPolicy(
            string root,
            QinglanAudioCueExpectation[] cues,
            bool uiPcm)
        {
            for (var index = 0; index < cues.Length; index++)
            {
                var path = FinalPath(root, cues[index].Name, uiPcm);
                var clip = AssetDatabase.LoadAssetAtPath<AudioClip>(path);
                Assert.That(clip, Is.Not.Null, path);
                Assert.That(clip.frequency, Is.EqualTo(48000), path);
                Assert.That(clip.channels, Is.EqualTo(1), path);
                Assert.That(clip.length, Is.InRange(cues[index].Duration - 0.01f, cues[index].Duration + 0.01f), path);

                var importer = AssetImporter.GetAtPath(path) as AudioImporter;
                Assert.That(importer, Is.Not.Null, path);
                Assert.That(importer.loadInBackground, Is.False, path);
                var settings = importer.defaultSampleSettings;
                Assert.That(settings.loadType,
                    Is.EqualTo(uiPcm ? AudioClipLoadType.DecompressOnLoad : AudioClipLoadType.CompressedInMemory), path);
                Assert.That(settings.compressionFormat,
                    Is.EqualTo(uiPcm ? AudioCompressionFormat.PCM : AudioCompressionFormat.Vorbis), path);
                Assert.That(settings.sampleRateSetting, Is.EqualTo(AudioSampleRateSetting.PreserveSampleRate), path);
                Assert.That(settings.preloadAudioData, Is.True, path);
            }
        }

        public static void AssertAddressableRoute(
            string root,
            QinglanAudioCueExpectation[] cues,
            bool uiPcm)
        {
            var settings = AddressableAssetSettingsDefaultObject.GetSettings(false);
            var group = settings?.FindGroup(AssetProvenanceValidator.QinglanAudioGroup);
            Assert.That(group, Is.Not.Null);
            for (var index = 0; index < cues.Length; index++)
            {
                var path = FinalPath(root, cues[index].Name, uiPcm);
                var entry = settings.FindAssetEntry(AssetDatabase.AssetPathToGUID(path));
                Assert.That(entry, Is.Not.Null, path);
                Assert.That(entry.parentGroup, Is.SameAs(group), path);
                Assert.That(entry.address, Is.EqualTo(cues[index].Address), path);
                Assert.That(entry.labels, Does.Contain(AssetProvenanceValidator.QinglanPackLabel), path);
                Assert.That(entry.labels, Does.Contain(AssetProvenanceValidator.ReleaseLabel), path);
                Assert.That(entry.labels, Does.Contain(AssetProvenanceValidator.AudioReleaseLabel), path);
                Assert.That(entry.labels, Does.Not.Contain(PlaceholderAssetGenerator.PlaceholderLabel), path);
                Assert.That(entry.labels, Does.Not.Contain(PlaceholderAssetGenerator.DevelopmentOnlyLabel), path);
            }
        }

        public static void AssertProvenanceAndSourceIsolation(
            string root,
            QinglanAudioCueExpectation[] cues,
            bool uiPcm)
        {
            var projectRoot = Directory.GetParent(UnityEngine.Application.dataPath)?.FullName;
            Assert.That(projectRoot, Is.Not.Null.And.Not.Empty);
            var settings = AddressableAssetSettingsDefaultObject.GetSettings(false);
            for (var index = 0; index < cues.Length; index++)
            {
                var finalPath = FinalPath(root, cues[index].Name, uiPcm);
                var issues = AssetProvenanceValidator.ValidateFile(
                    projectRoot,
                    Path.Combine(projectRoot, finalPath.Replace('/', Path.DirectorySeparatorChar)));
                Assert.That(issues, Is.Empty, finalPath);
                var sourcePath = root + "/source/" + cues[index].Name + ".wav";
                Assert.That(settings.FindAssetEntry(AssetDatabase.AssetPathToGUID(sourcePath)), Is.Null, sourcePath);
            }

            Assert.That(ProjectGovernanceValidator.ValidateCurrentProject().IsValid, Is.True);
        }

        private static string FinalPath(string root, string name, bool uiPcm) =>
            root + "/final/" + name + (uiPcm ? ".wav" : ".ogg");
    }

    internal readonly struct QinglanAudioCueExpectation
    {
        public QinglanAudioCueExpectation(string name, string address, float duration)
        {
            Name = name;
            Address = address;
            Duration = duration;
        }

        public string Name { get; }
        public string Address { get; }
        public float Duration { get; }
    }
}
