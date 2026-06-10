using UnityEngine;

[CreateAssetMenu(fileName = "NewQuest", menuName = "Quest/QuestData")]
public class QuestData : ScriptableObject
{
    public string questId;
    public string questName;
    public QuestStep[] steps;
    public bool isRepeatable = false;

    [Header("Phần thưởng khi hoàn thành quest")]
    public QuestReward[] rewards;
}