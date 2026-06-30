using UnityEngine;
using System.Collections.Generic;

[CreateAssetMenu(fileName = "QuestVisibilityConfig", menuName = "Quest/Visibility Config")]
public class QuestVisibilityConfig : ScriptableObject
{
    public enum ActionType
    {
        Show,   // Bật GameObject
        Hide    // Tắt GameObject
    }

    [System.Serializable]
    public class Condition
    {
        [Tooltip("ID của Quest (để trống nếu áp dụng cho quest hiện tại)")]
        public string questId;

        [Tooltip("Index của step cần kiểm tra. -1: kiểm tra toàn bộ quest đã hoàn thành.")]
        public int stepIndex = -1;

        [Tooltip("True: yêu cầu step đã hoàn thành. False: yêu cầu step CHƯA hoàn thành.")]
        public bool isCompleted = true;

        [Tooltip("Hành động thực hiện khi tất cả điều kiện thỏa mãn.")]
        public ActionType action = ActionType.Show;
    }

    public List<Condition> conditions = new List<Condition>();

    /// <summary>
    /// Kiểm tra xem tất cả điều kiện có thỏa mãn không.
    /// </summary>
    public bool EvaluateConditions(QuestManager questManager)
    {
        if (conditions == null || conditions.Count == 0)
            return false;

        foreach (var condition in conditions)
        {
            bool conditionMet = false;

            if (string.IsNullOrEmpty(condition.questId))
            {
                // Kiểm tra với quest hiện tại
                var currentQuest = questManager.CurrentQuest;
                if (currentQuest == null) return false;
                conditionMet = CheckStepCondition(currentQuest, condition.stepIndex, condition.isCompleted);
            }
            else
            {
                // Kiểm tra với quest cụ thể (có thể là quest hiện tại hoặc quest đã hoàn thành)
                // Giả định QuestManager có phương thức GetQuestById(questId)
                // Vì QuestManager chỉ lưu quest hiện tại, ta sẽ kiểm tra với currentQuest
                var currentQuest = questManager.CurrentQuest;
                if (currentQuest != null && currentQuest.questId == condition.questId)
                {
                    conditionMet = CheckStepCondition(currentQuest, condition.stepIndex, condition.isCompleted);
                }
                else
                {
                    // Nếu không phải quest hiện tại, kiểm tra xem quest đã hoàn thành trong lịch sử chưa
                    // Ở đây ta giả định QuestManager có lưu lịch sử các quest đã hoàn thành
                    conditionMet = questManager.IsQuestCompleted(condition.questId) == condition.isCompleted;
                }
            }

            if (!conditionMet)
                return false;
        }

        return true;
    }

    private bool CheckStepCondition(QuestData quest, int stepIndex, bool isCompleted)
    {
        if (stepIndex < 0)
        {
            // Kiểm tra toàn bộ quest đã hoàn thành chưa
            return QuestManager.Instance.IsQuestCompleted(quest.questId) == isCompleted;
        }
        else
        {
            if (stepIndex >= quest.steps.Length)
                return false;
            return quest.steps[stepIndex].isCompleted == isCompleted;
        }
    }
}