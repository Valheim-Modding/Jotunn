using System.Collections.Generic;
using JotunnLib.Managers;
using JotunnLib.Utils;
using UnityEngine;
using UnityEngine.UI;

namespace JotunnLib.Patches
{
    internal class SkillsPatches 
    {
        [PatchInit(0)]
        public static void Init()
        {
            On.Skills.IsSkillValid += Skills_IsSkillValid;
            On.Skills.CheatRaiseSkill += Skills_CheatRaiseSkill;
            On.Skills.CheatResetSkill += Skills_CheatResetSkill;
            On.SkillsDialog.Setup += SkillsDialog_Setup;
        }

        private static void Skills_CheatResetSkill(On.Skills.orig_CheatResetSkill orig, Skills self, string name)
        {
            foreach (var config in SkillManager.Instance.Skills.Values)
            {
                if (config.IsFromName(name))
                {
                    self.m_player.GetSkills().ResetSkill(config.UID);
                    return;
                }
            }

            orig(self, name);
        }

        private static void Skills_CheatRaiseSkill(On.Skills.orig_CheatRaiseSkill orig, Skills self, string name, float value)
        {
            foreach (var config in SkillManager.Instance.Skills.Values)
            {
                if (config.IsFromName(name))
                {
                    Skills.Skill skill = self.GetSkill(config.UID);
                    var localizedName = config.Name.StartsWith("$") ? Localization.instance.Translate(config.Name) : config.Name;

                    skill.m_level += value;
                    skill.m_level = Mathf.Clamp(skill.m_level, 0f, 100f);
                    self.m_player.Message(MessageHud.MessageType.TopLeft, "Skill increased " + localizedName + ": " + (int)skill.m_level, 0, skill.m_info.m_icon);
                    Console.instance.Print("Skill " + config.Name + " = " + skill.m_level);

                    return;
                }
            }

            orig(self, name, value);
        }

        private static bool Skills_IsSkillValid(On.Skills.orig_IsSkillValid orig, Skills self, Skills.SkillType type)
        {
            if (SkillManager.Instance.Skills.ContainsKey(type))
            {
                return true;
            }

            return orig(self, type);
        }

        private static void SkillsDialog_Setup(On.SkillsDialog.orig_Setup orig, SkillsDialog self, Player player)
        {
            orig(self, player);

            // Update skill names to allow for negative m_skill IDs
            List<Skills.Skill> skillList = player.GetSkills().GetSkillList();

            for (int i = 0; i < skillList.Count; i++)
            {
                var skill = skillList[i];
                var elem = self.m_elements[i];

                // Ignore vanilla skills
                if (!SkillManager.Instance.Skills.ContainsKey(skill.m_info.m_skill))
                {
                    continue;
                }

                var skillConfig = SkillManager.Instance.Skills[skill.m_info.m_skill];
                var name = skillConfig.Name.StartsWith("$") ? Localization.instance.Localize(skillConfig.Name) : skillConfig.Name;
                Logger.LogInfo($"Updated skill: {skillConfig.Name} -> {name}");
                global::Utils.FindChild(elem.transform, "name").GetComponent<Text>().text = name;
            }
        }
    }
}
