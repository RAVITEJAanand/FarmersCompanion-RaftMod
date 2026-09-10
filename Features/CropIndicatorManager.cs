using System.Collections.Generic;
using FarmersCompanion.Helpers;
using UnityEngine;

namespace FarmersCompanion.Features
{
    #region [START] FEATURE 23, 29: CROP INDICATOR & NOTIFICATIONS MANAGER
    // ============================================================================
    // [START] FEATURE 23, 29: CROP INDICATOR & NOTIFICATIONS MANAGER
    // Description: Floating indicators showing Crop Water/Growth status, and unobtrusive
    //              notifications when crops or livestock are ready.
    // ============================================================================
    public class CropIndicatorManager : MonoBehaviour
    {
        public static CropIndicatorManager Instance { get; private set; }

        #region [START] CONFIGURATION PROPERTIES
        public bool EnableHealthIndicators { get; set; } = true;
        public bool EnableNotifications { get; set; } = true;
        public float IndicatorMaxDistance { get; set; } = 8.0f;
        #endregion [END] CONFIGURATION PROPERTIES

        private GUIStyle _indicatorStyle;
        private GUIStyle _toastStyle;
        private string _activeToast = null;
        private float _toastTimer = 0f;

        #region [START] UNITY LIFECYCLE
        private void Awake()
        {
            Instance = this;
        }

        private void Update()
        {
            if (_toastTimer > 0f)
            {
                _toastTimer -= Time.deltaTime;
                if (_toastTimer <= 0f)
                {
                    _activeToast = null;
                }
            }
        }
        #endregion [END] UNITY LIFECYCLE

        #region [START] SHOW TOAST NOTIFICATION
        public void ShowNotification(string message, float duration = 3.5f)
        {
            if (!EnableNotifications) return;
            _activeToast = message;
            _toastTimer = duration;
        }
        #endregion [END] SHOW TOAST NOTIFICATION

        #region [START] ONGUI INDICATOR DRAWING
        private void OnGUI()
        {
            var camera = Camera.main;
            if (camera == null) return;

            InitStyles();

            // 1. Draw Active Toast Notification at Top Center
            if (!string.IsNullOrEmpty(_activeToast))
            {
                float toastWidth = 400f;
                float toastHeight = 40f;
                float toastX = (Screen.width - toastWidth) / 2f;
                float toastY = 50f;

                GUI.Box(new Rect(toastX, toastY, toastWidth, toastHeight), _activeToast, _toastStyle);
            }

            // 2. Draw Floating 3D Indicators above nearby plots
            if (!EnableHealthIndicators) return;

            var player = PlayerHelper.GetLocalPlayer();
            if (player == null) return;

            Vector3 playerPos = player.transform.position;
            float maxDistSqr = IndicatorMaxDistance * IndicatorMaxDistance;

            Cropplot[] plots = FindObjectsOfType<Cropplot>();
            if (plots == null || plots.Length == 0) return;

            foreach (var plot in plots)
            {
                if (plot == null) continue;

                Vector3 plotPos = plot.transform.position;
                if ((plotPos - playerPos).sqrMagnitude > maxDistSqr) continue;

                // Project to screen space
                Vector3 screenPos = camera.WorldToScreenPoint(plotPos + Vector3.up * 0.8f);
                if (screenPos.z <= 0.1f) continue; // Behind camera

                bool needsWater = plot.SlotsNeedWater();
                string waterText = needsWater ? "<color=#FF6666>Needs Water 💧</color>" : "<color=#66FF66>Hydrated 💧</color>";

                // Check first slot plant
                string cropText = "";
                var slots = plot.GetSlots();
                if (slots != null && slots.Count > 0 && slots[0].plant != null)
                {
                    var p = slots[0].plant;
                    float progress = p.growTime > 0 ? Mathf.Clamp01(p.GetGrowTimer() / p.growTime) * 100f : 100f;
                    cropText = p.FullyGrown() ? "<color=#FFFF44>🌾 Ready to Harvest!</color>" : $"🌱 {progress:F0}%";
                }

                string label = $"{waterText}  {cropText}";
                Vector2 size = _indicatorStyle.CalcSize(new GUIContent(label));
                Rect rect = new Rect(screenPos.x - size.x / 2f, Screen.height - screenPos.y - size.y, size.x + 16, size.y + 6);

                GUI.Box(rect, label, _indicatorStyle);
            }
        }
        #endregion [END] ONGUI INDICATOR DRAWING

        #region [START] STYLES INITIALIZATION
        private void InitStyles()
        {
            if (_indicatorStyle == null)
            {
                _indicatorStyle = new GUIStyle(GUI.skin.box)
                {
                    fontSize = 12,
                    fontStyle = FontStyle.Bold,
                    alignment = TextAnchor.MiddleCenter,
                    richText = true
                };
            }

            if (_toastStyle == null)
            {
                _toastStyle = new GUIStyle(GUI.skin.box)
                {
                    fontSize = 14,
                    fontStyle = FontStyle.Bold,
                    alignment = TextAnchor.MiddleCenter,
                    richText = true
                };
            }
        }
        #endregion [END] STYLES INITIALIZATION
    }
    // ============================================================================
    // [END] FEATURE 23, 29: CROP INDICATOR & NOTIFICATIONS MANAGER
    // ============================================================================
    #endregion
}
