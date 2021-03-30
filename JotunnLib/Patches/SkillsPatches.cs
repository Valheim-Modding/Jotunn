using System.Collections.Generic;
using JotunnLib.Managers;
using JotunnLib.Utils;

namespace JotunnLib.Patches
{
    class SkillsPatches : PatchInitializer
    {
        internal override void Init()
        {
            On.Skills.Awake += Skills_Awake;
            On.Skills.IsSkillValid += Skills_IsSkillValid;
        }

        private static bool Skills_IsSkillValid(On.Skills.orig_IsSkillValid orig, Skills self, Skills.SkillType type)
        {
            foreach (var pair in SkillManager.Instance.Skills)
            {
                if (pair.Value.m_skill == type)
                {
                     return true;
                }
            }

            return orig(self, type);
        }

        private static void Skills_Awake(On.Skills.orig_Awake orig, Skills self)
        {
            orig(self);

            // TODO: Move into SkillsManager Register
            foreach (var pair in SkillManager.Instance.Skills)
            {
                Skills.SkillDef skill = pair.Value;
                self.m_skills.Add(skill);
                System.Console.WriteLine("Added extra skill: " + skill.m_skill);
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
