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

    [Header("Hit Settings")]
    [Tooltip("Số lần đánh. VD: 3 = đánh 3 lần")]
    [Min(1)]
    public int hitCount = 1;

    [Header("Cooldown")]
    [Tooltip("0 = không có cooldown")]
    public int cooldown = 0;

    [Header("Animation")]
    [Tooltip("Tên Trigger trong Animator. VD: Skill1, Skill2...")]
    public string animationTrigger;
    public GameObject vfxPrefab;
    public float vfxOffset = 0f;

    [Header("Effects")]
    public SkillEffect[] effects;
}