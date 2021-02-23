using System.Collections.Generic;
using UnityEngine;

namespace ValheimLokiLoader.Managers
{
    public static class SkillManager
    {
        public static List<Skills.SkillDef> Skills = new List<Skills.SkillDef>();
        private static int nextSkillId = 1000;

        public static Skills.SkillDef AddSkill(string name, string description, float increaseStep = 1f, Sprite icon = null)
        {
            LocalizationManager.AddTranslation("skill_" + nextSkillId, name);
            LocalizationManager.AddTranslation("skill_" + nextSkillId + "_description", description);

            Skills.SkillDef skillDef = new Skills.SkillDef()
            {
                m_skill = (Skills.SkillType)nextSkillId,
                m_description = "skill_" + nextSkillId + "_description",
                m_increseStep = increaseStep, // nice they spelled increase wrong 
                m_icon = icon
            };

            Skills.Add(skillDef);
            nextSkillId++;

            return skillDef;
        }
    }
}
