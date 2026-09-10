using System;
using UnityEngine;
using UnityEngine.InputSystem;

namespace FarmersCompanion.Helpers
{
    #region [START] INPUT HELPER: HYBRID INPUT DETECTION
    // ============================================================================
    // [START] INPUT HELPER: HYBRID INPUT DETECTION
    // Description: Hybrid polling supporting both Unity Legacy Input and New Input System.
    //              Ensures function keys (F1, etc.) register reliably across all game states.
    // ============================================================================
    public static class InputHelper
    {
        #region [START] WAS KEY PRESSED
        public static bool WasKeyPressed(KeyCode legacyKey)
        {
            try
            {
                return Input.GetKeyDown(legacyKey);
            }
            catch
            {
                // Fallback to New Input System only if Legacy Input is disabled/throws
                try
                {
                    var kb = Keyboard.current;
                    if (kb != null)
                    {
                        switch (legacyKey)
                        {
                            case KeyCode.F1: return kb.f1Key.wasPressedThisFrame;
                            case KeyCode.F2: return kb.f2Key.wasPressedThisFrame;
                            case KeyCode.F3: return kb.f3Key.wasPressedThisFrame;
                            case KeyCode.F4: return kb.f4Key.wasPressedThisFrame;
                            case KeyCode.F5: return kb.f5Key.wasPressedThisFrame;
                            case KeyCode.F6: return kb.f6Key.wasPressedThisFrame;
                            case KeyCode.F7: return kb.f7Key.wasPressedThisFrame;
                            case KeyCode.F8: return kb.f8Key.wasPressedThisFrame;
                            case KeyCode.F9: return kb.f9Key.wasPressedThisFrame;
                            case KeyCode.F10: return kb.f10Key.wasPressedThisFrame;
                            case KeyCode.F11: return kb.f11Key.wasPressedThisFrame;
                            case KeyCode.F12: return kb.f12Key.wasPressedThisFrame;
                            case KeyCode.H: return kb.hKey.wasPressedThisFrame;
                            case KeyCode.Y: return kb.yKey.wasPressedThisFrame;
                            case KeyCode.Z: return kb.zKey.wasPressedThisFrame;
                            case KeyCode.Escape: return kb.escapeKey.wasPressedThisFrame;
                        }
                    }
                }
                catch { }
                return false;
            }
        }
        #endregion [END] WAS KEY PRESSED
    }
    // ============================================================================
    // [END] INPUT HELPER: HYBRID INPUT DETECTION
    // ============================================================================
    #endregion
}
