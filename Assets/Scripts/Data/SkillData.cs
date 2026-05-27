using UnityEngine;

public enum SkillMovementOverride
{
    InheritFromCharacter, // Dùng cài đặt của CharacterData
    ForceRushToTarget,    // Luôn lao đến mục tiêu
    ForceStationary       // Luôn đứng yên
}

[CreateAssetMenu(fileName = "NewSkill", menuName = "RPG/Skill")]
public class SkillData : ScriptableObject
{
    [Tooltip("Kỹ năng này có tự động xác nhận ngay khi được chọn không? (Hữu ích cho các skill buff không cần chọn mục tiêu)")]
    public bool autoConfirmOnSelect = false;

    [Header("Behavior")]
    [Tooltip("Ghi đè hành vi di chuyển mặc định của nhân vật.")]
    public SkillMovementOverride movementOverride = SkillMovementOverride.InheritFromCharacter;

    [Header("Identity")]
    public string skillName;
    [TextArea]
    public string description;
    public Sprite icon;

    [Header("Type")]
    public SkillType type = SkillType.Auto;     // Không còn clash, mặc định Auto
    public TargetType targetType = TargetType.SingleEnemy;
    public bool isChargeable = false;
    public bool doesNotEndTurn = false;

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