using UnityEngine;

/// <summary>
/// Backward-compatible wrapper — delegate to AudioManager.
/// Các script cũ gọi CombatAudioManager.Instance vẫn hoạt động.
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

    private void Awake()
    {
        if (_instance != null && _instance != this) { Destroy(gameObject); return; }
        Instance = this;
    }

    public void PlayBGM(string zoneTag, AudioClip overrideClip = null)
    {
        if (AudioManager.Instance != null)
            AudioManager.Instance.PlayBGM(zoneTag, overrideClip);
    }

    public void PlayCombatBGM(int areaIndex, AudioClip overrideClip = null)
    {
        if (AudioManager.Instance != null)
            AudioManager.Instance.PlayCombatBGM(areaIndex, overrideClip);
    }

    public void PlaySkillSFX(AudioClip[] sfxClips, int hitIndex = 0)
    {
        if (AudioManager.Instance != null)
            AudioManager.Instance.PlaySkillSFX(sfxClips, hitIndex);
    }

    public void PlaySFX(AudioClip clip, float volume = 1f)
    {
        if (AudioManager.Instance != null)
            AudioManager.Instance.PlaySFX2D(clip, volume);
    }

    public void StopBGM()
    {
        if (AudioManager.Instance != null)
            AudioManager.Instance.StopBGM();
    }

    private void OnDestroy()
    {
        if (_instance == this) _instance = null;
    }
}