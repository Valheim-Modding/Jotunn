using System;
using System.Collections.Generic;
using System.Linq;
using Jotunn.Configs;
using Jotunn.Utils;
using UnityEngine;

namespace Jotunn.Managers
{
    /// <summary>
    ///    Manager for handling custom skills added to the game.
    /// </summary>
    public class SkillManager : IManager
    {
        private static SkillManager _instance;
        /// <summary>
        ///     Global singleton instance of the manager.
        /// </summary>
        public static SkillManager Instance => _instance ??= new SkillManager();

        /// <summary>
        ///     Hide .ctor
        /// </summary>
        private SkillManager() { }

        /// <summary>
        ///     Initialize the manager
        /// </summary>
        public void Init()
        {
            On.Skills.Awake += RegisterCustomSkills;
            On.Skills.IsSkillValid += Skills_IsSkillValid;
            On.Skills.GetSkill += Skills_GetSkill;
            On.Skills.CheatRaiseSkill += Skills_CheatRaiseSkill;
            On.Skills.CheatResetSkill += Skills_CheatResetSkill;
        }

        internal Dictionary<Skills.SkillType, SkillConfig> Skills = new Dictionary<Skills.SkillType, SkillConfig>();

        /// <summary>
        ///     Add a new skill with given SkillConfig object.
        /// </summary>
        /// <param name="skillConfig">SkillConfig object representing new skill to register</param>
        /// <returns>The SkillType of the newly added skill</returns>
        public Skills.SkillType AddSkill(SkillConfig skillConfig)
        {
            if (string.IsNullOrEmpty(skillConfig?.Identifier))
            {
                Logger.LogError($"Failed to register skill with invalid identifier: {skillConfig?.Identifier}");
                return global::Skills.SkillType.None;
            }

            Skills.Add(skillConfig.UID, skillConfig);

            return skillConfig.UID;
        }

        /// <summary>
        ///     Register a new skill with given parameters, and registers translations for it in the current localization.
        /// </summary>
        /// <param name="identifer">Unique identifier of the new skill, ex: "com.jotunn.testmod.testskill"</param>
        /// <param name="name">Name of the new skill</param>
        /// <param name="description">Description of the new skill</param>
        /// <param name="increaseStep"></param>
        /// <param name="icon">Icon for the skill</param>
        /// <returns>The SkillType of the newly registered skill</returns>
        [Obsolete("Use AddSkill(SkillConfig) instead")]
        public Skills.SkillType AddSkill(
            string identifer,
            string name,
            string description,
            float increaseStep = 1f,
            Sprite icon = null)
        {
            return AddSkill(new SkillConfig
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
        public void AddSkillsFromJson(string path)
        {
            string json = AssetUtils.LoadText(path);

            if (string.IsNullOrEmpty(json))
            {
                Logger.LogError($"Failed to load skills from json: {path}");
                return;
            }

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

            return Player.m_localPlayer?.GetSkills()?.m_skills?
                .FirstOrDefault(skill => skill.m_skill == skillType);
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

            return GetSkill((Skills.SkillType)Math.Abs(identifier.GetStableHashCode()));
        }

        private void RegisterCustomSkills(On.Skills.orig_Awake orig, Skills self)
        {
            orig(self);

            if (Skills.Count > 0)
            {
                Logger.LogInfo($"Registering {Skills.Count} custom skills");

                foreach (var skill in Skills.Values)
                {
                    var localizedName = skill.Name.StartsWith("$") ? Localization.instance.Localize(skill.Name) : skill.Name;
                    Localization.instance.AddWord($"skill_{skill.UID}", localizedName);
                    self.m_skills.Add(skill.ToSkillDef());
                    Logger.LogDebug($"Registered skill {skill.Name} | ID: {skill.Identifier}");
                }
            }
        }

        private bool Skills_IsSkillValid(On.Skills.orig_IsSkillValid orig, Skills self, Skills.SkillType skillType)
        {
            var ret = orig(self, skillType);

            if (!ret && Skills.ContainsKey((Skills.SkillType)Math.Abs((int)skillType)))
            {
                ret = true;
            }

            return ret;
        }

        private Skills.Skill Skills_GetSkill(On.Skills.orig_GetSkill orig, Skills self, Skills.SkillType skillType)
        {
            // Fix the mess of whoever decided to have negative skill IDs and implement several workarounds...
            var abs = (Skills.SkillType)Math.Abs((int)skillType);

            if ((int)skillType < 0 && Skills.ContainsKey(abs))
            {
                return orig(self, abs);
            }

            return orig(self, skillType);
        }

        private void Skills_CheatRaiseSkill(On.Skills.orig_CheatRaiseSkill orig, Skills self, string name, float value)
        {
            foreach (var config in Skills.Values)
            {
                if (config.IsFromName(name))
                {
                    Skills.Skill skill = self.GetSkill(config.UID);
                    var localizedName = config.Name.StartsWith("$") ? Localization.instance.Localize(config.Name) : config.Name;

                    skill.m_level += value;
                    skill.m_level = Mathf.Clamp(skill.m_level, 0f, 100f);
                    self.m_player.Message(MessageHud.MessageType.TopLeft,
                        $"Skill increased {localizedName}: {(int)skill.m_level}", 0, skill.m_info.m_icon);
                    Console.instance.Print($"Skill {localizedName} = {skill.m_level}");
                    Logger.LogDebug($"Raised skill {localizedName} to {skill.m_level}");

                    return;
                }
            }

            orig(self, name, value);
        }

        private void Skills_CheatResetSkill(On.Skills.orig_CheatResetSkill orig, Skills self, string name)
        {
            foreach (var config in Skills.Values)
            {
                if (config.IsFromName(name))
                {
                    self.m_player.GetSkills().ResetSkill(config.UID);
                    Logger.LogDebug($"Reset skill {config.Name}");
                    return;
                }
            }

            orig(self, name);
        }
    }
}
