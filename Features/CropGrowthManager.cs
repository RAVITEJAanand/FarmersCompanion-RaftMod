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

        #region [START] GROWTH ACCELERATION COROUTINE
        private IEnumerator GrowthBoostLoop()
        {
            while (true)
            {
                yield return new WaitForSeconds(GrowthCheckIntervalSeconds);

                if (!EnableGrowthBoost) continue;

                var player = PlayerHelper.GetLocalPlayer();
                if (player == null) continue;

                Vector3 playerPos = player.transform.position;
                float radiusSqr = GrowthRadius * GrowthRadius;

                Plant[] plants = FindObjectsOfType<Plant>();
                if (plants == null || plants.Length == 0) continue;

                foreach (var plant in plants)
                {
                    if (plant == null || plant.FullyGrown()) continue;

                    if ((plant.transform.position - playerPos).sqrMagnitude > radiusSqr) continue;

                    // Calculate multiplier
                    bool isTree = plant is Plant_Palm || (plant.growTime > 180f);
                    float mult = isTree ? (EnableTreeGrowthBoost ? TreeGrowthMultiplier : 1.0f) : CropGrowthMultiplier;

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
        #endregion [END] GROWTH ACCELERATION COROUTINE
    }
    // ============================================================================
    // [END] FEATURE 19, 20, 25: CROP & TREE GROWTH MANAGER
    // ============================================================================
    #endregion
}
