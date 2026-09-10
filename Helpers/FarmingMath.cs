using UnityEngine;

namespace FarmersCompanion.Helpers
{
    #region [START] FARMING MATH & OPTIMIZATION HELPER
    // ============================================================================
    // [START] FARMING MATH & OPTIMIZATION HELPER
    // Description: Highly optimized math functions to prevent CPU & thermal strain.
    //              Avoids Math.Sqrt() by comparing squared magnitudes directly.
    // ============================================================================
    public static class FarmingMath
    {
        #region [START] IS WITHIN DISTANCE SQR
        /// <summary>
        /// Fast distance check using squared magnitude. Zero CPU thermal overhead.
        /// </summary>
        public static bool IsWithinDistanceSqr(Vector3 a, Vector3 b, float maxDistance)
        {
            float sqrDist = (a - b).sqrMagnitude;
            return sqrDist <= (maxDistance * maxDistance);
        }
        #endregion [END] IS WITHIN DISTANCE SQR
    }
    // ============================================================================
    // [END] FARMING MATH & OPTIMIZATION HELPER
    // ============================================================================
    #endregion
}
