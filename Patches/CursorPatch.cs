using System;
using HarmonyLib;
using UnityEngine;
using FarmersCompanion.UI;

namespace FarmersCompanion.Patches
{
    #region [START] PATCH: CURSOR UNLOCKING & CAMERA FREEZE
    // ============================================================================
    // [START] PATCH: CURSOR UNLOCKING & CAMERA FREEZE
    // Description: Ensures mouse cursor is freed and camera is frozen when Farmer's Companion UI is open.
    // ============================================================================
    [HarmonyPatch(typeof(MouseLook), "Update")]
    public static class MouseLookUpdatePatch
    {
        public static bool Prefix()
        {
            if (CanvasFarmersCompanionUI.IsWindowOpen)
            {
                return false; // Freeze camera rotation completely while Farmer's Companion UI is open!
            }
            return true;
        }
    }

    [HarmonyPatch(typeof(Helper), "SetCursorVisibleAndLockState")]
    public static class HelperSetCursorVisibleAndLockStatePatch
    {
        public static void Prefix(ref bool state, ref CursorLockMode mode)
        {
            if (CanvasFarmersCompanionUI.IsWindowOpen)
            {
                state = true;
                mode = CursorLockMode.None;
            }
        }
    }

    [HarmonyPatch(typeof(Helper), "SetCursorLockState")]
    public static class HelperSetCursorLockStatePatch
    {
        public static void Prefix(ref CursorLockMode mode)
        {
            if (CanvasFarmersCompanionUI.IsWindowOpen)
            {
                mode = CursorLockMode.None;
            }
        }
    }

    [HarmonyPatch(typeof(Helper), "SetCursorVisible")]
    public static class HelperSetCursorVisiblePatch
    {
        public static void Prefix(ref bool state)
        {
            if (CanvasFarmersCompanionUI.IsWindowOpen)
            {
                state = true;
            }
        }
    }
    // ============================================================================
    // [END] PATCH: CURSOR UNLOCKING & CAMERA FREEZE
    // ============================================================================
    #endregion
}
