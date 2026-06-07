using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Quản lý âm thanh trong combat: SFX skill + BGM theo zone.
/// Singleton — tự tạo nếu chưa có trong scene.
/// </summary>
public class CombatAudioManager : MonoBehaviour
{
    private static CombatAudioManager _instance;
    public static CombatAudioManager Instance
    {
        get
        {
            if (_instance == null)
            {
                var go = new GameObject("CombatAudioManager");
                _instance = go.AddComponent<CombatAudioManager>();
                DontDestroyOnLoad(go);
            }
            return _instance;
        }
        private set { _instance = value; }
    }

    [Header("Audio Sources")]
    public AudioSource bgmSource;
    public AudioSource sfxSource;

    [Header("Zone BGM List (fallback)")]
    public ZoneBGMEntry[] zoneBGMList;

    [System.Serializable]
    public class ZoneBGMEntry
    {
        public string zoneTag;   // "vùng 1", "vùng 2", ...
        public AudioClip bgmClip;
    }

    private string currentZone = "";
    private float defaultBGMVolume = 0.5f;

    private void Awake()
    {
        if (_instance != null && _instance != this) { Destroy(gameObject); return; }
        Instance = this;

        // Tự động tạo AudioSource nếu chưa gán
        if (bgmSource == null)
        {
            bgmSource = gameObject.AddComponent<AudioSource>();
            bgmSource.loop = true;
            bgmSource.volume = defaultBGMVolume;
            bgmSource.spatialBlend = 0f;
        }
        if (sfxSource == null)
        {
            sfxSource = gameObject.AddComponent<AudioSource>();
            sfxSource.loop = false;
            sfxSource.spatialBlend = 0f;
        }
    }

    /// <summary>
    /// Play BGM dựa trên zone tag và clip ghi đè (từ EnemyGroupData).
    /// </summary>
    public void PlayBGM(string zoneTag, AudioClip overrideClip = null)
    {
        if (overrideClip != null)
        {
            // Dùng clip từ EnemyGroupData
            if (bgmSource.clip != overrideClip)
            {
                bgmSource.clip = overrideClip;
                bgmSource.Play();
                currentZone = zoneTag;
                Debug.Log($"[CombatAudio] Play BGM: {overrideClip.name} (zone: {zoneTag})");
            }
            return;
        }

        // Fallback: tìm trong zoneBGMList
        if (zoneTag == currentZone && bgmSource.isPlaying) return;

        foreach (var entry in zoneBGMList)
        {
            if (entry.zoneTag == zoneTag && entry.bgmClip != null)
            {
                bgmSource.clip = entry.bgmClip;
                bgmSource.Play();
                currentZone = zoneTag;
                Debug.Log($"[CombatAudio] Play BGM: {entry.bgmClip.name} (zone: {zoneTag})");
                return;
            }
        }

        Debug.LogWarning($"[CombatAudio] Không tìm thấy BGM cho zone: {zoneTag}");
    }

    /// <summary>
    /// Play SFX từ skill — chọn clip theo hitIndex.
    /// </summary>
    public void PlaySkillSFX(AudioClip[] sfxClips, int hitIndex = 0)
    {
        if (sfxClips == null || sfxClips.Length == 0) return;

        int index = Mathf.Clamp(hitIndex, 0, sfxClips.Length - 1);
        if (sfxClips[index] != null)
        {
            sfxSource.PlayOneShot(sfxClips[index]);
        }
    }

    /// <summary>
    /// Play SFX bất kỳ (hit impact, heal, v.v.)
    /// </summary>
    public void PlaySFX(AudioClip clip, float volume = 1f)
    {
        if (clip != null)
            sfxSource.PlayOneShot(clip, volume);
    }

    /// <summary>Dừng BGM.</summary>
    public void StopBGM()
    {
        bgmSource.Stop();
        currentZone = "";
    }

    private void OnDestroy()
    {
        if (_instance == this) _instance = null;
    }
}