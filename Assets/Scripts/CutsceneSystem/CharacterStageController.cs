using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Quản lý sprite nhân vật trên màn hình cutscene kiểu JRPG.
/// Đặt component này trên một GameObject "CharacterStage" trong scene.
/// </summary>
public class CharacterStageController : MonoBehaviour
{
    [Header("Anchor Points")]
    public Transform anchorFarLeft;
    public Transform anchorLeft;
    public Transform anchorCenter;
    public Transform anchorRight;
    public Transform anchorFarRight;

    [Header("Slide Settings")]
    public float slideDistance = 3f;   // bao nhiêu unit ngoài màn hình
    public float slideDuration = 0.35f;
    public AnimationCurve slideCurve = AnimationCurve.EaseInOut(0, 0, 1, 1);

    [Header("Dim Settings")]
    public float dimAlpha = 0.45f;      // nhân vật không nói sẽ bị tối

    private Dictionary<DialogueCharacter, SpriteRenderer> _activeSprites
        = new Dictionary<DialogueCharacter, SpriteRenderer>();

    // ─────────────────────────────────────────
    //  Public API
    // ─────────────────────────────────────────

    public void ShowCharacter(DialogueCharacter character, DialogueEmotion emotion,
        CharacterStagePosition pos, bool slideIn, bool flipX)
    {
        if (_activeSprites.ContainsKey(character)) return;

        var anchor = GetAnchor(pos);
        if (anchor == null) return;

        var go = new GameObject($"Stage_{character.characterName}");
        go.transform.SetParent(transform);
        go.transform.position = anchor.position;

        var sr = go.AddComponent<SpriteRenderer>();
        sr.sprite = character.GetPortrait(emotion);
        sr.sortingOrder = (int)pos;
        if (flipX) go.transform.localScale = new Vector3(-1, 1, 1);

        _activeSprites[character] = sr;

        if (slideIn) StartCoroutine(SlideIn(go.transform, anchor.position));
    }

    public void HideCharacter(DialogueCharacter character)
    {
        if (!_activeSprites.TryGetValue(character, out var sr)) return;
        _activeSprites.Remove(character);
        if (sr != null) Destroy(sr.gameObject);
    }

    public void SetEmotion(DialogueCharacter character, DialogueEmotion emotion)
    {
        if (!_activeSprites.TryGetValue(character, out var sr)) return;
        sr.sprite = character.GetPortrait(emotion);
    }

    /// <summary>Làm tối tất cả trừ nhân vật đang nói.</summary>
    public void HighlightSpeaker(DialogueCharacter speaker)
    {
        foreach (var kvp in _activeSprites)
        {
            float alpha = kvp.Key == speaker ? 1f : dimAlpha;
            var c = kvp.Value.color;
            kvp.Value.color = new Color(c.r, c.g, c.b, alpha);
        }
    }

    public void HideAll()
    {
        foreach (var kvp in _activeSprites)
            if (kvp.Value != null) Destroy(kvp.Value.gameObject);
        _activeSprites.Clear();
    }

    // ─────────────────────────────────────────
    //  Internal
    // ─────────────────────────────────────────

    Transform GetAnchor(CharacterStagePosition pos) => pos switch
    {
        CharacterStagePosition.FarLeft  => anchorFarLeft,
        CharacterStagePosition.Left     => anchorLeft,
        CharacterStagePosition.Center   => anchorCenter,
        CharacterStagePosition.Right    => anchorRight,
        CharacterStagePosition.FarRight => anchorFarRight,
        _                               => anchorCenter
    };

    IEnumerator SlideIn(Transform t, Vector3 target)
    {
        Vector3 start = target + Vector3.left * slideDistance;
        float elapsed = 0f;
        while (elapsed < slideDuration)
        {
            elapsed += Time.deltaTime;
            t.position = Vector3.Lerp(start, target, slideCurve.Evaluate(elapsed / slideDuration));
            yield return null;
        }
        t.position = target;
    }
}