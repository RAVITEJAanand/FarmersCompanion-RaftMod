using System;
using System.Collections;
using System.Collections.Generic;
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
    // Purpose: "Modern Clean" in-game settings canvas for Farmer's Companion -
    //          a dark, flat, card-based redesign replacing the old wood-plank UI.
    //          Controls all agriculture, forestry, and livestock features.
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

        #region [START] MODERN CLEAN PALETTE (Option A - shared across all three mods)
        private static readonly Color ColBg          = new Color32(0x0F, 0x15, 0x18, 0xFF);
        private static readonly Color ColPanel       = new Color32(0x16, 0x1F, 0x24, 0xFF);
        private static readonly Color ColPanel2      = new Color32(0x1C, 0x27, 0x2D, 0xFF);
        private static readonly Color ColRow         = new Color32(0x1A, 0x24, 0x2A, 0xFF);
        private static readonly Color ColBorder      = new Color32(0x26, 0x33, 0x3B, 0xFF);
        private static readonly Color ColBorderSoft  = new Color32(0x1E, 0x29, 0x30, 0xFF);
        private static readonly Color ColText        = new Color32(0xEA, 0xF3, 0xF1, 0xFF);
        private static readonly Color ColTextMuted   = new Color32(0x8F, 0xA3, 0xA9, 0xFF);
        private static readonly Color ColTextFaint   = new Color32(0x5E, 0x73, 0x79, 0xFF);
        private static readonly Color ColAccent      = new Color32(0x2F, 0xC7, 0xB0, 0xFF);
        private static readonly Color ColAccentStrong= new Color32(0x20, 0xA7, 0x94, 0xFF);
        private static readonly Color ColAccentWash  = new Color(0x2F / 255f, 0xC7 / 255f, 0xB0 / 255f, 0.16f);
        private static readonly Color ColGold        = new Color32(0xE8, 0xB9, 0x4A, 0xFF);
        private static readonly Color ColSuccess     = new Color32(0x5F, 0xBE, 0x7A, 0xFF);
        private static readonly Color ColSuccessWash = new Color(0x5F / 255f, 0xBE / 255f, 0x7A / 255f, 0.16f);
        private static readonly Color ColDanger      = new Color32(0xE0, 0x5A, 0x5A, 0xFF);
        private static readonly Color ColOnAccentTxt = new Color32(0x06, 0x23, 0x1F, 0xFF);
        #endregion

        private const int SCREEN_COUNT = 5;
        // 0=Overview 1=Water&Growth 2=Harvest&Seeds 3=Livestock 4=Updates
        private readonly GameObject[] _screens = new GameObject[SCREEN_COUNT];
        private readonly Button[] _railButtons = new Button[SCREEN_COUNT];
        private readonly Image[] _railBg = new Image[SCREEN_COUNT];
        private readonly Text[] _railTexts = new Text[SCREEN_COUNT];
        private readonly Image[] _railIcons = new Image[SCREEN_COUNT];
        private readonly GameObject[] _railBars = new GameObject[SCREEN_COUNT];
        private int _activeScreen = 0;

        private Text _versionFooterText;
        private Text _footerVerText;
        private Text _updateBadgeText;
        private Image _updateBadgeImg;

        #region [START] UNITY LIFECYCLE
        #region [START] AWAKE
        private void Awake()
        {
            Instance = this;
            gameObject.hideFlags = HideFlags.HideAndDontSave;
            DontDestroyOnLoad(gameObject);
            Debug.Log("[Farmer's Companion] CanvasFarmersCompanionUI.Awake() starting...");
            try
            {
                GetGameFont();
                BuildCanvasUI();
                StartCoroutine(PollUpdateStatus());
                Debug.Log("[Farmer's Companion] CanvasFarmersCompanionUI.Awake() completed successfully.");
            }
            catch (System.Exception ex)
            {
                Debug.LogError($"[Farmer's Companion] CanvasFarmersCompanionUI.Awake() FAILED: {ex}");
            }
        }
        #endregion [END] AWAKE

        #region [START] GET GAME FONT
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
        #endregion [END] GET GAME FONT

        private static float _lastToggleTime = 0f;

        #region [START] TOGGLE WINDOW
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
        #endregion [END] TOGGLE WINDOW

        #region [START] OPEN
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

            Instance.RefreshOverview();
            Instance.SelectScreen(Instance._activeScreen);

            // The whole window is built once (in Awake, while _rootGO is still inactive so it
            // starts hidden) and only re-shown here. Unity's layout system skips computing
            // layout for inactive hierarchies and never automatically recalculates it later
            // just because the object becomes active again - every nested VerticalLayoutGroup/
            // HorizontalLayoutGroup rect stayed at its raw default (100x100, centered) forever,
            // which is why headings and other deeply-nested rows never actually appeared where
            // intended. Forcing one rebuild here, every time the window opens, fixes it for good.
            if (Instance._modWindowGO != null)
            {
                Canvas.ForceUpdateCanvases();
                LayoutRebuilder.ForceRebuildLayoutImmediate(Instance._modWindowGO.GetComponent<RectTransform>());
            }


            Debug.Log($"[Farmer's Companion] Settings UI Opened at Time={Time.unscaledTime:F2}");
        }
        #endregion [END] OPEN

        #region [START] CLOSE
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

            bool isInGame = ComponentManager<Raft>.Value != null || ComponentManager<Network_Player>.Value != null;

            try
            {
                var cic = CustomInputConfig.Instance;
                if (cic != null && !peerModOpen && !nativeMenuOpen)
                {
                    // Only gameplay has a "Player" action map to return to - switching to it
                    // from the main menu (no player/world loaded) left the home screen's own
                    // UI buttons unable to receive clicks until the game was restarted.
                    cic.SwitchCurrentActionMap(isInGame ? "Player" : "UI");
                }
            }
            catch { }

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
        #endregion [END] CLOSE

        #region [START] ENSURE EVENT SYSTEM
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
        #endregion [END] ENSURE EVENT SYSTEM

        private static bool _loggedFirstUpdate = false;

        #region [START] UPDATE
        private void Update()
        {
            if (!_loggedFirstUpdate)
            {
                _loggedFirstUpdate = true;
                Debug.Log($"[Farmer's Companion] DIAGNOSTIC: Update() loop is running (first frame at Time={Time.unscaledTime:F2}).");
            }

            KeyCode keyMenu = Plugin.KeyMenu != null ? Plugin.KeyMenu.Value : KeyCode.F1;
            if (InputHelper.WasKeyPressed(keyMenu))
            {
                ToggleWindow();
            }

            // InputHelper.WasKeyPressed already handles Escape through its New Input System
            // fallback; a raw Input.GetKeyDown call bypasses that fallback entirely and can
            // throw (and spam logs) every single frame on the exact setups InputHelper exists
            // to work around, so Escape is only ever read through InputHelper here.
            if (IsWindowOpen && InputHelper.WasKeyPressed(KeyCode.Escape))
            {
                if (Time.unscaledTime - _lastToggleTime >= 0.25f)
                {
                    _lastToggleTime = Time.unscaledTime;
                    Close();
                }
            }
        }
        #endregion [END] UPDATE
        #endregion [END] UNITY LIFECYCLE

        #region [START] CANVAS CONSTRUCTION
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
        #endregion [END] BUILD CANVAS UI

        #region [START] BUILD WINDOW
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

            // 2. Dimmer Background (visual backdrop only - non-blocking so clicks never accidentally close the menu)
            var dimmerGO = new GameObject("Dimmer");
            dimmerGO.transform.SetParent(_rootGO.transform, false);
            var dimmerRt = dimmerGO.AddComponent<RectTransform>();
            dimmerRt.anchorMin = Vector2.zero;
            dimmerRt.anchorMax = Vector2.one;
            dimmerRt.offsetMin = Vector2.zero;
            dimmerRt.offsetMax = Vector2.zero;
            var dimmerImg = dimmerGO.AddComponent<Image>();
            dimmerImg.color = new Color(0f, 0f, 0f, 0.6f);
            dimmerImg.raycastTarget = false;

            // 3. Window Root (two-column: rail + content)
            _modWindowGO = new GameObject("FarmersCompanion_Window");
            _modWindowGO.transform.SetParent(_rootGO.transform, false);
            var winRt = _modWindowGO.AddComponent<RectTransform>();
            winRt.anchorMin = new Vector2(0.5f, 0.5f);
            winRt.anchorMax = new Vector2(0.5f, 0.5f);
            winRt.pivot = new Vector2(0.5f, 0.5f);
            winRt.sizeDelta = new Vector2(1260, 720);
            winRt.anchoredPosition = Vector2.zero;

            var winImg = _modWindowGO.AddComponent<Image>();
            winImg.sprite = GetRoundedSprite(18);
            winImg.type = Image.Type.Sliced;
            winImg.color = ColBorder;
            AddInsetFill(_modWindowGO, 18, ColPanel);

            var railGO = BuildRail();
            railGO.transform.SetParent(_modWindowGO.transform, false);
            var railRt = railGO.GetComponent<RectTransform>();
            railRt.anchorMin = new Vector2(0, 0);
            railRt.anchorMax = new Vector2(0, 1);
            railRt.pivot = new Vector2(0, 0.5f);
            railRt.sizeDelta = new Vector2(248, 0);
            railRt.anchoredPosition = Vector2.zero;

            var contentGO = new GameObject("Content");
            contentGO.transform.SetParent(_modWindowGO.transform, false);
            var contentRt = contentGO.AddComponent<RectTransform>();
            contentRt.anchorMin = new Vector2(0, 0);
            contentRt.anchorMax = new Vector2(1, 1);
            contentRt.offsetMin = new Vector2(248, 0);
            contentRt.offsetMax = Vector2.zero;

            BuildFooter(contentGO);

            var screensHost = new GameObject("Screens");
            screensHost.transform.SetParent(contentGO.transform, false);
            var screensRt = screensHost.AddComponent<RectTransform>();
            screensRt.anchorMin = Vector2.zero;
            screensRt.anchorMax = Vector2.one;
            screensRt.offsetMin = new Vector2(0, 52); // leave room for footer
            screensRt.offsetMax = Vector2.zero;

            _screens[0] = BuildScreenOverview(screensHost);
            _screens[1] = BuildScreenWaterGrowth(screensHost);
            _screens[2] = BuildScreenHarvestSeeds(screensHost);
            _screens[3] = BuildScreenLivestock(screensHost);
            _screens[4] = BuildScreenUpdates(screensHost);

            SelectScreen(0);

            BuildCloseButton();
        }
        #endregion [END] BUILD WINDOW

        // The redesign never got a visible close/X control - F1 and Escape both still close the
        // window (see ToggleWindow/Update), but a panel this size reads as a standalone app and
        // players expect a corner close button regardless of knowing the hotkey. Parented directly
        // to the window root (last sibling) so it renders above the rail/content/footer.
        #region [START] BUILD CLOSE BUTTON
        private void BuildCloseButton()
        {
            var go = new GameObject("CloseBtn");
            go.transform.SetParent(_modWindowGO.transform, false);
            var rt = go.AddComponent<RectTransform>();
            rt.anchorMin = new Vector2(1, 1);
            rt.anchorMax = new Vector2(1, 1);
            rt.pivot = new Vector2(1, 1);
            rt.sizeDelta = new Vector2(32, 32);
            rt.anchoredPosition = new Vector2(-16, -16);

            var img = go.AddComponent<Image>();
            img.sprite = GetRoundedSprite(16);
            img.type = Image.Type.Sliced;
            img.color = ColPanel2;

            var btn = go.AddComponent<Button>();
            btn.targetGraphic = img;
            var cb = btn.colors;
            cb.normalColor = ColPanel2;
            cb.highlightedColor = ColDanger;
            cb.pressedColor = ColDanger;
            btn.colors = cb;
            btn.onClick.AddListener(Close);

            var txt = CreateText(go, "✕", 15, FontStyle.Bold, ColTextMuted, TextAnchor.MiddleCenter);
            txt.raycastTarget = false;
            FillParent(txt.gameObject);
        }
        #endregion [END] BUILD CLOSE BUTTON

        // ---------------- RAIL (left navigation) ----------------
        #region [START] BUILD RAIL
        private GameObject BuildRail()
        {
            var railGO = new GameObject("Rail");
            var railImg = railGO.AddComponent<Image>();
            railImg.color = ColPanel2;

            var railLayout = railGO.AddComponent<VerticalLayoutGroup>();
            railLayout.childControlWidth = true;
            railLayout.childControlHeight = true;
            railLayout.padding = new RectOffset(14, 14, 20, 16);
            railLayout.spacing = 2;
            railLayout.childForceExpandWidth = true;
            railLayout.childForceExpandHeight = false;
            railLayout.childControlWidth = true;
            railLayout.childControlHeight = true;

            // Brand row
            var brandGO = new GameObject("Brand");
            brandGO.transform.SetParent(railGO.transform, false);
            var brandLe = brandGO.AddComponent<LayoutElement>();
            brandLe.preferredHeight = 66;
            // A child whose OWN inner HorizontalLayoutGroup sets childForceExpandHeight = true
            // reports flexibleHeight = 1 to its parent (the group forces every one of its children
            // to at least 1 flexible unit on the cross axis, then advertises that total upward).
            // Without pinning it to 0 here, the rail's VerticalLayoutGroup hands surplus height to
            // this row and every nav item instead of only to the dedicated Spacer, inflating them
            // far past their preferredHeight. LayoutElement outranks a LayoutGroup, so 0 wins.
            brandLe.flexibleHeight = 0;
            var brandLayout = brandGO.AddComponent<HorizontalLayoutGroup>();
            brandLayout.childControlWidth = true;
            brandLayout.childControlHeight = true;
            brandLayout.spacing = 10;
            brandLayout.childAlignment = TextAnchor.MiddleLeft;
            brandLayout.childForceExpandWidth = false;
            brandLayout.childForceExpandHeight = true;

            var markGO = new GameObject("Mark");
            markGO.transform.SetParent(brandGO.transform, false);
            var markLe = markGO.AddComponent<LayoutElement>();
            markLe.preferredWidth = 38; markLe.preferredHeight = 38;
            var markImg = markGO.AddComponent<Image>();
            markImg.sprite = GetRoundedSprite(9);
            markImg.type = Image.Type.Sliced;
            markImg.color = ColAccent;
            var markTxt = CreateText(markGO, "F", 18, FontStyle.Bold, ColOnAccentTxt, TextAnchor.MiddleCenter);
            FillParent(markTxt.gameObject);

            var brandTextGO = new GameObject("BrandText");
            brandTextGO.transform.SetParent(brandGO.transform, false);
            var btLe = brandTextGO.AddComponent<LayoutElement>();
            btLe.flexibleWidth = 1f;
            var btLayout = brandTextGO.AddComponent<VerticalLayoutGroup>();
            btLayout.childControlWidth = true;
            btLayout.childControlHeight = true;
            btLayout.childForceExpandWidth = true;
            btLayout.childForceExpandHeight = false;
            btLayout.childControlHeight = true;
            btLayout.spacing = 1;

            var nameTxt = CreateText(brandTextGO, "Farmer's Companion", 16, FontStyle.Bold, ColText, TextAnchor.MiddleLeft);
            var nameLe = nameTxt.gameObject.AddComponent<LayoutElement>();
            nameLe.preferredHeight = 17;

            var discordGO = new GameObject("Discord");
            discordGO.transform.SetParent(brandTextGO.transform, false);
            var discordLe = discordGO.AddComponent<LayoutElement>();
            discordLe.preferredHeight = 15;
            var discordTxt = CreateText(discordGO, "Konduri Modding Hub", 13, FontStyle.Normal, ColTextFaint, TextAnchor.MiddleLeft);
            FillParent(discordTxt.gameObject);
            var discordBtn = discordGO.AddComponent<Button>();
            discordBtn.transition = Selectable.Transition.None;
            discordBtn.onClick.AddListener(() => { try { Application.OpenURL("https://discord.gg/B4EMrR5Vrf"); } catch { } });

            // Divider
            AddDivider(railGO.transform, 18);

            // Nav items
            string[] labels = { "Overview", "Water & Growth", "Harvest & Seeds", "Livestock", "Updates" };
            string[] monograms = { "O", "W", "H", "L", "U" };
            for (int i = 0; i < SCREEN_COUNT; i++)
            {
                int idx = i;
                var itemGO = BuildRailItem(labels[i], monograms[i], () => SelectScreen(idx));
                itemGO.transform.SetParent(railGO.transform, false);
                var itemLe = itemGO.AddComponent<LayoutElement>();
                itemLe.preferredHeight = 44;
                itemLe.flexibleHeight = 0; // see Brand above - keeps nav items at 44, not stretched
                _railButtons[i] = itemGO.GetComponent<Button>();
            }

            // spacer to push status to bottom
            var spacerGO = new GameObject("Spacer");
            spacerGO.transform.SetParent(railGO.transform, false);
            var spacerLe = spacerGO.AddComponent<LayoutElement>();
            spacerLe.flexibleHeight = 1f;

            AddDivider(railGO.transform, 10);

            var statusGO = new GameObject("Status");
            statusGO.transform.SetParent(railGO.transform, false);
            var statusLe = statusGO.AddComponent<LayoutElement>();
            statusLe.preferredHeight = 26;
            statusLe.flexibleHeight = 0; // see Brand above
            var statusLayout = statusGO.AddComponent<HorizontalLayoutGroup>();
            statusLayout.childControlWidth = true;
            statusLayout.childControlHeight = true;
            statusLayout.spacing = 7;
            statusLayout.padding = new RectOffset(6, 0, 0, 0);
            statusLayout.childAlignment = TextAnchor.MiddleLeft;
            statusLayout.childForceExpandWidth = false;
            statusLayout.childForceExpandHeight = true;

            var dotGO = new GameObject("Dot");
            dotGO.transform.SetParent(statusGO.transform, false);
            var dotLe = dotGO.AddComponent<LayoutElement>();
            dotLe.preferredWidth = 6; dotLe.preferredHeight = 6;
            var dotImg = dotGO.AddComponent<Image>();
            dotImg.sprite = GetRoundedSprite(3);
            dotImg.type = Image.Type.Sliced;
            dotImg.color = ColAccent;

            var statusTxt = CreateText(statusGO, "All systems running", 16f, FontStyle.Normal, ColTextFaint, TextAnchor.MiddleLeft);
            var stLe = statusTxt.gameObject.AddComponent<LayoutElement>();
            stLe.flexibleWidth = 1f;

            return railGO;
        }
        #endregion [END] BUILD RAIL

        #region [START] BUILD RAIL ITEM
        private GameObject BuildRailItem(string label, string monogram, Action onClick)
        {
            int myIndex = Array.IndexOf(new[] { "Overview", "Water & Growth", "Harvest & Seeds", "Livestock", "Updates" }, label);

            var go = new GameObject($"Rail_{label}");
            var img = go.AddComponent<Image>();
            img.sprite = GetRoundedSprite(9);
            img.type = Image.Type.Sliced;
            img.color = Color.clear;
            if (myIndex >= 0) _railBg[myIndex] = img;

            var layout = go.AddComponent<HorizontalLayoutGroup>();
            layout.childControlWidth = true;
            layout.childControlHeight = true;
            layout.padding = new RectOffset(10, 8, 0, 0);
            layout.spacing = 9;
            layout.childAlignment = TextAnchor.MiddleLeft;
            layout.childForceExpandWidth = false;
            layout.childForceExpandHeight = true;

            // active indicator bar (left edge)
            var barGO = new GameObject("Bar");
            barGO.transform.SetParent(go.transform, false);
            var barRt = barGO.AddComponent<RectTransform>();
            barRt.anchorMin = new Vector2(0, 0.5f);
            barRt.anchorMax = new Vector2(0, 0.5f);
            barRt.pivot = new Vector2(0.5f, 0.5f);
            barRt.sizeDelta = new Vector2(3, 16);
            barRt.anchoredPosition = new Vector2(-11, 0);
            var barImg = barGO.AddComponent<Image>();
            barImg.sprite = GetRoundedSprite(2);
            barImg.type = Image.Type.Sliced;
            barImg.color = ColAccent;
            // Pinned to the row's left edge by hand, so it must sit out of the row's own
            // HorizontalLayoutGroup - otherwise the group overwrites these anchors and lays the
            // indicator out as a regular item in the row instead of an edge accent.
            barGO.AddComponent<LayoutElement>().ignoreLayout = true;
            barGO.SetActive(false);
            if (myIndex >= 0) _railBars[myIndex] = barGO;

            var monoGO = new GameObject("Mono");
            monoGO.transform.SetParent(go.transform, false);
            var monoLe = monoGO.AddComponent<LayoutElement>();
            monoLe.preferredWidth = 20; monoLe.preferredHeight = 20;
            var monoImg = monoGO.AddComponent<Image>();
            monoImg.sprite = GetRoundedSprite(6);
            monoImg.type = Image.Type.Sliced;
            monoImg.color = ColBorderSoft;
            if (myIndex >= 0) _railIcons[myIndex] = monoImg;
            var monoTxt = CreateText(monoGO, monogram, 13, FontStyle.Bold, ColTextMuted, TextAnchor.MiddleCenter);
            FillParent(monoTxt.gameObject);

            var lblGO = new GameObject("Label");
            lblGO.transform.SetParent(go.transform, false);
            var lblLe = lblGO.AddComponent<LayoutElement>();
            lblLe.flexibleWidth = 1f;
            var lblTxt = CreateText(lblGO, label, 15.5f, FontStyle.Normal, ColTextMuted, TextAnchor.MiddleLeft);
            FillParent(lblTxt.gameObject);
            if (myIndex >= 0) _railTexts[myIndex] = lblTxt;

            var btn = go.AddComponent<Button>();
            btn.targetGraphic = img;
            btn.transition = Selectable.Transition.None;
            btn.onClick.AddListener(() => onClick());

            return go;
        }
        #endregion [END] BUILD RAIL ITEM

        #region [START] SELECT SCREEN
        public void SelectScreen(int index)
        {
            _activeScreen = index;
            for (int i = 0; i < SCREEN_COUNT; i++)
            {
                bool active = (i == index);
                if (_screens[i] != null) _screens[i].SetActive(active);
                if (_railBg[i] != null) _railBg[i].color = active ? ColAccentWash : Color.clear;
                if (_railTexts[i] != null) _railTexts[i].color = active ? ColText : ColTextMuted;
                if (_railIcons[i] != null) _railIcons[i].color = active ? ColAccent : ColBorderSoft;
                if (_railBars[i] != null) _railBars[i].SetActive(active);
            }

            // Same reason as in Open(): a screen that was inactive since Awake() never had its
            // nested layout groups computed, so the very first time it's switched to, force one.
            if (_screens[index] != null)
            {
                Canvas.ForceUpdateCanvases();
                LayoutRebuilder.ForceRebuildLayoutImmediate(_screens[index].GetComponent<RectTransform>());
            }

            Debug.Log($"[Farmer's Companion] Switched to Screen {index}");
        }
        #endregion [END] SELECT SCREEN

        // ---------------- SCREEN: OVERVIEW ----------------
        private GameObject _ovCardWater, _ovCardHarvest, _ovCardLivestock;
        private Text _ovStatWater, _ovStatHarvest, _ovStatSeeds, _ovStatusWater, _ovStatusHarvest, _ovStatusLivestock;

        #region [START] BUILD SCREEN OVERVIEW
        private GameObject BuildScreenOverview(GameObject parent)
        {
            var screen = CreateScreenShell(parent, "Overview", "Everything Farmer's Companion is doing across your raft, at a glance.", null, out var body);

            AddGroupLabel(body, "Categories");
            var catRowGO = new GameObject("CatGrid");
            catRowGO.transform.SetParent(body.transform, false);
            var catLe = catRowGO.AddComponent<LayoutElement>();
            catLe.preferredHeight = 132;
            var catLayout = catRowGO.AddComponent<HorizontalLayoutGroup>();
            catLayout.childControlWidth = true;
            catLayout.childControlHeight = true;
            catLayout.spacing = 12;
            catLayout.childForceExpandWidth = true;
            catLayout.childForceExpandHeight = true;
            catLayout.childControlWidth = true;
            catLayout.childControlHeight = true;

            _ovCardWater = BuildCategoryCard(catRowGO.transform, "Water & Growth", "Auto-watering, smart usage, and growth speed.", out _ovStatusWater, () => SelectScreen(1));
            _ovCardHarvest = BuildCategoryCard(catRowGO.transform, "Harvest & Seeds", "Reaping ripe crops and replanting seeds.", out _ovStatusHarvest, () => SelectScreen(2));
            _ovCardLivestock = BuildCategoryCard(catRowGO.transform, "Livestock", "Collecting wool and milk for you.", out _ovStatusLivestock, () => SelectScreen(3));

            AddGroupLabel(body, "This Session");
            var statGridGO = new GameObject("StatGrid");
            statGridGO.transform.SetParent(body.transform, false);
            var statLe = statGridGO.AddComponent<LayoutElement>();
            statLe.preferredHeight = 90;
            var statLayout = statGridGO.AddComponent<HorizontalLayoutGroup>();
            statLayout.childControlWidth = true;
            statLayout.childControlHeight = true;
            statLayout.spacing = 12;
            statLayout.childForceExpandWidth = true;
            statLayout.childForceExpandHeight = true;

            _ovStatWater = BuildStatTile(statGridGO.transform, "Plots watered");
            _ovStatHarvest = BuildStatTile(statGridGO.transform, "Crops harvested");
            _ovStatSeeds = BuildStatTile(statGridGO.transform, "Seeds replanted");

            var noteTxt = CreateText(body, "Since this session began.", 13, FontStyle.Italic, ColTextFaint, TextAnchor.MiddleLeft);
            var noteLe = noteTxt.gameObject.AddComponent<LayoutElement>();
            noteLe.preferredHeight = 16;

            AddCallout(body, "Using our other mods?",
                "Some features here also appear in Sailor's Companion, Inventory Master and Collection QoL. Turn each one on in a single mod only, so it never applies twice.");

            return screen;
        }
        #endregion [END] BUILD SCREEN OVERVIEW

        #region [START] BUILD CATEGORY CARD
        private GameObject BuildCategoryCard(Transform parent, string title, string desc, out Text statusText, Action onClick)
        {
            var go = new GameObject("Cat_" + title);
            go.transform.SetParent(parent, false);
            var img = go.AddComponent<Image>();
            img.sprite = GetRoundedSprite(11);
            img.type = Image.Type.Sliced;
            img.color = ColBorderSoft;
            var fillImg = AddInsetFill(go, 11, ColRow);

            var layout = go.AddComponent<VerticalLayoutGroup>();
            layout.childControlWidth = true;
            layout.childControlHeight = true;
            layout.padding = new RectOffset(14, 14, 12, 12);
            layout.spacing = 6;
            layout.childForceExpandWidth = true;
            layout.childForceExpandHeight = false;

            var nameTxt = CreateText(go, title, 16, FontStyle.Bold, ColText, TextAnchor.MiddleLeft);
            nameTxt.gameObject.AddComponent<LayoutElement>().preferredHeight = 18;

            var descTxt = CreateText(go, desc, 14, FontStyle.Normal, ColTextMuted, TextAnchor.UpperLeft);
            var descLe = descTxt.gameObject.AddComponent<LayoutElement>();
            descLe.preferredHeight = 30;
            descLe.flexibleHeight = 1f;

            statusText = CreateText(go, "-- active", 16f, FontStyle.Bold, ColAccent, TextAnchor.MiddleLeft);
            statusText.gameObject.AddComponent<LayoutElement>().preferredHeight = 16;

            var btn = go.AddComponent<Button>();
            btn.targetGraphic = fillImg;
            var cb = btn.colors;
            cb.normalColor = ColRow;
            cb.highlightedColor = new Color(ColRow.r + 0.03f, ColRow.g + 0.03f, ColRow.b + 0.03f, 1f);
            cb.pressedColor = ColPanel;
            btn.colors = cb;
            btn.onClick.AddListener(() => onClick());

            return go;
        }
        #endregion [END] BUILD CATEGORY CARD

        #region [START] BUILD STAT TILE
        private Text BuildStatTile(Transform parent, string label)
        {
            var go = new GameObject("Stat_" + label);
            go.transform.SetParent(parent, false);
            var img = go.AddComponent<Image>();
            img.sprite = GetRoundedSprite(11);
            img.type = Image.Type.Sliced;
            img.color = ColBorderSoft;
            AddInsetFill(go, 11, ColRow);

            var layout = go.AddComponent<VerticalLayoutGroup>();
            layout.childControlWidth = true;
            layout.childControlHeight = true;
            layout.padding = new RectOffset(16, 12, 10, 10);
            layout.childForceExpandWidth = true;

            var numTxt = CreateText(go, "0", 26, FontStyle.Bold, ColText, TextAnchor.MiddleLeft);
            numTxt.gameObject.AddComponent<LayoutElement>().preferredHeight = 30;

            var lblTxt = CreateText(go, label, 14, FontStyle.Normal, ColTextMuted, TextAnchor.MiddleLeft);
            lblTxt.gameObject.AddComponent<LayoutElement>().preferredHeight = 16;

            return numTxt;
        }
        #endregion [END] BUILD STAT TILE

        #region [START] REFRESH OVERVIEW
        private void RefreshOverview()
        {
            try
            {
                bool w1 = CropWaterManager.Instance != null && CropWaterManager.Instance.EnableAutoWater;
                bool w2 = CropWaterManager.Instance != null && CropWaterManager.Instance.EnableGrassWatering;
                bool w3 = CropWaterManager.Instance != null && CropWaterManager.Instance.SmartWaterUsage;
                bool g1 = CropGrowthManager.Instance != null && CropGrowthManager.Instance.EnableGrowthBoost;
                bool g2 = CropGrowthManager.Instance != null && CropGrowthManager.Instance.EnableTreeGrowthBoost;
                bool g3 = CropGrowthManager.Instance != null && CropGrowthManager.Instance.EnableFertilizerBoost;
                int waterOn = (w1 ? 1 : 0) + (w2 ? 1 : 0) + (w3 ? 1 : 0) + (g1 ? 1 : 0) + (g2 ? 1 : 0) + (g3 ? 1 : 0);
                SetCategoryStatus(_ovStatusWater, waterOn, 6);

                bool h1 = CropHarvestManager.Instance != null && CropHarvestManager.Instance.EnableAutoHarvest;
                bool h2 = CropHarvestManager.Instance != null && CropHarvestManager.Instance.EnableAutoReplant;
                bool h3 = CropHarvestManager.Instance != null && CropHarvestManager.Instance.EnableSeedSaver;
                int harvestOn = (h1 ? 1 : 0) + (h2 ? 1 : 0) + (h3 ? 1 : 0);
                SetCategoryStatus(_ovStatusHarvest, harvestOn, 3);

                bool l1 = LivestockManager.Instance != null && LivestockManager.Instance.EnableAutoCollectLivestock;
                bool l2 = CropIndicatorManager.Instance != null && CropIndicatorManager.Instance.EnableHealthIndicators;
                bool l3 = CropIndicatorManager.Instance != null && CropIndicatorManager.Instance.EnableNotifications;
                int liveOn = (l1 ? 1 : 0) + (l2 ? 1 : 0) + (l3 ? 1 : 0);
                SetCategoryStatus(_ovStatusLivestock, liveOn, 3);

                if (_ovStatWater != null) _ovStatWater.text = CropWaterManager.PlotsWateredThisSession.ToString(CultureInfo.InvariantCulture);
                if (_ovStatHarvest != null) _ovStatHarvest.text = CropHarvestManager.CropsHarvestedThisSession.ToString(CultureInfo.InvariantCulture);
                if (_ovStatSeeds != null) _ovStatSeeds.text = CropHarvestManager.SeedsReplantedThisSession.ToString(CultureInfo.InvariantCulture);
            }
            catch { }
        }
        #endregion [END] REFRESH OVERVIEW

        #region [START] SET CATEGORY STATUS
        private void SetCategoryStatus(Text t, int on, int total)
        {
            if (t == null) return;
            t.text = $"{on} of {total} active";
            t.color = on == total ? ColAccent : (on == 0 ? ColTextFaint : ColGold);
        }
        #endregion [END] SET CATEGORY STATUS

        // ---------------- SCREEN: WATER & GROWTH ----------------
        #region [START] BUILD SCREEN WATER GROWTH
        private GameObject BuildScreenWaterGrowth(GameObject parent)
        {
            var screen = CreateScreenShell(parent, "Water & Growth", "Keeps crop and grass plots hydrated and accelerates growth across your raft.", "F1", out var body);

            AddGroupLabel(body, "Watering");
            var wCard = CreateCard(body);
            AddToggleRow(wCard, "W", "Auto Water Crops", "Waters dry crop plots within range automatically.",
                () => CropWaterManager.Instance != null && CropWaterManager.Instance.EnableAutoWater,
                v => { if (CropWaterManager.Instance != null) CropWaterManager.Instance.EnableAutoWater = v; Plugin.EnableAutoWater.Value = v; });
            AddToggleRow(wCard, "G", "Animal Grass Watering", "Keeps grass plots watered so livestock always have something to graze.",
                () => CropWaterManager.Instance != null && CropWaterManager.Instance.EnableGrassWatering,
                v => { if (CropWaterManager.Instance != null) CropWaterManager.Instance.EnableGrassWatering = v; Plugin.EnableGrassWatering.Value = v; });
            AddToggleRow(wCard, "S", "Smart Water Usage", "Only waters plots that genuinely need it.",
                () => CropWaterManager.Instance != null && CropWaterManager.Instance.SmartWaterUsage,
                v => { if (CropWaterManager.Instance != null) CropWaterManager.Instance.SmartWaterUsage = v; Plugin.SmartWaterUsage.Value = v; });
            AddStepperRow(wCard, "R", "Farming Range Boost", "How far the raft is scanned for plots to water and harvest.",
                10f, 60f, 1f, "m",
                () => CropWaterManager.Instance != null ? CropWaterManager.Instance.WaterRadius : 30f,
                v => { if (CropWaterManager.Instance != null) CropWaterManager.Instance.WaterRadius = v; if (CropHarvestManager.Instance != null) CropHarvestManager.Instance.HarvestRadius = v; Plugin.WaterRadius.Value = v; });

            AddGroupLabel(body, "Growth");
            var gCard = CreateCard(body);
            AddToggleRow(gCard, "C", "Crop Growth Boost", "Speeds up how quickly planted crops mature.",
                () => CropGrowthManager.Instance != null && CropGrowthManager.Instance.EnableGrowthBoost,
                v => { if (CropGrowthManager.Instance != null) CropGrowthManager.Instance.EnableGrowthBoost = v; Plugin.EnableGrowthBoost.Value = v; }, true);
            AddStepperRow(gCard, "M", "Crop Growth Multiplier", "How much faster than normal your crops grow.",
                1f, 3f, 0.1f, "x",
                () => CropGrowthManager.Instance != null ? CropGrowthManager.Instance.CropGrowthMultiplier : 1.3f,
                v => { if (CropGrowthManager.Instance != null) CropGrowthManager.Instance.CropGrowthMultiplier = v; Plugin.CropGrowthMultiplier.Value = v; }, true);
            AddToggleRow(gCard, "T", "Tree Growth Boost", "Speeds up palm and fruit tree maturity.",
                () => CropGrowthManager.Instance != null && CropGrowthManager.Instance.EnableTreeGrowthBoost,
                v => { if (CropGrowthManager.Instance != null) CropGrowthManager.Instance.EnableTreeGrowthBoost = v; Plugin.EnableTreeGrowthBoost.Value = v; }, true);
            AddStepperRow(gCard, "M", "Tree Growth Multiplier", "How much faster than normal your trees grow.",
                1f, 3f, 0.1f, "x",
                () => CropGrowthManager.Instance != null ? CropGrowthManager.Instance.TreeGrowthMultiplier : 1.5f,
                v => { if (CropGrowthManager.Instance != null) CropGrowthManager.Instance.TreeGrowthMultiplier = v; Plugin.TreeGrowthMultiplier.Value = v; }, true);
            AddToggleRow(gCard, "F", "Fertilizer Surge", "Extra speed boost on fertilized plots specifically.",
                () => CropGrowthManager.Instance != null && CropGrowthManager.Instance.EnableFertilizerBoost,
                v => { if (CropGrowthManager.Instance != null) CropGrowthManager.Instance.EnableFertilizerBoost = v; Plugin.EnableFertilizerBoost.Value = v; }, true);

            AddCallout(body, "Range applies raft-wide", "Farming Range Boost affects Water & Growth and the Harvest tab together - raising it here also extends Auto Harvest's reach.");

            return screen;
        }
        #endregion [END] BUILD SCREEN WATER GROWTH

        // ---------------- SCREEN: HARVEST & SEEDS ----------------
        #region [START] BUILD SCREEN HARVEST SEEDS
        private GameObject BuildScreenHarvestSeeds(GameObject parent)
        {
            var screen = CreateScreenShell(parent, "Harvest & Seeds", "Reaps ripe crops for you and puts seeds back in the ground automatically.", "F1", out var body);

            AddGroupLabel(body, "Harvesting");
            var hCard = CreateCard(body);
            AddToggleRow(hCard, "H", "Auto Harvest Ripe Crops", "Harvests fully mature crops directly into your inventory.",
                () => CropHarvestManager.Instance != null && CropHarvestManager.Instance.EnableAutoHarvest,
                v => { if (CropHarvestManager.Instance != null) CropHarvestManager.Instance.EnableAutoHarvest = v; Plugin.EnableAutoHarvest.Value = v; });
            AddButtonRow(hCard, "M", "Multi-Harvest Sweep", "Instantly harvests every ripe crop within range, right now.", "Run Now", () =>
            {
                if (CropHarvestManager.Instance != null)
                {
                    int count = CropHarvestManager.Instance.HarvestNearbyCrops(CropHarvestManager.Instance.HarvestRadius, CropHarvestManager.Instance.EnableAutoReplant);
                    CropIndicatorManager.Instance?.ShowNotification($"Multi-Harvest: Harvested {count} ripe crops!");
                }
            });

            AddGroupLabel(body, "Replanting & Seeds");
            var sCard = CreateCard(body);
            AddToggleRow(sCard, "A", "Auto Replant", "Puts a matching seed back in the slot right after harvest.",
                () => CropHarvestManager.Instance != null && CropHarvestManager.Instance.EnableAutoReplant,
                v => { if (CropHarvestManager.Instance != null) CropHarvestManager.Instance.EnableAutoReplant = v; Plugin.EnableAutoReplant.Value = v; }, true);
            AddToggleRow(sCard, "S", "Seed Saver Mode", "Chance to keep the seed instead of consuming it on plant.",
                () => CropHarvestManager.Instance != null && CropHarvestManager.Instance.EnableSeedSaver,
                v => { if (CropHarvestManager.Instance != null) CropHarvestManager.Instance.EnableSeedSaver = v; Plugin.EnableSeedSaver.Value = v; }, true);
            AddStepperRow(sCard, "%", "Seed Saver Chance", "Chance per plant to preserve the seed.",
                5f, 50f, 1f, "%",
                () => CropHarvestManager.Instance != null ? CropHarvestManager.Instance.SeedSaverChancePercent : 25f,
                v => { if (CropHarvestManager.Instance != null) CropHarvestManager.Instance.SeedSaverChancePercent = v; Plugin.SeedSaverChancePercent.Value = v; }, true);

            return screen;
        }
        #endregion [END] BUILD SCREEN HARVEST SEEDS

        // ---------------- SCREEN: LIVESTOCK ----------------
        #region [START] BUILD SCREEN LIVESTOCK
        private GameObject BuildScreenLivestock(GameObject parent)
        {
            var screen = CreateScreenShell(parent, "Livestock", "Automatically collects wool and milk so you never have to chase animals down.", "F1", out var body);

            AddGroupLabel(body, "Auto-Collection");
            var card = CreateCard(body);
            AddToggleRow(card, "L", "Auto-Collect Livestock Products", "Shears llamas and milks goats automatically when ready.",
                () => LivestockManager.Instance != null && LivestockManager.Instance.EnableAutoCollectLivestock,
                v => { if (LivestockManager.Instance != null) LivestockManager.Instance.EnableAutoCollectLivestock = v; Plugin.EnableAutoCollectLivestock.Value = v; });

            AddGroupLabel(body, "Display");
            var dCard = CreateCard(body);
            AddToggleRow(dCard, "3", "3D Floating Crop Health HUD", "Shows in-world indicators for hydration and growth %.",
                () => CropIndicatorManager.Instance != null && CropIndicatorManager.Instance.EnableHealthIndicators,
                v => { if (CropIndicatorManager.Instance != null) CropIndicatorManager.Instance.EnableHealthIndicators = v; Plugin.EnableHealthIndicators.Value = v; });
            AddToggleRow(dCard, "T", "Toast Notifications", "Gentle on-screen alerts when crops or wool are ready.",
                () => CropIndicatorManager.Instance != null && CropIndicatorManager.Instance.EnableNotifications,
                v => { if (CropIndicatorManager.Instance != null) CropIndicatorManager.Instance.EnableNotifications = v; Plugin.EnableNotifications.Value = v; }, true);

            return screen;
        }
        #endregion [END] BUILD SCREEN LIVESTOCK

        // ---------------- SCREEN: UPDATES ----------------
        #region [START] BUILD SCREEN UPDATES
        private GameObject BuildScreenUpdates(GameObject parent)
        {
            var screen = CreateScreenShell(parent, "Updates", "Keep Farmer's Companion current without ever leaving the game.", "F1", out var body);

            // Status card
            var statusGO = new GameObject("StatusCard");
            statusGO.transform.SetParent(body.transform, false);
            var statusLe = statusGO.AddComponent<LayoutElement>();
            statusLe.preferredHeight = 76;
            var statusImg = statusGO.AddComponent<Image>();
            statusImg.sprite = GetRoundedSprite(11);
            statusImg.type = Image.Type.Sliced;
            statusImg.color = ColBorderSoft;
            AddInsetFill(statusGO, 11, ColRow);

            var statusLayout = statusGO.AddComponent<HorizontalLayoutGroup>();
            statusLayout.childControlWidth = true;
            statusLayout.childControlHeight = true;
            statusLayout.padding = new RectOffset(18, 14, 10, 10);
            statusLayout.childAlignment = TextAnchor.MiddleLeft;
            statusLayout.childForceExpandWidth = false;
            statusLayout.childForceExpandHeight = true;

            var vColGO = new GameObject("VCol");
            vColGO.transform.SetParent(statusGO.transform, false);
            var vColLe = vColGO.AddComponent<LayoutElement>();
            vColLe.flexibleWidth = 1f;
            var vColLayout = vColGO.AddComponent<VerticalLayoutGroup>();
            vColLayout.childControlWidth = true;
            vColLayout.childControlHeight = true;
            vColLayout.spacing = 4;
            vColLayout.childForceExpandWidth = true;

            var vTxt = CreateText(vColGO, $"Farmer's Companion  <color=#8FA3A9>v{PluginInfo.PLUGIN_VERSION}</color>", 18, FontStyle.Bold, ColText, TextAnchor.MiddleLeft);
            vTxt.horizontalOverflow = HorizontalWrapMode.Overflow;
            vTxt.gameObject.AddComponent<LayoutElement>().preferredHeight = 20;
            _versionFooterText = vTxt;

            var badgeGO = new GameObject("Badge");
            badgeGO.transform.SetParent(vColGO.transform, false);
            var badgeLe = badgeGO.AddComponent<LayoutElement>();
            badgeLe.preferredHeight = 22; badgeLe.preferredWidth = 150;
            var badgeImg = badgeGO.AddComponent<Image>();
            badgeImg.sprite = GetRoundedSprite(10);
            badgeImg.type = Image.Type.Sliced;
            badgeImg.color = ColSuccessWash;
            var badgeTxt = CreateText(badgeGO, "● Not checked yet", 13, FontStyle.Bold, ColTextMuted, TextAnchor.MiddleCenter);
            badgeTxt.horizontalOverflow = HorizontalWrapMode.Overflow;
            FillParent(badgeTxt.gameObject);
            _updateBadgeImg = badgeImg;
            _updateBadgeText = badgeTxt;

            var checkBtnGO = CreateGhostButton(statusGO.transform, "Check Now", () =>
            {
                UpdateChecker.Dismissed = false;
                UpdateChecker.Instance?.TriggerCheck();
                CropIndicatorManager.Instance?.ShowNotification("Checking GitHub for mod updates...");
                RefreshUpdateBadge();
            });
            checkBtnGO.AddComponent<LayoutElement>().preferredWidth = 130;

            AddGroupLabel(body, "Changelog");
            var changeCard = CreateCard(body);
            AddChangeRow(changeCard, $"v{PluginInfo.PLUGIN_VERSION}", "Fixed Auto-Water, Auto-Harvest, and Auto-Replant not syncing for other players in multiplayer.");
            AddChangeRow(changeCard, "v1.0.10", "Removed an unused internal helper. No functional changes.");
            AddChangeRow(changeCard, "v1.0.9", "Added diagnostic logging to help track down a menu hotkey issue.");

            AddGroupLabel(body, "Community");
            var linkRowGO = new GameObject("LinkRow");
            linkRowGO.transform.SetParent(body.transform, false);
            var linkLe = linkRowGO.AddComponent<LayoutElement>();
            linkLe.preferredHeight = 46;
            var linkLayout = linkRowGO.AddComponent<HorizontalLayoutGroup>();
            linkLayout.childControlWidth = true;
            linkLayout.childControlHeight = true;
            linkLayout.spacing = 12;
            linkLayout.childForceExpandWidth = true;
            linkLayout.childForceExpandHeight = true;

            CreateLinkButton(linkRowGO.transform, "View source on GitHub", () => { try { Application.OpenURL("https://github.com/RAVITEJAanand/FarmersCompanion-RaftMod"); } catch { } });
            CreateLinkButton(linkRowGO.transform, "Join the Discord", () => { try { Application.OpenURL("https://discord.gg/B4EMrR5Vrf"); } catch { } });

            return screen;
        }
        #endregion [END] BUILD SCREEN UPDATES

        #region [START] ADD CHANGE ROW
        private void AddChangeRow(GameObject card, string version, string desc)
        {
            var rowGO = new GameObject("Change");
            rowGO.transform.SetParent(card.transform, false);
            var le = rowGO.AddComponent<LayoutElement>();
            le.preferredHeight = 54;
            var layout = rowGO.AddComponent<HorizontalLayoutGroup>();
            layout.childControlWidth = true;
            layout.childControlHeight = true;
            layout.padding = new RectOffset(16, 16, 10, 10);
            layout.spacing = 12;
            layout.childForceExpandWidth = false;
            layout.childForceExpandHeight = true;

            int existingChangeRows = 0;
            foreach (Transform t in card.transform) { if (t.name == "Change") existingChangeRows++; }
            if (existingChangeRows > 1) AddRowSeparator(rowGO);

            var verTxt = CreateText(rowGO, version, 16f, FontStyle.Bold, ColAccent, TextAnchor.UpperLeft);
            verTxt.gameObject.AddComponent<LayoutElement>().preferredWidth = 48;

            var descTxt = CreateText(rowGO, desc, 14, FontStyle.Normal, ColTextMuted, TextAnchor.UpperLeft);
            var descLe = descTxt.gameObject.AddComponent<LayoutElement>();
            descLe.flexibleWidth = 1f;
        }
        #endregion [END] ADD CHANGE ROW

        // ---------------- shared shell / components ----------------
        #region [START] CREATE SCREEN SHELL
        private GameObject CreateScreenShell(GameObject parent, string title, string desc, string hotkey, out GameObject body)
        {
            var screen = new GameObject("Screen_" + title);
            screen.transform.SetParent(parent.transform, false);
            var screenRt = screen.AddComponent<RectTransform>();
            screenRt.anchorMin = Vector2.zero;
            screenRt.anchorMax = Vector2.one;
            screenRt.offsetMin = Vector2.zero;
            screenRt.offsetMax = Vector2.zero;

            // head
            var headGO = new GameObject("Head");
            headGO.transform.SetParent(screen.transform, false);
            var headRt = headGO.AddComponent<RectTransform>();
            headRt.anchorMin = new Vector2(0, 1);
            headRt.anchorMax = new Vector2(1, 1);
            headRt.pivot = new Vector2(0.5f, 1);
            headRt.sizeDelta = new Vector2(0, 112);
            headRt.anchoredPosition = Vector2.zero;

            var headLayout = headGO.AddComponent<HorizontalLayoutGroup>();
            headLayout.childControlWidth = true;
            headLayout.childControlHeight = true;
            // Right padding widened from 26 to 60: the close button (added after this screen tree,
            // anchored to the window's own top-right corner, not to this head) claims a 32x32 area
            // inset only 16px from the window edge. With the old 26px padding the "Menu F1" hotkey
            // chip's own right edge landed inside the close button's horizontal span - the two never
            // visibly collided in testing, but the margin was only a few px, easy to lose on a
            // different aspect ratio. This keeps the chip clear of the close button with real margin.
            headLayout.padding = new RectOffset(28, 60, 16, 12);
            headLayout.childForceExpandWidth = false;
            headLayout.childForceExpandHeight = true;

            var titleColGO = new GameObject("TitleCol");
            titleColGO.transform.SetParent(headGO.transform, false);
            var titleColLe = titleColGO.AddComponent<LayoutElement>();
            titleColLe.flexibleWidth = 1f;
            var titleColLayout = titleColGO.AddComponent<VerticalLayoutGroup>();
            titleColLayout.childControlWidth = true;
            titleColLayout.childControlHeight = true;
            titleColLayout.spacing = 3;
            titleColLayout.childForceExpandWidth = true;

            var titleTxt = CreateText(titleColGO, title, 24, FontStyle.Bold, ColText, TextAnchor.MiddleLeft);
            titleTxt.gameObject.AddComponent<LayoutElement>().preferredHeight = 30;
            var descTxt = CreateText(titleColGO, desc, 15, FontStyle.Normal, ColTextMuted, TextAnchor.UpperLeft);
            var descLe = descTxt.gameObject.AddComponent<LayoutElement>();
            descLe.preferredHeight = 44;
            descLe.flexibleHeight = 1f;


            if (!string.IsNullOrEmpty(hotkey))
            {
                var hkGO = new GameObject("Hotkey");
                hkGO.transform.SetParent(headGO.transform, false);
                var hkLe = hkGO.AddComponent<LayoutElement>();
                hkLe.preferredWidth = 100;
                var hkLayout = hkGO.AddComponent<HorizontalLayoutGroup>();
                hkLayout.childControlWidth = true;
                hkLayout.childControlHeight = true;
                hkLayout.childAlignment = TextAnchor.MiddleRight;
                hkLayout.spacing = 6;
                hkLayout.childForceExpandWidth = false;
                hkLayout.childForceExpandHeight = true;

                var hkLbl = CreateText(hkGO, "Menu", 13, FontStyle.Normal, ColTextFaint, TextAnchor.MiddleRight);
                hkLbl.gameObject.AddComponent<LayoutElement>().preferredWidth = 34;
                var hkChip = CreateKeyChip(hkGO.transform, hotkey);
            }

            AddDivider(screen.transform, 0, headRt);

            // Scrolling body: a card-heavy screen (e.g. 5 rows + a callout on Water & Growth) can
            // easily need more height than the fixed panel has room for. Previously this area had
            // no viewport/mask and no ScrollRect, so overflowing content didn't get cut off or
            // pushed anywhere - it just kept stacking downward past the panel's own layout and
            // visually landed on top of whatever was still on-screen below it (e.g. the "Range
            // applies raft-wide" callout ending up drawn directly over the last toggle row). A real
            // scroll view guarantees every screen's content is reachable and never overlaps itself,
            // regardless of how many rows a given tab ends up with.
            var scrollGO = new GameObject("ScrollArea");
            scrollGO.transform.SetParent(screen.transform, false);
            var scrollRt = scrollGO.AddComponent<RectTransform>();
            scrollRt.anchorMin = Vector2.zero;
            scrollRt.anchorMax = Vector2.one;
            scrollRt.offsetMin = new Vector2(28, 20);
            scrollRt.offsetMax = new Vector2(-26, -112);

            var viewportGO = new GameObject("Viewport");
            viewportGO.transform.SetParent(scrollGO.transform, false);
            var viewportRt = viewportGO.AddComponent<RectTransform>();
            viewportRt.anchorMin = Vector2.zero;
            viewportRt.anchorMax = Vector2.one;
            viewportRt.offsetMin = Vector2.zero;
            viewportRt.offsetMax = Vector2.zero;
            viewportGO.AddComponent<RectMask2D>();

            body = new GameObject("Body");
            body.transform.SetParent(viewportGO.transform, false);
            var bodyRt = body.AddComponent<RectTransform>();
            bodyRt.anchorMin = new Vector2(0, 1);
            bodyRt.anchorMax = new Vector2(1, 1);
            bodyRt.pivot = new Vector2(0.5f, 1);
            bodyRt.anchoredPosition = Vector2.zero;
            // RectTransform.sizeDelta defaults to (100,100) when never set explicitly. With
            // horizontal-stretch anchors that adds 100 EXTRA px of width beyond the viewport
            // (actual width = parentWidth + sizeDelta.x), centered on the pivot - so every row's
            // text ends up cut off equally on both the left and right edges once RectMask2D clips
            // it back down to the viewport. Zeroing sizeDelta.x removes that phantom overflow;
            // ContentSizeFitter still drives sizeDelta.y every layout pass as intended.
            bodyRt.sizeDelta = Vector2.zero;

            var bodyLayout = body.AddComponent<VerticalLayoutGroup>();
            bodyLayout.childControlWidth = true;
            bodyLayout.childControlHeight = true;
            bodyLayout.spacing = 14;
            bodyLayout.childForceExpandWidth = true;
            bodyLayout.childForceExpandHeight = false;

            var bodyFitter = body.AddComponent<ContentSizeFitter>();
            bodyFitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

            var scrollRect = scrollGO.AddComponent<ScrollRect>();
            scrollRect.content = bodyRt;
            scrollRect.viewport = viewportRt;
            scrollRect.horizontal = false;
            scrollRect.vertical = true;
            scrollRect.movementType = ScrollRect.MovementType.Clamped;
            scrollRect.scrollSensitivity = 28f;

            return screen;
        }
        #endregion [END] CREATE SCREEN SHELL

        #region [START] ADD GROUP LABEL
        private void AddGroupLabel(GameObject parent, string text)
        {
            var go = new GameObject("GroupLabel");
            go.transform.SetParent(parent.transform, false);
            go.AddComponent<LayoutElement>().preferredHeight = 16;
            var t = CreateText(go, text.ToUpperInvariant(), 13, FontStyle.Bold, ColTextFaint, TextAnchor.MiddleLeft);
            FillParent(t.gameObject);
        }
        #endregion [END] ADD GROUP LABEL

        #region [START] CREATE CARD
        private GameObject CreateCard(GameObject parent)
        {
            var go = new GameObject("Card");
            go.transform.SetParent(parent.transform, false);
            var img = go.AddComponent<Image>();
            img.sprite = GetRoundedSprite(11);
            img.type = Image.Type.Sliced;
            img.color = ColBorderSoft;
            AddInsetFill(go, 11, ColRow);

            var layout = go.AddComponent<VerticalLayoutGroup>();
            layout.childControlWidth = true;
            layout.childControlHeight = true;
            layout.childForceExpandWidth = true;
            layout.childForceExpandHeight = false;
            layout.childControlHeight = true;

            var fitter = go.AddComponent<ContentSizeFitter>();
            fitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

            var le = go.AddComponent<LayoutElement>();
            le.flexibleHeight = 0f;

            return go;
        }
        #endregion [END] CREATE CARD

        #region [START] ADD ROW SEPARATOR
        private void AddRowSeparator(GameObject row)
        {
            var sepGO = new GameObject("Sep");
            sepGO.transform.SetParent(row.transform, false);
            sepGO.transform.SetAsFirstSibling();
            var sepRt = sepGO.AddComponent<RectTransform>();
            sepRt.anchorMin = new Vector2(0, 1);
            sepRt.anchorMax = new Vector2(1, 1);
            sepRt.pivot = new Vector2(0.5f, 1);
            sepRt.sizeDelta = new Vector2(0, 1);
            sepRt.anchoredPosition = Vector2.zero;
            var img = sepGO.AddComponent<Image>();
            img.color = ColBorderSoft;
        }
        #endregion [END] ADD ROW SEPARATOR

        #region [START] ADD TOGGLE ROW
        private GameObject AddToggleRow(GameObject card, string monogram, string title, string desc, Func<bool> getter, Action<bool> setter, bool secondary = false)
        {
            var rowGO = CreateRowShell(card, monogram, title, desc, secondary, out _);
            var sw = CreateToggleSwitch(rowGO.transform, getter(), setter);
            sw.AddComponent<LayoutElement>();
            return rowGO;
        }
        #endregion [END] ADD TOGGLE ROW

        #region [START] ADD BUTTON ROW
        private GameObject AddButtonRow(GameObject card, string monogram, string title, string desc, string buttonLabel, Action onClick)
        {
            var rowGO = CreateRowShell(card, monogram, title, desc, false, out _);
            var btnGO = CreateGhostButton(rowGO.transform, buttonLabel, onClick);
            btnGO.AddComponent<LayoutElement>().preferredWidth = 110;
            return rowGO;
        }
        #endregion [END] ADD BUTTON ROW

        #region [START] ADD STEPPER ROW
        private GameObject AddStepperRow(GameObject card, string monogram, string title, string desc, float min, float max, float step, string suffix, Func<float> getter, Action<float> setter, bool secondary = false)
        {
            var rowGO = CreateRowShell(card, monogram, title, desc, secondary, out _);

            var stepperGO = new GameObject("Stepper");
            stepperGO.transform.SetParent(rowGO.transform, false);
            stepperGO.AddComponent<LayoutElement>().preferredWidth = 175;
            var stepperLayout = stepperGO.AddComponent<HorizontalLayoutGroup>();
            stepperLayout.childControlWidth = true;
            stepperLayout.childControlHeight = true;
            stepperLayout.spacing = 8;
            stepperLayout.childAlignment = TextAnchor.MiddleRight;
            stepperLayout.childForceExpandWidth = false;
            stepperLayout.childForceExpandHeight = true;

            var trackGO = new GameObject("Track");
            trackGO.transform.SetParent(stepperGO.transform, false);
            trackGO.AddComponent<LayoutElement>().preferredWidth = 100;
            var track = trackGO.GetComponent<RectTransform>() ?? trackGO.AddComponent<RectTransform>();
            var trackImg = trackGO.AddComponent<Image>();
            trackImg.sprite = GetRoundedSprite(2);
            trackImg.type = Image.Type.Sliced;
            trackImg.color = ColBorder;

            var trackHeightGO = new GameObject("TH");
            trackHeightGO.transform.SetParent(trackGO.transform, false);
            var thRt = trackHeightGO.AddComponent<RectTransform>();
            thRt.anchorMin = new Vector2(0, 0.5f); thRt.anchorMax = new Vector2(1, 0.5f);
            thRt.sizeDelta = new Vector2(0, 5);
            thRt.anchoredPosition = Vector2.zero;

            var fillGO = new GameObject("Fill");
            fillGO.transform.SetParent(trackGO.transform, false);
            var fillRt = fillGO.AddComponent<RectTransform>();
            fillRt.anchorMin = new Vector2(0, 0.5f);
            fillRt.anchorMax = new Vector2(0, 0.5f);
            fillRt.pivot = new Vector2(0, 0.5f);
            fillRt.sizeDelta = new Vector2(10, 5);
            var fillImg = fillGO.AddComponent<Image>();
            fillImg.sprite = GetRoundedSprite(2);
            fillImg.type = Image.Type.Sliced;
            fillImg.color = ColAccent;

            var thumbGO = new GameObject("Thumb");
            thumbGO.transform.SetParent(trackGO.transform, false);
            var thumbRt = thumbGO.AddComponent<RectTransform>();
            thumbRt.anchorMin = new Vector2(0, 0.5f);
            thumbRt.anchorMax = new Vector2(0, 0.5f);
            thumbRt.pivot = new Vector2(0.5f, 0.5f);
            thumbRt.sizeDelta = new Vector2(12, 12);
            var thumbImg = thumbGO.AddComponent<Image>();
            thumbImg.sprite = GetRoundedSprite(6);
            thumbImg.type = Image.Type.Sliced;
            thumbImg.color = ColText;

            var valGO = new GameObject("Val");
            valGO.transform.SetParent(stepperGO.transform, false);
            valGO.AddComponent<LayoutElement>().preferredWidth = 56;
            var valTxt = CreateText(valGO, "", 14, FontStyle.Normal, ColText, TextAnchor.MiddleRight);
            FillParent(valTxt.gameObject);

            var slider = trackGO.AddComponent<Slider>();
            slider.direction = Slider.Direction.LeftToRight;
            slider.minValue = min;
            slider.maxValue = max;
            slider.fillRect = fillRt;
            slider.handleRect = thumbRt;
            slider.targetGraphic = thumbImg;

            string fmt = step < 1f ? "F1" : "F0";
            void Refresh(float v)
            {
                valTxt.text = v.ToString(fmt, CultureInfo.InvariantCulture) + suffix;
            }

            float initial = getter();
            slider.value = initial;
            Refresh(initial);
            slider.onValueChanged.AddListener(v =>
            {
                setter(v);
                Refresh(v);
            });

            return rowGO;
        }
        #endregion [END] ADD STEPPER ROW

        #region [START] CREATE ROW SHELL
        private GameObject CreateRowShell(GameObject card, string monogram, string title, string desc, bool secondary, out RectTransform rt)
        {
            var rowGO = new GameObject("Row");
            rowGO.transform.SetParent(card.transform, false);
            rt = rowGO.AddComponent<RectTransform>();
            rowGO.AddComponent<LayoutElement>().preferredHeight = 72;

            var layout = rowGO.AddComponent<HorizontalLayoutGroup>();
            layout.childControlWidth = true;
            layout.childControlHeight = true;
            layout.padding = new RectOffset(18, 18, 12, 12);
            layout.spacing = 16;
            layout.childAlignment = TextAnchor.MiddleLeft;
            layout.childForceExpandWidth = false;
            layout.childForceExpandHeight = true;

            int existingRows = 0;
            foreach (Transform t in card.transform) { if (t.name == "Row") existingRows++; }
            if (existingRows > 1) AddRowSeparator(rowGO);

            var iconGO = new GameObject("Icon");
            iconGO.transform.SetParent(rowGO.transform, false);
            iconGO.AddComponent<LayoutElement>().preferredWidth = 40;
            var iconImg = iconGO.AddComponent<Image>();
            iconImg.sprite = GetRoundedSprite(11);
            iconImg.type = Image.Type.Sliced;
            iconImg.color = ColPanel2;
            var iconTxt = CreateText(iconGO, monogram, 16, FontStyle.Bold, secondary ? ColGold : ColAccent, TextAnchor.MiddleCenter);
            FillParent(iconTxt.gameObject);

            var textColGO = new GameObject("Text");
            textColGO.transform.SetParent(rowGO.transform, false);
            var textColLe = textColGO.AddComponent<LayoutElement>();
            textColLe.flexibleWidth = 1f;
            var textColLayout = textColGO.AddComponent<VerticalLayoutGroup>();
            textColLayout.childControlWidth = true;
            textColLayout.childControlHeight = true;
            textColLayout.childForceExpandWidth = true;
            textColLayout.spacing = 2;
            textColLayout.childAlignment = TextAnchor.MiddleLeft;

            var titleTxt = CreateText(textColGO, title, 18, FontStyle.Bold, ColText, TextAnchor.MiddleLeft);
            titleTxt.gameObject.AddComponent<LayoutElement>().preferredHeight = 24;
            var descTxt = CreateText(textColGO, desc, 14, FontStyle.Normal, ColTextMuted, TextAnchor.UpperLeft);
            var descLe = descTxt.gameObject.AddComponent<LayoutElement>();
            descLe.preferredHeight = 34;

            return rowGO;
        }
        #endregion [END] CREATE ROW SHELL

        // ---------------- real animated toggle switch ----------------
        private const float SwitchW = 48, SwitchH = 27, KnobSize = 21, KnobMargin = 3;

        #region [START] CREATE TOGGLE SWITCH
        private GameObject CreateToggleSwitch(Transform parent, bool initial, Action<bool> onChange)
        {
            var go = new GameObject("Switch");
            go.transform.SetParent(parent, false);
            var rt = go.AddComponent<RectTransform>();
            rt.sizeDelta = new Vector2(SwitchW, SwitchH);
            var le = go.AddComponent<LayoutElement>();
            le.preferredWidth = SwitchW; le.preferredHeight = SwitchH;

            var trackImg = go.AddComponent<Image>();
            trackImg.sprite = GetRoundedSprite((int)(SwitchH / 2));
            trackImg.type = Image.Type.Sliced;
            trackImg.color = initial ? ColAccent : ColBorder;

            var knobGO = new GameObject("Knob");
            knobGO.transform.SetParent(go.transform, false);
            var knobRt = knobGO.AddComponent<RectTransform>();
            knobRt.anchorMin = new Vector2(0, 0.5f);
            knobRt.anchorMax = new Vector2(0, 0.5f);
            knobRt.pivot = new Vector2(0, 0.5f);
            knobRt.sizeDelta = new Vector2(KnobSize, KnobSize);
            knobRt.anchoredPosition = new Vector2(initial ? SwitchW - KnobSize - KnobMargin : KnobMargin, 0);
            var knobImg = knobGO.AddComponent<Image>();
            knobImg.sprite = GetRoundedSprite((int)(KnobSize / 2));
            knobImg.type = Image.Type.Sliced;
            knobImg.color = ColBg;

            bool state = initial;
            var btn = go.AddComponent<Button>();
            btn.targetGraphic = trackImg;
            btn.transition = Selectable.Transition.None;
            btn.onClick.AddListener(() =>
            {
                state = !state;
                onChange(state);
                StopAllCoroutinesOn(go);
                StartCoroutine(AnimateSwitch(knobRt, trackImg, state));
            });

            return go;
        }
        #endregion [END] CREATE TOGGLE SWITCH

        #region [START] STOP ALL COROUTINES ON
        private void StopAllCoroutinesOn(GameObject go)
        {
            // switches don't run their own MonoBehaviour, animation coroutines are hosted on
            // this Canvas component itself; nothing to stop per-switch, kept for clarity/future use.
        }
        #endregion [END] STOP ALL COROUTINES ON

        #region [START] ANIMATE SWITCH
        private IEnumerator AnimateSwitch(RectTransform knob, Image track, bool on)
        {
            float duration = 0.12f;
            float t = 0f;
            float fromX = knob.anchoredPosition.x;
            float toX = on ? SwitchW - KnobSize - KnobMargin : KnobMargin;
            Color fromC = track.color;
            Color toC = on ? ColAccent : ColBorder;
            while (t < duration)
            {
                t += Time.unscaledDeltaTime;
                float k = Mathf.Clamp01(t / duration);
                knob.anchoredPosition = new Vector2(Mathf.Lerp(fromX, toX, k), 0);
                track.color = Color.Lerp(fromC, toC, k);
                yield return null;
            }
            knob.anchoredPosition = new Vector2(toX, 0);
            track.color = toC;
        }
        #endregion [END] ANIMATE SWITCH

        // ---------------- misc small components ----------------
        #region [START] CREATE GHOST BUTTON
        private GameObject CreateGhostButton(Transform parent, string label, Action onClick)
        {
            var go = new GameObject("GhostBtn");
            go.transform.SetParent(parent, false);
            var img = go.AddComponent<Image>();
            img.sprite = GetRoundedSprite(8);
            img.type = Image.Type.Sliced;
            img.color = ColBorder;
            var fillImg = AddInsetFill(go, 8, ColPanel2);

            var btn = go.AddComponent<Button>();
            btn.targetGraphic = fillImg;
            var cb = btn.colors;
            cb.normalColor = ColPanel2;
            cb.highlightedColor = ColAccentWash;
            cb.pressedColor = ColRow;
            btn.colors = cb;
            btn.onClick.AddListener(() => onClick());

            var txt = CreateText(go, label, 17f, FontStyle.Bold, ColText, TextAnchor.MiddleCenter);
            FillParent(txt.gameObject);
            return go;
        }
        #endregion [END] CREATE GHOST BUTTON

        #region [START] CREATE LINK BUTTON
        private void CreateLinkButton(Transform parent, string label, Action onClick)
        {
            var go = new GameObject("Link");
            go.transform.SetParent(parent, false);
            var img = go.AddComponent<Image>();
            img.sprite = GetRoundedSprite(10);
            img.type = Image.Type.Sliced;
            img.color = ColBorderSoft;
            var fillImg = AddInsetFill(go, 10, ColRow);

            var layout = go.AddComponent<HorizontalLayoutGroup>();
            layout.childControlWidth = true;
            layout.childControlHeight = true;
            layout.padding = new RectOffset(16, 12, 0, 0);
            layout.childAlignment = TextAnchor.MiddleLeft;
            layout.childForceExpandHeight = true;

            var btn = go.AddComponent<Button>();
            btn.targetGraphic = fillImg;
            var cb = btn.colors;
            cb.normalColor = ColRow;
            cb.highlightedColor = ColAccentWash;
            btn.colors = cb;
            btn.onClick.AddListener(() => onClick());

            var txt = CreateText(go, label, 17f, FontStyle.Bold, ColText, TextAnchor.MiddleLeft);
            FillParent(txt.gameObject);
        }
        #endregion [END] CREATE LINK BUTTON

        #region [START] CREATE KEY CHIP
        private GameObject CreateKeyChip(Transform parent, string text)
        {
            var go = new GameObject("Chip");
            go.transform.SetParent(parent, false);
            go.AddComponent<LayoutElement>().preferredWidth = 36;
            var img = go.AddComponent<Image>();
            img.sprite = GetRoundedSprite(5);
            img.type = Image.Type.Sliced;
            img.color = ColBorder;
            AddInsetFill(go, 5, ColRow);
            var txt = CreateText(go, text, 16f, FontStyle.Bold, ColTextMuted, TextAnchor.MiddleCenter);
            FillParent(txt.gameObject);
            return go;
        }
        #endregion [END] CREATE KEY CHIP

        #region [START] ADD CALLOUT
        private void AddCallout(GameObject parent, string title, string desc)
        {
            var go = new GameObject("Callout");
            go.transform.SetParent(parent.transform, false);
            var le = go.AddComponent<LayoutElement>();
            le.preferredHeight = 88;
            var img = go.AddComponent<Image>();
            img.sprite = GetRoundedSprite(11);
            img.type = Image.Type.Sliced;
            img.color = new Color(ColGold.r, ColGold.g, ColGold.b, 0.12f);
            var outline = go.AddComponent<Outline>();
            outline.effectColor = new Color(ColGold.r, ColGold.g, ColGold.b, 0.30f);
            outline.effectDistance = new Vector2(1, -1);

            var layout = go.AddComponent<VerticalLayoutGroup>();
            layout.childControlWidth = true;
            layout.childControlHeight = true;
            layout.padding = new RectOffset(16, 16, 8, 8);
            layout.spacing = 2;
            layout.childForceExpandWidth = true;

            var titleTxt = CreateText(go, title, 15, FontStyle.Bold, ColGold, TextAnchor.MiddleLeft);
            titleTxt.horizontalOverflow = HorizontalWrapMode.Overflow;
            titleTxt.gameObject.AddComponent<LayoutElement>().preferredHeight = 18;
            // Descriptions here run long enough to wrap to two lines; Unity truncates a wrapped
            // line that doesn't fit its box, so both this and the callout itself are sized for two.
            var descTxt = CreateText(go, desc, 14, FontStyle.Normal, ColTextMuted, TextAnchor.UpperLeft);
            descTxt.gameObject.AddComponent<LayoutElement>().preferredHeight = 46;
        }
        #endregion [END] ADD CALLOUT

        #region [START] ADD DIVIDER
        private void AddDivider(Transform parent, float marginBottom, RectTransform anchorBelow = null)
        {
            if (anchorBelow != null)
            {
                var dGO = new GameObject("Divider");
                dGO.transform.SetParent(parent, false);
                var dRt = dGO.AddComponent<RectTransform>();
                dRt.anchorMin = new Vector2(0, 1);
                dRt.anchorMax = new Vector2(1, 1);
                dRt.pivot = new Vector2(0.5f, 1);
                dRt.sizeDelta = new Vector2(0, 1);
                dRt.anchoredPosition = new Vector2(0, -anchorBelow.sizeDelta.y);
                var img = dGO.AddComponent<Image>();
                img.color = ColBorderSoft;
                return;
            }

            var go = new GameObject("Divider");
            go.transform.SetParent(parent, false);
            go.AddComponent<LayoutElement>().preferredHeight = 1;
            var im = go.AddComponent<Image>();
            im.color = ColBorderSoft;
        }
        #endregion [END] ADD DIVIDER

        #region [START] BUILD FOOTER
        private void BuildFooter(GameObject parent)
        {
            var footGO = new GameObject("Footer");
            footGO.transform.SetParent(parent.transform, false);
            var fRt = footGO.AddComponent<RectTransform>();
            fRt.anchorMin = new Vector2(0, 0);
            fRt.anchorMax = new Vector2(1, 0);
            fRt.pivot = new Vector2(0.5f, 0);
            fRt.sizeDelta = new Vector2(0, 52);
            fRt.anchoredPosition = Vector2.zero;

            AddDivider(footGO.transform, 0);
            var topLine = footGO.transform.Find("Divider");
            if (topLine != null)
            {
                var tlRt = topLine.GetComponent<RectTransform>();
                tlRt.anchorMin = new Vector2(0, 1);
                tlRt.anchorMax = new Vector2(1, 1);
                tlRt.pivot = new Vector2(0.5f, 1);
                tlRt.sizeDelta = new Vector2(0, 1);
                tlRt.anchoredPosition = Vector2.zero;

                // footGO's own HorizontalLayoutGroup (added right below) treats every one of its
                // children as a managed row item unless told otherwise - without this, the very
                // next layout rebuild (Open()/SelectScreen() both force one) would collapse this
                // divider's carefully-set full-width top-border anchors into "just another item in
                // the row", rendering as an unexplained grey box sized by leftover layout math
                // instead of the thin border line it's meant to be.
                var tlLe = topLine.GetComponent<LayoutElement>();
                if (tlLe == null) tlLe = topLine.gameObject.AddComponent<LayoutElement>();
                tlLe.ignoreLayout = true;
            }

            var layout = footGO.AddComponent<HorizontalLayoutGroup>();
            layout.childControlWidth = true;
            layout.childControlHeight = true;
            layout.padding = new RectOffset(28, 26, 8, 8);
            layout.childForceExpandHeight = true;
            layout.childAlignment = TextAnchor.MiddleLeft;

            var verGO = new GameObject("Ver");
            verGO.transform.SetParent(footGO.transform, false);
            verGO.AddComponent<LayoutElement>().flexibleWidth = 1f;
            var verTxt = CreateText(verGO, $"Farmer's Companion  <color=#8FA3A9>v{PluginInfo.PLUGIN_VERSION} · not checked yet</color>", 15f, FontStyle.Normal, ColTextMuted, TextAnchor.MiddleLeft);
            // This line sits in a single-line-tall footer strip. CreateText defaults to Wrap, and
            // Unity's Text component defaults verticalOverflow to Truncate - so if the line is ever
            // long enough to wrap (e.g. "... up to <newline> date"), the wrapped second line just
            // gets silently clipped away instead of showing, leaving a sentence that looks cut off
            // mid-word. Force single-line so it extends horizontally (into the ample flexible-width
            // space already reserved for it) instead of ever wrapping.
            verTxt.horizontalOverflow = HorizontalWrapMode.Overflow;
            FillParent(verTxt.gameObject);
            _footerVerText = verTxt;

            var btnGO = new GameObject("Btn_Check");
            btnGO.transform.SetParent(footGO.transform, false);
            btnGO.AddComponent<LayoutElement>().preferredWidth = 175;
            var btnImg = btnGO.AddComponent<Image>();
            btnImg.sprite = GetRoundedSprite(8);
            btnImg.type = Image.Type.Sliced;
            btnImg.color = ColAccent;
            var btn = btnGO.AddComponent<Button>();
            btn.targetGraphic = btnImg;
            var cb = btn.colors;
            cb.normalColor = ColAccent;
            cb.highlightedColor = ColAccentStrong;
            cb.pressedColor = ColAccentStrong;
            btn.colors = cb;
            btn.onClick.AddListener(() =>
            {
                UpdateChecker.Dismissed = false;
                UpdateChecker.Instance?.TriggerCheck();
                CropIndicatorManager.Instance?.ShowNotification("Checking GitHub for mod updates...");
                RefreshUpdateBadge();
            });
            var btnTxt = CreateText(btnGO, "Check for Updates", 17f, FontStyle.Bold, ColOnAccentTxt, TextAnchor.MiddleCenter);
            FillParent(btnTxt.gameObject);
        }
        #endregion [END] BUILD FOOTER

        // The footer/badge text used to be hardcoded ("up to date") at build time and never
        // touched again, so it kept lying about the mod's update status forever - a real check
        // via UpdateChecker.TriggerCheck() DID run and DID toast the true result, but nothing
        // ever wrote that result back into the persistent UI. This keeps both in sync with
        // UpdateChecker's actual state, and PollUpdateStatus calls it periodically so the badge
        // updates within ~1s of any check completing, not just right after a button click.
        #region [START] REFRESH UPDATE BADGE
        private void RefreshUpdateBadge()
        {
            string suffix, badgeLabel;
            Color badgeBg, badgeFg;

            if (UpdateChecker.IsChecking)
            {
                suffix = "checking...";
                badgeLabel = "● Checking...";
                badgeBg = ColPanel2; badgeFg = ColTextMuted;
            }
            else if (!UpdateChecker.HasChecked)
            {
                suffix = "not checked yet";
                badgeLabel = "● Not checked yet";
                badgeBg = ColPanel2; badgeFg = ColTextMuted;
            }
            else if (UpdateChecker.IsUpdateAvailable)
            {
                suffix = $"v{UpdateChecker.LatestVersion} available";
                badgeLabel = "● Update available";
                badgeBg = new Color(ColGold.r, ColGold.g, ColGold.b, 0.16f);
                badgeFg = ColGold;
            }
            else
            {
                suffix = "up to date";
                badgeLabel = "● Up to date";
                badgeBg = ColSuccessWash; badgeFg = ColSuccess;
            }

            if (_footerVerText != null)
                _footerVerText.text = $"Farmer's Companion  <color=#8FA3A9>v{PluginInfo.PLUGIN_VERSION} · {suffix}</color>";
            if (_updateBadgeText != null) { _updateBadgeText.text = badgeLabel; _updateBadgeText.color = badgeFg; }
            if (_updateBadgeImg != null) _updateBadgeImg.color = badgeBg;
        }
        #endregion [END] REFRESH UPDATE BADGE

        #region [START] POLL UPDATE STATUS
        private IEnumerator PollUpdateStatus()
        {
            while (true)
            {
                RefreshUpdateBadge();
                yield return new WaitForSeconds(1f);
            }
        }
        #endregion [END] POLL UPDATE STATUS

        // Outline (UnityEngine.UI.Outline) only renders an offset shadow-duplicate of a graphic - it
        // does NOT draw a stroke around a filled shape's edges, so every "bordered card" in this file
        // that relied on it rendered as a flat, undifferentiated box. This lays a slightly-inset fill
        // Image on top of the (now border-colored) parent Image, producing a real visible border ring.
        #region [START] ADD INSET FILL
        private Image AddInsetFill(GameObject go, int radius, Color fillColor, float inset = 1.5f)
        {
            var fillGO = new GameObject("Fill");
            fillGO.transform.SetParent(go.transform, false);
            fillGO.transform.SetAsFirstSibling();
            var rt = fillGO.AddComponent<RectTransform>();
            rt.anchorMin = Vector2.zero;
            rt.anchorMax = Vector2.one;
            rt.offsetMin = new Vector2(inset, inset);
            rt.offsetMax = new Vector2(-inset, -inset);
            var img = fillGO.AddComponent<Image>();
            img.sprite = GetRoundedSprite(radius);
            img.type = Image.Type.Sliced;
            img.color = fillColor;
            img.raycastTarget = false;
            fillGO.AddComponent<LayoutElement>().ignoreLayout = true;
            return img;
        }
        #endregion [END] ADD INSET FILL

        // ---------------- rounded-rect sprite generator (9-sliced, cached by radius) ----------------
        private static readonly Dictionary<int, Sprite> _roundedSpriteCache = new Dictionary<int, Sprite>();

        #region [START] GET ROUNDED SPRITE
        private static Sprite GetRoundedSprite(int radius)
        {
            radius = Mathf.Max(2, radius);
            if (_roundedSpriteCache.TryGetValue(radius, out var cached) && cached != null) return cached;

            int size = radius * 2 + 4;
            var tex = new Texture2D(size, size, TextureFormat.ARGB32, false);
            tex.hideFlags = HideFlags.HideAndDontSave;
            tex.wrapMode = TextureWrapMode.Clamp;
            tex.filterMode = FilterMode.Bilinear;

            for (int y = 0; y < size; y++)
            {
                for (int x = 0; x < size; x++)
                {
                    bool inCornerX = x < radius || x >= size - radius;
                    bool inCornerY = y < radius || y >= size - radius;
                    float alpha = 1f;
                    if (inCornerX && inCornerY)
                    {
                        float cx = x < radius ? radius : size - radius - 1;
                        float cy = y < radius ? radius : size - radius - 1;
                        float dist = Mathf.Sqrt((x - cx) * (x - cx) + (y - cy) * (y - cy));
                        alpha = Mathf.Clamp01(radius - dist + 0.5f);
                    }
                    tex.SetPixel(x, y, new Color(1f, 1f, 1f, alpha));
                }
            }
            tex.Apply();

            var sprite = Sprite.Create(tex, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f), 100f, 0, SpriteMeshType.FullRect, new Vector4(radius, radius, radius, radius));
            sprite.name = "FC_Rounded_" + radius;
            _roundedSpriteCache[radius] = sprite;
            return sprite;
        }
        #endregion [END] GET ROUNDED SPRITE

        #region [START] CREATE TEXT
        private Text CreateText(GameObject parent, string text, float fontSize, FontStyle style, Color color, TextAnchor alignment)
        {
            var go = new GameObject("Text");
            go.transform.SetParent(parent.transform, false);
            var t = go.AddComponent<Text>();
            t.text = text;
            t.font = GetGameFont();
            t.fontSize = Mathf.RoundToInt(fontSize);
            t.fontStyle = style;
            t.color = color;
            t.alignment = alignment;
            t.raycastTarget = false;
            t.supportRichText = true;
            t.horizontalOverflow = HorizontalWrapMode.Wrap;
            return t;
        }
        #endregion [END] CREATE TEXT

        #region [START] FILL PARENT
        private void FillParent(GameObject go)
        {
            var rt = go.GetComponent<RectTransform>() ?? go.AddComponent<RectTransform>();
            rt.anchorMin = Vector2.zero;
            rt.anchorMax = Vector2.one;
            rt.offsetMin = Vector2.zero;
            rt.offsetMax = Vector2.zero;
        }
        #endregion [END] FILL PARENT

        #region [START] SET LAYER RECURSIVELY
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
        #endregion [END] SET LAYER RECURSIVELY
        #endregion [END] CANVAS CONSTRUCTION
    }
    // ============================================================================
    // [END] CANVAS FARMER'S COMPANION SETTINGS UI
    // ============================================================================
    #endregion
}
