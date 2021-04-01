using System.Collections.Generic;
using UnityEngine;

namespace JotunnLib.Entities
{
    /// <summary>
    /// Configuration class for custom skills
    /// </summary>
    public class SkillConfig
    {
        private string _identifier;
        public string Identifier
        {
            get { return this._identifier; }
            set
            {
                this._identifier = value;
                if (value.GetStableHashCode() > 1000)
                {
                    UID = (Skills.SkillType)value.GetStableHashCode();
                }
                else
                {
                    throw new System.Exception("This identifier may conflict with default Valheim skills, please choose a different (or longer) identifer");
                }
            }
        }
        public Skills.SkillType UID { get; private set; }
        public string Name { get; set; }
        public string Description { get; set; }
        public Sprite Icon { get; set; }
        public float IncreaseStep { get; set; }
        public Dictionary<string, LocalizationConfig> Localizations { get; private set; } = new Dictionary<string, LocalizationConfig>();

        /// <summary>
        /// Creates a SkillConfig object for mods that previously used SkillInjector
        /// </summary>
        /// <param name="identifier">Unique identifier of the new skill, ex: "com.jotunnlib.testmod.testskill"</param>
        /// <param name="uid">"id" from SkillInjector</param>
        /// <param name="name">"name" from SkillInjector</param>
        /// <param name="description">"description" from SkillInjector</param>
        /// <param name="increaseStep">"increment" from SkillInjector</param>
        /// <param name="icon">"icon" from SkillInjector</param>
        /// <returns>New SkillConfig object that bridges SkillInjector to JotunnLib without losing user progress</returns>
        /// <remarks>For any new skills please do not use this method!</remarks>
        public static SkillConfig FromSkillInjector(string identifier, Skills.SkillType uid, string name, string description, float increaseStep, Sprite icon)
        {
            return new SkillConfig()
            {
                Identifier = identifier,
                UID = uid, // Overrides hash UID
                Name = name,
                Description = description,
                Icon = icon,
                IncreaseStep = increaseStep
            };
        }

        // BaseSkill and JSON support targets v0.2.0
        //private Skills.SkillType BaseSkill { get; set; }
        //private static SkillConfig FromJson(string json)
        //{
        //    return null; // TODO: Make this work
        //}
    }
}
