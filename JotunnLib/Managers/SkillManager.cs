using System;
using System.Collections.Generic;
using System.IO;
using BepInEx;
using UnityEngine;
using Jotunn.Utils;
using Jotunn.Configs;
using MonoMod.Cil;

namespace Jotunn.Managers
{
    /// <summary>
    ///     Handles all logic that has to do with skills, and adding custom skills.
    /// </summary>
    public class SkillManager : IManager
    {
        private static SkillManager _instance;
        /// <summary>
        ///     Global singleton instance of the manager.
        /// </summary>
        public static SkillManager Instance
        {
            get
            {
                if (_instance == null) _instance = new SkillManager();
                return _instance;
            }
        }

        public void Init()
        {

            IL.Skills.RaiseSkill += Skills_RaiseSkill;
            On.Skills.Awake += RegisterCustomSkills;
        }

        private void Skills_RaiseSkill(MonoMod.Cil.ILContext il)
        {
            ILCursor c = new ILCursor(il);
            c.GotoNext(MoveType.After,
                    zz => zz.MatchConstrained(out _),
                    zz => zz.MatchCallOrCallvirt<System.Object>("ToString"),
                    zz => zz.MatchCallOrCallvirt<System.String>("ToLower")
                );
            c.EmitDelegate<Func<string, string>>((string skillID) => {

            var asd = Enum.TryParse<global::Skills.SkillType>(skillID, out var result);

            if (asd && Skills.ContainsKey(result))
            {
                Jotunn.Logger.LogDebug($"Fixing Enum.ToString on {skillID}, match found: {Skills[result].Name}");
                return Skills[result].Name;
            }
            return skillID;
            });
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
                Logger.LogError($"Failed to register skill with invalid identifier: {skillConfig.Identifier}");
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

        private void RegisterCustomSkills(On.Skills.orig_Awake orig, Skills self)
        {
            orig(self);

            Logger.LogInfo($"---- Registering custom skills ----");

            foreach (var pair in Skills)
            {
                self.m_skills.Add(pair.Value.ToSkillDef());
                Logger.LogInfo($"Registered skill {pair.Value.Name} | ID: {pair.Value.Identifier}");
            }

        }

    }
}
