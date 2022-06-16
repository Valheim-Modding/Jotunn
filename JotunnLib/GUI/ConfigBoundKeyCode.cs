using System;
using UnityEngine;
using UnityEngine.UI;

namespace Jotunn.GUI
{
    /// <summary>
    ///     KeyCode binding
    /// </summary>
    internal class ConfigBoundKeyCode : ConfigBound<KeyCode>
    {
        private Text Text;

        public override void Register()
        {
            Config.Button.gameObject.SetActive(true);
            Text = Config.Button.transform.Find("Text").GetComponent<Text>();
        }

        public override KeyCode GetValue()
        {
            if (Enum.TryParse(Text.text, out KeyCode temp))
            {
                return temp;
            }

            Logger.LogError($"Error parsing Keycode {Text.text}");
            return KeyCode.None;
        }

        public override void SetValue(KeyCode value)
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
            foreach (KeyCode key in Enum.GetValues(typeof(KeyCode)))
            {
                if (Input.GetKeyDown(key))
                {
                    SetValue(key);
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
