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
        /// Register a new skill with given parameters, and registers translations for it in the current localization
        /// </summary>
        /// <param name="name">Name of the new skill</param>
        /// <param name="description">Description of the new skill</param>
        /// <param name="increaseStep"></param>
        /// <param name="icon">Icon for the skill</param>
        /// <returns>The SkillType of the newly registered skill</returns>
        public Skills.SkillType RegisterSkill(string name, string description, float increaseStep = 1f, Sprite icon = null)
        {
            LocalizationManager.Instance.RegisterTranslation("skill_" + nextSkillId, name);
            LocalizationManager.Instance.RegisterTranslation("skill_" + nextSkillId + "_description", description);

            Skills.SkillDef skillDef = new Skills.SkillDef()
            {
                m_skill = (Skills.SkillType)nextSkillId,
                m_description = "skill_" + nextSkillId + "_description",
                m_increseStep = increaseStep, // nice they spelled increase wrong 
                m_icon = icon
            };

            Skills.Add((Skills.SkillType)nextSkillId, skillDef);
            nextSkillId++;

            return skillDef.m_skill;
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

            return null;
        }
    }
}
