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

        #region [START] IS HOST
        /// <summary>
        /// World state (crop growth, watering, harvesting, livestock resources) is authoritative on the host.
        /// A remote client mutating it directly would only touch its own local copy, causing desync/flicker
        /// with the host's replicated state (and, for inventory changes, potential item duplication/loss).
        /// </summary>
        public static bool IsHost()
        {
            try
            {
                return ComponentManager<Raft_Network>.Value == null || Raft_Network.IsHost;
            }
            catch
            {
                return true;
            }
        }
        #endregion [END] IS HOST
    }
    // ============================================================================
    // [END] PLAYER & WORLD HELPER
    // ============================================================================
    #endregion
}
