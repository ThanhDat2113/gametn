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

/// <summary>
/// 🔥 THÊM: Chế độ phát SFX cho skill.
/// - OnHit: SFX phát theo animation event OnHit (mỗi hit phát 1 SFX).
/// - OnSpawnVFX: SFX phát theo animation event OnSpawnVFX (mỗi VFX spawn phát 1 SFX).
/// </summary>
public enum SFXTriggerMode
{
    OnHit,          // SFX phát theo OnHit event
    OnSpawnVFX      // SFX phát theo OnSpawnVFX event
}

/// <summary>
/// Chế do spawn VFX cho skill (tuong tu SFXTriggerMode).
/// - OnHit: VFX spawn theo animation event OnHit (moi hit spawn 1 VFX).
/// - OnSpawnVFX: VFX spawn theo animation event OnSpawnVFX (moi VFX spawn event spawn 1 VFX).
/// </summary>
public enum VFXTriggerMode
{
    OnHit,          // VFX spawn theo OnHit event
    OnSpawnVFX      // VFX spawn theo OnSpawnVFX event
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
    [Tooltip("Chon che do spawn VFX: OnHit (theo hit event) hoac OnSpawnVFX (theo vfx spawn event).")]
    public VFXTriggerMode vfxTriggerMode = VFXTriggerMode.OnHit; // Mac dinh OnHit de giu nguyen hanh vi cu

    // HIDE: Old fields are hidden but kept for backward compatibility
    [HideInInspector, Header("DEPRECATED: Use vfxEvents with 'AtCaster' mode")]
    public VFXEvent[] rangedVfxEvents;
    
    [HideInInspector, Header("DEPRECATED: Use vfxEvents with 'HitOnEachTarget' mode")]
    public VFXEvent[] hitVfxEvents;

    [Header("Audio")]
    public AudioClip[] sfxClips;  // SFX cho skill, mỗi hit 1 clip (nếu có)
    public bool playSFX = true; // Nếu false, skill sẽ không play SFX (dùng cho passive heal, etc)
    [Tooltip("🔥 Chọn chế độ phát SFX: OnHit (theo hit event) hoặc OnSpawnVFX (theo vfx spawn event).")]
    public SFXTriggerMode sfxTriggerMode = SFXTriggerMode.OnHit; // 🔥 THÊM: Option chọn chế độ SFX
    public AudioClip chargeSound;    // SFX khi bắt đầu charge skill
    public AudioClip[] voiceLines;   // Giọng nhân vật khi dùng skill (random)

    [Header("Animation")]
    public string animationTrigger;

    [Header("Beam Camera Settings")]
    [Tooltip("Bật nếu là skill beam (luồng năng lượng liên tục, ví dụ Enuma Elish). Khi bật: camera rung lâu + càng ngày càng mạnh.")]
    public bool isBeam = false;
    [Tooltip("Cường độ rung ban đầu (khi beam bắt đầu).")]
    public float beamShakeBaseIntensity = 0.35f;
    [Tooltip("Cường độ rung tăng thêm mỗi hit — beam rung mạnh dần theo thời gian.")]
    public float beamShakeStepIntensity = 0.12f;
    [Tooltip("Thời lượng rung liên tục (giây) mỗi lần beam đánh. Dùng hitCount để duy trì.")]
    public float beamShakeDuration = 0.5f;
    [Tooltip("Tần số rung của beam.")]
    public float beamShakeFrequency = 24f;
    
    
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