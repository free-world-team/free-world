using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Security.Cryptography;
using System.Text;
using System.Text.RegularExpressions;
using TMPro;
using UnityEditor;
using UnityEditor.Localization;
using UnityEngine;
using UnityEngine.Localization;
using UnityEngine.Localization.Pseudo;
using UnityEngine.Localization.Tables;

namespace Game.Editor
{
    /// <summary>Closes G3.3 by preloading tables and prewarming every governed runtime glyph.</summary>
    public static class QinglanG33FinalIntegration
    {
        public const string CommonRuntimeGlyphs =
            " 0123456789ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz" +
            "♥◇▲剑◆◎△▶×时%/+-.，。！？：；、“”‘’（）【】《》…—";

        public static void Run()
        {
            var exitCode = 0;
            try
            {
                var result = Configure();
                Debug.Log(
                    "[Qinglan G3.3 Final Integration] PASS: regular=" + result.RegularCharacterCount +
                    ", bold=" + result.BoldCharacterCount +
                    ", narrative=" + result.NarrativeCharacterCount +
                    ", atlases=" + result.AtlasTextureCount + ".");
            }
            catch (Exception exception)
            {
                Debug.LogException(exception);
                exitCode = 1;
            }
            EditorApplication.Exit(exitCode);
        }

        public static void RunFinalizeProvenance()
        {
            var exitCode = 0;
            try
            {
                FinalizeProvenance();
                Debug.Log("[Qinglan G3.3 Provenance Finalizer] PASS");
            }
            catch (Exception exception)
            {
                Debug.LogException(exception);
                exitCode = 1;
            }
            EditorApplication.Exit(exitCode);
        }

        public static G33GlyphIntegrationResult Configure()
        {
            var english = FindLocale("en");
            var chinese = FindLocale("zh-Hans");
            var pseudoLocales = LocalizationEditorSettings.GetPseudoLocales();
            if (english == null || chinese == null || pseudoLocales.Count == 0)
                throw new InvalidOperationException("G3.3 requires en, zh-Hans, and a generated Pseudo locale.");
            var pseudo = pseudoLocales[0];
            ConfigureFontSafePseudo(pseudo);

            var regularCharacters = NewCharacterSet();
            var boldCharacters = NewCharacterSet();
            var narrativeCharacters = NewCharacterSet();
            AddCharacters(regularCharacters, CommonRuntimeGlyphs);
            AddCharacters(boldCharacters, CommonRuntimeGlyphs);
            AddCharacters(narrativeCharacters, CommonRuntimeGlyphs);

            var ui = RequireCollection("UI");
            var content = RequireCollection("QinglanContent");
            var narrative = RequireCollection("QinglanNarrative");
            AppendCollection(ui, english, chinese, pseudo, regularCharacters);
            AppendCollection(content, english, chinese, pseudo, regularCharacters);
            AppendCollection(narrative, english, chinese, pseudo, regularCharacters);
            AppendCollection(ui, english, chinese, pseudo, boldCharacters);
            AppendCollection(narrative, english, chinese, pseudo, narrativeCharacters);

            SetPreload(ui, english, chinese);
            SetPreload(content, english, chinese);
            SetPreload(narrative, english, chinese);

            var regular = RequireFont(QinglanG33FontIntegration.SansRegularPath);
            var bold = RequireFont(QinglanG33FontIntegration.SansBoldPath);
            var serif = RequireFont(QinglanG33FontIntegration.SerifSemiBoldPath);
            Prewarm(regular, regularCharacters, "Noto Sans CJK SC Regular");
            Prewarm(bold, boldCharacters, "Noto Sans CJK SC Bold");
            Prewarm(serif, narrativeCharacters, "Noto Serif CJK SC SemiBold");
            EditorUtility.SetDirty(regular);
            EditorUtility.SetDirty(bold);
            EditorUtility.SetDirty(serif);
            MarkAtlasesDirty(regular);
            MarkAtlasesDirty(bold);
            MarkAtlasesDirty(serif);
            AssetDatabase.SaveAssets();

            return new G33GlyphIntegrationResult(
                regularCharacters.Count,
                boldCharacters.Count,
                narrativeCharacters.Count,
                regular.atlasTextures.Length + bold.atlasTextures.Length + serif.atlasTextures.Length);
        }

        public static void FinalizeProvenance()
        {
            var english = FindLocale("en");
            var chinese = FindLocale("zh-Hans");
            if (english == null || chinese == null)
                throw new InvalidOperationException("G3.3 provenance finalization requires en and zh-Hans.");
            var ui = RequireCollection("UI");
            var content = RequireCollection("QinglanContent");
            var narrative = RequireCollection("QinglanNarrative");
            UpdateProvenanceOutputHashes(
                QinglanG33FontIntegration.Font001Root + "/provenance.json",
                QinglanG33FontIntegration.SansRegularPath,
                QinglanG33FontIntegration.SansBoldPath);
            UpdateProvenanceOutputHashes(
                QinglanG33FontIntegration.Font002Root + "/provenance.json",
                QinglanG33FontIntegration.SerifSemiBoldPath);
            UpdateCollectionProvenance(QinglanG33LocalizationIntegration.UiBatchRoot, ui, english, chinese);
            UpdateCollectionProvenance(QinglanG33LocalizationIntegration.ContentBatchRoot, content, english, chinese);
            UpdateCollectionProvenance(QinglanG33LocalizationIntegration.NarrativeBatchRoot, narrative, english, chinese);
            AssetDatabase.Refresh(ImportAssetOptions.ForceSynchronousImport);
        }

        public static G33GlyphSets CollectCurrentGlyphSets()
        {
            var english = FindLocale("en");
            var chinese = FindLocale("zh-Hans");
            var pseudoLocales = LocalizationEditorSettings.GetPseudoLocales();
            if (english == null || chinese == null || pseudoLocales.Count == 0)
                throw new InvalidOperationException("The required G3.3 locales are unavailable.");
            var regular = NewCharacterSet();
            var bold = NewCharacterSet();
            var narrative = NewCharacterSet();
            AddCharacters(regular, CommonRuntimeGlyphs);
            AddCharacters(bold, CommonRuntimeGlyphs);
            AddCharacters(narrative, CommonRuntimeGlyphs);
            var ui = RequireCollection("UI");
            var content = RequireCollection("QinglanContent");
            var narrativeCollection = RequireCollection("QinglanNarrative");
            AppendCollection(ui, english, chinese, pseudoLocales[0], regular);
            AppendCollection(content, english, chinese, pseudoLocales[0], regular);
            AppendCollection(narrativeCollection, english, chinese, pseudoLocales[0], regular);
            AppendCollection(ui, english, chinese, pseudoLocales[0], bold);
            AppendCollection(narrativeCollection, english, chinese, pseudoLocales[0], narrative);
            return new G33GlyphSets(ToString(regular), ToString(bold), ToString(narrative));
        }

        private static void AppendCollection(
            StringTableCollection collection,
            Locale englishLocale,
            Locale chineseLocale,
            PseudoLocale pseudo,
            ISet<char> output)
        {
            var english = collection.GetTable(englishLocale.Identifier) as StringTable;
            var chinese = collection.GetTable(chineseLocale.Identifier) as StringTable;
            if (english == null || chinese == null)
                throw new InvalidOperationException(collection.TableCollectionName + " is missing a formal table.");
            var shared = collection.SharedData.Entries;
            for (var index = 0; index < shared.Count; index++)
            {
                var englishEntry = english.GetEntry(shared[index].Id);
                var chineseEntry = chinese.GetEntry(shared[index].Id);
                if (englishEntry == null || chineseEntry == null)
                    throw new InvalidOperationException("Missing bilingual glyph source: " + shared[index].Key + ".");
                AddCharacters(output, englishEntry.Value);
                AddCharacters(output, chineseEntry.Value);
                AddCharacters(output, pseudo.GetPseudoString(englishEntry.Value));
            }
        }

        private static void ConfigureFontSafePseudo(PseudoLocale pseudo)
        {
            Accenter accenter = null;
            Encapsulator encapsulator = null;
            for (var index = 0; index < pseudo.Methods.Count; index++)
            {
                if (pseudo.Methods[index] is Accenter foundAccenter) accenter = foundAccenter;
                if (pseudo.Methods[index] is Encapsulator foundEncapsulator) encapsulator = foundEncapsulator;
            }
            if (accenter == null || encapsulator == null)
                throw new InvalidOperationException("Pseudo locale requires Accenter and Encapsulator methods.");
            accenter.Method = CharacterSubstitutor.SubstitutionMethod.Map;
            accenter.ReplacementMap.Clear();
            accenter.ReplacementMap[' '] = '\u3000';
            for (var character = '!'; character <= '~'; character++)
                accenter.ReplacementMap[character] = (char)(character + 0xFEE0);
            encapsulator.Start = "【";
            encapsulator.End = "】";
            EditorUtility.SetDirty(pseudo);
        }

        private static void SetPreload(
            StringTableCollection collection,
            Locale english,
            Locale chinese)
        {
            var englishTable = collection.GetTable(english.Identifier) as StringTable;
            var chineseTable = collection.GetTable(chinese.Identifier) as StringTable;
            LocalizationEditorSettings.SetPreloadTableFlag(englishTable, true);
            LocalizationEditorSettings.SetPreloadTableFlag(chineseTable, true);
            EditorUtility.SetDirty(englishTable);
            EditorUtility.SetDirty(chineseTable);
        }

        private static void Prewarm(TMP_FontAsset font, ISet<char> characters, string label)
        {
            var text = ToString(characters);
            font.TryAddCharacters(text, out _, true);
            font.ReadFontAssetDefinition();
            var actualMissing = new StringBuilder();
            for (var index = 0; index < text.Length; index++)
                if (!font.HasCharacter(text[index], false, false)) actualMissing.Append(text[index]);
            if (actualMissing.Length == 0) return;
            throw new InvalidOperationException(
                label + " is missing governed glyphs: " + FormatMissingCharacters(actualMissing.ToString()) + ".");
        }

        private static void MarkAtlasesDirty(TMP_FontAsset font)
        {
            var atlases = font.atlasTextures;
            for (var index = 0; index < atlases.Length; index++)
                if (atlases[index] != null) EditorUtility.SetDirty(atlases[index]);
        }

        private static void UpdateCollectionProvenance(
            string root,
            StringTableCollection collection,
            Locale english,
            Locale chinese)
        {
            UpdateProvenanceOutputHashes(
                root + "/provenance.json",
                AssetDatabase.GetAssetPath(collection),
                AssetDatabase.GetAssetPath(collection.SharedData),
                AssetDatabase.GetAssetPath(collection.GetTable(english.Identifier)),
                AssetDatabase.GetAssetPath(collection.GetTable(chinese.Identifier)));
        }

        private static void UpdateProvenanceOutputHashes(string provenancePath, params string[] assetPaths)
        {
            if (!File.Exists(provenancePath))
                throw new FileNotFoundException("G3.3 provenance is missing.", provenancePath);
            var json = File.ReadAllText(provenancePath);
            for (var index = 0; index < assetPaths.Length; index++)
            {
                var fileName = Path.GetFileName(assetPaths[index]);
                var pattern = "(\\\"" + Regex.Escape(fileName) + "\\\"\\s*:\\s*\\\")[0-9a-f]{64}(\\\")";
                var regex = new Regex(pattern, RegexOptions.CultureInvariant);
                if (!regex.IsMatch(json))
                    throw new InvalidOperationException("Provenance has no output hash slot for " + fileName + ".");
                var hash = ComputeHash(assetPaths[index]);
                json = regex.Replace(
                    json,
                    match => match.Groups[1].Value + hash + match.Groups[2].Value,
                    1);
            }
            File.WriteAllText(provenancePath, json, new UTF8Encoding(false));
        }

        private static string ComputeHash(string path)
        {
            using (var stream = File.OpenRead(path))
            using (var sha = SHA256.Create())
            {
                var bytes = sha.ComputeHash(stream);
                var output = new StringBuilder(bytes.Length * 2);
                for (var index = 0; index < bytes.Length; index++)
                    output.Append(bytes[index].ToString("x2", CultureInfo.InvariantCulture));
                return output.ToString();
            }
        }

        private static TMP_FontAsset RequireFont(string path)
        {
            var font = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(path);
            return font != null ? font : throw new InvalidOperationException("G3.3 TMP asset is missing: " + path + ".");
        }

        private static StringTableCollection RequireCollection(string name)
        {
            var collection = LocalizationEditorSettings.GetStringTableCollection(name);
            return collection ?? throw new InvalidOperationException("G3.3 collection is missing: " + name + ".");
        }

        private static Locale FindLocale(string code)
        {
            var locales = LocalizationEditorSettings.GetLocales();
            for (var index = 0; index < locales.Count; index++)
                if (!(locales[index] is PseudoLocale) && string.Equals(
                        locales[index].Identifier.Code,
                        code,
                        StringComparison.OrdinalIgnoreCase))
                    return locales[index];
            return null;
        }

        private static HashSet<char> NewCharacterSet() => new HashSet<char>();

        private static void AddCharacters(ISet<char> output, string text)
        {
            if (string.IsNullOrEmpty(text)) return;
            for (var index = 0; index < text.Length; index++)
            {
                var character = text[index];
                if (!char.IsControl(character) && !char.IsSurrogate(character)) output.Add(character);
            }
        }

        private static string ToString(ISet<char> characters)
        {
            var array = new char[characters.Count];
            characters.CopyTo(array, 0);
            Array.Sort(array);
            return new string(array);
        }

        private static string FormatMissingCharacters(string missing)
        {
            if (string.IsNullOrEmpty(missing)) return "unknown";
            var output = new StringBuilder();
            for (var index = 0; index < missing.Length && index < 24; index++)
            {
                if (index > 0) output.Append(", ");
                output.Append('U').Append('+').Append(((int)missing[index]).ToString("X4", CultureInfo.InvariantCulture));
            }
            return output.ToString();
        }
    }

    public readonly struct G33GlyphIntegrationResult
    {
        public G33GlyphIntegrationResult(int regular, int bold, int narrative, int atlases)
        {
            RegularCharacterCount = regular;
            BoldCharacterCount = bold;
            NarrativeCharacterCount = narrative;
            AtlasTextureCount = atlases;
        }

        public int RegularCharacterCount { get; }
        public int BoldCharacterCount { get; }
        public int NarrativeCharacterCount { get; }
        public int AtlasTextureCount { get; }
    }

    public readonly struct G33GlyphSets
    {
        internal G33GlyphSets(string regular, string bold, string narrative)
        {
            Regular = regular;
            Bold = bold;
            Narrative = narrative;
        }

        public string Regular { get; }
        public string Bold { get; }
        public string Narrative { get; }
    }
}
