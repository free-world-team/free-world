using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Text;
using Game.Content.Runtime;
using UnityEngine;

namespace Game.Editor
{
    /// <summary>Builds the governed LOC-CONTENT-001 source from the baked Demo catalog.</summary>
    internal static class QinglanG33ContentLocalizationSource
    {
        internal const int CatalogDefinitionCount = 193;
        internal const int CatalogKeyCount = 386;
        internal const int LevelChangeKeyCount = 106;
        internal const int MinimumCount = CatalogKeyCount + LevelChangeKeyCount;

        private static readonly HashSet<string> StructuralTokens = new HashSet<string>(StringComparer.Ordinal)
        {
            "qinglan", "character", "mechanic", "trait", "status", "skill", "weapon", "hidden",
            "passive", "evolved", "synergy", "offer", "evolution", "enemy", "elite", "affix",
            "encounter", "reward", "map", "objective", "event", "landmark", "boss", "pickup",
            "relic", "fallback", "first", "clear", "meta", "node", "insert", "story",
            "collectible", "facility"
        };

        private static readonly Dictionary<string, string> SpecialEnglish = new Dictionary<string, string>(StringComparer.Ordinal)
        {
            { "lu_qingye", "Lu Qingye" },
            { "riding_wind", "Riding Wind" },
            { "yufeng_sword", "Yufeng Sword" },
            { "yellow_talisman", "Yellow Talisman" },
            { "lihuo_wheel", "Lihuo Wheel" },
            { "tide_orb", "Tide Orb" },
            { "zhenyue_seal", "Zhenyue Seal" },
            { "spirit_vine_seed", "Spirit Vine Seed" },
            { "qinglan_flowing_shadow_sword", "Qinglan Flowing-Shadow Sword" },
            { "taiyi_spirit_sealing_array", "Taiyi Spirit-Sealing Array" },
            { "chilu_hundred_craft_wheel", "Chilu Hundred-Craft Wheel" },
            { "mirror_sea_tide_wheel", "Mirror-Sea Tide Wheel" },
            { "mountain_boundary_seal", "Mountain-Boundary Seal" },
            { "earth_vein_spring_branch", "Earth-Vein Spring Branch" },
            { "old_court", "Old Court" },
            { "tingfeng", "Tingfeng" },
            { "zhezhi", "Zhezhi" }
        };

        private static readonly Dictionary<string, string> SpecialChinese = new Dictionary<string, string>(StringComparer.Ordinal)
        {
            { "lu_qingye", "陆青野" },
            { "riding_wind", "乘风" },
            { "yufeng_sword", "御风剑" },
            { "yellow_talisman", "黄符" },
            { "lihuo_wheel", "离火轮" },
            { "tide_orb", "潮生珠" },
            { "zhenyue_seal", "镇岳印" },
            { "spirit_vine_seed", "灵藤种" },
            { "qinglan_flowing_shadow_sword", "青岚流影剑" },
            { "taiyi_spirit_sealing_array", "太乙封灵阵" },
            { "chilu_hundred_craft_wheel", "赤炉百工轮" },
            { "mirror_sea_tide_wheel", "镜海潮轮" },
            { "mountain_boundary_seal", "山界镇岳印" },
            { "earth_vein_spring_branch", "地脉春生枝" },
            { "old_court", "旧庭" },
            { "tingfeng", "听风" },
            { "zhezhi", "折枝" }
        };

        private static readonly Dictionary<string, string> ChineseTokens = new Dictionary<string, string>(StringComparer.Ordinal)
        {
            { "12m", "十二刻" }, { "afflicted", "染煞" }, { "altar", "坛" }, { "and", "与" },
            { "armor", "甲" }, { "array", "阵" }, { "artifact", "御器" }, { "attack", "击" },
            { "aura", "灵域" }, { "balance", "衡" }, { "barrier", "屏障" }, { "bell", "铃" },
            { "blade", "刃" }, { "blank", "无字" }, { "bolt", "灵矢" }, { "boundary", "界" },
            { "brace", "架势" }, { "branch", "枝" }, { "breath", "长息" }, { "breeze", "微风" },
            { "broken", "残" }, { "burning", "灼烧" }, { "burst", "迸发" }, { "cache", "藏剑匣" },
            { "charge", "突进" }, { "chest", "宝匣" }, { "chilu", "赤炉" }, { "chime", "剑鸣" },
            { "clasp", "扣" }, { "control", "御器" }, { "copper", "铜片" }, { "core", "核心" },
            { "countershock", "反震" }, { "court", "庭" }, { "craft", "工" }, { "crane", "鹤" },
            { "crossing", "横渡" }, { "damage", "伤害" }, { "demo", "试炼" }, { "detonation", "引爆" },
            { "dew", "露" }, { "dive", "俯冲" }, { "domain", "领域" }, { "dummy", "木人" },
            { "earth", "地" }, { "echo", "回响" }, { "expansion", "展开" }, { "exploration", "探索" },
            { "explosion", "爆裂" }, { "explosive", "爆灵" }, { "falling", "落" }, { "false", "伪" },
            { "feather", "翎" }, { "first", "首" }, { "clear", "胜" }, { "flowing", "流" }, { "garden", "药圃" }, { "gathering", "聚灵" },
            { "gourd", "葫芦" }, { "grass", "草" }, { "greenwood", "青木" }, { "growth", "生长" },
            { "guard", "守卫" }, { "guest", "客" }, { "guide", "引导" }, { "hearing", "闻" },
            { "heart", "护心" }, { "heavy", "重" }, { "herb", "药" }, { "horizontal", "横斩" },
            { "hundred", "百" }, { "immunity", "免疫" }, { "inheritance", "传承" }, { "innate", "本命" },
            { "inquiry", "问脉" }, { "jade", "玉" }, { "lantern", "灯" }, { "letter", "书信" },
            { "lihuo", "离火" }, { "listen", "听风" }, { "listening", "听风" }, { "living", "生生" },
            { "long", "长" }, { "lu", "陆" }, { "manifestation", "显化" }, { "mark", "印记" },
            { "marked", "标记" }, { "mind", "心境" }, { "mirror", "镜" }, { "mountain", "山" },
            { "movement", "身法" }, { "moving", "行" }, { "myriad", "万" }, { "needle", "针" },
            { "oath", "古誓" }, { "obscuring", "蔽目" }, { "old", "旧" }, { "orb", "珠" },
            { "paper", "纸" }, { "path", "道" }, { "pattern", "纹" }, { "pavilion", "阁" },
            { "phenomena", "象" }, { "platform", "台" }, { "pod", "荚" }, { "poisoned", "中毒" },
            { "progress", "进境" }, { "propagation", "蔓延" }, { "pulse", "震波" }, { "puppet", "傀儡" },
            { "qi", "气" }, { "qinglan", "青岚" }, { "qingye", "青野" }, { "quaking", "震地" }, { "rampaging", "狂暴" },
            { "refusing", "辞却" }, { "region", "地域" }, { "remnant", "残" }, { "resonance", "共鸣" },
            { "return", "回返" }, { "revival", "复苏" }, { "riding", "乘" }, { "riot", "暴动" },
            { "rising", "涨潮" }, { "rooted", "定身" }, { "sand", "砂" }, { "scar", "痕" },
            { "scroll", "藏卷" }, { "sea", "海" }, { "seal", "印" }, { "sealed", "封存" },
            { "sealing", "封灵" }, { "seed", "种" }, { "shadow", "影" }, { "slash", "斩" },
            { "slowed", "迟缓" }, { "spirit", "灵" }, { "splitting", "分裂" }, { "spring", "春生" },
            { "stele", "碑" }, { "stone", "石" }, { "stop", "止" }, { "support", "援护" },
            { "swift", "迅风" }, { "sword", "剑" }, { "taiyi", "太乙" }, { "talisman", "符" },
            { "tassel", "剑穗" }, { "thunder", "雷" }, { "tide", "潮" }, { "tingfeng", "听风" },
            { "token", "令" }, { "trail", "痕" }, { "training", "试炼" }, { "treading", "踏" },
            { "trial", "试" }, { "undying", "不灭" }, { "variant", "异变" }, { "vein", "脉" },
            { "vine", "藤" }, { "wall", "墙" }, { "ward", "护持" }, { "wheel", "轮" },
            { "wind", "风" }, { "windfield", "风域" }, { "wood", "木" }, { "wooden", "木" },
            { "yellow", "黄" }, { "yufeng", "御风" }, { "zhenyue", "镇岳" }, { "zhezhi", "折枝" }
        };

        internal static List<ContentTranslation> Build()
        {
            if (!File.Exists(QinglanG12ContentSetup.BakedCatalogPath))
                throw new FileNotFoundException("The Qinglan Demo baked catalog is missing.", QinglanG12ContentSetup.BakedCatalogPath);
            var dto = JsonUtility.FromJson<BakedContentCatalogDto>(
                File.ReadAllText(QinglanG12ContentSetup.BakedCatalogPath));
            if (dto == null || dto.definitions == null)
                throw new InvalidOperationException("The Qinglan Demo baked catalog cannot be parsed.");
            if (dto.definitions.Length != CatalogDefinitionCount)
                throw new InvalidOperationException(
                    "LOC-CONTENT-001 expected " + CatalogDefinitionCount + " definitions but found " + dto.definitions.Length + ".");

            var output = new Dictionary<string, ContentTranslation>(StringComparer.Ordinal);
            var levelChangeCount = 0;
            for (var index = 0; index < dto.definitions.Length; index++)
            {
                var definition = dto.definitions[index];
                if (definition == null || string.IsNullOrWhiteSpace(definition.id) ||
                    string.IsNullOrWhiteSpace(definition.localizedNameKey) ||
                    string.IsNullOrWhiteSpace(definition.localizedDescriptionKey))
                    throw new InvalidOperationException("Catalog definition " + index + " has incomplete localization identity.");

                var names = BuildName(definition);
                var descriptions = BuildDescription(definition, names);
                Add(output, definition.localizedNameKey, names.English, names.Chinese);
                Add(output, definition.localizedDescriptionKey, descriptions.English, descriptions.Chinese);

                var patches = definition.levelPatches ?? Array.Empty<SkillLevelPatchDto>();
                for (var patchIndex = 0; patchIndex < patches.Length; patchIndex++)
                {
                    var patch = patches[patchIndex];
                    var keyRoot = definition.localizedDescriptionKey.EndsWith(".description", StringComparison.Ordinal)
                        ? definition.localizedDescriptionKey.Substring(0, definition.localizedDescriptionKey.Length - ".description".Length)
                        : definition.localizedDescriptionKey;
                    var key = keyRoot + ".level." + patch.level.ToString(CultureInfo.InvariantCulture) +
                              ".change." + (patchIndex + 1).ToString(CultureInfo.InvariantCulture);
                    var change = BuildLevelChange(patch);
                    Add(output, key, change.English, change.Chinese);
                    levelChangeCount++;
                }
            }

            if (levelChangeCount != LevelChangeKeyCount)
                throw new InvalidOperationException(
                    "LOC-CONTENT-001 expected " + LevelChangeKeyCount + " level changes but found " + levelChangeCount + ".");
            if (output.Count < MinimumCount)
                throw new InvalidOperationException(
                    "LOC-CONTENT-001 requires at least " + MinimumCount + " entries but built " + output.Count + ".");
            var result = new List<ContentTranslation>(output.Values);
            result.Sort((left, right) => string.CompareOrdinal(left.Key, right.Key));
            return result;
        }

        private static TextPair BuildName(RuntimeContentDefinitionDto definition)
        {
            var source = SelectDisplayTokens(definition.id);
            var joined = string.Join("_", source);
            var english = SpecialEnglish.TryGetValue(joined, out var exactEnglish)
                ? exactEnglish
                : BuildEnglish(source);
            var chinese = SpecialChinese.TryGetValue(joined, out var exactChinese)
                ? exactChinese
                : BuildChinese(source);

            switch (definition.kind)
            {
                case RuntimeContentKinds.Offer:
                    return new TextPair(english + " Selection", chinese + "选录");
                case RuntimeContentKinds.Evolution:
                    return new TextPair(english + " Evolution", chinese + "演化式");
                case RuntimeContentKinds.Reward:
                    return new TextPair(english + " Reward", chinese + "奖励");
                case RuntimeContentKinds.MetaNode:
                    return new TextPair(english + " Node", chinese + "悟道节点");
                default:
                    return new TextPair(english, chinese);
            }
        }

        private static TextPair BuildDescription(RuntimeContentDefinitionDto definition, TextPair name)
        {
            if (definition.id == "qinglan.character.lu_qingye")
                return new TextPair(
                    "A wandering sword cultivator who gathers Riding Wind while moving and turns momentum into sharper techniques.",
                    "行走四方的剑修，移动时积蓄乘风之势，并将身法化为更凌厉的剑招。");
            if (definition.id == "qinglan.map.old_court")
                return new TextPair(
                    "A ruined Qinglan training court where broken wards, herb gardens, and old sword marks still answer the wind.",
                    "青岚旧日修习之所，残阵、药圃与古老剑痕仍会随风回应。");

            switch (definition.kind)
            {
                case RuntimeContentKinds.Character:
                    return Pair(name.English + " is a playable cultivator with a defined starting technique, base attributes, and character mechanic.",
                        name.Chinese + "是可操控修行者，拥有明确的起始术式、基础属性与角色机制。");
                case RuntimeContentKinds.CharacterMechanic:
                    return Pair("The character mechanic that gathers momentum through movement and unlocks stronger Riding Wind tiers.",
                        "通过移动积蓄势能，并逐阶解锁更强乘风效果的角色机制。");
                case RuntimeContentKinds.Skill:
                    if (definition.id.IndexOf(".weapon.", StringComparison.Ordinal) >= 0)
                        return Pair(name.English + " joins the automatic attack cycle and gains a defined improvement at each technique level.",
                            name.Chinese + "加入自动出招循环，并在每个术法等级获得明确强化。");
                    if (definition.id.IndexOf(".evolved.", StringComparison.Ordinal) >= 0)
                        return Pair(name.English + " is a completed technique formed when its weapon and cultivation requirements converge.",
                            name.Chinese + "由武器与修行条件汇合而成，是该术式的完整演化形态。");
                    if (definition.id.IndexOf(".boss.", StringComparison.Ordinal) >= 0)
                        return Pair(name.English + " is an oath keeper's signature attack with a distinct warning and response window.",
                            name.Chinese + "是古誓守主的标志招式，拥有清晰的预警与应对窗口。");
                    if (definition.id.IndexOf(".enemy.", StringComparison.Ordinal) >= 0 ||
                        definition.id.IndexOf(".elite.", StringComparison.Ordinal) >= 0)
                        return Pair(name.English + " defines an enemy combat action and its configured timing, reach, and effects.",
                            name.Chinese + "定义敌方战斗动作及其既定时机、范围与效果。");
                    return Pair(name.English + " is a linked technique invoked by its parent weapon, relic, or character mechanic.",
                        name.Chinese + "是由对应武器、遗物或角色机制触发的关联术式。");
                case RuntimeContentKinds.Passive:
                    return Pair(name.English + " is a passive cultivation method that strengthens the build without occupying an active attack slot.",
                        name.Chinese + "是一门不占用主动攻击栏位、持续强化流派的被动修行法。");
                case RuntimeContentKinds.Trait:
                    return Pair(name.English + " grants a configured modifier when its source condition or progression tier is active.",
                        name.Chinese + "会在来源条件或进阶层级生效时提供既定增益。");
                case RuntimeContentKinds.Status:
                    return Pair(name.English + " is a combat state whose duration, stacking, dispel, and immunity rules are data-driven.",
                        name.Chinese + "是一种战斗状态，其持续、叠层、驱散与免疫规则均由数据定义。");
                case RuntimeContentKinds.Synergy:
                    return Pair(name.English + " records a build interaction that becomes available when its component techniques meet.",
                        name.Chinese + "记录一项流派联动，会在组成术式满足条件时生效。");
                case RuntimeContentKinds.Offer:
                    return Pair(name.English + " enters the level-up selection pool when its unlock and ownership conditions are satisfied.",
                        name.Chinese + "会在解锁与持有条件满足后进入升级选项池。");
                case RuntimeContentKinds.Evolution:
                    return Pair(name.English + " records the required weapon, passive, level, and resulting completed technique.",
                        name.Chinese + "记录所需武器、被动、等级及最终生成的完整术式。");
                case RuntimeContentKinds.Enemy:
                    return Pair(name.English + " is an Old Court combatant with a dedicated movement pattern, attack set, and reward value.",
                        name.Chinese + "是旧庭中的战斗单位，拥有独立移动方式、招式组合与奖励价值。");
                case RuntimeContentKinds.Boss:
                    return Pair(name.English + " is an oath keeper encounter whose phases, transitions, and rewards form a complete boss sequence.",
                        name.Chinese + "是一场古誓守主战，其阶段、转场与奖励共同组成完整首领流程。");
                case RuntimeContentKinds.EliteAffix:
                    return Pair(name.English + " alters an eligible enemy with a readable combat modifier and additional reward pressure.",
                        name.Chinese + "会为符合条件的敌人附加可辨识的战斗变化，并提高对应收益与威胁。");
                case RuntimeContentKinds.Encounter:
                    return Pair(name.English + " schedules the Old Court's timed enemy waves, caps, elite beats, and boss arrival.",
                        name.Chinese + "编排旧庭中的计时敌潮、数量上限、精英节点与首领登场。");
                case RuntimeContentKinds.MapObjective:
                    return Pair(name.English + " is a discoverable map objective with tracked progress, success, and failure states.",
                        name.Chinese + "是可探索的地图目标，具备进度追踪及成功、失败状态。");
                case RuntimeContentKinds.MapEvent:
                    return Pair(name.English + " is an optional Old Court event that changes local combat and grants a resolved outcome.",
                        name.Chinese + "是旧庭中的可选事件，会改变局部战况并在完成后结算结果。");
                case RuntimeContentKinds.Landmark:
                    return Pair(name.English + " marks a readable place in the Old Court and anchors exploration, lore, or rewards.",
                        name.Chinese + "标记旧庭中的可识别地点，并承载探索、见闻或奖励内容。");
                case RuntimeContentKinds.Map:
                    return Pair(name.English + " defines the playable region, encounter schedule, exploration anchors, and visual profile.",
                        name.Chinese + "定义可游玩区域、遭遇编排、探索锚点与视觉配置。");
                case RuntimeContentKinds.Pickup:
                    return Pair(name.English + " is a run pickup that applies its configured reward as soon as it is collected.",
                        name.Chinese + "是局内拾取物，收集后会立即结算其既定奖励。");
                case RuntimeContentKinds.Relic:
                    return Pair(name.English + " is a run relic that adds a persistent technique or modifier to the current build.",
                        name.Chinese + "是局内遗物，会为当前流派加入持续生效的术式或增益。");
                case RuntimeContentKinds.Reward:
                    return Pair(name.English + " is a governed reward operation used by encounters, pickups, clears, or fallback resolution.",
                        name.Chinese + "是一项受规则管理的奖励操作，可由遭遇、拾取、通关或保底结算触发。");
                case RuntimeContentKinds.MetaNode:
                    return Pair(name.English + " is a permanent Lu Qingye progression node with explicit cost, prerequisites, and effect.",
                        name.Chinese + "是陆青野的永久成长节点，拥有明确消耗、前置条件与效果。");
                case RuntimeContentKinds.MetaInsert:
                    return Pair(name.English + " is a socketable progression insert that augments a compatible permanent node.",
                        name.Chinese + "是可嵌入成长节点的进阶物，用于强化相容的永久节点。");
                case RuntimeContentKinds.Story:
                    return Pair(name.English + " is a Lu Qingye story record unlocked through governed progression and viewed in the Scroll Pavilion.",
                        name.Chinese + "是陆青野的身世藏录，经由成长条件解锁后可在藏卷阁阅览。");
                case RuntimeContentKinds.Collectible:
                    return Pair(name.English + " is an Old Court field record that preserves one fragment of the region's history.",
                        name.Chinese + "是旧庭中的野外藏录，保存着此地过往的一段残片。");
                case RuntimeContentKinds.MetaFacility:
                    return Pair(name.English + " is a hub facility that exposes one governed branch of permanent progression or records.",
                        name.Chinese + "是洞府中的功能设施，用于承载一条受规则管理的永久成长或藏录分支。");
                default:
                    throw new InvalidOperationException("LOC-CONTENT-001 has no description policy for kind '" + definition.kind + "'.");
            }
        }

        private static TextPair BuildLevelChange(SkillLevelPatchDto patch)
        {
            if (patch == null) throw new ArgumentNullException(nameof(patch));
            var prefixEn = "Level " + patch.level.ToString(CultureInfo.InvariantCulture) + ": ";
            var prefixZh = "等级" + patch.level.ToString(CultureInfo.InvariantCulture) + "：";
            if (patch.path == "cooldown" && patch.operation == "multiply")
            {
                var percent = (1f - patch.floatValue) * 100f;
                return Pair(prefixEn + "Cooldown -" + Format(percent) + "%.", prefixZh + "冷却时间缩短" + Format(percent) + "%。");
            }
            if (patch.path.StartsWith("effects[", StringComparison.Ordinal))
                return Pair(prefixEn + "Effect strength +" + Format(patch.floatValue) + ".",
                    prefixZh + "效果强度+" + Format(patch.floatValue) + "。");
            if (patch.path == "delivery.int0")
                return Pair(prefixEn + "Additional delivery count +" + patch.integerValue.ToString(CultureInfo.InvariantCulture) + ".",
                    prefixZh + "额外生效数量+" + patch.integerValue.ToString(CultureInfo.InvariantCulture) + "。");
            if (patch.path == "targeting.int0")
                return Pair(prefixEn + "Additional target count +" + patch.integerValue.ToString(CultureInfo.InvariantCulture) + ".",
                    prefixZh + "额外目标数量+" + patch.integerValue.ToString(CultureInfo.InvariantCulture) + "。");
            if (patch.path == "targeting.value0")
                return Pair(prefixEn + "Targeting reach +" + Format(patch.floatValue) + ".",
                    prefixZh + "索敌范围+" + Format(patch.floatValue) + "。");
            if (patch.path == "delivery.value2" && patch.operation == "multiply")
            {
                var percent = (1f - patch.floatValue) * 100f;
                return Pair(prefixEn + "Delivery interval -" + Format(percent) + "%.",
                    prefixZh + "生效间隔缩短" + Format(percent) + "%。");
            }
            if (patch.path.StartsWith("delivery.value", StringComparison.Ordinal))
                return Pair(prefixEn + "Technique reach +" + Format(patch.floatValue) + ".",
                    prefixZh + "术式作用范围+" + Format(patch.floatValue) + "。");
            throw new InvalidOperationException("LOC-CONTENT-001 has no level-change wording for " + patch.path + ".");
        }

        private static string[] SelectDisplayTokens(string id)
        {
            var raw = id.Split('.');
            var output = new List<string>();
            for (var index = 0; index < raw.Length; index++)
                if (!StructuralTokens.Contains(raw[index]))
                    output.Add(raw[index]);
            if (output.Count == 0) throw new InvalidOperationException("Content ID has no display tokens: " + id + ".");
            return output.ToArray();
        }

        private static string BuildEnglish(IReadOnlyList<string> segments)
        {
            var words = new List<string>();
            for (var segmentIndex = 0; segmentIndex < segments.Count; segmentIndex++)
            {
                var segment = segments[segmentIndex];
                if (SpecialEnglish.TryGetValue(segment, out var special))
                {
                    words.Add(special);
                    continue;
                }
                var tokens = segment.Split('_');
                for (var index = 0; index < tokens.Length; index++)
                    words.Add(TitleCase(tokens[index]));
            }
            return string.Join(" ", words);
        }

        private static string BuildChinese(IReadOnlyList<string> segments)
        {
            var output = new StringBuilder();
            for (var segmentIndex = 0; segmentIndex < segments.Count; segmentIndex++)
            {
                var segment = segments[segmentIndex];
                if (SpecialChinese.TryGetValue(segment, out var special))
                {
                    output.Append(special);
                    continue;
                }
                var tokens = segment.Split('_');
                for (var index = 0; index < tokens.Length; index++)
                {
                    if (int.TryParse(tokens[index], NumberStyles.None, CultureInfo.InvariantCulture, out var number))
                    {
                        output.Append('·').Append(number.ToString(CultureInfo.InvariantCulture));
                        continue;
                    }
                    if (!ChineseTokens.TryGetValue(tokens[index], out var translated))
                        throw new InvalidOperationException("LOC-CONTENT-001 Chinese terminology is missing token '" + tokens[index] + "'.");
                    output.Append(translated);
                }
            }
            return output.ToString();
        }

        private static string TitleCase(string value)
        {
            if (string.IsNullOrEmpty(value)) return value;
            if (char.IsDigit(value[0])) return value.ToUpperInvariant();
            return char.ToUpperInvariant(value[0]) + value.Substring(1);
        }

        private static string Format(float value) =>
            value.ToString(Math.Abs(value - (float)Math.Round(value)) < 0.0001f ? "0" : "0.##", CultureInfo.InvariantCulture);

        private static TextPair Pair(string english, string chinese) => new TextPair(english, chinese);

        private static void Add(
            IDictionary<string, ContentTranslation> output,
            string key,
            string english,
            string chinese)
        {
            if (output.ContainsKey(key)) throw new InvalidOperationException("Duplicate LOC-CONTENT key: " + key + ".");
            if (string.IsNullOrWhiteSpace(english) || string.IsNullOrWhiteSpace(chinese))
                throw new InvalidOperationException("Empty LOC-CONTENT translation: " + key + ".");
            output.Add(key, new ContentTranslation(key, english, chinese));
        }

        private readonly struct TextPair
        {
            internal TextPair(string english, string chinese)
            {
                English = english;
                Chinese = chinese;
            }

            internal string English { get; }
            internal string Chinese { get; }
        }
    }

    internal readonly struct ContentTranslation
    {
        internal ContentTranslation(string key, string english, string chinese)
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
