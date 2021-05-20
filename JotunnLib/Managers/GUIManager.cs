using System;
using System.Collections.Generic;
using System.Linq;
using Jotunn;
using Jotunn.Configs;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using Button = UnityEngine.UI.Button;
using Image = UnityEngine.UI.Image;
using Object = UnityEngine.Object;
using Toggle = UnityEngine.UI.Toggle;

namespace Jotunn.Managers
{
    /// <summary>
    ///     Manager for handling anything GUI related. Provides Valheim style 
    ///     GUI elements as well as an anchor for custom GUI prefabs.
    /// </summary>
    public class GUIManager : IManager, IPointerClickHandler
    {
        private static GUIManager _instance;
        /// <summary>
        ///     Singleton instance
        /// </summary>
        public static GUIManager Instance
        {
            get
            {
                if (_instance == null) _instance = new GUIManager();
                return _instance;
            }
        }

        /// <summary>
        ///     GUI container with automatic scaling for high res displays.
        ///     Gets rebuild at every scene change so make sure to add your custom
        ///     GUI prefabs again on each scene change.
        /// </summary>
        public static GameObject PixelFix { get; private set; }

        /// <summary>
        ///     Internal container for GUI elements created by the GUIManager. 
        ///     Acts as a static transform for the base GUI elements which can
        ///     be instantiated with this classes helper methods.
        /// </summary>
        internal static GameObject GUIContainer;

        /// <summary>
        ///     Unity layer constant for UI objects.
        /// </summary>
        public const int UILayer = 5;

        /// <summary>
        ///     The default Valheim orange color.
        /// </summary>
        public Color ValheimOrange = new Color(1f, 0.631f, 0.235f, 1f);

        /// <summary>
        ///     Scrollbar handle color block in default Valheim orange.
        /// </summary>
        public ColorBlock ValheimScrollbarHandleColorBlock = new ColorBlock()
        {
            colorMultiplier = 1f,
            disabledColor = new Color(0.783f, 0.783f, 0.783f, 0.502f),
            fadeDuration = 0.1f,
            highlightedColor = new Color(1, 0.786f, 0.088f, 1f),
            normalColor = new Color(0.926f, 0.646f, 0.341f, 1f),
            pressedColor = new Color(0.838f, 0.647f, 0.031f, 1f),
            selectedColor = new Color(1, 0.786f, 0.088f, 1f),
        };

        /// <summary>
        ///     Valheim standard font normal faced.
        /// </summary>
        public Font AveriaSerif { get; private set; }

        /// <summary>
        ///     Valheims standard font bold faced.
        /// </summary>
        public Font AveriaSerifBold { get; private set; }

        /// <summary>
        ///     Internal reference to Valheims TextureAtlas. 
        ///     Used to get Valheim sprites in <see cref="CreateSpriteFromAtlas"/>.
        /// </summary>
        internal Texture2D TextureAtlas { get; private set; }

        /// <summary>
        ///     Internal reference to Valheims second TextureAtlas.
        ///     Used to get Valheim sprites in <see cref="CreateSpriteFromAtlas2"/>.
        /// </summary>
        internal Texture2D TextureAtlas2 { get; private set; }
        
        /// <summary>
        ///     Internal Disctionary holding the references to the sprites used in the helper methods.
        /// </summary>
        internal readonly Dictionary<string, Sprite> Sprites = new Dictionary<string, Sprite>();

        /// <summary>
        ///     Internal Disctionary holding the references to the custom key hints added to the manager.
        /// </summary>
        internal readonly Dictionary<string, KeyHintConfig> KeyHints = new Dictionary<string, KeyHintConfig>();

        /// <summary>
        ///     Reference to the games "KeyHint" GameObjects RectTransform.
        /// </summary>
        private RectTransform KeyHintContainer;

        /// <summary>
        ///     Indicates if the initial resource load needs to be performed.
        /// </summary>
        private bool needsLoad = true;

        /// <summary>
        ///     Indicates if the PixelFix must be created for the start or main scene.
        /// </summary>
        private bool GUIInStart = true;

        /// <summary>
        ///     Event receiver for pointer click events
        /// </summary>
        /// <param name="eventData"></param>
        public void OnPointerClick(PointerEventData eventData)
        {
#if DEBUG
            Logger.LogMessage(eventData.GetObjectString());
#endif
        }

        /// <summary>
        ///     Initialize the manager
        /// </summary>
        public void Init()
        {
            GUIContainer = new GameObject("GUI");
            GUIContainer.transform.SetParent(Main.RootObject.transform);
            GUIContainer.layer = UILayer; // UI
            var canvas = GUIContainer.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 1;
            GUIContainer.GetComponent<RectTransform>().localScale = new Vector3(1, 1, 1);
            GUIContainer.AddComponent<GraphicRaycaster>();
            GUIContainer.AddComponent<CanvasScaler>();
            GUIContainer.GetComponent<RectTransform>().anchorMin = new Vector2(0.5f, 0.5f);
            GUIContainer.GetComponent<RectTransform>().anchorMax = new Vector2(0.5f, 0.5f);
            GUIContainer.GetComponent<RectTransform>().anchoredPosition = new Vector2(0, 0);
            GUIContainer.GetComponent<Canvas>().planeDistance = 0.0f;
            GUIContainer.AddComponent<GuiScaler>().UpdateScale();

            CreatePixelFix();

            SceneManager.sceneLoaded += SceneManager_sceneLoaded;
            On.KeyHints.UpdateHints += ShowCustomKeyHint;
        }

        internal void OnGUI()
        {
            // Load valheim GUI assets

            if (needsLoad && SceneManager.GetActiveScene().name == "start" && SceneManager.GetActiveScene().isLoaded)
            {
                try
                {
                    // Texture Atlas aka Sprite Sheet
                    var textures = Resources.FindObjectsOfTypeAll<Texture2D>();
                    TextureAtlas = textures.LastOrDefault(x => x.name.StartsWith("sactx-2048x2048-Uncompressed-UIAtlas-"));
                    TextureAtlas2 = textures.FirstOrDefault(x => x.name.StartsWith("sactx-2048x2048-Uncompressed-UIAtlas-"));
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

        private void SceneManager_sceneLoaded(Scene scene, LoadSceneMode loadMode)
        {
            if (scene.name == "start" && !GUIInStart)
            {
                // Create a new PixelFix for start scene
                GUIContainer.SetActive(true);
                CreatePixelFix();
                GUIInStart = true;
            }
            if (scene.name == "main" && GUIInStart)
            {
                // Create a new PixelFix for main scene
                GameObject root = SceneManager.GetActiveScene().GetRootGameObjects().FirstOrDefault(x => x.name == "_GameMain");
                if (root != null)
                {
                    GameObject gui = root.transform.Find("GUI/PixelFix").gameObject;
                    if (gui != null)
                    {
                        GameObject.Destroy(PixelFix);
                        PixelFix = new GameObject("GUIFix", typeof(RectTransform));
                        PixelFix.layer = UILayer; // UI
                        PixelFix.transform.SetParent(gui.transform, false);
                        GUIContainer.SetActive(false);
                        GUIInStart = false;
                    }
                }

                // Get the KeyHints transform for this scene and create new KeyHint objects
                KeyHintContainer = (RectTransform)root.transform.Find("GUI/PixelFix/IngameGui(Clone)/HUD/hudroot/KeyHints");
                CreateKeyHints();
            }
        }

        private void CreatePixelFix()
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

        private void CreateKeyHints()
        {
            // Create custom key hints
            Logger.LogInfo($"---- Adding custom key hints ----");

            foreach (var entry in KeyHints)
            {
                // Clone BuildHints and add it under KeyHints to get the position right
                var keyHintObject = GameObject.Instantiate(KeyHintContainer.Find("BuildHints").gameObject, KeyHintContainer, false);
                keyHintObject.name = entry.Key;
                keyHintObject.SetActive(false);

                // Get the Transforms of Keyboard and Gamepad
                var kb = keyHintObject.transform.Find("Keyboard");
                var gp = keyHintObject.transform.Find("Gamepad");

                // Clone vanilla key hint objects and use it as the base for custom key hints
                var baseKey = GameObject.Instantiate(kb.transform.Find("Place").gameObject);
                var baseRotate = GameObject.Instantiate(kb.transform.Find("rotate").gameObject);

                // Destroy all child objects
                foreach (RectTransform child in kb)
                {
                    GameObject.Destroy(child.gameObject);
                }
                foreach (RectTransform child in gp)
                {
                    GameObject.Destroy(child.gameObject);
                }

                foreach (var buttonConfig in entry.Value.ButtonConfigs)
                {
                    string key = ZInput.instance.GetBoundKeyString(buttonConfig.Name);
                    if (key[0].Equals(LocalizationManager.TokenFirstChar))
                    {
                        key = LocalizationManager.Instance.TryTranslate(key);
                    }
                    string hint = LocalizationManager.Instance.TryTranslate(buttonConfig.HintToken);

                    if (string.IsNullOrEmpty(buttonConfig.Axis) || !buttonConfig.Axis.Equals("Mouse ScrollWheel"))
                    {
                        var customObject = GameObject.Instantiate(baseKey, kb, false);
                        customObject.name = buttonConfig.Name;
                        customObject.transform.Find("key_bkg/Key").gameObject.SetText(key);
                        customObject.transform.Find("Text").gameObject.SetText(hint);
                        customObject.SetActive(true);
                    }
                    else
                    {
                        var customObject = GameObject.Instantiate(baseRotate, kb, false);
                        customObject.transform.Find("Text").gameObject.SetText(hint);
                        customObject.SetActive(true);
                    }
                }

                // Add UIInputHint to automatically switch between Keyboard and Gamepad objects
                /*var uihint = hint.AddComponent<UIInputHint>();
                var kb = hint.transform.Find("Keyboard");
                if (kb != null)
                {
                    uihint.m_mouseKeyboardHint = kb.gameObject;
                }
                var gp = hint.transform.Find("GamePad");
                if (gp != null)
                {
                    uihint.m_gamepadHint = gp.gameObject;
                }*/

                Logger.LogInfo($"Added key hints for Item : {entry.Key}");
            }
        }

        /// <summary>
        ///     Add a <see cref="KeyHintConfig"/> to the game.<br />
        ///     Checks if the custom key hint is unique (i.e. the first one registered for an item).<br />
        ///     Custom status effects are displayed in the game instead of the default 
        ///     KeyHints for equipped tools or weapons they are registered for.
        /// </summary>
        /// <param name="hintConfig">The custom key hint config to add.</param>
        /// <returns>true if the custom key hint config was added to the manager.</returns>
        public bool AddKeyHint(KeyHintConfig hintConfig)
        {
            if (hintConfig.Item == null || hintConfig.ButtonConfigs.Length == 0)
            {
                Logger.LogWarning($"Key hint config {hintConfig} is not valid");
                return false;
            }
            if (KeyHints.ContainsKey(hintConfig.Item))
            {
                Logger.LogWarning($"Key hint config for item {hintConfig.Item} already added");
                return false;
            }

            KeyHints.Add(hintConfig.Item, hintConfig);
            return true;
        }

        /// <summary>
        ///     Hook on <see cref="KeyHints.UpdateHints" /> to show custom key hints instead of the vanilla ones.
        /// </summary>
        /// <param name="orig"></param>
        /// <param name="self"></param>
        private void ShowCustomKeyHint(On.KeyHints.orig_UpdateHints orig, KeyHints self)
        {
            orig(self);

            // Needs a localPlayer
            if (Player.m_localPlayer == null)
            {
                return;
            }

            // Needs the current equipped item name
            ItemDrop.ItemData item = null;
            try
            {
                item = Player.m_localPlayer.GetInventory().GetEquipedtems().FirstOrDefault(x => x.IsWeapon() || x.m_shared.m_buildPieces != null);
            }
            catch (Exception) { }
            if (item == null)
            {
                return;
            }
            var prefabName = item.m_dropPrefab?.name;
            if (string.IsNullOrEmpty(prefabName))
            {
                return;
            }

            // First disable all custom key hints
            foreach (RectTransform transform in KeyHintContainer)
            {
                if (KeyHints.ContainsKey(transform.name))
                {
                    transform.gameObject?.SetActive(false);
                }
            }

            // Check if that item has a custom key hint and display it instead the vanilla one
            if (KeyHints.TryGetValue(prefabName, out var keyHint))
            {
                if (keyHint == null)
                {
                    return;
                }

                var hint = KeyHintContainer.Find(keyHint.Item)?.gameObject;
                if (hint == null)
                {
                    return;
                }

                self.m_buildHints.SetActive(false);
                self.m_combatHints.SetActive(false);
                    
                // Update bound keys
                foreach (var buttonConfig in keyHint.ButtonConfigs)
                {
                    string key = ZInput.instance.GetBoundKeyString(buttonConfig.Name);
                    if (key[0].Equals(LocalizationManager.TokenFirstChar))
                    {
                        key = LocalizationManager.Instance.TryTranslate(key);
                    }

                    if (string.IsNullOrEmpty(buttonConfig.Axis) || !buttonConfig.Axis.Equals("Mouse ScrollWheel"))
                    {
                        hint.transform.Find($"Keyboard/{buttonConfig.Name}/key_bkg/Key")?.gameObject?.SetText(key);
                    }
                }

                hint.SetActive(true);
                hint.GetComponent<UIInputHint>()?.Update();
            }
        }

        /// <summary>
        ///     Create a new button (Valheim style).
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

            var newButton = GameObject.Instantiate(baseButton, parent, false);

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

            FixPixelMultiplier(newButton.GetComponent<Image>());

            return newButton;
        }

        internal void FixPixelMultiplier(Image img)
        {
            img.pixelsPerUnitMultiplier = SceneManager.GetActiveScene().name == "start" ? 1.0f : 2f;
        }

        /// <summary>
        ///     Creates a Valheim style woodpanel
        /// </summary>
        /// <param name="parent"></param>
        /// <param name="anchorMin"></param>
        /// <param name="anchorMax"></param>
        /// <param name="position"></param>
        /// <param name="width"></param>
        /// <param name="height"></param>
        /// <returns></returns>
        public GameObject CreateWoodpanel(Transform parent, Vector2 anchorMin, Vector2 anchorMax, Vector2 position, float width = 0f, float height = 0f)
        {
            var basepanel = PrefabManager.Instance.GetPrefab("BaseWoodpanel");

            if (basepanel == null)
            {
                Logger.LogError("BasePanel is null");
            }

            var newPanel = GameObject.Instantiate(basepanel, parent, false);
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
        ///     Get sprite low level fashion from sactx-2048x2048-Uncompressed-UIAtlas atlas.
        /// </summary>
        /// <param name="rect">Rect on atlas texture</param>
        /// <param name="pivot">pivot</param>
        /// <param name="pixelsPerUnit"></param>
        /// <param name="extrude"></param>
        /// <param name="meshType"></param>
        /// <param name="slice"></param>
        /// <returns>The newly created sprite</returns>
        public Sprite CreateSpriteFromAtlas(Rect rect, Vector2 pivot, float pixelsPerUnit = 50f, uint extrude = 0,
            SpriteMeshType meshType = SpriteMeshType.FullRect, Vector4 slice = new Vector4())
        {
            return Sprite.Create(TextureAtlas, rect, pivot, pixelsPerUnit, extrude, meshType, slice);
        }

        /// <summary>
        ///     Get sprite low level fashion from sactx-2048x2048-Uncompressed-UIAtlas atlas (not much used ingame or old
        ///     textures).
        /// </summary>
        /// <param name="rect">Rect on atlas texture</param>
        /// <param name="pivot">pivot</param>
        /// <param name="pixelsPerUnit"></param>
        /// <param name="extrude"></param>
        /// <param name="meshType"></param>
        /// <param name="slice"></param>
        /// <returns>The newly created sprite</returns>
        public Sprite CreateSpriteFromAtlas2(Rect rect, Vector2 pivot, float pixelsPerUnit = 50f, uint extrude = 0,
            SpriteMeshType meshType = SpriteMeshType.FullRect, Vector4 slice = new Vector4())
        {
            return Sprite.Create(TextureAtlas2, rect, pivot, pixelsPerUnit, extrude, meshType, slice);
        }

        /// <summary>
        ///     Get a sprite by name.
        /// </summary>
        /// <param name="spriteName"></param>
        /// <returns>The sprite with given name</returns>
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
        ///     Apply Valheim style to an inputfield component.
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
        ///     Apply Valheim style to a toggle.
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

        /// <summary>
        /// Create a Gameobject with a Text (and optional Outline and ContentSizeFitter) component
        /// </summary>
        /// <param name="text">Text to show</param>
        /// <param name="parent">Parent transform</param>
        /// <param name="anchorMin">Anchor min</param>
        /// <param name="anchorMax">Anchor max</param>
        /// <param name="position">Anchored position</param>
        /// <param name="font">Font</param>
        /// <param name="fontSize">Font size</param>
        /// <param name="color">Font color</param>
        /// <param name="outline">Add outline component</param>
        /// <param name="outlineColor">Outline color</param>
        /// <param name="width">Width</param>
        /// <param name="height">Height</param>
        /// <param name="addContentSizeFitter">Add ContentSizeFitter</param>
        /// <returns></returns>
        public GameObject CreateText(string text, Transform parent, Vector2 anchorMin, Vector2 anchorMax, Vector2 position, Font font, int fontSize, Color color, bool outline, Color outlineColor, float width, float height, bool addContentSizeFitter)
        {
            GameObject go = new GameObject("Text", typeof(RectTransform), typeof(Text));
            if (outline)
            {
                go.AddComponent<Outline>().effectColor = outlineColor;
            }

            go.GetComponent<RectTransform>().anchorMin = anchorMin;
            go.GetComponent<RectTransform>().anchorMax = anchorMax;
            go.GetComponent<RectTransform>().anchoredPosition = position;
            if (!addContentSizeFitter)
            {
                go.GetComponent<RectTransform>().SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal, width);
                go.GetComponent<RectTransform>().SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical, height);
            }
            else
            {
                go.AddComponent<ContentSizeFitter>().verticalFit = ContentSizeFitter.FitMode.PreferredSize;
            }
            go.GetComponent<Text>().font = font;
            go.GetComponent<Text>().color = color;
            go.GetComponent<Text>().fontSize = fontSize;
            go.GetComponent<Text>().text = text;

            go.transform.SetParent(parent, false);

            return go;
        }


        /// <summary>
        /// Create a complete scroll view
        /// </summary>
        /// <param name="parent">parent transform</param>
        /// <param name="showHorizontalScrollbar">show horizontal scrollbar</param>
        /// <param name="showVerticalScrollbar">show vertical scrollbar</param>
        /// <param name="handleSize">size of the handle</param>
        /// <param name="handleDistanceToBorder"></param>
        /// <param name="handleColors">Colorblock for the handle</param>
        /// <param name="slidingAreaBackgroundColor">Background color for the sliding area</param>
        /// <param name="width">rect width</param>
        /// <param name="height">rect height</param>
        /// <returns></returns>
        public GameObject CreateScrollView(Transform parent, bool showHorizontalScrollbar, bool showVerticalScrollbar, float handleSize, float handleDistanceToBorder, ColorBlock handleColors, Color slidingAreaBackgroundColor, float width, float height)
        {

            GameObject canvas = new GameObject("Canvas", typeof(RectTransform), /*typeof(Canvas), */typeof(CanvasGroup), typeof(GraphicRaycaster));
            canvas.GetComponent<Canvas>().sortingOrder = 0;
            canvas.GetComponent<RectTransform>().anchorMin = new Vector2(0.5f, 0.5f);
            canvas.GetComponent<RectTransform>().anchorMax = new Vector2(0.5f, 0.5f);
            canvas.GetComponent<RectTransform>().pivot = new Vector2(0.5f, 0.5f);
            canvas.GetComponent<RectTransform>().localPosition = new Vector3(0, 0, 0);
            canvas.GetComponent<RectTransform>().position = new Vector3(0, 0, 0);
            canvas.GetComponent<RectTransform>().anchoredPosition = new Vector2(0, 0);

            canvas.GetComponent<CanvasGroup>().interactable = true;
            canvas.GetComponent<CanvasGroup>().ignoreParentGroups = true;
            canvas.GetComponent<CanvasGroup>().blocksRaycasts = true;
            canvas.GetComponent<CanvasGroup>().alpha = 1f;

            //canvas.transform.localScale = new Vector3(2,2,2);
            canvas.GetComponent<RectTransform>().SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal, width);
            canvas.GetComponent<RectTransform>().SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical, height);


            canvas.transform.SetParent(parent, false);

            // Create scrollView
            GameObject scrollView = new GameObject("Scroll View", typeof(Image), typeof(ScrollRect), typeof(Mask)).SetUpperRight();
            //scrollView.GetComponent<RectTransform>().anchoredPosition = new Vector2(0f, 0);
            scrollView.GetComponent<RectTransform>().SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal, width);
            scrollView.GetComponent<RectTransform>().SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical, height);

            scrollView.GetComponent<Image>().color = new Color(0, 0, 0, 1f);

            scrollView.GetComponent<ScrollRect>().horizontal = showHorizontalScrollbar;
            scrollView.GetComponent<ScrollRect>().vertical = showVerticalScrollbar;
            scrollView.GetComponent<ScrollRect>().horizontalScrollbarVisibility = ScrollRect.ScrollbarVisibility.AutoHide;
            scrollView.GetComponent<ScrollRect>().verticalScrollbarVisibility = ScrollRect.ScrollbarVisibility.AutoHide;
            scrollView.GetComponent<ScrollRect>().scrollSensitivity = 35f;

            scrollView.GetComponent<Mask>().showMaskGraphic = false;

            scrollView.transform.SetParent(canvas.transform, false);

            // Create viewport
            GameObject viewPort = new GameObject("Viewport", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
            viewPort.GetComponent<RectTransform>().pivot = new Vector2(0, 1);
            viewPort.GetComponent<RectTransform>().anchorMin = new Vector2(0, 1);
            viewPort.GetComponent<RectTransform>().anchorMax = new Vector2(0, 1);
            viewPort.GetComponent<RectTransform>().pivot = new Vector2(0, 1);
            viewPort.GetComponent<RectTransform>().SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal, width);
            viewPort.GetComponent<RectTransform>().SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical, height);
            viewPort.GetComponent<Image>().color = new Color(0, 0, 0, 0);

            viewPort.transform.SetParent(scrollView.transform, false);

            scrollView.GetComponent<ScrollRect>().viewport = viewPort.GetComponent<RectTransform>();

            if (showHorizontalScrollbar)
            {
                // Create Horizontal scroll bar
                GameObject horizontalScrollbar = new GameObject("Scrollbar horizontal", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image),
                    typeof(Scrollbar));

                horizontalScrollbar.transform.SetParent(scrollView.transform, false);
                scrollView.GetComponent<ScrollRect>().horizontalScrollbar = horizontalScrollbar.GetComponent<Scrollbar>();

                horizontalScrollbar.GetComponent<RectTransform>().anchorMin = new Vector2(0.5f, 1f);
                horizontalScrollbar.GetComponent<RectTransform>().anchorMax = new Vector2(0.5f, 1f);
                horizontalScrollbar.GetComponent<RectTransform>().pivot = new Vector2(0.5f, 1f);
                horizontalScrollbar.GetComponent<RectTransform>().anchoredPosition = new Vector2(-handleSize / 2f, -height + handleSize);
                horizontalScrollbar.GetComponent<RectTransform>().SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal, width - 2f * handleDistanceToBorder - handleSize);
                horizontalScrollbar.GetComponent<RectTransform>().SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical, handleSize);

                horizontalScrollbar.GetComponent<Image>().color = slidingAreaBackgroundColor;

                horizontalScrollbar.GetComponent<Scrollbar>().colors = handleColors;


                GameObject slidingArea = new GameObject("Sliding Area", typeof(RectTransform));
                slidingArea.GetComponent<RectTransform>().anchorMin = new Vector2(0, 1);
                slidingArea.GetComponent<RectTransform>().anchorMax = new Vector2(0, 1);
                slidingArea.GetComponent<RectTransform>().pivot = new Vector2(0, 0.5f);
                slidingArea.GetComponent<RectTransform>().anchoredPosition = new Vector2(0, 0);
                slidingArea.GetComponent<RectTransform>().SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal, width - 2f * handleDistanceToBorder - handleSize);
                slidingArea.GetComponent<RectTransform>().SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical, handleSize);

                slidingArea.transform.SetParent(horizontalScrollbar.transform, false);

                GameObject handle = new GameObject("Handle", typeof(RectTransform), typeof(Image));
                handle.GetComponent<RectTransform>().anchoredPosition = new Vector2(0, -handleSize / 2f);
                handle.transform.SetParent(slidingArea.transform, false);
                handle.GetComponent<RectTransform>().SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical, handleSize / 2f);
                handle.GetComponent<RectTransform>().SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal, handleSize / 2f);
                handle.GetComponent<Image>().sprite = GetSprite("UISprite");
                handle.GetComponent<Image>().type = Image.Type.Sliced;


                horizontalScrollbar.GetComponent<Scrollbar>().size = 0.4f;
                horizontalScrollbar.GetComponent<Scrollbar>().handleRect = handle.GetComponent<RectTransform>();
                horizontalScrollbar.GetComponent<Scrollbar>().targetGraphic = handle.GetComponent<Image>();
                horizontalScrollbar.GetComponent<Scrollbar>().direction = Scrollbar.Direction.LeftToRight;
            }

            if (showVerticalScrollbar)
            {
                // Create Vertical scroll bar
                GameObject verticalScrollbar = new GameObject("Scrollbar Vertical", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image),
                    typeof(Scrollbar));

                verticalScrollbar.transform.SetParent(scrollView.transform, false);
                scrollView.GetComponent<ScrollRect>().verticalScrollbar = verticalScrollbar.GetComponent<Scrollbar>();

                verticalScrollbar.GetComponent<RectTransform>().anchorMin = new Vector2(1f, 0.5f);
                verticalScrollbar.GetComponent<RectTransform>().anchorMax = new Vector2(1f, 0.5f);
                verticalScrollbar.GetComponent<RectTransform>().pivot = new Vector2(1f, 0.5f);
                verticalScrollbar.GetComponent<RectTransform>().anchoredPosition = new Vector2(0, -handleSize / 2f);
                verticalScrollbar.GetComponent<RectTransform>().SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal, handleSize);
                verticalScrollbar.GetComponent<RectTransform>().SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical, height - 2f * handleDistanceToBorder - handleSize);

                verticalScrollbar.GetComponent<Image>().color = slidingAreaBackgroundColor;

                verticalScrollbar.GetComponent<Scrollbar>().colors = handleColors;

                GameObject slidingArea = new GameObject("Sliding Area", typeof(RectTransform));
                slidingArea.GetComponent<RectTransform>().anchorMin = new Vector2(0, 1);
                slidingArea.GetComponent<RectTransform>().anchorMax = new Vector2(0, 1);
                slidingArea.GetComponent<RectTransform>().pivot = new Vector2(0.5f, 1f);
                slidingArea.GetComponent<RectTransform>().anchoredPosition = new Vector2(0, 0);
                slidingArea.GetComponent<RectTransform>().SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal, handleSize);
                slidingArea.GetComponent<RectTransform>().SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical, height - 2f * handleDistanceToBorder - handleSize);

                slidingArea.transform.SetParent(verticalScrollbar.transform, false);

                GameObject handle = new GameObject("Handle", typeof(RectTransform), typeof(Image));
                handle.GetComponent<RectTransform>().anchorMin = new Vector2(0.5f, 0.5f);
                handle.GetComponent<RectTransform>().anchorMax = new Vector2(0.5f, 0.5f);
                handle.GetComponent<RectTransform>().anchoredPosition = new Vector2(handleSize / 2f, 0);
                handle.transform.SetParent(slidingArea.transform, false);
                handle.GetComponent<RectTransform>().SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical, handleSize / 2f);
                handle.GetComponent<RectTransform>().SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal, handleSize / 2f);
                handle.GetComponent<Image>().sprite = GetSprite("UISprite");
                handle.GetComponent<Image>().type = Image.Type.Sliced;
                verticalScrollbar.GetComponent<Scrollbar>().size = 0.4f;


                verticalScrollbar.GetComponent<Scrollbar>().handleRect = handle.GetComponent<RectTransform>();
                verticalScrollbar.GetComponent<Scrollbar>().targetGraphic = handle.GetComponent<Image>();
                verticalScrollbar.GetComponent<Scrollbar>().size = handleSize;
                verticalScrollbar.GetComponent<Scrollbar>().direction = Scrollbar.Direction.BottomToTop;
                verticalScrollbar.GetComponent<Scrollbar>().SetValueWithoutNotify(1f);
            }

            GameObject content = new GameObject("Content", typeof(RectTransform), typeof(VerticalLayoutGroup), typeof(Canvas), typeof(GraphicRaycaster), typeof(ContentSizeFitter));

            content.GetComponent<Canvas>().planeDistance = 5.2f;
            content.GetComponent<RectTransform>().anchorMin = new Vector2(0, 1);
            content.GetComponent<RectTransform>().anchorMax = new Vector2(0, 1);
            content.GetComponent<RectTransform>().pivot = new Vector2(0, 0);
            content.GetComponent<RectTransform>().SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal, width - 2 * handleDistanceToBorder - handleSize);
            content.GetComponent<RectTransform>().SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical, height - 2 * handleDistanceToBorder - handleSize);

            content.GetComponent<VerticalLayoutGroup>().childAlignment = TextAnchor.UpperLeft;
            content.GetComponent<VerticalLayoutGroup>().childForceExpandWidth = true;
            content.GetComponent<VerticalLayoutGroup>().childForceExpandHeight = false;
            content.GetComponent<VerticalLayoutGroup>().childControlHeight = true;
            content.GetComponent<VerticalLayoutGroup>().childControlWidth = showHorizontalScrollbar;

            content.transform.SetParent(viewPort.transform, false);

            content.GetComponent<ContentSizeFitter>().verticalFit = ContentSizeFitter.FitMode.PreferredSize;

            scrollView.GetComponent<ScrollRect>().content = content.GetComponent<RectTransform>();

            return canvas;
        }

        /// <summary>
        /// Create toggle field
        /// </summary>
        /// <param name="parent"></param>
        /// <param name="position"></param>
        /// <param name="width"></param>
        /// <param name="height"></param>
        /// <returns></returns>
        public GameObject CreateToggle(Transform parent, Vector2 position, float width, float height)
        {
            GameObject toggle = new GameObject("Toggle", typeof(RectTransform), typeof(Toggle), typeof(LayoutElement)).SetUpperLeft().SetSize(width, height);
            toggle.GetComponent<LayoutElement>().preferredWidth = width;
            toggle.transform.SetParent(parent, false);


            GameObject background = new GameObject("Background", typeof(RectTransform), typeof(Image)).SetUpperRight().SetSize(28f, 28f);
            background.GetComponent<Image>().pixelsPerUnitMultiplier = 2f;
            background.transform.SetParent(toggle.transform, false);


            Sprite checkBoxSprite = GetSprite("checkbox");
            background.GetComponent<Image>().sprite = checkBoxSprite;
            background.GetComponent<Image>().type = Image.Type.Simple;

            toggle.GetComponent<Toggle>().toggleTransition = Toggle.ToggleTransition.Fade;

            GameObject checkMark = new GameObject("Checkmark", typeof(RectTransform), typeof(Image));
            checkMark.GetComponent<RectTransform>().anchoredPosition = new Vector2(0, 0);
            checkMark.SetSize(28f, 28f);
            checkMark.GetComponent<Image>().pixelsPerUnitMultiplier = 2f;
            checkMark.transform.SetParent(background.transform, false);

            toggle.GetComponent<Toggle>().graphic = checkMark.GetComponent<Image>();
            toggle.GetComponent<Toggle>().targetGraphic = background.GetComponent<Image>();


            ApplyToogleStyle(toggle.GetComponent<Toggle>());
            checkMark.GetComponent<Image>().type = Image.Type.Simple;

            return toggle;
        }

        /// <summary>
        /// Create key binding field
        /// </summary>
        /// <param name="text"></param>
        /// <param name="parent"></param>
        /// <param name="width"></param>
        /// <param name="height"></param>
        /// <returns></returns>
        public GameObject CreateKeyBindField(string text, Transform parent, float width, float height)
        {
            GameObject input = new GameObject("KeyBinding", typeof(RectTransform), typeof(LayoutElement)).SetUpperLeft().SetSize(width, height);
            input.GetComponent<LayoutElement>().preferredWidth = width;


            GameObject label = CreateText(text, input.transform, new Vector2(0, 0), new Vector2(0, 0), new Vector2(0, 0), AveriaSerifBold, 16, ValheimOrange, true,
                Color.black, width - 150f, 0f, false);
            label.SetUpperLeft().SetToTextHeight();
            input.SetHeight(label.GetHeight());

            input.transform.SetParent(parent, false);

            GameObject button = new GameObject("Button", typeof(RectTransform), typeof(Image), typeof(Button)).SetUpperRight().SetSize(100f, label.GetHeight());

            button.transform.SetParent(input.transform, false);
            button.GetComponent<Button>().image = button.GetComponent<Image>();
            button.GetComponent<Image>().sprite = CreateSpriteFromAtlas(new Rect(0, 2048 - 156, 139, 36), new Vector2(0.5f, 0.5f), 50f, 0, SpriteMeshType.FullRect,
                new Vector4(5, 5, 5, 5));

            var bindString = new GameObject("Text", typeof(RectTransform), typeof(Text), typeof(Outline)).SetMiddleCenter();
            bindString.GetComponent<Text>().text = "";
            bindString.GetComponent<Text>().font = AveriaSerifBold;
            bindString.GetComponent<Text>().color = new Color(1, 1, 1, 1);

            bindString.SetToTextHeight().SetWidth(button.GetComponent<RectTransform>().rect.width);
            bindString.SetMiddleLeft().GetComponent<Text>().alignment = TextAnchor.MiddleCenter;

            button.SetHeight(bindString.GetHeight() + 4f);

            bindString.transform.SetParent(button.transform, false);

            return input;
        }
    }
}
