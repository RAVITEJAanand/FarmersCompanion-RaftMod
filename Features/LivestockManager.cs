using System.Collections;
using FarmersCompanion.Helpers;
using UnityEngine;

namespace FarmersCompanion.Features
{
    #region [START] FEATURE 21, 22: LIVESTOCK & RESOURCE HARVEST MANAGER
    // ============================================================================
    // [START] FEATURE 21, 22: LIVESTOCK & RESOURCE HARVEST MANAGER
    // Description: Automatic collection of animal products (Wool from Llamas, Milk from Goats, Eggs from Clackers).
    //              Eliminates the tedious chasing of animals with shears/buckets.
    // ============================================================================
    public class LivestockManager : MonoBehaviour
    {
        public static LivestockManager Instance { get; private set; }

        #region [START] CONFIGURATION PROPERTIES
        public bool EnableAutoCollectLivestock { get; set; } = true;
        public float LivestockRadius { get; set; } = 30f;
        public float CheckIntervalSeconds { get; set; } = 3.5f;
        #endregion [END] CONFIGURATION PROPERTIES

        private Coroutine _livestockRoutine;

        #region [START] UNITY LIFECYCLE
        private void Awake()
        {
            Instance = this;
        }

        private void OnEnable()
        {
            if (_livestockRoutine != null)
            {
                StopCoroutine(_livestockRoutine);
            }
            _livestockRoutine = StartCoroutine(LivestockLoop());
        }

        private void OnDisable()
        {
            if (_livestockRoutine != null)
            {
                StopCoroutine(_livestockRoutine);
                _livestockRoutine = null;
            }
        }
        #endregion [END] UNITY LIFECYCLE

        #region [START] LIVESTOCK RESOURCE HARVEST COROUTINE
        private IEnumerator LivestockLoop()
        {
            while (true)
            {
                yield return new WaitForSeconds(CheckIntervalSeconds);

                if (!EnableAutoCollectLivestock) continue;

                var player = PlayerHelper.GetLocalPlayer();
                if (player == null) continue;

                Vector3 playerPos = player.transform.position;
                float radiusSqr = LivestockRadius * LivestockRadius;

                // Find all domestic resource animals within radius
                var domesticResources = FindObjectsOfType<AI_NetworkBehaviour_Domestic_Resource>();
                if (domesticResources == null || domesticResources.Length == 0) continue;

                foreach (var animal in domesticResources)
                {
                    if (animal == null) continue;

                    if ((animal.transform.position - playerPos).sqrMagnitude > radiusSqr) continue;

                    try
                    {
                        var resource = animal.Resource;
                        if (resource != null && resource.IsReady)
                        {
                            // Harvest resource directly into player inventory
                            resource.HarvestResource();
                        }
                    }
                    catch
                    {
                        // Safely ignore any transient animal state
                    }
                }
            }
        }
        #endregion [END] LIVESTOCK RESOURCE HARVEST COROUTINE
    }
    // ============================================================================
    // [END] FEATURE 21, 22: LIVESTOCK & RESOURCE HARVEST MANAGER
    // ============================================================================
    #endregion
}
