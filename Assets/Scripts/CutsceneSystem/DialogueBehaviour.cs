using UnityEngine;
using UnityEngine.Playables;

public class DialogueBehaviour : PlayableBehaviour
{
    public DialogueEvent dialogueEvent;

    private PlayableDirector _director;
    private bool _hasTriggered;
    private bool _isWaiting;
    private double _pauseTime;

    public override void OnGraphStart(Playable playable)
    {
        _director = Object.FindFirstObjectByType<PlayableDirector>();
        _hasTriggered = false;
        _isWaiting = false;
    }

    public override void OnBehaviourPlay(Playable playable, FrameData info)
    {
        if (!Application.isPlaying) return;
        if (dialogueEvent == null) return;
        if (_hasTriggered) return;

        _hasTriggered = true;
        _isWaiting = true;

        // Lưu thời điểm pause
        if (_director != null && _director.state == PlayState.Playing)
        {
            _pauseTime = _director.time;
            _director.Pause();
        }

        // Bắt đầu dialogue
        DialogueController.Instance?.StartDialogue(dialogueEvent, () =>
        {
            _isWaiting = false;
            if (_director != null && _director.state == PlayState.Paused)
            {
                // Khôi phục thời gian chính xác
                _director.time = _pauseTime;
                _director.Resume();
            }
        });
    }

    public override void OnBehaviourPause(Playable playable, FrameData info)
    {
        // Reset khi clip kết thúc hoặc bị tua qua
        if (!_isWaiting)
            _hasTriggered = false;
    }

    public override void OnGraphStop(Playable playable)
    {
        _hasTriggered = false;
        _isWaiting = false;
    }
}