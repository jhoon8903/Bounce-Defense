using Game.Skills;
using UnityEditor;
using UnityEngine;

namespace Game.Tests
{
    internal static class SkillTestFactory
    {
        public static SkillDefinition Skill(string id, SkillCategory cat, int maxLevel = 3, int[] ballDamage = null)
        {
            SkillDefinition so = ScriptableObject.CreateInstance<SkillDefinition>();
            SerializedObject s = new SerializedObject(so);
            s.FindProperty("skillId").stringValue = id;
            s.FindProperty("displayName").stringValue = id;
            s.FindProperty("category").enumValueIndex = (int)cat;
            s.FindProperty("maxLevel").intValue = maxLevel;
            if (ballDamage != null)
            {
                SerializedProperty arr = s.FindProperty("ballDamagePerLevel");
                arr.arraySize = ballDamage.Length;
                for (int i = 0; i < ballDamage.Length; i++) arr.GetArrayElementAtIndex(i).intValue = ballDamage[i];
            }
            s.ApplyModifiedPropertiesWithoutUndo();
            return so;
        }

        public static SkillDatabase Database(SkillDefinition[] actives, SkillDefinition[] passives)
        {
            SkillDatabase db = ScriptableObject.CreateInstance<SkillDatabase>();
            SerializedObject s = new SerializedObject(db);
            SerializedProperty a = s.FindProperty("activeSkills");
            a.arraySize = actives.Length;
            for (int i = 0; i < actives.Length; i++) a.GetArrayElementAtIndex(i).objectReferenceValue = actives[i];
            SerializedProperty p = s.FindProperty("passiveSkills");
            p.arraySize = passives.Length;
            for (int i = 0; i < passives.Length; i++) p.GetArrayElementAtIndex(i).objectReferenceValue = passives[i];
            s.ApplyModifiedPropertiesWithoutUndo();
            return db;
        }
    }
}
