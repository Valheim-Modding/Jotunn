using System.Collections.Generic;
using UnityEngine;

namespace JotunnLib.Managers
{
    /// <summary>
    /// Handles all logic that has to do with skills, and registering custom skills
    /// </summary>
    public class SkillManager : Manager
    {
        /// <summary>
        /// Global singleton instance of the manager
        /// </summary>
        public static SkillManager Instance { get; private set; }

        internal Dictionary<Skills.SkillType, Skills.SkillDef> Skills = new Dictionary<Skills.SkillType, Skills.SkillDef>();
        // FIXME: Deprecate
        private int nextSkillId = 1000;
        private void Awake()
        {
            if (Instance != null)
            {
                Debug.LogError("Error, two instances of singleton: " + this.GetType().Name);
                return;
            }

            Instance = this;
        }

        /// <summary>
        /// Configuration class for custom skills
        /// </summary>
        public class SkillConfig
        {
            public string Identifier
            {
                get { return Identifier; }
                set
                {
                    Identifier = value;
                    UID = value.GetStableHashCode();
                }
            }
            public int UID { get; private set; }
            public string Name { get; set; }
            public string Description { get; set; }
            public Sprite Icon { get; set; }
            public float IncreaseStep { get; set; }

            // BaseSkill and JSON support targets v0.2.0
            //private Skills.SkillType BaseSkill { get; set; }
            //private static SkillConfig FromJson(string json)
            //{
            //    return null; // TODO: Make this work
            //}
        }

        /// <summary>
        /// DEPRECATED DUE TO POSSIBLE CONFLICT ISSUE, see: <see href="https://github.com/jotunnlib/jotunnlib/issues/18">GitHub Issue</see>
        /// <para/>
        /// Register a new skill with given parameters, and registers translations for it in the current localization
        /// </summary>
        /// <param name="name">Name of the new skill</param>
        /// <param name="description">Description of the new skill</param>
        /// <param name="increaseStep"></param>
        /// <param name="icon">Icon for the skill</param>
        /// <returns>The SkillType of the newly registered skill</returns>
        [System.Obsolete("Use `RegisterSkill(SkillConfig config)` instead. This method could potentially break user saves.", true)]
        public Skills.SkillType RegisterSkill(string name, string description, float increaseStep = 1f, Sprite icon = null)
        {
            LocalizationManager.Instance.RegisterTranslation("skill_" + nextSkillId, name);
            LocalizationManager.Instance.RegisterTranslation("skill_" + nextSkillId + "_description", description);

            Skills.SkillDef skillDef = new Skills.SkillDef()
            {
                m_skill = (Skills.SkillType)nextSkillId,
                m_description = "$skill_" + nextSkillId + "_description",
                m_increseStep = increaseStep, // nice they spelled increase wrong 
                m_icon = icon
            };

            Skills.Add((Skills.SkillType)nextSkillId, skillDef);
            nextSkillId++;

            return skillDef.m_skill;
        }

        /// <summary>
        /// Register a new skill with given SkillConfig object, and registers translations for it in the current localization
        /// </summary>
        /// <param name="skillConfig">SkillConfig object representing new skill to register</param>
        /// <returns>The SkillType of the newly registered skill</returns>
        public Skills.SkillType RegisterSkill(SkillConfig skillConfig)
        {
            LocalizationManager.Instance.RegisterTranslation("skill_" + skillConfig.UID, skillConfig.Name);
            LocalizationManager.Instance.RegisterTranslation("skill_" + skillConfig.UID + "_description", skillConfig.Description);

            Skills.SkillDef skillDef = new Skills.SkillDef()
            {
                m_skill = (Skills.SkillType)skillConfig.UID,
                m_description = "$skill_" + skillConfig.UID + "_description",
                m_increseStep = skillConfig.IncreaseStep,
                m_icon = skillConfig.Icon
            };

            Skills.Add((Skills.SkillType)skillConfig.UID, skillDef);
            return skillDef.m_skill;
        }

        /// <summary>
        /// Register a new skill with given parameters, and registers translations for it in the current localization
        /// </summary>
        /// <param name="identifer">Unique identifier of the new skill, ex: "com.jotunnlib.testmod.testskill"</param>
        /// <param name="name">Name of the new skill</param>
        /// <param name="description">Description of the new skill</param>
        /// <param name="increaseStep"></param>
        /// <param name="icon">Icon for the skill</param>
        /// <returns>The SkillType of the newly registered skill</returns>
        public Skills.SkillType RegisterSkill(string identifer, string name, string description, float increaseStep = 1f, Sprite icon = null)
        {
            return RegisterSkill(new SkillConfig()
            {
                Identifier = identifer,
                Name = name,
                Description = description,
                IncreaseStep = increaseStep,
                Icon = icon
            });
        }

        /// <summary>
        /// Gets a custom skill with given SkillType
        /// </summary>
        /// <param name="skillType">SkillType to look for</param>
        /// <returns>Custom skill with given SkillType</returns>
        public Skills.SkillDef GetSkill(Skills.SkillType skillType)
        {
            if (Skills.ContainsKey(skillType))
            {
                return Skills[skillType];
            }

            if (Player.m_localPlayer != null)
            {
                if (Player.m_localPlayer.GetSkills() != null)
                {
                    foreach (Skills.SkillDef skill in Player.m_localPlayer.GetSkills().m_skills)
                    {
                        if (skill.m_skill == skillType)
                        {
                            return skill;
                        }
                    }
                }
            }

            return null;
        }

        /// <summary>
        /// Gets a custom skill with given skill identifier
        /// </summary>
        /// <param name="identifier">String indentifer of SkillType to look for</param>
        /// <returns>Custom skill with given SkillType</returns>
        public Skills.SkillDef GetSkill(string identifier)
        {
            return GetSkill((Skills.SkillType)identifier.GetStableHashCode());
        }
    }
}
