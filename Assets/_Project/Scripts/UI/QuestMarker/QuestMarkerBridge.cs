using UnityEngine;

/// <summary>
/// Gắn lên NPC hoặc object trong scene.
/// Khai báo bridge này thuộc về quest nào (questId) và step thứ mấy (stepIndex).
/// QuestMarkerManager sẽ tự tìm bridge khớp với quest/step đang chạy và spawn marker.
/// Không còn phụ thuộc vào DialogueTrigger hay TriggerID.
/// </summary>
[DisallowMultipleComponent]
public class QuestMarkerBridge : MonoBehaviour
{
    [Header("Quest Binding")]
    [Tooltip("questId của QuestData asset mà bridge này thuộc về.")]
    [SerializeField] private string questId;

    [Tooltip("Index của step trong QuestData.steps[] mà bridge này đại diện (bắt đầu từ 0).")]
    [SerializeField] private int stepIndex;

    [Header("Marker Position")]
    [SerializeField] private Transform headPosition;
    [SerializeField] private float defaultHeadOffset = 2f;

    /// <summary>Quest ID khai báo trong Inspector.</summary>
    public string QuestId  => questId;

    /// <summary>Step index khai báo trong Inspector.</summary>
    public int    StepIndex => stepIndex;

    /// <summary>Vị trí world-space dùng để vẽ marker (đầu NPC / object).</summary>
    public Vector3 MarkerPosition =>
        headPosition != null
            ? headPosition.position
            : transform.position + Vector3.up * defaultHeadOffset;

    /// <summary>
    /// Kiểm tra bridge này có khớp với quest + step đang chạy không.
    /// </summary>
    public bool MatchesCurrentStep(string runningQuestId, int runningStepIndex)
        => questId == runningQuestId && stepIndex == runningStepIndex;

#if UNITY_EDITOR
    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(MarkerPosition, 0.3f);
        UnityEditor.Handles.Label(
            MarkerPosition + Vector3.up * 0.5f,
            $"Quest: {questId}\nStep: {stepIndex}");
    }
#endif
}