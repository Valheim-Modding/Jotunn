using System;
using System.Collections.Generic;
using System.Linq;
using BepInEx.Configuration;
using UnityEngine;
using Jotunn.Configs;

namespace Jotunn.Managers
{
    /// <summary>
    ///    Manager for handling custom inputs registered by mods.
    /// </summary>
    public class InputManager : IManager
    {
        private static InputManager _instance;
        
        // Internal holder for all buttons added via Jotunn
        internal static Dictionary<string, ButtonConfig> Buttons = new Dictionary<string, ButtonConfig>();
        
        /// <summary>
        ///     Singleton instance
        /// </summary>
        public static InputManager Instance
        {
            get
            {
                if (_instance == null) _instance = new InputManager();
                return _instance;
            }
        }

        /// <summary>
        ///     Initialize the manager
        /// </summary>
        public void Init()
        {
            On.ZInput.Reset += RegisterCustomInputs;
            On.ZInput.GetButtonDown += ZInput_GetButtonDown;
            On.ZInput.GetButtonUp += ZInput_GetButtonUp;
        }

        /// <summary>
        /// Add Button to Valheim
        /// </summary>
        /// <param name="modGuid">Mod GUID</param>
        /// <param name="entry">KeyCode configuration entry</param>
        public void AddButton(string modGuid, ConfigEntry<KeyCode> entry)
        {
            ButtonConfig buttonConfig = entry.Description.Tags.FirstOrDefault(x => x is ButtonConfig) as ButtonConfig;
            if (buttonConfig == null)
            {
                throw new Exception($"Configuration entry {entry.Definition.Section}.{entry.Definition.Key} needs to have a ButtonConfig Tag.");
            }

            // Read value from config
            buttonConfig.Key = entry.Value ;
            buttonConfig.Name += "!" + modGuid;
            Buttons.Add(buttonConfig.Name, buttonConfig);
        }

        /// <summary>
        /// Add Button to Valheim
        /// </summary>
        /// <param name="modGuid">Mod GUID</param>
        /// <param name="button">Button config</param>
        public void AddButton(string modGuid, ButtonConfig button)
        {
            if (Buttons.ContainsKey(button.Name + "!" + modGuid))
            {
                Logger.LogError($"Cannot have duplicate button: {button.Name} (Mod {modGuid})");
                return;
            }

            button.Name += "!" + modGuid;

            Buttons.Add(button.Name, button);
        }

        /// <summary>
        /// Add Button to Valheim
        /// </summary>
        /// <param name="modGuid">Mod GUID</param>
        /// <param name="name">Name</param>
        /// <param name="key">KeyCode</param>
        /// <param name="repeatDelay">Repeat delay</param>
        /// <param name="repeatInterval">Repeat interval</param>
        [Obsolete("Use ButtonConfig instead")]
        public void AddButton(
            string modGuid,
            string name,
            KeyCode key,
            float repeatDelay = 0.0f,
            float repeatInterval = 0.0f)
        {

            if (Buttons.ContainsKey(name + "!" + modGuid))
            {
                Logger.LogError($"Cannot have duplicate button: {name} (Mod {modGuid})");
                return;
            }

            Buttons.Add(name + "!" + modGuid, new ButtonConfig()
            {
                Name = name + "!" + modGuid,
                Key = key,
                RepeatDelay = repeatDelay,
                RepeatInterval = repeatInterval
            });
        }

        /// <summary>
        /// Add button to Valheim
        /// </summary>
        /// <param name="modGuid">Mod GUID</param>
        /// <param name="name">Name</param>
        /// <param name="axis">Axis</param>
        /// <param name="inverted">Is axis inverted</param>
        /// <param name="repeatDelay">Repeat delay</param>
        /// <param name="repeatInterval">Repeat interval</param>
        [Obsolete("Use ButtonConfig instead")]
        public void AddButton(
            string modGuid,
            string name,
            string axis,
            bool inverted = false,
            float repeatDelay = 0.0f,
            float repeatInterval = 0.0f)
        {
            if (Buttons.ContainsKey(name + "!" + modGuid))
            {
                Logger.LogError("Cannot have duplicate button: " + name);
                return;
            }

            Buttons.Add(name + "!" + modGuid, new ButtonConfig()
            {
                Name = name + "!" + modGuid,
                Axis = axis,
                Inverted = inverted,
                RepeatDelay = repeatDelay,
                RepeatInterval = repeatInterval
            });
        }

        private bool ZInput_GetButtonUp(On.ZInput.orig_GetButtonUp orig, string name)
        {
            var result = orig(name);
            if (!result)
            {
                foreach (var buttonDef in ZInput.instance.m_buttons.Where(x => x.Key.StartsWith(name + "!")))
                {
                    if (Time.inFixedTimeStep ? buttonDef.Value.m_upFixed : buttonDef.Value.m_up)
                    {
                        return true;
                    }
                }
            }

            return result;
        }

        private bool ZInput_GetButtonDown(On.ZInput.orig_GetButtonDown orig, string name)
        {
            var result = orig(name);
            if (!result)
            {
                foreach (var def in ZInput.instance.m_buttons.Where(x => x.Key.StartsWith(name + "!")))
                {
                    if (Time.inFixedTimeStep ? def.Value.m_downFixed : def.Value.m_down)
                    {
                        return true;
                    }
                }
            }

            return result;
        }

        private void RegisterCustomInputs(On.ZInput.orig_Reset orig, ZInput self)
        {
            orig(self);

            if (Buttons.Count > 0)
            {
                Logger.LogInfo("---- Registering custom inputs ----");

                foreach (var pair in Buttons)
                {
                    var btn = pair.Value;

                    if (!string.IsNullOrEmpty(btn.Axis))
                    {
                        self.AddButton(btn.Name, btn.Axis, btn.Inverted, btn.RepeatDelay, btn.RepeatInterval);
                    }
                    else
                    {
                        self.AddButton(btn.Name, btn.Key, btn.RepeatDelay, btn.RepeatInterval);
                    }

                    Logger.LogInfo($"Registered input {pair.Key}");
                }
            }
        }
    }
}
