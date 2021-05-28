using System;
using System.Collections.Generic;
using System.Linq;
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
        internal static Dictionary<string, ButtonConfig> Buttons = new Dictionary<string, ButtonConfig>();

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
        ///     Add a custom button binding via config
        /// </summary>
        /// <param name="modguid"></param>
        /// <param name="button"></param>
        public void AddButton(string modguid, ButtonConfig button)
        {
            if (Buttons.ContainsKey(button.Name + "!" + modguid))
            {
                Logger.LogError("Cannot have duplicate button: " + button.Name);
                return;
            }

            button.Name += "!" + modguid;

            Buttons.Add(button.Name, button);
        }

        /// <summary>
        ///     Add a custom button binding
        /// </summary>
        /// <param name="modguid"></param>
        /// <param name="name"></param>
        /// <param name="key"></param>
        /// <param name="repeatDelay"></param>
        /// <param name="repeatInterval"></param>
        [Obsolete("Use ButtonConfig instead")]
        public void AddButton(
            string modguid,
            string name,
            KeyCode key,
            float repeatDelay = 0.0f,
            float repeatInterval = 0.0f)
        {

            if (Buttons.ContainsKey(name + "!" + modguid))
            {
                Logger.LogError("Cannot have duplicate button: " + name);
                return;
            }

            Buttons.Add(name + "!" + modguid, new ButtonConfig()
            {
                Name = name + "!" + modguid,
                Key = key,
                RepeatDelay = repeatDelay,
                RepeatInterval = repeatInterval
            });
        }

        /// <summary>
        ///     Add a custom button binding
        /// </summary>
        /// <param name="modguid"></param>
        /// <param name="name"></param>
        /// <param name="axis"></param>
        /// <param name="inverted"></param>
        /// <param name="repeatDelay"></param>
        /// <param name="repeatInterval"></param>
        [Obsolete("Use ButtonConfig instead")]
        public void AddButton(
            string modguid,
            string name,
            string axis,
            bool inverted = false,
            float repeatDelay = 0.0f,
            float repeatInterval = 0.0f)
        {
            if (Buttons.ContainsKey(name + "!" + modguid))
            {
                Logger.LogError("Cannot have duplicate button: " + name);
                return;
            }

            Buttons.Add(name + "!" + modguid, new ButtonConfig()
            {
                Name = name + "!" + modguid,
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

                    if (btn.Axis != null && btn.Axis.Length > 0)
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
