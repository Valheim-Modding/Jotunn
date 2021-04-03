using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using Button = UnityEngine.UI.Button;
using Image = UnityEngine.UI.Image;
using Toggle = UnityEngine.UI.Toggle;

namespace JotunnLib.Managers
{
    public class GUIManager : Manager, IPointerClickHandler
    {
        internal static GameObject GUIContainer;

        private bool needsLoad = true;

        internal Dictionary<string, Sprite> Sprites = new Dictionary<string, Sprite>();
        private bool GUIInStart = true;
        public static GUIManager Instance { get; private set; }

        public static GameObject PixelFix { get; private set; }

        internal Texture2D TextureAtlas { get; private set; }

        internal Texture2D TextureAtlas2 { get; private set; }

        internal Font AveriaSerif { get; private set; }

        internal Font AveriaSerifBold { get; private set; }

        private const int UILayer = 5;

        public void OnPointerClick(PointerEventData eventData)
        {
            Logger.LogMessage(eventData.GetObjectString());
        }

        private void Awake()
        {
            if (Instance != null)
            {
                Logger.LogError($"Two instances of singleton {GetType()}");
                return;
            }

            Logger.LogWarning("GUIManager instance is set!");
            Instance = this;
        }

        internal override void Init()
        {
            GUIContainer = new GameObject("GUI");
            GUIContainer.transform.SetParent(Main.RootObject.transform);
            GUIContainer.layer = UILayer; // UI
            var canvas = GUIContainer.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            GUIContainer.GetComponent<RectTransform>().localScale = new Vector3(1, 1, 1);
            GUIContainer.AddComponent<GraphicRaycaster>();
            GUIContainer.AddComponent<CanvasScaler>();
            GUIContainer.GetComponent<RectTransform>().anchorMin = new Vector2(0.5f, 0.5f);
            GUIContainer.GetComponent<RectTransform>().anchorMax = new Vector2(0.5f, 0.5f);
            GUIContainer.GetComponent<RectTransform>().anchoredPosition = new Vector2(0, 0);
            GUIContainer.GetComponent<Canvas>().planeDistance = 0.0f;
            GUIContainer.AddComponent<GuiScaler>().UpdateScale();


            CreatePixelFix();

            Logger.LogInfo("Initialized GUIManager");

            SceneManager.sceneLoaded += SceneManager_sceneLoaded;
        }

        private void SceneManager_sceneLoaded(Scene scene, LoadSceneMode loadMode)
        {
            if (scene.name == "start" && !GUIInStart)
            {
                GUIContainer.SetActive(true);
                CreatePixelFix();
                GUIInStart = true;
            }

            if (scene.name == "main" && GUIInStart)
            {
                GameObject root = SceneManager.GetActiveScene().GetRootGameObjects().FirstOrDefault(x => x.name == "_GameMain");
                if (root != null)
                {
                    GameObject gui = root.transform.Find("GUI/PixelFix").gameObject;
                    if (gui != null)
                    {
                        Destroy(PixelFix);
                        PixelFix = new GameObject("GUIFix", typeof(RectTransform));
                        PixelFix.layer = UILayer; // UI
                        PixelFix.transform.SetParent(gui.transform, false);
                        GUIContainer.SetActive(false);
                        GUIInStart = false;
                    }
                }
            }
        }

        private static void CreatePixelFix()
        {
            PixelFix = new GameObject("GUIFix", typeof(RectTransform), typeof(GuiPixelFix));
            PixelFix.layer = UILayer; // UI
            PixelFix.transform.SetParent(GUIContainer.transform);
            PixelFix.GetComponent<RectTransform>().localScale = new Vector3(1, 1, 1);
            PixelFix.GetComponent<RectTransform>().anchorMin = new Vector2(0.5f, 0.5f);
            PixelFix.GetComponent<RectTransform>().anchorMax = new Vector2(0.5f, 0.5f);
            PixelFix.GetComponent<RectTransform>().SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal, GUIContainer.GetComponent<RectTransform>().rect.width);
            PixelFix.GetComponent<RectTransform>().SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical, GUIContainer.GetComponent<RectTransform>().rect.height);
            PixelFix.GetComponent<RectTransform>().anchoredPosition = new Vector2(GUIContainer.GetComponent<RectTransform>().rect.width / 2f,
                GUIContainer.GetComponent<RectTransform>().rect.height / 2f);
        }

        private void OnGUI()
        {
            // Load valheim GUI assets

            if (needsLoad && SceneManager.GetActiveScene().name == "start" && SceneManager.GetActiveScene().isLoaded)
            {
                try
                {
                    // Texture Atlas aka Sprite Sheet
                    var textures = Resources.FindObjectsOfTypeAll<Texture2D>();
                    TextureAtlas = textures.LastOrDefault(x => x.name == "sactx-2048x2048-Uncompressed-UIAtlas-a5f4e704");
                    TextureAtlas2 = textures.FirstOrDefault(x => x.name == "sactx-2048x2048-Uncompressed-UIAtlas-a5f4e704");
                    if (TextureAtlas == null || TextureAtlas2 == null)
                    {
                        throw new Exception("Texture atlas not found");
                    }

                    // Sprites
                    string[] spriteNames = { "checkbox", "checkbox_marker", "woodpanel_trophys", "button" };
                    var sprites = Resources.FindObjectsOfTypeAll<Sprite>();

                    var notFound = false;
                    foreach (var spriteName in spriteNames)
                    {
                        var sprite = sprites.FirstOrDefault(x => x.name == spriteName);
                        notFound |= sprite == null;
                        if (sprite == null)
                        {
                            Logger.LogError($"Sprite {spriteName} not found.");
                            break;
                        }

                        Sprites.Add(spriteName, sprite);
                    }

                    if (notFound)
                    {
                        throw new Exception("Sprites not found");
                    }

                    // Fonts
                    var fonts = Resources.FindObjectsOfTypeAll<Font>();
                    AveriaSerif = fonts.FirstOrDefault(x => x.name == "AveriaSerifLibre-Regular");
                    AveriaSerifBold = fonts.FirstOrDefault(x => x.name == "AveriaSerifLibre-Bold");
                    if (AveriaSerifBold == null || AveriaSerif == null)
                    {
                        throw new Exception("Fonts not found");
                    }

                    // GUI components (ouch, my memory hurts... :))
                    GameObject ingameGui = null;
                    var gameobjects = Resources.FindObjectsOfTypeAll(typeof(GameObject));
                    foreach (var obj in gameobjects)
                    {
                        if (obj.name.Equals("IngameGui"))
                        {
                            ingameGui = (GameObject)obj;
                            break;
                        }
                    }

                    if (ingameGui == null)
                    {
                        throw new Exception("GameObjects not found");
                    }

                    // Base prefab for a valheim style button
                    var button = new GameObject("BaseButton", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image), typeof(Button), typeof(ButtonSfx));
                    button.transform.SetParent(PixelFix.transform, false);

                    var txt = new GameObject("Text", typeof(RectTransform), typeof(CanvasRenderer), typeof(Text), typeof(Outline));
                    txt.transform.SetParent(button.transform);
                    txt.transform.localScale = new Vector3(1f, 1f, 1f);

                    txt.GetComponent<RectTransform>().anchorMin = new Vector2(0.5f, 0.5f);
                    txt.GetComponent<RectTransform>().anchorMax = new Vector2(0.5f, 0.5f);
                    txt.GetComponent<RectTransform>().SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal, 140f);
                    txt.GetComponent<RectTransform>().SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical, 38f);
                    txt.GetComponent<RectTransform>().anchoredPosition = new Vector2(0, 0f);

                    txt.GetComponent<Text>().font = AveriaSerifBold;
                    txt.GetComponent<Text>().fontSize = 16;
                    txt.GetComponent<Text>().color = new Color(1f, 0.631f, 0.235f, 1f);
                    txt.GetComponent<Text>().alignment = TextAnchor.MiddleCenter;

                    txt.GetComponent<Outline>().effectColor = new Color(0f, 0f, 0f, 1f);
                    var sprite2 = GetSprite("button");
                    if (sprite2 == null)
                    {
                        Logger.LogError("Could not find 'button' sprite");
                    }
                    button.GetComponent<Image>().sprite = sprite2;
                    button.GetComponent<Image>().type = Image.Type.Sliced;
                    button.GetComponent<Image>().pixelsPerUnitMultiplier = 2f;

                    button.GetComponent<Button>().image = button.GetComponent<Image>();

                    button.GetComponent<RectTransform>().anchorMin = new Vector2(0.5f, 0.5f);
                    button.GetComponent<RectTransform>().anchorMax = new Vector2(0.5f, 0.5f);
                    button.GetComponent<RectTransform>().pivot = new Vector2(0.5f, 0.5f);
                    button.GetComponent<RectTransform>().SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal, 140);
                    button.GetComponent<RectTransform>().SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical, 38);
                    button.GetComponent<RectTransform>().anchoredPosition = new Vector2(0, 0);

                    button.GetComponent<ButtonSfx>().m_sfxPrefab = (GameObject)gameobjects.FirstOrDefault(x => x.name == "sfx_gui_button");
                    button.GetComponent<ButtonSfx>().m_selectSfxPrefab = (GameObject)gameobjects.FirstOrDefault(x => x.name == "sfx_gui_select");

                    var tinter = new ColorBlock()
                    {
                        disabledColor = new Color(0.566f, 0.566f, 0.566f, 0.502f),
                        fadeDuration = 0.1f,
                        normalColor = new Color(0.824f, 0.824f, 0.824f, 1f),
                        highlightedColor = new Color(1.3f, 1.3f, 1.3f, 1f),
                        pressedColor = new Color(0.537f, 0.556f, 0.556f, 1f),
                        selectedColor = new Color(0.824f, 0.824f, 0.824f, 1f),
                        colorMultiplier = 1f
                    };

                    button.GetComponent<Button>().colors = tinter;

                    button.SetActive(false);
                    button.layer = UILayer; // UI

                    PrefabManager.Instance.AddPrefab(button);


                    // Base woodpanel prefab

                    var woodpanel = new GameObject("BaseWoodpanel", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
                    woodpanel.GetComponent<RectTransform>().anchorMin = new Vector2(0.5f, 0.5f);
                    woodpanel.GetComponent<RectTransform>().anchorMax = new Vector2(0.5f, 0.5f);
                    woodpanel.GetComponent<RectTransform>().anchoredPosition = new Vector2(0f, 0f);

                    woodpanel.GetComponent<Image>().sprite = GetSprite("woodpanel_trophys");
                    woodpanel.GetComponent<Image>().type = Image.Type.Sliced;
                    woodpanel.GetComponent<Image>().pixelsPerUnitMultiplier = 2f;

                    woodpanel.layer = UILayer; // UI

                    PrefabManager.Instance.AddPrefab(woodpanel);

                }
                catch (Exception ex)
                {
                    Logger.LogError(ex);
                }
                finally
                {
                    needsLoad = false;
                }
            }
        }

        /// <summary>
        ///     Create a new button (Valheim style)
        /// </summary>
        /// <param name="text"></param>
        /// <param name="parent"></param>
        /// <param name="anchorMin"></param>
        /// <param name="anchorMax"></param>
        /// <param name="position"></param>
        /// <param name="width">Set width if > 0</param>
        /// <param name="height">Set height if > 0</param>
        /// <returns>new Button gameobject</returns>
        public GameObject CreateButton(string text, Transform parent, Vector2 anchorMin, Vector2 anchorMax, Vector2 position, float width = 0f,
            float height = 0f)
        {
            var baseButton = PrefabManager.Instance.GetPrefab("BaseButton");

            if (baseButton == null)
            {
                Logger.LogError("BaseButton is null");
                return null;
            }

            var newButton = Instantiate(baseButton, parent, false);

            newButton.GetComponent<Image>().pixelsPerUnitMultiplier = GUIInStart ? 2f : 1f;

            // Set text
            newButton.GetComponentInChildren<Text>().text = text;

            // Set positions and anchors
            ((RectTransform)newButton.transform).anchoredPosition = position;
            ((RectTransform)newButton.transform).anchorMin = anchorMin;
            ((RectTransform)newButton.transform).anchorMax = anchorMax;

            if (width > 0f)
            {
                ((RectTransform)newButton.transform).SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal, width);
                newButton.transform.Find("Text").GetComponent<RectTransform>().SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal, width);
            }

            if (height > 0f)
            {
                ((RectTransform)newButton.transform).SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical, height);
                newButton.transform.Find("Text").GetComponent<RectTransform>().SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical, height);
            }

            return newButton;
        }

        public GameObject CreateWoodpanel(Transform parent, Vector2 anchorMin, Vector2 anchorMax, Vector2 position, float width = 0f, float height = 0f)
        {
            var basepanel = PrefabManager.Instance.GetPrefab("BaseWoodpanel");

            if (basepanel == null)
            {
                Logger.LogError("BasePanel is null");
            }

            var newPanel = Instantiate(basepanel, parent, false);
            newPanel.GetComponent<Image>().pixelsPerUnitMultiplier = GUIInStart ? 2f : 1f;

            // Set positions and anchors
            ((RectTransform)newPanel.transform).anchoredPosition = position;
            ((RectTransform)newPanel.transform).anchorMin = anchorMin;
            ((RectTransform)newPanel.transform).anchorMax = anchorMax;

            if (width > 0f)
            {
                ((RectTransform)newPanel.transform).SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal, width);
            }

            if (height > 0f)
            {
                ((RectTransform)newPanel.transform).SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical, height);
            }

            return newPanel;
        }

        /// <summary>
        ///     Get sprite low level fashion from sactx-2048x2048-Uncompressed-UIAtlas-a5f4e704_0 atlas
        /// </summary>
        /// <param name="rect">Rect on atlas texture</param>
        /// <param name="pivot">pivot</param>
        /// <param name="pixelsPerUnit"></param>
        /// <param name="extrude"></param>
        /// <param name="meshType"></param>
        /// <param name="slice"></param>
        /// <returns></returns>
        public Sprite CreateSpriteFromAtlas(Rect rect, Vector2 pivot, float pixelsPerUnit = 50f, uint extrude = 0,
            SpriteMeshType meshType = SpriteMeshType.FullRect, Vector4 slice = new Vector4())
        {
            return Sprite.Create(TextureAtlas, rect, pivot, pixelsPerUnit, extrude, meshType, slice);
        }

        /// <summary>
        ///     Get sprite low level fashion from sactx-2048x2048-Uncompressed-UIAtlas-a5f4e704 atlas (not much used ingame or old
        ///     textures)
        /// </summary>
        /// <param name="rect">Rect on atlas texture</param>
        /// <param name="pivot">pivot</param>
        /// <param name="pixelsPerUnit"></param>
        /// <param name="extrude"></param>
        /// <param name="meshType"></param>
        /// <param name="slice"></param>
        /// <returns></returns>
        public Sprite CreateSpriteFromAtlas2(Rect rect, Vector2 pivot, float pixelsPerUnit = 50f, uint extrude = 0,
            SpriteMeshType meshType = SpriteMeshType.FullRect, Vector4 slice = new Vector4())
        {
            return Sprite.Create(TextureAtlas2, rect, pivot, pixelsPerUnit, extrude, meshType, slice);
        }

        /// <summary>
        ///     Get a sprite by name
        /// </summary>
        /// <param name="spriteName"></param>
        /// <returns></returns>
        public Sprite GetSprite(string spriteName)
        {
            if (Sprites.ContainsKey(spriteName))
            {
                return Sprites[spriteName];
            }

            var sprite = Resources.FindObjectsOfTypeAll<Sprite>().FirstOrDefault(x => x.name == spriteName);
            if (sprite != null)
            {
                Sprites.Add(spriteName, sprite);
                return sprite;
            }

            Logger.LogError($"Sprite {spriteName} not found.");

            return null;
        }

        /// <summary>
        ///     Apply Valheim style to an inputfield component
        /// </summary>
        /// <param name="field"></param>
        public void ApplyInputFieldStyle(InputField field)
        {
            var go = field.gameObject;

            go.GetComponent<Image>().sprite = CreateSpriteFromAtlas(new Rect(0, 2048 - 156, 139, 36), new Vector2(0.5f, 0.5f), 50f, 0, SpriteMeshType.FullRect,
                new Vector4(5, 5, 5, 5));
            go.transform.Find("Placeholder").GetComponent<Text>().font = AveriaSerifBold;
            go.transform.Find("Text").GetComponent<Text>().font = AveriaSerifBold;
            go.transform.Find("Text").GetComponent<Text>().color = new Color(1, 1, 1, 1);
        }

        /// <summary>
        ///     Apply Valheim style to a toggle
        /// </summary>
        /// <param name="toggle"></param>
        public void ApplyToogleStyle(Toggle toggle)
        {
            var tinter = new ColorBlock
            {
                colorMultiplier = 1f,
                disabledColor = new Color(0.784f, 0.784f, 0.784f, 0.502f),
                fadeDuration = 0.1f,
                highlightedColor = new Color(1f, 1f, 1f, 1f),
                normalColor = new Color(0.61f, 0.61f, 0.61f, 1f),
                pressedColor = new Color(0.784f, 0.784f, 0.784f, 1f),
                selectedColor = new Color(1f, 1f, 1f, 1f)
            };
            toggle.toggleTransition = Toggle.ToggleTransition.Fade;
            toggle.colors = tinter;

            toggle.gameObject.transform.Find("Background").GetComponent<Image>().sprite = GetSprite("checkbox");

            toggle.gameObject.transform.Find("Background/Checkmark").GetComponent<Image>().color = new Color(1f, 0.678f, 0.103f, 1f);

            toggle.gameObject.transform.Find("Background/Checkmark").GetComponent<Image>().sprite = GetSprite("checkbox_marker");
            toggle.gameObject.transform.Find("Background/Checkmark").GetComponent<Image>().maskable = true;
        }
    }
}