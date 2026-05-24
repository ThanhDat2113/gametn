using UnityEngine;
using UnityEngine.Playables;

/// <summary>
/// Đặt component này trên trigger zone hoặc NPC.
/// Khi player bước vào hoặc gọi Play() thì Timeline chạy.
/// </summary>
[RequireComponent(typeof(PlayableDirector))]
public class CutsceneTrigger : MonoBehaviour
{
    [Header("Settings")]
    public bool playOnce = true;
    public bool triggerOnEnter = true;
    public string playerTag = "Player";

    [Header("State")]
    [SerializeField] private bool _hasPlayed = false;

    private PlayableDirector _director;

    void Awake() => _director = GetComponent<PlayableDirector>();

    // ── Trigger zone ──────────────────────────────────
    void OnTriggerEnter2D(Collider2D other)
    {
        if (!triggerOnEnter) return;
        if (!other.CompareTag(playerTag)) return;
        Play();
    }

    // ── Gọi từ code (NPC interaction, v.v.) ──────────
    public void Play()
    {
        if (playOnce && _hasPlayed) return;
        if (_director == null) return;

        _hasPlayed = true;
        _director.Play();
    }

    public void Reset() => _hasPlayed = false;
}