using JotunnLib.Managers;
using JotunnLib.Utils;
using UnityEngine;

namespace JotunnLib.Patches
{
    class SkillsPatches 
    {
        [PatchInit(0)]
        public static void Init()
        {
            On.Skills.Awake += Skills_Awake;
            On.Skills.IsSkillValid += Skills_IsSkillValid;
            On.Skills.CheatRaiseSkill += Skills_CheatRaiseSkill;
            On.Skills.CheatResetSkill += Skills_CheatResetSkill;
        }

        private static void Skills_CheatResetSkill(On.Skills.orig_CheatResetSkill orig, Skills self, string name)
        {
            foreach (var config in SkillManager.Instance.Skills)
            {
                if (config.Value.Name.ToLower() == name.ToLower())
                {
                    self.m_player.GetSkills().ResetSkill(config.Value.UID);
                    return;
                }
            }

            orig(self, name);
        }

        private static void Skills_CheatRaiseSkill(On.Skills.orig_CheatRaiseSkill orig, Skills self, string name, float value)
        {
            foreach (var config in SkillManager.Instance.Skills)
            {
                if (config.Value.Name.ToLower() == name.ToLower())
                {
                    Skills.Skill skill = self.GetSkill(config.Value.UID);

                    skill.m_level += value;
                    skill.m_level = Mathf.Clamp(skill.m_level, 0f, 100f);
                    self.m_player.Message(MessageHud.MessageType.TopLeft, "Skill increased " + config.Value.Name + ": " + (int)skill.m_level, 0, skill.m_info.m_icon);
                    Console.instance.Print("Skill " + config.Value.Name + " = " + skill.m_level);
                    return;
                }
            }

            orig(self, name, value);
        }

        private static bool Skills_IsSkillValid(On.Skills.orig_IsSkillValid orig, Skills self, Skills.SkillType type)
        {
            foreach (var pair in SkillManager.Instance.Skills)
            {
                if (pair.Value.UID == type)
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
                // TODO: Quaesar pls fix me!
                // Skills.SkillDef skill = pair.Value;
                // self.m_skills.Add(skill);
                // System.Console.WriteLine("Added extra skill: " + skill.m_skill);
            }

        }
    }
}
