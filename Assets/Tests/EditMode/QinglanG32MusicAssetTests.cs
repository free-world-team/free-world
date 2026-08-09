using System.IO;
using Game.Editor;
using NUnit.Framework;
using UnityEditor;
using UnityEditor.AddressableAssets;
using UnityEngine;

namespace Game.Tests.EditMode
{
    public sealed class QinglanG32MusicAssetTests
    {
        private const string Root = "Assets/GameAssets/AI/QinglanDemo/AUDIO-MUSIC-001";

        private static readonly string[] Names =
        {
            "exploration-air",
            "exploration-strings",
            "combat-rhythm",
            "high-pressure-drive"
        };

        [Test]
        public void FourSynchronizedStemsMeetFormatDurationAndStreamingPolicy()
        {
            Assert.That(Names.Length, Is.EqualTo(4));
            for (var index = 0; index < Names.Length; index++)
            {
                var path = FinalPath(Names[index]);
                var clip = AssetDatabase.LoadAssetAtPath<AudioClip>(path);
                Assert.That(clip, Is.Not.Null, path);
                Assert.That(clip.frequency, Is.EqualTo(48000), path);
                Assert.That(clip.channels, Is.EqualTo(1), path);
                Assert.That(clip.length, Is.InRange(119.99f, 120.01f), path);

                var importer = AssetImporter.GetAtPath(path) as AudioImporter;
                Assert.That(importer, Is.Not.Null, path);
                Assert.That(importer.loadInBackground, Is.True, path);
                var sampleSettings = importer.defaultSampleSettings;
                Assert.That(sampleSettings.loadType, Is.EqualTo(AudioClipLoadType.Streaming), path);
                Assert.That(sampleSettings.compressionFormat, Is.EqualTo(AudioCompressionFormat.Vorbis), path);
                Assert.That(sampleSettings.sampleRateSetting, Is.EqualTo(AudioSampleRateSetting.PreserveSampleRate), path);
                Assert.That(sampleSettings.preloadAudioData, Is.False, path);
            }
        }

        [Test]
        public void MusicStemsUseCanonicalAddressesAndAudioReleaseRoute()
        {
            var settings = AddressableAssetSettingsDefaultObject.GetSettings(false);
            var group = settings?.FindGroup(AssetProvenanceValidator.QinglanAudioGroup);
            Assert.That(group, Is.Not.Null);
            for (var index = 0; index < Names.Length; index++)
            {
                var path = FinalPath(Names[index]);
                var entry = settings.FindAssetEntry(AssetDatabase.AssetPathToGUID(path));
                Assert.That(entry, Is.Not.Null, path);
                Assert.That(entry.parentGroup, Is.SameAs(group), path);
                Assert.That(entry.address, Is.EqualTo("qinglan/audio/music/" + Names[index]), path);
                Assert.That(entry.labels, Does.Contain(AssetProvenanceValidator.QinglanPackLabel), path);
                Assert.That(entry.labels, Does.Contain(AssetProvenanceValidator.ReleaseLabel), path);
                Assert.That(entry.labels, Does.Contain(AssetProvenanceValidator.AudioReleaseLabel), path);
                Assert.That(entry.labels, Does.Not.Contain(PlaceholderAssetGenerator.PlaceholderLabel), path);
                Assert.That(entry.labels, Does.Not.Contain(PlaceholderAssetGenerator.DevelopmentOnlyLabel), path);
            }
        }

        [Test]
        public void MusicProvenancePassesAndMastersRemainNonAddressable()
        {
            var projectRoot = Directory.GetParent(UnityEngine.Application.dataPath)?.FullName;
            Assert.That(projectRoot, Is.Not.Null.And.Not.Empty);
            var settings = AddressableAssetSettingsDefaultObject.GetSettings(false);
            for (var index = 0; index < Names.Length; index++)
            {
                var finalPath = FinalPath(Names[index]);
                var issues = AssetProvenanceValidator.ValidateFile(
                    projectRoot,
                    Path.Combine(projectRoot, finalPath.Replace('/', Path.DirectorySeparatorChar)));
                Assert.That(issues, Is.Empty, finalPath);
                var sourcePath = Root + "/source/" + Names[index] + ".wav";
                Assert.That(settings.FindAssetEntry(AssetDatabase.AssetPathToGUID(sourcePath)), Is.Null, sourcePath);
            }

            Assert.That(ProjectGovernanceValidator.ValidateCurrentProject().IsValid, Is.True);
        }

        private static string FinalPath(string name) => Root + "/final/" + name + ".ogg";
    }
}
