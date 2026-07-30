#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using UnityEditor;
using UnityEditor.Build;
using UnityEditor.Build.Reporting;
using UnityEditor.Compilation;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Networking;
using UnityEngine.SceneManagement;

namespace QLDATN.ProjectTracker
{
    [InitializeOnLoad]
    public static class SceneTracker
    {
        private const string TrackerUrlPreference = "QLDATN_PROJECT_TRACKER_URL";
        private const string DeviceSecretPreference = "QLDATN_PROJECT_TRACKER_TOKEN";
        private const string DeviceKeyIdPreference = "QLDATN_PROJECT_TRACKER_KEY_ID";
        private const string DeviceIdPreference = "QLDATN_PROJECT_TRACKER_DEVICE_ID";
        private const double HeartbeatSeconds = 30.0;
        private const double GitRefreshSeconds = 60.0;
        private const string ClientVersion = "qldatn-unity-3.0.0";

        private static readonly string SessionId = Guid.NewGuid().ToString("N");
        private static readonly Queue<StatusPayload> PendingPayloads = new Queue<StatusPayload>();
        private static string _scene = "";
        private static string _branch = "";
        private static string _revision = "";
        private static int _uncommittedFiles;
        private static bool _isDirty;
        private static bool _lastKnownDirty;
        private static bool _isSending;
        private static bool _isBuilding;
        private static int _pendingAssetChanges;
        private static double _assetFlushAt;
        private static double _lastHeartbeat;
        private static double _lastGitRefresh = -GitRefreshSeconds;
        private static long _lastSequence;

        static SceneTracker()
        {
            EditorApplication.update += OnEditorUpdate;
            EditorApplication.playModeStateChanged += OnPlayModeStateChanged;
            EditorSceneManager.sceneOpened += OnSceneChanged;
            EditorSceneManager.sceneClosed += OnSceneClosed;
            EditorSceneManager.sceneSaved += OnSceneSaved;
            CompilationPipeline.compilationStarted += OnCompilationStarted;
            CompilationPipeline.compilationFinished += OnCompilationFinished;
            EditorApplication.quitting += OnEditorQuitting;

            EditorApplication.delayCall += () =>
            {
                RefreshState(true);
                _lastKnownDirty = _isDirty;
                QueueSend(CurrentStatus());
            };
        }

        private static bool IsConfigured()
        {
            return !string.IsNullOrEmpty(EditorPrefs.GetString(TrackerUrlPreference))
                && !string.IsNullOrEmpty(EditorPrefs.GetString(DeviceSecretPreference))
                && !string.IsNullOrEmpty(EditorPrefs.GetString(DeviceKeyIdPreference))
                && !string.IsNullOrEmpty(EditorPrefs.GetString(DeviceIdPreference));
        }

        private static void OnEditorUpdate()
        {
            if (!IsConfigured()) return;
            var now = EditorApplication.timeSinceStartup;
            if (_pendingAssetChanges > 0 && now >= _assetFlushAt)
            {
                var changedAssets = _pendingAssetChanges;
                _pendingAssetChanges = 0;
                RefreshState(false);
                QueueSend(CurrentStatus(), "ASSETS_CHANGED", changedAssets);
            }
            var active = EditorSceneManager.GetActiveScene();
            var dirtyNow = active.IsValid() && active.isDirty;
            if (dirtyNow != _lastKnownDirty)
            {
                RefreshState(false);
                _lastKnownDirty = _isDirty;
                _lastHeartbeat = EditorApplication.timeSinceStartup;
                QueueSend(CurrentStatus());
            }

            if (now - _lastHeartbeat < HeartbeatSeconds) return;
            RefreshState(now - _lastGitRefresh >= GitRefreshSeconds);
            _lastKnownDirty = _isDirty;
            _lastHeartbeat = now;
            QueueSend(CurrentStatus());
        }

        private static void OnSceneChanged(Scene _sceneValue, OpenSceneMode _mode)
        {
            RefreshState(false);
            _lastKnownDirty = _isDirty;
            QueueSend(CurrentStatus());
        }

        private static void OnSceneClosed(Scene _sceneValue)
        {
            EditorApplication.delayCall += () =>
            {
                RefreshState(false);
                _lastKnownDirty = _isDirty;
                QueueSend(CurrentStatus());
            };
        }

        private static void OnSceneSaved(Scene _sceneValue)
        {
            RefreshState(false);
            _lastKnownDirty = _isDirty;
            QueueSend(CurrentStatus(), "SCENE_SAVED", 1);
        }

        private static void OnPlayModeStateChanged(PlayModeStateChange state)
        {
            RefreshState(false);
            if (state == PlayModeStateChange.EnteredPlayMode)
            {
                QueueSend("PLAYING", "PLAY_MODE_ENTERED");
            }
            else if (state == PlayModeStateChange.ExitingPlayMode)
            {
                QueueSend(CurrentStatus(), "PLAY_MODE_EXITED");
            }
        }

        private static void OnCompilationStarted(object _context)
        {
            QueueSend("COMPILING", "COMPILATION_STARTED");
        }

        private static void OnCompilationFinished(object _context)
        {
            RefreshState(true);
            QueueSend(CurrentStatus(), "COMPILATION_FINISHED");
        }

        private static void RefreshState(bool refreshGit)
        {
            var active = EditorSceneManager.GetActiveScene();
            _isDirty = active.IsValid() && active.isDirty;
            _scene = !active.IsValid()
                ? ""
                : !string.IsNullOrEmpty(active.path)
                    ? active.path
                    : !string.IsNullOrEmpty(active.name)
                        ? active.name
                        : "(Untitled Scene)";
            if (refreshGit) RefreshGitState();
        }

        private static void RefreshGitState()
        {
            _lastGitRefresh = EditorApplication.timeSinceStartup;
            var projectDirectory = Directory.GetParent(Application.dataPath)?.FullName;
            if (string.IsNullOrEmpty(projectDirectory)) return;
            var previousRevision = _revision;
            _branch = RunGit(projectDirectory, "rev-parse --abbrev-ref HEAD", 120);
            _revision = RunGit(projectDirectory, "rev-parse --short=12 HEAD", 64);
            var changes = RunGit(projectDirectory, "status --porcelain", 200000);
            _uncommittedFiles = string.IsNullOrEmpty(changes)
                ? 0
                : changes.Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries).Length;
            if (
                !string.IsNullOrEmpty(previousRevision)
                && !string.IsNullOrEmpty(_revision)
                && previousRevision != _revision
            )
            {
                QueueSend(CurrentStatus(), "REVISION_CHANGED", 1, 0, true, _revision);
            }
        }

        private static string RunGit(string workingDirectory, string arguments, int maxLength)
        {
            try
            {
                var startInfo = new System.Diagnostics.ProcessStartInfo
                {
                    FileName = "git",
                    Arguments = arguments,
                    WorkingDirectory = workingDirectory,
                    UseShellExecute = false,
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    CreateNoWindow = true
                };
                using var process = System.Diagnostics.Process.Start(startInfo);
                if (process == null) return "";
                var output = process.StandardOutput.ReadToEnd().Trim();
                if (!process.WaitForExit(2000))
                {
                    process.Kill();
                    return "";
                }
                return output.Length <= maxLength ? output : output.Substring(0, maxLength);
            }
            catch
            {
                return "";
            }
        }

        private static string CurrentStatus()
        {
            if (_isBuilding) return "BUILDING";
            if (EditorApplication.isCompiling) return "COMPILING";
            if (!IsEditorFocused()) return "BACKGROUND";
            if (EditorApplication.isPlayingOrWillChangePlaymode) return "PLAYING";
            if (_isDirty) return "EDITING";
            if (_uncommittedFiles > 0) return "UNCOMMITTED";
            return string.IsNullOrEmpty(_scene) ? "SAFE" : "VIEWING";
        }

        private static string CurrentActivity()
        {
            if (_isBuilding) return "Đang build";
            if (EditorApplication.isCompiling) return "Đang biên dịch";
            if (!IsEditorFocused()) return "Unity Editor chạy nền";
            if (EditorApplication.isPlayingOrWillChangePlaymode) return "Đang chạy Play Mode";
            if (_isDirty) return "Đang chỉnh sửa scene";
            return string.IsNullOrEmpty(_scene) ? "Unity Editor" : "Đang xem scene";
        }

        private static string RuntimeMode()
        {
            if (_isBuilding) return "BUILD";
            if (EditorApplication.isCompiling) return "COMPILE";
            if (!IsEditorFocused()) return "BACKGROUND";
            if (EditorApplication.isPlayingOrWillChangePlaymode) return "PLAY";
            if (EditorApplication.isPaused) return "PAUSED";
            return "EDIT";
        }

        private static bool IsEditorFocused()
        {
#if UNITY_2022_2_OR_NEWER
            return EditorApplication.isFocused;
#else
            return true;
#endif
        }

        private static StatusPayload CreatePayload(
            string status,
            string eventType = null,
            int eventCount = 1,
            long eventDurationMs = 0,
            bool eventSuccess = false,
            string eventLabel = null
        )
        {
            return new StatusPayload
            {
                status = status,
                context = _scene,
                activity = CurrentActivity(),
                isDirty = _isDirty,
                branch = _branch,
                revision = _revision,
                uncommittedFiles = _uncommittedFiles,
                runtimeMode = RuntimeMode(),
                sessionId = SessionId,
                clientVersion = ClientVersion,
                deviceName = SystemInfo.deviceName,
                environment = new EnvironmentPayload
                {
                    engine = Application.unityVersion,
                    operatingSystem = SystemInfo.operatingSystem,
                    target = EditorUserBuildSettings.activeBuildTarget.ToString(),
                    processorCount = SystemInfo.processorCount,
                    memoryMb = SystemInfo.systemMemorySize,
                    editorFocused = IsEditorFocused()
                },
                eventInfo = string.IsNullOrEmpty(eventType)
                    ? null
                    : new EventPayload
                    {
                        type = eventType,
                        count = eventCount,
                        durationMs = eventDurationMs,
                        success = eventSuccess,
                        label = eventLabel
                    }
            };
        }

        private static void QueueSend(
            string status,
            string eventType = null,
            int eventCount = 1,
            long eventDurationMs = 0,
            bool eventSuccess = false,
            string eventLabel = null
        )
        {
            if (!IsConfigured()) return;
            var payload = CreatePayload(
                status,
                eventType,
                eventCount,
                eventDurationMs,
                eventSuccess,
                eventLabel
            );
            lock (PendingPayloads)
            {
                while (PendingPayloads.Count >= 100) PendingPayloads.Dequeue();
                PendingPayloads.Enqueue(payload);
            }
            _ = FlushQueueAsync();
        }

        private static async Task FlushQueueAsync()
        {
            if (_isSending) return;
            _isSending = true;
            try
            {
                while (true)
                {
                    StatusPayload payload;
                    lock (PendingPayloads)
                    {
                        if (PendingPayloads.Count == 0) break;
                        payload = PendingPayloads.Dequeue();
                    }
                    var json = JsonUtility.ToJson(payload);
                    var body = Encoding.UTF8.GetBytes(json);
                    var timestamp = UnixTimeMilliseconds().ToString();
                    var sequence = NextSequence().ToString();
                    var secret = EditorPrefs.GetString(DeviceSecretPreference);
                    var signature = SignRequest(
                        secret,
                        timestamp + "." + sequence + "." + json
                    );
                    using var request = new UnityWebRequest(
                        EditorPrefs.GetString(TrackerUrlPreference),
                        "POST"
                    );
                    request.uploadHandler = new UploadHandlerRaw(body);
                    request.downloadHandler = new DownloadHandlerBuffer();
                    request.SetRequestHeader("Content-Type", "application/json");
                    request.SetRequestHeader(
                        "Authorization",
                        "Device " + EditorPrefs.GetString(DeviceKeyIdPreference)
                    );
                    request.SetRequestHeader("X-Tracker-Timestamp", timestamp);
                    request.SetRequestHeader("X-Tracker-Sequence", sequence);
                    request.SetRequestHeader(
                        "X-Tracker-Device",
                        EditorPrefs.GetString(DeviceIdPreference)
                    );
                    request.SetRequestHeader("X-Tracker-Signature", signature);
                    var operation = request.SendWebRequest();
                    while (!operation.isDone) await Task.Delay(50);
                    if (request.result != UnityWebRequest.Result.Success)
                    {
                        Debug.LogWarning("[QLDATN Tracker] Không thể đồng bộ: " + request.error);
                    }
                }
            }
            catch (Exception error)
            {
                Debug.LogWarning("[QLDATN Tracker] " + error.Message);
            }
            finally
            {
                _isSending = false;
            }
        }

        public static void ReportAssetChanges(int count)
        {
            if (count <= 0) return;
            _pendingAssetChanges += count;
            _assetFlushAt = EditorApplication.timeSinceStartup + 2.0;
        }

        public static void ReportBuildStarted(string target)
        {
            _isBuilding = true;
            RefreshState(true);
            QueueSend("BUILDING", "BUILD_STARTED", 1, 0, false, target);
        }

        public static void ReportBuildCompleted(
            string target,
            long durationMs,
            bool successful
        )
        {
            _isBuilding = false;
            RefreshState(true);
            QueueSend(CurrentStatus(), "BUILD_COMPLETED", 1, durationMs, successful, target);
        }

        private static void OnEditorQuitting()
        {
            if (!IsConfigured()) return;
            try
            {
                var payload = CreatePayload("OFFLINE");
                var body = JsonUtility.ToJson(payload);
                var timestamp = UnixTimeMilliseconds().ToString();
                var sequence = NextSequence().ToString();
                var secret = EditorPrefs.GetString(DeviceSecretPreference);
                using var client = new WebClient();
                client.Headers[HttpRequestHeader.ContentType] = "application/json";
                client.Headers[HttpRequestHeader.Authorization] =
                    "Device " + EditorPrefs.GetString(DeviceKeyIdPreference);
                client.Headers["X-Tracker-Timestamp"] = timestamp;
                client.Headers["X-Tracker-Sequence"] = sequence;
                client.Headers["X-Tracker-Device"] =
                    EditorPrefs.GetString(DeviceIdPreference);
                client.Headers["X-Tracker-Signature"] = SignRequest(
                    secret,
                    timestamp + "." + sequence + "." + body
                );
                client.UploadString(
                    EditorPrefs.GetString(TrackerUrlPreference),
                    "POST",
                    body
                );
            }
            catch
            {
                // Server tự chuyển offline nếu Unity đóng trước khi gửi xong.
            }
        }

        private static long UnixTimeMilliseconds()
        {
            return (long)(DateTime.UtcNow - new DateTime(
                1970,
                1,
                1,
                0,
                0,
                0,
                DateTimeKind.Utc
            )).TotalMilliseconds;
        }

        private static long NextSequence()
        {
            var candidate = UnixTimeMilliseconds() * 1000;
            _lastSequence = Math.Max(_lastSequence + 1, candidate);
            return _lastSequence;
        }

        private static string SignRequest(string secret, string message)
        {
            using var hmac = new HMACSHA256(Encoding.UTF8.GetBytes(secret));
            return Convert.ToBase64String(
                hmac.ComputeHash(Encoding.UTF8.GetBytes(message))
            ).TrimEnd('=').Replace('+', '-').Replace('/', '_');
        }

        [Serializable]
        private class StatusPayload
        {
            public string status;
            public string context;
            public string activity;
            public bool isDirty;
            public string branch;
            public string revision;
            public int uncommittedFiles;
            public string runtimeMode;
            public string sessionId;
            public string clientVersion;
            public string deviceName;
            public EnvironmentPayload environment;
            public EventPayload eventInfo;
        }

        [Serializable]
        private class EnvironmentPayload
        {
            public string engine;
            public string operatingSystem;
            public string target;
            public int processorCount;
            public int memoryMb;
            public bool editorFocused;
        }

        [Serializable]
        private class EventPayload
        {
            public string type;
            public int count;
            public long durationMs;
            public bool success;
            public string label;
        }
    }

    public class QLDATNAssetTracker : AssetPostprocessor
    {
        private static void OnPostprocessAllAssets(
            string[] importedAssets,
            string[] deletedAssets,
            string[] movedAssets,
            string[] _movedFromAssetPaths
        )
        {
            SceneTracker.ReportAssetChanges(
                importedAssets.Length + deletedAssets.Length + movedAssets.Length
            );
        }
    }

    public class QLDATNBuildTracker : IPreprocessBuildWithReport, IPostprocessBuildWithReport
    {
        public int callbackOrder => 10000;

        public void OnPreprocessBuild(BuildReport report)
        {
            SceneTracker.ReportBuildStarted(report.summary.platform.ToString());
        }

        public void OnPostprocessBuild(BuildReport report)
        {
            SceneTracker.ReportBuildCompleted(
                report.summary.platform.ToString(),
                (long)report.summary.totalTime.TotalMilliseconds,
                report.summary.result == BuildResult.Succeeded
            );
        }
    }
}
#endif
