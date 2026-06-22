using UnityEngine;
using UnityEngine.Playables;
using UnityEngine.UI;

public class ImageFadeBehaviour : PlayableBehaviour
{
    public Sprite image;
    public float fadeInDuration = 0.3f;
    public float fadeOutDuration = 0.3f;

    private Image _trackBinding;

    public override void OnPlayableCreate(Playable playable) { }

    public override void ProcessFrame(Playable playable, FrameData info, object playerData)
    {
        _trackBinding = playerData as Image;
        if (_trackBinding == null) return;

        if (image != null)
            _trackBinding.sprite = image;

        double duration = playable.GetDuration();
        double time = playable.GetTime();

        float alpha = 1f;

        if (time < fadeInDuration)
            alpha = Mathf.Clamp01((float)(time / fadeInDuration));
        else if (time > duration - fadeOutDuration)
            alpha = Mathf.Clamp01((float)((duration - time) / fadeOutDuration));

        var color = _trackBinding.color;
        color.a = alpha;
        _trackBinding.color = color;
    }

    public override void OnBehaviourPause(Playable playable, FrameData info)
    {
        // Ẩn image khi clip kết thúc
        if (_trackBinding != null)
        {
            var color = _trackBinding.color;
            color.a = 0f;
            _trackBinding.color = color;
        }
    }
}