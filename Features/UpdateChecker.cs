using System;
using System.Collections;
using UnityEngine;
using UnityEngine.Networking;
using FarmersCompanion.Helpers;

namespace FarmersCompanion.Features
{
    #region [START] MODULE: IN-GAME UPDATE CHECKER & NOTIFICATION ENGINE
    // ============================================================================
    // [START] MODULE: IN-GAME UPDATE CHECKER & NOTIFICATION ENGINE
    // Purpose: Asynchronously checks the remote GitHub repository for new releases
    //          of Farmer's Companion and alerts players via toast and UI banner.
    // ============================================================================
    public class UpdateChecker : MonoBehaviour
    {
        public static UpdateChecker Instance { get; private set; }

        public const string VERSION_URL = "https://raw.githubusercontent.com/RAVITEJAanand/FarmersCompanion-RaftMod/main/version.json";
        public const string DEFAULT_DOWNLOAD_URL = "https://github.com/RAVITEJAanand/FarmersCompanion-RaftMod/releases";

        public static bool IsUpdateAvailable { get; private set; } = false;
        public static string LatestVersion { get; private set; } = "";
        public static string DownloadUrl { get; private set; } = DEFAULT_DOWNLOAD_URL;
        public static string ReleaseNotes { get; private set; } = "";
        public static bool HasChecked { get; private set; } = false;
        public static bool IsChecking { get; private set; } = false;
        public static bool Dismissed { get; set; } = false;

        private static bool _notifiedInWorld = false;

        [Serializable]
        public class VersionManifest
        {
            public string version;
            public string name;
            public string downloadUrl;
            public string raftModdingUrl;
            public string notes;
        }

        private void Awake()
        {
            Instance = this;
            gameObject.hideFlags = HideFlags.HideAndDontSave;
            DontDestroyOnLoad(gameObject);
        }

        private void Start()
        {
            if (Plugin.CheckForUpdates != null && Plugin.CheckForUpdates.Value)
            {
                TriggerCheck();
            }
        }

        private void Update()
        {
            if (IsUpdateAvailable && !_notifiedInWorld)
            {
                var player = PlayerHelper.GetLocalPlayer();
                if (player != null)
                {
                    _notifiedInWorld = true;
                    StartCoroutine(DelayedWorldNotification());
                }
            }
        }

        private IEnumerator DelayedWorldNotification()
        {
            yield return new WaitForSeconds(3.5f);
            CropIndicatorManager.Instance?.ShowNotification($"🌾 Farmer's Companion: Update v{LatestVersion} available! Press [F1] for details.");
        }

        public void TriggerCheck()
        {
            if (IsChecking) return;
            StartCoroutine(CheckForUpdatesRoutine());
        }

        private IEnumerator CheckForUpdatesRoutine()
        {
            IsChecking = true;

            using (UnityWebRequest req = UnityWebRequest.Get(VERSION_URL))
            {
                req.timeout = 6;
                yield return req.SendWebRequest();

                HasChecked = true;
                IsChecking = false;

                bool hasError = req.result != UnityWebRequest.Result.Success;
                if (hasError)
                {
                    Debug.Log($"[Farmer's Companion] Update check skipped (offline or repo not yet created): {req.error}");
                    yield break;
                }

                string json = req.downloadHandler?.text;
                if (string.IsNullOrEmpty(json)) yield break;

                VersionManifest manifest = null;
                try
                {
                    manifest = JsonUtility.FromJson<VersionManifest>(json);
                }
                catch (Exception ex)
                {
                    Debug.LogWarning($"[Farmer's Companion] Failed to parse version.json: {ex.Message}");
                }

                if (manifest != null && !string.IsNullOrEmpty(manifest.version))
                {
                    LatestVersion = manifest.version.Trim();
                    if (!string.IsNullOrEmpty(manifest.downloadUrl))
                    {
                        DownloadUrl = manifest.downloadUrl.Trim();
                    }
                    if (!string.IsNullOrEmpty(manifest.notes))
                    {
                        ReleaseNotes = manifest.notes.Trim();
                    }

                    if (IsNewerVersion(PluginInfo.PLUGIN_VERSION, LatestVersion))
                    {
                        IsUpdateAvailable = true;
                        CropIndicatorManager.Instance?.ShowNotification($"✨ Update v{LatestVersion} available! Open [F1] menu to download.");
                        Debug.Log($"[Farmer's Companion] A new update (v{LatestVersion}) is available! URL: {DownloadUrl}");
                    }
                    else
                    {
                        IsUpdateAvailable = false;
                        CropIndicatorManager.Instance?.ShowNotification($"✅ Farmer's Companion is up to date (v{PluginInfo.PLUGIN_VERSION})!");
                        Debug.Log($"[Farmer's Companion] Up to date (v{PluginInfo.PLUGIN_VERSION}).");
                    }
                }
            }
        }

        public static bool IsNewerVersion(string currentVerStr, string remoteVerStr)
        {
            try
            {
                string cleanCurrent = currentVerStr.TrimStart('v', 'V').Trim();
                string cleanRemote = remoteVerStr.TrimStart('v', 'V').Trim();

                Version cur = NormalizeVersion(cleanCurrent);
                Version rem = NormalizeVersion(cleanRemote);

                return rem > cur;
            }
            catch
            {
                return false;
            }
        }

        private static Version NormalizeVersion(string ver)
        {
            var parts = ver.Split('.');
            if (parts.Length == 1) return new Version(int.Parse(parts[0]), 0, 0);
            if (parts.Length == 2) return new Version(int.Parse(parts[0]), int.Parse(parts[1]), 0);
            if (parts.Length == 3) return new Version(int.Parse(parts[0]), int.Parse(parts[1]), int.Parse(parts[2]));
            return new Version(ver);
        }
    }
    // ============================================================================
    // [END] MODULE: IN-GAME UPDATE CHECKER & NOTIFICATION ENGINE
    // ============================================================================
    #endregion
}
