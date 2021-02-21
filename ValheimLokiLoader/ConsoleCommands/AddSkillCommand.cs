using System.Collections.Generic;
using UnityEngine;

namespace ValheimLokiLoader.ConsoleCommands
{
    public class AddSkillCommand : ConsoleCommand
    {
        public override string Name => "add_skill";

        public override string Help => "wow some nice help for it";

        public override void Run(string[] args)
        {
            Skills skills = Player.m_localPlayer.GetSkills();

            Skills.SkillType type = (Skills.SkillType)420;
            Skills.SkillDef skillDef = new Skills.SkillDef()
            {
                m_skill = type,
                m_description = "dank meme"
            };
            Skills.Skill skill = new Skills.Skill(skillDef);

            skills.m_skills.Add(skillDef);

            var skillData = Util.GetPrivateField<Dictionary<Skills.SkillType, Skills.Skill>>(skills, "m_skillData");
            skillData.Add(type, skill);
        }
    }
}
