using UnityEngine;

[System.Serializable]
public class VFXEvent
{
    public GameObject vfxPrefab;
    public Vector3 offset = Vector3.up * 1.5f; // offset x,y,z so với caster
    public bool attachToCaster = false;
}

public enum SkillMovementOverride
{
    InheritFromCharacter,
    ForceRushToTarget,
    ForceStationary
}

[CreateAssetMenu(fileName = "NewSkill", menuName = "RPG/Skill")]
public class SkillData : ScriptableObject
{
    public bool autoConfirmOnSelect = false;

    [Header("Behavior")]
    public SkillMovementOverride movementOverride = SkillMovementOverride.InheritFromCharacter;

    [Header("Identity")]
    public string skillName;
    [TextArea] public string description;
    public Sprite icon;

    [Header("Type")]
    public SkillType type = SkillType.Auto;
    public TargetType targetType = TargetType.SingleEnemy;
    public bool isChargeable = false;
    public bool doesNotEndTurn = false;

    [Header("Cost")]
    public int apCost = 1;

    [Header("Hit Settings")]
    public int hitCount = 1;
    // VFX and ranged visual settings
    [Header("VFX")]
    public VFXEvent[] vfxEvents;
    [Header("Ranged VFX")]
public VFXEvent[] rangedVfxEvents;
[Header("Animation")]
    public string animationTrigger;
    // Ranged skill visual
    public bool isRanged = false; // Set true for skills that fire a projectile
    public GameObject projectilePrefab; // Prefab for the projectile effect
    public Vector3 projectileOffset = Vector3.zero; // Offset from caster when spawning projectile
    public float projectileTravelTime = 0.3f; // Duration of projectile travel

    
    [Header("Hit VFX Events - VFX xuất hiện trên mục tiêu khi bị trúng (giống cơ chế vfxEvents)")]
    public VFXEvent[] hitVfxEvents;

    // backward compatibility (vẫn giữ để không lỗi skill cũ)
    [HideInInspector] public GameObject vfxPrefab;
    [HideInInspector] public float vfxOffset = 0f;

    [Header("Effects")]
    public SkillEffect[] effects;
}