using System;
using System.Collections.Generic;
using System.Linq;
using Jotunn.Configs;
using UnityEngine;
using UnityEngine.UI;
using static Jotunn.Managers.InputManager;
using Object = UnityEngine.Object;

namespace Jotunn.Managers
{
    public class KeyHintManager : IManager
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
        ///     Internal Dictionary holding the references to the custom key hints added to the manager
        /// </summary>
        private readonly Dictionary<string, KeyHintConfig> KeyHints = new Dictionary<string, KeyHintConfig>();

        /// <summary>
        ///     Internal Dictionary holding the references to the key hint GameObjects created per KeyHintConfig
        /// </summary>
        private readonly Dictionary<string, GameObject> KeyHintObjects = new Dictionary<string, GameObject>();

        /// <summary>
        ///     Reference to the current "KeyHints" instance
        /// </summary>
        private KeyHints KeyHintInstance;

        /// <summary>
        ///     Reference to the games "KeyHint" GameObjects RectTransform
        /// </summary>
        private RectTransform KeyHintContainer;

        /// <summary>
        ///     Base GameObjects of vanilla key hint parts
        /// </summary>
        private GameObject BaseKey;
        private GameObject BaseRotate;
        private GameObject BaseButton;
        private GameObject BaseTrigger;
        private GameObject BaseShoulder;
        private GameObject BaseStick;
        private GameObject BaseDPad;

        /// <summary>
        ///     Initialize the manager
        /// </summary>
        public void Init()
        {
            // Dont init on a headless server
            if (!GUIManager.IsHeadless())
            {
                On.KeyHints.Start += KeyHints_Start;
                On.KeyHints.UpdateHints += KeyHints_UpdateHints;
            }
        }

        /// <summary>
        ///     Add a <see cref="KeyHintConfig"/> to the manager.<br />
        ///     Checks if the custom key hint is unique (i.e. the first one registered for an item).<br />
        ///     Custom key hints are displayed in the game instead of the default 
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

            // Register events for every ConfigEntry backed ButtonConfig to mark the KeyHint dirty
            foreach (var buttonConfig in hintConfig.ButtonConfigs.Where(x => x.IsConfigBacked))
            {
                if (buttonConfig.Config != null)
                {
                    buttonConfig.Config.SettingChanged += (sender, args) => hintConfig.Dirty = true;
                }

                if (buttonConfig.ShortcutConfig != null)
                {
                    buttonConfig.ShortcutConfig.SettingChanged += (sender, args) => hintConfig.Dirty = true;
                }

                if (buttonConfig.GamepadConfig != null)
                {
                    buttonConfig.GamepadConfig.SettingChanged += (sender, args) => hintConfig.Dirty = true;
                }
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

            if (KeyHintObjects.TryGetValue(hintConfig.ToString(), out var hintObject))
            {
                Object.Destroy(hintObject);
                KeyHintObjects.Remove(hintConfig.ToString());
            }
        }

        /// <summary>
        ///     Extract base key hint elements and create key hint objects.
        /// </summary>
        private void KeyHints_Start(On.KeyHints.orig_Start orig, KeyHints self)
        {
            try
            {
                orig(self);

                KeyHintInstance = self;
                KeyHintContainer = self.transform as RectTransform;

                GetBaseGameObjects();
            }
            catch (Exception ex)
            {
                Logger.LogWarning($"Exception caught while creating key hint objects: {ex}");
            }
        }

        /// <summary>
        ///     Instantiate base GameObjects from vanilla KeyHints to use in our custom key hints
        /// </summary>
        private void GetBaseGameObjects()
        {
            var baseKeyHint = KeyHintInstance.m_buildHints;

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

            BaseKey = Object.Instantiate(origKey);
            BaseRotate = Object.Instantiate(origRotate);
            BaseButton = Object.Instantiate(origButton);
            BaseTrigger = Object.Instantiate(origTrigger);
            BaseShoulder = Object.Instantiate(origShoulder);
            BaseStick = Object.Instantiate(origStick);
            Object.DestroyImmediate(BaseStick.transform.Find("Trigger").gameObject);
            Object.DestroyImmediate(BaseStick.transform.Find("plus").gameObject);
            BaseDPad = Object.Instantiate(BaseTrigger);
            BaseDPad.transform.Find("Trigger").GetComponent<RectTransform>().pivot = new Vector2(0.5f, 0.5f);
            BaseDPad.transform.Find("Trigger").GetComponent<RectTransform>().sizeDelta = new Vector2(25f, 25f);
        }

        /// <summary>
        ///     Copy vanilla BuildHints object and create a custom one from a KeyHintConfig.
        /// </summary>
        /// <param name="config"></param>
        private GameObject CreateKeyHintObject(KeyHintConfig config)
        {
            // Clone BuildHints and add it under KeyHints to get the position right
            var baseKeyHint = Object.Instantiate(KeyHintInstance.m_buildHints, KeyHintContainer, false);
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

            // Destroy all child objects
            foreach (Transform child in kb)
            {
                Object.Destroy(child.gameObject);
            }
            foreach (Transform child in gp)
            {
                Object.Destroy(child.gameObject);
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
                    var customKeyboard = Object.Instantiate(BaseKey, kb, false);
                    customKeyboard.name = buttonConfig.Name;
                    customKeyboard.transform.Find("key_bkg/Key").gameObject.SetText(key);
                    customKeyboard.transform.Find("Text").gameObject.SetText(hint);
                    customKeyboard.SetActive(true);
                }
                else
                {
                    var customKeyboard = Object.Instantiate(BaseRotate, kb, false);
                    customKeyboard.transform.Find("Text").gameObject.SetText(hint);
                    customKeyboard.SetActive(true);
                }

                var gamepadButton = buttonConfig.GamepadButton;
                if (gamepadButton == GamepadButton.None &&
                    ZInput.instance.m_buttons.TryGetValue($"Joy{buttonConfig.Name}", out var buttonDef))
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

                if (gamepadButton != GamepadButton.None)
                {
                    string buttonString = GetGamepadString(gamepadButton);

                    switch (gamepadButton)
                    {
                        case GamepadButton.DPadLeft:
                        case GamepadButton.DPadRight:
                            var customPadNoRotate = Object.Instantiate(BaseDPad, gp, false);
                            customPadNoRotate.name = buttonConfig.Name;
                            customPadNoRotate.GetComponentInChildren<Text>().text = buttonString;
                            customPadNoRotate.transform.Find("Text").gameObject.SetText(hint);
                            customPadNoRotate.SetActive(true);
                            break;
                        case GamepadButton.DPadUp:
                        case GamepadButton.DPadDown:
                            var customPadRotate = Object.Instantiate(BaseDPad, gp, false);
                            customPadRotate.name = buttonConfig.Name;
                            customPadRotate.transform.Find("Trigger").GetComponent<RectTransform>().Rotate(new Vector3(0, 0, 1f), 90f);
                            customPadRotate.GetComponentInChildren<Text>().text = buttonString;
                            customPadRotate.transform.Find("Text").gameObject.SetText(hint);
                            customPadRotate.SetActive(true);
                            break;
                        case GamepadButton.StartButton:
                        case GamepadButton.SelectButton:
                            var customPad = Object.Instantiate(BaseKey, gp, false);
                            customPad.name = buttonConfig.Name;
                            customPad.GetComponentInChildren<Text>().text = buttonString;
                            customPad.transform.Find("Text").gameObject.SetText(hint);
                            customPad.SetActive(true);
                            break;
                        case GamepadButton.ButtonNorth:
                        case GamepadButton.ButtonSouth:
                        case GamepadButton.ButtonWest:
                        case GamepadButton.ButtonEast:
                            var customButton = Object.Instantiate(BaseButton, gp, false);
                            customButton.name = buttonConfig.Name;
                            customButton.GetComponentInChildren<Text>().text = buttonString;
                            customButton.transform.Find("Text").gameObject.SetText(hint);
                            customButton.SetActive(true);
                            break;
                        case GamepadButton.LeftShoulder:
                        case GamepadButton.RightShoulder:
                            var customShoulder = Object.Instantiate(BaseShoulder, gp, false);
                            customShoulder.name = buttonConfig.Name;
                            customShoulder.GetComponentInChildren<Text>().text = buttonString;
                            customShoulder.transform.Find("Text").gameObject.SetText(hint);
                            customShoulder.SetActive(true);
                            break;
                        case GamepadButton.LeftTrigger:
                        case GamepadButton.RightTrigger:
                            var customTrigger = Object.Instantiate(BaseTrigger, gp, false);
                            customTrigger.name = buttonConfig.Name;
                            customTrigger.GetComponentInChildren<Text>().text = buttonString;
                            customTrigger.transform.Find("Text").gameObject.SetText(hint);
                            customTrigger.SetActive(true);
                            break;
                        case GamepadButton.LeftStickButton:
                        case GamepadButton.RightStickButton:
                            var customStick = Object.Instantiate(BaseStick, gp, false);
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

            KeyHintObjects[config.ToString()] = baseKeyHint;
            config.Dirty = false;
            return baseKeyHint;
        }

        /// <summary>
        ///     Hook on <see cref="global::KeyHints.UpdateHints" /> to show custom key hints instead of the vanilla ones.
        /// </summary>
        /// <param name="orig"></param>
        /// <param name="self"></param>
        private void KeyHints_UpdateHints(On.KeyHints.orig_UpdateHints orig, KeyHints self)
        {
            orig(self);

            // Needs at least a localPlayer
            if (Player.m_localPlayer == null)
            {
                return;
            }

            // If something went wrong, dont NRE every Update
            if (KeyHintInstance == null || KeyHintContainer == null)
            {
                return;
            }

            // First disable all custom key hints
            foreach (var obj in KeyHintObjects.Values)
            {
                obj.SetActive(false);
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

            // Try to get the hint object, if the keyhint is "dirty" (i.e. some config backed button changed), destroy the hint object
            if (KeyHintObjects.TryGetValue(hintConfig.ToString(), out var hintObject) && hintConfig.Dirty)
            {
                Object.DestroyImmediate(hintObject);
            }

            // Display the KeyHint instead the vanilla one or remove the config if it fails
            if (!hintObject)
            {
                try
                {
                    hintObject = CreateKeyHintObject(hintConfig);
                }
                catch (Exception ex)
                {
                    Logger.LogWarning($"Exception caught while creating KeyHint {hintConfig}: {ex}");
                    KeyHints.Remove(hintConfig.ToString());
                    return;
                }
            }

            // Update bound key strings if not backed by a ConfigEntry
            foreach (var buttonConfig in hintConfig.ButtonConfigs.Where(x => !x.IsConfigBacked))
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

            self.m_buildHints.SetActive(false);
            self.m_combatHints.SetActive(false);

            hintObject.SetActive(true);
            hintObject.GetComponent<UIInputHint>()?.Update();
        }
    }
}
