using System.Collections;
using FarmersCompanion.Helpers;
using UnityEngine;

namespace FarmersCompanion.Features
{
    #region [START] FEATURE 19, 20, 25: CROP & TREE GROWTH MANAGER
    // ============================================================================
    // [START] FEATURE 19, 20, 25: CROP & TREE GROWTH MANAGER
    // Description: Balanced growth speed acceleration for crops and trees with optional fertilizer boost.
    //              Features 19 (Faster Crop Growth), 20 (Tree Growth Boost), 25 (Fertilizer Boost).
    // ============================================================================
    public class CropGrowthManager : MonoBehaviour
    {
        public static CropGrowthManager Instance { get; private set; }

        #region [START] CONFIGURATION PROPERTIES
        public bool EnableGrowthBoost { get; set; } = true;
        public float CropGrowthMultiplier { get; set; } = 1.3f; // 1.3x balanced speed
        public bool EnableTreeGrowthBoost { get; set; } = true;
        public float TreeGrowthMultiplier { get; set; } = 1.5f; // 1.5x tree speed
        public bool EnableFertilizerBoost { get; set; } = false;
        public float FertilizerExtraMultiplier { get; set; } = 1.5f;
        public float GrowthCheckIntervalSeconds { get; set; } = 2.0f;
        public float GrowthRadius { get; set; } = 40f;
        #endregion [END] CONFIGURATION PROPERTIES

        private Coroutine _growthRoutine;

        #region [START] UNITY LIFECYCLE
        private void Awake()
        {
            Instance = this;
        }

        private void OnEnable()
        {
            if (_growthRoutine != null)
            {
                StopCoroutine(_growthRoutine);
            }
            _growthRoutine = StartCoroutine(GrowthBoostLoop());
        }

        private void OnDisable()
        {
            if (_growthRoutine != null)
            {
                StopCoroutine(_growthRoutine);
                _growthRoutine = null;
            }
        }
        #endregion [END] UNITY LIFECYCLE

        private readonly System.Collections.Generic.List<Cropplot> _cachedPlots = new System.Collections.Generic.List<Cropplot>();
        private float _lastPlotsScanTime = -30f;
        private const float PLOTS_SCAN_INTERVAL = 12f;

        #region [START] GROWTH ACCELERATION COROUTINE
        private IEnumerator GrowthBoostLoop()
        {
            while (true)
            {
                yield return new WaitForSeconds(GrowthCheckIntervalSeconds);

                if (!EnableGrowthBoost && !EnableTreeGrowthBoost) continue;

                var player = PlayerHelper.GetLocalPlayer();
                if (player == null) continue;

                if (!PlayerHelper.IsHost()) continue;

                Vector3 playerPos = player.transform.position;
                float radiusSqr = GrowthRadius * GrowthRadius;

                // Refresh cached cropplots periodically instead of scanning scene for all plants
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

                foreach (var plot in _cachedPlots)
                {
                    if (plot == null) continue;
                    if ((plot.transform.position - playerPos).sqrMagnitude > radiusSqr) continue;

                    var slots = plot.GetSlots();
                    if (slots == null) continue;

                    for (int s = 0; s < slots.Count; s++)
                    {
                        var plant = slots[s]?.plant;
                        if (plant == null || plant.FullyGrown()) continue;

                        // Calculate multiplier (tree boost and crop boost are independent toggles)
                        // Plant.growTime is in minutes (see Plant.Awake: growTimeSec = growTime * 60f), so
                        // this threshold is 3 minutes, not 180 minutes — otherwise no non-palm tree would
                        // ever qualify and Tree Growth Boost would silently never apply to fruit trees.
                        bool isTree = plant is Plant_Palm || (plant.growTime > 3f);
                        if (isTree && !EnableTreeGrowthBoost) continue;
                        if (!isTree && !EnableGrowthBoost) continue;
                        float mult = isTree ? TreeGrowthMultiplier : CropGrowthMultiplier;

                        if (EnableFertilizerBoost)
                        {
                            mult *= FertilizerExtraMultiplier;
                        }

                        // Delta time added extra = (mult - 1.0) * interval
                        float extraSeconds = (mult - 1.0f) * GrowthCheckIntervalSeconds;
                        if (extraSeconds > 0f)
                        {
                            plant.IncrementGrowTimer(extraSeconds);
                        }
                    }
                }
            }
        }
        #endregion [END] GROWTH ACCELERATION COROUTINE
    }
    // ============================================================================
    // [END] FEATURE 19, 20, 25: CROP & TREE GROWTH MANAGER
    // ============================================================================
    #endregion
}
