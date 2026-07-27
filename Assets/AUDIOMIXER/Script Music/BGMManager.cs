using System.Collections;
using UnityEngine;

public class BGMManager : MonoBehaviour
{
    public static BGMManager Instance { get; private set; }

    private AudioSource sourceA;
    private AudioSource sourceB;
    private AudioSource activeSource;
    private Coroutine crossfadeRoutine;

    [SerializeField] private float crossfadeDuration = 1.5f;
    [SerializeField] private float defaultMaxVolume = 0.8f;
    [SerializeField] private bool loopMusic = true;

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
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
        }
        else
        {
            sourceA = gameObject.AddComponent<AudioSource>();
            sourceB = gameObject.AddComponent<AudioSource>();
        }

        ConfigureSource(sourceA);
        ConfigureSource(sourceB);
        activeSource = sourceA;
    }

    public void SwitchBGM(AudioClip newTrack, float maxVolume = 0.8f, float fadeDuration = -1f)
    {
        if (newTrack == null) return;

        if (activeSource != null && activeSource.clip == newTrack && activeSource.isPlaying)
        {
            return;
        }

        float duration = fadeDuration > 0f ? fadeDuration : crossfadeDuration;
        float targetVolume = Mathf.Clamp01(maxVolume > 0f ? maxVolume : defaultMaxVolume);

        AudioSource fadeOutSource = activeSource;
        AudioSource fadeInSource = (fadeOutSource == sourceA) ? sourceB : sourceA;

        if (crossfadeRoutine != null)
        {
            StopCoroutine(crossfadeRoutine);
        }

        activeSource = fadeInSource;
        fadeInSource.clip = newTrack;
        fadeInSource.volume = 0f;
        fadeInSource.Play();

        crossfadeRoutine = StartCoroutine(CrossfadeRoutine(fadeOutSource, fadeInSource, targetVolume, duration));
    }

    private void ConfigureSource(AudioSource source)
    {
        if (source == null) return;

        source.playOnAwake = false;
        source.loop = loopMusic;
        source.volume = 0f;
        source.spatialBlend = 0f;
        source.priority = 128;
    }

    private IEnumerator CrossfadeRoutine(AudioSource fadeOut, AudioSource fadeIn, float targetVolume, float duration)
    {
        if (fadeIn == null) yield break;

        float elapsed = 0f;
        float startOutVolume = fadeOut != null ? fadeOut.volume : 0f;
        float startInVolume = fadeIn.volume;

        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.Clamp01(elapsed / duration);

            if (fadeOut != null)
            {
                fadeOut.volume = Mathf.Lerp(startOutVolume, 0f, t);
            }

            fadeIn.volume = Mathf.Lerp(startInVolume, targetVolume, t);
            yield return null;
        }

        if (fadeOut != null)
        {
            fadeOut.volume = 0f;
            fadeOut.Stop();
        }

        if (fadeIn != null)
        {
            fadeIn.volume = targetVolume;
        }

        crossfadeRoutine = null;
    }
}