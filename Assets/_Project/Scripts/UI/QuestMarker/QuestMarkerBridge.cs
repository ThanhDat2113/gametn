using UnityEngine;

/// <summary>
/// Gắn lên NPC hoặc object trong scene.
/// Khai báo bridge này thuộc về quest nào (questId) và step thứ mấy (stepIndex).
///
/// MỘT NPC CÓ THỂ XUẤT HIỆN Ở NHIỀU STEP: gắn nhiều QuestMarkerBridge lên cùng GameObject
/// (mỗi cái một questId/stepIndex khác nhau). DisallowMultipleComponent đã được bỏ.
/// </summary>
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

    public string  QuestId    => questId;
    public int     StepIndex  => stepIndex;

    public Vector3 MarkerPosition =>
        headPosition != null
            ? headPosition.position
            : transform.position + Vector3.up * defaultHeadOffset;

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