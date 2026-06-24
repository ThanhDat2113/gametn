using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Quản lý âm thanh toàn game: map, dialogue, UI.
/// Singleton — tự động tạo nếu chưa tồn tại.
/// Không phụ thuộc vào scene object nào.
/// </summary>
public class AudioManager : MonoBehaviour
{
    private static AudioManager _instance;
    public static AudioManager Instance
    {
        get
        {
            if (_instance == null)
            {
                var go = new GameObject("AudioManager");
                _instance = go.AddComponent<AudioManager>();
                DontDestroyOnLoad(go);
            }
            return _instance;
        }
        private set { _instance = value; }
    }

    [Header("Audio Sources")]
    public AudioSource musicSource;   // Nhạc nền map
    public AudioSource ambSource;     // Âm thanh môi trường
    public AudioSource sfxSource;     // SFX tổng (UI, dialogue, map)
    public AudioSource voiceSource;   // Giọng nói dialogue

    [Header("Default Clips")]
    public AudioClip defaultClickClip;       // Click UI mặc định
    public AudioClip defaultDialogueClip;    // Tiếng text dialogue
    public AudioClip defaultFootstepClip;    // Tiếng bước chân

    private void Awake()
    {
        if (_instance != null && _instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;

        // Tự động tạo AudioSource nếu chưa gán
        EnsureAudioSource(ref musicSource, "MusicSource", true, 0.4f);
        EnsureAudioSource(ref ambSource, "AmbSource", true, 0.3f);
        EnsureAudioSource(ref sfxSource, "SFXSource", false, 0.8f);
        EnsureAudioSource(ref voiceSource, "VoiceSource", false, 1.0f);
    }

    private void EnsureAudioSource(ref AudioSource source, string name, bool loop, float volume)
    {
        if (source == null)
        {
            var child = new GameObject(name);
            child.transform.SetParent(transform);
            source = child.AddComponent<AudioSource>();
            source.loop = loop;
            source.volume = volume;
            source.spatialBlend = 0f;
            source.playOnAwake = false;
        }
    }

    // ─── MAP (di chuyển) ──────────────────────────────────────

    /// <summary>Phát tiếng bước chân (gọi từ animation event hoặc movement script).</summary>
    public void PlayFootstep(AudioClip clip = null)
    {
        if (sfxSource != null)
            sfxSource.PlayOneShot(clip != null ? clip : defaultFootstepClip);
    }

    // ─── DIALOGUE (nói chuyện) ─────────────────────────────────

    /// <summary>Phát tiếng text dialogue mỗi khi hiện 1 chữ.</summary>
    public void PlayDialogueSound(AudioClip clip = null)
    {
        if (voiceSource != null)
        {
            voiceSource.pitch = Random.Range(0.9f, 1.1f);
            voiceSource.PlayOneShot(clip != null ? clip : defaultDialogueClip);
        }
    }

    /// <summary>Dừng âm thanh dialogue.</summary>
    public void StopDialogueSound()
    {
        if (voiceSource != null) voiceSource.Stop();
    }

    // ─── UI ───────────────────────────────────────────────────

    /// <summary>Phát âm thanh khi click UI / chọn skill.</summary>
    public void PlayUIClick(AudioClip clip = null)
    {
        if (sfxSource != null)
        {
            sfxSource.pitch = 1f;
            sfxSource.PlayOneShot(clip != null ? clip : defaultClickClip);
        }
    }

    /// <summary>Phát âm thanh hover UI.</summary>
    public void PlayUIHover(AudioClip clip = null)
    {
        if (sfxSource != null)
        {
            sfxSource.pitch = 1.2f;
            sfxSource.PlayOneShot(clip != null ? clip : defaultClickClip);
        }
    }

    // ─── MUSIC (nhạc nền map) ─────────────────────────────────

    /// <summary>Phát nhạc nền map (tự động loop).</summary>
    public void PlayMusic(AudioClip clip)
    {
        if (musicSource == null || clip == null) return;
        if (musicSource.clip == clip) return;
        musicSource.clip = clip;
        musicSource.Play();
    }

    public void StopMusic()
    {
        if (musicSource != null) musicSource.Stop();
    }

    private void OnDestroy()
    {
        if (_instance == this) _instance = null;
    }
}