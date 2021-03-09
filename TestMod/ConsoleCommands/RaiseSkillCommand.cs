using UnityEngine;
using JotunnLib;
using JotunnLib.Utils;

namespace TestMod.ConsoleCommands
{
    public class RaiseSkillCommand : ConsoleCommand
    {
        public override string Name => "raise_skill";

        public override string Help => "Raise a skill";

        public override void Run(string[] args)
        {
            if (args.Length != 2)
            {
                Console.instance.Print("Usage: raise_skill <skill> <amount>");
                return;
            }

            string name = args[0];
            int amount = int.Parse(args[1]);
            Skills skills = Player.m_localPlayer.GetSkills();

            foreach (Skills.SkillDef skillDef in skills.m_skills)
            {
                Debug.Log(skillDef.m_skill.ToString().ToLower());
                if (skillDef.m_skill.ToString().ToLower() == name)
                {
                    Skills.Skill skill = (Skills.Skill)ReflectionUtils.InvokePrivate(skills, "GetSkill", new object[] { skillDef.m_skill });
                    skill.m_level += amount;
                    skill.m_level = Mathf.Clamp(skill.m_level, 0.0f, 100f);
                    Player.m_localPlayer.Message(MessageHud.MessageType.TopLeft, "Skill incresed " + skill.m_info.m_skill.ToString() + ": " + (object)(int)skill.m_level, 0, skill.m_info.m_icon);
                    Console.instance.Print("Skill " + skillDef.m_skill.ToString() + " = " + skill.m_level.ToString());
                    return;
                }
            }

            Console.instance.Print("Skill not found " + name);
        }
    }
}
