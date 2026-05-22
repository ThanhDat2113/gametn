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
    public SkillType type = SkillType.Clash;
    public TargetType targetType = TargetType.SingleEnemy;

    [Header("Clash Settings")]
    [Tooltip("Chỉ dùng khi type = Clash")]
    public int basePoint = 4;

    [Header("Cost")]
    [Tooltip("Chi phí Action Point để sử dụng skill này.")]
    public int apCost = 1;

    [Header("Hit Settings")]

    [Header("Animation")]
    [Tooltip("Tên Trigger trong Animator. VD: Skill1, Skill2...")]
    public string animationTrigger;
    public GameObject vfxPrefab;
    public float vfxOffset = 0f;

    [Header("Effects")]
    public SkillEffect[] effects;
}