using UnityEngine;
using UnityEngine.Playables;
using UnityEngine.Timeline;

[TrackColor(0.55f, 0.25f, 0.9f)]
[TrackClipType(typeof(DialogueClip))]
[TrackBindingType(typeof(DialogueController))] // Quan trọng: gán binding
public class DialogueTrack : TrackAsset
{
    public override Playable CreateTrackMixer(PlayableGraph graph, GameObject go, int inputCount)
    {
        return ScriptPlayable<DialogueTrackMixer>.Create(graph, inputCount);
    }
}

// Lớp mixer này bắt buộc phải có để track hoạt động
public class DialogueTrackMixer : PlayableBehaviour { }