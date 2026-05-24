using UnityEngine;
using UnityEngine.Playables;
using UnityEngine.Timeline;

[System.Serializable]
public class CharacterStageClip : PlayableAsset, ITimelineClipAsset
{
    public DialogueCharacter character;
    public DialogueEmotion emotion = DialogueEmotion.Normal;

    [Header("Position")]
    public CharacterStagePosition position = CharacterStagePosition.Left;
    public bool slideIn = true;
    public bool flipX = false;

    public ClipCaps clipCaps => ClipCaps.None;

    public override Playable CreatePlayable(PlayableGraph graph, GameObject owner)
    {
        var playable = ScriptPlayable<CharacterStageBehaviour>.Create(graph);
        var b = playable.GetBehaviour();
        b.character = character;
        b.emotion   = emotion;
        b.position  = position;
        b.slideIn   = slideIn;
        b.flipX     = flipX;
        return playable;
    }
}
