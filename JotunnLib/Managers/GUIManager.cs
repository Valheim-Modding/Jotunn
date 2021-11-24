using System;
using System.Linq;
using Jotunn.Configs;
using Jotunn.GUI;
using Jotunn.Utils;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.SceneManagement;
using UnityEngine.U2D;
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
    public class GUIManager : IManager
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
        ///     Hide .ctor
        /// </summary>
        private GUIManager() {}

        /// <summary>
        ///     Event that gets fired every time the Unity scene changed and a new PixelFix
        ///     object was created. Subscribe to this event to create your custom GUI objects
        ///     and add them as a child to the <see cref="PixelFix"/>.
        /// </summary>
        [Obsolete("Use OnCustomGUIAvailable")]
        public static event Action OnPixelFixCreated;

        /// <summary>
        ///     GUI container with automatic scaling for high res displays.
        ///     Gets rebuild at every scene change so make sure to add your custom
        ///     GUI prefabs again on each scene change.
        /// </summary>
        [Obsolete("Use CustomGUIFront or CustomGUIBack")]
        public static GameObject PixelFix { get; private set; }

        /// <summary>
        ///     Event that gets fired every time the Unity scene changed and new CustomGUI 
        ///     objects were created. Subscribe to this event to create your custom GUI objects
        ///     and add them as a child to either <see cref="CustomGUIFront"/> or <see cref="CustomGUIBack"/>.
        /// </summary>
        public static event Action OnCustomGUIAvailable;

        /// <summary>
        ///     GUI container in front of Valheim's GUI elements with automatic scaling for
        ///     high res displays and pixel correction.
        ///     Gets rebuild at every scene change so make sure to add your custom
        ///     GUI prefabs again on each scene change.
        /// </summary>
        public static GameObject CustomGUIFront { get; private set; }

        /// <summary>
        ///     GUI container behind Valheim's GUI elements with automatic scaling for
        ///     high res displays and pixel correction.
        ///     Gets rebuild at every scene change so make sure to add your custom
        ///     GUI prefabs again on each scene change.
        /// </summary>
        public static GameObject CustomGUIBack { get; private set; }

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
        public ColorBlock ValheimScrollbarHandleColorBlock = new ColorBlock
        {
            normalColor = new Color(0.926f, 0.645f, 0.34f, 1f),
            highlightedColor = new Color(1f, 0.786f, 0.088f, 1f),
            pressedColor = new Color(0.838f, 0.647f, 0.03f, 1f),
            selectedColor = new Color(1f, 0.786f, 0.088f, 1f),
            disabledColor = new Color(0.784f, 0.784f, 0.784f, 0.502f),
            colorMultiplier = 1f,
            fadeDuration = 0.1f
        };

        /// <summary>
        ///     Toggle color block in Valheim style.
        /// </summary>
        public ColorBlock ValheimToggleColorBlock = new ColorBlock
        {
            normalColor = new Color(0.61f, 0.61f, 0.61f, 1f),
            highlightedColor = new Color(1f, 1f, 1f, 1f),
            pressedColor = new Color(0.784f, 0.784f, 0.784f, 1f),
            selectedColor = new Color(1f, 1f, 1f, 1f),
            disabledColor = new Color(0.784f, 0.784f, 0.784f, 0.502f),
            colorMultiplier = 1f,
            fadeDuration = 0.1f
        };

        /// <summary>
        ///     Button color block in Valheim style
        /// </summary>
        public ColorBlock ValheimButtonColorBlock = new ColorBlock
        {
            normalColor = new Color(0.824f, 0.824f, 0.824f, 1f),
            highlightedColor = new Color(1.3f, 1.3f, 1.3f, 1f),
            pressedColor = new Color(0.537f, 0.556f, 0.556f, 1f),
            selectedColor = new Color(0.824f, 0.824f, 0.824f, 1f),
            disabledColor = new Color(0.566f, 0.566f, 0.566f, 0.502f),
            colorMultiplier = 1f,
            fadeDuration = 0.1f
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
        ///     <see cref="DefaultControls.Resources"/> with default Valheim assets.
        /// </summary>
        public DefaultControls.Resources ValheimControlResources;

        /// <summary>
        ///     SpriteAtlas holding the references to the sprites used in the helper methods.
        /// </summary>
        private SpriteAtlas UIAtlas;

        /// <summary>
        ///     SpriteAtlas holding the references to the sprites used in the helper methods.
        /// </summary>
        private SpriteAtlas IconAtlas;

        /// <summary>
        ///     Indicates if the PixelFix must be created for the start or main scene.
        /// </summary>
        private bool GUIInStart;

        /// <summary>
        ///     Cache headless state
        /// </summary>
        private static bool Headless;

        /// <summary>
        ///     Detect headless mode (aka dedicated server)
        /// </summary>
        /// <returns></returns>
        public static bool IsHeadless()
        {
            return Headless;
        }

        /// <summary>
        ///     Global indicator if the input is currently blocked by the GUIManager.
        /// </summary>
        internal static bool InputBlocked;

        /// <summary>
        ///     Counter to track multiple block requests.
        /// </summary>
        private static int InputBlockRequests;
        
        /// <summary>
        ///     Block all input except GUI
        /// </summary>
        /// <param name="state">Indicator if the input should be blocked or released</param>
        public static void BlockInput(bool state)
        {
            if (!IsHeadless() && SceneManager.GetActiveScene().name == "main")
            {
                if (state)
                {
                    ++InputBlockRequests;
                }
                else
                {
                    InputBlockRequests = Math.Max(--InputBlockRequests, 0);
                }

                if (!InputBlocked && InputBlockRequests > 0)
                {
                    EnableInputBlock();
                }
                if (InputBlocked && InputBlockRequests == 0)
                {
                    ResetInputBlock();
                }

            }
        }

        /// <summary>
        ///     Enable the InputBlock
        /// </summary>
        private static void EnableInputBlock()
        {
            InputBlocked = true;

            On.Player.TakeInput += Player_TakeInput;
            On.PlayerController.TakeInput += PlayerController_TakeInput;
            On.Menu.IsVisible += Menu_IsVisible;

            if (GameCamera.instance)
            {
                GameCamera.instance.m_mouseCapture = false;
                GameCamera.instance.UpdateMouseCapture();
            }
        }

        /// <summary>
        ///     Reset the InputBlock to its initial state (disabled)
        /// </summary>
        private static void ResetInputBlock()
        {
            InputBlocked = false;
            InputBlockRequests = 0;

            On.Player.TakeInput -= Player_TakeInput;
            On.PlayerController.TakeInput -= PlayerController_TakeInput;
            On.Menu.IsVisible -= Menu_IsVisible;

            if (GameCamera.instance)
            {
                GameCamera.instance.m_mouseCapture = true;
                GameCamera.instance.UpdateMouseCapture();
            }
        }

        private static bool PlayerController_TakeInput(On.PlayerController.orig_TakeInput orig, PlayerController self)
        {
            orig(self);
            return false;
        }
        private static bool Player_TakeInput(On.Player.orig_TakeInput orig, Player self)
        {
            orig(self);
            return false;
        }
        private static bool Menu_IsVisible(On.Menu.orig_IsVisible orig)
        {
            orig();
            return true;
        }

        /// <summary>
        ///     Initialize the manager
        /// </summary>
        public void Init()
        {
            // Cache headless state
            Headless = SystemInfo.graphicsDeviceType == GraphicsDeviceType.Null;

            // Dont init on a headless server
            if (!IsHeadless())
            {
                SceneManager.sceneLoaded += InitialLoad;
                SceneManager.sceneLoaded += SceneManager_sceneLoaded;
            }
        }

        /// <summary>
        ///     Load GUI assets on first start
        /// </summary>
        /// <param name="scene"></param>
        /// <param name="loadMode"></param>
        internal void InitialLoad(Scene scene, LoadSceneMode loadMode)
        {
            // Load valheim GUI assets
            if (scene.name == "start")
            {
                try
                {
                    SpriteAtlas[] atlas = Resources.FindObjectsOfTypeAll<SpriteAtlas>();

                    UIAtlas = atlas.FirstOrDefault(x => x.name.Equals("UIAtlas"));
                    if (UIAtlas == null)
                    {
                        throw new Exception("UIAtlas not found");
                    }

                    IconAtlas = atlas.FirstOrDefault(x => x.name.Equals("IconAtlas"));
                    if (IconAtlas == null)
                    {
                        throw new Exception("IconAtlas not found");
                    }

                    // Fonts
                    Font[] fonts = Resources.FindObjectsOfTypeAll<Font>();
                    AveriaSerif = fonts.FirstOrDefault(x => x.name == "AveriaSerifLibre-Regular");
                    AveriaSerifBold = fonts.FirstOrDefault(x => x.name == "AveriaSerifLibre-Bold");
                    if (AveriaSerifBold == null || AveriaSerif == null)
                    {
                        throw new Exception("Fonts not found");
                    }

                    // DefaultControls.Resources pack
                    AssetBundle jotunnBundle = AssetUtils.LoadAssetBundleFromResources("jotunn", typeof(Main).Assembly);
                    GameObject stub = jotunnBundle.LoadAsset<GameObject>("UIMaskStub");

                    ValheimControlResources.standard = GetSprite("button");
                    ValheimControlResources.background = GetSprite("text_field");
                    ValheimControlResources.inputField = GetSprite("text_field");
                    ValheimControlResources.knob = GetSprite("checkbox_marker");
                    ValheimControlResources.checkmark = GetSprite("checkbox_marker");
                    ValheimControlResources.dropdown = GetSprite("checkbox_marker");
                    ValheimControlResources.mask = stub.GetComponent<Image>().sprite;

                    jotunnBundle.Unload(false);

                    // Color and Gradient picker
                    AssetBundle colorWheelBundle = AssetUtils.LoadAssetBundleFromResources("colorpicker", typeof(Main).Assembly);

                    // ColorPicker prefab
                    GameObject colorPicker = colorWheelBundle.LoadAsset<GameObject>("ColorPicker");

                    // Setting some vanilla styles
                    Image colorImage = colorPicker.GetComponent<Image>();
                    colorImage.sprite = GetSprite("woodpanel_settings");
                    colorImage.type = Image.Type.Sliced;
                    colorImage.pixelsPerUnitMultiplier = GUIInStart ? 2f : 1f;
                    colorImage.material = PrefabManager.Cache.GetPrefab<Material>("litpanel");
                    foreach (Text pickerTxt in colorPicker.GetComponentsInChildren<Text>(true))
                    {
                        ApplyTextStyle(pickerTxt, ValheimOrange, pickerTxt.fontSize);
                    }
                    foreach (InputField pickerInput in colorPicker.GetComponentsInChildren<InputField>(true))
                    {
                        ApplyInputFieldStyle(pickerInput, 13);
                    }
                    foreach (Button pickerButton in colorPicker.GetComponentsInChildren<Button>(true))
                    {
                        ApplyButtonStyle(pickerButton, 13);
                    }

                    PrefabManager.Instance.AddPrefab(colorPicker, Main.Instance.Info.Metadata);

                    // GradientPicker prefab
                    GameObject gradientPicker = colorWheelBundle.LoadAsset<GameObject>("GradientPicker");

                    // Setting some vanilla styles
                    Image gradientImage = gradientPicker.GetComponent<Image>();
                    gradientImage.sprite = GetSprite("woodpanel_settings");
                    gradientImage.type = Image.Type.Sliced;
                    gradientImage.pixelsPerUnitMultiplier = GUIInStart ? 2f : 1f;
                    gradientImage.material = PrefabManager.Cache.GetPrefab<Material>("litpanel");
                    foreach (Text pickerTxt in gradientPicker.GetComponentsInChildren<Text>(true))
                    {
                        ApplyTextStyle(pickerTxt, ValheimOrange, pickerTxt.fontSize);
                    }
                    foreach (InputField pickerInput in gradientPicker.GetComponentsInChildren<InputField>(true))
                    {
                        ApplyInputFieldStyle(pickerInput, 13);
                    }
                    foreach (Button pickerButton in gradientPicker.GetComponentsInChildren<Button>(true))
                    {
                        if (pickerButton.name != "ColorButton")
                        {
                            ApplyButtonStyle(pickerButton, 13);
                        }
                    }

                    PrefabManager.Instance.AddPrefab(gradientPicker, Main.Instance.Info.Metadata);

                    colorWheelBundle.Unload(false);
                }
                catch (Exception ex)
                {
                    Logger.LogError(ex);
                }
                finally
                {
                    SceneManager.sceneLoaded -= InitialLoad;
                }
            }
        }

        private void SceneManager_sceneLoaded(Scene scene, LoadSceneMode loadMode)
        {
            if (scene.name == "start")
            {
                GameObject root = SceneManager.GetActiveScene().GetRootGameObjects().FirstOrDefault(x => x.name == "GuiRoot");
                Transform gui = root?.transform.Find("GUI");
                if (!gui)
                {
                    Logger.LogWarning("GuiRoot GUI not found, not creating custom GUI");
                    return;
                }
                CreateCustomGUI(gui);

                ResetInputBlock();

                GUIInStart = true;
            }
            if (scene.name == "main")
            {
                GameObject root = SceneManager.GetActiveScene().GetRootGameObjects().FirstOrDefault(x => x.name == "_GameMain");
                Transform gui = root?.transform.Find("GUI");
                if (!gui)
                {
                    Logger.LogWarning("_GameMain GUI not found, not creating custom GUI");
                    return;
                }
                CreateCustomGUI(gui);

                ResetInputBlock();

                GUIInStart = false;
            }
        }

        /// <summary>
        ///     Create GameObjects for mods to append their custom GUI to
        /// </summary>
        /// <param name="parent"></param>
        private void CreateCustomGUI(Transform parent)
        {
            CustomGUIFront = new GameObject("CustomGUIFront", typeof(RectTransform), typeof(GuiPixelFix));
            CustomGUIFront.layer = UILayer;
            CustomGUIFront.transform.SetParent(parent.transform, false);
            CustomGUIFront.transform.SetAsLastSibling();
            CustomGUIFront.GetComponent<RectTransform>().anchorMin = Vector2.zero;
            CustomGUIFront.GetComponent<RectTransform>().anchorMax = Vector2.one;

#pragma warning disable CS0618 // Type or member is obsolete
            PixelFix = CustomGUIFront;
#pragma warning restore CS0618 // Type or member is obsolete

            CustomGUIBack = new GameObject("CustomGUIBack", typeof(RectTransform), typeof(GuiPixelFix));
            CustomGUIBack.layer = UILayer;
            CustomGUIBack.transform.SetParent(parent.transform, false);
            CustomGUIBack.transform.SetAsFirstSibling();
            CustomGUIBack.GetComponent<RectTransform>().anchorMin = Vector2.zero;
            CustomGUIBack.GetComponent<RectTransform>().anchorMax = Vector2.one;

#pragma warning disable CS0612 // Type or member is obsolete
            InvokeOnPixelFixCreated();
#pragma warning restore CS0612 // Type or member is obsolete

            InvokeOnCustomGUIAvailable();
        }

        [Obsolete]
        private void InvokeOnPixelFixCreated()
        {
            OnPixelFixCreated?.SafeInvoke();
        }

        private void InvokeOnCustomGUIAvailable()
        {
            OnCustomGUIAvailable?.SafeInvoke();
        }

        /// <summary>
        ///     Add a <see cref="KeyHintConfig"/> to the manager.<br />
        ///     Checks if the custom key hint is unique (i.e. the first one registered for an item).<br />
        ///     Custom status effects are displayed in the game instead of the default 
        ///     KeyHints for equipped tools or weapons they are registered for.
        /// </summary>
        /// <param name="hintConfig">The custom key hint config to add.</param>
        /// <returns>true if the custom key hint config was added to the manager.</returns>
        [Obsolete("Use KeyHintManager.AddKeyHint instead")]
        public bool AddKeyHint(KeyHintConfig hintConfig)
        {
            return KeyHintManager.Instance.AddKeyHint(hintConfig);
        }

        /// <summary>
        ///     Removes a <see cref="KeyHintConfig"/> from the game.
        /// </summary>
        /// <param name="hintConfig">The custom key hint config to add.</param>
        [Obsolete("Use KeyHintManager.RemoveKeyHint instead")]
        public void RemoveKeyHint(KeyHintConfig hintConfig)
        {
            KeyHintManager.Instance.RemoveKeyHint(hintConfig);
        }

        /// <summary>
        ///     Get a sprite by name.
        /// </summary>
        /// <param name="spriteName">The sprite name</param>
        /// <returns>The sprite with given name</returns>
        public Sprite GetSprite(string spriteName)
        {
            Sprite ret = UIAtlas?.GetSprite(spriteName);
            if (ret != null)
            {
                return ret;
            }

            ret = IconAtlas?.GetSprite(spriteName);
            if (ret != null)
            {
                return ret;
            }

            ret = PrefabManager.Cache.GetPrefab<Sprite>(spriteName);
            if (ret != null)
            {
                return ret;
            }

            Logger.LogWarning($"Sprite {spriteName} not found.");

            return null;
        }

        /// <summary>
        ///     Creates and displays a Valheim style ColorPicker
        /// </summary>
        /// <param name="anchorMin">Min anchor on first instantiation</param>
        /// <param name="anchorMax">Max anchor on first instantiation</param>
        /// <param name="position">Position on first instantiation</param>
        /// <param name="original">Color before editing</param>
        /// <param name="message">Display message</param>
        /// <param name="onColorChanged">Event that gets called when the color gets modified</param>
        /// <param name="onColorSelected">Event that gets called when one of the buttons done or cancel get pressed</param>
        /// <param name="useAlpha">When set to false the colors used don't have an alpha channel</param>
        /// <returns></returns>
        public void CreateColorPicker(
            Vector2 anchorMin, Vector2 anchorMax, Vector2 position,
            Color original, string message, ColorPicker.ColorEvent onColorChanged,
            ColorPicker.ColorEvent onColorSelected, bool useAlpha = false)
        {
            if (CustomGUIFront == null)
            {
                Logger.LogError("GUIManager CustomGUIFront is null");
                return;
            }

            GameObject color = PrefabManager.Instance.GetPrefab("ColorPicker");

            if (color == null)
            {
                Logger.LogError("ColorPicker is null");
            }

            if (CustomGUIFront.transform.Find("ColorPicker") == null)
            {
                GameObject newcolor = Object.Instantiate(color, CustomGUIFront.transform, false);
                newcolor.name = "ColorPicker";
                newcolor.GetComponent<Image>().pixelsPerUnitMultiplier = GUIInStart ? 2f : 1f;
                RectTransform tf = newcolor.GetComponent<RectTransform>();
                tf.anchoredPosition = position;
                tf.anchorMin = anchorMin;
                tf.anchorMax = anchorMax;
            }
            ColorPicker.Create(original, message, onColorChanged, onColorSelected, useAlpha);
        }

        /// <summary>
        ///     Creates and displays a Valheim style GradientPicker
        /// </summary>
        /// <param name="anchorMin">Min anchor on first instantiation</param>
        /// <param name="anchorMax">Max anchor on first instantiation</param>
        /// <param name="position">Position on first instantiation</param>
        /// <param name="original">Color before editing</param>
        /// <param name="message">Display message</param>
        /// <param name="onGradientChanged">Event that gets called when the gradient gets modified</param>
        /// <param name="onGradientSelected">Event that gets called when one of the buttons done or cancel gets pressed</param>
        /// <returns></returns>
        public void CreateGradientPicker(
            Vector2 anchorMin, Vector2 anchorMax, Vector2 position,
            Gradient original, string message, GradientPicker.GradientEvent onGradientChanged,
            GradientPicker.GradientEvent onGradientSelected)
        {
            if (CustomGUIFront == null)
            {
                Logger.LogError("GUIManager CustomGUIFront is null");
                return;
            }

            GameObject color = PrefabManager.Instance.GetPrefab("ColorPicker");

            if (color == null)
            {
                Logger.LogError("ColorPicker is null");
            }

            GameObject gradient = PrefabManager.Instance.GetPrefab("GradientPicker");

            if (gradient == null)
            {
                Logger.LogError("GradientPicker is null");
            }

            if (CustomGUIFront.transform.Find("ColorPicker") == null)
            {
                GameObject newcolor = Object.Instantiate(color, CustomGUIFront.transform, false);
                newcolor.name = "ColorPicker";
                newcolor.GetComponent<Image>().pixelsPerUnitMultiplier = GUIInStart ? 2f : 1f;
            }

            if (CustomGUIFront.transform.Find("GradientPicker") == null)
            {
                GameObject newGradient = Object.Instantiate(gradient, CustomGUIFront.transform, false);
                newGradient.name = "GradientPicker";
                newGradient.GetComponent<Image>().pixelsPerUnitMultiplier = GUIInStart ? 2f : 1f;
                RectTransform tf = newGradient.GetComponent<RectTransform>();
                tf.anchoredPosition = position;
                tf.anchorMin = anchorMin;
                tf.anchorMax = anchorMax;
            }
            GradientPicker.Create(original, message, onGradientChanged, onGradientSelected);
        }

        /// <summary>
        ///     Creates a Valheim style woodpanel which is draggable per default
        /// </summary>
        /// <param name="parent">Parent <see cref="Transform"/></param>
        /// <param name="anchorMin">Minimal anchor</param>
        /// <param name="anchorMax">Maximal anchor</param>
        /// <param name="position">Anchored position</param>
        /// <param name="width">Optional width</param>
        /// <param name="height">Optional height</param>
        /// <returns>A <see cref="GameObject"/> as a Valheim style woodpanel</returns>
        public GameObject CreateWoodpanel(Transform parent, Vector2 anchorMin, Vector2 anchorMax, Vector2 position, float width = 0f, float height = 0f)
        {
            return CreateWoodpanel(parent, anchorMin, anchorMax, position, width, height, true);
        }

        /// <summary>
        ///     Creates a Valheim style woodpanel, can optionally be draggable
        /// </summary>
        /// <param name="parent">Parent <see cref="Transform"/></param>
        /// <param name="anchorMin">Minimal anchor</param>
        /// <param name="anchorMax">Maximal anchor</param>
        /// <param name="position">Anchored position</param>
        /// <param name="width">Optional width</param>
        /// <param name="height">Optional height</param>
        /// <param name="draggable">Optional flag if the panel should be draggable (default true)</param>
        /// <returns>A <see cref="GameObject"/> as a Valheim style woodpanel</returns>
        public GameObject CreateWoodpanel(
            Transform parent, Vector2 anchorMin, Vector2 anchorMax, Vector2 position,
            float width = 0f, float height = 0f, bool draggable = true)
        {
            /*var basepanel = PrefabManager.Instance.GetPrefab("BaseWoodpanel");

            if (basepanel == null)
            {
                Logger.LogError("BasePanel is null");
            }

            var newPanel = Object.Instantiate(basepanel, parent, false);*/

            GameObject newPanel = DefaultControls.CreatePanel(ValheimControlResources);
            newPanel.transform.SetParent(parent, false);
            ApplyWoodpanelStyle(newPanel.transform);

            newPanel.GetComponent<Image>().pixelsPerUnitMultiplier = GUIInStart ? 2f : 1f;

            var tf = (RectTransform)newPanel.transform;

            // Set positions and anchors
            tf.anchoredPosition = position;
            tf.anchorMin = anchorMin;
            tf.anchorMax = anchorMax;

            // Set dimensions
            if (width > 0f)
            {
                tf.SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal, width);
            }
            if (height > 0f)
            {
                tf.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical, height);
            }

            // Add draggable component
            if (draggable)
            {
                newPanel.AddComponent<DragWindowCntrl>();
            }

            return newPanel;
        }

        /// <summary>
        ///     Create a complete scroll view
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
        public GameObject CreateScrollView(
            Transform parent, bool showHorizontalScrollbar, bool showVerticalScrollbar, float handleSize,
            float handleDistanceToBorder, ColorBlock handleColors, Color slidingAreaBackgroundColor,
            float width, float height)
        {
            GameObject canvas = new GameObject("Canvas", typeof(RectTransform), typeof(CanvasGroup), typeof(GraphicRaycaster));

            canvas.GetComponent<Canvas>().sortingOrder = 0;
            canvas.GetComponent<RectTransform>().anchorMin = new Vector2(0.5f, 0.5f);
            canvas.GetComponent<RectTransform>().anchorMax = new Vector2(0.5f, 0.5f);
            canvas.GetComponent<RectTransform>().pivot = new Vector2(0.5f, 0.5f);
            canvas.GetComponent<RectTransform>().localPosition = new Vector3(0, 0, 0);
            canvas.GetComponent<RectTransform>().position = new Vector3(0, 0, 0);
            canvas.GetComponent<RectTransform>().anchoredPosition = new Vector2(0, 0);
            canvas.GetComponent<RectTransform>().SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal, width);
            canvas.GetComponent<RectTransform>().SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical, height);

            canvas.GetComponent<CanvasGroup>().interactable = true;
            canvas.GetComponent<CanvasGroup>().ignoreParentGroups = true;
            canvas.GetComponent<CanvasGroup>().blocksRaycasts = true;
            canvas.GetComponent<CanvasGroup>().alpha = 1f;


            canvas.transform.SetParent(parent, false);

            // Create scrollView
            GameObject scrollView = new GameObject("Scroll View", typeof(Image), typeof(ScrollRect), typeof(Mask)).SetUpperRight();
            scrollView.GetComponent<RectTransform>().SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal, width);
            scrollView.GetComponent<RectTransform>().SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical, height);

            scrollView.GetComponent<Image>().color = new Color(0, 0, 0, 1f);

            scrollView.GetComponent<ScrollRect>().horizontal = showHorizontalScrollbar;
            scrollView.GetComponent<ScrollRect>().vertical = showVerticalScrollbar;
            scrollView.GetComponent<ScrollRect>().horizontalScrollbarVisibility = ScrollRect.ScrollbarVisibility.AutoHide;
            scrollView.GetComponent<ScrollRect>().verticalScrollbarVisibility = ScrollRect.ScrollbarVisibility.AutoHideAndExpandViewport;
            scrollView.GetComponent<ScrollRect>().scrollSensitivity = 35f;

            scrollView.GetComponent<Mask>().showMaskGraphic = false;

            scrollView.transform.SetParent(canvas.transform, false);

            // Create viewport
            GameObject viewPort = new GameObject("Viewport", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
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
            content.GetComponent<RectTransform>().anchorMin = new Vector2(0f, 1f);
            content.GetComponent<RectTransform>().anchorMax = new Vector2(0f, 1f);
            content.GetComponent<RectTransform>().pivot = new Vector2(0f, 1f);
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
        ///     Create a <see cref="GameObject"/> with a Text (and optional Outline and ContentSizeFitter) component
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
        /// <returns>A text <see cref="GameObject"/></returns>
        public GameObject CreateText(
            string text, Transform parent, Vector2 anchorMin, Vector2 anchorMax, Vector2 position,
            Font font, int fontSize, Color color, bool outline, Color outlineColor,
            float width, float height, bool addContentSizeFitter)
        {
            GameObject go = new GameObject("Text", typeof(RectTransform), typeof(Text));

            var tf = go.GetComponent<RectTransform>();
            tf.anchorMin = anchorMin;
            tf.anchorMax = anchorMax;
            tf.anchoredPosition = position;
            if (!addContentSizeFitter)
            {
                tf.SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal, width);
                tf.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical, height);
            }
            else
            {
                go.AddComponent<ContentSizeFitter>().verticalFit = ContentSizeFitter.FitMode.PreferredSize;
            }

            var txt = go.GetComponent<Text>();
            txt.text = text;

            ApplyTextStyle(txt, font, color, fontSize, outline);

            if (go.TryGetComponent<Outline>(out var outlineComponent))
            {
                outlineComponent.effectColor = outlineColor;
            }

            go.transform.SetParent(parent, false);

            return go;
        }

        /// <summary>
        ///     Create a new button (Valheim style).
        /// </summary>
        /// <param name="text">Text to display on the button</param>
        /// <param name="parent">Parent transform</param>
        /// <param name="anchorMin">Min anchor</param>
        /// <param name="anchorMax">Max anchor</param>
        /// <param name="position">Position</param>
        /// <param name="width">Set width if > 0</param>
        /// <param name="height">Set height if > 0</param>
        /// <returns>Button GameObject in Valheim style</returns>
        public GameObject CreateButton(
            string text, Transform parent, Vector2 anchorMin, Vector2 anchorMax, Vector2 position,
            float width = 0f, float height = 0f)
        {
            GameObject newButton = DefaultControls.CreateButton(ValheimControlResources);
            newButton.transform.SetParent(parent, false);
            ApplyButtonStyle(newButton.GetComponent<Button>());

            // Set text
            Text txtComponent = newButton.GetComponentInChildren<Text>();
            txtComponent.text = text;

            // Set positions and anchors
            RectTransform tf = newButton.transform as RectTransform;
            tf.anchoredPosition = position;
            tf.anchorMin = anchorMin;
            tf.anchorMax = anchorMax;

            // Optionally set width and height
            if (width > 0f)
            {
                tf.SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal, width);
                txtComponent.GetComponent<RectTransform>().SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal, width);
            }

            if (height > 0f)
            {
                tf.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical, height);
                txtComponent.GetComponent<RectTransform>().SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical, height);
            }

            return newButton;
        }

        /// <summary>
        ///     Create a new InputField (Valheim style).
        /// </summary>
        /// <param name="parent">Parent transform</param>
        /// <param name="anchorMin">Min anchor</param>
        /// <param name="anchorMax">Max anchor</param>
        /// <param name="position">Position</param>
        /// <param name="contentType">Content type for the input field</param>
        /// <param name="placeholderText">Text to display as a placeholder (can be null)</param>
        /// <param name="fontSize">Optional font size, defaults to 16</param>
        /// <param name="width">Set width if > 0</param>
        /// <param name="height">Set height if > 0</param>
        /// <returns>Input field GameObject in Valheim style</returns>
        public GameObject CreateInputField(
            Transform parent, Vector2 anchorMin, Vector2 anchorMax, Vector2 position,
            InputField.ContentType contentType = InputField.ContentType.Standard,
            string placeholderText = null, int fontSize = 16, float width = 0f, float height = 0f)
        {
            GameObject inputField = DefaultControls.CreateInputField(ValheimControlResources);
            inputField.transform.SetParent(parent, false);
            InputField inputComponent = inputField.GetComponent<InputField>();
            ApplyInputFieldStyle(inputComponent, fontSize);

            // Set content type
            inputComponent.contentType = contentType;

            // Set placeholder text and font size
            if (!string.IsNullOrEmpty(placeholderText) && inputComponent.placeholder is Text txt)
            {
                txt.text = placeholderText;
            }

            // Set positions and anchors
            RectTransform tf = inputField.transform as RectTransform;
            tf.anchoredPosition = position;
            tf.anchorMin = anchorMin;
            tf.anchorMax = anchorMax;

            // Optionally set width and height
            if (width > 0f)
            {
                inputField.SetWidth(width);
                inputComponent.placeholder.gameObject.SetWidth(width - 20f);
                inputComponent.textComponent.gameObject.SetWidth(width - 20f);
            }

            if (height > 0f)
            {
                inputField.SetHeight(height);
                inputComponent.placeholder.gameObject.SetHeight(height - 10f);
                inputComponent.textComponent.gameObject.SetHeight(height - 10f);
            }

            return inputField;
        }

        /// <summary>
        ///     Create toggle field
        /// </summary>
        /// <param name="parent">Parent transform</param>
        /// <param name="width">Set width</param>
        /// <param name="height">Set height</param>
        /// <returns></returns>
        public GameObject CreateToggle(Transform parent, float width, float height)
        {
            GameObject toggle = DefaultControls.CreateToggle(ValheimControlResources);
            toggle.transform.SetParent(parent);
            Toggle toggleComponent = toggle.GetComponent<Toggle>();
            ApplyToogleStyle(toggleComponent);

            // Set size
            toggle.SetSize(width, height);
            toggle.transform.Find("Background").gameObject.SetSize(width, height);
            toggle.transform.Find("Background/Checkmark").gameObject.SetSize(width, height);

            return toggle;
        }

        /// <summary>
        ///     Create dropdown field
        /// </summary>
        /// <param name="parent">Parent transform</param>
        /// <param name="anchorMin">Min anchor</param>
        /// <param name="anchorMax">Max anchor</param>
        /// <param name="position">Position</param>
        /// <param name="fontSize">Optional font size, defaults to 16</param>
        /// <param name="width">Set width if > 0</param>
        /// <param name="height">Set height if > 0</param>
        /// <returns></returns>
        public GameObject CreateDropDown(
            Transform parent, Vector2 anchorMin, Vector2 anchorMax, Vector2 position,
            int fontSize = 16, float width = 0f, float height = 0f)
        {
            GameObject dropdown = DefaultControls.CreateDropdown(ValheimControlResources);
            dropdown.transform.SetParent(parent, worldPositionStays: false);
            Dropdown dropdownComponent = dropdown.GetComponent<Dropdown>();
            dropdownComponent.ClearOptions();
            ApplyDropdownStyle(dropdownComponent, fontSize);

            // Set positions and anchors
            RectTransform tf = dropdown.transform as RectTransform;
            tf.anchoredPosition = position;
            tf.anchorMin = anchorMin;
            tf.anchorMax = anchorMax;

            // Optionally set width and height
            if (width > 0f)
            {
                float contentWidth = width - 50f;
                dropdown.SetWidth(width);
                dropdownComponent.captionText.gameObject.SetMiddleLeft();
                dropdownComponent.captionText.gameObject.SetWidth(contentWidth);
                dropdownComponent.captionText.GetComponent<RectTransform>()
                    .anchoredPosition = new Vector3(10f, 0f);
                dropdownComponent.itemText.gameObject.SetWidth(contentWidth);
            }

            if (height > 0f)
            {
                dropdown.SetHeight(height);
                dropdownComponent.captionText.gameObject.SetMiddleLeft();
                dropdownComponent.captionText.gameObject.SetHeight(height);
                dropdownComponent.captionText.GetComponent<RectTransform>()
                    .anchoredPosition = new Vector3(10f, 0f);
                dropdownComponent.itemText.gameObject.SetHeight(height);
            }

            return dropdown;
        }

        /// <summary>
        ///     Create key binding field
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
            label.GetComponent<Text>().verticalOverflow = VerticalWrapMode.Overflow;
            label.SetUpperLeft().SetToTextHeight();
            input.SetHeight(label.GetTextHeight());

            input.transform.SetParent(parent, false);

            GameObject button = new GameObject("Button", typeof(RectTransform), typeof(Image), typeof(Button)).SetUpperRight().SetSize(140f, label.GetTextHeight());

            button.transform.SetParent(input.transform, false);
            button.GetComponent<Button>().image = button.GetComponent<Image>();
            button.GetComponent<Image>().sprite = GetSprite("text_field");

            var bindString = new GameObject("Text", typeof(RectTransform), typeof(Text)).SetMiddleCenter();
            Text textComponent = bindString.GetComponent<Text>();
            ApplyTextStyle(textComponent, textComponent.fontSize);
            textComponent.text = "";

            bindString.SetHeight(bindString.GetTextHeight() + height - 2f).SetWidth(button.GetComponent<RectTransform>().rect.width);
            bindString.SetMiddleLeft().GetComponent<Text>().alignment = TextAnchor.MiddleCenter;

            button.SetHeight(bindString.GetTextHeight() + height);

            bindString.transform.SetParent(button.transform, false);

            return input;
        }

        /// <summary>
        ///     Apply Valheim style to a woodpanel.
        /// </summary>
        /// <param name="woodpanel"></param>
        public void ApplyWoodpanelStyle(Transform woodpanel)
        {
            woodpanel.GetComponent<RectTransform>().anchorMin = new Vector2(0.5f, 0.5f);
            woodpanel.GetComponent<RectTransform>().anchorMax = new Vector2(0.5f, 0.5f);
            woodpanel.GetComponent<RectTransform>().anchoredPosition = new Vector2(0f, 0f);

            woodpanel.GetComponent<Image>().sprite = GetSprite("woodpanel_trophys");
            woodpanel.GetComponent<Image>().type = Image.Type.Sliced;
            woodpanel.GetComponent<Image>().pixelsPerUnitMultiplier = GUIInStart ? 2f : 1f;
            woodpanel.GetComponent<Image>().material = PrefabManager.Cache.GetPrefab<Material>("litpanel");
            woodpanel.GetComponent<Image>().color = Color.white;

            woodpanel.gameObject.layer = UILayer;
        }

        /// <summary>
        ///     Apply Valheim style to a <see cref="Text"/> Component
        /// </summary>
        /// <param name="text">Target component</param>
        /// <param name="font">Own font or <code>GUIManager.Instance.AveriaSerifBold</code>/<code>GUIManager.Instance.AveriaSerif</code></param>
        /// <param name="color">Custom color or <code>GUIManager.Instance.ValheimOrange</code></param>
        /// <param name="createOutline">creates an <see cref="Outline"/> component when true</param>
        /// <param name="fontSize">Optional font size, defaults to 16</param>
        public void ApplyTextStyle(Text text, Font font, Color color, int fontSize = 16, bool createOutline = true)
        {
            text.font = font;
            text.fontSize = fontSize;
            text.color = color;

            if (createOutline)
            {
                Outline outline = text.gameObject.GetOrAddComponent<Outline>();
                outline.effectColor = Color.black;
            }
        }

        /// <summary>
        ///     Apply Valheim style to a <see cref="Text"/> Component.
        ///     Uses <code>GUIManager.Instance.AveriaSerifBold</code> by default
        /// </summary>
        /// <param name="text">Target component</param>
        /// <param name="color">Custom color or <code>GUIManager.Instance.ValheimOrange</code></param>
        /// <param name="createOutline">creates an <see cref="Outline"/> component when true</param>
        /// <param name="fontSize">Optional font size, defaults to 16</param>
        public void ApplyTextStyle(Text text, Color color, int fontSize = 16, bool createOutline = true)
        {
            ApplyTextStyle(text, AveriaSerifBold, color, fontSize, createOutline);
        }

        /// <summary>
        ///     Apply Valheim style to a <see cref="Text"/> Component.
        ///     Uses <code>GUIManager.Instance.AveriaSerifBold</code>, <code>Color.white</code> and creates an outline by default
        /// </summary>
        /// <param name="text">Target component</param>
        /// <param name="fontSize">Optional font size, defaults to 16</param>
        public void ApplyTextStyle(Text text, int fontSize = 16)
        {
            ApplyTextStyle(text, AveriaSerifBold, Color.white, fontSize, true);
        }

        /// <summary>
        ///     Apply valheim style to a <see cref="Button"/> Component
        /// </summary>
        /// <param name="button">Component to apply the style to</param>
        /// <param name="fontSize">Optional font size, defaults to 16</param>
        public void ApplyButtonStyle(Button button, int fontSize = 16)
        {
            GameObject go = button.gameObject;

            // Image
            Image image = go.GetOrAddComponent<Image>();
            image.sprite = GetSprite("button");
            image.type = Image.Type.Sliced;
            image.pixelsPerUnitMultiplier = GUIInStart ? 2f : 1f;
            button.image = image;

            // SFX
            if (!go.TryGetComponent<ButtonSfx>(out var sfx))
            {
                sfx = go.AddComponent<ButtonSfx>();
            }

            sfx.m_sfxPrefab = PrefabManager.Cache.GetPrefab<GameObject>("sfx_gui_button");
            sfx.m_selectSfxPrefab = PrefabManager.Cache.GetPrefab<GameObject>("sfx_gui_select");

            // Colors
            go.GetComponent<Button>().colors = ValheimButtonColorBlock;

            // Text
            var txt = go.GetComponentInChildren<Text>();

            if (!txt)
            {
                return;
            }

            ApplyTextStyle(txt, ValheimOrange, fontSize);
            txt.alignment = TextAnchor.MiddleCenter;
        }

        /// <summary>
        ///     Apply Valheim style to an <see cref="InputField"/> Component.
        /// </summary>
        /// <param name="field">Component to apply the style to</param>
        [Obsolete("Only here for backward compat")]
        public void ApplyInputFieldStyle(InputField field)
        {
            ApplyInputFieldStyle(field, 16);
        }

        /// <summary>
        ///     Apply Valheim style to an <see cref="InputField"/> Component.
        /// </summary>
        /// <param name="field">Component to apply the style to</param>
        /// <param name="fontSize">Optional font size, defaults to 16</param>
        public void ApplyInputFieldStyle(InputField field, int fontSize = 16)
        {
            // Image
            if (field.targetGraphic is Image imageField)
            {
                imageField.color = Color.white;
                imageField.sprite = GetSprite("text_field");
                imageField.pixelsPerUnitMultiplier = GUIInStart ? 2f : 1f;
            }

            // Placeholder
            if (field.placeholder is Text placeholder)
            {
                placeholder.font = AveriaSerifBold;
                placeholder.color = Color.grey;
                placeholder.fontSize = fontSize;
            }

            // Text
            if (field.textComponent)
            {
                ApplyTextStyle(field.textComponent, fontSize);
            }
        }

        /// <summary>
        ///     Apply Valheim style to a <see cref="Toggle"/> component.
        /// </summary>
        /// <param name="toggle">Component to apply the style to</param>
        public void ApplyToogleStyle(Toggle toggle)
        {
            toggle.toggleTransition = Toggle.ToggleTransition.Fade;
            toggle.colors = ValheimToggleColorBlock;

            if (toggle.targetGraphic is Image background)
            {
                background.sprite = GetSprite("checkbox");
                background.pixelsPerUnitMultiplier = GUIInStart ? 2f : 1f;
            }

            if (toggle.graphic is Image checkbox)
            {
                checkbox.color = new Color(1f, 0.678f, 0.103f, 1f);
                checkbox.sprite = GetSprite("checkbox_marker");
                checkbox.pixelsPerUnitMultiplier = GUIInStart ? 2f : 1f;
                checkbox.maskable = true;
            }
        }

        /// <summary>
        ///     Apply Valheim style to a <see cref="Dropdown"/> component.
        /// </summary>
        /// <param name="dropdown">Component to apply the style to</param>
        /// <param name="fontSize">Optional font size, defaults to 16</param>
        public void ApplyDropdownStyle(Dropdown dropdown, int fontSize = 16)
        {
            // Dropdown
            if (dropdown.captionText)
            {
                ApplyTextStyle(dropdown.captionText, fontSize);
                dropdown.captionText.verticalOverflow = VerticalWrapMode.Overflow;
            }

            if (dropdown.itemText)
            {
                ApplyTextStyle(dropdown.itemText, fontSize);
                dropdown.captionText.verticalOverflow = VerticalWrapMode.Overflow;
            }

            if (dropdown.TryGetComponent<Image>(out var dropdownImage))
            {
                dropdownImage.sprite = GetSprite("text_field");
                dropdownImage.pixelsPerUnitMultiplier = GUIInStart ? 2f : 1f;
            }

            // Arrow
            GameObject arrow = dropdown.transform.Find("Arrow").gameObject;
            arrow.transform.localRotation = Quaternion.Euler(new Vector3(0f, 0f, 180f));

            if (arrow.TryGetComponent<Image>(out var arrowImage))
            {
                arrow.SetSize(25f, 25f);
                arrowImage.sprite = GetSprite("map_marker");
                arrowImage.color = Color.white;
                arrowImage.pixelsPerUnitMultiplier = GUIInStart ? 2f : 1f;
            }

            // Template
            if (dropdown.template && dropdown.template.TryGetComponent<ScrollRect>(out var scrollRect))
            {
                ApplyScrollRectStyle(scrollRect);
            }

            if (dropdown.template && dropdown.template.TryGetComponent<Image>(out var templateImage))
            {
                templateImage.sprite = GetSprite("button_small");
                templateImage.color = Color.white;
                templateImage.pixelsPerUnitMultiplier = GUIInStart ? 2f : 1f;
            }

            // Item
            GameObject item = dropdown.template.Find("Viewport/Content/Item").gameObject;

            if (item && item.TryGetComponent<Toggle>(out var toggle))
            {
                toggle.toggleTransition = Toggle.ToggleTransition.None;
                toggle.colors = ValheimToggleColorBlock;
                toggle.spriteState = new SpriteState { highlightedSprite = GetSprite("button_highlight") };

                if (toggle.targetGraphic is Image background)
                {
                    background.enabled = false;
                }

                if (toggle.graphic is Image checkbox)
                {
                    checkbox.sprite = GetSprite("checkbox_marker");
                    checkbox.color = Color.white;
                    checkbox.type = Image.Type.Simple;
                    checkbox.maskable = true;
                    checkbox.pixelsPerUnitMultiplier = GUIInStart ? 2f : 1f;
                    checkbox.gameObject.GetOrAddComponent<Outline>().effectColor = Color.black;
                }
            }
        }

        /// <summary>
        ///     Apply Valheim style to a <see cref="ScrollRect"/> component.
        /// </summary>
        /// <param name="scrollRect">Component to apply the style to</param>
        public void ApplyScrollRectStyle(ScrollRect scrollRect)
        {
            scrollRect.scrollSensitivity = 40f;

            if (scrollRect.horizontalScrollbar)
            {
                ApplyScrollbarStyle(scrollRect.horizontalScrollbar);
            }

            if (scrollRect.verticalScrollbar)
            {
                ApplyScrollbarStyle(scrollRect.verticalScrollbar);
            }

            if (scrollRect.TryGetComponent<Image>(out var image))
            {
                image.color = new Color(0f, 0f, 0f, 0.564f);
                image.pixelsPerUnitMultiplier = GUIInStart ? 2f : 1f;
            }
        }

        /// <summary>
        ///     Apply Valheim style to a <see cref="Scrollbar"/> component.
        /// </summary>
        /// <param name="scrollbar">Component to apply the style to</param>
        public void ApplyScrollbarStyle(Scrollbar scrollbar)
        {
            scrollbar.transition = Selectable.Transition.ColorTint;
            scrollbar.colors = ValheimScrollbarHandleColorBlock;

            var rectTransform = (RectTransform)scrollbar.transform;

            if (scrollbar.direction == Scrollbar.Direction.LeftToRight || scrollbar.direction == Scrollbar.Direction.RightToLeft)
            {
                rectTransform.sizeDelta = new Vector2(rectTransform.sizeDelta.x, 10);
            }

            if (scrollbar.direction == Scrollbar.Direction.BottomToTop || scrollbar.direction == Scrollbar.Direction.TopToBottom)
            {
                rectTransform.sizeDelta = new Vector2(10, rectTransform.sizeDelta.y);
            }

            if (scrollbar.targetGraphic is Image handleImage)
            {
                handleImage.sprite = GetSprite("UISprite");
                handleImage.pixelsPerUnitMultiplier = GUIInStart ? 2f : 1f;
            }

            if (scrollbar.TryGetComponent<Image>(out var image))
            {
                image.sprite = GetSprite("Background");
                image.color = Color.black;
                image.raycastTarget = true;
                image.pixelsPerUnitMultiplier = GUIInStart ? 2f : 1f;
            }
        }

        /// <summary>
        ///     Apply Valheim style to a <see cref="Slider"/> component.
        /// </summary>
        /// <param name="slider"></param>
        public void ApplySliderStyle(Slider slider)
        {
            slider.handleRect.sizeDelta = new Vector2(40, 10);

            if (slider.fillRect && slider.fillRect.TryGetComponent<Image>(out var image))
            {
                image.sprite = GetSprite("UISprite");
                image.color = ValheimOrange;
            }

            if (slider.handleRect && slider.handleRect.transform.parent)
            {
                RectTransform handleSlideArea = (RectTransform)slider.handleRect.transform.parent;
                handleSlideArea.offsetMin = new Vector2(5f, handleSlideArea.offsetMin.y);
                handleSlideArea.offsetMax = new Vector2(-5f, handleSlideArea.offsetMax.y);
            }

            if (slider.handleRect && slider.fillRect.transform.parent)
            {
                RectTransform fillArea = (RectTransform)slider.fillRect.transform.parent;
                fillArea.offsetMin = new Vector2(5f, fillArea.offsetMin.y);
                fillArea.offsetMax = new Vector2(-5f, fillArea.offsetMax.y);
            }
        }
    }
}
