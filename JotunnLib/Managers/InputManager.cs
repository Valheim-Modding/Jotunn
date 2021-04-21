using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Jotunn.Configs;

namespace Jotunn.Managers
{
    public class InputManager : IManager
    {
        private static InputManager _instance;
        public static InputManager Instance
        {
            get
            {
                if (_instance == null) _instance = new InputManager();
                return _instance;
            }
        }
        internal static Dictionary<string, ButtonConfig> Buttons = new Dictionary<string, ButtonConfig>();


        public void Init()
        {
            On.ZInput.Reset += RegisterCustomInputs;
            On.ZInput.GetButtonDown += ZInput_GetButtonDown;
            On.ZInput.GetButtonUp += ZInput_GetButtonUp;
        }

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
