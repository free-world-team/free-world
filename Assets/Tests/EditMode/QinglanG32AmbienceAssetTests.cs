using System.IO;
using Game.Editor;
using NUnit.Framework;
using UnityEditor;
using UnityEditor.AddressableAssets;
using UnityEngine;

namespace Game.Tests.EditMode
{
    public sealed class QinglanG32AmbienceAssetTests
    {
        private const string Root = "Assets/GameAssets/AI/QinglanDemo/AUDIO-AMB-001";

        private static readonly string[] Names =
        {
            "courtyard-wind",
            "bamboo-leaves",
            "old-timber-creak",
            "stone-court-resonance",
            "distant-bell",
            "garden-water"
        };

        [Test]
        public void SixApprovedLoopsMeetFormatDurationAndStreamingPolicy()
        {
            Assert.That(Names.Length, Is.EqualTo(6));
            for (var index = 0; index < Names.Length; index++)
            {
                var path = FinalPath(Names[index]);
                var clip = AssetDatabase.LoadAssetAtPath<AudioClip>(path);
                Assert.That(clip, Is.Not.Null, path);
                Assert.That(clip.frequency, Is.EqualTo(48000), path);
                Assert.That(clip.channels, Is.EqualTo(1), path);
                Assert.That(clip.length, Is.InRange(44.99f, 45.01f), path);

                var importer = AssetImporter.GetAtPath(path) as AudioImporter;
                Assert.That(importer, Is.Not.Null, path);
                Assert.That(importer.loadInBackground, Is.True, path);
                var settings = importer.defaultSampleSettings;
                Assert.That(settings.loadType, Is.EqualTo(AudioClipLoadType.Streaming), path);
                Assert.That(settings.compressionFormat, Is.EqualTo(AudioCompressionFormat.Vorbis), path);
                Assert.That(settings.sampleRateSetting, Is.EqualTo(AudioSampleRateSetting.PreserveSampleRate), path);
                Assert.That(settings.preloadAudioData, Is.False, path);
            }
        }

        [Test]
        public void FinalLoopsUseCanonicalAudioAddressesAndReleaseLabels()
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
                Assert.That(entry.address, Is.EqualTo("qinglan/audio/ambience/" + Names[index]), path);
                Assert.That(entry.labels, Does.Contain(AssetProvenanceValidator.QinglanPackLabel), path);
                Assert.That(entry.labels, Does.Contain(AssetProvenanceValidator.ReleaseLabel), path);
                Assert.That(entry.labels, Does.Contain(AssetProvenanceValidator.AudioReleaseLabel), path);
                Assert.That(entry.labels, Does.Not.Contain(PlaceholderAssetGenerator.PlaceholderLabel), path);
                Assert.That(entry.labels, Does.Not.Contain(PlaceholderAssetGenerator.DevelopmentOnlyLabel), path);
            }
        }

        [Test]
        public void BatchProvenancePassesAndSourceFilesAreNotAddressable()
        {
            var projectRoot = Directory.GetParent(UnityEngine.Application.dataPath)?.FullName;
            Assert.That(projectRoot, Is.Not.Null.And.Not.Empty);
            var settings = AddressableAssetSettingsDefaultObject.GetSettings(false);
            for (var index = 0; index < Names.Length; index++)
            {
                var path = FinalPath(Names[index]);
                var issues = AssetProvenanceValidator.ValidateFile(
                    projectRoot,
                    Path.Combine(projectRoot, path.Replace('/', Path.DirectorySeparatorChar)));
                Assert.That(issues, Is.Empty, path);

                var sourcePath = Root + "/source/" + Names[index] + ".wav";
                Assert.That(
                    settings.FindAssetEntry(AssetDatabase.AssetPathToGUID(sourcePath)),
                    Is.Null,
                    sourcePath);
            }

            Assert.That(ProjectGovernanceValidator.ValidateCurrentProject().IsValid, Is.True);
        }

        private static string FinalPath(string name) => Root + "/final/" + name + ".ogg";
    }
}
