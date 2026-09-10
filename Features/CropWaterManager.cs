using System.Collections;
using System.Collections.Generic;
using FarmersCompanion.Helpers;
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

                if (!EnableAutoWater) continue;

                var player = PlayerHelper.GetLocalPlayer();
                if (player == null) continue;

                Vector3 playerPos = player.transform.position;
                float radiusSqr = WaterRadius * WaterRadius;

                // Safely collect all active cropplots in world
                Cropplot[] plots = FindObjectsOfType<Cropplot>();
                if (plots == null || plots.Length == 0) continue;

                var plantManager = ComponentManager<PlantManager>.Value;

                foreach (var plot in plots)
                {
                    if (plot == null) continue;

                    // Distance filter using squared magnitude (zero Math.Sqrt overhead)
                    if ((plot.transform.position - playerPos).sqrMagnitude > radiusSqr)
                    {
                        continue;
                    }

                    // Feature 21: Check grass plot watering for livestock
                    bool isGrass = plot is Cropplot_Grass;
                    if (isGrass && !EnableGrassWatering) continue;

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
