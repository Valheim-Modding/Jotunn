using UnityEngine;
using UnityEngine.UI;

namespace Jotunn.DebugUtils
{
    internal class TwoColumnPanel
    {
        private GameObject _panel = null;
        private GameObject _leftColumn = null;
        private GameObject _rightColumn = null;

        private readonly Font _font = null;

        public TwoColumnPanel(Transform parent, Font font)
        {
            CreatePanel(parent);
            _font = font;
        }

        public void DestroyPanel()
        {
            GameObject.Destroy(_panel);
        }

        public void SetActive(bool active)
        {
            if (_panel.activeSelf != active)
            {
                _panel.SetActive(active);
            }
        }

        public TwoColumnPanel SetPosition(Vector2 position)
        {
            _panel.GetComponent<RectTransform>().anchoredPosition = position;
            return this;
        }

        void CreatePanel(Transform parent)
        {
            _panel = new("TwoColumnPanel", typeof(RectTransform));
            _panel.transform.SetParent(parent, worldPositionStays: false);

            RectTransform transform = _panel.GetComponent<RectTransform>();
            transform.anchorMin = new Vector2(1, 0);
            transform.anchorMax = new Vector2(1, 0);
            transform.pivot = Vector2.zero;
            transform.anchoredPosition = Vector2.zero;

            HorizontalLayoutGroup panelLayout = _panel.AddComponent<HorizontalLayoutGroup>();
            panelLayout.spacing = 8f;

            ContentSizeFitter panelFitter = _panel.AddComponent<ContentSizeFitter>();
            panelFitter.horizontalFit = ContentSizeFitter.FitMode.PreferredSize;
            panelFitter.verticalFit = ContentSizeFitter.FitMode.MinSize;

            _leftColumn = new("LeftColumn", typeof(RectTransform));
            _leftColumn.transform.SetParent(_panel.transform, worldPositionStays: false);

            VerticalLayoutGroup leftLayout = _leftColumn.AddComponent<VerticalLayoutGroup>();
            leftLayout.childControlWidth = true;
            leftLayout.childControlHeight = true;
            leftLayout.childForceExpandWidth = true;
            leftLayout.childForceExpandHeight = false;
            leftLayout.childAlignment = TextAnchor.LowerLeft;
            leftLayout.spacing = 5f;

            ContentSizeFitter leftFitter = _leftColumn.AddComponent<ContentSizeFitter>();
            leftFitter.horizontalFit = ContentSizeFitter.FitMode.Unconstrained;
            leftFitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

            _rightColumn = new("RightColumn", typeof(RectTransform));
            _rightColumn.transform.SetParent(_panel.transform, worldPositionStays: false);

            VerticalLayoutGroup rightLayout = _rightColumn.AddComponent<VerticalLayoutGroup>();
            rightLayout.childControlWidth = true;
            rightLayout.childControlHeight = true;
            rightLayout.childForceExpandWidth = true;
            rightLayout.childForceExpandHeight = false;
            rightLayout.childAlignment = TextAnchor.LowerRight;
            rightLayout.spacing = 5f;

            ContentSizeFitter rightFitter = _rightColumn.AddComponent<ContentSizeFitter>();
            rightFitter.horizontalFit = ContentSizeFitter.FitMode.PreferredSize;
            rightFitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;
        }

        public TwoColumnPanel AddPanelRow(out Text leftText, out Text rightText)
        {
            GameObject leftSide = new("Label", typeof(RectTransform));
            leftSide.transform.SetParent(_leftColumn.transform, worldPositionStays: false);

            leftText = leftSide.AddComponent<Text>();
            leftText.alignment = TextAnchor.MiddleRight;
            leftText.horizontalOverflow = HorizontalWrapMode.Overflow;
            leftText.font = _font;
            leftText.fontSize = 18;
            leftText.text = "Label";

            Outline leftOutline = leftSide.AddComponent<Outline>();
            leftOutline.effectColor = Color.black;
            leftOutline.effectDistance = new Vector2(1, -1);

            GameObject rightSide = new("Value", typeof(RectTransform));
            rightSide.transform.SetParent(_rightColumn.transform, worldPositionStays: false);

            rightText = rightSide.AddComponent<Text>();
            rightText.alignment = TextAnchor.MiddleLeft;
            rightText.horizontalOverflow = HorizontalWrapMode.Wrap;
            rightText.font = _font;
            rightText.fontSize = 18;
            rightText.text = "Value";

            Outline rightOutline = rightSide.AddComponent<Outline>();
            rightOutline.effectColor = Color.black;
            rightOutline.effectDistance = new Vector2(1, -1);

            return this;
        }
    }
}
