using System;
using System.Collections.Generic;
using Game.Content.Authoring;
using Game.Content.Runtime;
using UnityEditor;
using UnityEngine;

namespace Game.Editor
{
    /// <summary>Reapplies reviewed G3.4 content numbers and advances the Demo pack version.</summary>
    public static class QinglanG34BalanceContentSetup
    {
        public const string FrozenPackVersion = "0.10.0";

        [MenuItem("Tools/Free World/Qinglan/G3.4 Apply Balance Candidate")]
        public static void Configure()
        {
            // G1.3 remains the canonical source. Apply the reviewed subset directly so
            // rerunning a balance candidate cannot repopulate the formal UI table with
            // legacy content-localization entries.
            ApplyCharacterAndSwordCandidate();

            var pack = AssetDatabase.LoadAssetAtPath<ContentPackAuthoring>(QinglanG12ContentSetup.PackPath);
            if (pack == null) throw new InvalidOperationException("Qinglan Demo pack is missing.");
            var definitions = new List<ContentAuthoringBase>(pack.Definitions.Count);
            for (var index = 0; index < pack.Definitions.Count; index++) definitions.Add(pack.Definitions[index]);
            pack.Configure(
                "qinglan.pack.demo",
                FrozenPackVersion,
                ContentPackTopology.QinglanDemoSchemaVersion,
                "0.1.0",
                string.Empty,
                Array.Empty<ContentPackDependencyAuthoring>(),
                "packs/qinglan.demo/catalog",
                "pack.qinglan.demo",
                false,
                definitions.ToArray());
            EditorUtility.SetDirty(pack);
            AssetDatabase.SaveAssets();
            var baked = ContentBakeUtility.Bake(pack);
            if (!baked.IsSuccess) throw new UnityException(baked.Error.ToString());
            ContentBakeUtility.WriteCatalog(QinglanG12ContentSetup.PackPath, baked.Value);
            Debug.Log("[Qinglan G3.4 Balance Content] Applied pack " + FrozenPackVersion +
                      " hash " + baked.Value.ContentHash);
        }

        private static void ApplyCharacterAndSwordCandidate()
        {
            var character = AssetDatabase.LoadAssetAtPath<CharacterAuthoring>(
                QinglanG12ContentSetup.Folder + "/LuQingye.asset");
            var sword = AssetDatabase.LoadAssetAtPath<SkillAuthoring>(
                QinglanG12ContentSetup.Folder + "/YufengSword.asset");
            if (character == null || sword == null)
                throw new InvalidOperationException("G3.4 character or starting sword authoring is missing.");

            var characterObject = new SerializedObject(character);
            characterObject.FindProperty("baseMaxHealth").floatValue = 900f;
            characterObject.ApplyModifiedPropertiesWithoutUndo();

            var swordObject = new SerializedObject(sword);
            swordObject.FindProperty("cooldownSeconds").floatValue = 1.45f;
            var targeting = swordObject.FindProperty("targeting");
            targeting.FindPropertyRelative("value0").floatValue = 20f;
            targeting.FindPropertyRelative("int0").intValue = 2;
            var delivery = swordObject.FindProperty("delivery");
            delivery.FindPropertyRelative("value0").floatValue = 16f;
            delivery.FindPropertyRelative("value1").floatValue = 20f;
            swordObject.FindProperty("effects").GetArrayElementAtIndex(0)
                .FindPropertyRelative("value0").floatValue = 24f;
            var patches = swordObject.FindProperty("levelPatches");
            patches.GetArrayElementAtIndex(2).FindPropertyRelative("integerValue").intValue = 1;
            patches.GetArrayElementAtIndex(7).FindPropertyRelative("floatValue").floatValue = 8f;
            swordObject.ApplyModifiedPropertiesWithoutUndo();

            SetFirstEffectValue("YellowTalisman.asset", 12f);
            SetFirstEffectValue("TalismanDetonation.asset", 8f);
            SetFirstEffectValue("SpiritVineSeed.asset", 6f);
            SetFirstEffectValue("EnemyGrassSpiritAura.asset", 1f);
            SetFirstEffectValue("EnemyPaperCraneDive.asset", 2f);
            SetFirstEffectValue("EnemyWoodenPuppetHeavySlash.asset", 3f);
            SetFirstEffectValue("EnemyStoneLanternBolt.asset", 2f);
            SetFirstEffectValue("EnemyExplosiveSeedBurst.asset", 3f);
            SetEnemyDamageMultiplier("GrassSpirit.asset");
            SetEnemyDamageMultiplier("PaperCraneSpirit.asset");
            SetEnemyDamageMultiplier("WoodenSwordPuppet.asset");
            SetEnemyDamageMultiplier("StoneLanternGuard.asset");
            SetEnemyDamageMultiplier("WindBellSpirit.asset");
            SetEnemyDamageMultiplier("ExplosiveSeedPod.asset");
            SetEnemyDamageMultiplier("BossZhezhiEnemy.asset");
            SetEnemyDamageMultiplier("BossTingfengEnemy.asset");
            SetEnemyHealth("BossZhezhiEnemy.asset", 800f);
            SetEnemyHealth("BossTingfengEnemy.asset", 50f);
            SetEnemyExperience("GrassSpirit.asset", 1.25f);
            SetEnemyExperience("PaperCraneSpirit.asset", 1.875f);
            SetEnemyExperience("WoodenSwordPuppet.asset", 3.75f);
            SetEnemyExperience("StoneLanternGuard.asset", 3.125f);
            SetEnemyExperience("WindBellSpirit.asset", 2.5f);
            SetEnemyExperience("ExplosiveSeedPod.asset", 2.5f);
            EditorUtility.SetDirty(character);
            EditorUtility.SetDirty(sword);
        }

        private static void SetEnemyDamageMultiplier(string fileName)
        {
            var enemy = AssetDatabase.LoadAssetAtPath<EnemyAuthoring>(
                QinglanG12ContentSetup.Folder + "/" + fileName);
            if (enemy == null) throw new InvalidOperationException("G3.4 enemy authoring is missing: " + fileName);
            var serialized = new SerializedObject(enemy);
            serialized.FindProperty("baseDamage").floatValue = 1f;
            serialized.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(enemy);
        }

        private static void SetEnemyHealth(string fileName, float value)
        {
            var enemy = AssetDatabase.LoadAssetAtPath<EnemyAuthoring>(
                QinglanG12ContentSetup.Folder + "/" + fileName);
            if (enemy == null) throw new InvalidOperationException("G3.4 enemy authoring is missing: " + fileName);
            var serialized = new SerializedObject(enemy);
            serialized.FindProperty("baseMaxHealth").floatValue = value;
            serialized.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(enemy);
        }

        private static void SetEnemyExperience(string fileName, float value)
        {
            var enemy = AssetDatabase.LoadAssetAtPath<EnemyAuthoring>(
                QinglanG12ContentSetup.Folder + "/" + fileName);
            if (enemy == null) throw new InvalidOperationException("G3.4 enemy authoring is missing: " + fileName);
            var serialized = new SerializedObject(enemy);
            serialized.FindProperty("experienceReward").floatValue = value;
            serialized.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(enemy);
        }

        private static void SetFirstEffectValue(string fileName, float value)
        {
            var skill = AssetDatabase.LoadAssetAtPath<SkillAuthoring>(
                QinglanG12ContentSetup.Folder + "/" + fileName);
            if (skill == null) throw new InvalidOperationException("G3.4 skill authoring is missing: " + fileName);
            var serialized = new SerializedObject(skill);
            serialized.FindProperty("effects").GetArrayElementAtIndex(0)
                .FindPropertyRelative("value0").floatValue = value;
            serialized.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(skill);
        }
    }
}
