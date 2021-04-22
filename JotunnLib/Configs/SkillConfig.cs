using System;
using System.Collections.Generic;
using UnityEngine;
using Jotunn.Utils;

namespace Jotunn.Configs
{
    /// <summary>
    ///     Configuration class for adding custom skills.
    /// </summary>
    public class SkillConfig
    {
        /// <summary>
        ///     A SkillType used to distinguish this skill from others. This is a unique ID that Jotunn generates
        ///     based on the Identifier provided.
        /// </summary>
        public Skills.SkillType UID { get; private set; }

        /// <summary>
        ///     A <b>unique</b> string used to identify the skill, and used to generate the <see cref="UID"/>.
        ///     <para>
        ///         <b>Do not</b> change the Identifier after you have released a mod using it.
        ///         If the Identifier changes, so will the skill's SkillType/UID, so
        ///         all users who have your mod will lose their save progress for the skill.
        ///     </para>
        /// </summary>
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

        /// <summary>
        ///     The in-game name for your skill.
        ///     Can either be the name you want to see in-game, or a localization token.
        /// </summary>
        public string Name { get; set; }

        /// <summary>
        ///     The in-game description for your skill.
        ///     Can either be the description you want to see in-game, or a localization token.
        /// </summary>
        public string Description { get; set; }
        
        /// <summary>
        ///     The in-game icon for your skill. If null, will default to a "shield" icon.
        /// </summary>
        public Sprite Icon { get; set; }

        /// <summary>
        ///     The multiplier applied to all XP gained for this skill via <see cref="Skills.RaiseSkill(Skills.SkillType, float)"/>.
        ///     If this is set to 0, your skill will be unable to gain XP at all.
        /// </summary>
        public float IncreaseStep { get; set; } = 1.0f;

        /// <summary>
        ///     The path to load an icon png/jpg file from.
        ///     If you wish to load from an asset bundle, use a <c>$</c> to separate the path to the asset bundle,
        ///     and your sprite name in the asset bundle
        ///     
        ///     <para>
        ///         This <b>cannot</b> be set if <see cref="Icon"/> is also set. You can only set one of them at once.
        ///     </para>
        ///     
        ///     <example>
        ///         This sample shows how you would load a sprite from an asset bundle:
        ///         <code>
        ///             IconPath = "MyMod/Assets/assetbundle$mysprite"
        ///         </code>
        ///     </example>
        /// </summary>
        public string IconPath
        {
            set
            {
                if (Icon != null)
                {
                    Logger.LogWarning($"Icon and IconPath both set for skill {Identifier}, ignoring IconPath");
                    return;
                }

                Icon = AssetUtils.LoadSprite(value);
            }
        }

        private string _identifier;


        /// <summary>
        ///     Converts the SkillConfig to a printable string.
        /// </summary>
        /// <returns>String representation of the SkillConfig</returns>
        public override string ToString()
        {
            return $"SkillConfig(Identifier='{Identifier}', UID={UID}, Name='{Name}')";
        }

        /// <summary>
        ///     Converts a Jotunn SkillConfig into a Valheim SkillDef.
        /// </summary>
        /// <returns>Valheim SkillDef representation of the SkillConfig</returns>
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

        internal bool IsFromName(string name)
        {
            return Name.ToLower() == name.ToLower() ||
                Identifier == name ||
                Localization.instance.Localize(Name).ToLower() == name.ToLower();
        }

        /// <summary>
        ///     Creates a SkillConfig object for mods that previously used SkillInjector.
        /// </summary>
        /// <param name="identifier">Unique identifier of the new skill, ex: "com.jotunn.testmod.testskill"</param>
        /// <param name="uid">"id" from SkillInjector</param>
        /// <param name="name">"name" from SkillInjector</param>
        /// <param name="description">"description" from SkillInjector</param>
        /// <param name="increaseStep">"increment" from SkillInjector</param>
        /// <param name="icon">"icon" from SkillInjector</param>
        /// <returns>New SkillConfig object that bridges SkillInjector to Jotunn without losing user progress</returns>
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
        ///     Loads a single SkillConfig from a JSON string.
        /// </summary>
        /// <param name="json">JSON text</param>
        /// <returns>Loaded SkillConfig</returns>
        public static SkillConfig FromJson(string json)
        {
            return SimpleJson.SimpleJson.DeserializeObject<SkillConfig>(json);
        }

        /// <summary>
        ///     Loads a list of SkillConfigs from a JSON string.
        /// </summary>
        /// <param name="json">JSON text</param>
        /// <returns>Loaded list of SkillConfigs</returns>
        public static List<SkillConfig> ListFromJson(string json)
        {
            return SimpleJson.SimpleJson.DeserializeObject<List<SkillConfig>>(json);
        }
    }
}
