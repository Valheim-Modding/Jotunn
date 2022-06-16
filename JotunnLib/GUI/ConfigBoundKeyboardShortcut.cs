using System.Collections.Generic;
using System.Linq;
using BepInEx.Configuration;
using UnityEngine;
using UnityEngine.UI;

namespace Jotunn.GUI
{
    /// <summary>
    ///     KeyboardShortcut binding
    /// </summary>
    internal class ConfigBoundKeyboardShortcut : ConfigBound<KeyboardShortcut>
    {
        private static readonly IEnumerable<KeyCode> KeysToCheck = KeyboardShortcut.AllKeyCodes.Except(new[] { KeyCode.Mouse0, KeyCode.None }).ToArray();

        private Text Text;

        public override void Register()
        {
            Config.Button.gameObject.SetActive(true);
            Text = Config.Button.transform.Find("Text").GetComponent<Text>();
        }

        public override KeyboardShortcut GetValue()
        {
            if (Text.text == KeyboardShortcut.Empty.ToString())
            {
                return KeyboardShortcut.Empty;
            }

            return KeyboardShortcut.Deserialize(Text.text);
        }

        public override void SetValue(KeyboardShortcut value)
        {
            Text.text = value.ToString();
        }

        public void Start()
        {
            var buttonName = Entry.GetBoundButtonName();
            Config.Button.onClick.AddListener(() =>
            {
                InGameConfig.SettingsRoot.GetComponent<ModSettings>().OpenBindDialog(buttonName, KeyBindCheck);
            });
        }

        private bool KeyBindCheck()
        {
            foreach (var key in KeysToCheck)
            {
                if (Input.GetKeyUp(key))
                {
                    SetValue(new KeyboardShortcut(key, KeysToCheck.Where(Input.GetKey).ToArray()));
                    if (ZInput.m_binding != null)
                    {
                        ZInput.m_binding.m_key = key;
                    }

                    return true;
                }
            }

            return false;
        }

        public override void SetEnabled(bool enabled)
        {
            Config.Button.enabled = enabled;
        }

        public override void SetReadOnly(bool readOnly)
        {
            Config.Button.interactable = !readOnly;
            Text.color = readOnly ? Color.grey : Color.white;
        }
    }
}
