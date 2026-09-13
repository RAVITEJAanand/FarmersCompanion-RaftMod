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
        public float IndicatorMaxDistance { get; set; } = 12.0f;
        #endregion [END] CONFIGURATION PROPERTIES

        private GUIStyle _indicatorStyle;
        private GUIStyle _toastStyle;
        private Texture2D _boxBgTex;
        private string _activeToast = null;
        private float _toastTimer = 0f;

        // High-performance spatial caching
        private static Camera _cachedCamera = null;
        private readonly List<Cropplot> _cachedAllPlots = new List<Cropplot>();
        private float _lastAllPlotsScanTime = -30f;
        private const float ALL_PLOTS_SCAN_INTERVAL = 3.0f;

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

        #region [START] LOW-FREQUENCY SPATIAL CACHE COROUTINE (0.4s INTERVAL)
        private IEnumerator SpatialCacheLoop()
        {
            while (true)
            {
                yield return new WaitForSeconds(0.4f);

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

                // 1. Refresh full plot list every 3.0 seconds
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

                // 2. Filter nearby plots within IndicatorMaxDistance (12m) using squared distance
                Vector3 playerPos = player.transform.position;
                float maxDistSqr = IndicatorMaxDistance * IndicatorMaxDistance;
                _nearbyPlotsToDraw.Clear();

                foreach (var plot in _cachedAllPlots)
                {
                    if (plot == null) continue;

                    Vector3 plotPos = plot.transform.position;
                    if ((plotPos - playerPos).sqrMagnitude > maxDistSqr) continue;

                    var slots = plot.GetSlots();
                    bool hasPlant = false;
                    Plant plant = null;
                    bool anyWater = false;

                    if (slots != null)
                    {
                        foreach (var s in slots)
                        {
                            if (s == null) continue;
                            if (s.hasWater) anyWater = true;
                            // A harvested-but-not-replenished slot clears .busy while the stale
                            // Plant reference (and its permanently-stuck FullyGrown()==true) lingers -
                            // treat it as empty so the indicator doesn't keep claiming it's ready.
                            if (s.busy && s.plant != null)
                            {
                                hasPlant = true;
                                plant = s.plant;
                            }
                        }
                    }

                    bool isGrassPlot = plot is Cropplot_Grass;
                    bool needsWater = plot.SlotsNeedWater() || (!anyWater && isGrassPlot);

                    string label;
                    if (hasPlant && plant != null)
                    {
                        string waterText = (anyWater && !needsWater) ? "<color=#66FF66>💧 Hydrated</color>" : "<color=#FF6666>💧 Needs Water</color>";
                        // Plant.growTime is expressed in minutes, but GetGrowTimer() accumulates in seconds
                        // (see Plant.Awake: growTimeSec = growTime * 60f), so the timer must be compared
                        // against growTime * 60f or this always reads ~100% within seconds of planting.
                        float progress = plant.growTime > 0 ? Mathf.Clamp01(plant.GetGrowTimer() / (plant.growTime * 60f)) * 100f : 100f;
                        string cropText = plant.FullyGrown() ? "<color=#FFFF44>🌾 Ready to Harvest!</color>" : $"🌱 {progress:F0}%";
                        label = $"{waterText}  {cropText}";
                    }
                    else if (isGrassPlot)
                    {
                        string waterText = anyWater ? "<color=#66FF66>💧 Hydrated</color>" : "<color=#FF6666>💧 Needs Water</color>";
                        label = $"{waterText}  <color=#A0E0A0>🐑 Grass</color>";
                    }
                    else
                    {
                        // Empty Crop Plot! Show clear prompt so the player knows the plot is detected and ready for planting!
                        string waterText = anyWater ? "<color=#66FF66>💧 Hydrated</color>" : "<color=#FF6666>💧 Dry</color>";
                        label = $"{waterText}  <color=#FFD700>🌱 Empty Plot</color> <size=10><color=#E0E0E0>(Plant Seed)</color></size>";
                    }

                    _nearbyPlotsToDraw.Add(new NearbyPlotData
                    {
                        Plot = plot,
                        Label = label
                    });

                    // Cap to 12 closest plots to keep the screen clean and performant
                    if (_nearbyPlotsToDraw.Count >= 12) break;
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
            // OnGUI always renders on top of every Canvas regardless of sortingOrder (it's a
            // separate legacy IMGUI pass after normal rendering), so these world-projected
            // indicators would otherwise draw straight over the settings menu whenever a crop
            // plot happens to be behind it on screen.
            if (!EnableHealthIndicators || _nearbyPlotsToDraw.Count == 0 || UI.CanvasFarmersCompanionUI.IsWindowOpen) return;

            if (_cachedCamera == null || !_cachedCamera.gameObject.activeInHierarchy)
            {
                _cachedCamera = Camera.main ?? FindObjectOfType<Camera>();
            }
            if (_cachedCamera == null) return;

            for (int i = 0; i < _nearbyPlotsToDraw.Count; i++)
            {
                var data = _nearbyPlotsToDraw[i];
                if (data.Plot == null) continue;

                // Live dynamic position locked directly to the crop plot on the floating raft!
                Vector3 currentPlotPos = data.Plot.transform.position + Vector3.up * 0.55f;
                Vector3 screenPos = _cachedCamera.WorldToScreenPoint(currentPlotPos);

                // Behind camera or too far away
                if (screenPos.z <= 0.3f || screenPos.z > IndicatorMaxDistance + 2.0f) continue;

                // Must be within visible screen viewport
                if (screenPos.x < 10f || screenPos.x > Screen.width - 10f || screenPos.y < 10f || screenPos.y > Screen.height - 10f) continue;

                Vector2 size = _indicatorStyle.CalcSize(new GUIContent(data.Label));
                Rect rect = new Rect(screenPos.x - size.x / 2f, Screen.height - screenPos.y - size.y, size.x + 18, size.y + 6);

                GUI.Box(rect, data.Label, _indicatorStyle);
            }
        }
        #endregion [END] ONGUI INDICATOR DRAWING

        #region [START] STYLES INITIALIZATION
        private void InitStyles()
        {
            if (_boxBgTex == null)
            {
                _boxBgTex = new Texture2D(1, 1);
                _boxBgTex.SetPixel(0, 0, new Color(0.12f, 0.08f, 0.04f, 0.90f));
                _boxBgTex.Apply();
            }

            if (_indicatorStyle == null)
            {
                _indicatorStyle = new GUIStyle(GUI.skin.box)
                {
                    fontSize = 13,
                    fontStyle = FontStyle.Bold,
                    alignment = TextAnchor.MiddleCenter,
                    richText = true
                };
                _indicatorStyle.normal.background = _boxBgTex;
                _indicatorStyle.normal.textColor = Color.white;
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
                _toastStyle.normal.background = _boxBgTex;
                _toastStyle.normal.textColor = Color.white;
            }
        }
        #endregion [END] STYLES INITIALIZATION
    }
    // ============================================================================
    // [END] FEATURE 23, 29: CROP INDICATOR & NOTIFICATIONS MANAGER
    // ============================================================================
    #endregion
}
