using UnityEngine;

public enum VFXSpawnMode
{
    AtCaster,       // Spawn at the character using the skill
    AtTarget,       // Spawn at the primary target (or center point for AoE)
    HitOnEachTarget // Spawn on each target as they are hit
}

[System.Serializable]
public class VFXEvent
{
    public GameObject vfxPrefab;
    public VFXSpawnMode spawnMode = VFXSpawnMode.AtTarget; // default backward-compat
    public Vector3 offset = Vector3.up * 1.5f;
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
    
    [Header("VFX")]
    public VFXEvent[] vfxEvents; // This is now the main array for all VFX

    // HIDE: Old fields are hidden but kept for backward compatibility
    [HideInInspector, Header("DEPRECATED: Use vfxEvents with 'AtCaster' mode")]
    public VFXEvent[] rangedVfxEvents;
    
    [HideInInspector, Header("DEPRECATED: Use vfxEvents with 'HitOnEachTarget' mode")]
    public VFXEvent[] hitVfxEvents;

    [Header("Audio")]
    public AudioClip[] sfxClips;  // SFX cho skill, mỗi hit 1 clip (nếu có)

    [Header("Animation")]
    public string animationTrigger;
    
    // Ranged skill visual
    public bool isRanged = false; // Set true for skills that fire a projectile
    public GameObject projectilePrefab; // Prefab for the projectile effect
    public Vector3 projectileOffset = Vector3.zero; // Offset from caster when spawning projectile
    public float projectileTravelTime = 0.3f; // Duration of projectile travel

    // backward compatibility (vẫn giữ để không lỗi skill cũ)
    [HideInInspector] public GameObject vfxPrefab;
    [HideInInspector] public float vfxOffset = 0f;

    [Header("Effects")]
    public SkillEffect[] effects;
}