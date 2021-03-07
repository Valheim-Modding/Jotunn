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

        internal static void LoadInputs()
        {
            Debug.Log("---- Registering custom inputs ----");

            foreach (var pair in Buttons)
            {
                var btn = pair.Value;
                if (btn.Axis.Length > 0)
                {
                    ZInput.instance.AddButton(btn.Name, btn.Axis, btn.Inverted, btn.RepeatDelay, btn.RepeatInterval);
                }
                else
                {
                    ZInput.instance.AddButton(btn.Name, btn.Key, btn.RepeatDelay, btn.RepeatInterval);
                }
                Debug.Log("Registered input: " + pair.Key);
            }
        }

        public static void AddButton(
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

        public static void AddButton(
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

        public static void AddButton(string name, ButtonConfig button)
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
