using UnityEngine;
using UnityEngine.Playables;
using UnityEngine.Timeline;

[TrackColor(0.1f, 0.1f, 0.1f)]
[TrackClipType(typeof(LetterboxClip))]
[TrackBindingType(typeof(LetterboxController))]
public class LetterboxTrack : TrackAsset
{
    public override Playable CreateTrackMixer(PlayableGraph graph, GameObject go, int inputCount)
        => ScriptPlayable<LetterboxMixer>.Create(graph, inputCount);
}
