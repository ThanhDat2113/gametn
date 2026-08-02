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
        private const double RecentSourceActivitySeconds = 90.0;
        private const string ClientVersion = "qldatn-unity-3.3.1";
        private const string PendingUpdatePreference = "QLDATN_PROJECT_TRACKER_PENDING_UPDATE";
        private const int MaximumOfflinePayloads = 50;
        private static readonly double[] RetryDelaysSeconds = { 2.0, 5.0, 15.0 };

        private static readonly string SessionIdPath = Path.Combine(
            "Library",
            "QLDATNTracker",
            "session-id.txt"
        );
        private static readonly string SessionId = LoadOrCreateSessionId();
        private static readonly Queue<StatusPayload> PendingPayloads = new Queue<StatusPayload>();
        private static StatusPayload _latestHeartbeat;
        private static int _retryAttempt;
        private static double _nextRetryAt;
        private static readonly string OfflineQueuePath = Path.Combine(
            "Library",
            "QLDATNTracker",
            "offline-queue.json"
        );
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
        private static double _lastSourceActivity = -RecentSourceActivitySeconds;
        private static long _lastSequence;
        private static bool _updateCompilationFailed;

        static SceneTracker()
        {
            RestorePendingPayloads();
            EditorApplication.update += OnEditorUpdate;
            EditorApplication.playModeStateChanged += OnPlayModeStateChanged;
            EditorSceneManager.sceneOpened += OnSceneChanged;
            EditorSceneManager.sceneClosed += OnSceneClosed;
            EditorSceneManager.sceneSaved += OnSceneSaved;
            CompilationPipeline.compilationStarted += OnCompilationStarted;
            CompilationPipeline.compilationFinished += OnCompilationFinished;
            CompilationPipeline.assemblyCompilationFinished += OnAssemblyCompilationFinished;
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
                _pendingAssetChanges = 0;
                RefreshState(false);
                // Server chỉ ghi một lượt sự kiện cho mỗi request; không tin số lượng do client khai.
                QueueSend(CurrentStatus(), "ASSETS_CHANGED", 1);
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
            else if (state == PlayModeStateChange.EnteredEditMode)
            {
                // Đợi Unity trở lại Edit Mode hoàn toàn để heartbeat thoát Play
                // không tiếp tục mang trạng thái PLAYING thêm một chu kỳ.
                QueueSend(CurrentStatus(), "PLAY_MODE_EXITED");
            }
        }

        private static void OnCompilationStarted(object _context)
        {
            QueueSend("COMPILING", "COMPILATION_STARTED");
        }

        private static void OnCompilationFinished(object _context)
        {
            FinalizePendingUpdate();
            RefreshState(true);
            QueueSend(CurrentStatus(), "COMPILATION_FINISHED");
        }

        private static void OnAssemblyCompilationFinished(
            string _assemblyPath,
            CompilerMessage[] messages
        )
        {
            if (string.IsNullOrEmpty(EditorPrefs.GetString(PendingUpdatePreference))) return;
            foreach (var message in messages)
            {
                if (message.type == CompilerMessageType.Error)
                {
                    _updateCompilationFailed = true;
                    break;
                }
            }
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
            if (EditorApplication.isPlayingOrWillChangePlaymode) return "PLAYING";
            if (_isDirty || HasRecentSourceActivity()) return "EDITING";
            if (!IsEditorFocused()) return "BACKGROUND";
            if (_uncommittedFiles > 0) return "UNCOMMITTED";
            return string.IsNullOrEmpty(_scene) ? "SAFE" : "VIEWING";
        }

        private static string CurrentActivity()
        {
            if (_isBuilding) return "Đang build";
            if (EditorApplication.isCompiling) return "Đang biên dịch";
            if (EditorApplication.isPlayingOrWillChangePlaymode) return "Đang chạy Play Mode";
            if (_isDirty) return "Đang chỉnh sửa scene";
            if (HasRecentSourceActivity()) return "Source/asset vừa được thay đổi";
            if (!IsEditorFocused()) return "Unity Editor chạy nền";
            return string.IsNullOrEmpty(_scene) ? "Unity Editor" : "Đang xem scene";
        }

        private static string RuntimeMode()
        {
            if (_isBuilding) return "BUILD";
            if (EditorApplication.isCompiling) return "COMPILE";
            if (EditorApplication.isPlayingOrWillChangePlaymode) return "PLAY";
            if (EditorApplication.isPaused) return "PAUSED";
            if (_isDirty || HasRecentSourceActivity()) return "EDIT";
            if (!IsEditorFocused()) return "BACKGROUND";
            return "EDIT";
        }

        private static bool HasRecentSourceActivity()
        {
            return EditorApplication.timeSinceStartup - _lastSourceActivity
                <= RecentSourceActivitySeconds;
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
                if (string.IsNullOrEmpty(eventType))
                {
                    // Keep only the newest periodic state so stale heartbeats
                    // cannot delay the next live heartbeat after reconnect.
                    _latestHeartbeat = payload;
                }
                else
                {
                    while (PendingPayloads.Count >= MaximumOfflinePayloads) PendingPayloads.Dequeue();
                    PendingPayloads.Enqueue(payload);
                }
            }
            PersistPendingPayloads();
            _ = FlushQueueAsync();
        }

        private static void RestorePendingPayloads()
        {
            try
            {
                if (!File.Exists(OfflineQueuePath)) return;
                var stored = JsonUtility.FromJson<OfflineQueue>(
                    File.ReadAllText(OfflineQueuePath, Encoding.UTF8)
                );
                if (stored?.items == null) return;
                foreach (var payload in stored.items)
                {
                    if (payload == null) continue;
                    while (PendingPayloads.Count >= MaximumOfflinePayloads)
                    {
                        PendingPayloads.Dequeue();
                    }
                    PendingPayloads.Enqueue(payload);
                }
            }
            catch
            {
                // File hỏng không được làm gián đoạn Unity Editor.
            }
        }

        private static void PersistPendingPayloads()
        {
            try
            {
                List<StatusPayload> snapshot;
                lock (PendingPayloads)
                {
                    snapshot = new List<StatusPayload>(PendingPayloads);
                }
                var directory = Path.GetDirectoryName(OfflineQueuePath);
                if (!string.IsNullOrEmpty(directory)) Directory.CreateDirectory(directory);
                File.WriteAllText(
                    OfflineQueuePath,
                    JsonUtility.ToJson(new OfflineQueue { items = snapshot }),
                    new UTF8Encoding(false)
                );
            }
            catch
            {
                // Hàng đợi trên RAM vẫn tiếp tục hoạt động nếu không ghi được Library.
            }
        }

        private static void RemoveSentPayload(StatusPayload expected, bool heartbeat)
        {
            lock (PendingPayloads)
            {
                if (heartbeat)
                {
                    if (ReferenceEquals(_latestHeartbeat, expected)) _latestHeartbeat = null;
                }
                else if (PendingPayloads.Count > 0 && ReferenceEquals(PendingPayloads.Peek(), expected))
                {
                    PendingPayloads.Dequeue();
                }
            }
            PersistPendingPayloads();
        }

        private static async Task FlushQueueAsync()
        {
            if (_isSending) return;
            var now = EditorApplication.timeSinceStartup;
            if (now < _nextRetryAt) return;
            _isSending = true;
            try
            {
                var sentThisPass = 0;
                while (sentThisPass < 5)
                {
                    StatusPayload payload;
                    bool heartbeat;
                    lock (PendingPayloads)
                    {
                        heartbeat = _latestHeartbeat != null;
                        payload = heartbeat
                            ? _latestHeartbeat
                            : PendingPayloads.Count > 0 ? PendingPayloads.Peek() : null;
                    }
                    if (payload == null) break;
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
                    // Prevent one stalled network request from blocking every
                    // later heartbeat while Unity remains open.
                    request.timeout = 15;
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
                        var responseBody = request.downloadHandler?.text;
                        var serverMessage = string.IsNullOrWhiteSpace(responseBody)
                            ? request.error
                            : responseBody;
                        Debug.LogWarning(
                            "[QLDATN Tracker] Không thể đồng bộ (HTTP "
                            + request.responseCode
                            + "): "
                            + serverMessage
                        );
                        // Server vẫn có thể trả gói cập nhật đã ký kèm HTTP 422.
                        // Xử lý trước khi loại payload để client cũ có cơ hội tự sửa.
                        await TryApplyUpdate(responseBody);
                        var transientFailure = request.responseCode == 0
                            || request.responseCode == 408
                            || request.responseCode == 429
                            || request.responseCode >= 500;
                        if (transientFailure)
                        {
                            var delayIndex = Math.Min(_retryAttempt, RetryDelaysSeconds.Length - 1);
                            _nextRetryAt = EditorApplication.timeSinceStartup + RetryDelaysSeconds[delayIndex];
                            _retryAttempt += 1;
                            PersistPendingPayloads();
                            break;
                        }
                        RemoveSentPayload(payload, heartbeat);
                        _retryAttempt = 0;
                        _nextRetryAt = 0;
                        sentThisPass += 1;
                    }
                    else
                    {
                        RemoveSentPayload(payload, heartbeat);
                        _retryAttempt = 0;
                        _nextRetryAt = 0;
                        sentThisPass += 1;
                        await TryApplyUpdate(request.downloadHandler?.text);
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

        private static async Task TryApplyUpdate(string responseBody)
        {
            if (string.IsNullOrWhiteSpace(responseBody)) return;
            HeartbeatResponse response;
            try
            {
                response = JsonUtility.FromJson<HeartbeatResponse>(responseBody);
            }
            catch
            {
                return;
            }
            var update = response?.clientUpdate;
            if (
                update == null
                || update.platform != "UNITY"
                || update.version == ClientVersion
                || string.IsNullOrEmpty(update.url)
                || string.IsNullOrEmpty(update.sha256)
                || string.IsNullOrEmpty(update.signature)
            )
            {
                return;
            }
            var manifest = "artifact."
                + update.platform + "."
                + update.version + "."
                + update.sha256 + "."
                + update.url;
            var expectedSignature = SignRequest(
                EditorPrefs.GetString(DeviceSecretPreference),
                manifest
            );
            if (!ConstantTimeEquals(expectedSignature, update.signature)) return;

            string source;
            using (var request = UnityWebRequest.Get(update.url))
            {
                var operation = request.SendWebRequest();
                while (!operation.isDone) await Task.Delay(50);
                if (request.result != UnityWebRequest.Result.Success) return;
                source = request.downloadHandler?.text;
            }
            if (
                string.IsNullOrWhiteSpace(source)
                || !ConstantTimeEquals(Sha256Hex(source), update.sha256.ToLowerInvariant())
            )
            {
                return;
            }

            var editorDirectory = Path.Combine(Application.dataPath, "Editor");
            var targetPath = Path.Combine(editorDirectory, "QLDATNSceneTracker.cs");
            var downloadPath = targetPath + ".download";
            var backupPath = targetPath + ".backup";
            File.WriteAllText(downloadPath, source, new UTF8Encoding(false));
            try
            {
                File.Copy(targetPath, backupPath, true);
                EditorPrefs.SetString(PendingUpdatePreference, update.version);
                _updateCompilationFailed = false;
                File.Copy(downloadPath, targetPath, true);
                File.Delete(downloadPath);
                AssetDatabase.ImportAsset(
                    "Assets/Editor/QLDATNSceneTracker.cs",
                    ImportAssetOptions.ForceUpdate
                );
                Debug.Log("[QLDATN Tracker] Đang tự cập nhật lên " + update.version + "...");
            }
            catch
            {
                EditorPrefs.DeleteKey(PendingUpdatePreference);
                if (File.Exists(backupPath))
                {
                    File.Copy(backupPath, targetPath, true);
                    File.Delete(backupPath);
                }
                if (File.Exists(downloadPath)) File.Delete(downloadPath);
                throw;
            }
        }

        private static void FinalizePendingUpdate()
        {
            var pendingVersion = EditorPrefs.GetString(PendingUpdatePreference);
            if (string.IsNullOrEmpty(pendingVersion)) return;
            var targetPath = Path.Combine(Application.dataPath, "Editor/QLDATNSceneTracker.cs");
            var backupPath = targetPath + ".backup";
            EditorPrefs.DeleteKey(PendingUpdatePreference);
            if (_updateCompilationFailed && File.Exists(backupPath))
            {
                File.Copy(backupPath, targetPath, true);
                AssetDatabase.ImportAsset(
                    "Assets/Editor/QLDATNSceneTracker.cs",
                    ImportAssetOptions.ForceUpdate
                );
                Debug.LogError(
                    "[QLDATN Tracker] Bản cập nhật "
                    + pendingVersion
                    + " không biên dịch được; đã tự khôi phục bản trước."
                );
            }
            else
            {
                Debug.Log("[QLDATN Tracker] Đã cập nhật thành công lên " + pendingVersion + ".");
            }
            if (File.Exists(backupPath)) File.Delete(backupPath);
            _updateCompilationFailed = false;
        }

        private static string Sha256Hex(string value)
        {
            using var sha = SHA256.Create();
            var hash = sha.ComputeHash(Encoding.UTF8.GetBytes(value));
            return BitConverter.ToString(hash).Replace("-", "").ToLowerInvariant();
        }

        private static bool ConstantTimeEquals(string first, string second)
        {
            if (first == null || second == null || first.Length != second.Length) return false;
            var difference = 0;
            for (var index = 0; index < first.Length; index++)
            {
                difference |= first[index] ^ second[index];
            }
            return difference == 0;
        }

        public static void ReportAssetChanges(int count)
        {
            if (count <= 0) return;
            _lastSourceActivity = EditorApplication.timeSinceStartup;
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
            DeleteSessionId();
        }

        private static string LoadOrCreateSessionId()
        {
            try
            {
                if (File.Exists(SessionIdPath))
                {
                    var stored = File.ReadAllText(SessionIdPath).Trim();
                    if (stored.Length == 32) return stored;
                }
                var directory = Path.GetDirectoryName(SessionIdPath);
                if (!string.IsNullOrEmpty(directory)) Directory.CreateDirectory(directory);
                var created = Guid.NewGuid().ToString("N");
                File.WriteAllText(SessionIdPath, created, new UTF8Encoding(false));
                return created;
            }
            catch
            {
                return Guid.NewGuid().ToString("N");
            }
        }

        private static void DeleteSessionId()
        {
            try
            {
                if (File.Exists(SessionIdPath)) File.Delete(SessionIdPath);
            }
            catch
            {
                // Session cũ vẫn an toàn vì server không cộng gap quá timeout.
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
        private class OfflineQueue
        {
            public List<StatusPayload> items = new List<StatusPayload>();
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

        [Serializable]
        private class HeartbeatResponse
        {
            public ClientUpdatePayload clientUpdate;
        }

        [Serializable]
        private class ClientUpdatePayload
        {
            public string platform;
            public string version;
            public string sha256;
            public string url;
            public string signature;
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
