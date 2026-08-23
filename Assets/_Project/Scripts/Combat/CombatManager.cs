using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.SceneManagement;
using Game.Combat;

namespace Game.Combat
{
    public class PlannedAction
    {
        public CombatUnit Caster { get; }
        public SkillData Skill { get; }
        public List<CombatUnit> Targets { get; }

        public PlannedAction(CombatUnit caster, SkillData skill, List<CombatUnit> targets)
        {
            Caster = caster;
            Skill = skill;
            Targets = targets;
        }
    }
}

public class CombatManager : MonoBehaviour
{
    public static CombatManager Instance { get; private set; }

    private CombatStateMachine stateMachine = new();
    private ActionResolver actionResolver = new();
    private EnemyAI enemyAI = new();
    private MadaraAI madaraAI = new();
    private GilgameshAI gilgameshAI = new();

    private readonly Queue<ICombatCommand> _commandQueue = new Queue<ICombatCommand>();
    private bool _isProcessingCommands = false;

    public List<CombatUnit> PlayerUnits { get; private set; } = new();
    public List<CombatUnit> EnemyUnits { get; private set; } = new();

    // ── AP System (Shared Pool) ───────────────────────────────
    public int CurrentPlayerAP { get; private set; }
    private const int MAX_PLAYER_AP = 5;
    private const int STARTING_PLAYER_AP = 3;

    [Header("Grid Spawn Settings")]
    public Transform[] playerGridSlots;
    public Transform[] enemyGridSlots;
    public Transform enemyRallyPoint;
    private List<UnitView> unitViews = new();
    public List<UnitView> GetAllUnitViews() { return unitViews; }

    // ── Side-Based Turn State ─────────────────────────────────
    private bool _playerEndedTurn = false;
    private bool _isWaitingForPlayerSelection = false;
    private CombatUnit _selectedUnit = null;
    private SkillData _selectedSkill = null;
    private List<CombatUnit> _selectedTargets = null;

// ── Interrupt System (Counter Attack giữa turn) ───────────
    private bool _isInterrupting = false;
    private bool _interruptPending = false;
    private CombatUnit _interruptAttacker = null;
    private CombatUnit _interruptTarget = null;

    // ── Charlotte Follow-Up System (Gió Tiên) ─────────────────
    private bool _charlotteFollowUpPending = false;
    private bool _isProcessingCharlotteFollowUp = false;
    private CombatUnit _charlotteFollowUpTarget = null;
    private const int MAX_CHARLOTTE_FOLLOW_UPS_PER_TURN = 2;
    private int _charlotteFollowUpCountThisTurn = 0;

    // ── Events ─────────────────────────────────────────────────
    public event System.Action OnCombatStarted;
    public event System.Action<List<CombatUnit>> OnPlayerTurnStart;
    public event System.Action<CombatUnit> OnUnitTurnStart;
    public event System.Action OnPlayerTurnEnd;
    public event System.Action OnEnemyTurnStart;
    public event System.Action OnEnemyTurnEnd;
    public event System.Action<ActionResult> OnActionResolved;
    public event System.Action<Dictionary<CharacterData, int>> OnVictory;
    public event System.Action OnDefeat;
    public event System.Action<int> OnAPChanged;
    public event System.Action OnIntroEnded;

public delegate void DamageModificationHandler(ActionOutcome outcome, CombatUnit actor);
    public event DamageModificationHandler OnDamageCalculation;
    public void TriggerDamageCalculation(ActionOutcome outcome, CombatUnit actor) => OnDamageCalculation?.Invoke(outcome, actor);

    // ── Debuff Applied Event (Charlotte Gió Tiên) ─────────────
    public event System.Action<CombatUnit, CombatUnit, StatusEffectType> OnDebuffApplied;
    public void TriggerDebuffApplied(CombatUnit caster, CombatUnit target, StatusEffectType status)
        => OnDebuffApplied?.Invoke(caster, target, status);

    public CombatPhase CurrentPhase => stateMachine.Current;

    private EnemyGroupData _currentEnemyGroup;

    [Header("UI & Animation")]
    public ClashAnimationSequence clashSequence;
    public CombatCameraManager cameraManager;
    public CanvasGroup combatUICanvasGroup;
    private TargetingArrowController arrowController;

    [Header("Background")]
    [Tooltip("Controller đổi background theo EnemyGroupData. Nếu để trống sẽ tự tìm trong scene.")]
    public CombatBackgroundController backgroundController;

    private void Awake()
    {
        if (Instance != null) { Destroy(gameObject); return; }
        Instance = this;
        stateMachine.OnPhaseChanged += HandlePhaseChanged;
        if (cameraManager == null) cameraManager = FindFirstObjectByType<CombatCameraManager>();

        if (combatUICanvasGroup == null)
        {
            var planningUI = FindFirstObjectByType<CombatPlanningUI>();
            if (planningUI != null && planningUI.planningCanvas != null)
            {
                combatUICanvasGroup = planningUI.planningCanvas.GetComponent<CanvasGroup>();
                if (combatUICanvasGroup == null)
                    combatUICanvasGroup = planningUI.planningCanvas.gameObject.AddComponent<CanvasGroup>();
            }
        }

        arrowController = GetComponent<TargetingArrowController>();
        if (arrowController == null) arrowController = gameObject.AddComponent<TargetingArrowController>();
    }

    private void OnDestroy()
    {
        if (stateMachine != null) stateMachine.OnPhaseChanged -= HandlePhaseChanged;
    }

    public List<CombatUnit> GetTeam(bool isPlayer) => isPlayer ? PlayerUnits : EnemyUnits;
    public List<CombatUnit> GetOpposingTeam(bool isPlayer) => isPlayer ? EnemyUnits : PlayerUnits;

    private static int SlotToRow(int slot) => 2 - (slot / 3);

    private List<CombatUnit> GetAllAliveUnits()
    {
        return PlayerUnits.Where(u => u.IsAlive)
            .Concat(EnemyUnits.Where(u => u.IsAlive))
            .ToList();
    }

    // ── START COMBAT ──────────────────────────────────────────
    public void StartCombat(FormationData playerFormation, EnemyGroupData enemyGroup)
    {
        _currentEnemyGroup = enemyGroup;
        PlayerUnits.Clear(); EnemyUnits.Clear();
        CurrentPlayerAP = STARTING_PLAYER_AP;
        OnAPChanged?.Invoke(CurrentPlayerAP);

        // Đổi background theo enemy group
        ApplyEnemyGroupBackground(enemyGroup);

        // Tạo player units
        foreach (var slot in playerFormation.slots)
        {
            if (slot?.data == null) continue;
            int level = slot.level;
            CharacterData charData = slot.data;
            int hpBonus = 0, atkBonus = 0, pdefBonus = 0, mdefBonus = 0;
            if (EquipmentManager.Instance != null)
            {
                var equipment = EquipmentManager.Instance.GetEquipment(charData);
                if (equipment != null)
                {
                    hpBonus = equipment.GetHPBonus();
                    atkBonus = equipment.GetATKBonus();
                    pdefBonus = equipment.GetPDEFBonus();
                    mdefBonus = equipment.GetMDEFBonus();
                }
            }
            var unit = new CombatUnit();
            unit.Initialize(charData, level, isPlayer: true, hpBonus, atkBonus, pdefBonus, mdefBonus);
            unit.GridRow = SlotToRow(slot.gridSlot);
            unit.GridSlot = slot.gridSlot;
            PlayerUnits.Add(unit);
        }

        // Tạo enemy units
        foreach (var entry in enemyGroup.enemies)
        {
            if (entry?.data == null) continue;
            var u = new CombatUnit();
            u.Initialize(entry.data, entry.level, isPlayer: false);
            u.GridRow = SlotToRow(entry.gridSlot);
            u.GridSlot = entry.gridSlot;
            if (entry.data.characterName == "Reinhard") u.IgnoreTaunt = true;
            EnemyUnits.Add(u);
        }

        SpawnUnitViews();
        Debug.Log($"=== COMBAT STARTED === Player:{PlayerUnits.Count} vs Enemy:{EnemyUnits.Count}");
        OnCombatStarted?.Invoke();

        if (enemyGroup.introStinger != null && AudioManager.Instance != null)
            AudioManager.Instance.PlaySFX2D(enemyGroup.introStinger, 0.8f);

        CombatAudioManager.Instance?.PlayCombatBGM(enemyGroup.combatArea, enemyGroup.bgmClip);
        stateMachine.TransitionTo(CombatPhase.Intro);
    }

    // Overload dùng List
    public void StartCombat(
        List<(CharacterData data, int level, int gridSlot)> playerSetup,
        List<(CharacterData data, int level, int gridSlot)> enemySetup)
    {
        var formation = new FormationData
        {
            slots = playerSetup.ConvertAll(p => new FormationSlot { data = p.data, level = p.level, gridSlot = p.gridSlot }).ToArray()
        };
        var enemyGroup = ScriptableObject.CreateInstance<EnemyGroupData>();
        enemyGroup.enemies = enemySetup.ConvertAll(e => new EnemyGroupData.EnemyEntry { data = e.data, level = e.level, gridSlot = e.gridSlot }).ToArray();
        StartCombat(formation, enemyGroup);
    }

    /// <summary>
    /// Đổi background combat scene theo EnemyGroupData.
    /// Nếu enemy group có backgroundImage, sẽ set vào SpriteRenderer background.
    /// </summary>
    private void ApplyEnemyGroupBackground(EnemyGroupData enemyGroup)
    {
        if (enemyGroup == null || enemyGroup.backgroundImage == null) return;

        if (backgroundController == null)
            backgroundController = FindFirstObjectByType<CombatBackgroundController>();

        if (backgroundController == null)
        {
            Debug.LogWarning("[CombatManager] Không tìm thấy CombatBackgroundController trong scene!");
            return;
        }

        backgroundController.SetBackground(enemyGroup.backgroundImage);
    }

    private void SpawnUnitViews()
    {
        foreach (var v in unitViews) if (v != null) Destroy(v.gameObject);
        unitViews.Clear();
        SpawnSide(PlayerUnits, playerGridSlots);
        SpawnSide(EnemyUnits, enemyGridSlots);
    }

    private void SpawnSide(List<CombatUnit> units, Transform[] gridSlots)
    {
        foreach (var unit in units)
        {
            var prefab = unit.Data.prefab;
            if (prefab == null) { Debug.LogError($"[CM] {unit.UnitName} chưa có prefab!"); continue; }
            int slot = Mathf.Clamp(unit.GridSlot, 0, 8);
            if (gridSlots == null || slot >= gridSlots.Length || gridSlots[slot] == null) continue;

            Vector3 finalPos = gridSlots[slot].position;
            Vector3 spawnPos = (!unit.IsPlayer && enemyRallyPoint != null) ? enemyRallyPoint.position : finalPos;

            var go = Instantiate(prefab, spawnPos, Quaternion.identity);
            SceneManager.MoveGameObjectToScene(go, gameObject.scene);
            var view = go.GetComponent<UnitView>();
            if (view == null) continue;
            view.Setup(unit);
            InitializePassives(unit);
            // Debug: verify passive đã được set
            if (unit.Passive != null)
                Debug.Log($"[CM] {unit.UnitName} passive loaded: {unit.Passive.GetType().Name}, MaxActions={unit.MaxActionsPerTurn}");
            else
                Debug.LogWarning($"[CM] {unit.UnitName} KHÔNG có passive! Kiểm tra InitializePassives.");
            view.StoreOriginalPosition(finalPos);
            unitViews.Add(view);

            // Đăng ký sự kiện kill
            unit.OnKill += (target) => {
                if (target != null && !target.IsPlayer && target.Data != null)
                {
                    if (unit.IsPlayer)
                    {
                        Debug.Log($"[CombatManager] Player {unit.UnitName} killed {target.UnitName}");
                        QuestManager.Instance?.OnEnemyDefeated(target.Data.characterName);
                    }
                }
            };
        }
    }

    // Mapping fallback: tên nhân vật → tên passive class tương ứng.
    // Dùng khi passiveScript bị null/missing trên CharacterData asset.
    private static readonly Dictionary<string, string> PassiveNameFallback = new Dictionary<string, string>
    {
        { "Kurumi", "CharlottePassive" },   // Kurumi dùng passive của Charlotte (Gió Tiên)
        { "Charlotte", "CharlottePassive" }
    };

    private void InitializePassives(CombatUnit unit)
    {
        // Thử lấy className từ passiveScript (nếu MonoScript còn tồn tại)
        string className = null;

        if (unit.Data.passiveScript != null)
        {
            className = unit.Data.passiveScript.name;
        }

        // Fallback 1: nếu MonoScript bị null (Unity fake null) hoặc broken reference (name rỗng),
        // tra cứu mapping theo tên nhân vật.
        if (string.IsNullOrEmpty(className))
        {
            if (PassiveNameFallback.TryGetValue(unit.UnitName, out var mapped))
            {
                className = mapped;
                Debug.Log($"[CM] passiveScript cho {unit.UnitName} bị missing. Dùng mapping fallback: '{mapped}'");
            }
            else
            {
                className = unit.UnitName + "Passive";
                Debug.Log($"[CM] passiveScript cho {unit.UnitName} bị missing, fallback: '{className}'");
            }
        }

        var passiveType = System.Type.GetType(className);
        if (passiveType == null)
            foreach (var asm in System.AppDomain.CurrentDomain.GetAssemblies())
            { passiveType = asm.GetType(className); if (passiveType != null) break; }

        if (passiveType != null && typeof(PassiveAbility).IsAssignableFrom(passiveType))
        {
            var instance = System.Activator.CreateInstance(passiveType) as PassiveAbility;
            if (instance != null)
            {
                unit.SetPassive(instance);
                Debug.Log($"[CM] Đã khởi tạo passive '{className}' cho {unit.UnitName}");
            }
        }
        else
        {
            Debug.LogWarning($"[CM] Không tìm thấy passive class '{className}' cho {unit.UnitName}. Bỏ qua.");
        }
    }

    private void HandlePhaseChanged(CombatPhase prev, CombatPhase next)
    {
        switch (next)
        {
            case CombatPhase.Intro: StartCoroutine(DoIntro()); break;
            case CombatPhase.PlayerTurn: StartCoroutine(DoPlayerTurn()); break;
            case CombatPhase.EnemyTurn: StartCoroutine(DoEnemyTurn()); break;
            case CombatPhase.Victory: DoVictory(); break;
            case CombatPhase.Defeat: DoDefeat(); break;
        }
    }

    // ── INTRO ─────────────────────────────────────────────────
    private IEnumerator DoIntro()
    {
        cameraManager.BeginIntroSequence();
        yield return FadeUI(0f, 0.5f);

        Vector3 center = Vector3.zero; int count = 0;
        if (enemyGridSlots != null)
            foreach (var s in enemyGridSlots) { if (s != null) { center += s.position; count++; } }
        if (count > 0) { center /= count; yield return cameraManager.FadeInAndSetPosition(center, 7.5f, Vector3.left * 20f, 1.0f); }
        else yield return cameraManager.FadeInAndSetPosition(Vector3.zero, 10f, Vector3.zero, 0f);

        yield return new WaitForSeconds(0.75f);
        StartCoroutine(cameraManager.ZoomOutToFinalView(2.0f));

        // Lọc enemy views - bỏ qua Afterimage (clone) để camera không focus vào chúng
        var enemyViews = unitViews.Where(v => v.LinkedUnit != null && !v.LinkedUnit.IsPlayer && v.LinkedUnit.UnitName != "Afterimage").ToList();
        if (enemyViews.Count > 0)
        {
            var leader = enemyViews[enemyViews.Count / 2]; enemyViews.Remove(leader);
            yield return MoveUnitToPosition(leader, leader.GetOriginalPosition(), 0.5f);
            yield return new WaitForSeconds(0.2f);
            foreach (var f in enemyViews)
                yield return MoveUnitToPosition(f, f.GetOriginalPosition(), Random.Range(0.4f, 0.6f));
        }

        yield return new WaitForSeconds(2.0f);
        yield return FadeUI(1f, 0.5f);
        cameraManager.EndIntroSequence();
        OnIntroEnded?.Invoke();

        // Kiểm tra enemy nào có AlwaysActsFirst (ví dụ: Sói)
        // Những enemy này sẽ act trước cả player
        var firstActors = EnemyUnits.Where(e => e.IsAlive && e.AlwaysActsFirst).ToList();
        if (firstActors.Count > 0)
        {
            Debug.Log("=== ALWAYS ACTS FIRST PHASE ===");
            foreach (var actor in firstActors)
            {
                // Reset action tracking và trigger turn start
                actor.ActionsRemainingThisTurn = actor.MaxActionsPerTurn;
                actor.HasActedThisTurn = true;

                // Handle start-of-turn effects
                yield return HandleStartOfTurnEffects(actor);
                if (!actor.IsAlive) continue;

                OnUnitTurnStart?.Invoke(actor);
                actor.TriggerTurnStart();

                // AI chọn skill + target
                GetAIForEnemy(actor).PlanTurn(actor, PlayerUnits);

                if (actor.SelectedSkill != null && actor.SelectedTargets.Any())
                {
                    yield return ResolveAction(new PlannedAction(actor, actor.SelectedSkill, actor.SelectedTargets));
                    if (CheckForCombatEnd()) yield break;
                }

                // TickStatuses sau khi act
                TickAllStatuses();
                if (CheckForCombatEnd()) yield break;
            }
            Debug.Log("=== END ALWAYS ACTS FIRST PHASE ===");
        }

        // Bắt đầu lượt Player
        stateMachine.TransitionTo(CombatPhase.PlayerTurn);
    }

    private IEnumerator FadeUI(float target, float duration)
    {
        if (combatUICanvasGroup == null) yield break;
        float start = combatUICanvasGroup.alpha, elapsed = 0f;
        while (elapsed < duration) { elapsed += Time.deltaTime; combatUICanvasGroup.alpha = Mathf.Lerp(start, target, elapsed / duration); yield return null; }
        combatUICanvasGroup.alpha = target;
    }

    private IEnumerator MoveUnitToPosition(UnitView view, Vector3 target, float duration)
    {
        view.SetAnimationTrigger("Rush");
        Vector3 start = view.transform.position; float elapsed = 0f;
        while (elapsed < duration) { elapsed += Time.deltaTime; view.transform.position = Vector3.Lerp(start, target, (elapsed / duration) * (elapsed / duration)); yield return null; }
        view.transform.position = target;
        view.SetAnimationTrigger("Idle");
    }

    // ── PLAYER TURN (Side-Based) ──────────────────────────────
    private IEnumerator DoPlayerTurn()
    {
        Debug.Log("=== PLAYER TURN ===");

// Reset AP mỗi đầu lượt Player
        CurrentPlayerAP = STARTING_PLAYER_AP;
        OnAPChanged?.Invoke(CurrentPlayerAP);

        // Reset Charlotte follow-up counter mỗi đầu lượt player
        _charlotteFollowUpCountThisTurn = 0;
        _charlotteFollowUpPending = false;
        _charlotteFollowUpTarget = null;

// Reset HasActedThisTurn và ActionsRemainingThisTurn cho tất cả player units
        foreach (var unit in PlayerUnits)
        {
            unit.HasActedThisTurn = false;
            unit.ActionsRemainingThisTurn = unit.MaxActionsPerTurn;
        }

        // Xử lý burn (ThieuDot) đầu lượt player: đối tượng bị burn sẽ
        // chịu sát thương ngay trong lượt của chính nó.
        foreach (var unit in PlayerUnits.Where(u => u.IsAlive && u.HasStatus(StatusEffectType.ThieuDot)).ToList())
        {
            yield return HandleStartOfTurnEffects(unit);
        }
        if (CheckForCombatEnd()) yield break;

        _playerEndedTurn = false;

        // Vòng lặp: player chọn unit → chọn skill → target → resolve
        while (!_playerEndedTurn)
        {
            // Lấy danh sách unit còn có thể act (còn sống, chưa act, không bị Stun)
            var unitsCanAct = PlayerUnits.Where(u => u.IsAlive && !u.HasActedThisTurn && !u.HasStatus(StatusEffectType.Stun)).ToList();
            if (unitsCanAct.Count == 0)
            {
                Debug.Log("[PlayerTurn] Không còn unit nào có thể hành động.");
                break;
            }

            // Chờ player chọn unit + skill + target
            _isWaitingForPlayerSelection = true;
            _selectedUnit = null;
            _selectedSkill = null;
            _selectedTargets = null;

            // Gửi danh sách unit có thể act để UI cho player chọn
            OnPlayerTurnStart?.Invoke(unitsCanAct);

            yield return new WaitUntil(() => !_isWaitingForPlayerSelection);

            if (_playerEndedTurn) break;
            if (_selectedUnit == null || _selectedSkill == null || _selectedTargets == null) continue;

            // Kiểm tra AP
            if (_selectedSkill.apCost > CurrentPlayerAP)
            {
                Debug.LogWarning($"[AP] Không đủ AP! Cần {_selectedSkill.apCost}, có {CurrentPlayerAP}");
                _isWaitingForPlayerSelection = true;
                continue;
            }

            // Trừ AP
            CurrentPlayerAP -= _selectedSkill.apCost;
            OnAPChanged?.Invoke(CurrentPlayerAP);
            _selectedUnit.SpendAP(_selectedSkill.apCost);

            // Chọn skill + target
            _selectedUnit.SelectSkill(_selectedSkill, _selectedTargets);

            // Resolve action
            yield return ResolveAction(new PlannedAction(_selectedUnit, _selectedSkill, _selectedTargets));

            // Kiểm tra kết thúc combat
            if (CheckForCombatEnd()) yield break;

            // Giảm số action còn lại sau mỗi lần act.
            // Mặc định ActionsRemainingThisTurn = 1 → sau action đầu tiên giảm xuống 0.
            // Nếu unit được GrantExtraAction (vd: NicholasPassive kết liễu kẻ địch → extra turn),
            // ActionsRemainingThisTurn sẽ > 1 → sau action đầu tiên vẫn còn > 0 → GIỮ
            // HasActedThisTurn = false để unit được chọn hành động thêm lần nữa.
            _selectedUnit.ActionsRemainingThisTurn--;

            // Đánh dấu unit đã act nếu không còn action nào.
            if (_selectedUnit.ActionsRemainingThisTurn <= 0)
                _selectedUnit.HasActedThisTurn = true;

// Kiểm tra interrupt (Reinhard phản đòn)
            if (_interruptPending)
            {
                yield return ProcessInterrupt();
                if (CheckForCombatEnd()) yield break;
            }

            // Xử lý Charlotte follow-up (nhảy lượt + dùng skill 1 ngay)
            if (_charlotteFollowUpPending)
            {
                yield return ProcessCharlotteFollowUp();
                if (CheckForCombatEnd()) yield break;
            }

            // Nếu skill có doesNotEndTurn, unit đó vẫn có thể act tiếp
            if (_selectedSkill.doesNotEndTurn)
            {
                _selectedUnit.HasActedThisTurn = false;
                _selectedUnit.ClearSelection();
            }
        }

        // Reset HasActedThisTurn cho lượt sau
        foreach (var unit in PlayerUnits) unit.HasActedThisTurn = false;

        // Clear Stun cho tất cả player units khi player turn kết thúc
        foreach (var unit in PlayerUnits)
        {
            if (unit.HasStatus(StatusEffectType.Stun))
            {
                unit.ClearStatus(StatusEffectType.Stun);
                var view = GetUnitView(unit);
                if (view != null) view.SetStunVisual(false);
                Debug.Log($"[CombatManager] {unit.UnitName} hết Stun sau khi player turn kết thúc.");
            }
        }

        Debug.Log("=== END PLAYER TURN ===");
        OnPlayerTurnEnd?.Invoke();
        stateMachine.TransitionTo(CombatPhase.EnemyTurn);
    }

    // ── ENEMY TURN (Side-Based) ───────────────────────────────
    private IEnumerator DoEnemyTurn()
    {
        Debug.Log("=== ENEMY TURN ===");
        OnEnemyTurnStart?.Invoke();

        // Reset HasActedThisTurn và ActionsRemainingThisTurn cho tất cả enemy units
        foreach (var unit in EnemyUnits)
        {
            unit.HasActedThisTurn = false;
            unit.ActionsRemainingThisTurn = 0;
        }

        // Lần lượt từng enemy hành động
        foreach (var enemy in EnemyUnits.Where(e => e.IsAlive))
        {
            // Reset multi-action counter cho enemy này
            // Giữ lại extra action đã tích lũy từ player turn (GrantExtraAction)
            // Cộng dồn với MaxActionsPerTurn để có tổng số action trong turn này
            int extraFromPlayerTurn = Mathf.Max(0, enemy.ActionsRemainingThisTurn);
            enemy.ActionsRemainingThisTurn = enemy.MaxActionsPerTurn + extraFromPlayerTurn;
            Debug.Log($"[EnemyTurn] {enemy.UnitName}: MaxActions={enemy.MaxActionsPerTurn}, Extra={extraFromPlayerTurn}, Total={enemy.ActionsRemainingThisTurn}");
            enemy.HasActedThisTurn = true;

            bool firstAction = true;

            // Vòng lặp multi-action: boss có thể act nhiều lần
            while (enemy.CanActThisTurn)
            {
                // Handle start-of-turn effects (burn, etc.) - chỉ lần đầu tiên
                if (firstAction)
                {
                    yield return HandleStartOfTurnEffects(enemy);
                    if (!enemy.IsAlive)
                    {
                        if (CheckForCombatEnd()) yield break;
                        break;
                    }

                    OnUnitTurnStart?.Invoke(enemy);
                    enemy.TriggerTurnStart();
                    firstAction = false;
                }

                // AI chọn skill + target
                GetAIForEnemy(enemy).PlanTurn(enemy, PlayerUnits);

                if (enemy.SelectedSkill != null && enemy.SelectedTargets.Any())
                {
                    yield return ResolveAction(new PlannedAction(enemy, enemy.SelectedSkill, enemy.SelectedTargets));
                    if (CheckForCombatEnd()) yield break;
                }

                // Giảm số action còn lại
                enemy.ActionsRemainingThisTurn--;

                // TickStatuses sau mỗi act
                TickAllStatuses();
                if (CheckForCombatEnd()) yield break;
            }
        }

        Debug.Log("=== END ENEMY TURN ===");
        OnEnemyTurnEnd?.Invoke();

        // Quay lại PlayerTurn
        stateMachine.TransitionTo(CombatPhase.PlayerTurn);
    }

    // ── TickStatuses cho tất cả unit ──────────────────────────
    private void TickAllStatuses()
    {
        foreach (var unit in GetAllAliveUnits())
        {
            unit.TickStatuses();
        }
    }

    // ── Interrupt System (Counter Attack giữa turn) ───────────
    /// <summary>
    /// Enemy gọi hàm này từ passive khi muốn đánh trả ngay lập tức.
    /// Sẽ dừng player turn hiện tại, cho enemy act 1 lần, rồi resume.
    /// </summary>
    public void RequestInterrupt(CombatUnit attacker, CombatUnit target)
    {
        if (_isInterrupting) return; // Chống stack interrupt
        if (CurrentPhase != CombatPhase.PlayerTurn) return; // Chỉ trong player turn

        _interruptPending = true;
        _interruptAttacker = attacker;
        _interruptTarget = target;
        Debug.Log($"[Interrupt] {attacker.UnitName} kích hoạt phản đòn! Sẽ đánh ngay sau hành động hiện tại.");
    }

    /// <summary>
    /// Xử lý interrupt: enemy act 1 lần ngay giữa lượt player.
    /// </summary>
    private IEnumerator ProcessInterrupt()
    {
        if (!_interruptPending || _interruptAttacker == null || !_interruptAttacker.IsAlive)
        {
            _interruptPending = false;
            yield break;
        }

        _isInterrupting = true;
        _interruptPending = false;

        CombatUnit enemy = _interruptAttacker;
        CombatUnit target = _interruptTarget;

        Debug.Log($"[Interrupt] {enemy.UnitName} phản đòn {target?.UnitName}!");

        // AI chọn skill (nếu enemy không có skill hợp lệ, dùng skill đầu tiên)
        GetAIForEnemy(enemy).PlanTurn(enemy, PlayerUnits);

        if (enemy.SelectedSkill != null && enemy.SelectedTargets.Any())
        {
            yield return ResolveAction(new PlannedAction(enemy, enemy.SelectedSkill, enemy.SelectedTargets));
        }
        else
        {
            // Fallback: tấn công thẳng vào target
            var fallbackSkill = enemy.AvailableSkills.FirstOrDefault();
            if (fallbackSkill == null)
            {
                Debug.LogWarning($"[Interrupt] {enemy.UnitName} không có skill nào để phản đòn!");
            }
            else
            {
                enemy.SelectSkill(fallbackSkill, new List<CombatUnit> { target ?? enemy });
                yield return ResolveAction(new PlannedAction(enemy, fallbackSkill, new List<CombatUnit> { target ?? enemy }));
            }
        }

        TickAllStatuses();
        CheckForCombatEnd();

_isInterrupting = false;
        Debug.Log($"[Interrupt] {enemy.UnitName} kết thúc phản đòn. Player turn tiếp tục.");
    }

    // ── Charlotte Follow-Up System (Gió Tiên) ─────────────────
    /// <summary>
    /// Charlotte passive gọi hàm này khi một đồng minh áp debuff lên kẻ địch.
    /// Charlotte sẽ nhảy lượt (bỏ qua lượt chọn của chính cô) và ngay lập tức
    /// dùng skill 1 (Cắt Gió) vào đúng kẻ địch vừa nhận debuff.
    /// </summary>
public void RequestCharlotteFollowUp(CombatUnit target)
    {
        if (target == null || !target.IsAlive) return;
        if (_isProcessingCharlotteFollowUp) return; // Chống stack
        if (_charlotteFollowUpCountThisTurn >= MAX_CHARLOTTE_FOLLOW_UPS_PER_TURN) return;

        _charlotteFollowUpPending = true;
        _charlotteFollowUpTarget = target;
        Debug.Log($"[CharlotteFollowUp] Charlotte chuẩn bị nhảy lượt để tấn công {target.UnitName}!");

        // KHÔNG set HasActedThisTurn ở đây.
        // Lý do: nếu Charlotte tự gây debuff (vd: skill 3 Bão Cắt), việc set HasActedThisTurn
        // ngay lập tức sẽ làm UI reset giữa chừng skill → cả 2 skill xử lý cùng lúc.
        // HasActedThisTurn chỉ được set khi follow-up THỰC SỰ bắt đầu (trong ProcessCharlotteFollowUp).
    }

    /// <summary>
    /// Tìm unit có passive Charlotte. Tên nhân vật trong data có thể là "Kurumi",
    /// nên tìm theo kiểu passive thay vì theo tên.
    /// </summary>
    private CombatUnit GetCharlotteUnit()
    {
        return PlayerUnits.FirstOrDefault(p => p.IsAlive && p.Passive is CharlottePassive);
    }

    /// <summary>
    /// Xử lý follow-up: Charlotte dùng skill 1 ngay lập tức vào mục tiêu đã bị debuff.
    /// </summary>
private IEnumerator ProcessCharlotteFollowUp()
    {
        if (!_charlotteFollowUpPending || _charlotteFollowUpTarget == null || !_charlotteFollowUpTarget.IsAlive)
        {
            _charlotteFollowUpPending = false;
            yield break;
        }

        _isProcessingCharlotteFollowUp = true;
        _charlotteFollowUpPending = false;

        var charlotte = GetCharlotteUnit();
        if (charlotte == null || !charlotte.IsAlive)
        {
            _isProcessingCharlotteFollowUp = false;
            yield break;
        }

        // Đánh dấu Charlotte đã act để UI bỏ qua lượt chọn của cô trong lúc follow-up.
        // Set ở ĐÂY (khi follow-up thực sự bắt đầu) thay vì trong RequestCharlotteFollowUp,
        // để skill hiện tại (vd: skill 3) chạy xong trước khi follow-up bắt đầu.
        charlotte.HasActedThisTurn = true;
        if (charlotte.SelectedSkill != null) charlotte.ClearSelection();

        // Tìm skill 1 (Cắt Gió)
        var skill1 = charlotte.AvailableSkills.FirstOrDefault(s => s.skillName == "Cắt Gió");
        if (skill1 == null)
        {
            Debug.LogWarning($"[CharlotteFollowUp] Charlotte không tìm thấy skill 'Cắt Gió'!");
            _isProcessingCharlotteFollowUp = false;
            yield break;
        }

        Debug.Log($"[CharlotteFollowUp] Charlotte nhảy lượt và dùng [{skill1.skillName}] vào {_charlotteFollowUpTarget.UnitName}!");

        // Trừ AP (chi phí skill 1)
        if (CurrentPlayerAP >= skill1.apCost)
        {
            CurrentPlayerAP -= skill1.apCost;
            OnAPChanged?.Invoke(CurrentPlayerAP);
            charlotte.SpendAP(skill1.apCost);
        }

        // Resolve action
        charlotte.SelectSkill(skill1, new List<CombatUnit> { _charlotteFollowUpTarget });
        yield return ResolveAction(new PlannedAction(charlotte, skill1, new List<CombatUnit> { _charlotteFollowUpTarget }));

        _charlotteFollowUpCountThisTurn++;
        _isProcessingCharlotteFollowUp = false;

        // FIX: Khôi phục lượt hành động cho Charlotte — follow-up KHÔNG được làm cô ấy mất lượt.
        // Charlotte vẫn có thể chọn và hành động bình thường trong lượt này sau khi follow-up xong.
        charlotte.HasActedThisTurn = false;

        Debug.Log($"[CharlotteFollowUp] Hoàn tất follow-up #{_charlotteFollowUpCountThisTurn}. Charlotte vẫn còn lượt hành động bình thường.");
    }

    // ── Player Selection (gọi từ UI) ──────────────────────────
    /// <summary>
    /// UI gọi hàm này khi player chọn unit + skill + target.
    /// </summary>
    public void SubmitPlayerAction(CombatUnit unit, SkillData skill, List<CombatUnit> targets)
    {
        if (!_isWaitingForPlayerSelection) return;
        
        // Kiểm tra: tất cả target phải có IsTargetable = true
        foreach (var target in targets)
        {
            if (target != null && !target.IsTargetable)
            {
                Debug.LogWarning($"[CombatManager] Không thể tấn công {target.UnitName} - mục tiêu không thể target!");
                return;
            }
        }
        
        _selectedUnit = unit;
        _selectedSkill = skill;
        _selectedTargets = targets;
        _isWaitingForPlayerSelection = false;
    }

    /// <summary>
    /// UI gọi hàm này khi player bấm "End Turn".
    /// </summary>
    public void EndPlayerTurn()
    {
        if (!_isWaitingForPlayerSelection) return;
        _playerEndedTurn = true;
        _isWaitingForPlayerSelection = false;
    }

    /// <summary>
    /// Cho phép unit hành động thêm 1 lần nữa trong lượt hiện tại.
    /// Hoạt động cho cả player (HasActedThisTurn) và enemy (ActionsRemainingThisTurn).
    /// </summary>
    public void GrantExtraAction(CombatUnit unit)
    {
        if (unit != null && unit.IsAlive)
        {
            unit.HasActedThisTurn = false;
            unit.ActionsRemainingThisTurn++;
            Debug.Log($"[CombatManager] {unit.UnitName} được act thêm lần nữa! (ActionsRemaining: {unit.ActionsRemainingThisTurn})");
        }
    }

    public void SpendPlayerAP(int amount) { if (amount <= CurrentPlayerAP) { CurrentPlayerAP -= amount; OnAPChanged?.Invoke(CurrentPlayerAP); } }
    public void GainPlayerAP(int amount) { CurrentPlayerAP = Mathf.Min(CurrentPlayerAP + amount, MAX_PLAYER_AP); OnAPChanged?.Invoke(CurrentPlayerAP); }

    #region Command System
    public void AddCommand(ICombatCommand command)
    {
        _commandQueue.Enqueue(command);
        if (!_isProcessingCommands) StartCoroutine(ProcessCommandQueue());
    }
    private IEnumerator ProcessCommandQueue()
    {
        _isProcessingCommands = true;
        while (_commandQueue.Count > 0)
        {
            ICombatCommand command = _commandQueue.Dequeue();
            IEnumerator coroutine = command.Execute();
            if (coroutine != null) yield return StartCoroutine(coroutine);
        }
        _isProcessingCommands = false;
    }
    #endregion

    private IEnumerator HandleStartOfTurnEffects(CombatUnit unit)
    {
        if (unit == null || !unit.IsAlive) yield break;

        var burn = unit.GetActiveStatus(StatusEffectType.ThieuDot);
        if (burn != null)
        {
            // Sát thương burn = giá trị burn mỗi stack * số stack hiện tại.
            int dmg = Mathf.RoundToInt(burn.Value * burn.Stacks);
            dmg = Mathf.Max(1, dmg);
            // Bật cờ SuppressDamageText để UnitView hiển thị "BURN!" thay vì số damage trắng
            // (tránh trùng lặp: vừa hiển thị BURN! vừa hiển thị số trắng).
            unit.SuppressDamageText = true;
            unit.TakeDamage(null, dmg, DamageType.True);
            var view = unitViews.FirstOrDefault(v => v.LinkedUnit == unit);
            if (view != null) view.TriggerHitFlash();
            yield return new WaitForSeconds(0.5f);
        }
    }

    private IEnumerator ResolveAction(PlannedAction action)
    {
        var result = actionResolver.Resolve(action.Caster, action.Skill, action.Targets);
        OnActionResolved?.Invoke(result);

        bool hasDamage = false;
        if (action.Skill.effects != null)
        {
            foreach (var effect in action.Skill.effects)
            {
                if (effect == null) continue;
                if (effect is DamageEffect) hasDamage = true;
                else effect.Apply(action.Caster, action.Targets.ToArray());
            }
        }

        var actorView = GetUnitView(action.Caster);
        if (actorView != null && result.Outcomes.Count > 0)
            actorView.SetPendingOutcomes(result.Outcomes, action.Caster, Mathf.Max(1, action.Skill.hitCount));

        if (clashSequence != null) yield return StartCoroutine(clashSequence.PlayAction(result));
        else yield return new WaitForSeconds(1f);

        action.Caster.RaiseActionConfirmed(action.Skill, action.Targets);
        if (CheckForCombatEnd()) yield break;
    }

    public bool CheckForCombatEnd()
    {
        // Bỏ qua Afterimage (clone của Hassan) khi kiểm tra victory để tránh bị kẹt
        if (!EnemyUnits.Any(e => e.IsAlive && e.UnitName != "Afterimage"))
        {
            stateMachine.TransitionTo(CombatPhase.Victory);
            return true;
        }
        if (!PlayerUnits.Any(p => p.IsAlive)) { stateMachine.TransitionTo(CombatPhase.Defeat); return true; }
        return false;
    }

    /// <summary>
    /// Chọn AI phù hợp dựa vào tên enemy
    /// </summary>
    private EnemyAI GetAIForEnemy(CombatUnit enemy)
    {
        if (enemy == null) return enemyAI;
        switch (enemy.UnitName)
        {
            case "Madara": return madaraAI;
            case "Gilgamesh": return gilgameshAI;
            default: return enemyAI;
        }
    }

public UnitView GetUnitView(CombatUnit unit) => unitViews.Find(v => v.LinkedUnit == unit);

    /// <summary>
    /// Chạy lại camera check để dựng khung quanh tất cả đơn vị hiện tại.
    /// Được gọi khi có unit mới xuất hiện giữa combat (vd: Afterimage của Hassan
    /// hồi sinh). Chờ 1 frame để đảm bảo view đã được đăng ký xong, sau đó
    /// AutoFitUnitsInView sẽ lấy đúng vị trí của mọi nhân vật.
    /// </summary>
    public void RefitCameraToAllUnits()
    {
        StartCoroutine(RefitCameraDelayed());
    }

    private System.Collections.IEnumerator RefitCameraDelayed()
    {
        // Chờ 1 frame để view mới được Setup/AddUnitView hoàn tất.
        yield return null;

        if (cameraManager == null)
            cameraManager = FindFirstObjectByType<CombatCameraManager>();
        if (cameraManager != null)
            cameraManager.AutoFitUnitsInView();
    }
    public bool WillAttackResultInClash(CombatUnit a, CombatUnit b) => false;
    public void AddUnitView(UnitView view)
    {
        if (view != null && !unitViews.Contains(view))
            unitViews.Add(view);
    }

    private void DoVictory()
    {
        Debug.Log("=== VICTORY ===");
        if (_currentEnemyGroup != null && _currentEnemyGroup.victoryFanfare != null && AudioManager.Instance != null)
            AudioManager.Instance.PlaySFX2D(_currentEnemyGroup.victoryFanfare, 0.8f);

        // Xóa tất cả Afterimage sau khi victory để tránh lỗi camera/scene sau combat
        KillAllAfterimages();

        int totalExp = 0;
        foreach (var enemy in EnemyUnits)
        {
            // Bỏ qua Afterimage khi tính exp
            if (enemy.UnitName == "Afterimage") continue;
            int baseReward = enemy.Data != null ? enemy.Data.expReward : 100;
            int bonus = (enemy.Level - 1) * 10;
            totalExp += baseReward + bonus;
        }

        var alivePlayers = PlayerUnits.Where(p => p.IsAlive).ToList();
        if (alivePlayers.Count == 0) return;
        int expPerPlayer = totalExp / alivePlayers.Count;
        if (expPerPlayer <= 0) expPerPlayer = totalExp;

        var expGained = new Dictionary<CharacterData, int>();
        foreach (var player in alivePlayers) expGained[player.Data] = expPerPlayer;
        OnVictory?.Invoke(expGained);
    }

    private void KillAllAfterimages()
    {
        if (CombatManager.Instance == null) return;
        foreach (var afterimage in EnemyUnits.Where(e => e.UnitName == "Afterimage").ToList())
        {
            if (afterimage != null)
            {
                afterimage.CurrentHP = 0;
                // Xóa view nếu có
                var view = GetUnitView(afterimage);
                if (view != null)
                {
                    if (view.gameObject != null)
                        Object.Destroy(view.gameObject);
                    unitViews.Remove(view);
                }
            }
        }
        // Xóa khỏi danh sách
        EnemyUnits.RemoveAll(e => e.UnitName == "Afterimage");
        Debug.Log("[CombatManager] Đã xóa tất cả Afterimage sau Victory.");
    }

    private void DoDefeat()
    {
        Debug.Log("=== DEFEAT ===");
        OnDefeat?.Invoke();
    }
}
