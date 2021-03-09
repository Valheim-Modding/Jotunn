using System;
using System.Collections.Generic;
using UnityEngine;
using ValheimLokiLoader.Entities;

namespace ValheimLokiLoader.Managers
{
    public static class InputManager
    {
        public static EventHandler InputLoad;
        internal static Dictionary<string, ButtonConfig> Buttons = new Dictionary<string, ButtonConfig>();
        private static bool inputsLoaded = false;

        internal static void LoadInputs(ZInput zinput)
        {
            Debug.Log("---- Registering custom inputs ----");

            if (zinput == null)
            {
                Debug.LogError("\t-> ZInput does not exist yet, delaying...");
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

                Debug.Log("Registered input: " + pair.Key);
            }
        }

        internal static void RegisterInputs()
        {
            if (inputsLoaded)
            {
                return;
            }

            InputLoad?.Invoke(null, EventArgs.Empty);
            inputsLoaded = true;
        }

        public static void RegisterButton(
            string name,
            KeyCode key,
            float repeatDelay = 0.0f,
            float repeatInterval = 0.0f)
        {
            if (Buttons.ContainsKey(name))
            {
                Debug.LogError("Cannot have duplicate button: " + name);
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

        public static void RegisterButton(
            string name,
            string axis,
            bool inverted = false,
            float repeatDelay = 0.0f,
            float repeatInterval = 0.0f)
        {
            if (Buttons.ContainsKey(name))
            {
                Debug.LogError("Cannot have duplicate button: " + name);
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

        public static void RegisterButton(string name, ButtonConfig button)
        {
            if (Buttons.ContainsKey(name))
            {
                Debug.LogError("Cannot have duplicate button: " + name);
                return;
            }

            Buttons.Add(name, button);
        }
    }
}
