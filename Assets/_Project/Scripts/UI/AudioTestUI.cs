using UnityEngine;

/// <summary>
/// AudioTestUI — AudioManager riêng cho Test Scene.
/// Tự quản lý AudioSource components và phát âm thanh trực tiếp từ các test clips.
/// KHÔNG phụ thuộc vào AudioManager của game.
/// Gắn script này vào bất kỳ GameObject nào trong Test Scene.
/// </summary>
public class AudioTestUI : MonoBehaviour
{
    [Header("Audio Sources (tự động tạo nếu để null)")]
    public AudioSource bgmSource;
    public AudioSource sfxSource;
    public AudioSource uiSource;
    public AudioSource ambienceSource;

    [Header("Test Audio Clips")]
    public AudioClip testBGMClip;
    public AudioClip testSFXClip;
    public AudioClip testAmbienceClip;
    public AudioClip testSelectClip;
    public AudioClip testCancelClip;
    public AudioClip testHoverClip;

    [Header("Settings")]
    [Tooltip("Hiện panel ngay khi game chạy")]
    public bool showOnStart = true;
    [Tooltip("Phím tắt để bật/tắt panel")]
    public KeyCode toggleKey = KeyCode.F12;

    private bool _showPanel = true;
    private Vector2 _scrollPosition;
    private string _statusLog = "Ready";

    private float _masterVolume = 1f;
    private float _bgmVolume = 0.5f;
    private float _sfxVolume = 0.7f;
    private float _uiVolume = 0.7f;

    private const float BUTTON_WIDTH = 180f;
    private const float BUTTON_HEIGHT = 30f;
    private const float LABEL_WIDTH = 100f;
    private const float SLIDER_WIDTH = 200f;

    private float _lastHoverTime;
    private const float HOVER_COOLDOWN = 0.2f;

    private void Awake()
    {
        if (bgmSource == null)
        {
            bgmSource = gameObject.AddComponent<AudioSource>();
            bgmSource.loop = true;
            bgmSource.playOnAwake = false;
            bgmSource.spatialBlend = 0f;
        }
        if (sfxSource == null)
        {
            sfxSource = gameObject.AddComponent<AudioSource>();
            sfxSource.loop = false;
            sfxSource.playOnAwake = false;
            sfxSource.spatialBlend = 0f;
        }
        if (uiSource == null)
        {
            uiSource = gameObject.AddComponent<AudioSource>();
            uiSource.loop = false;
            uiSource.playOnAwake = false;
            uiSource.spatialBlend = 0f;
        }
        if (ambienceSource == null)
        {
            ambienceSource = gameObject.AddComponent<AudioSource>();
            ambienceSource.loop = true;
            ambienceSource.playOnAwake = false;
            ambienceSource.spatialBlend = 0f;
        }
    }

    private void Start()
    {
        _showPanel = showOnStart;
        int clipCount = 0;
        if (testBGMClip != null) clipCount++;
        if (testSFXClip != null) clipCount++;
        if (testAmbienceClip != null) clipCount++;
        if (testSelectClip != null) clipCount++;
        if (testCancelClip != null) clipCount++;
        if (testHoverClip != null) clipCount++;
        _statusLog = $"AudioTestUI sẵn sàng — {clipCount} clips được gán";
    }

    private void Update()
    {
        if (Input.GetKeyDown(toggleKey))
            _showPanel = !_showPanel;
    }

    private void OnGUI()
    {
        if (!_showPanel) return;

        GUI.Box(new Rect(10, 10, 450, 600), "🎵 AUDIO TEST UI");
        GUILayout.BeginArea(new Rect(20, 30, 420, 570));
        _scrollPosition = GUILayout.BeginScrollView(_scrollPosition);

        DrawStatusSection();
        DrawBGMSection();
        DrawSFXSection();
        DrawUISection();
        DrawAmbienceSection();
        DrawVolumeSection();
        DrawInfoSection();

        GUILayout.EndScrollView();
        GUILayout.EndArea();
    }

    private void DrawStatusSection()
    {
        GUILayout.Label("─── Status ───", GUI.skin.box);
        GUILayout.Label(_statusLog);
        if (bgmSource == null || sfxSource == null)
            GUILayout.Label("⚠ AudioSources chưa được khởi tạo!");
        GUILayout.Space(5);
    }

    private void DrawBGMSection()
    {
        GUILayout.Label("─── BGM ───", GUI.skin.box);
        GUILayout.BeginHorizontal();
        if (GUILayout.Button("Play BGM", GUILayout.Width(BUTTON_WIDTH), GUILayout.Height(BUTTON_HEIGHT)))
        {
            if (testBGMClip != null)
            {
                bgmSource.volume = _bgmVolume * _masterVolume;
                bgmSource.clip = testBGMClip;
                bgmSource.Play();
                _statusLog = $"BGM: {testBGMClip.name}";
            }
            else _statusLog = "⚠ Chưa gán testBGMClip!";
        }
        if (GUILayout.Button("Stop BGM", GUILayout.Width(BUTTON_WIDTH), GUILayout.Height(BUTTON_HEIGHT)))
        {
            bgmSource.Stop();
            _statusLog = "BGM stopped";
        }
        GUILayout.EndHorizontal();
        GUILayout.Space(5);
    }

    private void DrawSFXSection()
    {
        GUILayout.Label("─── SFX ───", GUI.skin.box);
        GUILayout.BeginHorizontal();
        if (GUILayout.Button("Play SFX 2D", GUILayout.Width(BUTTON_WIDTH), GUILayout.Height(BUTTON_HEIGHT)))
        {
            if (testSFXClip != null)
            {
                sfxSource.volume = _sfxVolume * _masterVolume;
                sfxSource.PlayOneShot(testSFXClip);
                _statusLog = $"SFX: {testSFXClip.name}";
            }
            else _statusLog = "⚠ Chưa gán testSFXClip!";
        }
        if (GUILayout.Button("Play SFX 3D", GUILayout.Width(BUTTON_WIDTH), GUILayout.Height(BUTTON_HEIGHT)))
        {
            if (testSFXClip != null && Camera.main != null)
            {
                GameObject tempGO = new GameObject("TempSFX3D");
                tempGO.transform.position = Camera.main.transform.position;
                AudioSource tempSrc = tempGO.AddComponent<AudioSource>();
                tempSrc.spatialBlend = 1f;
                tempSrc.volume = _sfxVolume * _masterVolume;
                tempSrc.PlayOneShot(testSFXClip);
                Destroy(tempGO, testSFXClip.length + 0.1f);
                _statusLog = $"SFX 3D: {testSFXClip.name}";
            }
            else _statusLog = testSFXClip == null ? "⚠ Chưa gán testSFXClip!" : "⚠ Không có Camera.main!";
        }
        GUILayout.EndHorizontal();
        GUILayout.BeginHorizontal();
        if (GUILayout.Button("Repeat SFX (3x)", GUILayout.Width(BUTTON_WIDTH), GUILayout.Height(BUTTON_HEIGHT)))
        {
            if (testSFXClip != null)
            {
                sfxSource.volume = _sfxVolume * _masterVolume * 0.5f;
                for (int i = 0; i < 3; i++) sfxSource.PlayOneShot(testSFXClip);
                _statusLog = "SFX 3x played";
            }
            else _statusLog = "⚠ Chưa gán testSFXClip!";
        }
        if (GUILayout.Button("Random Pitch", GUILayout.Width(BUTTON_WIDTH), GUILayout.Height(BUTTON_HEIGHT)))
        {
            if (testSFXClip != null)
            {
                sfxSource.volume = _sfxVolume * _masterVolume;
                sfxSource.pitch = Random.Range(0.8f, 1.2f);
                sfxSource.PlayOneShot(testSFXClip);
                sfxSource.pitch = 1f;
                _statusLog = $"SFX random pitch";
            }
            else _statusLog = "⚠ Chưa gán testSFXClip!";
        }
        GUILayout.EndHorizontal();
        GUILayout.Space(5);
    }

    private void DrawUISection()
    {
        GUILayout.Label("─── UI Sounds ───", GUI.skin.box);
        GUILayout.BeginHorizontal();
        if (GUILayout.Button("Select", GUILayout.Width(BUTTON_WIDTH), GUILayout.Height(BUTTON_HEIGHT)))
        {
            uiSource.volume = _uiVolume * _masterVolume;
            if (testSelectClip != null) { uiSource.PlayOneShot(testSelectClip); _statusLog = $"UI Select: {testSelectClip.name}"; }
            else { uiSource.PlayOneShot(testSFXClip); _statusLog = testSFXClip != null ? "UI Select (dùng testSFXClip)" : "⚠ Không có clip!"; }
        }
        if (GUILayout.Button("Cancel", GUILayout.Width(BUTTON_WIDTH), GUILayout.Height(BUTTON_HEIGHT)))
        {
            uiSource.volume = _uiVolume * _masterVolume;
            if (testCancelClip != null) { uiSource.PlayOneShot(testCancelClip); _statusLog = $"UI Cancel: {testCancelClip.name}"; }
            else { uiSource.PlayOneShot(testSFXClip); _statusLog = testSFXClip != null ? "UI Cancel (dùng testSFXClip)" : "⚠ Không có clip!"; }
        }
        if (GUILayout.Button("Confirm", GUILayout.Width(BUTTON_WIDTH), GUILayout.Height(BUTTON_HEIGHT)))
        {
            uiSource.volume = _uiVolume * _masterVolume;
            if (testSelectClip != null) { uiSource.PlayOneShot(testSelectClip); _statusLog = "UI Confirm"; }
            else { uiSource.PlayOneShot(testSFXClip); _statusLog = testSFXClip != null ? "UI Confirm (dùng testSFXClip)" : "⚠ Không có clip!"; }
        }
        GUILayout.EndHorizontal();
        GUILayout.BeginHorizontal();
        if (GUILayout.Button("Hover", GUILayout.Width(BUTTON_WIDTH), GUILayout.Height(BUTTON_HEIGHT)))
        {
            if (Time.unscaledTime - _lastHoverTime < HOVER_COOLDOWN) return;
            _lastHoverTime = Time.unscaledTime;
            uiSource.volume = _uiVolume * _masterVolume * 0.5f;
            if (testHoverClip != null) { uiSource.PlayOneShot(testHoverClip); _statusLog = $"UI Hover: {testHoverClip.name}"; }
            else { uiSource.PlayOneShot(testSFXClip); _statusLog = testSFXClip != null ? "UI Hover (dùng testSFXClip)" : "⚠ Không có clip!"; }
        }
        if (GUILayout.Button("Typing", GUILayout.Width(BUTTON_WIDTH), GUILayout.Height(BUTTON_HEIGHT)))
        {
            uiSource.volume = _uiVolume * _masterVolume * 0.3f;
            if (testSFXClip != null) { uiSource.PlayOneShot(testSFXClip); _statusLog = "UI Typing"; }
            else _statusLog = "⚠ Chưa gán SFX clip!";
        }
        if (GUILayout.Button("Advance", GUILayout.Width(BUTTON_WIDTH), GUILayout.Height(BUTTON_HEIGHT)))
        {
            uiSource.volume = _uiVolume * _masterVolume;
            if (testSelectClip != null) { uiSource.PlayOneShot(testSelectClip); _statusLog = "UI Dialogue Advance"; }
            else { uiSource.PlayOneShot(testSFXClip); _statusLog = testSFXClip != null ? "UI Advance (dùng testSFXClip)" : "⚠ Không có clip!"; }
        }
        GUILayout.EndHorizontal();
        GUILayout.Space(5);
    }

    private void DrawAmbienceSection()
    {
        GUILayout.Label("─── Ambience ───", GUI.skin.box);
        GUILayout.BeginHorizontal();
        if (GUILayout.Button("Play Ambience", GUILayout.Width(BUTTON_WIDTH), GUILayout.Height(BUTTON_HEIGHT)))
        {
            if (testAmbienceClip != null)
            {
                ambienceSource.volume = _masterVolume;
                ambienceSource.clip = testAmbienceClip;
                ambienceSource.Play();
                _statusLog = $"Ambience: {testAmbienceClip.name}";
            }
            else _statusLog = "⚠ Chưa gán testAmbienceClip!";
        }
        if (GUILayout.Button("Stop Ambience", GUILayout.Width(BUTTON_WIDTH), GUILayout.Height(BUTTON_HEIGHT)))
        {
            ambienceSource.Stop();
            _statusLog = "Ambience stopped";
        }
        GUILayout.EndHorizontal();
        GUILayout.Space(5);
    }

    private void DrawVolumeSection()
    {
        GUILayout.Label("─── Volume ───", GUI.skin.box);
        _masterVolume = DrawVolumeSlider("Master", _masterVolume, () => { bgmSource.volume = _bgmVolume * _masterVolume; ambienceSource.volume = _masterVolume; });
        _bgmVolume = DrawVolumeSlider("BGM", _bgmVolume, () => { bgmSource.volume = _bgmVolume * _masterVolume; });
        _sfxVolume = DrawVolumeSlider("SFX", _sfxVolume, null);
        _uiVolume = DrawVolumeSlider("UI", _uiVolume, null);
        GUILayout.Space(5);
    }

    private float DrawVolumeSlider(string label, float currentValue, System.Action onChanged)
    {
        GUILayout.BeginHorizontal();
        GUILayout.Label(label, GUILayout.Width(LABEL_WIDTH));
        float newValue = GUILayout.HorizontalSlider(currentValue, 0f, 1f, GUILayout.Width(SLIDER_WIDTH));
        GUILayout.Label($"{(newValue * 100):F0}%", GUILayout.Width(40f));
        GUILayout.EndHorizontal();
        if (Mathf.Abs(newValue - currentValue) > 0.01f) { onChanged?.Invoke(); return newValue; }
        return currentValue;
    }

    private void DrawInfoSection()
    {
        GUILayout.Label("─── Info ───", GUI.skin.box);
        GUILayout.BeginHorizontal();
        GUILayout.Label("BGM:", GUILayout.Width(LABEL_WIDTH));
        GUILayout.Label(bgmSource.isPlaying ? $"Playing: {(bgmSource.clip != null ? bgmSource.clip.name : "?")}" : "Stopped");
        GUILayout.EndHorizontal();
        GUILayout.BeginHorizontal();
        GUILayout.Label("Ambience:", GUILayout.Width(LABEL_WIDTH));
        GUILayout.Label(ambienceSource.isPlaying ? $"Playing: {(ambienceSource.clip != null ? ambienceSource.clip.name : "?")}" : "Stopped");
        GUILayout.EndHorizontal();
        GUILayout.BeginHorizontal();
        GUILayout.Label("Status:", GUILayout.Width(LABEL_WIDTH));
        GUILayout.Label(_statusLog);
        GUILayout.EndHorizontal();
        GUILayout.BeginHorizontal();
        GUILayout.Label("Toggle:", GUILayout.Width(LABEL_WIDTH));
        GUILayout.Label(toggleKey.ToString());
        GUILayout.EndHorizontal();
        GUILayout.Space(5);
    }
}