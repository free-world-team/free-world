using System;
using System.Collections.Generic;

namespace Game.Editor
{
    /// <summary>Contains the reviewed bilingual script for LOC-NARRATIVE-001.</summary>
    internal static class QinglanG33NarrativeLocalizationSource
    {
        internal const int StoryCount = 24;
        internal const int CollectibleCount = 24;
        internal const int ObjectiveCount = 12;
        internal const int EventCount = 12;
        internal const int LandmarkCount = 15;
        internal const int BossCount = 30;
        internal const int MapCount = 6;
        internal const int ExpectedCount = StoryCount + CollectibleCount + ObjectiveCount + EventCount + LandmarkCount + BossCount + MapCount;
        internal const int MinimumCount = 120;

        internal static List<NarrativeTranslation> Build()
        {
            var entries = new Dictionary<string, NarrativeTranslation>(StringComparer.Ordinal);
            AddStories(entries);
            AddCollectibles(entries);
            AddObjectives(entries);
            AddEvents(entries);
            AddLandmarks(entries);
            AddBosses(entries);
            AddMapSequence(entries);
            if (entries.Count != ExpectedCount || entries.Count < MinimumCount)
                throw new InvalidOperationException(
                    "LOC-NARRATIVE-001 expected " + ExpectedCount + " entries but built " + entries.Count + ".");
            var result = new List<NarrativeTranslation>(entries.Values);
            result.Sort((left, right) => string.CompareOrdinal(left.Key, right.Key));
            return result;
        }

        private static void AddStories(IDictionary<string, NarrativeTranslation> entries)
        {
            AddSequence(entries, "story.qinglan.story.lu_qingye.hearing_sword", new[]
            {
                Pair("When the wind crossed the ruined stele, Lu Qingye heard a sword chime where no sword remained.", "风过残碑时，陆青野听见了一声剑鸣——那里分明早已无剑。"),
                Pair("It was not a voice calling his name, but an unfinished stroke asking to be remembered.", "那不是谁在唤他的名字，而是一式未竟的剑招，请后来者记住。"),
                Pair("He set two fingers against the cold stone. The echo climbed his arm like a waking pulse.", "他以双指抵住冰冷石面，那道回响便如初醒的脉搏，沿手臂而上。"),
                Pair("For one breath, the broken court returned: disciples laughing, leaves turning, wooden swords meeting at dusk.", "一息之间，残破旧庭重现眼前：弟子笑语，落叶回旋，木剑在暮色中相击。"),
                Pair("Then the vision split beneath a single forbidden note, and every figure looked toward the sealed hall.", "随后，一声禁绝的剑音劈开幻象，所有人都望向那座封闭的大殿。"),
                Pair("Lu Qingye withdrew his hand before the memory could borrow his eyes for its final moment.", "在那段记忆借他的双眼看完结局之前，陆青野收回了手。"),
                Pair("The stele fell silent, but the cadence remained in his steps: three light, one still.", "石碑归于沉寂，节拍却留在他的步中：三轻，一止。"),
                Pair("He named the cadence Riding Wind, and carried the old court's unanswered question onward.", "他将这段步律称作“乘风”，也把旧庭未解的疑问一并带上了路。")
            });

            AddSequence(entries, "story.qinglan.story.lu_qingye.old_sword_and_gourd", new[]
            {
                Pair("The sword was too rusted to draw and the gourd was too dry to pour, yet the old man kept both within reach.", "旧剑锈得不能出鞘，酒葫芦也干得倒不出一滴，老人却始终把它们放在手边。"),
                Pair("He told Lu Qingye that a useless thing could still hold a promise better than a polished treasure.", "他告诉陆青野：无用之物，有时比光鲜宝器更守得住一句承诺。"),
                Pair("Each morning they climbed the ridge. The old man watched the clouds; the boy practiced one plain cut.", "每日清晨，他们登上山脊。老人看云，少年只练一记最寻常的直斩。"),
                Pair("Each evening the cut was judged by the gourd's shadow: hurried, proud, hesitant, or finally clear.", "每日黄昏，那一剑便由葫芦的影子评判：急、傲、疑，或终于澄明。"),
                Pair("On the last morning, only the sword and gourd waited beside a line of footprints leading into fog.", "最后一个清晨，原地只剩旧剑与葫芦，一行脚印没入雾中。"),
                Pair("Lu Qingye did not follow. He finished the plain cut before lifting either keepsake.", "陆青野没有追。他先练完那记直斩，才俯身拿起两件旧物。"),
                Pair("The sword opened without resistance. Inside the scabbard, a strip of paper read: Leave room for the wind.", "旧剑竟轻易出鞘。鞘内藏着一张纸条：给风留一步。"),
                Pair("He tied the empty gourd at his waist so every silent step would remind him of that space.", "他把空葫芦系在腰间，让每一步无声的摇晃，都提醒自己记得那一步余地。")
            });

            AddSequence(entries, "story.qinglan.story.lu_qingye.refusing_inheritance", new[]
            {
                Pair("The inheritance hall offered Lu Qingye a complete sword path, sealed against doubt and deviation.", "传承殿为陆青野备好了一条完整剑途，封住疑问，也不许偏离。"),
                Pair("Its first lesson named every enemy. Its final lesson left no name for the person holding the sword.", "开篇替他列尽敌名，终篇却没有给执剑之人留下名字。"),
                Pair("The elders called that emptiness discipline. Lu Qingye heard in it the same forbidden note from the old court.", "长老称那片空白为戒律，陆青野却从中听见了旧庭那声禁绝的剑音。"),
                Pair("He bowed to the tablets, returned the jade seal, and asked for nothing but his weathered sword.", "他向祖师牌位行礼，交还玉印，只取回那柄风雨磨旧的剑。"),
                Pair("No thunder answered. Only the hall doors opened, and ordinary mountain wind crossed the threshold.", "没有雷霆回应。殿门只是缓缓开启，寻常山风越过了门槛。"),
                Pair("The first step outside cost him a title. The second cost him a home. The third felt like his own.", "门外第一步，失去名位；第二步，失去归处；第三步，才真正属于他自己。"),
                Pair("He would learn from every road, but let no road decide where his blade must end.", "他愿向每一条路求教，却不再让任何道路替他的剑决定终点。"),
                Pair("Years later, he returned to Qinglan not as an heir, but as a witness willing to hear what remained.", "多年后，他重返青岚，不以继承者之名，只做一个愿意倾听遗声的见证人。")
            });
        }

        private static void AddCollectibles(IDictionary<string, NarrativeTranslation> entries)
        {
            AddCollectible(entries, "01",
                Pair("Balance Court rule, seventh line: no disciple may draw steel when the wind bells are silent.", "《衡庭规》其七：风铃无声时，弟子不得拔出真剑。"),
                Pair("A later hand added: If the bells ring below ground, leave the court without looking back.", "后人添注：若铃声自地下传来，立即离庭，不可回首。"),
                Pair("The ink of the warning is newer than the paper by at least thirty years.", "这句警示的墨迹，至少比纸张晚了三十年。"),
                Pair("Archive note: the seventh line is absent from every surviving copy outside Qinglan.", "藏录注：青岚之外的所有存本，都没有第七条。"));
            AddCollectible(entries, "02",
                Pair("Duty roster: tend the herb beds, oil the training puppets, and count the wind bells before dusk.", "值日簿：理药畦、油木人、暮前清点风铃。"),
                Pair("For thirteen consecutive days, the same name is marked absent while every assigned task is marked complete.", "连续十三日，同一人名被记作缺席，他名下的差事却全部完成。"),
                Pair("Pressed between the pages is a fresh blade of grass from a garden abandoned decades ago.", "簿页间压着一片仍带青色的草叶，来自一座荒废数十年的药圃。"),
                Pair("Archive note: no enrollment record matches the absent disciple's name.", "藏录注：入门名册中找不到这名缺席弟子。"));
            AddCollectible(entries, "03",
                Pair("Senior Shen: the sealed hall breathes after midnight. I heard it behind the western wall.", "沈师兄：封殿子时之后会呼吸。我在西墙后听见了。"),
                Pair("Do not bring the wardens. Bring the small bronze mirror and the sword that refuses to chime.", "不要惊动守殿人。带上那面小铜镜，还有那柄怎么也不肯鸣响的剑。"),
                Pair("The letter ends before the meeting place. Its lower half was cut away with a very sharp edge.", "信在约见地点之前戛然而止，下半截被极锋利的刃口整齐裁去。"),
                Pair("Archive note: Shen Tingyun later left the Old Court carrying an unnamed broken sword.", "藏录注：沈停云后来带着一柄无名残剑离开了旧庭。"));
            AddCollectible(entries, "04",
                Pair("I swear to hold the court until every disciple has crossed the mountain gate.", "我立誓守庭，直至最后一名弟子越过山门。"),
                Pair("If none return, I will still answer the bell, so the mountain does not mistake silence for surrender.", "若无人归来，我仍会应铃，免得群山把沉默误作屈服。"),
                Pair("The signature has been burned away, but a shallow sword mark divides the ash without tearing the page.", "署名已被烧去，一道浅浅剑痕分开灰烬，却没有割破纸页。"),
                Pair("Archive note: the oath uses the same cadence as Tingfeng's surviving sword forms.", "藏录注：誓文节律与听风留存的剑式完全一致。"));
            AddCollectible(entries, "05",
                Pair("Herb-garden broth: two greenwood leaves, one spirit seed, and enough ginger to wake a freezing disciple.", "药圃醒寒汤：青木叶两片、灵种一枚，老姜多放，须能把冻僵的弟子辣醒。"),
                Pair("Do not use dew gathered under a red moon. It makes the broth remember conversations.", "赤月下收的露水不可用，会让汤记住席间说过的话。"),
                Pair("Several stains beside the recipe form the words: It remembers songs too.", "方子旁的几处汤渍连成小字：唱过的歌，它也记得。"),
                Pair("Archive note: the garden cauldron is missing, but its stone hearth remains warm.", "藏录注：药圃铁锅已经不见，石灶却仍有余温。"));
            AddCollectible(entries, "06",
                Pair("Repair ledger: west wall, twelve bricks; guest pavilion, three tiles; bell line, one hundred knots.", "修缮簿：西墙补砖十二，客亭换瓦三，铃索重结一百扣。"),
                Pair("Final entry: sealed hall threshold, no material accepted the repair.", "末条：封殿门槛，无论何种材料皆不受补。"),
                Pair("Below it, the caretaker wrote a smaller line: perhaps the crack is meant to open, not close.", "下方有管事小字：也许这道裂缝本就该开，不该合。"),
                Pair("Archive note: the ledger date is one day after the Old Court was officially abandoned.", "藏录注：簿上日期，比旧庭正式废弃之日晚了一天。"));
        }

        private static void AddObjectives(IDictionary<string, NarrativeTranslation> entries)
        {
            AddStates(entries, "narrative.qinglan.objective.wind_altar.listen",
                Pair("Reach the wind altar and listen without attacking.", "抵达风坛，在不出招的情况下静听风声。"),
                Pair("Hold still. The altar is separating your footsteps from an older rhythm.", "保持静止。风坛正在从你的脚步中分辨一道更古老的节律。"),
                Pair("The altar answers with a clear sword chime. Its memory is now recorded.", "风坛以清越剑鸣回应，这段记忆已被收录。"),
                Pair("The resonance broke before it could settle. Return when the court is quieter.", "共鸣尚未稳定便已中断，待旧庭平静些再来。"));
            AddStates(entries, "narrative.qinglan.objective.wind_altar.guide",
                Pair("Guide three wandering wind wisps back to the altar.", "将三缕游散风息引回风坛。"),
                Pair("A wisp follows your wake. Keep moving, but do not let the court walls cut the trail.", "一缕风息正循着你的余风跟随。保持移动，别让庭墙截断风路。"),
                Pair("The returned wisps circle the altar and reopen a path through the herb garden.", "归来的风息绕坛成环，重新吹开了通往药圃的道路。"),
                Pair("The last wisp dispersed. Gather a new trail before the altar closes again.", "最后一缕风息已经散去，须在风坛再次闭合前重新引路。"));
            AddStates(entries, "narrative.qinglan.objective.wind_altar.stop_balance",
                Pair("Stop on the marked stones and restore the altar's broken balance.", "依次停在标记石上，校正风坛失衡的阵势。"),
                Pair("The next stone answers only after your momentum has fully settled.", "须待身势完全停稳，下一块阵石才会回应。"),
                Pair("The opposing currents align. A sealed sword cache rises from beneath the court.", "相逆的风流重新归衡，一座封存剑匣自庭下升起。"),
                Pair("The sequence was disturbed. Let the stones fall silent before trying again.", "阵序已经扰乱，待诸石归静后再重新尝试。"));
        }

        private static void AddEvents(IDictionary<string, NarrativeTranslation> entries)
        {
            AddStates(entries, "narrative.qinglan.event.wind_vein_riot",
                Pair("The buried wind vein surges. Listen at the altar before the pressure tears through the court.", "地底风脉骤然奔涌。须在压力撕裂旧庭前，于风坛听清它的走向。"),
                Pair("Wind scars are crossing the ground. Move between the quiet gaps.", "风痕正横扫地面，从短暂的静隙之间穿行。"),
                Pair("The vein accepts the altar's cadence and sinks back below the stone.", "风脉应和风坛节律，重新沉入石下。"),
                Pair("The surge escaped into the outer court. Expect denser enemy waves.", "奔涌之势逸入外庭，接下来的敌潮会更加密集。"));
            AddStates(entries, "narrative.qinglan.event.herb_garden_revival",
                Pair("Dormant roots stir beneath the herb garden. Lead living wind across all three beds.", "药圃地下的沉眠根系开始苏醒，将生风依次引过三处药畦。"),
                Pair("One bed has awakened. Its roots are carrying the trail onward.", "一处药畦已经苏醒，根须正在把风路传向别处。"),
                Pair("The garden flowers at once and leaves greenwood dew among the stones.", "药圃顷刻开花，并在石隙间凝出青木露。"),
                Pair("The roots recoiled from corrupted wind. Clear the garden before guiding them again.", "根系被浊风惊退，清理药圃后再重新引导。"));
            AddStates(entries, "narrative.qinglan.event.old_sword_resonance",
                Pair("A buried sword answers your weapon. Stand on the marked stones to complete its final form.", "一柄埋藏的旧剑正在回应你的兵刃，依次踏定阵石，补完它最后一式。"),
                Pair("The sword remembers the next stance. Let your movement settle into it.", "旧剑记起了下一重架势，让你的身法随之归定。"),
                Pair("The final resonance opens the sealed cache without breaking its ward.", "最后一道共鸣在不损伤封禁的情况下开启了剑匣。"),
                Pair("The remembered form collapsed. Begin again from the first marked stone.", "记忆中的剑式已经崩散，从第一块阵石重新开始。"));
        }

        private static void AddLandmarks(IDictionary<string, NarrativeTranslation> entries)
        {
            AddLandmark(entries, "wind_vein_stele",
                Pair("The stele hums one note below the surrounding wind.", "石碑的低鸣，比四周风声恰好低了一音。"),
                Pair("Its carved channels map a vein that should pass directly beneath the sealed hall.", "碑上刻槽描出一条本应直穿封殿下方的地脉。"),
                Pair("A worn handprint fits Lu Qingye's palm too closely to be chance.", "磨损的掌印与陆青野的手掌严丝合缝，不像巧合。"));
            AddLandmark(entries, "sealed_sword_cache",
                Pair("Seven locks remain; the eighth was opened from inside.", "剑匣七锁尚存，第八道却像是从内部开启。"),
                Pair("No rust touches the gap around its lid, though the hinges have not moved for years.", "匣盖缝隙不染半点锈迹，铰链却已多年未动。"),
                Pair("The ward recognizes balanced wind, not blood or sect authority.", "封禁只认归衡之风，不认血脉，也不认宗门权印。"));
            AddLandmark(entries, "herb_garden_variant",
                Pair("The garden repeats the same three beds in a pattern that changes whenever no one watches.", "药圃总是三畦，排列却会在人移开视线时悄然改变。"),
                Pair("Roots avoid the sealed hall but grow eagerly toward the guest pavilion.", "根须避开封殿，却不断向客亭方向生长。"),
                Pair("A single red flower turns to face every sword drawn nearby.", "附近每有兵刃出鞘，唯一一朵红花便会转向剑锋。"));
            AddLandmark(entries, "broken_wall_sword_mark",
                Pair("The wall was not cut through; the stone seems to have stepped aside.", "墙体并非被斩断，更像是石头自行向两侧让开。"),
                Pair("Dust inside the mark still drifts upward in a windless court.", "剑痕内的尘埃在无风旧庭中缓缓上升。"),
                Pair("The stroke ends with the same deliberate pause as Riding Wind.", "这一剑的收势，与“乘风”末尾那次刻意停步完全相同。"));
            AddLandmark(entries, "guest_pavilion_letter",
                Pair("The guest pavilion table is set for two, though one cup is fused to the wood.", "客亭桌上摆着两人茶席，其中一只杯子却与木桌融为一体。"),
                Pair("An unfinished letter asks whether an oath can protect people after it begins protecting itself.", "未写完的信中问道：当誓言开始维护自身，它还能否继续保护立誓之人？"),
                Pair("The final blank line carries the scent of fresh ink whenever the bell rings.", "每当铃声响起，末尾空白处都会浮出新墨气息。"));
        }

        private static void AddBosses(IDictionary<string, NarrativeTranslation> entries)
        {
            AddSequence(entries, "narrative.qinglan.boss.zhezhi", new[]
            {
                Pair("Training manifestation Zhezhi: court protocol restored. Present your opening stance.", "试炼显化·折枝：衡庭规制已复。报上起手式。"),
                Pair("Your feet arrive before your blade. Again.", "脚先于剑，重来。"),
                Pair("A branch bends because it understands the weight above it.", "枝所以弯，是因为它明白头顶之重。"),
                Pair("Horizontal trial. Read the empty space, not the wooden edge.", "横斩试。看空处，不要只看木锋。"),
                Pair("Falling wood casts more than one shadow.", "落木之下，不止一道影。"),
                Pair("The court once taught patience before power. Show me what remains.", "衡庭昔日先授耐心，后授力量。让我看看还剩多少。"),
                Pair("Second measure: the training ring contracts.", "第二衡：试炼圈收束。"),
                Pair("Do not confuse retreat with surrender.", "莫把退步误作认输。"),
                Pair("Good. The pause in your form belongs to you, not the manual.", "很好。你剑式中的停顿属于你，不属于剑谱。"),
                Pair("Final measure. Cross the line without abandoning your center.", "最后一衡。越线，不可失中。"),
                Pair("Protocol complete. The court records your cadence.", "规制完成。衡庭已记下你的节律。"),
                Pair("Seek the keeper below. He has forgotten that a gate may open both ways.", "去寻下方的守誓者吧。他忘了门本就可以向两边开启。")
            });

            AddSequence(entries, "narrative.qinglan.boss.tingfeng", new[]
            {
                Pair("Stop. The last disciple has not yet crossed the mountain gate.", "止步。最后一名弟子尚未越过山门。"),
                Pair("I am Tingfeng, keeper of the Old Court. State whom you came to retrieve.", "我是听风，旧庭守誓剑傀。说出你要带走谁。"),
                Pair("No name? Then the court still counts you among the missing.", "没有名字？那衡庭仍会把你计入失踪名册。"),
                Pair("Sword qi—first warning.", "剑气——一警。"),
                Pair("The wind behind you is not an escape. It is my second blade.", "身后的风不是退路，是我的第二柄剑。"),
                Pair("False chime. Trust the ground beneath your feet.", "伪鸣。信脚下，不可信耳中。"),
                Pair("I held this gate while the bells burned red.", "赤铃燃响之时，是我守住了这道门。"),
                Pair("I held it when the elders fled with the true records.", "长老携真录遁走之后，仍是我守着。"),
                Pair("Phase two: the obscuring windfield fills the court.", "二式启：蔽目风域覆庭。"),
                Pair("If sight fails, listen for the oath between the chimes.", "若目不可用，就在铃声间听我的誓。"),
                Pair("Remnant swords, answer the roll.", "残剑列阵，应名。"),
                Pair("Crossing wind scar—do not stand where the memory divides.", "横风痕——莫立在记忆断开的地方。"),
                Pair("You carry the old man's plain cut. Why did he send you back?", "你带着那老人的直斩。他为何让你回来？"),
                Pair("No. He would never ask another person to finish his choice.", "不。他从不会让别人替自己完成选择。"),
                Pair("Then this oath has mistaken endurance for purpose.", "原来这道誓，把坚持错当成了意义。"),
                Pair("Final form: Undying Oath.", "终式：不灭古誓。"),
                Pair("Break the gate, Lu Qingye. Let the mountain count us both as returned.", "破门吧，陆青野。让群山把你我都计作归来。"),
                Pair("The bells are quiet at last. Take the records—and leave the door open.", "风铃终于安静了。带走藏录——也请让门继续开着。")
            });
        }

        private static void AddMapSequence(IDictionary<string, NarrativeTranslation> entries)
        {
            AddSequence(entries, "narrative.qinglan.map.old_court", new[]
            {
                Pair("The Old Court receives no visitors, yet its wind bells count one arrival.", "旧庭早已不迎来客，风铃却仍数出一人入山。"),
                Pair("Follow the broken wall east. The wind altar lies beyond the silent training rings.", "沿东侧断墙前行，穿过寂静试炼场，便是风坛。"),
                Pair("Something beneath the sealed hall has begun matching your footsteps.", "封殿之下，有什么开始模仿你的脚步。"),
                Pair("The court's three memories are awake. Their paths now converge below the central hall.", "旧庭三段记忆已经苏醒，风路正汇向中央大殿之下。"),
                Pair("The keeper's oath is released. Dawn reaches the training court for the first time in years.", "守誓已解。多年之后，晨光第一次照进衡庭。"),
                Pair("Not every bell rang, and not every name returned—but the gate remains open.", "并非每只铃都再度响起，也并非每个名字都能归来——但山门已经打开。")
            });
        }

        private static void AddCollectible(
            IDictionary<string, NarrativeTranslation> entries,
            string number,
            TextPair body,
            TextPair inscriptionOne,
            TextPair inscriptionTwo,
            TextPair annotation)
        {
            var root = "collectible.qinglan.collectible.old_court." + number;
            Add(entries, root + ".body", body);
            Add(entries, root + ".inscription.01", inscriptionOne);
            Add(entries, root + ".inscription.02", inscriptionTwo);
            Add(entries, root + ".annotation", annotation);
        }

        private static void AddStates(
            IDictionary<string, NarrativeTranslation> entries,
            string root,
            TextPair prompt,
            TextPair progress,
            TextPair complete,
            TextPair failed)
        {
            Add(entries, root + ".prompt", prompt);
            Add(entries, root + ".progress", progress);
            Add(entries, root + ".complete", complete);
            Add(entries, root + ".failed", failed);
        }

        private static void AddLandmark(
            IDictionary<string, NarrativeTranslation> entries,
            string id,
            TextPair approach,
            TextPair inspect,
            TextPair insight)
        {
            var root = "narrative.qinglan.landmark." + id;
            Add(entries, root + ".approach", approach);
            Add(entries, root + ".inspect", inspect);
            Add(entries, root + ".insight", insight);
        }

        private static void AddSequence(
            IDictionary<string, NarrativeTranslation> entries,
            string root,
            IReadOnlyList<TextPair> lines)
        {
            for (var index = 0; index < lines.Count; index++)
                Add(entries, root + "." + (index + 1).ToString("00"), lines[index]);
        }

        private static void Add(
            IDictionary<string, NarrativeTranslation> entries,
            string key,
            TextPair text)
        {
            if (entries.ContainsKey(key)) throw new InvalidOperationException("Duplicate LOC-NARRATIVE key: " + key + ".");
            if (string.IsNullOrWhiteSpace(text.English) || string.IsNullOrWhiteSpace(text.Chinese))
                throw new InvalidOperationException("Empty LOC-NARRATIVE translation: " + key + ".");
            entries.Add(key, new NarrativeTranslation(key, text.English, text.Chinese));
        }

        private static TextPair Pair(string english, string chinese) => new TextPair(english, chinese);

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

    internal readonly struct NarrativeTranslation
    {
        internal NarrativeTranslation(string key, string english, string chinese)
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
