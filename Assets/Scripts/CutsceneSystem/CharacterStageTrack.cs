using UnityEngine;
using UnityEngine.Playables;
using UnityEngine.Timeline;

[TrackColor(0.2f, 0.7f, 0.6f)]
[TrackClipType(typeof(CharacterStageClip))]
[TrackBindingType(typeof(CharacterStageController))]
public class CharacterStageTrack : TrackAsset
{
    public override Playable CreateTrackMixer(PlayableGraph graph, GameObject go, int inputCount)
        => ScriptPlayable<CharacterStageMixer>.Create(graph, inputCount);
}

public class CharacterStageMixer : PlayableBehaviour { }
