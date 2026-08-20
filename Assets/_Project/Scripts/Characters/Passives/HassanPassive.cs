using System.Collections.Generic;
using System.Linq;
using UnityEngine;

/// <summary>
/// Passive của Hassan (Mini Boss - Assassin):
/// "Zabaniya - Ảo Ảnh Tử Thần"
/// 
/// Cơ chế Afterimage (Phân Thân Ảo Ảnh):
/// - Khi combat bắt đầu: spawn 2 Afterimage vào grid enemy trống
///   + Afterimage có 1 HP, 0 ATK, không skill, không passive
///   + Chết ngay sau 1 đòn bất kỳ từ player
/// - Hassan thật không thể bị target khi còn Afterimage sống
/// - Khi hết Afterimage: Hassan lộ diện, buff ATK +50% 1 lượt
/// - Sau 2 lượt Enemy Turn: Hassan tái tạo 1 Afterimage
/// - Battle Continuation: hồi sinh 25% HP lần đầu tiên chết
///   + Nếu chưa có Afterimage, spawn 1 Afterimage mới
/// </summary>
public class HassanPassive : PassiveAbility
{
    // ── Constants ──────────────────────────────────────────────
    private const int AFTERIMAGE_SPAWN_COUNT = 2;
    private const int AFTERIMAGE_HP = 1;        // 1 HP - chết ngay sau 1 đòn
    private const int AFTERIMAGE_ATK = 1;        // Sát thương tối thiểu
    private const int AFTERIMAGE_PDEF = 0;
    private const int AFTERIMAGE_MDEF = 0;
    private const string AFTERIMAGE_NAME = "Afterimage";
    
    private const float EXPOSED_ATK_BUFF = 1.5f;     // +50% ATK khi lộ diện
    private const int EXPOSED_BUFF_DURATION = 1;     // 1 lượt
    private const int REGEN_AFTERIMAGE_TURNS = 2;    // Tái tạo sau 2 lượt enemy
    
    private const float REVIVE_HP_RATIO = 0.25f;
    private const int HASSAN_ACTIONS_PER_TURN = 2;
    
    // ── State ─────────────────────────────────────────────────
    private List<CombatUnit> afterimages = new List<CombatUnit>();
    private bool isExposed = false;         // Hassan đang lộ diện (không có afterimage)
    private bool hasRevived = false;
    private int turnsSinceExposed = 0;

    // Lưu reference để spawn view cho afterimage
    private CombatUnit originalHassan;
    private Dictionary<CombatUnit, UnitView> afterimageViews = new Dictionary<CombatUnit, UnitView>();

    public override void Initialize(CombatUnit owner)
    {
        base.Initialize(owner);
        originalHassan = owner;
        
        // Hassan hành động 2 lần/lượt
        owner.MaxActionsPerTurn = HASSAN_ACTIONS_PER_TURN;
        owner.IgnoreTaunt = true;
        
        // Đăng ký sự kiện
        Owner.OnDied += OnOwnerDied;
        Owner.OnKill += OnOwnerKilledUnit;
        
        if (CombatManager.Instance != null)
        {
            CombatManager.Instance.OnEnemyTurnEnd += OnEnemyTurnEnd;
            // Spawn afterimage sau khi combat đã khởi tạo xong (tránh modify collection đang iterate)
            CombatManager.Instance.OnCombatStarted += OnCombatStarted;
        }
        
        Debug.Log($"[HassanPassive] Zabaniya - Ảo Ảnh Tử Thần đã sẵn sàng.");
    }

    private void OnCombatStarted()
    {
        // Spawn 2 Afterimage khi combat đã khởi tạo xong hoàn toàn
        SpawnAfterimages(AFTERIMAGE_SPAWN_COUNT);
        Debug.Log($"[HassanPassive] Zabaniya - Ảo Ảnh Tử Thần kích hoạt! {AFTERIMAGE_SPAWN_COUNT} Afterimage được tạo ra.");

        // Text hiệu ứng
        var view = CombatManager.Instance?.GetUnitView(Owner);
        if (view != null)
            DamageTextManager.Instance?.ShowStatusText("ẢO ẢNH!", view.GetDamageTextPosition(), DamageTextManager.Instance.illusionColor, Vector2.up);
    }

    public override void Cleanup()
    {
        base.Cleanup();

        if (Owner != null)
        {
            Owner.OnDied -= OnOwnerDied;
            Owner.OnKill -= OnOwnerKilledUnit;
        }

        if (CombatManager.Instance != null)
        {
            CombatManager.Instance.OnEnemyTurnEnd -= OnEnemyTurnEnd;
            CombatManager.Instance.OnCombatStarted -= OnCombatStarted;
        }

        // Cleanup afterimages khỏi CombatManager
        if (CombatManager.Instance != null)
        {
            foreach (var ai in afterimages)
            {
                if (ai != null)
                {
                    // Hủy GameObject view nếu còn tồn tại
                    if (afterimageViews.TryGetValue(ai, out var view) && view != null && view.gameObject != null)
                    {
                        Object.Destroy(view.gameObject);
                    }
                    CombatManager.Instance.EnemyUnits.Remove(ai);
                }
            }

            // Xóa hết view khỏi unitViews
            var unitViewsField = typeof(CombatManager).GetField("unitViews",
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            if (unitViewsField != null)
            {
                var unitViews = unitViewsField.GetValue(CombatManager.Instance) as List<UnitView>;
                if (unitViews != null)
                {
                    foreach (var kvp in afterimageViews)
                    {
                        unitViews.Remove(kvp.Value);
                    }
                }
            }
        }
        afterimages.Clear();
        afterimageViews.Clear();
    }

    // ── Spawn Afterimage ───────────────────────────────────────
    private void SpawnAfterimages(int count)
    {
        if (CombatManager.Instance == null) return;
        
        var emptySlots = GetEmptyEnemySlots();
        if (emptySlots.Count == 0)
        {
            Debug.LogWarning("[HassanPassive] Không còn grid slot trống để spawn Afterimage!");
            return;
        }
        
        int spawned = 0;
        for (int i = 0; i < count && emptySlots.Count > 0; i++)
        {
            int randomIndex = Random.Range(0, emptySlots.Count);
            int gridSlot = emptySlots[randomIndex];
            emptySlots.RemoveAt(randomIndex);
            
            // Tạo Afterimage Unit
            var aiUnit = CreateAfterimageUnit(gridSlot);
            afterimages.Add(aiUnit);
            CombatManager.Instance.EnemyUnits.Add(aiUnit);
            
            // Spawn UnitView cho afterimage
            SpawnAfterimageView(aiUnit, gridSlot);
            
            Debug.Log($"[HassanPassive] Afterimage {spawned + 1} spawn tại grid slot {gridSlot}.");
            spawned++;
        }
        
        // Hassan không thể target khi còn afterimage
        SetHassanTargetable(false);

// Nếu combat đã vào lượt (không phải Intro/None), yêu cầu camera
        // chạy lại check để dựng khung bao gồm Afterimage vừa spawn.
        // RefitCameraToAllUnits đợi 1 frame để view đã được đăng ký xong,
        // rồi mới AutoFitUnitsInView để camera lấy đúng vị trí mọi nhân vật.
        // (Trường hợp spawn lúc bắt đầu: Intro sẽ tự fit khi vào PlayerTurn)
        if (CombatManager.Instance != null &&
            CombatManager.Instance.CurrentPhase != CombatPhase.None &&
            CombatManager.Instance.CurrentPhase != CombatPhase.Intro &&
            CombatManager.Instance.CurrentPhase != CombatPhase.Victory &&
            CombatManager.Instance.CurrentPhase != CombatPhase.Defeat)
        {
            CombatManager.Instance.RefitCameraToAllUnits();
        }
    }

    /// <summary>
    /// Lấy danh sách grid slot enemy còn trống (không có unit sống nào đang đứng).
    /// </summary>
    private List<int> GetEmptyEnemySlots()
    {
        if (CombatManager.Instance == null) return new List<int>();
        
        var filledSlots = new HashSet<int>();
        foreach (var unit in CombatManager.Instance.EnemyUnits)
        {
            if (unit != null && unit.IsAlive)
            {
                filledSlots.Add(unit.GridSlot);
            }
        }
        
        var allSlots = new List<int> { 0, 1, 2, 3, 4, 5, 6, 7, 8 };
        return allSlots.Where(s => !filledSlots.Contains(s)).ToList();
    }

    /// <summary>
    /// Tạo CombatUnit cho Afterimage với stats cực yếu.
    /// </summary>
    private CombatUnit CreateAfterimageUnit(int gridSlot)
    {
        // Clone data từ Hassan để tránh null reference
        CharacterData hassanData = Owner.Data;
        
        var aiUnit = new CombatUnit
        {
            // Dùng thẳng field để set (không qua Initialize để tránh clone skills)
            Data = hassanData,  // Gán data để tránh null ở UnitView, DoVictory, v.v.
            UnitName = AFTERIMAGE_NAME,
            IsPlayer = false,
            Level = 1,
            GridSlot = gridSlot,
            GridRow = 2 - (gridSlot / 3),
            
            MaxHP = AFTERIMAGE_HP,
            CurrentHP = AFTERIMAGE_HP,
            ATK = AFTERIMAGE_ATK,
            PDEF = AFTERIMAGE_PDEF,
            MDEF = AFTERIMAGE_MDEF,
            
            CritChance = 0f,
            CritDamage = 1.5f,
            ArmorPenetration = 0f,
            
            MaxActionsPerTurn = 0,  // Afterimage không hành động
            ActionsRemainingThisTurn = 0,
            HasActedThisTurn = true, // Đánh dấu đã act để không được AI điều khiển
            
AlwaysActsFirst = false,
            IgnoreTaunt = true,
            IsTargetable = true,
            PassiveClassName = null,
            // Afterimage là phân thân ảo ảnh: không thể dính status/buff
            // (chỉ nhận sát thương). Nhưng vẫn có thể bị tiêu diệt vì có 1 HP.
            IsImmuneToStatusEffects = true
        };
        
        // Afterimage không có skills
        aiUnit.AvailableSkills = new List<SkillData>();
        
        // Đăng ký sự kiện chết
        aiUnit.OnDied += () => OnAfterimageKilled(aiUnit);
        
        return aiUnit;
    }

    /// <summary>
    /// Spawn visual cho Afterimage (dùng prefab của Hassan nhưng alpha thấp).
    /// </summary>
    private void SpawnAfterimageView(CombatUnit aiUnit, int gridSlot)
    {
        var cm = CombatManager.Instance;
        if (cm == null) return;

        Transform gridTransform = cm.enemyGridSlots != null && gridSlot < cm.enemyGridSlots.Length
            ? cm.enemyGridSlots[gridSlot]
            : null;
        if (gridTransform == null) return;

        Vector3 spawnPos = gridTransform.position;

        // Dùng prefab của Hassan để spawn
        GameObject prefab = Owner.Data != null ? Owner.Data.prefab : null;
        if (prefab == null)
        {
            Debug.LogError("[HassanPassive] Hassan chưa có prefab!");
            return;
        }

        var go = Object.Instantiate(prefab, spawnPos, Quaternion.identity);
        UnityEngine.SceneManagement.SceneManager.MoveGameObjectToScene(go, cm.gameObject.scene);

        var view = go.GetComponent<UnitView>();
        if (view == null)
        {
            Debug.LogError("[HassanPassive] Prefab không có UnitView component!");
            Object.Destroy(go);
            return;
        }

// Setup view cho afterimage
        view.Setup(aiUnit);

        // Lưu vị trí gốc để camera AutoFitUnitsInView có thể dựng khung đủ
        // và các hệ thống khác (framing, targeting) biết vị trí của unit này.
        view.StoreOriginalPosition(spawnPos);

        // Afterimage visual: alpha thấp, hơi trong suốt
        if (view.spriteRenderer != null)
        {
            Color c = view.spriteRenderer.color;
            c.a = 0.5f;  // 50% opacity
            view.spriteRenderer.color = c;
        }

        // Đăng ký view vào CombatManager bằng public method (giống Madara clone).
        // Đảm bảo Afterimage được tính là 1 đơn vị: camera bắt được, có thể chọn,
        // và bị tiêu diệt đúng khi nhận đủ sát thương.
        cm.AddUnitView(view);

        // Lưu reference để cleanup sau này
        afterimageViews[aiUnit] = view;
    }

    // ── Afterimage chết ─────────────────────────────────────────
    private void OnAfterimageKilled(CombatUnit afterimage)
    {
        if (!afterimages.Contains(afterimage)) return;

        afterimages.Remove(afterimage);
        Debug.Log($"[HassanPassive] Afterimage bị tiêu diệt! Còn {afterimages.Count} afterimage.");

        // Chỉ xóa khỏi EnemyUnits, giữ view trong unitViews để camera continue follow bình thường
        if (CombatManager.Instance != null)
        {
            // Giữ view trong unitViews (không xóa) để camera vẫn có thể nhìn thấy animation chết
            // Chỉ xóa unit đi khỏi danh sách enemy đang sống
            CombatManager.Instance.EnemyUnits.Remove(afterimage);
        }

        // Mỗi afterimage bị phá → Hassan nhận +5% sát thương từ mọi nguồn VĨNH VIỄN
        Owner.ApplyStatus(StatusEffectType.DiemYeu, 0, 0.05f, 1);
        Debug.Log($"[HassanPassive] Hassan nhận thêm 5% sát thương từ mọi nguồn (vĩnh viễn)!");

        // Bù lại: Hassan hành động thêm 1 lần trong round hiện tại (Reset mỗi lượt enemy)
        if (CombatManager.Instance != null)
            CombatManager.Instance.GrantExtraAction(Owner);
        Debug.Log($"[HassanPassive] Hassan được thêm 1 action trong round này! (ActionsRemaining: {Owner.ActionsRemainingThisTurn})");

        if (afterimages.Count == 0)
        {
            // Hết afterimage → Hassan lộ diện
            isExposed = true;
            turnsSinceExposed = 0;
            SetHassanTargetable(true);

            // Buff ATK +50% vì giận dữ, kéo dài 1 lượt
            Owner.ApplyBuff(StatType.ATK, EXPOSED_ATK_BUFF, EXPOSED_BUFF_DURATION);

            Debug.Log($"[HassanPassive] Hassan lộ diện! ATK +50% trong {EXPOSED_BUFF_DURATION} lượt.");

            // Text hiệu ứng
            var view = CombatManager.Instance?.GetUnitView(Owner);
            if (view != null)
                DamageTextManager.Instance?.ShowStatusText("LỘ DIỆN!", view.GetDamageTextPosition(), DamageTextManager.Instance.rageColor, Vector2.up);
        }
    }

    // ── Enemy Turn End - Tái tạo Afterimage ────────────────────
    private void OnEnemyTurnEnd()
    {
        if (!isExposed || !Owner.IsAlive) return;
        
        turnsSinceExposed++;
        if (turnsSinceExposed >= REGEN_AFTERIMAGE_TURNS)
        {
            Debug.Log($"[HassanPassive] Hassan tái tạo Afterimage sau {REGEN_AFTERIMAGE_TURNS} lượt enemy!");
            
            SpawnAfterimages(1);
            isExposed = false;
            
            // Hủy buff ATK
            Owner.ApplyBuff(StatType.ATK, 1f, 0); // Reset về 1x
        }
    }

    // ── Battle Continuation: Hồi sinh 1 lần ────────────────────
    private void OnOwnerDied()
    {
        if (hasRevived) return;
        hasRevived = true;
        
        // Hồi sinh 25% HP
        int reviveHP = Mathf.RoundToInt(Owner.MaxHP * REVIVE_HP_RATIO);
        reviveHP = Mathf.Max(1, reviveHP);
        Owner.Heal(reviveHP);
        
        // Hiệu ứng hồi sinh (giống Skeleton)
        var view = CombatManager.Instance?.GetUnitView(Owner);
        if (view != null)
        {
            view.StopAllCoroutines();
            view.gameObject.SetActive(true);
            view.spriteRenderer.color = Color.white;
            view.SetAlpha(1f);
            view.UpdateHealthBar();
            view.TriggerReviveFlash(); // Hiệu ứng nháy vàng
        }
        
        Debug.Log($"[HassanPassive] Battle Continuation! Hassan hồi sinh với {reviveHP} HP.");

        // Text hiệu ứng hồi sinh
        if (view != null)
            DamageTextManager.Instance?.ShowStatusText("HỒI SINH!", view.GetDamageTextPosition(), DamageTextManager.Instance.reviveColor, Vector2.up);
        
        // Nếu chưa có afterimage, spawn 1 cái
        if (afterimages.Count == 0)
        {
            SpawnAfterimages(1);
            isExposed = false;
        }
    }

    // ── Khi Hassan giết unit (xử lý kill) ──────────────────────
    private void OnOwnerKilledUnit(CombatUnit target)
    {
        // Hassan hồi 5% máu khi giết kẻ địch
        if (target != null && !target.IsAlly(Owner))
        {
            int healAmount = Mathf.RoundToInt(Owner.MaxHP * 0.05f);
            Owner.Heal(healAmount);
            Debug.Log($"[HassanPassive] Hassan hút máu: +{healAmount} HP.");
        }
    }

    // ── Helper: Set Targetable ─────────────────────────────────
    private void SetHassanTargetable(bool targetable)
    {
        if (Owner == null) return;
        Owner.IsTargetable = targetable;
        Debug.Log($"[HassanPassive] Hassan.IsTargetable = {targetable}");
    }
}