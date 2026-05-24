using UnityEngine;
using UnityEngine.Playables;
using UnityEngine.Timeline;

[System.Serializable]
public class FadeClip : PlayableAsset, ITimelineClipAsset
{
    [Range(0, 1)] public float startAlpha = 0f;
    [Range(0, 1)] public float endAlpha   = 1f;

    public ClipCaps clipCaps => ClipCaps.None;

    public override Playable CreatePlayable(PlayableGraph graph, GameObject owner)
    {
        var playable = ScriptPlayable<FadeBehaviour>.Create(graph);
        var b = playable.GetBehaviour();
        b.startAlpha = startAlpha;
        b.endAlpha   = endAlpha;
        return playable;
    }
}
