using UnityEngine;
using UnityEngine.UI;

namespace Jotunn.DebugUtils
{
    internal class TwoColumnPanel
    {
        private GameObject _panel;
        private GameObject _leftColumn;
        private GameObject _rightColumn;

        private readonly Font _font;
        private readonly Color32 _color;

        public TwoColumnPanel(Transform parent, Font font)
        {
            _font = font;
            _color = Color.clear;
            CreatePanel(parent);
        }

        public TwoColumnPanel(Transform parent, Font font, Color32 color)
        {
            _font = font;
            _color = color;
            CreatePanel(parent);
        }

        public void DestroyPanel()
        {
            Object.Destroy(_panel);
        }

        public void SetActive(bool active)
        {
            if (_panel.activeSelf != active)
            {
                _panel.SetActive(active);
            }
        }

        public TwoColumnPanel SetAnchor(Vector2 anchor)
        {
            _panel.GetComponent<RectTransform>().anchorMin = anchor;
            _panel.GetComponent<RectTransform>().anchorMax = anchor;
            return this;
        }

        public TwoColumnPanel SetPosition(Vector2 position)
        {
            _panel.GetComponent<RectTransform>().anchoredPosition = position;
            return this;
        }
        
        private void CreatePanel(Transform parent)
        {
            _panel = new GameObject("TwoColumnPanel", typeof(RectTransform));
            _panel.transform.SetParent(parent, worldPositionStays: false);

            RectTransform transform = _panel.GetComponent<RectTransform>();
            transform.anchorMin = new Vector2(1, 0);
            transform.anchorMax = new Vector2(1, 0);
            transform.pivot = Vector2.zero;
            transform.anchoredPosition = Vector2.zero;

            HorizontalLayoutGroup panelLayout = _panel.AddComponent<HorizontalLayoutGroup>();
            panelLayout.padding = new RectOffset(left: 5, right: 5, top: 5, bottom: 5);
            panelLayout.spacing = 8f;

            ContentSizeFitter panelFitter = _panel.AddComponent<ContentSizeFitter>();
            panelFitter.horizontalFit = ContentSizeFitter.FitMode.PreferredSize;
            panelFitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

            Image panelImage = _panel.AddComponent<Image>();
            panelImage.color = _color;

            _leftColumn = new GameObject("LeftColumn", typeof(RectTransform));
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

            _rightColumn = new GameObject("RightColumn", typeof(RectTransform));
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

        public TwoColumnPanel AddPanelRow(string leftText, out Text rightText)
        {
            AddPanelRow(out var tmpText, out rightText);
            tmpText.text = leftText;
            return this;
        }

        public TwoColumnPanel AddPanelRow(out Text leftText, out Text rightText)
        {
            GameObject leftSide = new GameObject("Label", typeof(RectTransform));
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

            GameObject rightSide = new GameObject("Value", typeof(RectTransform));
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
