using UnityEngine;

[RequireComponent(typeof(AudioSource))]
public class Clicking : MonoBehaviour
{
    [Header("Sound")]
    public AudioClip clickSound;

    private AudioSource audioSource;

    private void Start()
    {
        audioSource = GetComponent<AudioSource>();

        // Prevent the AudioSource from playing automatically
        audioSource.playOnAwake = false;
    }

    private void Update()
    {
        if (Input.GetMouseButtonDown(0))
        {
            if (clickSound != null)
            {
                audioSource.PlayOneShot(clickSound);
            }
        }
    }
}