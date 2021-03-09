using System;
using System.Collections.Generic;
using UnityEngine;
using JotunnLib.Entities;

namespace JotunnLib.Managers
{
    public class InputManager : Manager
    {
        public static InputManager Instance { get; private set; }

        public EventHandler InputRegister;
        internal static Dictionary<string, ButtonConfig> Buttons = new Dictionary<string, ButtonConfig>();
        private bool inputsRegistered = false;

        private void Awake()
        {
            if (Instance != null)
            {
                Debug.LogError("Error, two instances of singleton: " + this.GetType().Name);
                return;
            }

            Instance = this;
        }

        internal void Load(ZInput zinput)
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
                Debug.LogError("Cannot have duplicate button: " + name);
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

        public void RegisterButton(
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
    }
}
