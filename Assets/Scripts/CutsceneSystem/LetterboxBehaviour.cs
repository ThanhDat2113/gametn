using UnityEngine;
using UnityEngine.Playables;

public class LetterboxBehaviour : PlayableBehaviour
{
    public float targetHeight;
}

public class LetterboxMixer : PlayableBehaviour
{
    private LetterboxController _ctrl;

    public override void ProcessFrame(Playable playable, FrameData info, object playerData)
    {
        _ctrl = playerData as LetterboxController;
        if (_ctrl == null) return;

        float blended = 0f;
        float totalW  = 0f;
        int count = playable.GetInputCount();

        for (int i = 0; i < count; i++)
        {
            float w = playable.GetInputWeight(i);
            if (w <= 0f) continue;
            var b = ((ScriptPlayable<LetterboxBehaviour>)playable.GetInput(i)).GetBehaviour();
            blended += b.targetHeight * w;
            totalW  += w;
        }

        _ctrl.SetHeight(totalW > 0f ? blended : 0f);
    }
}
