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

            _panel = new GameObject(
                "HoverActionsRoot", typeof(RectTransform), typeof(VerticalLayoutGroup), typeof(ContentSizeFitter));

            _panel.SetActive(true);
            _panel.transform.SetParent(hud.m_hoverName.transform.parent);

            RectTransform transform = _panel.GetComponent<RectTransform>();
            transform.anchorMin = Vector2.zero;
            transform.anchorMax = new Vector2(-1f, 1f);
            transform.pivot = Vector2.zero;
            transform.anchoredPosition = new Vector2(22f, 22f);

            VerticalLayoutGroup layoutGroup = _panel.GetComponent<VerticalLayoutGroup>();
            layoutGroup.childControlHeight = true;
            layoutGroup.childAlignment = TextAnchor.LowerLeft;
            layoutGroup.spacing = 8f;

            ContentSizeFitter fitter = _panel.GetComponent<ContentSizeFitter>();
            fitter.verticalFit = ContentSizeFitter.FitMode.MinSize;
            fitter.horizontalFit = ContentSizeFitter.FitMode.PreferredSize;
        }

        public static void AddRow(string lhs, string rhs)
        {
            GameObject row = new GameObject("row", typeof(RectTransform), typeof(GridLayoutGroup));
            row.GetComponent<RectTransform>().SetParent(_panel.transform);
            GridLayoutGroup grid = row.GetComponent<GridLayoutGroup>();
            grid.cellSize = new Vector2(150f, 20f);
            grid.spacing = new Vector2(5f, 5f);
            grid.constraint = GridLayoutGroup.Constraint.FixedColumnCount;
            grid.constraintCount = 2;

            GameObject left = new GameObject("lhs", typeof(RectTransform), typeof(Text));
            left.GetComponent<RectTransform>().SetParent(row.transform);
            Text ltext = left.GetComponent<Text>();
            ltext.alignment = TextAnchor.MiddleRight;
            ltext.font = GUIManager.Instance.AveriaSerif;
            ltext.fontSize = 18;
            ltext.text = lhs;

            GameObject right = new GameObject("rhs", typeof(RectTransform), typeof(Text));
            right.GetComponent<RectTransform>().SetParent(row.transform);
            Text rtext = right.GetComponent<Text>();
            rtext.alignment = TextAnchor.MiddleLeft;
            rtext.font = GUIManager.Instance.AveriaSerif;
            rtext.fontSize = 18;
            rtext.text = rhs;
        }

    }
}
