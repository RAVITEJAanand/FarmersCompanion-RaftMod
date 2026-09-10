using UnityEngine;

namespace FarmersCompanion.Helpers
{
    #region [START] PLAYER & WORLD HELPER
    // ============================================================================
    // [START] PLAYER & WORLD HELPER
    // Description: Safe wrappers to access local player, inventory, and raft instances.
    // ============================================================================
    public static class PlayerHelper
    {
        #region [START] GET LOCAL PLAYER
        public static Network_Player GetLocalPlayer()
        {
            try
            {
                return ComponentManager<Network_Player>.Value;
            }
            catch
            {
                return null;
            }
        }
        #endregion [END] GET LOCAL PLAYER

        #region [START] GET PLAYER INVENTORY
        public static PlayerInventory GetPlayerInventory()
        {
            try
            {
                var player = GetLocalPlayer();
                if (player != null && player.Inventory != null)
                {
                    return player.Inventory;
                }
                return ComponentManager<PlayerInventory>.Value;
            }
            catch
            {
                return null;
            }
        }
        #endregion [END] GET PLAYER INVENTORY

        #region [START] GET LOCAL RAFT
        public static Raft GetRaft()
        {
            try
            {
                return ComponentManager<Raft>.Value;
            }
            catch
            {
                return null;
            }
        }
        #endregion [END] GET LOCAL RAFT
    }
    // ============================================================================
    // [END] PLAYER & WORLD HELPER
    // ============================================================================
    #endregion
}
