﻿using JotunnLib.Entities;
using System.Collections.Generic;
using UnityEngine;
using JotunnLib.Entities;

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

        internal Dictionary<Skills.SkillType, SkillConfig> Skills = new Dictionary<Skills.SkillType, SkillConfig>();
        // FIXME: Deprecate
        private int nextSkillId = 1000;

        public SkillManager()
        {
            if (Instance != null)
            {
                Logger.LogError("Error, two instances of singleton: " + this.GetType().Name);
                return;
            }

            Instance = this;
        }

        internal override void Clear()
        {
            Instance = null;
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
        public Skills.SkillType RegisterSkill(
            string name,
            string description,
            float increaseStep = 1f,
            Sprite icon = null,
            bool createLocalizations = true)
        {
            if (createLocalizations)
            {
                LocalizationManager.Instance.RegisterTranslation("skill_" + nextSkillId, name);
                LocalizationManager.Instance.RegisterTranslation("skill_" + nextSkillId + "_description", description);
            }

            SkillConfig skillConfig = new SkillConfig()
            {
                Identifier = name + nextSkillId.ToString(),
                Name = name,
                Description = description,
                IncreaseStep = increaseStep,
                Icon = icon
            };

            LocalizationManager.Instance.RegisterTranslation("skill_" + skillConfig.UID, skillConfig.Name);
            LocalizationManager.Instance.RegisterTranslation("skill_" + skillConfig.UID + "_description", skillConfig.Description);

            Skills.Add(skillConfig.UID, skillConfig);
            nextSkillId++;

            return skillConfig.UID;
        }

        /// <summary>
        /// Register a new skill with given SkillConfig object, and registers translations for it in the current localization
        /// </summary>
        /// <param name="skillConfig">SkillConfig object representing new skill to register</param>
        /// <returns>The SkillType of the newly registered skill</returns>
        public Skills.SkillType RegisterSkill(SkillConfig skillConfig, bool registerLocalizations = true)
        {
            if (string.IsNullOrEmpty(skillConfig.Identifier))
            {
                Logger.LogError("Failed to register skill with invalid identifier: " + skillConfig.Identifier);
                return global::Skills.SkillType.None;
            }

            Skills.Add(skillConfig.UID, skillConfig);

            if (registerLocalizations)
            {
                foreach (var translation in skillConfig.Localizations.Values)
                {
                    LocalizationManager.Instance.RegisterLocalizationConfig(translation);
                }
            }

            return skillConfig.UID;
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
        public Skills.SkillType RegisterSkill(
            string identifer,
            string name,
            string description,
            float increaseStep = 1f,
            Sprite icon = null,
            bool registerLocalizations = true)
        {
            return RegisterSkill(new SkillConfig()
            {
                Identifier = identifer,
                Name = name,
                Description = description,
                IncreaseStep = increaseStep,
                Icon = icon
            }, registerLocalizations);
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
                return new Skills.SkillDef()
                {
                    m_description = Skills[skillType].Description,
                    m_icon = Skills[skillType].Icon,
                    m_increseStep = Skills[skillType].IncreaseStep,
                    m_skill = Skills[skillType].UID
                };
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
            if (string.IsNullOrEmpty(identifier))
            {
                return null;
            }

            return GetSkill((Skills.SkillType)identifier.GetStableHashCode());
        }
    }
}
