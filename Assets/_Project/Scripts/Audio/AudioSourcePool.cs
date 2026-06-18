using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Object pool cho AudioSource SFX — cho phép phát nhiều SFX 3D cùng lúc.
/// </summary>
public class AudioSourcePool : MonoBehaviour
{
    public GameObject audioSourcePrefab;
    public int initialPoolSize = 10;
    public bool autoExpand = true;

    private Queue<AudioSource> available = new();
    private List<AudioSource> allSources = new();

    public void Initialize(Transform parent)
    {
        if (audioSourcePrefab == null)
        {
            audioSourcePrefab = new GameObject("SFX Source");
            var src = audioSourcePrefab.AddComponent<AudioSource>();
            src.playOnAwake = false;
            src.spatialBlend = 1f;
            src.dopplerLevel = 0f;
            src.rolloffMode = AudioRolloffMode.Logarithmic;
            src.minDistance = 1f;
            src.maxDistance = 20f;
            audioSourcePrefab.SetActive(false);
        }

        for (int i = 0; i < initialPoolSize; i++)
            CreateNewSource(parent);
    }

    private void CreateNewSource(Transform parent)
    {
        GameObject go = Instantiate(audioSourcePrefab, parent);
        go.name = $"SFX Source {allSources.Count}";
        var src = go.GetComponent<AudioSource>();
        if (src == null)
        {
            src = go.AddComponent<AudioSource>();
            src.playOnAwake = false;
        }
        go.SetActive(false);
        available.Enqueue(src);
        allSources.Add(src);
    }

    public AudioSource Get()
    {
        if (available.Count == 0)
        {
            if (autoExpand)
                CreateNewSource(transform);
            else
                return null;
        }
        AudioSource source = available.Dequeue();
        source.gameObject.SetActive(true);
        return source;
    }

    public void Return(AudioSource source)
    {
        if (source == null) return;
        source.Stop();
        source.clip = null;
        source.gameObject.SetActive(false);
        available.Enqueue(source);
    }
}