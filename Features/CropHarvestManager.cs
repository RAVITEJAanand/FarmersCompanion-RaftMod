using System.Collections;
using System.Collections.Generic;
using FarmersCompanion.Helpers;
using Steamworks;
using UnityEngine;

namespace FarmersCompanion.Features
{
    #region [START] FEATURE 17, 18, 27, 28: CROP HARVEST & REPLANT MANAGER
    // ============================================================================
    // [START] FEATURE 17, 18, 27, 28: CROP HARVEST & REPLANT MANAGER
    // Description: Automatic harvesting of ripe crops, auto-replanting, multi-harvest,
    //              and seed conservation.
    // ============================================================================
    public class CropHarvestManager : MonoBehaviour
    {
        public static CropHarvestManager Instance { get; private set; }

        #region [START] CONFIGURATION PROPERTIES
        public bool EnableAutoHarvest { get; set; } = true;
        public bool EnableAutoReplant { get; set; } = true;
        public bool EnableSeedSaver { get; set; } = true;
        public float SeedSaverChancePercent { get; set; } = 25f; // 25% chance to save seed
        public float HarvestRadius { get; set; } = 30f;
        public float HarvestIntervalSeconds { get; set; } = 3.0f;
        #endregion [END] CONFIGURATION PROPERTIES

        // Live counters surfaced on the settings menu's Overview tab.
        public static int CropsHarvestedThisSession = 0;
        public static int SeedsReplantedThisSession = 0;

        private Coroutine _harvestRoutine;

        #region [START] UNITY LIFECYCLE
        private void Awake()
        {
            Instance = this;
        }

        private void OnEnable()
        {
            if (_harvestRoutine != null)
            {
                StopCoroutine(_harvestRoutine);
            }
            _harvestRoutine = StartCoroutine(HarvestLoop());
        }

        private void OnDisable()
        {
            if (_harvestRoutine != null)
            {
                StopCoroutine(_harvestRoutine);
                _harvestRoutine = null;
            }
        }
        #endregion [END] UNITY LIFECYCLE

        #region [START] LOW-FREQUENCY HARVEST COROUTINE
        private IEnumerator HarvestLoop()
        {
            while (true)
            {
                yield return new WaitForSeconds(HarvestIntervalSeconds);

                if (!EnableAutoHarvest) continue;

                int harvested = HarvestNearbyCrops(HarvestRadius, EnableAutoReplant);
                if (harvested > 0 && CropIndicatorManager.Instance != null)
                {
                    CropIndicatorManager.Instance.ShowNotification($"🌾 Auto-Harvested {harvested} ripe crop(s)!");
                }
            }
        }
        #endregion [END] LOW-FREQUENCY HARVEST COROUTINE

        private readonly List<Cropplot> _cachedPlots = new List<Cropplot>();
        private float _lastPlotsScanTime = -30f;
        private const float PLOTS_SCAN_INTERVAL = 12f;

        #region [START] FEATURE 27: MULTI HARVEST SWEEP
        /// <summary>
        /// Instantly harvests all ripe crops within radius in one action.
        /// </summary>
        public int HarvestNearbyCrops(float radius, bool replantAfterHarvest)
        {
            var player = PlayerHelper.GetLocalPlayer();
            if (player == null) return 0;

            if (!PlayerHelper.IsHost()) return 0;

            Vector3 playerPos = player.transform.position;
            float radiusSqr = radius * radius;

            // Refresh cached cropplots periodically
            if (Time.unscaledTime - _lastPlotsScanTime > PLOTS_SCAN_INTERVAL || _cachedPlots.Count == 0)
            {
                _lastPlotsScanTime = Time.unscaledTime;
                _cachedPlots.Clear();
                var found = FindObjectsOfType<Cropplot>();
                if (found != null && found.Length > 0)
                {
                    _cachedPlots.AddRange(found);
                }
            }
            else
            {
                _cachedPlots.RemoveAll(p => p == null);
            }

            if (_cachedPlots.Count == 0) return 0;

            var plantManager = ComponentManager<PlantManager>.Value;
            int harvestedCount = 0;

            foreach (var plot in _cachedPlots)
            {
                if (plot == null) continue;
                if ((plot.transform.position - playerPos).sqrMagnitude > radiusSqr) continue;

                var slots = plot.GetSlots();
                if (slots == null) continue;

                for (int s = 0; s < slots.Count; s++)
                {
                    var slot = slots[s];
                    var plant = slot?.plant;
                    // Cropplot.PlantRemoved() clears slot.busy after a harvest but never
                    // clears PlantationSlot.plant or resets Plant.fullyGrown, so a plant
                    // that was already harvested (and not auto-replenished) keeps reporting
                    // FullyGrown()==true forever - re-checking slot.busy here stops us from
                    // re-"harvesting" the same dead plant every cycle indefinitely.
                    if (slot == null || !slot.busy || plant == null || !plant.FullyGrown()) continue;

                    try
                    {
                        if (plantManager != null)
                        {
                            // PlantManager.Harvest() only mutates local state - vanilla's own
                            // caller (PlantManager.HarvestPlants) always broadcasts a
                            // Message_HarvestPlant RPC first (built from the plant's still-intact
                            // pickupComponent, before Harvest() clears/destroys it), otherwise the
                            // harvested plant stays visibly present and interactable for clients.
                            try
                            {
                                if (plant.pickupComponent != null && plant.pickupComponent.networkID != null)
                                {
                                    var harvestMsg = new Message_HarvestPlant(Messages.PlantManager_HarvestPlant, plantManager, plant, true);
                                    player.Network.RPC(harvestMsg, Target.Other, EP2PSend.k_EP2PSendReliable, NetworkChannel.Channel_Game);
                                }
                            }
                            catch { }

                            plantManager.Harvest(plant, true);
                        }
                        else if (plant.pickupComponent != null && plant.pickupComponent.yieldHandler != null)
                        {
                            plant.pickupComponent.yieldHandler.CollectYield(player);
                            plant.PullRoots();
                        }
                        else
                        {
                            plant.PullRoots();
                        }

                        harvestedCount++;
                        CropsHarvestedThisSession++;

                        if (replantAfterHarvest)
                        {
                            TryAutoReplant(plot, player);
                        }
                    }
                    catch
                    {
                    }
                }
            }

            return harvestedCount;
        }
        #endregion [END] FEATURE 27: MULTI HARVEST SWEEP

        #region [START] FEATURE 18 & 28: AUTO REPLANT & SEED SAVER
        /// <summary>
        /// Checks for empty slots in cropplot and replants available seeds from player inventory.
        /// </summary>
        private void TryAutoReplant(Cropplot plot, Network_Player player)
        {
            if (plot == null || player == null || player.Inventory == null) return;

            var slots = plot.GetSlots();
            if (slots == null || slots.Count == 0) return;

            PlantationSlot emptySlot = null;
            int emptySlotIndex = -1;
            for (int i = 0; i < slots.Count; i++)
            {
                if (slots[i] != null && (!slots[i].busy || slots[i].plant == null))
                {
                    emptySlot = slots[i];
                    emptySlotIndex = i;
                    break;
                }
            }

            if (emptySlot == null) return;

            // Search player inventory for any acceptable seed/plant item
            var allSlots = player.Inventory.allSlots;
            if (allSlots == null) return;

            foreach (var slot in allSlots)
            {
                if (slot == null || slot.IsEmpty) continue;
                if (slot.locked) continue; // Respect Inventory Master favorite item locks

                Item_Base item = slot.GetItemBase();
                if (item == null || !plot.AcceptsPlantType(item)) continue;

                // Feature 28: Seed Saver Mode — roll chance to preserve seed
                bool saveSeed = EnableSeedSaver && (Random.Range(0f, 100f) <= SeedSaverChancePercent);

                if (!saveSeed)
                {
                    player.Inventory.RemoveItem(item.UniqueName, 1);
                }

                // Plant seed using Cropplot
                try
                {
                    // Resolve the seed's Plant prefab via PlantManager's own registry (the same
                    // lookup the game uses for PlantComponent's held-seed interaction) instead of
                    // digging through settings_buildable's block prefabs, which isn't how seed
                    // items map to their Plant prefab and silently failed for some plant types
                    // (e.g. trees) even though it happened to work for common ground crops.
                    var pm = ComponentManager<PlantManager>.Value;
                    var plantPrefab = pm != null ? pm.GetPlantByIndex(item.UniqueIndex) : null;

                    if (plantPrefab != null)
                    {
                        // Every planted object needs a unique network object index (matches the game's own
                        // Cropplot.RefillSlot pattern); a hardcoded 0 would collide with every other
                        // auto-replanted plant's ID and break host/client plant lookups in multiplayer.
                        uint newPlantObjectIndex = SaveAndLoad.GetUniqueObjectIndex();
                        plot.PlantSeed(plantPrefab, newPlantObjectIndex, true, emptySlotIndex >= 0 ? emptySlotIndex : 0);
                        SeedsReplantedThisSession++;

                        // Cropplot.PlantSeed() only mutates local state - vanilla's own caller
                        // (Cropplot.RefillSlot) always pairs it with a Message_PlantSeed RPC using
                        // this exact same object index, otherwise clients never see the replanted
                        // seedling appear.
                        if (pm != null)
                        {
                            try
                            {
                                var plantMsg = new Message_PlantSeed(Messages.PlantManager_PlantSeed, pm, plot, plantPrefab, true);
                                plantMsg.plantObjectIndex = newPlantObjectIndex;
                                player.Network.RPC(plantMsg, Target.Other, EP2PSend.k_EP2PSendReliable, NetworkChannel.Channel_Game);
                            }
                            catch { }
                        }
                    }
                }
                catch
                {
                }
                break;
            }
        }
        #endregion [END] FEATURE 18 & 28: AUTO REPLANT & SEED SAVER
    }
    // ============================================================================
    // [END] FEATURE 17, 18, 27, 28: CROP HARVEST & REPLANT MANAGER
    // ============================================================================
    #endregion
}
