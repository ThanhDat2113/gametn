using UnityEngine;
using UnityEngine.Playables;
using UnityEngine.UI;

public class ImageFadeMixerBehaviour : PlayableBehaviour
{
    public override void ProcessFrame(Playable playable, FrameData info, object playerData)
    {
        var trackBinding = playerData as Image;
        if (trackBinding == null) return;

        int inputCount = playable.GetInputCount();
        for (int i = 0; i < inputCount; i++)
        {
            float weight = playable.GetInputWeight(i);
            if (weight <= 0f) continue;

            var inputPlayable = (ScriptPlayable<ImageFadeBehaviour>)playable.GetInput(i);
            var behaviour = inputPlayable.GetBehaviour();

            if (behaviour.image != null)
                trackBinding.sprite = behaviour.image;
        }
    }
}