using UnityEngine;

[CreateAssetMenu(fileName = "NewSkill", menuName = "RPG/Skill")]
public class SkillData : ScriptableObject
{
    [Header("Identity")]
    public string skillName;
    [TextArea]
    public string description;
    public Sprite icon;

    [Header("Type")]
    public SkillType type = SkillType.Auto;     // Không còn clash, mặc định Auto
    public TargetType targetType = TargetType.SingleEnemy;

    [Header("Cost")]
    [Tooltip("Chi phí Action Point để sử dụng skill này.")]
    public int apCost = 1;

    [Header("Hit Settings")]
    [Tooltip("Số lần gây sát thương (hiển thị nhiều số)")]
    public int hitCount = 1;

    [Header("Animation")]
    public string animationTrigger;
    public GameObject vfxPrefab;
    public float vfxOffset = 0f;

    [Header("Effects")]
    public SkillEffect[] effects;
}