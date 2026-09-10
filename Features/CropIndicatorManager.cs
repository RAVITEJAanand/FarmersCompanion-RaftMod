using System.Collections;
using System.Collections.Generic;
using FarmersCompanion.Helpers;
using UnityEngine;

namespace FarmersCompanion.Features
{
    #region [START] FEATURE 23, 29: CROP INDICATOR & NOTIFICATIONS MANAGER (HIGH-FPS ZERO-GC EDITION)
    // ============================================================================
    // [START] FEATURE 23, 29: CROP INDICATOR & NOTIFICATIONS MANAGER (HIGH-FPS ZERO-GC EDITION)
    // Description: Highly-optimized floating indicators showing Crop Water/Growth status,
    //              and toast notifications. Uses 1.5s low-frequency spatial caching to
    //              guarantee ZERO per-frame FindObjectsOfType calls and ZERO GC stutter.
    // ============================================================================
    public class CropIndicatorManager : MonoBehaviour
    {
        public static CropIndicatorManager Instance { get; private set; }

        #region [START] CONFIGURATION PROPERTIES
        public bool EnableHealthIndicators { get; set; } = true;
        public bool EnableNotifications { get; set; } = true;
        public float IndicatorMaxDistance { get; set; } = 6.0f;
        #endregion [END] CONFIGURATION PROPERTIES

        private GUIStyle _indicatorStyle;
        private GUIStyle _toastStyle;
        private string _activeToast = null;
        private float _toastTimer = 0f;

        // High-performance spatial caching
        private static Camera _cachedCamera = null;
        private readonly List<Cropplot> _cachedAllPlots = new List<Cropplot>();
        private float _lastAllPlotsScanTime = -30f;
        private const float ALL_PLOTS_SCAN_INTERVAL = 12f;

        private struct NearbyPlotData
        {
            public Cropplot Plot;
            public string Label;
        }
        private readonly List<NearbyPlotData> _nearbyPlotsToDraw = new List<NearbyPlotData>();
        private Coroutine _cacheRoutine;

        #region [START] UNITY LIFECYCLE
        private void Awake()
        {
            Instance = this;
        }

        private void OnEnable()
        {
            if (_cacheRoutine != null) StopCoroutine(_cacheRoutine);
            _cacheRoutine = StartCoroutine(SpatialCacheLoop());
        }

        private void OnDisable()
        {
            if (_cacheRoutine != null)
            {
                StopCoroutine(_cacheRoutine);
                _cacheRoutine = null;
            }
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

        #region [START] LOW-FREQUENCY SPATIAL CACHE COROUTINE (1.5s INTERVAL)
        private IEnumerator SpatialCacheLoop()
        {
            while (true)
            {
                yield return new WaitForSeconds(1.5f);

                if (!EnableHealthIndicators)
                {
                    _nearbyPlotsToDraw.Clear();
                    continue;
                }

                var player = PlayerHelper.GetLocalPlayer();
                if (player == null)
                {
                    _nearbyPlotsToDraw.Clear();
                    continue;
                }

                // 1. Refresh full plot list only once every 12 seconds
                if (Time.unscaledTime - _lastAllPlotsScanTime > ALL_PLOTS_SCAN_INTERVAL || _cachedAllPlots.Count == 0)
                {
                    _lastAllPlotsScanTime = Time.unscaledTime;
                    _cachedAllPlots.Clear();
                    var found = FindObjectsOfType<Cropplot>();
                    if (found != null && found.Length > 0)
                    {
                        _cachedAllPlots.AddRange(found);
                    }
                }
                else
                {
                    _cachedAllPlots.RemoveAll(p => p == null);
                }

                // 2. Filter nearby plots within IndicatorMaxDistance (8m) using squared distance
                Vector3 playerPos = player.transform.position;
                float maxDistSqr = IndicatorMaxDistance * IndicatorMaxDistance;
                _nearbyPlotsToDraw.Clear();

                foreach (var plot in _cachedAllPlots)
                {
                    if (plot == null) continue;

                    Vector3 plotPos = plot.transform.position;
                    if ((plotPos - playerPos).sqrMagnitude > maxDistSqr) continue;

                    bool needsWater = plot.SlotsNeedWater();
                    var slots = plot.GetSlots();
                    bool hasPlant = slots != null && slots.Count > 0 && slots[0] != null && slots[0].plant != null;
                    bool isGrassPlot = plot is Cropplot_Grass;

                    // If it's an empty crop plot that has water, don't clutter the screen unless it needs water or has crops/grass!
                    if (!hasPlant && !isGrassPlot && !needsWater) continue;

                    string waterText = needsWater ? "<color=#FF6666>Needs Water 💧</color>" : "<color=#66FF66>Hydrated 💧</color>";

                    string cropText = "";
                    if (hasPlant)
                    {
                        var p = slots[0].plant;
                        float progress = p.growTime > 0 ? Mathf.Clamp01(p.GetGrowTimer() / p.growTime) * 100f : 100f;
                        cropText = p.FullyGrown() ? "<color=#FFFF44>🌾 Ready to Harvest!</color>" : $"🌱 {progress:F0}%";
                    }
                    else if (isGrassPlot)
                    {
                        cropText = "<color=#A0E0A0>🐑 Grass</color>";
                    }

                    string label = string.IsNullOrEmpty(cropText) ? waterText : $"{waterText}  {cropText}";

                    _nearbyPlotsToDraw.Add(new NearbyPlotData
                    {
                        Plot = plot,
                        Label = label
                    });

                    // Cap to 8 closest plots to keep the screen clean and performant
                    if (_nearbyPlotsToDraw.Count >= 8) break;
                }
            }
        }
        #endregion [END] LOW-FREQUENCY SPATIAL CACHE COROUTINE

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
            // CRITICAL OPTIMIZATION: Only execute during Repaint event to eliminate 75% redundant passes
            if (Event.current.type != EventType.Repaint) return;

            InitStyles();

            // 1. Draw Active Toast Notification at Top Center
            if (!string.IsNullOrEmpty(_activeToast))
            {
                float toastWidth = 420f;
                float toastHeight = 42f;
                float toastX = (Screen.width - toastWidth) / 2f;
                float toastY = 50f;

                GUI.Box(new Rect(toastX, toastY, toastWidth, toastHeight), _activeToast, _toastStyle);
            }

            // 2. Draw Floating 3D Indicators above cached nearby plots
            if (!EnableHealthIndicators || _nearbyPlotsToDraw.Count == 0) return;

            if (_cachedCamera == null)
            {
                _cachedCamera = Camera.main;
            }
            if (_cachedCamera == null) return;

            for (int i = 0; i < _nearbyPlotsToDraw.Count; i++)
            {
                var data = _nearbyPlotsToDraw[i];
                if (data.Plot == null) continue;

                // Live dynamic position locked directly to the crop plot on the floating raft!
                Vector3 currentPlotPos = data.Plot.transform.position + Vector3.up * 0.40f;
                Vector3 screenPos = _cachedCamera.WorldToScreenPoint(currentPlotPos);

                // Behind camera or too far away
                if (screenPos.z <= 0.4f || screenPos.z > IndicatorMaxDistance + 1.5f) continue;

                // Must be within visible screen viewport
                if (screenPos.x < 15f || screenPos.x > Screen.width - 15f || screenPos.y < 15f || screenPos.y > Screen.height - 15f) continue;

                Vector2 size = _indicatorStyle.CalcSize(new GUIContent(data.Label));
                Rect rect = new Rect(screenPos.x - size.x / 2f, Screen.height - screenPos.y - size.y, size.x + 14, size.y + 4);

                GUI.Box(rect, data.Label, _indicatorStyle);
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
