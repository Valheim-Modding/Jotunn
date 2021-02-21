using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace ValheimLokiLoader
{
    public static class SkillManager
    {
        public static List<Skills.SkillDef> Skills = new List<Skills.SkillDef>();
        private static int nextId = 1000;

        public static void AddSkill(string name, string description)
        {
            LocalizationManager.AddTranslation("skill_" + nextId, name);

            Skills.Add(new Skills.SkillDef()
            {
                m_skill = (Skills.SkillType)nextId,
                m_description = description
            });

            nextId++;

            /*
            int id = 1000;
            Skills.SkillType type = (Skills.SkillType)420;
            Skills.SkillDef skillDef = new Skills.SkillDef()
            {
                m_skill = type,
                m_description = "dank meme"
            };
            Skills.Skill skill = new Skills.Skill(skillDef);

            Console.instance.Print("Added skill: " + skillDef.m_skill.ToString().ToLower());

            Skills skills = Player.m_localPlayer.GetSkills();
            skills.m_skills.Add(skillDef);

            var skillData = Util.GetPrivateField<Dictionary<Skills.SkillType, Skills.Skill>>(skills, "m_skillData");
            skillData.Add(type, skill);

            LocalizationManager.AddTranslation("skill_" + id, description);
            */
        }
    }
}
