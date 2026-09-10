using System;
using UnityEngine;
using UnityEngine.UI;
using FarmersCompanion.Helpers;
using FarmersCompanion.Features;

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
        private GameObject _modWindowGO;
        private Font _gameFont;

        public static bool IsWindowOpen => Instance != null && Instance._modWindowGO != null && Instance._modWindowGO.activeSelf;

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

        public static void ToggleWindow()
        {
            if (Instance == null)
            {
                var go = new GameObject("FarmersCompanion_CanvasUI");
                DontDestroyOnLoad(go);
                Instance = go.AddComponent<CanvasFarmersCompanionUI>();
            }
            if (Instance._modWindowGO == null) Instance.BuildCanvasUI();

            bool newState = !Instance._modWindowGO.activeSelf;
            Instance._modWindowGO.SetActive(newState);

            if (newState)
            {
                try { Helper.SetCursorVisibleAndLockState(true, CursorLockMode.None); }
                catch
                {
                    Cursor.lockState = CursorLockMode.None;
                    Cursor.visible = true;
                }
                Instance.SelectTab(Instance._activeTab);
            }
            else
            {
                try { Helper.SetCursorVisibleAndLockState(false, CursorLockMode.Locked); }
                catch
                {
                    Cursor.lockState = CursorLockMode.Locked;
                    Cursor.visible = false;
                }
            }
        }

        private void Update()
        {
            if ((Plugin.KeyMenu != null && InputHelper.WasKeyPressed(Plugin.KeyMenu.Value)) || InputHelper.WasKeyPressed(KeyCode.F1))
            {
                ToggleWindow();
            }

            if (IsWindowOpen && InputHelper.WasKeyPressed(KeyCode.Escape))
            {
                ToggleWindow();
            }
        }
        #endregion [END] UNITY LIFECYCLE

        #region [START] BUILD CANVAS UI
        private void BuildCanvasUI()
        {
            if (_canvasGO != null) return;

            _canvasGO = new GameObject("FarmersCompanion_Canvas");
            _canvasGO.transform.SetParent(transform, false);

            _canvas = _canvasGO.AddComponent<Canvas>();
            _canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            _canvas.sortingOrder = 9520;

            _scaler = _canvasGO.AddComponent<CanvasScaler>();
            _scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            _scaler.referenceResolution = new Vector2(1920, 1080);
            _scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
            _scaler.matchWidthOrHeight = 0.5f;

            _raycaster = _canvasGO.AddComponent<GraphicRaycaster>();

            BuildWindow();
            _modWindowGO.SetActive(false);
        }

        private void BuildWindow()
        {
            // Dimmer Background
            var dimmerGO = new GameObject("Dimmer");
            dimmerGO.transform.SetParent(_canvasGO.transform, false);
            var dimmerRt = dimmerGO.AddComponent<RectTransform>();
            dimmerRt.anchorMin = Vector2.zero;
            dimmerRt.anchorMax = Vector2.one;
            dimmerRt.offsetMin = Vector2.zero;
            dimmerRt.offsetMax = Vector2.zero;
            var dimmerImg = dimmerGO.AddComponent<Image>();
            dimmerImg.color = new Color(0, 0, 0, 0.65f);
            var dimmerBtn = dimmerGO.AddComponent<Button>();
            dimmerBtn.onClick.AddListener(ToggleWindow);

            // Window Root
            _modWindowGO = new GameObject("FarmersCompanion_Window");
            _modWindowGO.transform.SetParent(_canvasGO.transform, false);
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
            var titleTxt = CreateText(headGO, "🌾 <color=#66FF66><b>FARMER'S COMPANION</b></color>  <size=15><color=#D0E5D0>— Agriculture & Livestock Suite v1.0.0</color></size>", 22, FontStyle.Bold, TextGreenHeading, TextAnchor.MiddleLeft);
            var titleRt = titleTxt.GetComponent<RectTransform>();
            titleRt.anchorMin = new Vector2(0, 0);
            titleRt.anchorMax = new Vector2(1, 1);
            titleRt.offsetMin = new Vector2(24, 0);
            titleRt.offsetMax = new Vector2(-70, 0);

            // Close Button
            var closeGO = new GameObject("Btn_Close");
            closeGO.transform.SetParent(headGO.transform, false);
            var closeRt = closeGO.AddComponent<RectTransform>();
            closeRt.anchorMin = new Vector2(1, 0.5f);
            closeRt.anchorMax = new Vector2(1, 0.5f);
            closeRt.pivot = new Vector2(1, 0.5f);
            closeRt.sizeDelta = new Vector2(44, 38);
            closeRt.anchoredPosition = new Vector2(-12, 0);
            var closeImg = closeGO.AddComponent<Image>();
            closeImg.color = ButtonCloseRed;
            var closeBtn = closeGO.AddComponent<Button>();
            closeBtn.onClick.AddListener(ToggleWindow);
            var closeTxt = CreateText(closeGO, "✕", 20, FontStyle.Bold, Color.white, TextAnchor.MiddleCenter);
            FillParent(closeTxt.gameObject);

            // Tabs Bar
            BuildTabsBar();

            // Tab Pages Container
            var pagesGO = new GameObject("TabPagesContainer");
            pagesGO.transform.SetParent(_modWindowGO.transform, false);
            var pagesRt = pagesGO.AddComponent<RectTransform>();
            pagesRt.anchorMin = new Vector2(0, 0);
            pagesRt.anchorMax = new Vector2(1, 1);
            pagesRt.offsetMin = new Vector2(24, 60);
            pagesRt.offsetMax = new Vector2(-24, -114);

            BuildTabPageWater(pagesGO, 0);
            BuildTabPageHarvest(pagesGO, 1);
            BuildTabPageGrowth(pagesGO, 2);
            BuildTabPageLivestock(pagesGO, 3);

            // Footer Bar
            BuildFooterBar();
        }

        private void BuildTabsBar()
        {
            var tabsGO = new GameObject("TabsBar");
            tabsGO.transform.SetParent(_modWindowGO.transform, false);
            var tRt = tabsGO.AddComponent<RectTransform>();
            tRt.anchorMin = new Vector2(0, 1);
            tRt.anchorMax = new Vector2(1, 1);
            tRt.pivot = new Vector2(0.5f, 1);
            tRt.sizeDelta = new Vector2(0, 48);
            tRt.anchoredPosition = new Vector2(0, -60);

            string[] tabNames = new[]
            {
                "💧 WATER & SMART USAGE",
                "🌾 HARVEST & SEED SAVER",
                "🌱 GROWTH & FORESTRY",
                "🐑 LIVESTOCK & 3D HUD"
            };

            float tabWidth = 1140f / TAB_COUNT;
            for (int i = 0; i < TAB_COUNT; i++)
            {
                int tabIndex = i;
                var btnGO = new GameObject($"Tab_{i}");
                btnGO.transform.SetParent(tabsGO.transform, false);
                var bRt = btnGO.AddComponent<RectTransform>();
                bRt.anchorMin = new Vector2(0, 0);
                bRt.anchorMax = new Vector2(0, 1);
                bRt.pivot = new Vector2(0, 0.5f);
                bRt.sizeDelta = new Vector2(tabWidth - 4, 0);
                bRt.anchoredPosition = new Vector2(i * tabWidth + 2, 0);

                var img = btnGO.AddComponent<Image>();
                img.color = TabInactiveBg;
                _tabButtonImages[i] = img;

                var outline = btnGO.AddComponent<Outline>();
                outline.effectColor = TabInactiveBorder;
                outline.effectDistance = new Vector2(2, -2);
                _tabButtonOutlines[i] = outline;

                var txt = CreateText(btnGO, tabNames[i], 14, FontStyle.Bold, TabInactiveText, TextAnchor.MiddleCenter);
                FillParent(txt.gameObject);
                _tabButtonTexts[i] = txt;

                var btn = btnGO.AddComponent<Button>();
                btn.onClick.AddListener(() => SelectTab(tabIndex));
            }
        }

        public void SelectTab(int index)
        {
            _activeTab = index;
            for (int i = 0; i < TAB_COUNT; i++)
            {
                bool active = (i == index);
                if (_tabPages[i] != null) _tabPages[i].SetActive(active);
                if (_tabButtonImages[i] != null) _tabButtonImages[i].color = active ? TabActiveBg : TabInactiveBg;
                if (_tabButtonOutlines[i] != null) _tabButtonOutlines[i].effectColor = active ? TabActiveBorder : TabInactiveBorder;
                if (_tabButtonTexts[i] != null) _tabButtonTexts[i].color = active ? TabActiveText : TabInactiveText;
            }
        }

        private void BuildTabPageWater(GameObject parent, int index)
        {
            var page = CreatePageContainer(parent, $"Page_{index}");
            _tabPages[index] = page;

            CreateToggleTile(page, "Auto Water Crops (Feature 16)", "Automatically waters dry crop plots within farming radius.", () => CropWaterManager.Instance != null && CropWaterManager.Instance.EnableAutoWater, (v) => { if (CropWaterManager.Instance != null) CropWaterManager.Instance.EnableAutoWater = v; }, 0);
            CreateToggleTile(page, "Animal Grass Plot Watering (Feature 21)", "Keeps grass plots watered continuously so livestock can feed.", () => CropWaterManager.Instance != null && CropWaterManager.Instance.EnableGrassWatering, (v) => { if (CropWaterManager.Instance != null) CropWaterManager.Instance.EnableGrassWatering = v; }, 1);
            CreateToggleTile(page, "Smart Water Usage (Feature 26)", "Only uses water when plots genuinely require hydration.", () => CropWaterManager.Instance != null && CropWaterManager.Instance.SmartWaterUsage, (v) => { if (CropWaterManager.Instance != null) CropWaterManager.Instance.SmartWaterUsage = v; }, 2);
        }

        private void BuildTabPageHarvest(GameObject parent, int index)
        {
            var page = CreatePageContainer(parent, $"Page_{index}");
            _tabPages[index] = page;

            CreateToggleTile(page, "Auto Harvest Ripe Crops (Feature 17)", "Harvests fully mature crops directly into your inventory.", () => CropHarvestManager.Instance != null && CropHarvestManager.Instance.EnableAutoHarvest, (v) => { if (CropHarvestManager.Instance != null) CropHarvestManager.Instance.EnableAutoHarvest = v; }, 0);
            CreateToggleTile(page, "Auto Replant Seeds (Feature 18)", "Picks available matching seeds from inventory and replants empty plots.", () => CropHarvestManager.Instance != null && CropHarvestManager.Instance.EnableAutoReplant, (v) => { if (CropHarvestManager.Instance != null) CropHarvestManager.Instance.EnableAutoReplant = v; }, 1);
            CreateToggleTile(page, "Seed Saver Mode (Feature 28)", "Grants a 25% chance to refund / preserve the seed upon planting.", () => CropHarvestManager.Instance != null && CropHarvestManager.Instance.EnableSeedSaver, (v) => { if (CropHarvestManager.Instance != null) CropHarvestManager.Instance.EnableSeedSaver = v; }, 2);

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
            var bOutline = btnGO.AddComponent<Outline>();
            bOutline.effectColor = ActionTileBorder;
            bOutline.effectDistance = new Vector2(2, -2);
            var btn = btnGO.AddComponent<Button>();
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
            var bTxt = CreateText(btnGO, "⚡ MULTI-HARVEST SWEEP: Harvest All Ripe Crops Now! (Feature 27)", 15, FontStyle.Bold, TextGoldHeading, TextAnchor.MiddleCenter);
            FillParent(bTxt.gameObject);
        }

        private void BuildTabPageGrowth(GameObject parent, int index)
        {
            var page = CreatePageContainer(parent, $"Page_{index}");
            _tabPages[index] = page;

            CreateToggleTile(page, "Accelerated Crop Growth (Feature 19)", "Increases crop growth speed by 1.3x for balanced pacing.", () => CropGrowthManager.Instance != null && CropGrowthManager.Instance.EnableGrowthBoost, (v) => { if (CropGrowthManager.Instance != null) CropGrowthManager.Instance.EnableGrowthBoost = v; }, 0);
            CreateToggleTile(page, "Palm & Fruit Tree Growth Boost (Feature 20)", "Increases palm tree and large fruit tree growth by 1.5x.", () => CropGrowthManager.Instance != null && CropGrowthManager.Instance.EnableTreeGrowthBoost, (v) => { if (CropGrowthManager.Instance != null) CropGrowthManager.Instance.EnableTreeGrowthBoost = v; }, 1);
            CreateToggleTile(page, "Fertilizer Surge Multiplier (Feature 25)", "Applies an additional 1.5x speed boost to all fertilized plots.", () => CropGrowthManager.Instance != null && CropGrowthManager.Instance.EnableFertilizerBoost, (v) => { if (CropGrowthManager.Instance != null) CropGrowthManager.Instance.EnableFertilizerBoost = v; }, 2);
        }

        private void BuildTabPageLivestock(GameObject parent, int index)
        {
            var page = CreatePageContainer(parent, $"Page_{index}");
            _tabPages[index] = page;

            CreateToggleTile(page, "Auto-Collect Livestock Products (Feature 22)", "Automatically shears Llamas for wool and milks Goats when ready.", () => LivestockManager.Instance != null && LivestockManager.Instance.EnableAutoCollectLivestock, (v) => { if (LivestockManager.Instance != null) LivestockManager.Instance.EnableAutoCollectLivestock = v; }, 0);
            CreateToggleTile(page, "3D Floating Crop Health HUD (Feature 23)", "Displays in-world indicators showing hydration and growth %.", () => CropIndicatorManager.Instance != null && CropIndicatorManager.Instance.EnableHealthIndicators, (v) => { if (CropIndicatorManager.Instance != null) CropIndicatorManager.Instance.EnableHealthIndicators = v; }, 1);
            CreateToggleTile(page, "Toast Notifications on Ready Crops (Feature 29)", "Shows gentle on-screen alerts when crops or wool are ready.", () => CropIndicatorManager.Instance != null && CropIndicatorManager.Instance.EnableNotifications, (v) => { if (CropIndicatorManager.Instance != null) CropIndicatorManager.Instance.EnableNotifications = v; }, 2);
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

        private void CreateToggleTile(GameObject parent, string title, string description, Func<bool> getter, Action<bool> setter, int rowIndex)
        {
            var tileGO = new GameObject($"Tile_{rowIndex}");
            tileGO.transform.SetParent(parent.transform, false);
            var tRt = tileGO.AddComponent<RectTransform>();
            tRt.anchorMin = new Vector2(0, 1);
            tRt.anchorMax = new Vector2(1, 1);
            tRt.pivot = new Vector2(0.5f, 1);
            tRt.sizeDelta = new Vector2(0, 80);
            tRt.anchoredPosition = new Vector2(0, -rowIndex * 92);

            var img = tileGO.AddComponent<Image>();
            img.color = new Color(0.15f, 0.10f, 0.06f, 0.95f);

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
            var boxOutline = boxGO.AddComponent<Outline>();
            boxOutline.effectColor = ActionTileBorder;
            boxOutline.effectDistance = new Vector2(1.5f, -1.5f);

            var checkTxt = CreateText(boxGO, getter() ? "ON" : "OFF", 14, FontStyle.Bold, getter() ? CheckmarkGreen : Color.gray, TextAnchor.MiddleCenter);
            FillParent(checkTxt.gameObject);

            var btn = boxGO.AddComponent<Button>();
            btn.onClick.AddListener(() =>
            {
                bool newState = !getter();
                setter(newState);
                checkTxt.text = newState ? "ON" : "OFF";
                checkTxt.color = newState ? CheckmarkGreen : Color.gray;
            });
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

            var footTxt = CreateText(footGO, "💡 <b>Quick Tip:</b> Press <b>[F1]</b> anytime to toggle this menu. Multi-Harvest sweep harvests all nearby ripe crops.", 14, FontStyle.Normal, TextParchmentLight, TextAnchor.MiddleLeft);
            var ftRt = footTxt.GetComponent<RectTransform>();
            ftRt.anchorMin = new Vector2(0, 0);
            ftRt.anchorMax = new Vector2(1, 1);
            ftRt.offsetMin = new Vector2(24, 0);
            ftRt.offsetMax = new Vector2(-160, 0);

            var closeBtnGO = new GameObject("Btn_FootClose");
            closeBtnGO.transform.SetParent(footGO.transform, false);
            var cRt = closeBtnGO.AddComponent<RectTransform>();
            cRt.anchorMin = new Vector2(1, 0.5f);
            cRt.anchorMax = new Vector2(1, 0.5f);
            cRt.pivot = new Vector2(1, 0.5f);
            cRt.sizeDelta = new Vector2(130, 36);
            cRt.anchoredPosition = new Vector2(-18, 0);
            var cImg = closeBtnGO.AddComponent<Image>();
            cImg.color = ButtonCloseRed;
            var cBtn = closeBtnGO.AddComponent<Button>();
            cBtn.onClick.AddListener(ToggleWindow);
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
        #endregion [END] BUILD CANVAS UI
    }
    // ============================================================================
    // [END] CANVAS FARMER'S COMPANION SETTINGS UI
    // ============================================================================
    #endregion
}
