using UnityEngine;
using UnityEngine.Playables;

public class InputBlockerBehaviour : PlayableBehaviour
{
    private bool _blocked;

    public override void OnBehaviourPlay(Playable playable, FrameData info)
    {
        if (!Application.isPlaying || _blocked) return;
        _blocked = true;
        InputBlocker.Instance?.Block();
    }

    public override void OnBehaviourPause(Playable playable, FrameData info)
    {
        if (!Application.isPlaying) return;
        _blocked = false;
        InputBlocker.Instance?.Unblock();
    }
}
