using System;
using System.Reflection;
using HarmonyLib;
using UnityEngine;
using FarmersCompanion.UI;

namespace FarmersCompanion.Patches
{
    #region [START] PATCH: CURSOR UNLOCKING & CAMERA FREEZE
    // ============================================================================
    // [START] PATCH: CURSOR UNLOCKING & CAMERA FREEZE
    // Description: Ensures mouse cursor is freed and camera is frozen when any mod UI is open.
    // ============================================================================
    public static class CursorPatchHelper
    {
        private static PropertyInfo _scWindowProp;
        private static PropertyInfo _imWindowProp;
        private static bool _typesResolved = false;

        public static bool ShouldForceCursorFree()
        {
            // 1. Farmer's Companion UI
            if (CanvasFarmersCompanionUI.IsWindowOpen) return true;

            // 2. Peer Mods (Sailor's Companion & Inventory Master)
            if (!_typesResolved) ResolvePeerTypes();

            if (_scWindowProp != null)
            {
                try { if ((bool)_scWindowProp.GetValue(null)) return true; } catch { }
            }
            if (_imWindowProp != null)
            {
                try { if ((bool)_imWindowProp.GetValue(null)) return true; } catch { }
            }

            return false;
        }

        private static void ResolvePeerTypes()
        {
            try
            {
                foreach (var asm in AppDomain.CurrentDomain.GetAssemblies())
                {
                    if (_scWindowProp == null)
                    {
                        var scType = asm.GetType("SailorsCompanion.UI.CanvasModUI");
                        if (scType != null)
                            _scWindowProp = scType.GetProperty("IsWindowOpen", BindingFlags.Public | BindingFlags.Static);
                    }
                    if (_imWindowProp == null)
                    {
                        var imType = asm.GetType("InventoryMaster.UI.CanvasInventoryMasterUI");
                        if (imType != null)
                            _imWindowProp = imType.GetProperty("IsWindowOpen", BindingFlags.Public | BindingFlags.Static);
                    }
                }
                if (_scWindowProp != null && _imWindowProp != null) _typesResolved = true;
            }
            catch { }
        }
    }

    [HarmonyPatch(typeof(MouseLook), "Update")]
    public static class MouseLookUpdatePatch
    {
        public static bool Prefix()
        {
            if (CursorPatchHelper.ShouldForceCursorFree())
            {
                return false; // Freeze camera rotation completely while any mod UI is open!
            }
            return true;
        }
    }

    [HarmonyPatch(typeof(Helper), "SetCursorVisibleAndLockState")]
    public static class HelperSetCursorVisibleAndLockStatePatch
    {
        public static void Prefix(ref bool state, ref CursorLockMode mode)
        {
            if (CursorPatchHelper.ShouldForceCursorFree())
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
            if (CursorPatchHelper.ShouldForceCursorFree())
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
            if (CursorPatchHelper.ShouldForceCursorFree())
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
