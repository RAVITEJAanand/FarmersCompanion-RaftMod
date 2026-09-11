using System.Collections;
using System.Collections.Generic;
using FarmersCompanion.Helpers;
using Steamworks;
using UnityEngine;

namespace FarmersCompanion.Features
{
    #region [START] FEATURE 16, 21, 26: CROP & GRASS AUTO-WATER MANAGER
    // ============================================================================
    // [START] FEATURE 16, 21, 26: CROP & GRASS AUTO-WATER MANAGER
    // Description: Lightweight, non-intrusive auto-watering for crops and grass plots.
    //              Features 16 (Auto Water Crops), 21 (Animal Grass Feeding), 26 (Smart Water Usage).
    //              Guarantees zero thermal/CPU load using low-frequency coroutines.
    // ============================================================================
    public class CropWaterManager : MonoBehaviour
    {
        public static CropWaterManager Instance { get; private set; }

        #region [START] CONFIGURATION PROPERTIES
        public bool EnableAutoWater { get; set; } = true;
        public bool EnableGrassWatering { get; set; } = true;
        public bool SmartWaterUsage { get; set; } = true;
        public float WaterRadius { get; set; } = 30f;
        public float WaterIntervalSeconds { get; set; } = 3.0f;
        #endregion [END] CONFIGURATION PROPERTIES

        private Coroutine _waterRoutine;
        private readonly List<Cropplot> _cachedPlots = new List<Cropplot>();
        private float _lastPlotsScanTime = -30f;
        private const float PLOTS_SCAN_INTERVAL = 12f;

        #region [START] UNITY LIFECYCLE
        private void Awake()
        {
            Instance = this;
        }

        private void OnEnable()
        {
            if (_waterRoutine != null)
            {
                StopCoroutine(_waterRoutine);
            }
            _waterRoutine = StartCoroutine(WateringLoop());
        }

        private void OnDisable()
        {
            if (_waterRoutine != null)
            {
                StopCoroutine(_waterRoutine);
                _waterRoutine = null;
            }
        }
        #endregion [END] UNITY LIFECYCLE

        #region [START] LOW-FREQUENCY WATERING COROUTINE
        /// <summary>
        /// Periodic background coroutine (runs once every few seconds, zero FPS/thermal impact).
        /// </summary>
        private IEnumerator WateringLoop()
        {
            while (true)
            {
                yield return new WaitForSeconds(WaterIntervalSeconds);

                if (!EnableAutoWater && !EnableGrassWatering) continue;

                var player = PlayerHelper.GetLocalPlayer();
                if (player == null) continue;

                if (!PlayerHelper.IsHost()) continue;

                Vector3 playerPos = player.transform.position;
                float radiusSqr = WaterRadius * WaterRadius;

                // Refresh cached cropplots periodically instead of scanning scene every tick
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

                if (_cachedPlots.Count == 0) continue;

                var plantManager = ComponentManager<PlantManager>.Value;

                foreach (var plot in _cachedPlots)
                {
                    if (plot == null) continue;

                    // Distance filter using squared magnitude (zero Math.Sqrt overhead)
                    if ((plot.transform.position - playerPos).sqrMagnitude > radiusSqr)
                    {
                        continue;
                    }

                    // Feature 21: Check grass plot watering for livestock (independent of EnableAutoWater)
                    bool isGrass = plot is Cropplot_Grass;
                    if (isGrass) { if (!EnableGrassWatering) continue; }
                    else if (!EnableAutoWater) continue;

                    // Feature 26: Smart Water Usage — only water when slots genuinely need water
                    if (SmartWaterUsage && !plot.SlotsNeedWater())
                    {
                        continue;
                    }

                    // Water the plot cleanly via PlantManager or local plot method
                    try
                    {
                        if (plantManager != null)
                        {
                            plantManager.WaterCropplot(plot, false);
                        }
                        else
                        {
                            plot.AddWater(false);
                        }
                    }
                    catch
                    {
                        // Safe fallback to direct method
                        plot.AddWater(false);
                    }

                    // WaterCropplot/AddWater only mutate local state - vanilla's own callers
                    // (Cropplot's interact handler, PlantManager.WaterAllPlantsWithRain) always
                    // pair this with a Message_WaterCrop RPC broadcast, otherwise clients never
                    // see the plot become watered until an unrelated resync happens.
                    if (plantManager != null)
                    {
                        try
                        {
                            var msg = new Message_WaterCrop(Messages.PlantManager_WaterPlant, plantManager, plot, false);
                            player.Network.RPC(msg, Target.Other, EP2PSend.k_EP2PSendReliable, NetworkChannel.Channel_Game);
                        }
                        catch { }
                    }
                }
            }
        }
        #endregion [END] LOW-FREQUENCY WATERING COROUTINE
    }
    // ============================================================================
    // [END] FEATURE 16, 21, 26: CROP & GRASS AUTO-WATER MANAGER
    // ============================================================================
    #endregion
}
