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

    private readonly Queue<ICombatCommand> _commandQueue = new Queue<ICombatCommand>();
    private bool _isProcessingCommands = false;

    public List<CombatUnit> PlayerUnits { get; private set; } = new();
    public List<CombatUnit> EnemyUnits { get; private set; } = new();
    public List<CombatUnit> ActionOrder { get; private set; } = new();

    public int CurrentPlayerAP { get; private set; }
    private const int MAX_PLAYER_AP = 5;
    private const int STARTING_PLAYER_AP = 3;

    [Header("Grid Spawn Settings")]
    public Transform[] playerGridSlots;
    public Transform[] enemyGridSlots;
    public Transform enemyRallyPoint;
    private List<UnitView> unitViews = new();

    public List<UnitView> GetAllUnitViews() { return unitViews; }

    public void GrantExtraTurn(CombatUnit unit)
    {
        Debug.Log($"[CombatManager] Cấp thêm lượt cho {unit.UnitName}");
        if (ActionOrder.Contains(unit)) ActionOrder.Remove(unit);
        ActionOrder.Insert(turnIndex + 1, unit);
    }

    public void GrantImmediateTurn(CombatUnit unit)
    {
        Debug.Log($"[CombatManager] Cấp lượt hành động ngay lập tức cho {unit.UnitName}");
        if (ActionOrder.Contains(unit)) ActionOrder.Remove(unit);
        ActionOrder.Insert(turnIndex + 1, unit);
    }

    public List<CombatUnit> GetTeam(bool isPlayer) => isPlayer ? PlayerUnits : EnemyUnits;
    public List<CombatUnit> GetOpposingTeam(bool isPlayer) => isPlayer ? EnemyUnits : PlayerUnits;

    private static int SlotToRow(int slot) => 2 - (slot / 3);

    private int turnIndex = 0;
    private bool isWaitingForPlayerInput = false;
    private EnemyGroupData _currentEnemyGroup;

    [Header("Animation")]
    public ClashAnimationSequence clashSequence;
    public CombatCameraManager cameraManager;
    public CanvasGroup combatUICanvasGroup;
    private TargetingArrowController arrowController;

    public event System.Action OnCombatStarted;
    public event System.Action<CombatUnit> OnPlayerUnitPlanning;

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

    public event System.Action<CombatUnit> OnPlayerTurnStart;
    public event System.Action<CombatUnit> OnUnitTurnStart;
    public event System.Action<List<CombatUnit>> OnTurnOrderUpdated;
    public event System.Action OnExecuteStarted;
    public event System.Action OnRoundEnded;
    public event System.Action<ActionResult> OnActionResolved;
    public event System.Action<Dictionary<CharacterData, int>> OnVictory;
    public event System.Action OnDefeat;
    public event System.Action OnPlanChanged;
    public event System.Action<int> OnAPChanged;

    public delegate void DamageModificationHandler(ActionOutcome outcome, CombatUnit actor);
    public event DamageModificationHandler OnDamageCalculation;
    public void TriggerDamageCalculation(ActionOutcome outcome, CombatUnit actor) => OnDamageCalculation?.Invoke(outcome, actor);
    public CombatPhase CurrentPhase => stateMachine.Current;

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

    public void RebuildActionOrder()
    {
        var alive = PlayerUnits.Where(u => u.IsAlive).Concat(EnemyUnits.Where(u => u.IsAlive));
        ActionOrder = alive.OrderByDescending(u => u.Speed).ThenBy(u => Random.value).ToList();
        OnTurnOrderUpdated?.Invoke(ActionOrder);
    }

    private void MoveCurrentUnitToEnd()
    {
        if (ActionOrder.Count == 0) return;
        var current = ActionOrder[0];
        ActionOrder.RemoveAt(0);
        if (ActionOrder.Count == 0) { ActionOrder.Add(current); }
        else
        {
            int idx = ActionOrder.Count;
            for (int i = ActionOrder.Count - 1; i >= 0; i--)
                if (ActionOrder[i].Speed > current.Speed) { idx = i + 1; break; }
            ActionOrder.Insert(idx, current);
        }
        OnTurnOrderUpdated?.Invoke(ActionOrder);
    }

    // === START COMBAT (từ FormationData) – ĐÃ SỬA: cộng bonus trang bị riêng cho 5 chỉ số ===
    public void StartCombat(FormationData playerFormation, EnemyGroupData enemyGroup)
    {
        _currentEnemyGroup = enemyGroup;
        PlayerUnits.Clear(); EnemyUnits.Clear();
        CurrentPlayerAP = STARTING_PLAYER_AP;
        OnAPChanged?.Invoke(CurrentPlayerAP);

        // Tạo player units – có áp dụng bonus từ trang bị
        foreach (var slot in playerFormation.slots)
        {
            if (slot?.data == null) continue;

            int level = slot.level;
            CharacterData charData = slot.data;

            // Lấy bonus trang bị từ EquipmentManager
            int hpBonus = 0, atkBonus = 0, pdefBonus = 0, mdefBonus = 0, speedBonus = 0;
            if (EquipmentManager.Instance != null)
            {
                var equipment = EquipmentManager.Instance.GetEquipment(charData);
                if (equipment != null)
                {
                    hpBonus = equipment.GetHPBonus();
                    atkBonus = equipment.GetATKBonus();
                    pdefBonus = equipment.GetPDEFBonus();
                    mdefBonus = equipment.GetMDEFBonus();
                    speedBonus = equipment.GetSpeedBonus();
                }
            }

            var unit = new CombatUnit();
            unit.Initialize(charData, level, isPlayer: true, hpBonus, atkBonus, pdefBonus, mdefBonus, speedBonus);
            unit.GridRow = SlotToRow(slot.gridSlot);
            unit.GridSlot = slot.gridSlot;
            PlayerUnits.Add(unit);
        }

        // Tạo enemy units (không có bonus)
        foreach (var entry in enemyGroup.enemies)
        {
            if (entry?.data == null) continue;
            var unit = new CombatUnit();
            unit.Initialize(entry.data, entry.level, isPlayer: false);
            unit.GridRow = SlotToRow(entry.gridSlot);
            unit.GridSlot = entry.gridSlot;
            EnemyUnits.Add(unit);
        }

        SpawnUnitViews();
        Debug.Log($"=== COMBAT STARTED === Player:{PlayerUnits.Count} vs Enemy:{EnemyUnits.Count}");
        OnCombatStarted?.Invoke();

        // Play intro stinger nếu có
        if (enemyGroup.introStinger != null && AudioManager.Instance != null)
            AudioManager.Instance.PlaySFX2D(enemyGroup.introStinger, 0.8f);

        // Play BGM
        if (CombatAudioManager.Instance != null)
        {
            CombatAudioManager.Instance.PlayBGM(enemyGroup.zoneTag, enemyGroup.bgmClip);
        }

        stateMachine.TransitionTo(CombatPhase.Intro);
    }

    // === START COMBAT (overload dùng List, dùng cho test) – cũng đã sửa bonus ===
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
            view.StoreOriginalPosition(finalPos);
            unitViews.Add(view);
        }
    }

    private void InitializePassives(CombatUnit unit)
    {
        if (unit.Data.passiveScript == null) return;
        string className = unit.Data.passiveScript.name;
        var passiveType = System.Type.GetType(className);
        if (passiveType == null)
            foreach (var asm in System.AppDomain.CurrentDomain.GetAssemblies())
                { passiveType = asm.GetType(className); if (passiveType != null) break; }
        if (passiveType != null && typeof(PassiveAbility).IsAssignableFrom(passiveType))
        {
            var instance = System.Activator.CreateInstance(passiveType) as PassiveAbility;
            if (instance != null) unit.SetPassive(instance);
        }
    }

    private void HandlePhaseChanged(CombatPhase prev, CombatPhase next)
    {
        switch (next)
        {
            case CombatPhase.Intro: StartCoroutine(DoIntro()); break;
            case CombatPhase.Execute: StartCoroutine(ExecuteCoreLoop()); break;
            case CombatPhase.Victory: DoVictory(); break;
            case CombatPhase.Defeat: DoDefeat(); break;
        }
    }

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

        var enemyViews = unitViews.Where(v => v.LinkedUnit != null && !v.LinkedUnit.IsPlayer).ToList();
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
        RebuildActionOrder();
        stateMachine.TransitionTo(CombatPhase.Execute);
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

    public void SubmitPlayerTurnAction(SkillData skill, List<CombatUnit> targets)
    {
        if (!isWaitingForPlayerInput) return;
        if (skill.apCost > CurrentPlayerAP) { Debug.LogWarning($"[AP] Không đủ AP"); return; }
        var unit = ActionOrder[turnIndex];
        if (unit == null || !unit.IsPlayer) return;

        CurrentPlayerAP -= skill.apCost;
        OnAPChanged?.Invoke(CurrentPlayerAP);
        unit.SpendAP(skill.apCost);
        unit.SelectSkill(skill, targets);

        if (skill.doesNotEndTurn) StartCoroutine(ExecuteAndRequestNewAction(unit));
        else isWaitingForPlayerInput = false;
    }

    private IEnumerator ExecuteAndRequestNewAction(CombatUnit unit)
    {
        try { unit.ExecuteSelectedSkill(0); unit.ClearSelection(); }
        catch (System.Exception e) { Debug.LogError(e); isWaitingForPlayerInput = false; yield break; }
        yield return new WaitForSeconds(0.5f);
        isWaitingForPlayerInput = true;
        OnPlayerTurnStart?.Invoke(unit);
    }

    public void SpendPlayerAP(int amount) { if (amount <= CurrentPlayerAP) { CurrentPlayerAP -= amount; OnAPChanged?.Invoke(CurrentPlayerAP); } }
    public void GainPlayerAP(int amount) { CurrentPlayerAP = Mathf.Min(CurrentPlayerAP + amount, MAX_PLAYER_AP); OnAPChanged?.Invoke(CurrentPlayerAP); }

    private IEnumerator HandleStartOfTurnEffects(CombatUnit unit)
    {
        var burn = unit.GetActiveStatus(StatusEffectType.ThieuDot);
        if (burn != null)
        {
            int dmg = Mathf.RoundToInt(burn.Value);
            unit.TakeDamage(null, dmg);
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

    private IEnumerator ExecuteCoreLoop()
    {
        OnExecuteStarted?.Invoke();
        while (true)
        {
            if (ActionOrder.Count == 0) RebuildActionOrder();
            var unit = ActionOrder.FirstOrDefault();
            if (unit == null) { yield return null; continue; }
            turnIndex = 0;
            if (!unit.IsAlive) { RebuildActionOrder(); continue; }

            yield return HandleStartOfTurnEffects(unit);
            if (!unit.IsAlive) { if (CheckForCombatEnd()) yield break; RebuildActionOrder(); continue; }

            OnUnitTurnStart?.Invoke(unit);
            unit.TriggerTurnStart();

            if (!unit.IsPlayer) enemyAI.PlanTurn(unit, PlayerUnits);
            else
            {
                if (CurrentPlayerAP < MAX_PLAYER_AP) { CurrentPlayerAP++; OnAPChanged?.Invoke(CurrentPlayerAP); }
                isWaitingForPlayerInput = true;
                OnPlayerTurnStart?.Invoke(unit);
                yield return new WaitUntil(() => !isWaitingForPlayerInput);
            }

            if (unit.SelectedSkill != null && unit.SelectedTargets.Any())
                yield return ResolveAction(new PlannedAction(unit, unit.SelectedSkill, unit.SelectedTargets));

            unit.TickStatuses();
            if (CheckForCombatEnd()) yield break;
            MoveCurrentUnitToEnd();
        }
    }

    private bool CheckForCombatEnd()
    {
        if (!EnemyUnits.Any(e => e.IsAlive)) { stateMachine.TransitionTo(CombatPhase.Victory); return true; }
        if (!PlayerUnits.Any(p => p.IsAlive)) { stateMachine.TransitionTo(CombatPhase.Defeat); return true; }
        return false;
    }

    public UnitView GetUnitView(CombatUnit unit) => unitViews.Find(v => v.LinkedUnit == unit);

    private void DoVictory()
    {
        Debug.Log("=== VICTORY ===");

        // Play victory fanfare nếu có
        if (_currentEnemyGroup != null && _currentEnemyGroup.victoryFanfare != null && AudioManager.Instance != null)
            AudioManager.Instance.PlaySFX2D(_currentEnemyGroup.victoryFanfare, 0.8f);

        // Tính tổng EXP từ enemy
        int totalExp = 0;
        foreach (var enemy in EnemyUnits)
        {
            int baseReward = enemy.Data != null ? enemy.Data.expReward : 100;
            int bonus = (enemy.Level - 1) * 10;
            totalExp += baseReward + bonus;
            Debug.Log($"[Exp] {enemy.UnitName} (Lv.{enemy.Level}): {baseReward} base + {bonus} bonus = {baseReward + bonus} EXP");
        }

        // Chia đều cho player còn sống
        var alivePlayers = PlayerUnits.Where(p => p.IsAlive).ToList();
        if (alivePlayers.Count == 0) return;
        int expPerPlayer = totalExp / alivePlayers.Count;
        if (expPerPlayer <= 0) expPerPlayer = totalExp;

        var expGained = new Dictionary<CharacterData, int>();
        foreach (var player in alivePlayers)
        {
            expGained[player.Data] = expPerPlayer;
        }

        // Gửi event kèm dictionary EXP (KHÔNG cộng vào PlayerProgression ngay)
        OnVictory?.Invoke(expGained);
    }

    private void DoDefeat()
    {
        Debug.Log("=== DEFEAT ===");
        OnDefeat?.Invoke();
    }

    public bool WillAttackResultInClash(CombatUnit a, CombatUnit b) => false;
}