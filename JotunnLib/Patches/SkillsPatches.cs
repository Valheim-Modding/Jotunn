using System.Collections.Generic;
using HarmonyLib;
using JotunnLib.Managers;
using UnityEngine;

namespace JotunnLib.Patches
{
    class SkillsPatches
    {
        [HarmonyPatch(typeof(Skills), "Awake")]
        public static class AwakePatch
        {
            public static void Postfix(ref Skills __instance)
            {
                // TODO: Move into SkillsManager Register
                foreach (var pair in SkillManager.Instance.Skills)
                {
                    Skills.SkillDef skill = new Skills.SkillDef()
                    {
                        m_description = pair.Value.Description,
                        m_icon = pair.Value.Icon,
                        m_increseStep = pair.Value.IncreaseStep,
                        m_skill = pair.Value.UID
                    };
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
                foreach (var pair in SkillManager.Instance.Skills)
                {
                    if (pair.Value.UID == type)
                    {
                        __result = true;
                        return false;
                    }
                }

                return true;
            }
        }

        [HarmonyPatch(typeof(Skills), "CheatRaiseSkill")]
        public static class CheatRaiseSkillPatch
        {
            [HarmonyPrefix]
            public static bool Prefix(string name, float value, Skills __instance, Player ___m_player)
            {
                foreach (var config in SkillManager.Instance.Skills)
                {
                    if (config.Value.Name.ToLower() == name.ToLower())
                    {
                        Skills.Skill skill = Traverse.Create(__instance).Method("GetSkill", config.Value.UID).GetValue<Skills.Skill>(config.Value.UID);
                        skill.m_level += value;
                        skill.m_level = Mathf.Clamp(skill.m_level, 0f, 100f);
                        ___m_player.Message(MessageHud.MessageType.TopLeft, "Skill increased " + config.Value.Name + ": " + (int)skill.m_level, 0, skill.m_info.m_icon);
                        Console.instance.Print("Skill " + config.Value.Name + " = " + skill.m_level);
                        return false;
                    }
                }
                return true;
            }
        }

        [HarmonyPatch(typeof(Skills), "CheatResetSkill")]
        public static class CheatResetSkillPatch
        {
            [HarmonyPrefix]
            public static bool Prefix(string name, Skills __instance, Player ___m_player)
            {
                foreach (var config in SkillManager.Instance.Skills)
                {
                    if (config.Value.Name.ToLower() == name.ToLower())
                    {
                        ___m_player.GetSkills().ResetSkill(config.Value.UID);
                        return false;
                    }
                }
                return true;
            }
        }
    }
}
