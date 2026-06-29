using System.Collections;
using UnityEngine;

public class BGMManager : MonoBehaviour
{
    // Tạo Instance tĩnh để các Script khác có thể gọi BGMManager.Instance
    public static BGMManager Instance { get; private set; }

    private AudioSource sourceA;
    private AudioSource sourceB;
    private AudioSource activeSource;

    [SerializeField] private float crossfadeDuration = 2.5f; 

    private void Awake()
    {
        // Tự cấu hình Quản lý Singleton trực tiếp tại đây
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject); // Giữ nhạc xuyên suốt khi đổi Scene
        }
        else
        {
            Destroy(gameObject);
            return;
        }

        AudioSource[] sources = GetComponents<AudioSource>();
        if (sources.Length >= 2)
        {
            sourceA = sources[0];
            sourceB = sources[1];
            activeSource = sourceA; 
        }
        else
        {
            Debug.LogError("BGMManager cần ít nhất 2 component Audio Source trên cùng một Object!");
        }
    }

    public void SwitchBGM(AudioClip newTrack, float maxVolume = 0.8f)
    {
        if (activeSource == null || (activeSource.clip == newTrack && activeSource.isPlaying)) return;

        AudioSource fadeOutSource = activeSource;
        AudioSource fadeInSource = (activeSource == sourceA) ? sourceB : sourceA;

        activeSource = fadeInSource; 
        fadeInSource.clip = newTrack;
        
        StopAllCoroutines();
        StartCoroutine(CrossfadeRoutine(fadeOutSource, fadeInSource, maxVolume));
    }

    private IEnumerator CrossfadeRoutine(AudioSource fadeOut, AudioSource fadeIn, float targetVolume)
    {
        if (fadeIn.clip != null) fadeIn.Play();

        float timeElapsed = 0f;
        float startOutVolume = fadeOut != null ? fadeOut.volume : 0f;

        while (timeElapsed < crossfadeDuration)
        {
            timeElapsed += Time.deltaTime;
            float t = timeElapsed / crossfadeDuration;

            if (fadeOut != null) fadeOut.volume = Mathf.Lerp(startOutVolume, 0f, t);
            fadeIn.volume = Mathf.Lerp(0f, targetVolume, t);

            yield return null;
        }

        if (fadeOut != null)
        {
            fadeOut.volume = 0f;
            fadeOut.Stop();
        }

        fadeIn.volume = targetVolume;
    }
}