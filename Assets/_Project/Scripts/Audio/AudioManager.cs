using System.Collections;
using UnityEngine;
using UnityEngine.Audio;

/// <summary>
/// Hệ thống audio trung tâm của game.
/// Singleton — tự tạo nếu chưa có trong scene.
/// Hỗ trợ: BGM crossfade, SFX 3D pool, UI sounds, Ambience, Volume settings.
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

    public enum AudioChannel { Master, BGM, SFX, UI, Ambience }

    [Header("Audio Mixer")]
    public AudioMixer audioMixer;

    [Header("Audio Sources")]
    public AudioSource bgmSource;
    public AudioSource ambienceSource;
    public AudioSource uiSource;
    public AudioSourcePool sfxPool;

    [Header("Zone BGM List (fallback)")]
    public ZoneBGMEntry[] zoneBGMList;

    [System.Serializable]
    public class ZoneBGMEntry
    {
        public string zoneTag;
        public AudioClip bgmClip;
    }

    private string currentZone = "";
    private Coroutine bgmFadeCoroutine;

    private const string PREFS_MASTER = "Audio_MasterVolume";
    private const string PREFS_BGM = "Audio_BGMVolume";
    private const string PREFS_SFX = "Audio_SFXVolume";
    private const string PREFS_UI = "Audio_UIVolume";

    private void Awake()
    {
        if (_instance != null && _instance != this) { Destroy(gameObject); return; }
        Instance = this;

        if (bgmSource == null)
        {
            bgmSource = gameObject.AddComponent<AudioSource>();
            bgmSource.loop = true;
            bgmSource.spatialBlend = 0f;
            bgmSource.outputAudioMixerGroup = GetMixerGroup("BGM");
        }
        if (ambienceSource == null)
        {
            ambienceSource = gameObject.AddComponent<AudioSource>();
            ambienceSource.loop = true;
            ambienceSource.spatialBlend = 0f;
            ambienceSource.outputAudioMixerGroup = GetMixerGroup("Ambience");
        }
        if (uiSource == null)
        {
            uiSource = gameObject.AddComponent<AudioSource>();
            uiSource.loop = false;
            uiSource.spatialBlend = 0f;
            uiSource.outputAudioMixerGroup = GetMixerGroup("UI");
        }
        if (sfxPool == null)
        {
            sfxPool = gameObject.AddComponent<AudioSourcePool>();
            sfxPool.Initialize(transform);
        }

        SetVolume(AudioChannel.Master, PlayerPrefs.GetFloat(PREFS_MASTER, 1f));
        SetVolume(AudioChannel.BGM, PlayerPrefs.GetFloat(PREFS_BGM, 0.5f));
        SetVolume(AudioChannel.SFX, PlayerPrefs.GetFloat(PREFS_SFX, 0.7f));
        SetVolume(AudioChannel.UI, PlayerPrefs.GetFloat(PREFS_UI, 0.7f));
    }

    private AudioMixerGroup GetMixerGroup(string name)
    {
        if (audioMixer != null)
        {
            var groups = audioMixer.FindMatchingGroups(name);
            if (groups != null && groups.Length > 0)
                return groups[0];
        }
        return null;
    }

    #region Volume

    public void SetVolume(AudioChannel channel, float value)
    {
        value = Mathf.Clamp01(value);
        string paramName = channel switch
        {
            AudioChannel.Master => "MasterVolume",
            AudioChannel.BGM => "BGMVolume",
            AudioChannel.SFX => "SFXVolume",
            AudioChannel.UI => "UIVolume",
            AudioChannel.Ambience => "AmbienceVolume",
            _ => "MasterVolume"
        };

        float dB = value > 0.0001f ? Mathf.Log10(value) * 20f : -80f;
        if (audioMixer != null)
            audioMixer.SetFloat(paramName, dB);

        string prefsKey = channel switch
        {
            AudioChannel.Master => PREFS_MASTER,
            AudioChannel.BGM => PREFS_BGM,
            AudioChannel.SFX => PREFS_SFX,
            AudioChannel.UI => PREFS_UI,
            _ => PREFS_MASTER
        };
        PlayerPrefs.SetFloat(prefsKey, value);
    }

    public float GetVolume(AudioChannel channel)
    {
        string prefsKey = channel switch
        {
            AudioChannel.Master => PREFS_MASTER,
            AudioChannel.BGM => PREFS_BGM,
            AudioChannel.SFX => PREFS_SFX,
            AudioChannel.UI => PREFS_UI,
            _ => PREFS_MASTER
        };
        return PlayerPrefs.GetFloat(prefsKey, 1f);
    }

    #endregion

    #region BGM

    public void PlayBGM(AudioClip clip, float fadeDuration = 0.5f)
    {
        if (clip == null) return;
        if (bgmSource.isPlaying && bgmSource.clip == clip) return;

        if (bgmFadeCoroutine != null) StopCoroutine(bgmFadeCoroutine);
        bgmFadeCoroutine = StartCoroutine(CrossfadeBGM(clip, fadeDuration));
    }

    public void PlayBGM(string zoneTag, AudioClip overrideClip = null)
    {
        if (bgmSource == null)
        {
            bgmSource = gameObject.AddComponent<AudioSource>();
            bgmSource.loop = true;
            bgmSource.spatialBlend = 0f;
        }

        if (overrideClip != null)
        {
            if (bgmSource.clip != overrideClip)
            {
                PlayBGM(overrideClip);
                currentZone = zoneTag;
            }
            return;
        }

        if (zoneTag == currentZone && bgmSource.isPlaying) return;

        if (zoneBGMList != null)
        {
            foreach (var entry in zoneBGMList)
            {
                if (entry == null) continue;
                if (entry.zoneTag == zoneTag && entry.bgmClip != null)
                {
                    PlayBGM(entry.bgmClip);
                    currentZone = zoneTag;
                    return;
                }
            }
        }
    }

    private IEnumerator CrossfadeBGM(AudioClip newClip, float duration)
    {
        float startVolume = GetVolume(AudioChannel.BGM);
        float elapsed = 0f;

        if (bgmSource.isPlaying)
        {
            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                bgmSource.volume = Mathf.Lerp(startVolume, 0f, elapsed / duration);
                yield return null;
            }
        }

        bgmSource.Stop();
        bgmSource.clip = newClip;
        bgmSource.Play();

        elapsed = 0f;
        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            bgmSource.volume = Mathf.Lerp(0f, startVolume, elapsed / duration);
            yield return null;
        }
        bgmSource.volume = startVolume;
        bgmFadeCoroutine = null;
    }

    public void StopBGM(float fadeDuration = 0.5f)
    {
        if (bgmFadeCoroutine != null) StopCoroutine(bgmFadeCoroutine);
        bgmFadeCoroutine = StartCoroutine(FadeOutBGM(fadeDuration));
        currentZone = "";
    }

    private IEnumerator FadeOutBGM(float duration)
    {
        float startVolume = bgmSource.volume;
        float elapsed = 0f;
        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            bgmSource.volume = Mathf.Lerp(startVolume, 0f, elapsed / duration);
            yield return null;
        }
        bgmSource.Stop();
        bgmSource.volume = startVolume;
        bgmFadeCoroutine = null;
    }

    #endregion

    #region Ambience

    public void PlayAmbience(AudioClip clip, float fadeDuration = 2f)
    {
        if (clip == null) return;
        if (ambienceSource.isPlaying && ambienceSource.clip == clip) return;
        StartCoroutine(CrossfadeAmbience(clip, fadeDuration));
    }

    private IEnumerator CrossfadeAmbience(AudioClip newClip, float duration)
    {
        float startVolume = GetVolume(AudioChannel.Ambience);
        float elapsed = 0f;

        if (ambienceSource.isPlaying)
        {
            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                ambienceSource.volume = Mathf.Lerp(startVolume, 0f, elapsed / duration);
                yield return null;
            }
        }

        ambienceSource.Stop();
        ambienceSource.clip = newClip;
        ambienceSource.Play();

        elapsed = 0f;
        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            ambienceSource.volume = Mathf.Lerp(0f, startVolume, elapsed / duration);
            yield return null;
        }
        ambienceSource.volume = startVolume;
    }

    public void StopAmbience(float fadeDuration = 1f)
    {
        StartCoroutine(FadeOutAmbience(fadeDuration));
    }

    private IEnumerator FadeOutAmbience(float duration)
    {
        float startVolume = ambienceSource.volume;
        float elapsed = 0f;
        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            ambienceSource.volume = Mathf.Lerp(startVolume, 0f, elapsed / duration);
            yield return null;
        }
        ambienceSource.Stop();
        ambienceSource.volume = startVolume;
    }

    #endregion

    #region SFX

    public void PlaySFX(AudioClip clip, Vector3 position, float volume = 1f)
    {
        if (clip == null) return;
        AudioSource source = sfxPool.Get();
        if (source == null) return;

        source.transform.position = position;
        source.clip = clip;
        source.volume = volume;
        source.spatialBlend = 1f;
        source.Play();

        StartCoroutine(ReturnToPoolAfterPlay(source));
    }

    public void PlaySFX2D(AudioClip clip, float volume = 1f)
    {
        if (clip == null) return;
        AudioSource source = sfxPool.Get();
        if (source == null) return;

        source.transform.position = Vector3.zero;
        source.clip = clip;
        source.volume = volume;
        source.spatialBlend = 0f;
        source.Play();

        StartCoroutine(ReturnToPoolAfterPlay(source));
    }

    public void PlaySkillSFX(AudioClip[] sfxClips, int hitIndex = 0)
    {
        if (sfxClips == null || sfxClips.Length == 0) return;
        int index = Mathf.Clamp(hitIndex, 0, sfxClips.Length - 1);
        if (sfxClips[index] != null)
            PlaySFX2D(sfxClips[index]);
    }

    public void PlayUISound(AudioClip clip, float volume = 1f)
    {
        if (clip != null && uiSource != null)
            uiSource.PlayOneShot(clip, volume);
    }

    private IEnumerator ReturnToPoolAfterPlay(AudioSource source)
    {
        yield return new WaitForSeconds(source.clip != null ? source.clip.length + 0.1f : 0.5f);
        if (sfxPool != null)
            sfxPool.Return(source);
    }

    #endregion

    private void OnDestroy()
    {
        if (_instance == this) _instance = null;
    }
}