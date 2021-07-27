using UnityEngine;
using UnityEngine.UI;

namespace Jotunn.DebugUtils
{
    internal class HoverActionsPanel
    {
        private static GameObject _hoverActionsPanel = null;

        public static GameObject GetPanel(Hud hud)
        {
            if (!_hoverActionsPanel)
            {
                _hoverActionsPanel = CreatePanel(hud);
            }

            return _hoverActionsPanel;
        }

        private static GameObject CreatePanel(Hud hud)
        {
            var hoverActionsRoot =
                new GameObject(
                    "HoverActionsRoot", typeof(RectTransform), typeof(VerticalLayoutGroup), typeof(ContentSizeFitter));

            hoverActionsRoot.transform.SetParent(hud.m_hoverName.transform.parent);

            RectTransform transform = hoverActionsRoot.GetComponent<RectTransform>();
            transform.anchorMin = Vector2.zero;
            transform.anchorMax = new Vector2(-1f, 1f);
            transform.pivot = Vector2.zero;
            transform.anchoredPosition = new Vector2(22f, 22f);

            VerticalLayoutGroup layoutGroup = hoverActionsRoot.GetComponent<VerticalLayoutGroup>();
            layoutGroup.childControlHeight = true;
            layoutGroup.childAlignment = TextAnchor.LowerLeft;
            layoutGroup.spacing = 8f;

            ContentSizeFitter fitter = hoverActionsRoot.GetComponent<ContentSizeFitter>();
            fitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;
            fitter.horizontalFit = ContentSizeFitter.FitMode.PreferredSize;

            hoverActionsRoot.SetActive(true);
            return hoverActionsRoot;
        }
    }
}
