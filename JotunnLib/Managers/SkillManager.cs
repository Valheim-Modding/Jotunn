using JotunnLib.Configs;
using System.Collections.Generic;
using System.IO;
using BepInEx;
using UnityEngine;

namespace JotunnLib.Managers
{
    /// <summary>
    ///     Handles all logic that has to do with skills, and adding custom skills.
    /// </summary>
    public class SkillManager : Manager
    {
        /// <summary>
        ///     Global singleton instance of the manager.
        /// </summary>
        public static SkillManager Instance { get; private set; }

        internal Dictionary<Skills.SkillType, SkillConfig> Skills = new Dictionary<Skills.SkillType, SkillConfig>();
        
        // FIXME: Deprecate
        private int nextSkillId = 1000;
        private void Awake()
        {
            if (Instance != null)
            {
                Logger.LogError($"Cannot have multiple instances of singleton: {GetType().Name}");
                return;
            }

            Instance = this;
        }

        /// <summary>
        ///     DEPRECATED DUE TO POSSIBLE CONFLICT ISSUE, see: <see href="https://github.com/jotunnlib/jotunnlib/issues/18">GitHub Issue</see>.
        ///     <para/>
        ///     Add a new skill with given parameters, and adds translations for it in the current localization.
        /// </summary>
        /// <param name="name">Name of the new skill</param>
        /// <param name="description">Description of the new skill</param>
        /// <param name="increaseStep"></param>
        /// <param name="icon">Icon for the skill</param>
        /// <returns>The SkillType of the newly registered skill</returns>
        [System.Obsolete("Use `AddSkill(SkillConfig config)` instead. This method could potentially break user saves.", true)]
        public Skills.SkillType AddSkill(
            string name,
            string description,
            float increaseStep = 1f,
            Sprite icon = null,
            bool autoLocalize = true)
        {
            SkillConfig skillConfig = new SkillConfig()
            {
                Identifier = name + nextSkillId.ToString(),
                Name = name,
                Description = description,
                IncreaseStep = increaseStep,
                Icon = icon
            };

            if (autoLocalize)
            {
                LocalizationManager.Instance.AddLocalization("English", new Dictionary<string, string>()
                {
                    { "skill_" + skillConfig.UID, skillConfig.Name },
                    { "skill_" + skillConfig.UID + "_description", skillConfig.Description }
                });
            }

            Skills.Add(skillConfig.UID, skillConfig);
            nextSkillId++;

            return skillConfig.UID;
        }

        /// <summary>
        ///     Add a new skill with given SkillConfig object, and adds translations for it in the current localization.
        /// </summary>
        /// <param name="skillConfig">SkillConfig object representing new skill to register</param>
        /// <returns>The SkillType of the newly added skill</returns>
        public Skills.SkillType AddSkill(SkillConfig skillConfig)
        {
            if (string.IsNullOrEmpty(skillConfig?.Identifier))
            {
                Logger.LogError($"Failed to register skill with invalid identifier: {skillConfig.Identifier}");
                return global::Skills.SkillType.None;
            }

            Skills.Add(skillConfig.UID, skillConfig);
            Logger.LogInfo($"Added skill: {skillConfig}");
            
            return skillConfig.UID;
        }

        /// <summary>
        ///     Register a new skill with given parameters, and registers translations for it in the current localization.
        /// </summary>
        /// <param name="identifer">Unique identifier of the new skill, ex: "com.jotunnlib.testmod.testskill"</param>
        /// <param name="name">Name of the new skill</param>
        /// <param name="description">Description of the new skill</param>
        /// <param name="increaseStep"></param>
        /// <param name="icon">Icon for the skill</param>
        /// <param name="autoLocalize">Automatically generate English localizations for the given name and description</param>
        /// <returns>The SkillType of the newly registered skill</returns>
        public Skills.SkillType AddSkill(
            string identifer,
            string name,
            string description,
            float increaseStep = 1f,
            Sprite icon = null)
        {
            return AddSkill(new SkillConfig()
            {
                Identifier = identifer,
                Name = name,
                Description = description,
                IncreaseStep = increaseStep,
                Icon = icon
            });
        }

        /// <summary>
        ///     Adds skills defined in a JSON file at given path, relative to BepInEx/plugins
        /// </summary>
        /// <param name="path">JSON file path, relative to BepInEx/plugins folder</param>
        public void AddSkillFromJson(string path)
        {
            string absPath = Path.Combine(Paths.PluginPath, path);

            if (!File.Exists(absPath))
            {
                Logger.LogError($"Error, failed to register skill from non-existant path: ${absPath}");
                return;
            }

            string json = File.ReadAllText(absPath);
            List<SkillConfig> skills = SkillConfig.ListFromJson(json);

            foreach (SkillConfig skill in skills)
            {
                AddSkill(skill);
            }
        }

        /// <summary>
        ///     Gets a custom skill with given SkillType.
        /// </summary>
        /// <param name="skillType">SkillType to look for</param>
        /// <returns>Custom skill with given SkillType</returns>
        public Skills.SkillDef GetSkill(Skills.SkillType skillType)
        {
            if (Skills.ContainsKey(skillType))
            {
                return Skills[skillType].ToSkillDef();
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
        ///     Gets a custom skill with given skill identifier.
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
