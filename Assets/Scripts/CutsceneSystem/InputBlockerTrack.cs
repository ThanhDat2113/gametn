using UnityEngine;
using UnityEngine.Playables;
using UnityEngine.Timeline;

[TrackColor(0.8f, 0.2f, 0.2f)]
[TrackClipType(typeof(InputBlockerClip))]
public class InputBlockerTrack : TrackAsset
{
    public override Playable CreateTrackMixer(PlayableGraph graph, GameObject go, int inputCount)
        => ScriptPlayable<InputBlockerMixer>.Create(graph, inputCount);
}

public class InputBlockerMixer : PlayableBehaviour { }
