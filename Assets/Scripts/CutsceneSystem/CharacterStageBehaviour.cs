using UnityEngine;
using UnityEngine.Playables;

public enum CharacterStagePosition { Left, Center, Right, FarLeft, FarRight }

public class CharacterStageBehaviour : PlayableBehaviour
{
    public DialogueCharacter character;
    public DialogueEmotion emotion;
    public CharacterStagePosition position;
    public bool slideIn;
    public bool flipX;

    private CharacterStageController _stage;
    private bool _shown;

    public override void OnGraphStart(Playable playable)
    {
        _stage = Object.FindObjectOfType<CharacterStageController>();
    }

    public override void OnBehaviourPlay(Playable playable, FrameData info)
    {
        if (!Application.isPlaying || _shown) return;
        _shown = true;
        _stage?.ShowCharacter(character, emotion, position, slideIn, flipX);
    }

    public override void OnBehaviourPause(Playable playable, FrameData info)
    {
        if (!Application.isPlaying) return;
        _stage?.HideCharacter(character);
        _shown = false;
    }
}
