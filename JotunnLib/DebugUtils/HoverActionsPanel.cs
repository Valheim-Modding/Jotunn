using System;
using Jotunn.Managers;
using UnityEngine;
using UnityEngine.UI;

namespace Jotunn.DebugUtils
{
    internal class HoverActionsPanel : MonoBehaviour
    {
        public static Action<GameObject> OnHoverChanged;
        private static GameObject _panel;
        private GameObject _lastHoverObject;

        private void Awake()
        {
            On.Hud.UpdateCrosshair += HudUpdateCrosshairPostfix;
        }

        private void HudUpdateCrosshairPostfix(On.Hud.orig_UpdateCrosshair orig, Hud self, Player player, float bowDrawPercentage)
        {
            orig(self, player, bowDrawPercentage);

            GameObject hoverObject = GetValidHoverObject(player);

            if (hoverObject != _lastHoverObject)
            {
                CreatePanel(self);
                _lastHoverObject = hoverObject;
                OnHoverChanged?.SafeInvoke(hoverObject);
            }
        }

        private GameObject GetValidHoverObject(Player player)
        {
            if (!player || !player.GetHoverObject())
            {
                return null;
            }

            GameObject hoverObject = player.GetHoverObject();
            ZNetView zNetView = hoverObject.GetComponentInParent<ZNetView>();

            if (!zNetView || !zNetView.IsValid())
            {
                return null;
            }

            return hoverObject;
        }

        private void CreatePanel(Hud hud)
        {
            if (_panel)
            {
                GameObject.Destroy(_panel);
            }

            _panel = new GameObject("HoverActionsRoot", typeof(RectTransform));
            _panel.transform.SetParent(hud.m_hoverName.transform.parent);

            RectTransform transform = _panel.GetComponent<RectTransform>();
            transform.anchorMin = Vector2.zero;
            transform.anchorMax = new Vector2(-1f, 1f);
            transform.pivot = Vector2.zero;
            transform.anchoredPosition = new Vector2(22f, 22f);
            HorizontalLayoutGroup panelLayout = _panel.AddComponent<HorizontalLayoutGroup>();
            panelLayout.spacing = 8f;
            ContentSizeFitter fitter = _panel.AddComponent<ContentSizeFitter>();
            fitter.horizontalFit = ContentSizeFitter.FitMode.PreferredSize;
            fitter.verticalFit = ContentSizeFitter.FitMode.MinSize;

            GameObject left = new GameObject("lhs", typeof(RectTransform));
            left.transform.SetParent(_panel.transform);

            VerticalLayoutGroup leftLayout = left.AddComponent<VerticalLayoutGroup>();
            leftLayout.childControlWidth = true;
            leftLayout.childControlHeight = true;
            leftLayout.childForceExpandWidth = true;
            leftLayout.childForceExpandHeight = false;
            leftLayout.childAlignment = TextAnchor.LowerLeft;
            ContentSizeFitter leftFitter = left.AddComponent<ContentSizeFitter>();
            leftFitter.horizontalFit = ContentSizeFitter.FitMode.Unconstrained;
            leftFitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

            GameObject right = new GameObject("rhs", typeof(RectTransform));
            right.transform.SetParent(_panel.transform);

            VerticalLayoutGroup rightLayout = right.AddComponent<VerticalLayoutGroup>();
            rightLayout.childControlWidth = true;
            rightLayout.childControlHeight = true;
            rightLayout.childForceExpandWidth = true;
            rightLayout.childForceExpandHeight = false;
            rightLayout.childAlignment = TextAnchor.LowerRight;
            ContentSizeFitter rightFitter = right.AddComponent<ContentSizeFitter>();
            rightFitter.horizontalFit = ContentSizeFitter.FitMode.PreferredSize;
            rightFitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

            _panel.SetActive(true);
        }

        public static void AddRow(string lhs, string rhs)
        {
            GameObject left = new GameObject("lhs", typeof(RectTransform));
            left.transform.SetParent(_panel.transform.Find("lhs"));
            Text ltext = left.AddComponent<Text>();
            ltext.alignment = TextAnchor.MiddleRight;
            ltext.horizontalOverflow = HorizontalWrapMode.Overflow;
            ltext.font = GUIManager.Instance.AveriaSerif;
            ltext.fontSize = 18;
            ltext.text = lhs;

            GameObject right = new GameObject("rhs", typeof(RectTransform));
            right.transform.SetParent(_panel.transform.Find("rhs"));
            Text rtext = right.AddComponent<Text>();
            rtext.alignment = TextAnchor.MiddleLeft;
            rtext.horizontalOverflow = HorizontalWrapMode.Wrap;
            rtext.font = GUIManager.Instance.AveriaSerif;
            rtext.fontSize = 18;
            rtext.text = rhs;

            ((RectTransform)_panel.transform).anchoredPosition += new Vector2(0f, 10f);
        }

    }
}
