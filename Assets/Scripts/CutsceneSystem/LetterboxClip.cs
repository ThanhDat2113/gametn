using UnityEngine;
using UnityEngine.Playables;
using UnityEngine.Timeline;

[System.Serializable]
public class LetterboxClip : PlayableAsset, ITimelineClipAsset
{
    [Range(0, 0.3f)] public float targetHeight = 0.1f;

    public ClipCaps clipCaps => ClipCaps.None;

    public override Playable CreatePlayable(PlayableGraph graph, GameObject owner)
    {
        var p = ScriptPlayable<LetterboxBehaviour>.Create(graph);
        p.GetBehaviour().targetHeight = targetHeight;
        return p;
    }
}
