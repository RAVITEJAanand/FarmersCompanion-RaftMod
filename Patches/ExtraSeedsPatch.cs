using System;
using FarmersCompanion.Features;
using HarmonyLib;
using UnityEngine;

namespace FarmersCompanion.Patches
{
    #region [START] PATCH: BONUS FRUIT & TREE SEEDS
    // ============================================================================
    // [START] PATCH: BONUS FRUIT & TREE SEEDS
    // Description: Extra seeds when a fruit crop is harvested and when a tree is felled.
    //              Hooks the game's own yield paths, so manual harvest, Auto Harvest and
    //              Multi-Harvest all count, and only the player who did the work gets seeds.
    // ============================================================================
    internal static class ExtraSeeds
    {
        // Tree types we know the seed for. The seed is normally read from the tree's own Plant
        // (raft trees) or its yield list; these names are only a fallback for island trees.
        private static readonly string[] TreeKinds = { "Palm", "Mango", "Banana", "Birch", "Pine" };

        internal static bool IsSeed(Item_Base item)
        {
            return item != null && item.UniqueName != null
                && item.UniqueName.StartsWith("Seed_", StringComparison.OrdinalIgnoreCase)
                && !item.UniqueName.Equals("Seed_Grass", StringComparison.OrdinalIgnoreCase);
        }

        internal static bool IsFruitCrop(Plant plant)
        {
            if (plant == null || plant is Plant_Palm || !IsSeed(plant.item)) return false;
            string list = Plugin.ExtraFruitSeedCrops != null ? Plugin.ExtraFruitSeedCrops.Value : "Watermelon,Pineapple,Strawberry";
            if (string.IsNullOrEmpty(list)) return false;
            foreach (var raw in list.Split(','))
            {
                string key = raw.Trim();
                if (key.Length > 0 && plant.item.UniqueName.IndexOf(key, StringComparison.OrdinalIgnoreCase) >= 0) return true;
            }
            return false;
        }

        internal static Item_Base ResolveTreeSeed(HarvestableTree tree)
        {
            if (tree == null) return null;

            // Trees grown on the raft are Plant_Palm (used for every tree type); its item is the seed.
            var plant = tree.GetComponentInParent<Plant>();
            if (plant != null && IsSeed(plant.item)) return plant.item;

            // Otherwise look for a seed in the tree's full (unchopped) yield list.
            var yh = tree.PickupItem != null ? tree.PickupItem.yieldHandler : null;
            if (yh != null && yh.yieldAsset != null && yh.yieldAsset.yieldAssets != null)
            {
                foreach (var cost in yh.yieldAsset.yieldAssets)
                {
                    if (cost != null && IsSeed(cost.item)) return cost.item;
                }
            }

            // Last resort: the tree's object name (own object and two parents).
            Transform t = tree.transform;
            for (int depth = 0; depth < 3 && t != null; depth++, t = t.parent)
            {
                string name = t.name ?? string.Empty;
                if (name.IndexOf("Pineapple", StringComparison.OrdinalIgnoreCase) >= 0) continue;
                foreach (var kind in TreeKinds)
                {
                    if (name.IndexOf(kind, StringComparison.OrdinalIgnoreCase) >= 0)
                    {
                        var seed = ItemManager.GetItemByName("Seed_" + kind);
                        if (IsSeed(seed)) return seed;
                    }
                }
            }
            return null;
        }

        internal static void Give(PlayerInventory inventory, Item_Base seed, int amount, string source)
        {
            if (inventory == null || seed == null || amount <= 0) return;
            // Same call the game uses for harvest yields: it stacks into the inventory and drops
            // whatever doesn't fit at the player's feet, so nothing is lost on a full inventory.
            inventory.AddItem(seed.UniqueName, amount);
            CropHarvestManager.BonusSeedsGivenThisSession += amount;
            Debug.Log($"[Farmer's Companion] Bonus seeds: +{amount} {seed.UniqueName} ({source})");
        }
    }

    // Fruit crops. Every crop harvest that pays out ends in PlantManager.Harvest(plant, giveYield: true):
    // the vanilla Interact harvest, the network replay of a remote player's harvest, and FC's own Auto
    // Harvest / Multi-Harvest. Animals and seagulls eating crops call it with giveYield: false.
    [HarmonyPatch(typeof(PlantManager), nameof(PlantManager.Harvest))]
    internal static class PlantManager_Harvest_ExtraSeedsPatch
    {
        private static void Prefix(Plant plant, bool giveYield, out Item_Base __state)
        {
            __state = null;
            try
            {
                var mgr = CropHarvestManager.Instance;
                if (mgr == null || !mgr.EnableExtraFruitSeeds || !giveYield || plant == null) return;
                // Same gate as vanilla's harvest prompt, read before Harvest() pulls the plant.
                if (!plant.FullyGrown() || !plant.harvestable || !plant.playerCanHarvest) return;
                if (ExtraSeeds.IsFruitCrop(plant)) __state = plant.item;
            }
            catch { }
        }

        private static void Postfix(PlantManager __instance, bool giveYield, Item_Base __state)
        {
            if (__state == null || !giveYield) return;
            try
            {
                // Vanilla only pays the yield out on the harvesting player's own machine
                // (playerNetwork.IsLocalPlayer); match that so seeds are never doubled or misrouted.
                var player = __instance != null ? __instance.GetComponentInParent<Network_Player>() : null;
                if (player == null || !player.IsLocalPlayer || player.Inventory == null) return;
                var mgr = CropHarvestManager.Instance;
                if (mgr == null) return;
                ExtraSeeds.Give(player.Inventory, __state, mgr.ExtraFruitSeeds, "fruit harvest");
            }
            catch { }
        }
    }

    // Trees. Each axe hit calls HarvestableTree.Harvest(inventory); the inventory is non-null only on
    // the chopping player's machine. The hit that empties the yield list is the one that fells the tree.
    [HarmonyPatch(typeof(HarvestableTree), nameof(HarvestableTree.Harvest))]
    internal static class HarvestableTree_Harvest_ExtraSeedsPatch
    {
        private static void Postfix(HarvestableTree __instance, PlayerInventory playerInventory, bool __result)
        {
            if (!__result || playerInventory == null || __instance == null) return;
            try
            {
                var mgr = CropHarvestManager.Instance;
                if (mgr == null || !mgr.EnableExtraTreeSeeds) return;
                if (!__instance.Depleted) return;

                var seed = ExtraSeeds.ResolveTreeSeed(__instance);
                if (seed == null)
                {
                    Debug.Log($"[Farmer's Companion] Bonus tree seeds: no seed type found for '{__instance.name}'");
                    return;
                }
                ExtraSeeds.Give(playerInventory, seed, mgr.ExtraTreeSeeds, "tree felled: " + __instance.name);
            }
            catch { }
        }
    }
    // ============================================================================
    // [END] PATCH: BONUS FRUIT & TREE SEEDS
    // ============================================================================
    #endregion
}
