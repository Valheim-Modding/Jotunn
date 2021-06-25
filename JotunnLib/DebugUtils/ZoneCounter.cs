using UnityEngine;
using UnityEngine.UI;

namespace Jotunn.DebugUtils
{
    internal class ZoneCounter : MonoBehaviour
    {
        private GameObject _zoneScouterRoot;
        private Text _zonePositionText;
        private Text _zoneSectorText;
        private Text _zoneZdoCountText;
        private Vector3 _lastPosition;
        private Vector2i _lastSector;
        private long _lastZdoCount;

        private void Awake()
        {
            On.Hud.Awake += Hud_Awake;
            On.Hud.OnDestroy += Hud_OnDestroy;
        }

        private void Hud_Awake(On.Hud.orig_Awake orig, Hud self)
        {
            orig(self);

            _zoneScouterRoot = CreateZoneScouterRoot(self);

            _zonePositionText = Instantiate<Text>(self.m_hoverName, _zoneScouterRoot.transform);
            _zonePositionText.gameObject.SetActive(true);

            _zoneSectorText = Instantiate<Text>(self.m_hoverName, _zoneScouterRoot.transform);
            _zoneSectorText.gameObject.SetActive(true);

            _zoneZdoCountText = Instantiate<Text>(self.m_hoverName, _zoneScouterRoot.transform);
            _zoneZdoCountText.gameObject.SetActive(true);

            InvokeRepeating("UpdateZoneSectorPositionText", 1.0f, 0.5f);
        }

        private void Hud_OnDestroy(On.Hud.orig_OnDestroy orig, Hud self)
        {
            if (_zoneScouterRoot)
            {
                Destroy(_zoneScouterRoot);
            }

            orig(self);
        }

        private void UpdateZoneSectorPositionText()
        {
            if (!ZoneSystem.instance || !Player.m_localPlayer)
            {
                return;
            }

            Vector3 position = Player.m_localPlayer.transform.position;

            if (position != _lastPosition)
            {
                _zonePositionText.text =
                    string.Format(
                        "{0}\t\tX <color={1}>{2,-8:0}</color>\tZ <color={3}>{4,-8:0}</color>\tY <color={5}>{6,-8:0}</color>",
                        "Position",
                        "#ffe082",
                        position.x,
                        "#a5d6a7",
                        position.z,
                        "#90caf9",
                        position.y);

                _lastPosition = position;
            }

            Vector2i sector = ZoneSystem.instance.GetZone(position);

            if (sector != _lastSector)
            {
                _zoneSectorText.text =
                    string.Format(
                        "{0,-10}\t\t<color={1}>{2}</color>, <color={3}>{4}</color>",
                        "Sector",
                        "#ffe082",
                        sector.x,
                        "#a5d6a7",
                        sector.y);

                _lastSector = sector;
            }

            int sectorIndex = ZDOMan.instance.SectorToIndex(sector);
            long zdoCount =
                sectorIndex >= 0 && ZDOMan.instance.m_objectsBySector[sectorIndex] != null
                    ? ZDOMan.instance.m_objectsBySector[sectorIndex].Count
                    : 0L;

            if (_lastZdoCount != zdoCount)
            {
                _zoneZdoCountText.text = string.Format("{0,-10}\t<color={1}>{2}</color>", "ZDOs", "#ffe082", zdoCount);
                _lastZdoCount = zdoCount;
            }
        }
        private GameObject CreateZoneScouterRoot(Hud hud)
        {
            var hotkeyBar = hud.GetComponentInChildren<HotkeyBar>();

            var zoneScouterRoot =
                new GameObject(
                    "ZoneScouterRoot",
                    typeof(RectTransform),
                    typeof(VerticalLayoutGroup),
                    typeof(ContentSizeFitter),
                    typeof(Image));

            zoneScouterRoot.transform.SetParent(hotkeyBar.transform);

            RectTransform transform = zoneScouterRoot.GetComponent<RectTransform>();
            transform.anchorMin = Vector2.one;
            transform.anchorMax = Vector2.one;
            transform.pivot = new Vector2(0f, 1f);
            transform.anchoredPosition = new Vector2(10f, 0f);

            VerticalLayoutGroup layoutGroup = zoneScouterRoot.GetComponent<VerticalLayoutGroup>();
            layoutGroup.childControlHeight = true;
            layoutGroup.childAlignment = TextAnchor.UpperLeft;
            layoutGroup.padding = new RectOffset(left: 5, right: 5, top: 5, bottom: 5);
            layoutGroup.spacing = 8f;

            ContentSizeFitter fitter = zoneScouterRoot.GetComponent<ContentSizeFitter>();
            fitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;
            fitter.horizontalFit = ContentSizeFitter.FitMode.PreferredSize;

            zoneScouterRoot.GetComponent<Image>().color = new Color32(0, 0, 0, 96);
            zoneScouterRoot.SetActive(true);

            return zoneScouterRoot;
        }
    }
}
