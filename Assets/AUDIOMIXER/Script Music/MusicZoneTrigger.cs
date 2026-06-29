using UnityEngine;

public class MusicZoneTrigger : MonoBehaviour
{
    [Header("Cấu hình nhạc vùng")]
    [SerializeField] private AudioClip zoneMusicClip; 
    [SerializeField] private float zoneMaxVolume = 0.7f;

    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            if (BGMManager.Instance != null && zoneMusicClip != null)
            {
                BGMManager.Instance.SwitchBGM(zoneMusicClip, zoneMaxVolume);
            }
        }
    }
}