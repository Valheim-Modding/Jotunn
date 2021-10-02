using System;
using System.Collections.Generic;
using System.Linq;
using BepInEx.Configuration;
using Jotunn.Configs;
using UnityEngine;

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

        internal static Dictionary<ConfigEntryBase, ButtonConfig> ButtonToConfigDict = new Dictionary<ConfigEntryBase, ButtonConfig>();

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
            // Dont init on a dedicated server
            if (!GUIManager.IsHeadless())
            {
                On.ZInput.Load += RegisterCustomInputs;
                On.ZInput.GetButtonDown += ZInput_GetButtonDown;
                On.ZInput.GetButton += ZInput_GetButton;
                On.ZInput.GetButtonUp += ZInput_GetButtonUp;
            }
        }

        /// <summary>
        ///     Add a Button to Valheim
        /// </summary>
        /// <param name="modGuid">Mod GUID</param>
        /// <param name="buttonConfig">Button config</param>
        public void AddButton(string modGuid, ButtonConfig buttonConfig)
        {
            if (buttonConfig == null)
            {
                throw new ArgumentNullException(nameof(buttonConfig));
            }

            if (string.IsNullOrEmpty(modGuid))
            {
                throw new ArgumentException($"{nameof(modGuid)} can not be empty or null", nameof(modGuid));
            }

            if (buttonConfig.Config == null && buttonConfig.Key == KeyCode.None && 
                buttonConfig.ShortcutConfig == null && buttonConfig.Shortcut.MainKey == KeyCode.None &&
                string.IsNullOrEmpty(buttonConfig.Axis))
            {
                throw new ArgumentException($"{nameof(buttonConfig)} needs either Axis, Key, Shortcut or a Config set.", nameof(buttonConfig));
            }

            if (Buttons.ContainsKey(buttonConfig.Name + "!" + modGuid))
            {
                Logger.LogWarning($"Cannot have duplicate button: {buttonConfig.Name} (Mod {modGuid})");
                return;
            }
            
            if (buttonConfig.Key != KeyCode.None && buttonConfig.Shortcut.MainKey != KeyCode.None)
            {
                Logger.LogWarning($"Cannot have a Key and Shortcut in button config {buttonConfig.Name} (Mod {modGuid})");
                return;
            }

            if (buttonConfig.Config != null && buttonConfig.ShortcutConfig != null)
            {
                Logger.LogWarning($"Cannot have a Key and Shortcut config in button config {buttonConfig.Name} (Mod {modGuid})");
                return;
            }

            if (buttonConfig.Config != null)
            {
                ButtonToConfigDict.Add(buttonConfig.Config, buttonConfig);
            }

            if (buttonConfig.ShortcutConfig != null)
            {
                ButtonToConfigDict.Add(buttonConfig.ShortcutConfig, buttonConfig);
            }

            buttonConfig.Name += "!" + modGuid;
            Buttons.Add(buttonConfig.Name, buttonConfig);
        }

        private void RegisterCustomInputs(On.ZInput.orig_Load orig, ZInput self)
        {
            orig(self);

            if (Buttons.Any())
            {
                Logger.LogInfo($"Registering {Buttons.Count} custom inputs");

                foreach (var pair in Buttons)
                {
                    var btn = pair.Value;
                    
                    if (!string.IsNullOrEmpty(btn.Axis))
                    {
                        self.AddButton(btn.Name, btn.Axis, btn.Inverted, btn.RepeatDelay, btn.RepeatInterval);
                    }
                    else if (btn.Config != null)
                    {
                        self.AddButton(btn.Name, btn.Config.Value, btn.RepeatDelay, btn.RepeatInterval);
                    }
                    else if (btn.Key != KeyCode.None)
                    {
                        self.AddButton(btn.Name, btn.Key, btn.RepeatDelay, btn.RepeatInterval);
                    }
                    else if (btn.ShortcutConfig != null)
                    {
                        self.AddButton(btn.Name, btn.ShortcutConfig.Value.MainKey, btn.RepeatDelay, btn.RepeatInterval);
                    }
                    else if (btn.Shortcut.MainKey != KeyCode.None)
                    {
                        self.AddButton(btn.Name, btn.Shortcut.MainKey, btn.RepeatDelay, btn.RepeatInterval);
                    }

                    Logger.LogDebug($"Registered input {pair.Key}");
                }
            }
        }
        
        private bool ZInput_GetButtonDown(On.ZInput.orig_GetButtonDown orig, string name)
        {
            if (!orig(name))
            {
                return false;
            }
            
            if (!Buttons.TryGetValue(name, out var button))
            {
                return true;
            }
            
            if (button.Shortcut.MainKey != KeyCode.None && !button.Shortcut.IsDown())
            {
                return false;
            }
            
            return TakeInput(button);
        }

        private bool ZInput_GetButton(On.ZInput.orig_GetButton orig, string name)
        {
            if (!orig(name))
            {
                return false;
            }
            
            if (!Buttons.TryGetValue(name, out var button))
            {
                return true;
            }
            
            if (button.Shortcut.MainKey != KeyCode.None && !button.Shortcut.IsPressed())
            {
                return false;
            }
            
            return TakeInput(button);
        }

        private bool ZInput_GetButtonUp(On.ZInput.orig_GetButtonUp orig, string name)
        {
            if (!orig(name))
            {
                return false;
            }

            if (!Buttons.TryGetValue(name, out var button))
            {
                return true;
            }
            
            if (button.Shortcut.MainKey != KeyCode.None && !button.Shortcut.IsUp())
            {
                return false;
            }
            
            return TakeInput(button);
        }

        private bool TakeInput(ButtonConfig button)
        {
            if (Player.m_localPlayer == null)
            {
                return true;
            }
            
            if (button.ActiveInGUI && !GUIManager.InputBlocked)
            {
                return true;
            }

            if (button.ActiveInCustomGUI && GUIManager.InputBlocked)
            {
                return true;
            }

            return Player.m_localPlayer.TakeInput();
        }
    }
}
