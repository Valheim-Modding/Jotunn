using System.Collections.Generic;
using HarmonyLib;
using ValheimLokiLoader.Managers;

namespace ValheimLokiLoader.Patches
{
    class SkillsPatches
    {
        [HarmonyPatch(typeof(Skills), "Awake")]
        public static class AwakePatch
        {
            public static void Postfix(ref Skills __instance)
            {
                foreach (Skills.SkillDef skill in SkillManager.Skills)
                {
                    __instance.m_skills.Add(skill);
                    System.Console.WriteLine("Added extra skill: " + skill.m_skill);
                }
            }
        }

        [HarmonyPatch(typeof(Skills), "IsSkillValid")]
        public static class IsSkillValidPatch
        {
            public static bool Prefix(ref bool __result, Skills.SkillType type)
            {
                if (SkillManager.Skills.Exists(s => s.m_skill == type))
                {
                    __result = true;
                    return false;
                }

                return true;
            }
        }
    }
}
