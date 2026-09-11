using System;
using System.Globalization;
using UnityEngine;
using UnityEngine.UI;
using FarmersCompanion.Helpers;
using FarmersCompanion.Features;
using FarmersCompanion.Patches;

namespace FarmersCompanion.UI
{
    #region [START] CANVAS FARMER'S COMPANION SETTINGS UI
    // ============================================================================
    // [START] CANVAS FARMER'S COMPANION SETTINGS UI
    // Purpose: High-resolution (1140x630), crisp in-game settings canvas for Farmer's Companion.
    //          Controls all 15 agriculture, forestry, and livestock features.
    // ============================================================================
    public class CanvasFarmersCompanionUI : MonoBehaviour
    {
        public static CanvasFarmersCompanionUI Instance { get; private set; }

        private GameObject _canvasGO;
        private Canvas _canvas;
        private CanvasScaler _scaler;
        private GraphicRaycaster _raycaster;
        private GameObject _rootGO;
        private GameObject _modWindowGO;
        private Font _gameFont;

        public static bool IsWindowOpen => Instance != null && Instance._rootGO != null && Instance._rootGO.activeSelf;

        #region [START] RAFT NATIVE HIGH-CONTRAST TIMBER PALETTE
        private static readonly Color WoodWindowBg      = new Color(0.18f, 0.13f, 0.08f, 0.99f); // Deep Forest Timber
        private static readonly Color WoodWindowBorder  = new Color(0.10f, 0.06f, 0.03f, 1.00f); // Dark Timber Outline
        private static readonly Color WoodTitleBar      = new Color(0.14f, 0.10f, 0.05f, 1.00f); // Header Bar
        private static readonly Color WoodTrimAccent    = new Color(0.45f, 0.85f, 0.40f, 1.00f); // Sprout Green Trim

        private static readonly Color TabActiveBg       = new Color(0.24f, 0.42f, 0.20f, 1.00f); // Emerald Plank
        private static readonly Color TabActiveBorder   = new Color(0.50f, 0.95f, 0.45f, 1.00f); // Glowing Green Rim
        private static readonly Color TabActiveText     = new Color(1.00f, 1.00f, 1.00f, 1.00f); // Pure White
        private static readonly Color TabInactiveBg     = new Color(0.16f, 0.11f, 0.07f, 0.96f); // Dark Wood Plank
        private static readonly Color TabInactiveBorder = new Color(0.25f, 0.18f, 0.10f, 0.60f); // Dark Inactive Rim
        private static readonly Color TabInactiveText   = new Color(0.80f, 0.75f, 0.65f, 1.00f); // Soft Text

        private static readonly Color TextWhite          = new Color(1.00f, 1.00f, 1.00f, 1.00f);
        private static readonly Color TextParchmentLight = new Color(0.96f, 0.94f, 0.88f, 1.00f);
        private static readonly Color TextGoldHeading    = new Color(1.00f, 0.82f, 0.35f, 1.00f);
        private static readonly Color TextGreenHeading   = new Color(0.45f, 0.95f, 0.45f, 1.00f);

        private static readonly Color ActionTileBg      = new Color(0.24f, 0.35f, 0.18f, 1.00f);
        private static readonly Color ActionTileHover   = new Color(0.32f, 0.48f, 0.24f, 1.00f);
        private static readonly Color ActionTileBorder  = new Color(0.55f, 0.92f, 0.48f, 0.95f);
        private static readonly Color CheckboxWoodBg    = new Color(0.10f, 0.07f, 0.03f, 0.98f);
        private static readonly Color CheckmarkGreen    = new Color(0.40f, 0.98f, 0.45f, 1.00f);
        private static readonly Color ButtonCloseRed    = new Color(0.70f, 0.16f, 0.14f, 0.98f);
        #endregion

        private const int TAB_COUNT = 4;
        private GameObject[] _tabPages = new GameObject[TAB_COUNT];
        private Image[] _tabButtonImages = new Image[TAB_COUNT];
        private Outline[] _tabButtonOutlines = new Outline[TAB_COUNT];
        private Text[] _tabButtonTexts = new Text[TAB_COUNT];
        private int _activeTab = 0;

        #region [START] UNITY LIFECYCLE
        private void Awake()
        {
            Instance = this;
            gameObject.hideFlags = HideFlags.HideAndDontSave;
            DontDestroyOnLoad(gameObject);
            GetGameFont();
            BuildCanvasUI();
        }

        public Font GetGameFont()
        {
            if (_gameFont != null) return _gameFont;

            try
            {
                _gameFont = Font.CreateDynamicFontFromOSFont(new[] { "Segoe UI Semibold", "Segoe UI", "Arial", "Tahoma" }, 24);
            }
            catch { }

            if (_gameFont != null) return _gameFont;

            var texts = Resources.FindObjectsOfTypeAll<Text>();
            foreach (var t in texts)
            {
                if (t != null && t.font != null)
                {
                    _gameFont = t.font;
                    return _gameFont;
                }
            }

            _gameFont = Resources.GetBuiltinResource<Font>("Arial.ttf");
            return _gameFont;
        }

        private static float _lastToggleTime = 0f;

        public static void ToggleWindow()
        {
            if (Time.unscaledTime - _lastToggleTime < 0.25f) return;
            _lastToggleTime = Time.unscaledTime;

            Debug.Log($"[Farmer's Companion] ToggleWindow called! IsWindowOpen={IsWindowOpen} at Time={Time.unscaledTime:F2}");
            if (IsWindowOpen)
            {
                Close();
            }
            else
            {
                Open();
            }
        }

        public static void Open()
        {
            if (Instance == null)
            {
                var go = new GameObject("FarmersCompanion_CanvasUI");
                DontDestroyOnLoad(go);
                Instance = go.AddComponent<CanvasFarmersCompanionUI>();
            }
            if (Instance._rootGO == null) Instance.BuildCanvasUI();

            Instance.EnsureEventSystem();
            if (Instance._raycaster != null && !Instance._raycaster.enabled) Instance._raycaster.enabled = true;
            Instance._rootGO.SetActive(true);

            // 1. Crucial for Unity New Input System: Switch action map to "UI" so clicks register!
            try
            {
                var cic = CustomInputConfig.Instance;
                if (cic != null)
                {
                    cic.EnableInput();
                    cic.SwitchCurrentActionMap("UI");
                }
            }
            catch { }

            // 2. Free cursor
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;
            try { Helper.SetCursorVisibleAndLockState(true, CursorLockMode.None); }
            catch { }

            // 3. Mark ActiveMenu to freeze in-game raycasting/interactions
            try
            {
                if (CanvasHelper.ActiveMenu == MenuType.None)
                {
                    CanvasHelper.ActiveMenu = MenuType.Cheat;
                }
            }
            catch { }

            // 4. Freeze mouse look
            try
            {
                var np = ComponentManager<Network_Player>.Value;
                if (np != null && np.PlayerScript != null)
                {
                    np.PlayerScript.SetLockMouseLook(true);
                }
            }
            catch { }

            Instance.SelectTab(Instance._activeTab);
            Debug.Log($"[Farmer's Companion] Settings UI Opened at Time={Time.unscaledTime:F2}");
        }

        public static void Close()
        {
            if (Instance == null || Instance._rootGO == null) return;
            if (!Instance._rootGO.activeSelf) return;

            Instance._rootGO.SetActive(false);

            bool peerModOpen = CursorPatchHelper.ShouldForceCursorFree();
            bool nativeMenuOpen = false;
            try
            {
                if (CanvasHelper.ActiveMenu != MenuType.None && CanvasHelper.ActiveMenu != MenuType.Cheat)
                {
                    nativeMenuOpen = true;
                }
            }
            catch { }

            try
            {
                if (CanvasHelper.ActiveMenu == MenuType.Cheat && !peerModOpen)
                {
                    CanvasHelper.ActiveMenu = MenuType.None;
                }
            }
            catch { }

            try
            {
                var np = ComponentManager<Network_Player>.Value;
                if (np != null && np.PlayerScript != null && !peerModOpen && !nativeMenuOpen)
                {
                    np.PlayerScript.SetLockMouseLook(false);
                }
            }
            catch { }

            try
            {
                var cic = CustomInputConfig.Instance;
                if (cic != null && !peerModOpen && !nativeMenuOpen)
                {
                    cic.SwitchCurrentActionMap("Player");
                }
            }
            catch { }

            bool isInGame = ComponentManager<Raft>.Value != null || ComponentManager<Network_Player>.Value != null;
            if (isInGame)
            {
                if (!peerModOpen && !nativeMenuOpen)
                {
                    try { Helper.SetCursorVisibleAndLockState(false, CursorLockMode.Locked); }
                    catch
                    {
                        Cursor.lockState = CursorLockMode.Locked;
                        Cursor.visible = false;
                    }
                }
            }
            else
            {
                try { Helper.SetCursorVisibleAndLockState(true, CursorLockMode.None); }
                catch
                {
                    Cursor.lockState = CursorLockMode.None;
                    Cursor.visible = true;
                }
            }

            Debug.Log($"[Farmer's Companion] Settings UI Closed at Time={Time.unscaledTime:F2}");
        }

        private void EnsureEventSystem()
        {
            var es = UnityEngine.EventSystems.EventSystem.current;
            if (es == null)
            {
                var existing = FindObjectOfType<UnityEngine.EventSystems.EventSystem>();
                if (existing != null)
                {
                    UnityEngine.EventSystems.EventSystem.current = existing;
                    es = existing;
                }
                else
                {
                    var esGO = new GameObject("FarmersCompanion_EventSystem");
                    esGO.hideFlags = HideFlags.HideAndDontSave;
                    es = esGO.AddComponent<UnityEngine.EventSystems.EventSystem>();
                    esGO.AddComponent<UnityEngine.EventSystems.StandaloneInputModule>();
                    DontDestroyOnLoad(esGO);
                    UnityEngine.EventSystems.EventSystem.current = es;
                }
            }

            if (es != null)
            {
                if (!es.enabled) es.enabled = true;
                if (!es.gameObject.activeInHierarchy) es.gameObject.SetActive(true);
                es.SetSelectedGameObject(null);
            }
        }

        private void Update()
        {
            KeyCode keyMenu = Plugin.KeyMenu != null ? Plugin.KeyMenu.Value : KeyCode.F1;
            if (InputHelper.WasKeyPressed(keyMenu))
            {
                ToggleWindow();
            }

            if (IsWindowOpen && (InputHelper.WasKeyPressed(KeyCode.Escape) || Input.GetKeyDown(KeyCode.Escape)))
            {
                if (Time.unscaledTime - _lastToggleTime >= 0.25f)
                {
                    _lastToggleTime = Time.unscaledTime;
                    Close();
                }
            }
        }
        #endregion [END] UNITY LIFECYCLE

        #region [START] BUILD CANVAS UI
        private void BuildCanvasUI()
        {
            if (_canvasGO != null && _rootGO != null) return;

            if (_canvasGO == null)
            {
                _canvasGO = new GameObject("FarmersCompanion_Canvas");
                _canvasGO.hideFlags = HideFlags.HideAndDontSave;
                _canvasGO.layer = LayerMask.NameToLayer("UI") >= 0 ? LayerMask.NameToLayer("UI") : 5;
                DontDestroyOnLoad(_canvasGO);

                _canvas = _canvasGO.AddComponent<Canvas>();
                _canvas.renderMode = RenderMode.ScreenSpaceOverlay;
                _canvas.overrideSorting = true;
                _canvas.sortingOrder = 33000; // Top-most priority (above InstalledMods 32500 and native menus)

                _scaler = _canvasGO.AddComponent<CanvasScaler>();
                _scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
                _scaler.referenceResolution = new Vector2(1920, 1080);
                _scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
                _scaler.matchWidthOrHeight = 0.5f;

                _raycaster = _canvasGO.AddComponent<GraphicRaycaster>();
            }

            if (_rootGO == null)
            {
                BuildWindow();
                SetLayerRecursively(_canvasGO, LayerMask.NameToLayer("UI") >= 0 ? LayerMask.NameToLayer("UI") : 5);
                _rootGO.SetActive(false); // Cleanly hide entire popup (dimmer + window) by default!
            }
        }

        private void BuildWindow()
        {
            // 1. Root Container for entire UI
            _rootGO = new GameObject("Root_FarmersCompanion");
            _rootGO.transform.SetParent(_canvasGO.transform, false);
            var rootRt = _rootGO.AddComponent<RectTransform>();
            rootRt.anchorMin = Vector2.zero;
            rootRt.anchorMax = Vector2.one;
            rootRt.offsetMin = Vector2.zero;
            rootRt.offsetMax = Vector2.zero;

            // 2. Dimmer Background (Visual backdrop only - non-blocking so clicks never accidentally close the menu)
            var dimmerGO = new GameObject("Dimmer");
            dimmerGO.transform.SetParent(_rootGO.transform, false);
            var dimmerRt = dimmerGO.AddComponent<RectTransform>();
            dimmerRt.anchorMin = Vector2.zero;
            dimmerRt.anchorMax = Vector2.one;
            dimmerRt.offsetMin = Vector2.zero;
            dimmerRt.offsetMax = Vector2.zero;
            var dimmerImg = dimmerGO.AddComponent<Image>();
            dimmerImg.color = new Color(0, 0, 0, 0.65f);
            dimmerImg.raycastTarget = false;

            // 3. Window Root
            _modWindowGO = new GameObject("FarmersCompanion_Window");
            _modWindowGO.transform.SetParent(_rootGO.transform, false);
            var winRt = _modWindowGO.AddComponent<RectTransform>();
            winRt.anchorMin = new Vector2(0.5f, 0.5f);
            winRt.anchorMax = new Vector2(0.5f, 0.5f);
            winRt.pivot = new Vector2(0.5f, 0.5f);
            winRt.sizeDelta = new Vector2(1140, 640);
            winRt.anchoredPosition = Vector2.zero;

            var winImg = _modWindowGO.AddComponent<Image>();
            winImg.color = WoodWindowBg;
            var winOutline = _modWindowGO.AddComponent<Outline>();
            winOutline.effectColor = WoodWindowBorder;
            winOutline.effectDistance = new Vector2(4, -4);

            // Header Bar
            var headGO = new GameObject("Header");
            headGO.transform.SetParent(_modWindowGO.transform, false);
            var headRt = headGO.AddComponent<RectTransform>();
            headRt.anchorMin = new Vector2(0, 1);
            headRt.anchorMax = new Vector2(1, 1);
            headRt.pivot = new Vector2(0.5f, 1);
            headRt.sizeDelta = new Vector2(0, 60);
            headRt.anchoredPosition = Vector2.zero;
            var headImg = headGO.AddComponent<Image>();
            headImg.color = WoodTitleBar;

            // Trim
            var trimGO = new GameObject("Trim");
            trimGO.transform.SetParent(headGO.transform, false);
            var trRt = trimGO.AddComponent<RectTransform>();
            trRt.anchorMin = new Vector2(0, 0);
            trRt.anchorMax = new Vector2(1, 0);
            trRt.pivot = new Vector2(0.5f, 0);
            trRt.sizeDelta = new Vector2(0, 3);
            var trImg = trimGO.AddComponent<Image>();
            trImg.color = WoodTrimAccent;

            // Title Text
            var titleTxt = CreateText(headGO, $"🌾 <color=#66FF66><b>FARMER'S COMPANION</b></color> <size=15><color=#D0E5D0>v{PluginInfo.PLUGIN_VERSION}</color></size>  <size=15><color=#D0E5D0>— Agriculture & Livestock Suite</color></size>", 22, FontStyle.Bold, TextGreenHeading, TextAnchor.MiddleLeft);
            var titleRt = titleTxt.GetComponent<RectTransform>();
            titleRt.anchorMin = new Vector2(0, 0);
            titleRt.anchorMax = new Vector2(1, 1);
            titleRt.offsetMin = new Vector2(24, 0);
            titleRt.offsetMax = new Vector2(-70, 0);

            // Close Button
            var closeGO = new GameObject("Btn_Close");
            closeGO.transform.SetParent(headGO.transform, false);
            closeGO.transform.SetAsLastSibling();
            var closeRt = closeGO.AddComponent<RectTransform>();
            closeRt.anchorMin = new Vector2(1, 0.5f);
            closeRt.anchorMax = new Vector2(1, 0.5f);
            closeRt.pivot = new Vector2(1, 0.5f);
            closeRt.sizeDelta = new Vector2(44, 38);
            closeRt.anchoredPosition = new Vector2(-12, 0);
            var closeImg = closeGO.AddComponent<Image>();
            closeImg.color = ButtonCloseRed;
            closeImg.raycastTarget = true;
            var closeBtn = closeGO.AddComponent<Button>();
            closeBtn.targetGraphic = closeImg;
            var cb = closeBtn.colors;
            cb.normalColor = ButtonCloseRed;
            cb.highlightedColor = new Color(0.90f, 0.25f, 0.20f, 1f);
            cb.pressedColor = new Color(0.50f, 0.10f, 0.08f, 1f);
            closeBtn.colors = cb;
            closeBtn.onClick.AddListener(Close);
            var closeTxt = CreateText(closeGO, "✕", 20, FontStyle.Bold, Color.white, TextAnchor.MiddleCenter);
            FillParent(closeTxt.gameObject);

            // Tabs Bar
            // Tab Pages Container (bounded below TabsBar)
            var pagesGO = new GameObject("TabPagesContainer");
            pagesGO.transform.SetParent(_modWindowGO.transform, false);
            var pagesRt = pagesGO.AddComponent<RectTransform>();
            pagesRt.anchorMin = new Vector2(0, 0);
            pagesRt.anchorMax = new Vector2(1, 1);
            pagesRt.offsetMin = new Vector2(24, 60);
            pagesRt.offsetMax = new Vector2(-24, -122);

            BuildTabPageWater(pagesGO, 0);
            BuildTabPageHarvest(pagesGO, 1);
            BuildTabPageGrowth(pagesGO, 2);
            BuildTabPageLivestock(pagesGO, 3);

            // Tabs Bar (Placed after Pages in hierarchy to guarantee topmost raycast reception)
            BuildTabsBar();

            // Footer Bar
            BuildFooterBar();

            // Ensure Header is on the very top of raycasts
            headGO.transform.SetAsLastSibling();
        }

        private void BuildTabsBar()
        {
            var tabsGO = new GameObject("TabsBar");
            tabsGO.transform.SetParent(_modWindowGO.transform, false);
            var tRt = tabsGO.AddComponent<RectTransform>();
            tRt.anchorMin = new Vector2(0, 1);
            tRt.anchorMax = new Vector2(1, 1);
            tRt.pivot = new Vector2(0.5f, 1);
            tRt.sizeDelta = new Vector2(-48, 46);
            tRt.anchoredPosition = new Vector2(0, -66);

            var tabLayout = tabsGO.AddComponent<HorizontalLayoutGroup>();
            tabLayout.spacing = 8;
            tabLayout.childForceExpandWidth = true;
            tabLayout.childForceExpandHeight = true;

            string[] tabNames = new[]
            {
                "💧 WATER & SMART USAGE",
                "🌾 HARVEST & SEED SAVER",
                "🌱 GROWTH & FORESTRY",
                "🐑 LIVESTOCK & 3D HUD"
            };

            for (int i = 0; i < TAB_COUNT; i++)
            {
                int tabIndex = i;
                var btnGO = new GameObject($"Tab_{i}");
                btnGO.transform.SetParent(tabsGO.transform, false);

                var le = btnGO.AddComponent<LayoutElement>();
                le.preferredHeight = 46;
                le.flexibleWidth = 1f;

                var img = btnGO.AddComponent<Image>();
                img.color = TabInactiveBg;
                img.raycastTarget = true;
                _tabButtonImages[i] = img;

                var outline = btnGO.AddComponent<Outline>();
                outline.effectColor = TabInactiveBorder;
                outline.effectDistance = new Vector2(2, -2);
                _tabButtonOutlines[i] = outline;

                var btn = btnGO.AddComponent<Button>();
                btn.targetGraphic = img;
                var cb = btn.colors;
                cb.normalColor = TabInactiveBg;
                cb.highlightedColor = WoodTrimAccent;
                cb.pressedColor = TabActiveBg;
                cb.selectedColor = TabInactiveBg;
                btn.colors = cb;
                btn.transition = Selectable.Transition.ColorTint;
                btn.onClick.AddListener(() =>
                {
                    SelectTab(tabIndex);
                });

                var txt = CreateText(btnGO, tabNames[i], 14, FontStyle.Bold, TabInactiveText, TextAnchor.MiddleCenter);
                FillParent(txt.gameObject);
                _tabButtonTexts[i] = txt;
            }

            tabsGO.transform.SetAsLastSibling();
        }

        public void SelectTab(int index)
        {
            _activeTab = index;
            for (int i = 0; i < TAB_COUNT; i++)
            {
                bool active = (i == index);
                if (_tabPages[i] != null)
                {
                    _tabPages[i].SetActive(active);
                }

                Color targetBg = active ? TabActiveBg : TabInactiveBg;
                Color targetBorder = active ? TabActiveBorder : TabInactiveBorder;
                Color targetText = active ? TabActiveText : TabInactiveText;

                if (_tabButtonImages[i] != null)
                {
                    _tabButtonImages[i].color = targetBg;
                    var btn = _tabButtonImages[i].GetComponent<Button>();
                    if (btn != null)
                    {
                        var cb = btn.colors;
                        cb.normalColor = targetBg;
                        cb.highlightedColor = active ? TabActiveBg : WoodTrimAccent;
                        cb.pressedColor = TabActiveBg;
                        cb.selectedColor = targetBg;
                        btn.colors = cb;
                    }
                }
                if (_tabButtonOutlines[i] != null)
                {
                    _tabButtonOutlines[i].effectColor = targetBorder;
                }
                if (_tabButtonTexts[i] != null)
                {
                    _tabButtonTexts[i].color = targetText;
                    _tabButtonTexts[i].fontStyle = active ? FontStyle.Bold : FontStyle.Normal;
                }
            }
            Debug.Log($"[Farmer's Companion] Switched to Tab {index}");
        }

        private void BuildTabPageWater(GameObject parent, int index)
        {
            var page = CreatePageContainer(parent, $"Page_{index}");
            _tabPages[index] = page;

            CreateToggleTile(page, "Auto Water Crops", "Automatically waters dry crop plots within farming radius.", () => CropWaterManager.Instance != null && CropWaterManager.Instance.EnableAutoWater, (v) => { if (CropWaterManager.Instance != null) CropWaterManager.Instance.EnableAutoWater = v; Plugin.EnableAutoWater.Value = v; }, 0);
            CreateToggleTile(page, "Animal Grass Plot Watering", "Keeps grass plots watered continuously so livestock can feed.", () => CropWaterManager.Instance != null && CropWaterManager.Instance.EnableGrassWatering, (v) => { if (CropWaterManager.Instance != null) CropWaterManager.Instance.EnableGrassWatering = v; Plugin.EnableGrassWatering.Value = v; }, 1);
            CreateToggleTile(page, "Smart Water Usage", "Only uses water when plots genuinely require hydration.", () => CropWaterManager.Instance != null && CropWaterManager.Instance.SmartWaterUsage, (v) => { if (CropWaterManager.Instance != null) CropWaterManager.Instance.SmartWaterUsage = v; Plugin.SmartWaterUsage.Value = v; }, 2);
            CreateSliderTile(page, "Farming Range Boost", 10f, 60f, "F0", "m", () => CropWaterManager.Instance != null ? CropWaterManager.Instance.WaterRadius : 30f, (v) => { if (CropWaterManager.Instance != null) CropWaterManager.Instance.WaterRadius = v; Plugin.WaterRadius.Value = v; }, 3);
        }

        private void BuildTabPageHarvest(GameObject parent, int index)
        {
            var page = CreatePageContainer(parent, $"Page_{index}");
            _tabPages[index] = page;

            CreateToggleTile(page, "Auto Harvest Ripe Crops", "Harvests fully mature crops directly into your inventory.", () => CropHarvestManager.Instance != null && CropHarvestManager.Instance.EnableAutoHarvest, (v) => { if (CropHarvestManager.Instance != null) CropHarvestManager.Instance.EnableAutoHarvest = v; Plugin.EnableAutoHarvest.Value = v; }, 0);
            CreateToggleTile(page, "Auto Replant Seeds", "Picks available matching seeds from inventory and replants empty plots.", () => CropHarvestManager.Instance != null && CropHarvestManager.Instance.EnableAutoReplant, (v) => { if (CropHarvestManager.Instance != null) CropHarvestManager.Instance.EnableAutoReplant = v; Plugin.EnableAutoReplant.Value = v; }, 1);
            CreateToggleTile(page, "Seed Saver Mode", "Grants a 25% chance to refund / preserve the seed upon planting.", () => CropHarvestManager.Instance != null && CropHarvestManager.Instance.EnableSeedSaver, (v) => { if (CropHarvestManager.Instance != null) CropHarvestManager.Instance.EnableSeedSaver = v; Plugin.EnableSeedSaver.Value = v; }, 2);
            CreateSliderTile(page, "Seed Saver Chance", 5f, 50f, "F0", "%", () => CropHarvestManager.Instance != null ? CropHarvestManager.Instance.SeedSaverChancePercent : 25f, (v) => { if (CropHarvestManager.Instance != null) CropHarvestManager.Instance.SeedSaverChancePercent = v; Plugin.SeedSaverChancePercent.Value = v; }, 3);

            // Sweep Button
            var btnGO = new GameObject("Btn_MultiHarvest");
            btnGO.transform.SetParent(page.transform, false);
            var bRt = btnGO.AddComponent<RectTransform>();
            bRt.anchorMin = new Vector2(0, 0);
            bRt.anchorMax = new Vector2(1, 0);
            bRt.pivot = new Vector2(0.5f, 0);
            bRt.sizeDelta = new Vector2(0, 46);
            bRt.anchoredPosition = new Vector2(0, 10);
            var bImg = btnGO.AddComponent<Image>();
            bImg.color = ActionTileBg;
            bImg.raycastTarget = true;
            var bOutline = btnGO.AddComponent<Outline>();
            bOutline.effectColor = ActionTileBorder;
            bOutline.effectDistance = new Vector2(2, -2);
            var btn = btnGO.AddComponent<Button>();
            btn.targetGraphic = bImg;
            var bcb = btn.colors;
            bcb.normalColor = ActionTileBg;
            bcb.highlightedColor = new Color(0.35f, 0.25f, 0.12f, 1f);
            bcb.pressedColor = new Color(0.18f, 0.12f, 0.05f, 1f);
            btn.colors = bcb;
            btn.onClick.AddListener(() =>
            {
                if (CropHarvestManager.Instance != null)
                {
                    int count = CropHarvestManager.Instance.HarvestNearbyCrops(CropHarvestManager.Instance.HarvestRadius, CropHarvestManager.Instance.EnableAutoReplant);
                    if (CropIndicatorManager.Instance != null)
                    {
                        CropIndicatorManager.Instance.ShowNotification($"🌾 Multi-Harvest: Harvested {count} ripe crops!");
                    }
                }
            });
            var bTxt = CreateText(btnGO, "⚡ MULTI-HARVEST SWEEP: Harvest All Ripe Crops Now!", 15, FontStyle.Bold, TextGoldHeading, TextAnchor.MiddleCenter);
            FillParent(bTxt.gameObject);
        }

        private void BuildTabPageGrowth(GameObject parent, int index)
        {
            var page = CreatePageContainer(parent, $"Page_{index}");
            _tabPages[index] = page;

            const float rowSpacing = 88f;
            CreateToggleTile(page, "Accelerated Crop Growth", "Increases crop growth speed by 1.3x for balanced pacing.", () => CropGrowthManager.Instance != null && CropGrowthManager.Instance.EnableGrowthBoost, (v) => { if (CropGrowthManager.Instance != null) CropGrowthManager.Instance.EnableGrowthBoost = v; Plugin.EnableGrowthBoost.Value = v; }, 0, rowSpacing);
            CreateSliderTile(page, "Crop Growth Multiplier", 1.0f, 3.0f, "F1", "x", () => CropGrowthManager.Instance != null ? CropGrowthManager.Instance.CropGrowthMultiplier : 1.3f, (v) => { if (CropGrowthManager.Instance != null) CropGrowthManager.Instance.CropGrowthMultiplier = v; Plugin.CropGrowthMultiplier.Value = v; }, 1, rowSpacing);
            CreateToggleTile(page, "Palm & Fruit Tree Growth Boost", "Increases palm tree and large fruit tree growth by 1.5x.", () => CropGrowthManager.Instance != null && CropGrowthManager.Instance.EnableTreeGrowthBoost, (v) => { if (CropGrowthManager.Instance != null) CropGrowthManager.Instance.EnableTreeGrowthBoost = v; Plugin.EnableTreeGrowthBoost.Value = v; }, 2, rowSpacing);
            CreateSliderTile(page, "Tree Growth Multiplier", 1.0f, 3.0f, "F1", "x", () => CropGrowthManager.Instance != null ? CropGrowthManager.Instance.TreeGrowthMultiplier : 1.5f, (v) => { if (CropGrowthManager.Instance != null) CropGrowthManager.Instance.TreeGrowthMultiplier = v; Plugin.TreeGrowthMultiplier.Value = v; }, 3, rowSpacing);
            CreateToggleTile(page, "Fertilizer Surge Multiplier", "Applies an additional 1.5x speed boost to all fertilized plots.", () => CropGrowthManager.Instance != null && CropGrowthManager.Instance.EnableFertilizerBoost, (v) => { if (CropGrowthManager.Instance != null) CropGrowthManager.Instance.EnableFertilizerBoost = v; Plugin.EnableFertilizerBoost.Value = v; }, 4, rowSpacing);
        }

        private void BuildTabPageLivestock(GameObject parent, int index)
        {
            var page = CreatePageContainer(parent, $"Page_{index}");
            _tabPages[index] = page;

            CreateToggleTile(page, "Auto-Collect Livestock Products", "Automatically shears Llamas for wool and milks Goats when ready.", () => LivestockManager.Instance != null && LivestockManager.Instance.EnableAutoCollectLivestock, (v) => { if (LivestockManager.Instance != null) LivestockManager.Instance.EnableAutoCollectLivestock = v; Plugin.EnableAutoCollectLivestock.Value = v; }, 0);
            CreateToggleTile(page, "3D Floating Crop Health HUD", "Displays in-world indicators showing hydration and growth %.", () => CropIndicatorManager.Instance != null && CropIndicatorManager.Instance.EnableHealthIndicators, (v) => { if (CropIndicatorManager.Instance != null) CropIndicatorManager.Instance.EnableHealthIndicators = v; Plugin.EnableHealthIndicators.Value = v; }, 1);
            CreateToggleTile(page, "Toast Notifications on Ready Crops", "Shows gentle on-screen alerts when crops or wool are ready.", () => CropIndicatorManager.Instance != null && CropIndicatorManager.Instance.EnableNotifications, (v) => { if (CropIndicatorManager.Instance != null) CropIndicatorManager.Instance.EnableNotifications = v; Plugin.EnableNotifications.Value = v; }, 2);

            BuildVersionUpdateRow(page, 3);
        }

        private void BuildVersionUpdateRow(GameObject parent, int rowIndex)
        {
            var rowGO = new GameObject($"VerRow_{rowIndex}");
            rowGO.transform.SetParent(parent.transform, false);
            var rRt = rowGO.AddComponent<RectTransform>();
            rRt.anchorMin = new Vector2(0, 1);
            rRt.anchorMax = new Vector2(1, 1);
            rRt.pivot = new Vector2(0.5f, 1);
            rRt.sizeDelta = new Vector2(0, 40);
            rRt.anchoredPosition = new Vector2(0, -rowIndex * 92);

            var rImg = rowGO.AddComponent<Image>();
            rImg.color = new Color(0.15f, 0.10f, 0.06f, 0.95f);
            var rOutline = rowGO.AddComponent<Outline>();
            rOutline.effectColor = new Color(0.30f, 0.22f, 0.12f, 0.90f);
            rOutline.effectDistance = new Vector2(1.5f, -1.5f);

            var verTxt = CreateText(rowGO, $"🌾 Farmer's Companion v{PluginInfo.PLUGIN_VERSION}", 14, FontStyle.Normal, TextParchmentLight, TextAnchor.MiddleLeft);
            var vRt = verTxt.GetComponent<RectTransform>();
            vRt.anchorMin = new Vector2(0, 0);
            vRt.anchorMax = new Vector2(1, 1);
            vRt.offsetMin = new Vector2(18, 0);
            vRt.offsetMax = new Vector2(-190, 0);

            var btnGO = new GameObject("Btn_CheckUpdates");
            btnGO.transform.SetParent(rowGO.transform, false);
            var bRt = btnGO.AddComponent<RectTransform>();
            bRt.anchorMin = new Vector2(1, 0.5f);
            bRt.anchorMax = new Vector2(1, 0.5f);
            bRt.pivot = new Vector2(1, 0.5f);
            bRt.sizeDelta = new Vector2(170, 32);
            bRt.anchoredPosition = new Vector2(-10, 0);

            var bImg = btnGO.AddComponent<Image>();
            bImg.color = ActionTileBg;
            var bOutline = btnGO.AddComponent<Outline>();
            bOutline.effectColor = ActionTileBorder;
            bOutline.effectDistance = new Vector2(1.5f, -1.5f);

            var btn = btnGO.AddComponent<Button>();
            btn.targetGraphic = bImg;
            var bcb = btn.colors;
            bcb.normalColor = ActionTileBg;
            bcb.highlightedColor = new Color(0.32f, 0.48f, 0.24f, 1f);
            bcb.pressedColor = new Color(0.15f, 0.10f, 0.06f, 1f);
            btn.colors = bcb;
            btn.onClick.AddListener(() =>
            {
                UpdateChecker.Dismissed = false;
                UpdateChecker.Instance?.TriggerCheck();
                CropIndicatorManager.Instance?.ShowNotification("Checking GitHub for mod updates...");
            });

            var btnTxt = CreateText(btnGO, "🔄 Check for Updates", 13, FontStyle.Bold, TextGoldHeading, TextAnchor.MiddleCenter);
            FillParent(btnTxt.gameObject);
        }

        private GameObject CreatePageContainer(GameObject parent, string name)
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent.transform, false);
            var rt = go.AddComponent<RectTransform>();
            rt.anchorMin = Vector2.zero;
            rt.anchorMax = Vector2.one;
            rt.offsetMin = Vector2.zero;
            rt.offsetMax = Vector2.zero;
            return go;
        }

        private void CreateToggleTile(GameObject parent, string title, string description, Func<bool> getter, Action<bool> setter, int rowIndex, float rowSpacing = 92f)
        {
            var tileGO = new GameObject($"Tile_{rowIndex}");
            tileGO.transform.SetParent(parent.transform, false);
            var tRt = tileGO.AddComponent<RectTransform>();
            tRt.anchorMin = new Vector2(0, 1);
            tRt.anchorMax = new Vector2(1, 1);
            tRt.pivot = new Vector2(0.5f, 1);
            tRt.sizeDelta = new Vector2(0, rowSpacing - 12f);
            tRt.anchoredPosition = new Vector2(0, -rowIndex * rowSpacing);

            var img = tileGO.AddComponent<Image>();
            img.color = new Color(0.15f, 0.10f, 0.06f, 0.95f);
            img.raycastTarget = true;

            var outline = tileGO.AddComponent<Outline>();
            outline.effectColor = new Color(0.30f, 0.22f, 0.12f, 0.90f);
            outline.effectDistance = new Vector2(1.5f, -1.5f);

            // Title
            var titleTxt = CreateText(tileGO, title, 16, FontStyle.Bold, TextGreenHeading, TextAnchor.MiddleLeft);
            var tiRt = titleTxt.GetComponent<RectTransform>();
            tiRt.anchorMin = new Vector2(0, 0.5f);
            tiRt.anchorMax = new Vector2(1, 1);
            tiRt.offsetMin = new Vector2(18, 0);
            tiRt.offsetMax = new Vector2(-120, 0);

            // Description
            var descTxt = CreateText(tileGO, description, 13, FontStyle.Normal, TextParchmentLight, TextAnchor.MiddleLeft);
            var dRt = descTxt.GetComponent<RectTransform>();
            dRt.anchorMin = new Vector2(0, 0);
            dRt.anchorMax = new Vector2(1, 0.5f);
            dRt.offsetMin = new Vector2(18, 0);
            dRt.offsetMax = new Vector2(-120, 0);

            // Checkbox Box
            var boxGO = new GameObject("Checkbox");
            boxGO.transform.SetParent(tileGO.transform, false);
            var bRt = boxGO.AddComponent<RectTransform>();
            bRt.anchorMin = new Vector2(1, 0.5f);
            bRt.anchorMax = new Vector2(1, 0.5f);
            bRt.pivot = new Vector2(1, 0.5f);
            bRt.sizeDelta = new Vector2(86, 42);
            bRt.anchoredPosition = new Vector2(-18, 0);

            var boxImg = boxGO.AddComponent<Image>();
            boxImg.color = CheckboxWoodBg;
            boxImg.raycastTarget = true;
            var boxOutline = boxGO.AddComponent<Outline>();
            boxOutline.effectColor = ActionTileBorder;
            boxOutline.effectDistance = new Vector2(1.5f, -1.5f);

            var checkTxt = CreateText(boxGO, getter() ? "ON" : "OFF", 14, FontStyle.Bold, getter() ? CheckmarkGreen : Color.gray, TextAnchor.MiddleCenter);
            FillParent(checkTxt.gameObject);

            void Toggle()
            {
                bool newState = !getter();
                setter(newState);
                checkTxt.text = newState ? "ON" : "OFF";
                checkTxt.color = newState ? CheckmarkGreen : Color.gray;
            }

            // Click checkbox
            var btn = boxGO.AddComponent<Button>();
            btn.targetGraphic = boxImg;
            var bcb = btn.colors;
            bcb.normalColor = CheckboxWoodBg;
            bcb.highlightedColor = new Color(0.28f, 0.18f, 0.10f, 1f);
            bcb.pressedColor = new Color(0.12f, 0.08f, 0.04f, 1f);
            btn.colors = bcb;
            btn.onClick.AddListener(Toggle);

            // Entire tile row is also clickable with smooth hover effect!
            var rowBtn = tileGO.AddComponent<Button>();
            rowBtn.targetGraphic = img;
            var rcb = rowBtn.colors;
            rcb.normalColor = new Color(0.15f, 0.10f, 0.06f, 0.95f);
            rcb.highlightedColor = new Color(0.22f, 0.15f, 0.09f, 0.98f);
            rcb.pressedColor = new Color(0.10f, 0.06f, 0.03f, 1f);
            rowBtn.colors = rcb;
            rowBtn.onClick.AddListener(Toggle);
        }

        private void CreateSliderTile(GameObject parent, string title, float min, float max, string valueFormat, string valueSuffix, Func<float> getter, Action<float> setter, int rowIndex, float rowSpacing = 92f)
        {
            var tileGO = new GameObject($"SliderTile_{rowIndex}");
            tileGO.transform.SetParent(parent.transform, false);
            var tRt = tileGO.AddComponent<RectTransform>();
            tRt.anchorMin = new Vector2(0, 1);
            tRt.anchorMax = new Vector2(1, 1);
            tRt.pivot = new Vector2(0.5f, 1);
            tRt.sizeDelta = new Vector2(0, rowSpacing - 12f);
            tRt.anchoredPosition = new Vector2(0, -rowIndex * rowSpacing);

            var img = tileGO.AddComponent<Image>();
            img.color = new Color(0.15f, 0.10f, 0.06f, 0.95f);
            img.raycastTarget = true;

            var outline = tileGO.AddComponent<Outline>();
            outline.effectColor = new Color(0.30f, 0.22f, 0.12f, 0.90f);
            outline.effectDistance = new Vector2(1.5f, -1.5f);

            // Title
            var titleTxt = CreateText(tileGO, title, 16, FontStyle.Bold, TextGreenHeading, TextAnchor.MiddleLeft);
            var tiRt = titleTxt.GetComponent<RectTransform>();
            tiRt.anchorMin = new Vector2(0, 0.5f);
            tiRt.anchorMax = new Vector2(1, 1);
            tiRt.offsetMin = new Vector2(18, 0);
            tiRt.offsetMax = new Vector2(-120, 0);

            // Value Badge (mirrors the checkbox position/style from CreateToggleTile)
            var boxGO = new GameObject("ValueBadge");
            boxGO.transform.SetParent(tileGO.transform, false);
            var bRt = boxGO.AddComponent<RectTransform>();
            bRt.anchorMin = new Vector2(1, 0.5f);
            bRt.anchorMax = new Vector2(1, 0.5f);
            bRt.pivot = new Vector2(1, 0.5f);
            bRt.sizeDelta = new Vector2(86, 42);
            bRt.anchoredPosition = new Vector2(-18, 0);

            var boxImg = boxGO.AddComponent<Image>();
            boxImg.color = CheckboxWoodBg;
            var boxOutline = boxGO.AddComponent<Outline>();
            boxOutline.effectColor = ActionTileBorder;
            boxOutline.effectDistance = new Vector2(1.5f, -1.5f);

            var valueTxt = CreateText(boxGO, FormatSliderValue(getter(), valueFormat, valueSuffix), 14, FontStyle.Bold, CheckmarkGreen, TextAnchor.MiddleCenter);
            FillParent(valueTxt.gameObject);

            // Slider Track (bottom half, left of the value badge)
            var sliderGO = new GameObject("Slider");
            sliderGO.transform.SetParent(tileGO.transform, false);
            var sliderRt = sliderGO.AddComponent<RectTransform>();
            sliderRt.anchorMin = new Vector2(0, 0);
            sliderRt.anchorMax = new Vector2(1, 0.5f);
            sliderRt.offsetMin = new Vector2(18, 10);
            sliderRt.offsetMax = new Vector2(-120, -6);

            var slider = sliderGO.AddComponent<Slider>();
            slider.direction = Slider.Direction.LeftToRight;
            slider.minValue = min;
            slider.maxValue = max;

            var trackGO = new GameObject("Track");
            trackGO.transform.SetParent(sliderGO.transform, false);
            var trackRt = trackGO.AddComponent<RectTransform>();
            trackRt.anchorMin = new Vector2(0, 0.35f);
            trackRt.anchorMax = new Vector2(1, 0.65f);
            trackRt.offsetMin = Vector2.zero;
            trackRt.offsetMax = Vector2.zero;
            var trackImg = trackGO.AddComponent<Image>();
            trackImg.color = CheckboxWoodBg;

            var fillAreaGO = new GameObject("Fill Area");
            fillAreaGO.transform.SetParent(sliderGO.transform, false);
            var fillAreaRt = fillAreaGO.AddComponent<RectTransform>();
            fillAreaRt.anchorMin = new Vector2(0, 0.35f);
            fillAreaRt.anchorMax = new Vector2(1, 0.65f);
            fillAreaRt.offsetMin = new Vector2(4, 0);
            fillAreaRt.offsetMax = new Vector2(-4, 0);

            var fillGO = new GameObject("Fill");
            fillGO.transform.SetParent(fillAreaGO.transform, false);
            var fillRt = fillGO.AddComponent<RectTransform>();
            fillRt.anchorMin = new Vector2(0, 0);
            fillRt.anchorMax = new Vector2(0, 1);
            fillRt.sizeDelta = new Vector2(10, 0);
            var fillImg = fillGO.AddComponent<Image>();
            fillImg.color = WoodTrimAccent;
            slider.fillRect = fillRt;

            var handleAreaGO = new GameObject("Handle Slide Area");
            handleAreaGO.transform.SetParent(sliderGO.transform, false);
            var handleAreaRt = handleAreaGO.AddComponent<RectTransform>();
            handleAreaRt.anchorMin = Vector2.zero;
            handleAreaRt.anchorMax = Vector2.one;
            handleAreaRt.offsetMin = new Vector2(8, 0);
            handleAreaRt.offsetMax = new Vector2(-8, 0);

            var handleGO = new GameObject("Handle");
            handleGO.transform.SetParent(handleAreaGO.transform, false);
            var handleRt = handleGO.AddComponent<RectTransform>();
            handleRt.sizeDelta = new Vector2(18, 28);
            var handleImg = handleGO.AddComponent<Image>();
            handleImg.color = TabActiveBorder;
            slider.handleRect = handleRt;
            slider.targetGraphic = handleImg;

            slider.value = getter();
            slider.onValueChanged.AddListener((v) =>
            {
                setter(v);
                valueTxt.text = FormatSliderValue(v, valueFormat, valueSuffix);
            });
        }

        private static string FormatSliderValue(float value, string format, string suffix)
        {
            return value.ToString(format, CultureInfo.InvariantCulture) + suffix;
        }

        private void BuildFooterBar()
        {
            var footGO = new GameObject("Footer");
            footGO.transform.SetParent(_modWindowGO.transform, false);
            var fRt = footGO.AddComponent<RectTransform>();
            fRt.anchorMin = new Vector2(0, 0);
            fRt.anchorMax = new Vector2(1, 0);
            fRt.pivot = new Vector2(0.5f, 0);
            fRt.sizeDelta = new Vector2(0, 52);
            fRt.anchoredPosition = Vector2.zero;
            var fImg = footGO.AddComponent<Image>();
            fImg.color = WoodTitleBar;

            // Trim
            var trimGO = new GameObject("FootTrim");
            trimGO.transform.SetParent(footGO.transform, false);
            var trRt = trimGO.AddComponent<RectTransform>();
            trRt.anchorMin = new Vector2(0, 1);
            trRt.anchorMax = new Vector2(1, 1);
            trRt.pivot = new Vector2(0.5f, 1);
            trRt.sizeDelta = new Vector2(0, 2);
            var trImg = trimGO.AddComponent<Image>();
            trImg.color = WoodTrimAccent;

            var footTxt = CreateText(footGO, "💡 <b>Quick Tip:</b> Press <b>[F1]</b> to toggle this menu. Use the Multi-Harvest button in the Harvest tab for an instant farm sweep.", 14, FontStyle.Normal, TextParchmentLight, TextAnchor.MiddleLeft);
            var ftRt = footTxt.GetComponent<RectTransform>();
            ftRt.anchorMin = new Vector2(0, 0);
            ftRt.anchorMax = new Vector2(1, 1);
            ftRt.offsetMin = new Vector2(24, 0);
            ftRt.offsetMax = new Vector2(-160, 0);

            var closeBtnGO = new GameObject("Btn_FootClose");
            closeBtnGO.transform.SetParent(footGO.transform, false);
            closeBtnGO.transform.SetAsLastSibling();
            var cRt = closeBtnGO.AddComponent<RectTransform>();
            cRt.anchorMin = new Vector2(1, 0.5f);
            cRt.anchorMax = new Vector2(1, 0.5f);
            cRt.pivot = new Vector2(1, 0.5f);
            cRt.sizeDelta = new Vector2(130, 36);
            cRt.anchoredPosition = new Vector2(-18, 0);
            var cImg = closeBtnGO.AddComponent<Image>();
            cImg.color = ButtonCloseRed;
            cImg.raycastTarget = true;
            var cBtn = closeBtnGO.AddComponent<Button>();
            cBtn.targetGraphic = cImg;
            var fb = cBtn.colors;
            fb.normalColor = ButtonCloseRed;
            fb.highlightedColor = new Color(0.90f, 0.25f, 0.20f, 1f);
            fb.pressedColor = new Color(0.50f, 0.10f, 0.08f, 1f);
            cBtn.colors = fb;
            cBtn.onClick.AddListener(Close);
            var cTxt = CreateText(closeBtnGO, "Close (ESC)", 13, FontStyle.Bold, Color.white, TextAnchor.MiddleCenter);
            FillParent(cTxt.gameObject);
        }

        private Text CreateText(GameObject parent, string text, int fontSize, FontStyle style, Color color, TextAnchor alignment)
        {
            var go = new GameObject("Text");
            go.transform.SetParent(parent.transform, false);
            var t = go.AddComponent<Text>();
            t.text = text;
            t.font = GetGameFont();
            t.fontSize = fontSize;
            t.fontStyle = style;
            t.color = color;
            t.alignment = alignment;
            t.raycastTarget = false;
            t.supportRichText = true;
            return t;
        }

        private void FillParent(GameObject go)
        {
            var rt = go.GetComponent<RectTransform>() ?? go.AddComponent<RectTransform>();
            rt.anchorMin = Vector2.zero;
            rt.anchorMax = Vector2.one;
            rt.offsetMin = Vector2.zero;
            rt.offsetMax = Vector2.zero;
        }

        private static void SetLayerRecursively(GameObject obj, int newLayer)
        {
            if (obj == null) return;
            obj.layer = newLayer;
            for (int i = 0; i < obj.transform.childCount; i++)
            {
                var child = obj.transform.GetChild(i);
                if (child != null) SetLayerRecursively(child.gameObject, newLayer);
            }
        }
        #endregion [END] BUILD CANVAS UI
    }
    // ============================================================================
    // [END] CANVAS FARMER'S COMPANION SETTINGS UI
    // ============================================================================
    #endregion
}
