using FarmersCompanion.Features;
using UnityEngine;

namespace FarmersCompanion.UI
{
    #region [START] FEATURE 30: IN-GAME FARMING SETTINGS MENU CANVAS
    // ============================================================================
    // [START] FEATURE 30: IN-GAME FARMING SETTINGS MENU CANVAS
    // Description: Rustic in-game settings canvas (F1 hotkey default).
    //              Controls all 15 agriculture & livestock automation features.
    // ============================================================================
    public class FarmingMenuUI : MonoBehaviour
    {
        public static FarmingMenuUI Instance { get; private set; }

        public bool IsVisible { get; set; } = false;

        private Rect _windowRect = new Rect(80, 80, 520, 620);
        private Vector2 _scrollPos = Vector2.zero;

        private GUIStyle _headerStyle;
        private GUIStyle _subHeaderStyle;
        private GUIStyle _buttonStyle;
        private GUIStyle _toggleStyle;
        private GUIStyle _boxStyle;

        #region [START] UNITY LIFECYCLE
        private void Awake()
        {
            Instance = this;
        }

        private void Update()
        {
            if (Input.GetKeyDown(Plugin.KeyMenu.Value))
            {
                ToggleMenu();
            }
        }
        #endregion [END] UNITY LIFECYCLE

        #region [START] TOGGLE MENU
        public void ToggleMenu()
        {
            IsVisible = !IsVisible;
            if (IsVisible)
            {
                Cursor.lockState = CursorLockMode.None;
                Cursor.visible = true;
            }
            else
            {
                Cursor.lockState = CursorLockMode.Locked;
                Cursor.visible = false;
            }
        }
        #endregion [END] TOGGLE MENU

        #region [START] ONGUI RENDER
        private void OnGUI()
        {
            if (!IsVisible) return;

            InitStyles();
            _windowRect = GUI.Window(948201, _windowRect, DrawWindow, "🌾 FARMER'S COMPANION — AGRICULTURE SUITE", _boxStyle);
        }

        private void DrawWindow(int windowID)
        {
            GUILayout.BeginVertical();

            _scrollPos = GUILayout.BeginScrollView(_scrollPos, GUILayout.Width(500), GUILayout.Height(530));

            // Section 1: Auto-Watering & Smart Water
            GUILayout.Label("💧 WATERING & SMART USAGE", _headerStyle);
            if (CropWaterManager.Instance != null)
            {
                CropWaterManager.Instance.EnableAutoWater = GUILayout.Toggle(CropWaterManager.Instance.EnableAutoWater, " Enable Auto Water Crops (Feature 16)", _toggleStyle);
                CropWaterManager.Instance.EnableGrassWatering = GUILayout.Toggle(CropWaterManager.Instance.EnableGrassWatering, " Enable Grass Watering for Livestock (Feature 21)", _toggleStyle);
                CropWaterManager.Instance.SmartWaterUsage = GUILayout.Toggle(CropWaterManager.Instance.SmartWaterUsage, " Smart Water: Only water genuinely dry plots (Feature 26)", _toggleStyle);

                GUILayout.Space(6);
                GUILayout.Label($"Watering Radius: {CropWaterManager.Instance.WaterRadius:F0}m");
                CropWaterManager.Instance.WaterRadius = GUILayout.HorizontalSlider(CropWaterManager.Instance.WaterRadius, 10f, 60f);
            }

            GUILayout.Space(12);

            // Section 2: Harvesting & Replanting
            GUILayout.Label("🌾 HARVESTING & SEED CONSERVATION", _headerStyle);
            if (CropHarvestManager.Instance != null)
            {
                CropHarvestManager.Instance.EnableAutoHarvest = GUILayout.Toggle(CropHarvestManager.Instance.EnableAutoHarvest, " Enable Auto Harvest Ripe Crops (Feature 17)", _toggleStyle);
                CropHarvestManager.Instance.EnableAutoReplant = GUILayout.Toggle(CropHarvestManager.Instance.EnableAutoReplant, " Enable Auto Replant Seeds from Inventory (Feature 18)", _toggleStyle);
                CropHarvestManager.Instance.EnableSeedSaver = GUILayout.Toggle(CropHarvestManager.Instance.EnableSeedSaver, " Enable Seed Saver Mode (Feature 28)", _toggleStyle);

                if (CropHarvestManager.Instance.EnableSeedSaver)
                {
                    GUILayout.Label($"Seed Refund Chance: {CropHarvestManager.Instance.SeedSaverChancePercent:F0}%");
                    CropHarvestManager.Instance.SeedSaverChancePercent = GUILayout.HorizontalSlider(CropHarvestManager.Instance.SeedSaverChancePercent, 5f, 50f);
                }

                GUILayout.Space(6);
                if (GUILayout.Button("⚡ Multi-Harvest Sweep: Harvest All Ripe Crops Now! (Feature 27)", _buttonStyle))
                {
                    int count = CropHarvestManager.Instance.HarvestNearbyCrops(CropHarvestManager.Instance.HarvestRadius, CropHarvestManager.Instance.EnableAutoReplant);
                    if (CropIndicatorManager.Instance != null)
                    {
                        CropIndicatorManager.Instance.ShowNotification($"🌾 Multi-Harvest: Collected {count} ripe crops!");
                    }
                }
            }

            GUILayout.Space(12);

            // Section 3: Growth Speed & Forestry
            GUILayout.Label("🌱 GROWTH RATE & FORESTRY ACCELERATION", _headerStyle);
            if (CropGrowthManager.Instance != null)
            {
                CropGrowthManager.Instance.EnableGrowthBoost = GUILayout.Toggle(CropGrowthManager.Instance.EnableGrowthBoost, " Enable Accelerated Crop Growth (Feature 19)", _toggleStyle);
                if (CropGrowthManager.Instance.EnableGrowthBoost)
                {
                    GUILayout.Label($"Crop Growth Multiplier: {CropGrowthManager.Instance.CropGrowthMultiplier:F1}x");
                    CropGrowthManager.Instance.CropGrowthMultiplier = GUILayout.HorizontalSlider(CropGrowthManager.Instance.CropGrowthMultiplier, 1.0f, 3.0f);
                }

                CropGrowthManager.Instance.EnableTreeGrowthBoost = GUILayout.Toggle(CropGrowthManager.Instance.EnableTreeGrowthBoost, " Enable Palm & Fruit Tree Growth Boost (Feature 20)", _toggleStyle);
                if (CropGrowthManager.Instance.EnableTreeGrowthBoost)
                {
                    GUILayout.Label($"Tree Growth Multiplier: {CropGrowthManager.Instance.TreeGrowthMultiplier:F1}x");
                    CropGrowthManager.Instance.TreeGrowthMultiplier = GUILayout.HorizontalSlider(CropGrowthManager.Instance.TreeGrowthMultiplier, 1.0f, 3.0f);
                }

                CropGrowthManager.Instance.EnableFertilizerBoost = GUILayout.Toggle(CropGrowthManager.Instance.EnableFertilizerBoost, " Fertilizer Surge: Extra 1.5x Growth Multiplier (Feature 25)", _toggleStyle);
            }

            GUILayout.Space(12);

            // Section 4: Livestock & Indicators
            GUILayout.Label("🐑 LIVESTOCK & HEALTH VISIBILITY", _headerStyle);
            if (LivestockManager.Instance != null)
            {
                LivestockManager.Instance.EnableAutoCollectLivestock = GUILayout.Toggle(LivestockManager.Instance.EnableAutoCollectLivestock, " Auto-Shear Llamas & Milk Goats when Ready (Feature 22)", _toggleStyle);
            }

            if (CropIndicatorManager.Instance != null)
            {
                CropIndicatorManager.Instance.EnableHealthIndicators = GUILayout.Toggle(CropIndicatorManager.Instance.EnableHealthIndicators, " Show 3D Floating Crop Health/Water Indicators (Feature 23)", _toggleStyle);
                CropIndicatorManager.Instance.EnableNotifications = GUILayout.Toggle(CropIndicatorManager.Instance.EnableNotifications, " Show Toast Notifications on Ready Harvest (Feature 29)", _toggleStyle);
            }

            GUILayout.EndScrollView();

            GUILayout.Space(8);
            if (GUILayout.Button("Close Menu [F1]", _buttonStyle))
            {
                ToggleMenu();
            }

            GUILayout.EndVertical();
            GUI.DragWindow();
        }
        #endregion [END] ONGUI RENDER

        #region [START] STYLES INITIALIZATION
        private void InitStyles()
        {
            if (_headerStyle == null)
            {
                _headerStyle = new GUIStyle(GUI.skin.label)
                {
                    fontSize = 13,
                    fontStyle = FontStyle.Bold,
                    normal = { textColor = new Color(0.95f, 0.8f, 0.4f) }
                };
            }

            if (_subHeaderStyle == null)
            {
                _subHeaderStyle = new GUIStyle(GUI.skin.label)
                {
                    fontSize = 11,
                    fontStyle = FontStyle.Italic,
                    normal = { textColor = new Color(0.8f, 0.8f, 0.8f) }
                };
            }

            if (_buttonStyle == null)
            {
                _buttonStyle = new GUIStyle(GUI.skin.button)
                {
                    fontSize = 12,
                    fontStyle = FontStyle.Bold,
                    fixedHeight = 32
                };
            }

            if (_toggleStyle == null)
            {
                _toggleStyle = new GUIStyle(GUI.skin.toggle)
                {
                    fontSize = 12,
                    fontStyle = FontStyle.Normal
                };
            }

            if (_boxStyle == null)
            {
                _boxStyle = new GUIStyle(GUI.skin.window)
                {
                    fontSize = 13,
                    fontStyle = FontStyle.Bold
                };
            }
        }
        #endregion [END] STYLES INITIALIZATION
    }
    // ============================================================================
    // [END] FEATURE 30: IN-GAME FARMING SETTINGS MENU CANVAS
    // ============================================================================
    #endregion
}
