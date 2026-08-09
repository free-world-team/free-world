using System.IO;
using Game.Editor;
using NUnit.Framework;
using UnityEditor;
using UnityEditor.AddressableAssets;
using UnityEngine;

namespace Game.Tests.EditMode
{
    public sealed class QinglanG32BossAudioAssetTests
    {
        private const string Root = "Assets/GameAssets/AI/QinglanDemo/AUDIO-BOSS-001";

        private static readonly Stem[] Stems =
        {
            new Stem("zhezhi-phase-one", "qinglan/audio/boss/zhezhi/phase-one"),
            new Stem("zhezhi-phase-two", "qinglan/audio/boss/zhezhi/phase-two"),
            new Stem("zhezhi-phase-three", "qinglan/audio/boss/zhezhi/phase-three"),
            new Stem("tingfeng-phase-one", "qinglan/audio/boss/tingfeng/phase-one"),
            new Stem("tingfeng-phase-two", "qinglan/audio/boss/tingfeng/phase-two"),
            new Stem("tingfeng-phase-three", "qinglan/audio/boss/tingfeng/phase-three")
        };

        [Test]
        public void TwoBossesHaveThreeStreamingPhaseStemsEach()
        {
            Assert.That(Stems.Length, Is.EqualTo(6));
            for (var index = 0; index < Stems.Length; index++)
            {
                var path = FinalPath(Stems[index].Name);
                var clip = AssetDatabase.LoadAssetAtPath<AudioClip>(path);
                Assert.That(clip, Is.Not.Null, path);
                Assert.That(clip.frequency, Is.EqualTo(48000), path);
                Assert.That(clip.channels, Is.EqualTo(1), path);
                Assert.That(clip.length, Is.InRange(89.99f, 90.01f), path);

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
        public void BossPhaseStemsUseCanonicalAddressesAndAudioReleaseRoute()
        {
            var settings = AddressableAssetSettingsDefaultObject.GetSettings(false);
            var group = settings?.FindGroup(AssetProvenanceValidator.QinglanAudioGroup);
            Assert.That(group, Is.Not.Null);
            for (var index = 0; index < Stems.Length; index++)
            {
                var path = FinalPath(Stems[index].Name);
                var entry = settings.FindAssetEntry(AssetDatabase.AssetPathToGUID(path));
                Assert.That(entry, Is.Not.Null, path);
                Assert.That(entry.parentGroup, Is.SameAs(group), path);
                Assert.That(entry.address, Is.EqualTo(Stems[index].Address), path);
                Assert.That(entry.labels, Does.Contain(AssetProvenanceValidator.QinglanPackLabel), path);
                Assert.That(entry.labels, Does.Contain(AssetProvenanceValidator.ReleaseLabel), path);
                Assert.That(entry.labels, Does.Contain(AssetProvenanceValidator.AudioReleaseLabel), path);
                Assert.That(entry.labels, Does.Not.Contain(PlaceholderAssetGenerator.PlaceholderLabel), path);
                Assert.That(entry.labels, Does.Not.Contain(PlaceholderAssetGenerator.DevelopmentOnlyLabel), path);
            }
        }

        [Test]
        public void BossAudioProvenancePassesAndMastersAreNotAddressable()
        {
            var projectRoot = Directory.GetParent(UnityEngine.Application.dataPath)?.FullName;
            Assert.That(projectRoot, Is.Not.Null.And.Not.Empty);
            var settings = AddressableAssetSettingsDefaultObject.GetSettings(false);
            for (var index = 0; index < Stems.Length; index++)
            {
                var finalPath = FinalPath(Stems[index].Name);
                var issues = AssetProvenanceValidator.ValidateFile(
                    projectRoot,
                    Path.Combine(projectRoot, finalPath.Replace('/', Path.DirectorySeparatorChar)));
                Assert.That(issues, Is.Empty, finalPath);
                var sourcePath = Root + "/source/" + Stems[index].Name + ".wav";
                Assert.That(settings.FindAssetEntry(AssetDatabase.AssetPathToGUID(sourcePath)), Is.Null, sourcePath);
            }

            Assert.That(ProjectGovernanceValidator.ValidateCurrentProject().IsValid, Is.True);
        }

        private static string FinalPath(string name) => Root + "/final/" + name + ".ogg";

        private readonly struct Stem
        {
            public Stem(string name, string address)
            {
                Name = name;
                Address = address;
            }

            public string Name { get; }
            public string Address { get; }
        }
    }
}
