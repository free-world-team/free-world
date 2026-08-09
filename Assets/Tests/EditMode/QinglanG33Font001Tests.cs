using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Security.Cryptography;
using System.Text;
using Game.Editor;
using NUnit.Framework;
using TMPro;
using UnityEditor;
using UnityEditor.AddressableAssets;
using UnityEngine.TextCore;

namespace Game.Tests.EditMode
{
    public sealed class QinglanG33Font001Tests
    {
        [Test]
        public void OfficialSourceFilesMatchPinnedHashesSizesAndLicense()
        {
            AssertSource(
                "Assets/ThirdParty/Fonts/NotoCJKSC/FONT-001/NotoSansCJKsc-Regular.otf",
                16437364,
                "2c76254f6fc379fddfce0a7e84fb5385bb135d3e399294f6eeb6680d0365b74b");
            AssertSource(
                "Assets/ThirdParty/Fonts/NotoCJKSC/FONT-001/NotoSansCJKsc-Bold.otf",
                17002248,
                "b5f0d1a190a7f9b43c310a8850630af12553df32c4c050543f9059732d9b4c0a");
            AssertSource(
                "Assets/ThirdParty/Fonts/NotoCJKSC/FONT-001/LICENSE.txt",
                4301,
                "6a73f9541c2de74158c0e7cf6b0a58ef774f5a780bf191f2d7ec9cc53efe2bf2");
            StringAssert.Contains(
                "SIL Open Font License",
                File.ReadAllText("Assets/ThirdParty/Fonts/NotoCJKSC/FONT-001/LICENSE.txt"));
            StringAssert.Contains(
                "523d033d6cb47f4a80c58a35753646f5c3608a78",
                File.ReadAllText("Assets/ThirdParty/Fonts/NotoCJKSC/FONT-001/source-record.json"));
        }

        [Test]
        public void DerivedAssetsUseDynamicMultiAtlasAndPinnedSourceFonts()
        {
            AssertFont(QinglanG33FontIntegration.SansRegularPath, "Regular");
            AssertFont(QinglanG33FontIntegration.SansBoldPath, "Bold");
        }

        [Test]
        public void FontAssetsUseDedicatedGroupAddressesAndReleaseLabels()
        {
            var settings = AddressableAssetSettingsDefaultObject.GetSettings(false);
            Assert.That(settings, Is.Not.Null);
            var group = settings.FindGroup(AssetProvenanceValidator.ThirdPartyFontGroup);
            Assert.That(group, Is.Not.Null);
            AssertEntry(group.entries, QinglanG33FontIntegration.SansRegularPath,
                QinglanG33FontIntegration.SansRegularAddress);
            AssertEntry(group.entries, QinglanG33FontIntegration.SansBoldPath,
                QinglanG33FontIntegration.SansBoldAddress);
        }

        [Test]
        public void RepositoryTmpSettingsUseFormalSansInsteadOfPackageDefaultFont()
        {
            var settings = AssetDatabase.LoadAssetAtPath<TMP_Settings>(
                QinglanG33FontIntegration.TmpSettingsPath);
            var regular = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(
                QinglanG33FontIntegration.SansRegularPath);
            Assert.That(settings, Is.Not.Null);
            Assert.That(TMP_Settings.defaultFontAsset, Is.SameAs(regular));
            var serialized = new SerializedObject(settings);
            Assert.That(serialized.FindProperty("m_ClearDynamicDataOnBuild").boolValue, Is.False);
        }

        [Test]
        public void LocalizationReleaseGovernanceRejectsWrongGroupAndMixedCategory()
        {
            var labels = new List<string>
            {
                AssetProvenanceValidator.QinglanPackLabel,
                AssetProvenanceValidator.ReleaseLabel,
                AssetProvenanceValidator.LocalizationReleaseLabel
            };
            var root = Directory.GetParent(UnityEngine.Application.dataPath).FullName;
            var wrongGroup = AssetProvenanceValidator.ValidateReleaseInput(
                root,
                QinglanG33FontIntegration.SansRegularPath,
                labels,
                AssetProvenanceValidator.QinglanLocalizationGroup);
            Assert.That(Contains(wrongGroup, "M9-RELEASE-LOCALIZATION-ROUTING"), Is.True);

            labels.Add(AssetProvenanceValidator.AudioReleaseLabel);
            var mixed = AssetProvenanceValidator.ValidateReleaseInput(
                root,
                QinglanG33FontIntegration.SansRegularPath,
                labels,
                AssetProvenanceValidator.ThirdPartyFontGroup);
            Assert.That(Contains(mixed, "M9-RELEASE-CATEGORY"), Is.True);
        }

        private static void AssertFont(string path, string expectedStyle)
        {
            var font = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(path);
            Assert.That(font, Is.Not.Null, path);
            Assert.That(font.atlasPopulationMode, Is.EqualTo(AtlasPopulationMode.Dynamic), path);
            Assert.That(font.isMultiAtlasTexturesEnabled, Is.True, path);
            Assert.That(font.sourceFontFile, Is.Not.Null, path);
            StringAssert.Contains(expectedStyle, font.faceInfo.styleName, path);
            Assert.That(font.atlasWidth, Is.EqualTo(1024), path);
            Assert.That(font.atlasHeight, Is.EqualTo(1024), path);
        }

        private static void AssertEntry(
            IEnumerable<UnityEditor.AddressableAssets.Settings.AddressableAssetEntry> entries,
            string expectedPath,
            string expectedAddress)
        {
            foreach (var entry in entries)
            {
                if (AssetDatabase.GUIDToAssetPath(entry.guid) != expectedPath) continue;
                Assert.That(entry.address, Is.EqualTo(expectedAddress));
                Assert.That(entry.labels, Does.Contain(AssetProvenanceValidator.QinglanPackLabel));
                Assert.That(entry.labels, Does.Contain(AssetProvenanceValidator.ReleaseLabel));
                Assert.That(entry.labels, Does.Contain(AssetProvenanceValidator.LocalizationReleaseLabel));
                return;
            }
            Assert.Fail("Missing Addressables entry for " + expectedPath + ".");
        }

        private static bool Contains(IReadOnlyList<ValidationIssue> issues, string code)
        {
            for (var index = 0; index < issues.Count; index++)
                if (issues[index].Code == code) return true;
            return false;
        }

        private static void AssertSource(string path, long size, string sha256)
        {
            var file = new FileInfo(path);
            Assert.That(file.Exists, Is.True, path);
            Assert.That(file.Length, Is.EqualTo(size), path);
            using (var stream = file.OpenRead())
            using (var sha = SHA256.Create())
            {
                var bytes = sha.ComputeHash(stream);
                var output = new StringBuilder(64);
                for (var index = 0; index < bytes.Length; index++)
                    output.Append(bytes[index].ToString("x2", CultureInfo.InvariantCulture));
                Assert.That(output.ToString(), Is.EqualTo(sha256), path);
            }
        }
    }
}
