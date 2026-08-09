using System;
using System.Collections.Generic;
using UnityEngine.Localization.Tables;

namespace Game.Editor
{
    /// <summary>Builds the reviewed LOC-UI-001 bilingual source from the preserved M8 UI and G3.3 additions.</summary>
    internal static class QinglanG33UiLocalizationSource
    {
        internal const int MinimumCount = 180;

        internal static List<UiTranslation> Build(StringTable legacyEnglish, StringTable legacyChinese)
        {
            if (legacyEnglish == null) throw new ArgumentNullException(nameof(legacyEnglish));
            if (legacyChinese == null) throw new ArgumentNullException(nameof(legacyChinese));
            var entries = new Dictionary<string, UiTranslation>(StringComparer.Ordinal);
            var shared = legacyEnglish.SharedData.Entries;
            for (var index = 0; index < shared.Count; index++)
            {
                var key = shared[index].Key;
                if (!IsUiOrDiagnosticKey(key)) continue;
                var english = legacyEnglish.GetEntry(shared[index].Id);
                var chinese = legacyChinese.GetEntry(shared[index].Id);
                if (english == null || chinese == null ||
                    string.IsNullOrWhiteSpace(english.Value) || string.IsNullOrWhiteSpace(chinese.Value))
                    throw new InvalidOperationException("Legacy bilingual UI source is incomplete: " + key + ".");
                entries.Add(key, new UiTranslation(key, english.Value, chinese.Value));
            }

            Add(entries, "ui.common.confirm", "Confirm", "确认");
            Add(entries, "ui.common.cancel", "Cancel", "取消");
            Add(entries, "ui.common.continue", "Continue", "继续");
            Add(entries, "ui.common.apply", "Apply", "应用");
            Add(entries, "ui.common.reset", "Reset", "重置");
            Add(entries, "ui.common.yes", "Yes", "是");
            Add(entries, "ui.common.no", "No", "否");
            Add(entries, "ui.common.next", "Next", "下一项");
            Add(entries, "ui.common.previous", "Previous", "上一项");
            Add(entries, "ui.common.details", "Details", "详情");
            Add(entries, "ui.qinglan.hud.health", "Health", "生命");
            Add(entries, "ui.qinglan.hud.experience", "Sword Insight", "剑悟");
            Add(entries, "ui.qinglan.hud.level", "Realm Level", "境界等级");
            Add(entries, "ui.qinglan.hud.elapsed_time", "Elapsed Time", "历时");
            Add(entries, "ui.qinglan.hud.defeated", "Enemies Defeated", "已破敌");
            Add(entries, "ui.qinglan.hud.boss", "Oath Keeper", "古誓守主");
            Add(entries, "ui.qinglan.hud.elite", "Elite", "精英");
            Add(entries, "ui.qinglan.hud.objective", "Current Objective", "当前目标");
            Add(entries, "ui.qinglan.hud.objective.nearby", "Objective Nearby", "目标在附近");
            Add(entries, "ui.qinglan.hud.objective.complete", "Objective Complete", "目标已完成");
            Add(entries, "ui.qinglan.hud.objective.failed", "Objective Failed", "目标未完成");
            Add(entries, "ui.qinglan.settings.tab.gameplay", "Gameplay", "玩法");
            Add(entries, "ui.qinglan.settings.tab.audio", "Audio", "音频");
            Add(entries, "ui.qinglan.settings.tab.video", "Display", "显示");
            Add(entries, "ui.qinglan.settings.tab.controls", "Controls", "控制");
            Add(entries, "ui.qinglan.settings.tab.accessibility", "Accessibility", "可访问性");
            Add(entries, "ui.qinglan.settings.language.en", "English", "英文");
            Add(entries, "ui.qinglan.settings.language.zh_hans", "Simplified Chinese", "简体中文");
            Add(entries, "ui.qinglan.settings.font_scale.100", "100%", "100%");
            Add(entries, "ui.qinglan.settings.font_scale.125", "125%", "125%");
            Add(entries, "ui.qinglan.settings.font_scale.150", "150%", "150%");
            Add(entries, "ui.qinglan.map.legend.player", "Sword Bearer", "御剑者");
            Add(entries, "ui.qinglan.map.legend.landmark", "Landmark", "地标");
            Add(entries, "ui.qinglan.map.legend.objective", "Objective", "目标");
            Add(entries, "ui.qinglan.map.legend.boss", "Oath Keeper", "古誓守主");
            Add(entries, "ui.qinglan.toast.unlock", "New Record Unlocked", "新藏录已解锁");
            Add(entries, "ui.qinglan.toast.currency", "Spirit Sand Acquired", "获得灵砂");
            Add(entries, "ui.qinglan.toast.reward", "Run Reward Secured", "本局奖励已收纳");
            Add(entries, "ui.qinglan.toast.autosaved", "Progress Saved", "进度已保存");

            if (entries.Count < MinimumCount)
                throw new InvalidOperationException(
                    "LOC-UI-001 requires at least " + MinimumCount + " reviewed keys but built " + entries.Count + ".");
            var result = new List<UiTranslation>(entries.Values);
            result.Sort((left, right) => string.CompareOrdinal(left.Key, right.Key));
            return result;
        }

        private static bool IsUiOrDiagnosticKey(string key) =>
            key.StartsWith("ui.", StringComparison.Ordinal) ||
            key.StartsWith("save.", StringComparison.Ordinal) ||
            key.StartsWith("platform.", StringComparison.Ordinal);

        private static void Add(
            IDictionary<string, UiTranslation> entries,
            string key,
            string english,
            string chinese)
        {
            if (entries.ContainsKey(key)) throw new InvalidOperationException("Duplicate LOC-UI key: " + key + ".");
            entries.Add(key, new UiTranslation(key, english, chinese));
        }
    }

    internal readonly struct UiTranslation
    {
        internal UiTranslation(string key, string english, string chinese)
        {
            Key = key;
            English = english;
            Chinese = chinese;
        }

        internal string Key { get; }
        internal string English { get; }
        internal string Chinese { get; }
    }
}
