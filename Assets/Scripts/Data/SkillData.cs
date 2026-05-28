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

    [Header("Animation")]
    public string animationTrigger;

    [Header("VFX Events - Gắn Animation Event OnSpawnVFX với Int = index")]
    public VFXEvent[] vfxEvents;

    // backward compatibility (vẫn giữ để không lỗi skill cũ)
    [HideInInspector] public GameObject vfxPrefab;
    [HideInInspector] public float vfxOffset = 0f;

    [Header("Effects")]
    public SkillEffect[] effects;
}