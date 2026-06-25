using UnityEngine;

[RequireComponent(typeof(DialogueTrigger))]
[DisallowMultipleComponent]
public class QuestMarkerBridge : MonoBehaviour
{
    [Header("Marker Position")]
    [SerializeField] private Transform headPosition;
    [SerializeField] private float defaultHeadOffset = 2f;

    [Header("Player Reference")]
    [SerializeField] private Transform playerTransform;

    private DialogueTrigger _trigger;
    private bool _markerActive = false;

    public Vector3 MarkerPosition =>
        headPosition != null
            ? headPosition.position
            : transform.position + Vector3.up * defaultHeadOffset;

    public string TriggerID => _trigger != null ? _trigger.triggerID : string.Empty;
    public Transform PlayerTransform => playerTransform;

    private void Awake()
    {
        _trigger = GetComponent<DialogueTrigger>();

        if (playerTransform == null)
        {
            GameObject p = GameObject.FindGameObjectWithTag("Player");
            if (p != null) playerTransform = p.transform;
            else Debug.LogWarning("[QuestMarkerBridge] Không tìm thấy object tag 'Player'!");
        }

        Debug.Log($"[QuestMarkerBridge] Awake — object: '{gameObject.name}', triggerID: '{TriggerID}', player: {(playerTransform != null ? playerTransform.name : "NULL")}");
    }

    private void Start()
    {
        Debug.Log($"[QuestMarkerBridge] Start — QuestManager: {(QuestManager.Instance != null ? "OK" : "NULL")}");

        if (QuestManager.Instance == null)
        {
            Debug.LogError($"[QuestMarkerBridge] QuestManager.Instance is NULL trên '{gameObject.name}'!");
            return;
        }

        QuestManager.Instance.OnStepChanged.AddListener(OnStepChanged);
        QuestManager.Instance.OnStepCompleted.AddListener(OnStepCompleted);

        // Kiểm tra step hiện tại ngay
        var currentStep = QuestManager.Instance.CurrentStep;
        Debug.Log($"[QuestMarkerBridge] CurrentStep: {(currentStep != null ? $"type={currentStep.type} targetId='{currentStep.targetId}' completed={currentStep.isCompleted}" : "NULL")}");
        Debug.Log($"[QuestMarkerBridge] My TriggerID: '{TriggerID}'");
        Debug.Log($"[QuestMarkerBridge] Match: {(currentStep != null && currentStep.type == QuestStepType.Talk && currentStep.targetId == TriggerID)}");

        EvaluateCurrentStep(currentStep);
    }

    private void OnDestroy()
    {
        SetMarker(false);
        if (QuestManager.Instance == null) return;
        QuestManager.Instance.OnStepChanged.RemoveListener(OnStepChanged);
        QuestManager.Instance.OnStepCompleted.RemoveListener(OnStepCompleted);
    }

    private void OnStepChanged(QuestStep step)
    {
        Debug.Log($"[QuestMarkerBridge] OnStepChanged → type={step?.type} targetId='{step?.targetId}' | myID='{TriggerID}'");
        EvaluateCurrentStep(step);
    }

    private void OnStepCompleted(QuestStep step)
    {
        if (step != null && step.type == QuestStepType.Talk && step.targetId == TriggerID)
            SetMarker(false);
    }

    private void EvaluateCurrentStep(QuestStep step)
    {
        bool shouldShow = step != null
                       && step.type == QuestStepType.Talk
                       && step.targetId == TriggerID
                       && !step.isCompleted;

        Debug.Log($"[QuestMarkerBridge] EvaluateCurrentStep → shouldShow={shouldShow}");
        SetMarker(shouldShow);
    }

    private void SetMarker(bool active)
    {
        if (_markerActive == active) return;
        _markerActive = active;

        Debug.Log($"[QuestMarkerBridge] SetMarker({active}) — QuestMarkerManager: {(QuestMarkerManager.Instance != null ? "OK" : "NULL")}");

        if (QuestMarkerManager.Instance == null) return;
        if (active) QuestMarkerManager.Instance.RegisterBridge(this);
        else        QuestMarkerManager.Instance.UnregisterBridge(this);
    }

#if UNITY_EDITOR
    private void OnDrawGizmosSelected()
    {
        Gizmos.color = _markerActive ? Color.green : Color.gray;
        Gizmos.DrawWireSphere(MarkerPosition, 0.3f);
        UnityEditor.Handles.Label(MarkerPosition + Vector3.up * 0.4f,
            $"Marker: {(_markerActive ? "ON" : "OFF")}\nID: {TriggerID}");
    }
#endif
}