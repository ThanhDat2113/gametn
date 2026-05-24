using UnityEngine;
using UnityEngine.Playables;
using UnityEngine.Timeline;
using UnityEngine.UI;

[TrackColor(0.05f, 0.05f, 0.05f)]
[TrackClipType(typeof(FadeClip))]
[TrackBindingType(typeof(CanvasGroup))]
public class FadeTrack : TrackAsset
{
    public override Playable CreateTrackMixer(PlayableGraph graph, GameObject go, int inputCount)
        => ScriptPlayable<FadeMixer>.Create(graph, inputCount);
}