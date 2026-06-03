using UnityEngine;
using UnityEngine.Playables;
using UnityEngine.Timeline;

[System.Serializable]
public class ImageFadeClip : PlayableAsset, ITimelineClipAsset
{
    public Sprite image;
    [Range(0f, 1f)] public float fadeInDuration = 0.3f;
    [Range(0f, 1f)] public float fadeOutDuration = 0.3f;

    public ClipCaps clipCaps => ClipCaps.None;

    public override Playable CreatePlayable(PlayableGraph graph, GameObject owner)
    {
        var playable = ScriptPlayable<ImageFadeBehaviour>.Create(graph);
        var behaviour = playable.GetBehaviour();
        behaviour.image = image;
        behaviour.fadeInDuration = fadeInDuration;
        behaviour.fadeOutDuration = fadeOutDuration;
        return playable;
    }
}