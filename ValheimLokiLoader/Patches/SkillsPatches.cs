using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using HarmonyLib;
using UnityEngine;

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

                System.Console.WriteLine("Skills awake \nm_skills:");
                foreach (Skills.SkillDef skill in __instance.m_skills)
                {
                    System.Console.WriteLine(skill.m_skill + " - " + skill.m_description);
                }
            }
        }

        [HarmonyPatch(typeof(Skills), "Load")]
        public static class LoadPatch
        {
            public static void Postfix(ref Skills __instance, ZPackage pkg, ref Dictionary<Skills.SkillType, Skills.Skill> ___m_skillData)
            {
                System.Console.WriteLine("Skills Load\nm_skills:");
                foreach (Skills.SkillDef skill in __instance.m_skills)
                {
                    System.Console.WriteLine(skill.m_skill + " " + skill.m_description);
                }

                System.Console.WriteLine("\nSkillData load:");
                foreach (KeyValuePair<Skills.SkillType, Skills.Skill> pair in ___m_skillData)
                {
                    System.Console.WriteLine(pair.Key.ToString() + " - " + pair.Value.m_level);
                }
            }
        }

        [HarmonyPatch(typeof(Skills), "IsSkillValid")]
        public static class IsSkillValidPatch
        {
            public static bool Prefix(ref Skills __instance, ref bool __result, Skills.SkillType type)
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
