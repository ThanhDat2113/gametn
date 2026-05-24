using UnityEngine;
using UnityEngine.Playables;
using UnityEngine.UI;

public class FadeBehaviour : PlayableBehaviour
{
    public float startAlpha;
    public float endAlpha;
}

public class FadeMixer : PlayableBehaviour
{
    private CanvasGroup _canvasGroup;

    public override void ProcessFrame(Playable playable, FrameData info, object playerData)
    {
        _canvasGroup = playerData as CanvasGroup;
        if (_canvasGroup == null) return;

        int count = playable.GetInputCount();
        float totalWeight = 0f;
        float blendedAlpha = 0f;

        for (int i = 0; i < count; i++)
        {
            float weight = playable.GetInputWeight(i);
            if (weight <= 0f) continue;

            var inputPlayable = (ScriptPlayable<FadeBehaviour>)playable.GetInput(i);
            var b = inputPlayable.GetBehaviour();

            double duration = inputPlayable.GetDuration();
            double time     = inputPlayable.GetTime();
            float t = duration > 0 ? (float)(time / duration) : 1f;

            blendedAlpha += Mathf.Lerp(b.startAlpha, b.endAlpha, t) * weight;
            totalWeight  += weight;
        }

        if (totalWeight > 0f)
            _canvasGroup.alpha = blendedAlpha;
    }
}
