using System.Globalization;
using System.IO;
using System.Security.Cryptography;
using System.Text;
using Game.Editor;
using NUnit.Framework;
using TMPro;
using UnityEditor;
using UnityEditor.AddressableAssets;

namespace Game.Tests.EditMode
{
    public sealed class QinglanG33Font002Tests
    {
        [Test]
        public void OfficialSerifSourceMatchesPinnedHashSizeLicenseAndCommit()
        {
            AssertSource(
                "Assets/ThirdParty/Fonts/NotoCJKSC/FONT-002/NotoSerifCJKsc-SemiBold.otf",
                24700256,
                "d627b53dbcde61e07de1498d2623a8b287f78585ffbc90cc0618d0caaa2ed6b0");
            AssertSource(
                "Assets/ThirdParty/Fonts/NotoCJKSC/FONT-002/LICENSE.txt",
                4393,
                "88f117575237307bdd86a17ef15e21790fc9a662fe4dfb103ca1ca077f0d9982");
            StringAssert.Contains(
                "SIL OPEN FONT LICENSE Version 1.1",
                File.ReadAllText("Assets/ThirdParty/Fonts/NotoCJKSC/FONT-002/LICENSE.txt"));
            StringAssert.Contains(
                "9b0f1436e455d902de067a2501422e5dc71ad16b",
                File.ReadAllText("Assets/ThirdParty/Fonts/NotoCJKSC/FONT-002/source-record.json"));
        }

        [Test]
        public void SerifDerivedAssetUsesDynamicMultiAtlasAndPinnedSource()
        {
            var font = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(
                QinglanG33FontIntegration.SerifSemiBoldPath);
            Assert.That(font, Is.Not.Null);
            Assert.That(font.atlasPopulationMode, Is.EqualTo(AtlasPopulationMode.Dynamic));
            Assert.That(font.isMultiAtlasTexturesEnabled, Is.True);
            Assert.That(font.sourceFontFile, Is.Not.Null);
            StringAssert.Contains("Noto Serif CJK SC", font.faceInfo.familyName);
            StringAssert.Contains("SemiBold", font.faceInfo.styleName);
            Assert.That(font.atlasWidth, Is.EqualTo(1024));
            Assert.That(font.atlasHeight, Is.EqualTo(1024));
        }

        [Test]
        public void SerifUsesStableAddressAndCompletesThreeFormalFontEntries()
        {
            var settings = AddressableAssetSettingsDefaultObject.GetSettings(false);
            Assert.That(settings, Is.Not.Null);
            var group = settings.FindGroup(AssetProvenanceValidator.ThirdPartyFontGroup);
            Assert.That(group, Is.Not.Null);
            var releaseCount = 0;
            var found = false;
            foreach (var entry in group.entries)
            {
                if (!entry.labels.Contains(AssetProvenanceValidator.LocalizationReleaseLabel)) continue;
                releaseCount++;
                if (AssetDatabase.GUIDToAssetPath(entry.guid) != QinglanG33FontIntegration.SerifSemiBoldPath)
                    continue;
                found = true;
                Assert.That(entry.address, Is.EqualTo(QinglanG33FontIntegration.SerifSemiBoldAddress));
                Assert.That(entry.labels, Does.Contain(AssetProvenanceValidator.QinglanPackLabel));
                Assert.That(entry.labels, Does.Contain(AssetProvenanceValidator.ReleaseLabel));
            }
            Assert.That(found, Is.True);
            Assert.That(releaseCount, Is.EqualTo(3));
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
