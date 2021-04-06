using System;
using System.Collections.Generic;
using UnityEngine;
using JotunnLib.Configs;

namespace JotunnLib.Managers
{
    public class InputManager : Manager
    {
        public static InputManager Instance { get; private set; }
        internal static Dictionary<string, ButtonConfig> Buttons = new Dictionary<string, ButtonConfig>();

        public EventHandler InputRegister;
        private bool inputsRegistered = false;

        private void Awake()
        {
            if (Instance != null)
            {
                Logger.LogError($"Cannot have multiple instances of singleton: {GetType().Name}");
                return;
            }

            Instance = this;
        }

        internal override void Init()
        {
            On.ZInput.Initialize += ZInput_Initialize;
            On.ZInput.Reset += ZInput_Reset;
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

        internal override void Register()
        {
            if (inputsRegistered)
            {
                return;
            }

            InputRegister?.Invoke(null, EventArgs.Empty);
            inputsRegistered = true;
        }

        public void RegisterButton(string name, ButtonConfig button)
        {
            if (Buttons.ContainsKey(name))
            {
                Logger.LogError("Cannot have duplicate button: " + name);
                return;
            }

            Buttons.Add(name, button);
        }

        public void RegisterButton(
            string name,
            KeyCode key,
            float repeatDelay = 0.0f,
            float repeatInterval = 0.0f)
        {
            if (Buttons.ContainsKey(name))
            {
                Logger.LogError("Cannot have duplicate button: " + name);
                return;
            }

            Buttons.Add(name, new ButtonConfig()
            {
                Name = name,
                Key = key,
                RepeatDelay = repeatDelay,
                RepeatInterval = repeatInterval
            });
        }

        public void RegisterButton(
            string name,
            string axis,
            bool inverted = false,
            float repeatDelay = 0.0f,
            float repeatInterval = 0.0f)
        {
            if (Buttons.ContainsKey(name))
            {
                Logger.LogError("Cannot have duplicate button: " + name);
                return;
            }

            Buttons.Add(name, new ButtonConfig()
            {
                Name = name,
                Axis = axis,
                Inverted = inverted,
                RepeatDelay = repeatDelay,
                RepeatInterval = repeatInterval
            });
        }


        private static void ZInput_Reset(On.ZInput.orig_Reset orig, ZInput self)
        {
            orig(self);
            Logger.LogDebug("----> ZInput Reset");
            InputManager.Instance.Load(self);
        }

        private static void ZInput_Initialize(On.ZInput.orig_Initialize orig)
        {
            Logger.LogDebug("----> ZInput Initialize");
            InputManager.Instance.Register();
            
            orig();
        }

    }
}
