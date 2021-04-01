using System;
using System.Collections.Generic;
using UnityEngine;
using JotunnLib.Utils;

namespace JotunnLib.Configs
{
    /// <summary>
    ///     Configuration class for adding custom skills.
    /// </summary>
    public class SkillConfig
    {
        public string Identifier
        {
            get { return _identifier; }
            set
            {
                int hashCode = value.GetStableHashCode();
                if (hashCode < 0 || hashCode > 1000)
                {
                    _identifier = value;
                    UID = (Skills.SkillType)value.GetStableHashCode();
                }
                else
                {
                    throw new Exception("This identifier may conflict with default Valheim skills, please choose a different (or longer) identifer");
                }
            }
        }
        public Skills.SkillType UID { get; private set; }
        public string Name { get; set; }
        public string Description { get; set; }
        public Sprite Icon { get; set; }
        public float IncreaseStep { get; set; }
        public Dictionary<string, LocalizationConfig> Localizations { get; private set; } = new Dictionary<string, LocalizationConfig>();

        public string IconPath
        {
            set
            {
                if (Icon != null)
                {
                    JotunnLib.Logger.LogWarning($"Icon and IconPath both set for skill {Identifier}, ignoring IconPath");
                    return;
                }

                Icon = AssetUtils.LoadSprite(value);
            }
        }

        private string _identifier;

        public override string ToString()
        {
            return $"SkillConfig(Identifier='{Identifier}', UID={UID}, Name='{Name}')";
        }

        /// <summary>
        ///     Converts a JotunnLib SkillConfig into a Valheim SkillDef
        /// </summary>
        /// <returns>Valheim SkillDef</returns>
        public Skills.SkillDef ToSkillDef()
        {
            return new Skills.SkillDef()
            {
                m_description = Description,
                m_icon = Icon,
                m_increseStep = IncreaseStep,
                m_skill = UID
            };
        }

        /// <summary>
        ///     Creates a SkillConfig object for mods that previously used SkillInjector
        /// </summary>
        /// <param name="identifier">Unique identifier of the new skill, ex: "com.jotunnlib.testmod.testskill"</param>
        /// <param name="uid">"id" from SkillInjector</param>
        /// <param name="name">"name" from SkillInjector</param>
        /// <param name="description">"description" from SkillInjector</param>
        /// <param name="increaseStep">"increment" from SkillInjector</param>
        /// <param name="icon">"icon" from SkillInjector</param>
        /// <returns>New SkillConfig object that bridges SkillInjector to JotunnLib without losing user progress</returns>
        /// <remarks>For any new skills please do not use this method!</remarks>
        [Obsolete("This is kept for easy compatibility with SkillInjector. Use other ways of registering skills if possible to avoid conflicts with other mods")]
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

        /// <summary>
        ///     Loads a single SkillConfig from a JSON string
        /// </summary>
        /// <param name="json">JSON text</param>
        /// <returns>Loaded SkillConfigs</returns>
        public static SkillConfig FromJson(string json)
        {
            return SimpleJson.SimpleJson.DeserializeObject<SkillConfig>(json);
        }

        /// <summary>
        ///     Loads a list of SkillConfigs from a JSON string
        /// </summary>
        /// <param name="json">JSON text</param>
        /// <returns>Loaded list of SkillConfigs</returns>
        public static List<SkillConfig> ListFromJson(string json)
        {
            return SimpleJson.SimpleJson.DeserializeObject<List<SkillConfig>>(json);
        }
    }
}
