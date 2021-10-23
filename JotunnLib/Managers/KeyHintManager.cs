using System;
using System.Collections.Generic;
using System.Linq;
using Jotunn.Configs;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using static Jotunn.Managers.InputManager;

namespace Jotunn.Managers
{
    internal class KeyHintManager : IManager
    {
        private static KeyHintManager _instance;
        public static KeyHintManager Instance
        {
            get
            {
                if (_instance == null) _instance = new KeyHintManager();
                return _instance;
            }
        }

        /// <summary>
        ///     Internal Dictionary holding the references to the custom key hints added to the manager.
        /// </summary>
        internal readonly Dictionary<string, KeyHintConfig> KeyHints = new Dictionary<string, KeyHintConfig>();

        /// <summary>
        ///     Reference to the games "KeyHint" GameObjects RectTransform.
        /// </summary>
        private RectTransform KeyHintContainer;

        public void Init()
        {
            // Dont init on a headless server
            if (!GUIManager.IsHeadless())
            {
                SceneManager.sceneLoaded += SceneManager_sceneLoaded;
                On.KeyHints.UpdateHints += ShowCustomKeyHint;
            }
        }

        private void SceneManager_sceneLoaded(Scene scene, LoadSceneMode loadMode)
        {
            if (scene.name == "main")
            {
                GameObject root = SceneManager.GetActiveScene().GetRootGameObjects().FirstOrDefault(x => x.name == "_GameMain");
                Transform gui = root?.transform.Find("GUI");
                if (!gui)
                {
                    Logger.LogWarning("_GameMain GUI not found, not creating custom GUI");
                    return;
                }

                // Get the KeyHints transform for this scene to create new KeyHint objects
                KeyHintContainer = (RectTransform)gui?.Find("PixelFix/IngameGui(Clone)/HUD/hudroot/KeyHints");

                // Create all custom KeyHints
                RegisterKeyHints();
            }
        }

        /// <summary>
        ///     Create custom KeyHint objects for every config added.
        /// </summary>
        private void RegisterKeyHints()
        {
            if (KeyHints.Count > 0)
            {
                Logger.LogInfo($"Adding {KeyHints.Count} custom key hints");

                List<string> toDelete = new List<string>();

                // Create hint objects for all configs
                foreach (var entry in KeyHints)
                {
                    try
                    {
                        CreateKeyHintObject(entry.Value);
                    }
                    catch (Exception ex)
                    {
                        Logger.LogWarning($"Exception caught while creating KeyHint for {entry.Key}: {ex}");
                        toDelete.Add(entry.Key);
                    }
                }

                // Delete key hints with errors
                foreach (string key in toDelete)
                {
                    KeyHints.Remove(key);
                }
            }
        }

        /// <summary>
        ///     Copy vanilla BuildHints object and create a custom one from a KeyHintConfig.
        /// </summary>
        /// <param name="config"></param>
        private GameObject CreateKeyHintObject(KeyHintConfig config)
        {
            // Clone BuildHints and add it under KeyHints to get the position right
            var keyHints = KeyHintContainer?.GetComponent<KeyHints>();

            if (keyHints == null)
            {
                throw new Exception("Could not find KeyHints component");
            }

            var baseKeyHint = UnityEngine.Object.Instantiate(keyHints.m_buildHints, KeyHintContainer, false);
            baseKeyHint.name = config.ToString();
            baseKeyHint.SetActive(false);

            // Get the Transforms of Keyboard and Gamepad
            var inputHint = baseKeyHint.GetComponent<UIInputHint>();
            var kb = inputHint?.m_mouseKeyboardHint?.transform;
            var gp = inputHint?.m_gamepadHint?.transform;

            if (kb == null || gp == null)
            {
                throw new Exception("Could not find child objects for KeyHints");
            }

            // Clone vanilla key hint objects and use it as the base for custom key hints
            var origKey = kb.transform.Find("Place")?.gameObject;
            var origRotate = kb.transform.Find("rotate")?.gameObject;
            var origButton = gp.transform.Find("BuildMenu")?.gameObject;
            var origTrigger = gp.transform.Find("Place")?.gameObject;
            var origShoulder = gp.transform.Find("Remove")?.gameObject;
            var origStick = gp.transform.Find("rotate")?.gameObject;

            if (!origKey || !origRotate || !origButton || !origTrigger || !origShoulder || !origStick)
            {
                throw new Exception("Could not find child objects for KeyHints");
            }

            var baseKey = UnityEngine.Object.Instantiate(origKey);
            var baseRotate = UnityEngine.Object.Instantiate(origRotate);
            var baseButton = UnityEngine.Object.Instantiate(origButton);
            var baseTrigger = UnityEngine.Object.Instantiate(origTrigger);
            var baseShoulder = UnityEngine.Object.Instantiate(origShoulder);
            var baseStick = UnityEngine.Object.Instantiate(origStick);
            UnityEngine.Object.DestroyImmediate(baseStick.transform.Find("Trigger").gameObject);
            UnityEngine.Object.DestroyImmediate(baseStick.transform.Find("plus").gameObject);

            // Destroy all child objects
            foreach (RectTransform child in kb)
            {
                UnityEngine.Object.Destroy(child.gameObject);
            }
            foreach (RectTransform child in gp)
            {
                UnityEngine.Object.Destroy(child.gameObject);
            }

            foreach (var buttonConfig in config.ButtonConfigs)
            {
                string key = ZInput.instance.GetBoundKeyString(buttonConfig.Name);
                if (key.Contains("MISSING"))
                {
                    key = buttonConfig.Name;
                }
                if (key[0].Equals(LocalizationManager.TokenFirstChar))
                {
                    key = LocalizationManager.Instance.TryTranslate(key);
                }
                string hint = LocalizationManager.Instance.TryTranslate(buttonConfig.HintToken);

                if (string.IsNullOrEmpty(buttonConfig.Axis) || !buttonConfig.Axis.Equals("Mouse ScrollWheel"))
                {
                    var customKeyboard = UnityEngine.Object.Instantiate(baseKey, kb, false);
                    customKeyboard.name = buttonConfig.Name;
                    customKeyboard.transform.Find("key_bkg/Key").gameObject.SetText(key);
                    customKeyboard.transform.Find("Text").gameObject.SetText(hint);
                    customKeyboard.SetActive(true);
                }
                else
                {
                    var customKeyboard = UnityEngine.Object.Instantiate(baseRotate, kb, false);
                    customKeyboard.transform.Find("Text").gameObject.SetText(hint);
                    customKeyboard.SetActive(true);
                }

                var gamepadButton = buttonConfig.GamepadButton;
                if (gamepadButton == InputManager.GamepadButton.None && ZInput.instance.m_buttons.TryGetValue($"Joy{buttonConfig.Name}", out var buttonDef))
                {
                    if (!string.IsNullOrEmpty(buttonDef.m_axis))
                    {
                        string invAxis = $"{(buttonDef.m_inverted ? "-" : null)}{buttonDef.m_axis}";
                        gamepadButton = GetGamepadButton(invAxis);
                    }
                    else
                    {
                        gamepadButton = GetGamepadButton(buttonDef.m_key);
                    }
                }

                if (gamepadButton != InputManager.GamepadButton.None)
                {
                    string buttonString = GetGamepadString(gamepadButton);

                    switch (gamepadButton)
                    {
                        case InputManager.GamepadButton.DPadUp:
                        case InputManager.GamepadButton.DPadDown:
                        case InputManager.GamepadButton.DPadLeft:
                        case InputManager.GamepadButton.DPadRight:
                        case InputManager.GamepadButton.StartButton:
                        case InputManager.GamepadButton.SelectButton:
                        case InputManager.GamepadButton.ButtonNorth:
                        case InputManager.GamepadButton.ButtonSouth:
                        case InputManager.GamepadButton.ButtonWest:
                        case InputManager.GamepadButton.ButtonEast:
                            var customButton = UnityEngine.Object.Instantiate(baseButton, gp, false);
                            customButton.name = buttonConfig.Name;
                            customButton.GetComponentInChildren<Text>().text = buttonString;
                            customButton.transform.Find("Text").gameObject.SetText(hint);
                            customButton.SetActive(true);
                            break;
                        case InputManager.GamepadButton.LeftShoulder:
                        case InputManager.GamepadButton.RightShoulder:
                            var customShoulder = UnityEngine.Object.Instantiate(baseShoulder, gp, false);
                            customShoulder.name = buttonConfig.Name;
                            customShoulder.GetComponentInChildren<Text>().text = buttonString;
                            customShoulder.transform.Find("Text").gameObject.SetText(hint);
                            customShoulder.SetActive(true);
                            break;
                        case InputManager.GamepadButton.LeftTrigger:
                        case InputManager.GamepadButton.RightTrigger:
                            var customTrigger = UnityEngine.Object.Instantiate(baseTrigger, gp, false);
                            customTrigger.name = buttonConfig.Name;
                            customTrigger.GetComponentInChildren<Text>().text = buttonString;
                            customTrigger.transform.Find("Text").gameObject.SetText(hint);
                            customTrigger.SetActive(true);
                            break;
                        case InputManager.GamepadButton.LeftStickButton:
                        case InputManager.GamepadButton.RightStickButton:
                            var customStick = UnityEngine.Object.Instantiate(baseStick, gp, false);
                            customStick.name = buttonConfig.Name;
                            customStick.GetComponentInChildren<Text>().text = buttonString;
                            customStick.transform.Find("Text").gameObject.SetText(hint);
                            customStick.SetActive(true);
                            break;
                        default:
                            throw new ArgumentOutOfRangeException();
                    }

                }
            }

            Logger.LogDebug($"Added key hints for Item : {config}");

            return baseKeyHint;
        }

        /// <summary>
        ///     Add a <see cref="KeyHintConfig"/> to the manager.<br />
        ///     Checks if the custom key hint is unique (i.e. the first one registered for an item).<br />
        ///     Custom status effects are displayed in the game instead of the default 
        ///     KeyHints for equipped tools or weapons they are registered for.
        /// </summary>
        /// <param name="hintConfig">The custom key hint config to add.</param>
        /// <returns>true if the custom key hint config was added to the manager.</returns>
        public bool AddKeyHint(KeyHintConfig hintConfig)
        {
            if (hintConfig.Item == null)
            {
                Logger.LogWarning($"Key hint config {hintConfig} is not valid");
                return false;
            }
            if (KeyHints.ContainsKey(hintConfig.ToString()))
            {
                Logger.LogWarning($"Key hint config for item {hintConfig} already added");
                return false;
            }

            KeyHints.Add(hintConfig.ToString(), hintConfig);
            return true;
        }

        /// <summary>
        ///     Removes a <see cref="KeyHintConfig"/> from the game.
        /// </summary>
        /// <param name="hintConfig">The custom key hint config to add.</param>
        public void RemoveKeyHint(KeyHintConfig hintConfig)
        {
            if (KeyHints.ContainsKey(hintConfig.ToString()))
            {
                KeyHints.Remove(hintConfig.ToString());
            }
            if (SceneManager.GetActiveScene().name == "main")
            {
                var keyHintObject = KeyHintContainer.Find(hintConfig.ToString())?.gameObject;
                if (keyHintObject)
                {
                    UnityEngine.Object.Destroy(keyHintObject);
                }
            }
        }

        /// <summary>
        ///     Hook on <see cref="global::KeyHints.UpdateHints" /> to show custom key hints instead of the vanilla ones.
        /// </summary>
        /// <param name="orig"></param>
        /// <param name="self"></param>
        private void ShowCustomKeyHint(On.KeyHints.orig_UpdateHints orig, KeyHints self)
        {
            orig(self);

            // Needs at least a localPlayer
            if (Player.m_localPlayer == null)
            {
                return;
            }

            // If something went wrong, dont NRE every Update
            if (KeyHintContainer == null)
            {
                return;
            }

            // First disable all custom key hints
            foreach (RectTransform transform in KeyHintContainer)
            {
                if (KeyHints.ContainsKey(transform.name))
                {
                    transform.gameObject?.SetActive(false);
                }
            }

            // Don't show hints when chat window is visible
            if (Chat.instance.IsChatDialogWindowVisible())
            {
                return;
            }

            // Get the current equipped item name
            ItemDrop.ItemData item = null;
            try
            {
                item = Player.m_localPlayer.GetInventory().GetEquipedtems().FirstOrDefault(x => x.IsWeapon() || x.m_shared.m_buildPieces != null);
            }
            catch (Exception)
            {
                // ignored
            }

            if (item == null)
            {
                return;
            }
            string prefabName = item.m_dropPrefab?.name;
            if (string.IsNullOrEmpty(prefabName))
            {
                return;
            }

            // Get the current selected piece name if any
            string pieceName = null;
            try
            {
                pieceName = Player.m_localPlayer.m_buildPieces.GetSelectedPiece()?.name;
            }
            catch (Exception)
            {
                // ignored
            }

            // Try to get a KeyHint for the item and piece selected or just the item without a piece
            KeyHintConfig hintConfig = null;
            if (!string.IsNullOrEmpty(pieceName))
            {
                KeyHints.TryGetValue($"{prefabName}:{pieceName}", out hintConfig);
            }
            if (hintConfig == null)
            {
                KeyHints.TryGetValue(prefabName, out hintConfig);
            }
            if (hintConfig == null)
            {
                return;
            }

            // Display the KeyHint instead the vanilla one or remove the config if it fails
            var hintObject = KeyHintContainer.Find(hintConfig.ToString())?.gameObject;
            if (!hintObject)
            {
                try
                {
                    hintObject = CreateKeyHintObject(hintConfig);
                }
                catch (Exception ex)
                {
                    Logger.LogWarning($"Exception caught while creating dynamic KeyHint {hintConfig}: {ex}");
                    KeyHints.Remove(hintConfig.ToString());
                    return;
                }
            }

            self.m_buildHints.SetActive(false);
            self.m_combatHints.SetActive(false);

            // Update bound keys
            foreach (var buttonConfig in hintConfig.ButtonConfigs)
            {
                string key = ZInput.instance.GetBoundKeyString(buttonConfig.Name);
                if (key.Contains("MISSING"))
                {
                    key = buttonConfig.Name;
                }
                if (key[0].Equals(LocalizationManager.TokenFirstChar))
                {
                    key = LocalizationManager.Instance.TryTranslate(key);
                }
                if (string.IsNullOrEmpty(buttonConfig.Axis) || !buttonConfig.Axis.Equals("Mouse ScrollWheel"))
                {
                    hintObject.transform.Find($"Keyboard/{buttonConfig.Name}/key_bkg/Key")?.gameObject?.SetText(key);
                }
            }

            hintObject.SetActive(true);
            hintObject.GetComponent<UIInputHint>()?.Update();
        }
    }
}
