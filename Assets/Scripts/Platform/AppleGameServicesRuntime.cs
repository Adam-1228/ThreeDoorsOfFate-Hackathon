using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.Scripting;

namespace ThreeDoorsOfFate.Platform
{
    [Preserve]
    public sealed class AppleGameServicesRuntime : MonoBehaviour
    {
        private const string RuntimeObjectName = "ThreeDoorsOfFate.AppleGameServices";
        private const float SyncIntervalSeconds = 60f;
        private const int NativeTimeoutMilliseconds = 20000;
        private const int AuthenticationTimeoutMilliseconds = 180000;
        private const string ReportedScoreKey =
            PlayerPrefsProgressStore.ProductionPrefix + "GameCenter.Reported.EndlessScore";
        private const string ReportedAchievementPrefix =
            PlayerPrefsProgressStore.ProductionPrefix + "GameCenter.Reported.Achievement.";

        private static AppleGameServicesRuntime instance;

        private readonly Dictionary<int, TaskCompletionSource<NativeCloudMessage>> pendingRequests =
            new();
        private readonly SemaphoreSlim cloudOperationGate = new(1, 1);

        private bool isAuthenticated;
        private bool syncLoopRunning;
        private bool syncRequested;
        private int nextRequestId = 1;
        private float nextSyncAt;

        public static void SetAccessPointVisible(bool visible)
        {
            if (NativeCloudBridge.IsAvailable)
            {
                NativeCloudBridge.SetAccessPointVisible(visible);
            }
        }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        private static void Bootstrap()
        {
#if UNITY_IOS && !UNITY_EDITOR
            if (instance != null)
            {
                return;
            }

            GameObject runtimeObject = new(RuntimeObjectName);
            DontDestroyOnLoad(runtimeObject);
            instance = runtimeObject.AddComponent<AppleGameServicesRuntime>();
#endif
        }

        private void Awake()
        {
            if (instance != null && instance != this)
            {
                Destroy(gameObject);
                return;
            }

            instance = this;
            gameObject.name = RuntimeObjectName;
            DontDestroyOnLoad(gameObject);
        }

        private async void Start()
        {
            if (!NativeCloudBridge.IsAvailable)
            {
                return;
            }

            NativeCloudBridge.Initialize(RuntimeObjectName);
            try
            {
                NativeCloudMessage authentication = await SendNativeRequestAsync(
                    NativeCloudBridge.Authenticate,
                    AuthenticationTimeoutMilliseconds);
                EnsureSuccess(authentication);
                isAuthenticated = true;
                nextSyncAt = Time.realtimeSinceStartup + SyncIntervalSeconds;
                RequestSync();
            }
            catch (Exception exception)
            {
                isAuthenticated = false;
                Debug.LogWarning(
                    "Game Center authentication was not completed. " +
                    "Local progress remains available. " + exception.Message);
            }
        }

        private void Update()
        {
            if (!isAuthenticated || Time.realtimeSinceStartup < nextSyncAt)
            {
                return;
            }

            nextSyncAt = Time.realtimeSinceStartup + SyncIntervalSeconds;
            RequestSync();
        }

        private void OnApplicationPause(bool paused)
        {
            if (isAuthenticated)
            {
                RequestSync();
            }
        }

        private void OnDestroy()
        {
            if (instance == this)
            {
                instance = null;
            }

            InvalidOperationException exception = new("Apple game services stopped.");
            foreach (TaskCompletionSource<NativeCloudMessage> request in pendingRequests.Values)
            {
                request.TrySetException(exception);
            }

            pendingRequests.Clear();
        }

        private void RequestSync()
        {
            syncRequested = true;
            if (!syncLoopRunning)
            {
                _ = RunSyncLoopAsync();
            }
        }

        private async Task RunSyncLoopAsync()
        {
            syncLoopRunning = true;
            try
            {
                while (syncRequested && isAuthenticated && this != null)
                {
                    syncRequested = false;
                    try
                    {
                        await SynchronizeCloudProgressAsync();
                        await ReportGameCenterProgressAsync();
                    }
                    catch (Exception exception)
                    {
                        Debug.LogWarning(
                            $"Apple game services sync was deferred; local progress is unchanged. " +
                            exception.Message);
                    }
                }
            }
            finally
            {
                syncLoopRunning = false;
            }
        }

        private async Task SynchronizeCloudProgressAsync()
        {
            await cloudOperationGate.WaitAsync();
            try
            {
                long now = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
                string localJson = PlayerProgressSyncState.CaptureLocalJson(
                    PlayerPrefsProgressStore.ProductionPrefix,
                    now);
                NativeCloudMessage fetch = await FetchCloudAsync(AppleGameServices.CloudSaveName);
                EnsureSuccess(fetch);

                NativeCloudSave[] matchingSaves = (fetch.saves ?? Array.Empty<NativeCloudSave>())
                    .Where(save => save != null
                        && string.Equals(
                            save.name,
                            AppleGameServices.CloudSaveName,
                            StringComparison.Ordinal))
                    .ToArray();
                if (matchingSaves.Length == 0)
                {
                    EnsureSuccess(await SaveCloudAsync(
                        AppleGameServices.CloudSaveName,
                        localJson));
                    return;
                }

                bool contentChanged = false;
                bool invalidRemoteFound = false;
                int validRemoteCount = 0;
                string mergedJson = localJson;
                foreach (NativeCloudSave save in matchingSaves)
                {
                    if (!TryDecodeSnapshot(save.data, out string cloudJson))
                    {
                        invalidRemoteFound = true;
                        continue;
                    }

                    try
                    {
                        string localHash = PlayerProgressFingerprint.ComputeContentHash(mergedJson);
                        string cloudHash = PlayerProgressFingerprint.ComputeContentHash(cloudJson);
                        validRemoteCount++;
                        if (string.Equals(localHash, cloudHash, StringComparison.Ordinal))
                        {
                            PlayerProgressSyncState.AdoptRemoteMetadata(
                                PlayerPrefsProgressStore.ProductionPrefix,
                                cloudJson);
                            mergedJson = PlayerProgressSyncState.CaptureLocalJson(
                                PlayerPrefsProgressStore.ProductionPrefix,
                                now);
                            continue;
                        }

                        mergedJson = PlayerProgressSyncState.MergeAndApplyJson(
                            PlayerPrefsProgressStore.ProductionPrefix,
                            mergedJson,
                            cloudJson,
                            now);
                        contentChanged = true;
                    }
                    catch (Exception exception)
                    {
                        invalidRemoteFound = true;
                        Debug.LogWarning(
                            "An invalid iCloud progress snapshot was ignored. " + exception.Message);
                    }
                }

                if (validRemoteCount == 0 && invalidRemoteFound)
                {
                    throw new InvalidOperationException(
                        "No valid iCloud progress snapshot was available.");
                }

                if (contentChanged && !invalidRemoteFound)
                {
                    EnsureSuccess(await SaveCloudAsync(
                        AppleGameServices.CloudSaveName,
                        mergedJson));
                }
            }
            finally
            {
                cloudOperationGate.Release();
            }
        }

        private async Task ResolveConflictsAsync(NativeCloudMessage conflict)
        {
            await cloudOperationGate.WaitAsync();
            try
            {
                long now = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
                string mergedJson = PlayerProgressSyncState.CaptureLocalJson(
                    PlayerPrefsProgressStore.ProductionPrefix,
                    now);
                int validRemoteCount = 0;

                foreach (NativeCloudSave save in conflict.saves ?? Array.Empty<NativeCloudSave>())
                {
                    if (save == null
                        || !string.Equals(
                            save.name,
                            AppleGameServices.CloudSaveName,
                            StringComparison.Ordinal)
                        || !TryDecodeSnapshot(save.data, out string cloudJson))
                    {
                        continue;
                    }

                    try
                    {
                        string localHash = PlayerProgressFingerprint.ComputeContentHash(mergedJson);
                        string cloudHash = PlayerProgressFingerprint.ComputeContentHash(cloudJson);
                        validRemoteCount++;
                        if (string.Equals(localHash, cloudHash, StringComparison.Ordinal))
                        {
                            PlayerProgressSyncState.AdoptRemoteMetadata(
                                PlayerPrefsProgressStore.ProductionPrefix,
                                cloudJson);
                            mergedJson = PlayerProgressSyncState.CaptureLocalJson(
                                PlayerPrefsProgressStore.ProductionPrefix,
                                now);
                        }
                        else
                        {
                            mergedJson = PlayerProgressSyncState.MergeAndApplyJson(
                                PlayerPrefsProgressStore.ProductionPrefix,
                                mergedJson,
                                cloudJson,
                                now);
                        }
                    }
                    catch (Exception exception)
                    {
                        Debug.LogWarning(
                            "An invalid conflicting iCloud snapshot was ignored. " +
                            exception.Message);
                    }
                }

                if (validRemoteCount == 0)
                {
                    throw new InvalidOperationException(
                        "The iCloud conflict contained no valid progress snapshot.");
                }

                EnsureSuccess(await ResolveCloudAsync(
                    AppleGameServices.CloudSaveName,
                    mergedJson));
            }
            catch (Exception exception)
            {
                Debug.LogWarning(
                    "The iCloud save conflict remains pending; local progress is unchanged. " +
                    exception.Message);
            }
            finally
            {
                cloudOperationGate.Release();
            }
        }

        private async Task ReportGameCenterProgressAsync()
        {
            GameCenterProgressReport report = AppleGameServices.CaptureGameCenterProgress(
                PlayerPrefsProgressStore.ProductionPrefix);
            int previousScore = PlayerPrefs.GetInt(ReportedScoreKey, 0);
            if (report.endlessScore > previousScore)
            {
                NativeCloudMessage scoreReport = await SendNativeRequestAsync(requestId =>
                    NativeCloudBridge.ReportScore(
                        requestId,
                        AppleGameServices.EndlessLeaderboardId,
                        report.endlessScore));
                if (scoreReport.success)
                {
                    PlayerPrefs.SetInt(ReportedScoreKey, (int)report.endlessScore);
                }
            }

            foreach (string achievementId in report.completedAchievementIds)
            {
                string reportedKey = ReportedAchievementPrefix + achievementId;
                if (PlayerPrefs.GetInt(reportedKey, 0) != 0)
                {
                    continue;
                }

                NativeCloudMessage achievementReport = await SendNativeRequestAsync(requestId =>
                    NativeCloudBridge.ReportAchievement(requestId, achievementId));
                if (achievementReport.success)
                {
                    PlayerPrefs.SetInt(reportedKey, 1);
                }
            }

            PlayerPrefs.Save();
        }

        private Task<NativeCloudMessage> FetchCloudAsync(string name)
        {
            return SendNativeRequestAsync(requestId =>
                NativeCloudBridge.Fetch(requestId, name));
        }

        private Task<NativeCloudMessage> SaveCloudAsync(string name, string json)
        {
            string base64 = Convert.ToBase64String(Encoding.UTF8.GetBytes(json));
            return SendNativeRequestAsync(requestId =>
                NativeCloudBridge.Save(requestId, name, base64));
        }

        private Task<NativeCloudMessage> ResolveCloudAsync(string name, string json)
        {
            string base64 = Convert.ToBase64String(Encoding.UTF8.GetBytes(json));
            return SendNativeRequestAsync(requestId =>
                NativeCloudBridge.Resolve(requestId, name, base64));
        }

        private async Task<NativeCloudMessage> SendNativeRequestAsync(
            Action<int> dispatch,
            int timeoutMilliseconds = NativeTimeoutMilliseconds)
        {
            int requestId = nextRequestId++;
            if (nextRequestId <= 0)
            {
                nextRequestId = 1;
            }

            TaskCompletionSource<NativeCloudMessage> completion = new();
            pendingRequests.Add(requestId, completion);
            try
            {
                dispatch(requestId);
                Task finished = await Task.WhenAny(
                    completion.Task,
                    Task.Delay(timeoutMilliseconds));
                if (finished != completion.Task)
                {
                    throw new TimeoutException("The iCloud request timed out.");
                }

                return await completion.Task;
            }
            finally
            {
                pendingRequests.Remove(requestId);
            }
        }

        [Preserve]
        public void OnNativeCloudMessage(string json)
        {
            NativeCloudMessage message;
            try
            {
                message = JsonUtility.FromJson<NativeCloudMessage>(json);
            }
            catch (ArgumentException exception)
            {
                Debug.LogWarning("Ignored malformed iCloud callback JSON. " + exception.Message);
                return;
            }

            if (message == null || string.IsNullOrWhiteSpace(message.kind))
            {
                Debug.LogWarning("Ignored an empty iCloud callback.");
                return;
            }

            if (string.Equals(message.kind, "authenticate", StringComparison.Ordinal)
                && message.success)
            {
                bool wasAuthenticated = isAuthenticated;
                isAuthenticated = true;
                nextSyncAt = Time.realtimeSinceStartup + SyncIntervalSeconds;
                if (!wasAuthenticated)
                {
                    RequestSync();
                }
            }

            if (string.Equals(message.kind, "modified", StringComparison.Ordinal))
            {
                RequestSync();
                return;
            }

            if (string.Equals(message.kind, "conflict", StringComparison.Ordinal))
            {
                _ = ResolveConflictsAsync(message);
                return;
            }

            if (pendingRequests.TryGetValue(
                message.requestId,
                out TaskCompletionSource<NativeCloudMessage> request))
            {
                request.TrySetResult(message);
            }
        }

        private static bool TryDecodeSnapshot(string base64, out string json)
        {
            json = null;
            if (string.IsNullOrWhiteSpace(base64))
            {
                return false;
            }

            try
            {
                json = Encoding.UTF8.GetString(Convert.FromBase64String(base64));
                return !string.IsNullOrWhiteSpace(json);
            }
            catch (FormatException)
            {
                return false;
            }
        }

        private static void EnsureSuccess(NativeCloudMessage message)
        {
            if (message == null || !message.success)
            {
                throw new InvalidOperationException(
                    string.IsNullOrWhiteSpace(message?.error)
                        ? "The iCloud operation failed."
                        : message.error);
            }
        }

        [Serializable]
        private sealed class NativeCloudMessage
        {
            public string kind;
            public int requestId;
            public bool success;
            public string error;
            public NativeCloudSave[] saves = Array.Empty<NativeCloudSave>();
        }

        [Serializable]
        private sealed class NativeCloudSave
        {
            public string name;
            public string data;
            public long modifiedAtUnixSeconds;
        }

        private static class NativeCloudBridge
        {
#if UNITY_IOS && !UNITY_EDITOR
            public static bool IsAvailable => true;

            [DllImport("__Internal")]
            private static extern void TDOF_CloudInitialize(string receiverGameObject);

            [DllImport("__Internal")]
            private static extern void TDOF_GameCenterAuthenticate(int requestId);

            [DllImport("__Internal")]
            private static extern void TDOF_GameCenterReportScore(
                int requestId,
                string leaderboardId,
                long score);

            [DllImport("__Internal")]
            private static extern void TDOF_GameCenterReportAchievement(
                int requestId,
                string achievementId);

            [DllImport("__Internal")]
            private static extern void TDOF_GameCenterSetAccessPointVisible(int visible);

            [DllImport("__Internal")]
            private static extern void TDOF_CloudFetch(int requestId, string name);

            [DllImport("__Internal")]
            private static extern void TDOF_CloudSave(
                int requestId,
                string name,
                string base64Data);

            [DllImport("__Internal")]
            private static extern void TDOF_CloudResolve(
                int requestId,
                string name,
                string base64Data);

            public static void Initialize(string receiverGameObject) =>
                TDOF_CloudInitialize(receiverGameObject);

            public static void Authenticate(int requestId) =>
                TDOF_GameCenterAuthenticate(requestId);

            public static void ReportScore(int requestId, string leaderboardId, long score) =>
                TDOF_GameCenterReportScore(requestId, leaderboardId, score);

            public static void ReportAchievement(int requestId, string achievementId) =>
                TDOF_GameCenterReportAchievement(requestId, achievementId);

            public static void SetAccessPointVisible(bool visible) =>
                TDOF_GameCenterSetAccessPointVisible(visible ? 1 : 0);

            public static void Fetch(int requestId, string name) =>
                TDOF_CloudFetch(requestId, name);

            public static void Save(int requestId, string name, string base64Data) =>
                TDOF_CloudSave(requestId, name, base64Data);

            public static void Resolve(int requestId, string name, string base64Data) =>
                TDOF_CloudResolve(requestId, name, base64Data);
#else
            public static bool IsAvailable => false;

            public static void Initialize(string receiverGameObject) =>
                throw new PlatformNotSupportedException();

            public static void Authenticate(int requestId) =>
                throw new PlatformNotSupportedException();

            public static void ReportScore(int requestId, string leaderboardId, long score) =>
                throw new PlatformNotSupportedException();

            public static void ReportAchievement(int requestId, string achievementId) =>
                throw new PlatformNotSupportedException();

            public static void SetAccessPointVisible(bool visible)
            {
            }

            public static void Fetch(int requestId, string name) =>
                throw new PlatformNotSupportedException();

            public static void Save(int requestId, string name, string base64Data) =>
                throw new PlatformNotSupportedException();

            public static void Resolve(int requestId, string name, string base64Data) =>
                throw new PlatformNotSupportedException();
#endif
        }
    }
}
