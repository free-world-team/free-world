using System;
using System.Collections.Generic;
using System.IO;
using Game.Editor;
using NUnit.Framework;

namespace Game.Tests.EditMode
{
    public sealed class QinglanG32AudioGovernanceTests
    {
        [Test]
        public void ApprovedAudioReleaseUsesDedicatedGroupAndLabels()
        {
            var root = CreateProjectRoot();
            try
            {
                var batch = WriteApprovedAudioBatch(root);
                var issues = AssetProvenanceValidator.ValidateReleaseInput(
                    root,
                    batch.FinalRelative,
                    AudioLabels(),
                    AssetProvenanceValidator.QinglanAudioGroup);

                Assert.That(issues, Is.Empty, JoinIssues(issues));
            }
            finally
            {
                Directory.Delete(root, true);
            }
        }

        [Test]
        public void AudioReleaseRejectsWrongGroupAndMixedCategoryLabels()
        {
            var root = CreateProjectRoot();
            try
            {
                var batch = WriteApprovedAudioBatch(root);
                var wrongGroup = AssetProvenanceValidator.ValidateReleaseInput(
                    root,
                    batch.FinalRelative,
                    AudioLabels(),
                    AssetProvenanceValidator.QinglanVisualGroup);
                var mixedLabels = AudioLabels();
                mixedLabels.Add(AssetProvenanceValidator.VisualReleaseLabel);
                var mixed = AssetProvenanceValidator.ValidateReleaseInput(
                    root,
                    batch.FinalRelative,
                    mixedLabels,
                    AssetProvenanceValidator.QinglanAudioGroup);

                Assert.That(ContainsIssue(wrongGroup, "M9-RELEASE-AUDIO-ROUTING"), Is.True);
                Assert.That(ContainsIssue(mixed, "M9-RELEASE-CATEGORY"), Is.True);
            }
            finally
            {
                Directory.Delete(root, true);
            }
        }

        [Test]
        public void AudioReleaseRejectsSourceMasterAndMissingReleaseLabel()
        {
            var root = CreateProjectRoot();
            try
            {
                var batch = WriteApprovedAudioBatch(root);
                var sourceIssues = AssetProvenanceValidator.ValidateReleaseInput(
                    root,
                    batch.SourceRelative,
                    AudioLabels(),
                    AssetProvenanceValidator.QinglanAudioGroup);
                var noRelease = AudioLabels();
                noRelease.Remove(AssetProvenanceValidator.ReleaseLabel);
                var labelIssues = AssetProvenanceValidator.ValidateReleaseInput(
                    root,
                    batch.FinalRelative,
                    noRelease,
                    AssetProvenanceValidator.QinglanAudioGroup);

                Assert.That(ContainsIssue(sourceIssues, "M9-RELEASE-NONRUNTIME"), Is.True);
                Assert.That(ContainsIssue(labelIssues, "M9-RELEASE-LABELS"), Is.True);
            }
            finally
            {
                Directory.Delete(root, true);
            }
        }

        private static string CreateProjectRoot()
        {
            var root = Path.Combine(Path.GetTempPath(), "free-world-g32-audio-" + Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(Path.Combine(root, "Assets", "GameAssets", "AI"));
            Directory.CreateDirectory(Path.Combine(root, "Assets", "ThirdParty"));
            File.WriteAllText(Path.Combine(root, "THIRD_PARTY_NOTICES.md"), "# Test notices\n");
            return root;
        }

        private static BatchPaths WriteApprovedAudioBatch(string root)
        {
            var batchRoot = Path.Combine(
                root,
                "Assets",
                "GameAssets",
                "FirstParty",
                "QinglanDemo",
                "AUDIO-UI-001");
            var source = Path.Combine(batchRoot, "source", "confirm.wav");
            var prompt = Path.Combine(batchRoot, "prompt.txt");
            var final = Path.Combine(batchRoot, "final", "confirm.wav");
            Directory.CreateDirectory(Path.GetDirectoryName(source));
            Directory.CreateDirectory(Path.GetDirectoryName(final));
            File.WriteAllBytes(source, new byte[] { 1, 3, 5, 7 });
            File.WriteAllText(prompt, "First-party procedural audio synthesis specification.\n");
            File.WriteAllBytes(final, new byte[] { 2, 4, 6, 8 });
            var sourceRelative = Relative(root, source);
            var promptRelative = Relative(root, prompt);
            var finalRelative = Relative(root, final);
            var sourceHash = ContentPackBuilder.ComputeFileHash(source);
            var promptHash = ContentPackBuilder.ComputeFileHash(prompt);
            var finalHash = ContentPackBuilder.ComputeFileHash(final);
            File.WriteAllText(
                Path.Combine(batchRoot, "provenance.json"),
                "{\n" +
                "  \"schemaVersion\": 2,\n" +
                "  \"assetId\": \"AUDIO-UI-001\",\n" +
                "  \"owner\": \"Qinglan Demo Audio Owner\",\n" +
                "  \"relativePaths\": [\"" + sourceRelative + "\", \"" + promptRelative +
                "\", \"" + finalRelative + "\"],\n" +
                "  \"sourceCategory\": \"first-party-procedural-synthesis\",\n" +
                "  \"tool\": \"repository-generator\",\n" +
                "  \"modelVersion\": \"git-test-sha\",\n" +
                "  \"generatedOrAcquiredAt\": \"2026-08-10T00:00:00+08:00\",\n" +
                "  \"operatorName\": \"Codex\",\n" +
                "  \"promptFile\": \"" + promptRelative + "\",\n" +
                "  \"seed\": \"32009\",\n" +
                "  \"referenceInputs\": [],\n" +
                "  \"referenceRightsConfirmed\": true,\n" +
                "  \"humanEdits\": [\"none\"],\n" +
                "  \"sourceSha256\": {\"confirm.wav\": \"" + sourceHash +
                "\", \"prompt.txt\": \"" + promptHash + "\"},\n" +
                "  \"outputSha256\": {\"confirm.wav\": \"" + finalHash + "\"},\n" +
                "  \"licenseOrTermsUrl\": \"repository://AGENTS.md\",\n" +
                "  \"licenseOrTermsSnapshot\": \"Docs/AssetTerms/audio.md\",\n" +
                "  \"termsReviewedAt\": \"2026-08-10\",\n" +
                "  \"allowedPlatforms\": [\"Windows x64\", \"Steam\"],\n" +
                "  \"allowedUses\": [\"commercial game runtime\"],\n" +
                "  \"commercialUseReviewed\": true,\n" +
                "  \"steamDisclosureCategory\": \"first-party procedural audio\",\n" +
                "  \"technicalReviewer\": \"Codex\",\n" +
                "  \"creativeReviewer\": \"Codex\",\n" +
                "  \"rightsReviewer\": \"Codex\",\n" +
                "  \"reviewedAt\": \"2026-08-10T00:00:00+08:00\",\n" +
                "  \"status\": \"approved-for-release\"\n" +
                "}\n");
            return new BatchPaths(sourceRelative, finalRelative);
        }

        private static HashSet<string> AudioLabels() => new HashSet<string>
        {
            AssetProvenanceValidator.QinglanPackLabel,
            AssetProvenanceValidator.ReleaseLabel,
            AssetProvenanceValidator.AudioReleaseLabel
        };

        private static string Relative(string root, string path)
        {
            var rootUri = new Uri(root.TrimEnd(Path.DirectorySeparatorChar) + Path.DirectorySeparatorChar);
            return Uri.UnescapeDataString(rootUri.MakeRelativeUri(new Uri(path)).ToString()).Replace('\\', '/');
        }

        private static bool ContainsIssue(IReadOnlyList<ValidationIssue> issues, string code)
        {
            for (var index = 0; index < issues.Count; index++)
                if (string.Equals(issues[index].Code, code, StringComparison.Ordinal)) return true;
            return false;
        }

        private static string JoinIssues(IReadOnlyList<ValidationIssue> issues)
        {
            var text = string.Empty;
            for (var index = 0; index < issues.Count; index++)
                text += (index == 0 ? string.Empty : "\n") + issues[index];
            return text;
        }

        private readonly struct BatchPaths
        {
            public BatchPaths(string sourceRelative, string finalRelative)
            {
                SourceRelative = sourceRelative;
                FinalRelative = finalRelative;
            }

            public string SourceRelative { get; }
            public string FinalRelative { get; }
        }
    }
}
