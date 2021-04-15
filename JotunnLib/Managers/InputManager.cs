using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using JotunnLib.Configs;

namespace JotunnLib.Managers
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

        public EventHandler InputRegister;
        private bool inputsRegistered = false;


        public void Init()
        {
            On.ZInput.Initialize += ZInput_Initialize;
            On.ZInput.Reset += ZInput_Reset;
            On.ZInput.GetButtonDown += ZInput_GetButtonDown;
            On.ZInput.GetButtonUp += ZInput_GetButtonUp;
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

        internal void Load(ZInput zinput)
        {
            Logger.LogInfo("---- Registering custom inputs ----");

            if (zinput == null)
            {
                Logger.LogError("\t-> ZInput does not exist yet, delaying...");
                return;
            }

            foreach (var pair in Buttons)
            {
                var btn = pair.Value;

                if (btn.Axis != null && btn.Axis.Length > 0)
                {
                    zinput.AddButton(btn.Name, btn.Axis, btn.Inverted, btn.RepeatDelay, btn.RepeatInterval);
                }
                else
                {
                    zinput.AddButton(btn.Name, btn.Key, btn.RepeatDelay, btn.RepeatInterval);
                }

                Logger.LogInfo("Registered input: " + pair.Key);
            }
        }

        internal void Register()
        {
            if (inputsRegistered)
            {
                return;
            }

            InputRegister?.Invoke(null, EventArgs.Empty);
            inputsRegistered = true;
        }

        public void AddButton(string name, ButtonConfig button)
        {
            if (Buttons.ContainsKey(name))
            {
                Logger.LogError("Cannot have duplicate button: " + name);
                return;
            }

            Buttons.Add(name, button);
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


        private void ZInput_Reset(On.ZInput.orig_Reset orig, ZInput self)
        {
            orig(self);
            Logger.LogDebug("----> ZInput Reset");
            Load(self);
        }

        private void ZInput_Initialize(On.ZInput.orig_Initialize orig)
        {
            Logger.LogDebug("----> ZInput Initialize");
            Register();
            orig();
        }

    }
}
