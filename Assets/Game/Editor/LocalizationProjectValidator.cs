using System;
using UnityEditor.Localization;
using UnityEngine.Localization.Pseudo;
using UnityEngine.Localization.Tables;

namespace Game.Editor
{
    /// <summary>Validates the checked-in M8 locales and every required localization key.</summary>
    internal static class LocalizationProjectValidator
    {
        internal static void AppendCurrentProject(ValidationReport report)
        {
            if (report == null) throw new ArgumentNullException(nameof(report));
            var settings = LocalizationEditorSettings.ActiveLocalizationSettings;
            if (settings == null)
            {
                report.Add("M8-LOCALIZATION-SETTINGS", "No active Localization Settings asset is configured.");
                return;
            }

            var englishLocale = FindLocale("en");
            var chineseLocale = FindLocale("zh-Hans");
            var pseudoFound = LocalizationEditorSettings.GetPseudoLocales().Count > 0;
            if (englishLocale == null) report.Add("M8-LOCALE-EN", "English locale 'en' is missing.");
            if (chineseLocale == null) report.Add("M8-LOCALE-ZH-HANS", "Simplified Chinese locale 'zh-Hans' is missing.");
            if (!pseudoFound) report.Add("M8-LOCALE-PSEUDO", "A pseudo locale is missing.");

            var collection = LocalizationEditorSettings.GetStringTableCollection("UI");
            if (collection == null)
            {
                report.Add("M8-LOCALIZATION-TABLE", "The UI string table collection is missing.");
                return;
            }
            var english = englishLocale == null ? null : collection.GetTable(englishLocale.Identifier) as StringTable;
            var chinese = chineseLocale == null ? null : collection.GetTable(chineseLocale.Identifier) as StringTable;
            if (english == null) report.Add("M8-LOCALIZATION-TABLE-EN", "The English UI string table is missing.");
            if (chinese == null) report.Add("M8-LOCALIZATION-TABLE-ZH-HANS", "The Simplified Chinese UI string table is missing.");
            if (english == null || chinese == null) return;

            var sharedEntries = collection.SharedData.Entries;
            if (sharedEntries.Count < QinglanG33UiLocalizationSource.MinimumCount)
                report.Add(
                    "G33-LOC-UI-COUNT",
                    "The formal UI collection requires at least " +
                    QinglanG33UiLocalizationSource.MinimumCount + " keys but found " + sharedEntries.Count + ".");
            for (var index = 0; index < sharedEntries.Count; index++)
            {
                ValidateEntry(english, sharedEntries[index].Key, "en", report);
                ValidateEntry(chinese, sharedEntries[index].Key, "zh-Hans", report);
            }

            var contentCollection = LocalizationEditorSettings.GetStringTableCollection("QinglanContent");
            if (contentCollection == null)
            {
                report.Add("G33-LOC-CONTENT-TABLE", "The formal QinglanContent string table collection is missing.");
            }
            else
            {
                var contentEnglish = contentCollection.GetTable(englishLocale.Identifier) as StringTable;
                var contentChinese = contentCollection.GetTable(chineseLocale.Identifier) as StringTable;
                if (contentEnglish == null)
                    report.Add("G33-LOC-CONTENT-EN", "The English QinglanContent table is missing.");
                if (contentChinese == null)
                    report.Add("G33-LOC-CONTENT-ZH-HANS", "The Simplified Chinese QinglanContent table is missing.");
                if (contentCollection.SharedData.Entries.Count < QinglanG33ContentLocalizationSource.MinimumCount)
                    report.Add(
                        "G33-LOC-CONTENT-COUNT",
                        "The formal content collection requires at least " +
                        QinglanG33ContentLocalizationSource.MinimumCount + " keys but found " +
                        contentCollection.SharedData.Entries.Count + ".");
                if (contentEnglish != null && contentChinese != null)
                {
                    var contentEntries = contentCollection.SharedData.Entries;
                    for (var index = 0; index < contentEntries.Count; index++)
                    {
                        ValidateEntry(contentEnglish, contentEntries[index].Key, "en", report);
                        ValidateEntry(contentChinese, contentEntries[index].Key, "zh-Hans", report);
                    }
                }
            }

            var narrativeCollection = LocalizationEditorSettings.GetStringTableCollection("QinglanNarrative");
            if (narrativeCollection == null)
            {
                report.Add("G33-LOC-NARRATIVE-TABLE", "The formal QinglanNarrative string table collection is missing.");
            }
            else
            {
                var narrativeEnglish = narrativeCollection.GetTable(englishLocale.Identifier) as StringTable;
                var narrativeChinese = narrativeCollection.GetTable(chineseLocale.Identifier) as StringTable;
                if (narrativeEnglish == null)
                    report.Add("G33-LOC-NARRATIVE-EN", "The English QinglanNarrative table is missing.");
                if (narrativeChinese == null)
                    report.Add("G33-LOC-NARRATIVE-ZH-HANS", "The Simplified Chinese QinglanNarrative table is missing.");
                if (narrativeCollection.SharedData.Entries.Count < QinglanG33NarrativeLocalizationSource.MinimumCount)
                    report.Add(
                        "G33-LOC-NARRATIVE-COUNT",
                        "The formal narrative collection requires at least " +
                        QinglanG33NarrativeLocalizationSource.MinimumCount + " keys but found " +
                        narrativeCollection.SharedData.Entries.Count + ".");
                if (narrativeEnglish != null && narrativeChinese != null)
                {
                    var narrativeEntries = narrativeCollection.SharedData.Entries;
                    for (var index = 0; index < narrativeEntries.Count; index++)
                    {
                        ValidateEntry(narrativeEnglish, narrativeEntries[index].Key, "en", report);
                        ValidateEntry(narrativeChinese, narrativeEntries[index].Key, "zh-Hans", report);
                    }
                }
            }

            if (pseudoFound)
            {
                var pseudo = LocalizationEditorSettings.GetPseudoLocales()[0];
                var sample = english.GetEntry("ui.qinglan.title.subtitle");
                if (sample == null || pseudo.Methods.Count == 0 ||
                    string.Equals(pseudo.GetPseudoString(sample.Value), sample.Value, StringComparison.Ordinal))
                    report.Add("G33-LOC-PSEUDO", "Pseudo locale must transform the formal English UI table.");
            }
        }

        private static UnityEngine.Localization.Locale FindLocale(string code)
        {
            var locales = LocalizationEditorSettings.GetLocales();
            for (var index = 0; index < locales.Count; index++)
            {
                var locale = locales[index];
                if (!(locale is PseudoLocale) && string.Equals(locale.Identifier.Code, code, StringComparison.OrdinalIgnoreCase))
                    return locale;
            }
            return null;
        }

        private static void ValidateEntry(StringTable table, string key, string locale, ValidationReport report)
        {
            var entry = table.GetEntry(key);
            if (entry == null || string.IsNullOrWhiteSpace(entry.Value))
                report.Add("M8-LOCALIZATION-KEY", locale + " is missing non-empty localized entry '" + key + "'.");
            else if (entry.Value.IndexOf("[Placeholder]", StringComparison.OrdinalIgnoreCase) >= 0 ||
                     entry.Value.IndexOf("[占位]", StringComparison.Ordinal) >= 0 ||
                     (key.StartsWith("content.", StringComparison.Ordinal) &&
                      entry.Value.IndexOf("Unavailable", StringComparison.OrdinalIgnoreCase) >= 0) ||
                     string.Equals(entry.Value, key, StringComparison.Ordinal))
                report.Add("G33-LOC-PLACEHOLDER", locale + " contains unfinished localized entry '" + key + "'.");
        }
    }
}
