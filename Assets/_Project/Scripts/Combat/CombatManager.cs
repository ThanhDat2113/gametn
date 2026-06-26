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

    [Header("Turn Meter Settings")]
    public float actionValueTickRate = 10f;

    private bool _isExecutingTurn = false;
    private CombatUnit _currentActingUnit = null;

    // ── Events ─────────────────────────────────────────────────
    public event System.Action OnCombatStarted;
    public event System.Action<CombatUnit> OnPlayerUnitPlanning;
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

    private EnemyGroupData _currentEnemyGroup;

    [Header("UI & Animation")]
    public ClashAnimationSequence clashSequence;
    public CombatCameraManager cameraManager;
    public CanvasGroup combatUICanvasGroup;
    private TargetingArrowController arrowController;

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

    public void GrantExtraTurn(CombatUnit unit)
    {
        Debug.Log($"[CombatManager] Cấp thêm lượt cho {unit.UnitName}");
        unit.CurrentActionValue = CombatUnit.ACTION_THRESHOLD - 1f;
    }

    public void GrantImmediateTurn(CombatUnit unit)
    {
        Debug.Log($"[CombatManager] Cấp lượt hành động ngay lập tức cho {unit.UnitName}");
        unit.CurrentActionValue = CombatUnit.ACTION_THRESHOLD;
    }

    public List<CombatUnit> GetTeam(bool isPlayer) => isPlayer ? PlayerUnits : EnemyUnits;
    public List<CombatUnit> GetOpposingTeam(bool isPlayer) => isPlayer ? EnemyUnits : PlayerUnits;

    private static int SlotToRow(int slot) => 2 - (slot / 3);

    private void UpdateActionOrderForUI()
    {
        var alive = GetAllAliveUnits();
        ActionOrder = alive
            .OrderByDescending(u => u.CurrentActionValue)
            .ThenByDescending(u => u.Speed)
            .ToList();
        OnTurnOrderUpdated?.Invoke(ActionOrder);
    }

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

        // Tạo player units
        foreach (var slot in playerFormation.slots)
        {
            if (slot?.data == null) continue;
            int level = slot.level;
            CharacterData charData = slot.data;
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
            case CombatPhase.Execute: StartCoroutine(TurnMeterLoop()); break;
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

        foreach (var unit in GetAllAliveUnits()) { unit.CurrentActionValue = 0f; }
        UpdateActionOrderForUI();
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

    private IEnumerator TurnMeterLoop()
    {
        OnExecuteStarted?.Invoke();
        _isExecutingTurn = false;

        while (true)
        {
            if (!_isExecutingTurn)
            {
                AccumulateActionValues();
                var readyUnit = GetAllAliveUnits()
                    .Where(u => u.IsActionReady)
                    .OrderByDescending(u => u.CurrentActionValue)
                    .FirstOrDefault();

                if (readyUnit != null)
                {
                    _isExecutingTurn = true;
                    _currentActingUnit = readyUnit;
                    yield return StartCoroutine(ProcessUnitTurn(readyUnit));
                    _isExecutingTurn = false;
                    _currentActingUnit = null;
                    if (CheckForCombatEnd()) yield break;
                }
                else yield return null;
            }
            else yield return null;
        }
    }

    private void AccumulateActionValues()
    {
        float delta = Time.deltaTime * actionValueTickRate;
        foreach (var unit in GetAllAliveUnits())
        {
            if (unit.HasStatus(StatusEffectType.Stun))
                unit.AddActionValue(unit.Speed * delta * 0.5f);
            else
                unit.AddActionValue(unit.Speed * delta);
        }
    }

    private IEnumerator ProcessUnitTurn(CombatUnit unit)
    {
        unit.ConsumeActionValue();
        Debug.Log($"[TurnMeter] {unit.UnitName} hành động (Speed={unit.Speed}, AV còn lại={unit.CurrentActionValue:F1})");
        UpdateActionOrderForUI();

        yield return HandleStartOfTurnEffects(unit);
        if (!unit.IsAlive) { if (CheckForCombatEnd()) yield break; yield break; }

        OnUnitTurnStart?.Invoke(unit);
        unit.TriggerTurnStart();

        if (!unit.IsPlayer) enemyAI.PlanTurn(unit, PlayerUnits);
        else
        {
            if (CurrentPlayerAP < MAX_PLAYER_AP) { CurrentPlayerAP++; OnAPChanged?.Invoke(CurrentPlayerAP); }
            yield return StartCoroutine(WaitForPlayerInput(unit));
        }

        if (unit.SelectedSkill != null && unit.SelectedTargets.Any())
            yield return ResolveAction(new PlannedAction(unit, unit.SelectedSkill, unit.SelectedTargets));

        unit.TickStatuses();
        if (CheckForCombatEnd()) yield break;
        UpdateActionOrderForUI();
    }

    private IEnumerator WaitForPlayerInput(CombatUnit unit)
    {
        bool inputReceived = false;
        OnPlayerTurnStart?.Invoke(unit);

        System.Action<SkillData, List<CombatUnit>> onSubmit = null;
        onSubmit = (skill, targets) =>
        {
            if (inputReceived && !skill.doesNotEndTurn) return;
            if (skill.apCost > CurrentPlayerAP) { Debug.LogWarning($"[AP] Không đủ AP"); return; }

            CurrentPlayerAP -= skill.apCost;
            OnAPChanged?.Invoke(CurrentPlayerAP);
            unit.SpendAP(skill.apCost);
            unit.SelectSkill(skill, targets);

            if (skill.doesNotEndTurn)
            {
                try { unit.ExecuteSelectedSkill(0); unit.ClearSelection(); }
                catch (System.Exception e) { Debug.LogError(e); return; }
                OnPlayerTurnStart?.Invoke(unit);
            }
            else inputReceived = true;
        };

        _pendingPlayerSubmit = onSubmit;
        yield return new WaitUntil(() => inputReceived);
        _pendingPlayerSubmit = null;
    }

    private System.Action<SkillData, List<CombatUnit>> _pendingPlayerSubmit;
    public void SubmitPlayerTurnAction(SkillData skill, List<CombatUnit> targets) => _pendingPlayerSubmit?.Invoke(skill, targets);
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

    private bool CheckForCombatEnd()
    {
        if (!EnemyUnits.Any(e => e.IsAlive)) { stateMachine.TransitionTo(CombatPhase.Victory); return true; }
        if (!PlayerUnits.Any(p => p.IsAlive)) { stateMachine.TransitionTo(CombatPhase.Defeat); return true; }
        return false;
    }

    public UnitView GetUnitView(CombatUnit unit) => unitViews.Find(v => v.LinkedUnit == unit);
    public bool WillAttackResultInClash(CombatUnit a, CombatUnit b) => false;

    private void DoVictory()
    {
        Debug.Log("=== VICTORY ===");
        if (_currentEnemyGroup != null && _currentEnemyGroup.victoryFanfare != null && AudioManager.Instance != null)
            AudioManager.Instance.PlaySFX2D(_currentEnemyGroup.victoryFanfare, 0.8f);

        int totalExp = 0;
        foreach (var enemy in EnemyUnits)
        {
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

    private void DoDefeat()
    {
        Debug.Log("=== DEFEAT ===");
        OnDefeat?.Invoke();
    }
}