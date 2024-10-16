// Adjusted code from BepInEx.ConfigurationManager:
// https://github.com/BepInEx/BepInEx.ConfigurationManager/blob/master/ConfigurationManager.Shared/Utilities/ComboBox.cs

using System;
using System.Reflection;
using HarmonyLib;
using UnityEngine;

namespace Jotunn.Settings
{
    internal class ComboBox
    {
        private static FieldInfo forceToUnShow;
        private static FieldInfo useControlID;

        private bool isClickedComboButton;
        private readonly GUIContent[] listContent;
        private readonly GUIStyle listStyle;
        private readonly GUIStyle dropDownStyle;
        private readonly int _windowYmax;

        private static MethodInfo DrawContolBackground { get; }

        private static PropertyInfo CurrentDropdownDrawer { get; }

        static ComboBox()
        {
            var imageUtilsType = AccessTools.TypeByName("ConfigurationManager.Utilities.ImguiUtils, ConfigurationManager");
            DrawContolBackground = AccessTools.Method(imageUtilsType, "DrawContolBackground", new Type[] { typeof(Rect), typeof(Color) });

            var bepComboBoxType = AccessTools.TypeByName("ConfigurationManager.Utilities.ComboBox, ConfigurationManager");
            forceToUnShow = AccessTools.Field(bepComboBoxType, "forceToUnShow");
            useControlID = AccessTools.Field(bepComboBoxType, "useControlID");
            CurrentDropdownDrawer = AccessTools.Property(bepComboBoxType, "CurrentDropdownDrawer");
        }

        public ComboBox(Rect rect, GUIContent buttonContent, GUIContent[] listContent, GUIStyle listStyle, GUIStyle dropDownStyle, float windowYmax)
        {
            Rect = rect;
            ButtonContent = buttonContent;
            this.listContent = listContent;
            this.listStyle = listStyle;
            this.dropDownStyle = dropDownStyle;
            _windowYmax = (int)windowYmax;
        }

        public Rect Rect { get; set; }

        public GUIContent ButtonContent { get; set; }

        public void Show(Action<int> onItemSelected)
        {
            if ((bool)forceToUnShow.GetValue(null))
            {
                forceToUnShow.SetValue(null, false);
                isClickedComboButton = false;
            }

            var done = false;
            var controlID = GUIUtility.GetControlID(FocusType.Passive);

            Vector2 currentMousePosition = Vector2.zero;
            if (Event.current.GetTypeForControl(controlID) == EventType.MouseUp)
            {
                if (isClickedComboButton)
                {
                    done = true;
                    currentMousePosition = Event.current.mousePosition;
                }
            }

            if (UnityEngine.GUI.Button(Rect, ButtonContent, dropDownStyle))
            {
                if ((int)useControlID.GetValue(null) == -1)
                {
                    useControlID.SetValue(null, controlID);
                    isClickedComboButton = false;
                }

                if ((int)useControlID.GetValue(null) != controlID)
                {
                    forceToUnShow.SetValue(null, true);
                    useControlID.SetValue(null, controlID);
                }

                isClickedComboButton = true;
            }

            if (isClickedComboButton)
            {
                UnityEngine.GUI.enabled = false;
                UnityEngine.GUI.color = new Color(1, 1, 1, 2);

                var listRect = new Rect(Rect.x - 150f, Rect.y, Rect.width + 150f, Rect.height);

                var location = GUIUtility.GUIToScreenPoint(new Vector2(listRect.x, listRect.y + listStyle.CalcHeight(listContent[0], 1.0f)));
                var size = new Vector2(listRect.width, listStyle.CalcHeight(listContent[0], 1.0f) * listContent.Length);

                var innerRect = new Rect(0, 0, size.x, size.y);

                var outerRectScreen = new Rect(location.x, location.y, size.x, size.y);
                if (outerRectScreen.yMax > _windowYmax)
                {
                    outerRectScreen.height = _windowYmax - outerRectScreen.y;
                    outerRectScreen.width += 20;
                }

                if (currentMousePosition != Vector2.zero && outerRectScreen.Contains(GUIUtility.GUIToScreenPoint(currentMousePosition)))
                    done = false;

                Action Drawer = () =>
                {
                    UnityEngine.GUI.enabled = true;

                    var scrpos = GUIUtility.ScreenToGUIPoint(location);
                    var outerRectLocal = new Rect(scrpos.x, scrpos.y, outerRectScreen.width, outerRectScreen.height);

                    DrawContolBackground.Invoke(null, new object[] { outerRectLocal, default(Color) });

                    _scrollPosition = UnityEngine.GUI.BeginScrollView(outerRectLocal, _scrollPosition, innerRect, false, false);
                    {
                        const int initialSelectedItem = -1;
                        var newSelectedItemIndex = UnityEngine.GUI.SelectionGrid(innerRect, initialSelectedItem, listContent, 1, listStyle);
                        if (newSelectedItemIndex != initialSelectedItem)
                        {
                            onItemSelected(newSelectedItemIndex);
                            isClickedComboButton = false;
                        }
                    }
                    UnityEngine.GUI.EndScrollView(true);
                };

                CurrentDropdownDrawer.SetValue(null, Drawer);
            }

            if (done)
                isClickedComboButton = false;
        }

        private Vector2 _scrollPosition = Vector2.zero;
    }
}
