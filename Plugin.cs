using System;
using BepInEx;
using BepInEx.Configuration;
using FarmersCompanion.Features;
using FarmersCompanion.UI;
using UnityEngine;

namespace FarmersCompanion
{
    #region [START] MAIN PLUGIN ENTRY: FARMER'S COMPANION
    // ============================================================================
    // [START] MAIN PLUGIN ENTRY: FARMER'S COMPANION
    // Description: Complete Agriculture & Livestock Automation Suite for Raft (Features 16–30).
    //              100% independent, zero interference with Sailor's Companion or Inventory Master.
    // ============================================================================
    [BepInPlugin(PluginInfo.PLUGIN_GUID, PluginInfo.PLUGIN_NAME, PluginInfo.PLUGIN_VERSION)]
    [BepInProcess("Raft.exe")]
    public class Plugin : BaseUnityPlugin
    {
        public static Plugin Instance { get; private set; }
        public static GameObject ManagerGO { get; private set; }

        #region [START] CONFIGURATION DEFINITIONS
        // Hotkeys (F1 default - zero clash with Sailor's Companion F3-F11 or Inventory Master F2)
        public static ConfigEntry<KeyCode> KeyMenu;

        // Features 16, 21, 26: Water
        public static ConfigEntry<bool> EnableAutoWater;
        public static ConfigEntry<bool> EnableGrassWatering;
        public static ConfigEntry<bool> SmartWaterUsage;
        public static ConfigEntry<float> WaterRadius;

        // Features 17, 18, 27, 28: Harvest & Seeds
        public static ConfigEntry<bool> EnableAutoHarvest;
        public static ConfigEntry<bool> EnableAutoReplant;
        public static ConfigEntry<bool> EnableSeedSaver;
        public static ConfigEntry<float> SeedSaverChancePercent;

        // Features 19, 20, 25: Growth
        public static ConfigEntry<bool> EnableGrowthBoost;
        public static ConfigEntry<float> CropGrowthMultiplier;
        public static ConfigEntry<bool> EnableTreeGrowthBoost;
        public static ConfigEntry<float> TreeGrowthMultiplier;
        public static ConfigEntry<bool> EnableFertilizerBoost;

        // Features 21, 22: Livestock
        public static ConfigEntry<bool> EnableAutoCollectLivestock;
        public static ConfigEntry<float> LivestockRadius;

        // Features 23, 29: Visibility & Alerts
        public static ConfigEntry<bool> EnableHealthIndicators;
        public static ConfigEntry<bool> EnableNotifications;
        #endregion [END] CONFIGURATION DEFINITIONS

        #region [START] AWAKE
        private void Awake()
        {
            Instance = this;

            // Bind Hotkeys (F1 for Farmer's Companion Menu)
            KeyMenu = Config.Bind("General.Hotkeys", "KeyMenu", KeyCode.F1, "Hotkey to toggle the in-game Farmer's Companion menu (F1).");

            // Water
            EnableAutoWater = Config.Bind("Features.Watering", "EnableAutoWater", true, "Automatically water dry crop plots.");
            EnableGrassWatering = Config.Bind("Features.Watering", "EnableGrassWatering", true, "Automatically water grass plots so animals can eat.");
            SmartWaterUsage = Config.Bind("Features.Watering", "SmartWaterUsage", true, "Only water plots when they genuinely require water.");
            WaterRadius = Config.Bind("Features.Watering", "WaterRadius", 30f, "Radius around player/raft for automated watering.");

            // Harvest & Seeds
            EnableAutoHarvest = Config.Bind("Features.Harvesting", "EnableAutoHarvest", true, "Automatically harvest fully grown crops.");
            EnableAutoReplant = Config.Bind("Features.Harvesting", "EnableAutoReplant", true, "Automatically replant seeds after harvesting.");
            EnableSeedSaver = Config.Bind("Features.Harvesting", "EnableSeedSaver", true, "Chance to preserve/refund seed upon replanting.");
            SeedSaverChancePercent = Config.Bind("Features.Harvesting", "SeedSaverChancePercent", 25f, "Percentage chance (0-100) to save seed.");

            // Growth
            EnableGrowthBoost = Config.Bind("Features.Growth", "EnableGrowthBoost", true, "Enable balanced growth speed multiplier.");
            CropGrowthMultiplier = Config.Bind("Features.Growth", "CropGrowthMultiplier", 1.3f, "Speed multiplier for crops (1.0 = normal, 1.3 = +30% faster).");
            EnableTreeGrowthBoost = Config.Bind("Features.Growth", "EnableTreeGrowthBoost", true, "Enable growth boost for palm and fruit trees.");
            TreeGrowthMultiplier = Config.Bind("Features.Growth", "TreeGrowthMultiplier", 1.5f, "Speed multiplier for trees.");
            EnableFertilizerBoost = Config.Bind("Features.Growth", "EnableFertilizerBoost", false, "Enable extra fertilizer surge multiplier.");

            // Livestock
            EnableAutoCollectLivestock = Config.Bind("Features.Livestock", "EnableAutoCollectLivestock", true, "Automatically collect milk, wool, and animal resources when ready.");
            LivestockRadius = Config.Bind("Features.Livestock", "LivestockRadius", 30f, "Radius for livestock resource collection.");

            // UI & Alerts
            EnableHealthIndicators = Config.Bind("Features.UI", "EnableHealthIndicators", true, "Show floating 3D indicators for water/growth.");
            EnableNotifications = Config.Bind("Features.UI", "EnableNotifications", true, "Show on-screen toast alerts when harvests are ready.");

            // Spawn persistent manager
            EnsureManagerGameObject();

            Logger.LogInfo($"{PluginInfo.PLUGIN_NAME} v{PluginInfo.PLUGIN_VERSION} initialized successfully! (F1 for Menu)");
        }
        #endregion [END] AWAKE

        #region [START] ENSURE MANAGER GAMEOBJECT
        private void EnsureManagerGameObject()
        {
            try
            {
                if (ManagerGO == null)
                {
                    ManagerGO = new GameObject("FarmersCompanion_Manager");
                    DontDestroyOnLoad(ManagerGO);

                    // Attach modular managers
                    var waterMgr = ManagerGO.AddComponent<CropWaterManager>();
                    waterMgr.EnableAutoWater = EnableAutoWater.Value;
                    waterMgr.EnableGrassWatering = EnableGrassWatering.Value;
                    waterMgr.SmartWaterUsage = SmartWaterUsage.Value;
                    waterMgr.WaterRadius = WaterRadius.Value;

                    var harvestMgr = ManagerGO.AddComponent<CropHarvestManager>();
                    harvestMgr.EnableAutoHarvest = EnableAutoHarvest.Value;
                    harvestMgr.EnableAutoReplant = EnableAutoReplant.Value;
                    harvestMgr.EnableSeedSaver = EnableSeedSaver.Value;
                    harvestMgr.SeedSaverChancePercent = SeedSaverChancePercent.Value;
                    harvestMgr.HarvestRadius = WaterRadius.Value;

                    var growthMgr = ManagerGO.AddComponent<CropGrowthManager>();
                    growthMgr.EnableGrowthBoost = EnableGrowthBoost.Value;
                    growthMgr.CropGrowthMultiplier = CropGrowthMultiplier.Value;
                    growthMgr.EnableTreeGrowthBoost = EnableTreeGrowthBoost.Value;
                    growthMgr.TreeGrowthMultiplier = TreeGrowthMultiplier.Value;
                    growthMgr.EnableFertilizerBoost = EnableFertilizerBoost.Value;

                    var livestockMgr = ManagerGO.AddComponent<LivestockManager>();
                    livestockMgr.EnableAutoCollectLivestock = EnableAutoCollectLivestock.Value;
                    livestockMgr.LivestockRadius = LivestockRadius.Value;

                    var indicatorMgr = ManagerGO.AddComponent<CropIndicatorManager>();
                    indicatorMgr.EnableHealthIndicators = EnableHealthIndicators.Value;
                    indicatorMgr.EnableNotifications = EnableNotifications.Value;

                    ManagerGO.AddComponent<CanvasFarmersCompanionUI>();
                }
            }
            catch (Exception ex)
            {
                Logger.LogError($"Failed to initialize FarmersCompanion manager: {ex}");
            }
        }
        #endregion [END] ENSURE MANAGER GAMEOBJECT

        #region [START] ONDESTROY
        private void OnDestroy()
        {
            if (ManagerGO != null)
            {
                Destroy(ManagerGO);
            }
        }
        #endregion [END] ONDESTROY
    }
    // ============================================================================
    // [END] MAIN PLUGIN ENTRY: FARMER'S COMPANION
    // ============================================================================
    #endregion
}
