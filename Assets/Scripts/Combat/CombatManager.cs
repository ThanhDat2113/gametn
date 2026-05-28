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

    // Command System
    private readonly Queue<ICombatCommand> _commandQueue = new Queue<ICombatCommand>();
    private bool _isProcessingCommands = false;

    public List<CombatUnit> PlayerUnits { get; private set; } = new();
    public List<CombatUnit> EnemyUnits { get; private set; } = new();
    public List<CombatUnit> ActionOrder { get; private set; } = new();

    // Action Point System
    public int CurrentPlayerAP { get; private set; }
    private const int MAX_PLAYER_AP = 5;
    private const int STARTING_PLAYER_AP = 3;
    private bool isFirstPlayerTurnOfRound;

    [Header("Grid Spawn Settings")]
    public Transform[] playerGridSlots;
    public Transform[] enemyGridSlots;
    public Transform enemyRallyPoint;
    private List<UnitView> unitViews = new();

    public List<UnitView> GetAllUnitViews()
    {
        return unitViews;
    }

    public void GrantExtraTurn(CombatUnit unit)
    {
        Debug.Log($"[CombatManager] Cấp thêm lượt cho {unit.UnitName}");
        // Logic để thêm lượt sẽ được triển khai ở đây
        // Ví dụ: thêm unit vào một hàng đợi ưu tiên
    }

    public void GrantImmediateTurn(CombatUnit unit)
    {
        Debug.Log($"[CombatManager] Cấp lượt hành động ngay lập tức cho {unit.UnitName}");
        // Đây là một logic phức tạp, cần phải chèn unit vào vị trí tiếp theo trong ActionOrder
        // và có thể cần phải cấu trúc lại vòng lặp ExecuteRound.
        // Tạm thời, chúng ta sẽ chèn vào vị trí tiếp theo.
        if (ActionOrder.Contains(unit))
        {
            ActionOrder.Remove(unit);
        }
        ActionOrder.Insert(turnIndex + 1, unit);
    }

    public List<CombatUnit> GetTeam(bool isPlayer)
    {
        return isPlayer ? PlayerUnits : EnemyUnits;
    }

    public List<CombatUnit> GetOpposingTeam(bool isPlayer)
    {
        return isPlayer ? EnemyUnits : PlayerUnits;
    }

    private static int SlotToRow(int slot) => 2 - (slot / 3);

    private int planningIndex = 0;
    private int turnIndex = 0;
    private bool isWaitingForPlayerInput = false;
    public int CurrentRound { get; private set; } = 0;

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
        if (!_isProcessingCommands)
        {
            StartCoroutine(ProcessCommandQueue());
        }
    }

    private IEnumerator ProcessCommandQueue()
    {
        _isProcessingCommands = true;
        while (_commandQueue.Count > 0)
        {
            ICombatCommand command = _commandQueue.Dequeue();
            IEnumerator commandCoroutine = command.Execute();

            if (commandCoroutine != null)
            {
                yield return StartCoroutine(commandCoroutine);
            }
        }
        _isProcessingCommands = false;
    }
    #endregion

    public event System.Action<CombatUnit> OnPlayerTurnStart;
    public event System.Action<CombatUnit> OnUnitTurnStart;
    public event System.Action<List<CombatUnit>> OnRoundSetup;
    public event System.Action<List<CombatUnit>> OnPlayerPlanStarted;
    public event System.Action<CombatUnit> OnPlayerSkillSelected;
    public event System.Action OnEnemyPlanDone;
    public event System.Action OnExecuteStarted;
    public event System.Action<ActionResult> OnActionResolved;
    public event System.Action OnRoundEnded;
    public event System.Action OnVictory;
    public event System.Action OnDefeat;
    public event System.Action OnPlanChanged;
    public event System.Action<int> OnAPChanged;

    // Damage modification hook
    public delegate void DamageModificationHandler(ActionOutcome outcome, CombatUnit actor);
    public event DamageModificationHandler OnDamageCalculation;
    public void TriggerDamageCalculation(ActionOutcome outcome, CombatUnit actor) => OnDamageCalculation?.Invoke(outcome, actor);

    public CombatPhase CurrentPhase => stateMachine.Current;

    private void Awake()
    {
        if (Instance != null) { Destroy(gameObject); return; }
        Instance = this;
        stateMachine.OnPhaseChanged += HandlePhaseChanged;

        if (cameraManager == null)
            cameraManager = FindFirstObjectByType<CombatCameraManager>();
        
        if (combatUICanvasGroup == null)
        {
            var planningUI = FindFirstObjectByType<CombatPlanningUI>();
            if (planningUI != null && planningUI.planningCanvas != null)
            {
                combatUICanvasGroup = planningUI.planningCanvas.GetComponent<CanvasGroup>();
                if (combatUICanvasGroup == null)
                {
                    Debug.Log("[CombatManager] Automatically adding CanvasGroup to planningCanvas.");
                    combatUICanvasGroup = planningUI.planningCanvas.gameObject.AddComponent<CanvasGroup>();
                }
            }
            else
            {
                Debug.LogWarning("[CombatManager] Could not automatically find UI Canvas. UI fade will not work.");
            }
        }

        arrowController = GetComponent<TargetingArrowController>();
        if (arrowController == null)
        {
            arrowController = gameObject.AddComponent<TargetingArrowController>();
        }
    }

    private void OnDestroy()
    {
        if (stateMachine != null)
        {
            stateMachine.OnPhaseChanged -= HandlePhaseChanged;
        }
    }

    public void StartCombat(FormationData playerFormation, EnemyGroupData enemyGroup)
    {
        PlayerUnits.Clear();
        EnemyUnits.Clear();
        CurrentRound = 0;
        CurrentPlayerAP = STARTING_PLAYER_AP;
        OnAPChanged?.Invoke(CurrentPlayerAP);

        foreach (var slot in playerFormation.slots)
        {
            if (slot?.data == null) continue;
            var unit = new CombatUnit();
            unit.Initialize(slot.data, slot.level, isPlayer: true);
            unit.GridRow = SlotToRow(slot.gridSlot);
            unit.GridSlot = slot.gridSlot;
            PlayerUnits.Add(unit);
        }

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

        stateMachine.TransitionTo(CombatPhase.Intro);
    }

    public void StartCombat(
        List<(CharacterData data, int level, int gridSlot)> playerSetup,
        List<(CharacterData data, int level, int gridSlot)> enemySetup)
    {
        var formation = new FormationData
        {
            slots = playerSetup.ConvertAll(p => new FormationSlot
            {
                data = p.data,
                level = p.level,
                gridSlot = p.gridSlot
            }).ToArray()
        };
        var enemyGroup = ScriptableObject.CreateInstance<EnemyGroupData>();
        enemyGroup.enemies = enemySetup.ConvertAll(e => new EnemyGroupData.EnemyEntry
        {
            data = e.data,
            level = e.level,
            gridSlot = e.gridSlot
        }).ToArray();
        StartCombat(formation, enemyGroup);
    }

    private void SpawnUnitViews()
    {
        foreach (var view in unitViews)
            if (view != null) Destroy(view.gameObject);
        unitViews.Clear();
        SpawnSide(PlayerUnits, playerGridSlots);
        SpawnSide(EnemyUnits, enemyGridSlots);
    }

    private void SpawnSide(List<CombatUnit> units, Transform[] gridSlots)
    {
        foreach (var unit in units)
        {
            var prefab = unit.Data.prefab;
            if (prefab == null)
            {
                Debug.LogError($"[CombatManager] {unit.UnitName} chưa có prefab!");
                continue;
            }
            int slot = Mathf.Clamp(unit.GridSlot, 0, 8);
            if (gridSlots == null || slot >= gridSlots.Length || gridSlots[slot] == null)
            {
                Debug.LogError($"[CombatManager] gridSlot {slot} của {unit.UnitName} không có Transform!");
                continue;
            }

            Vector3 finalGridPosition = gridSlots[slot].position;

            bool isEnemy = !unit.IsPlayer;
            Vector3 spawnPos = isEnemy && enemyRallyPoint != null ? enemyRallyPoint.position : finalGridPosition;

            var go = Instantiate(prefab, spawnPos, Quaternion.identity);
            SceneManager.MoveGameObjectToScene(go, gameObject.scene);
            var view = go.GetComponent<UnitView>();
            if (view == null) { Debug.LogError($"Prefab {prefab.name} thiếu UnitView!"); continue; }
            view.Setup(unit);
            InitializePassives(unit);

            view.StoreOriginalPosition(finalGridPosition);

            unitViews.Add(view);
            Debug.Log($"[Spawn] {unit.UnitName} slot{slot} at {spawnPos}. Final grid pos: {finalGridPosition}");
        }
    }

    private void InitializePassives(CombatUnit unit)
    {
        if (unit.Data.passiveScript == null) return;

        string className = unit.Data.passiveScript.name;

        var passiveType = System.Type.GetType(className);
        if (passiveType == null)
        {
            foreach (var assembly in System.AppDomain.CurrentDomain.GetAssemblies())
            {
                passiveType = assembly.GetType(className);
                if (passiveType != null) break;
            }
        }

        if (passiveType != null && typeof(PassiveAbility).IsAssignableFrom(passiveType))
        {
            var passiveInstance = System.Activator.CreateInstance(passiveType) as PassiveAbility;
            if (passiveInstance != null)
            {
                unit.SetPassive(passiveInstance);
                Debug.Log($"[Passive] Initialized passive '{className}' for {unit.UnitName}.");
            }
        }
        else
        {
            Debug.LogWarning($"[Passive] '{className}' không tìm thấy hoặc không kế thừa PassiveAbility.");
        }
    }

    private void HandlePhaseChanged(CombatPhase prev, CombatPhase next)
    {
        switch (next)
        {
            case CombatPhase.Intro: StartCoroutine(DoIntro()); break;
            case CombatPhase.EnemyPlan: SetupRound(); break;
            case CombatPhase.PlayerPlan: StartPlayerPlan(); break;
            case CombatPhase.RetargetCheck: StartCoroutine(DoRetargetCheck()); break;
            case CombatPhase.Execute: StartCoroutine(ExecuteRound()); break;
            case CombatPhase.RoundEnd: DoRoundEnd(); break;
            case CombatPhase.Victory: DoVictory(); break;
            case CombatPhase.Defeat: DoDefeat(); break;
        }
    }

    private IEnumerator DoIntro()
    {
        cameraManager.BeginIntroSequence();

        yield return FadeUI(0f, 0.5f);

        Vector3 enemyGridCenter = Vector3.zero;
        int validSlotCount = 0;
        if (enemyGridSlots != null && enemyGridSlots.Length > 0)
        {
            foreach (var slot in enemyGridSlots)
            {
                if (slot != null)
                {
                    enemyGridCenter += slot.position;
                    validSlotCount++;
                }
            }
        }

        if (validSlotCount > 0)
        {
            enemyGridCenter /= validSlotCount;
            yield return cameraManager.FadeInAndSetPosition(enemyGridCenter, 7.5f, Vector3.left * 20f, 1.0f);
        }
        else
        {
            Debug.LogError("[Intro] Không có enemy grid slots hợp lệ để tính trung tâm! Dùng vị trí mặc định.");
            yield return cameraManager.FadeInAndSetPosition(Vector3.zero, 10f, Vector3.zero, 0f);
        }

        yield return new WaitForSeconds(0.75f);

        float dollyOutDuration = 2.0f;
        StartCoroutine(cameraManager.ZoomOutToFinalView(dollyOutDuration));

        var enemyViews = unitViews.Where(v => v.LinkedUnit != null && !v.LinkedUnit.IsPlayer).ToList();
        if (enemyViews.Count > 0)
        {
            List<Coroutine> movementCoroutines = new List<Coroutine>();

            var leader = enemyViews[enemyViews.Count / 2];
            enemyViews.Remove(leader);
            movementCoroutines.Add(StartCoroutine(MoveUnitToPosition(leader, leader.GetOriginalPosition(), 0.5f)));

            yield return new WaitForSeconds(0.2f);

            foreach (var follower in enemyViews)
            {
                float randomSpeed = Random.Range(0.4f, 0.6f);
                movementCoroutines.Add(StartCoroutine(MoveUnitToPosition(follower, follower.GetOriginalPosition(), randomSpeed)));
                yield return new WaitForSeconds(Random.Range(0.05f, 0.15f));
            }

            // Chờ tất cả các coroutine di chuyển hoàn thành
            foreach (var coroutine in movementCoroutines)
            {
                yield return coroutine;
            }
        }

        yield return new WaitForSeconds(dollyOutDuration);

        yield return FadeUI(1f, 0.5f);

        Debug.Log("[Intro] Intro sequence finished. Starting combat.");
        cameraManager.EndIntroSequence();
        stateMachine.TransitionTo(CombatPhase.EnemyPlan);
    }


    private IEnumerator FadeUI(float targetAlpha, float duration)
    {
        if (combatUICanvasGroup == null) yield break;

        float startAlpha = combatUICanvasGroup.alpha;
        float elapsed = 0f;

        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            combatUICanvasGroup.alpha = Mathf.Lerp(startAlpha, targetAlpha, elapsed / duration);
            yield return null;
        }

        combatUICanvasGroup.alpha = targetAlpha;
    }

    private IEnumerator MoveUnitToPosition(UnitView unitView, Vector3 targetPosition, float duration)
    {
        unitView.SetAnimationTrigger("Rush");

        Vector3 startPosition = unitView.transform.position;
        float elapsed = 0f;

        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / duration;
            unitView.transform.position = Vector3.Lerp(startPosition, targetPosition, t * t);
            yield return null;
        }

        unitView.transform.position = targetPosition;
        
        unitView.SetAnimationTrigger("Idle");
    }

    private Vector3 GetSideCenter(List<CombatUnit> units, Transform[] gridSlots)
    {
        if (units == null || units.Count == 0 || gridSlots == null || gridSlots.Length == 0)
        {
            return Vector3.zero;
        }

        Vector3 center = Vector3.zero;
        int count = 0;

        foreach (var unit in units)
        {
            if (unit.GridSlot >= 0 && unit.GridSlot < gridSlots.Length && gridSlots[unit.GridSlot] != null)
            {
                center += gridSlots[unit.GridSlot].position;
                count++;
            }
        }

        return count > 0 ? center / count : Vector3.zero;
    }

    private void SetupRound()
    {
        CurrentRound++;
        isFirstPlayerTurnOfRound = true;
        Debug.Log($"\n=== ROUND {CurrentRound} ===");

        var allUnits = PlayerUnits.Where(u => u.IsAlive).Concat(EnemyUnits.Where(u => u.IsAlive));
        ActionOrder = allUnits.OrderByDescending(u => u.Speed).ThenBy(u => Random.value).ToList();

        Debug.Log("--- Turn Order ---");
        for(int i = 0; i < ActionOrder.Count; i++)
        {
            Debug.Log($"{i+1}. {ActionOrder[i].UnitName} (Speed: {ActionOrder[i].Speed})");
        }

        OnRoundSetup?.Invoke(ActionOrder);

        var turnOrderUI = FindFirstObjectByType<TurnOrderUIController>();
        if (turnOrderUI != null)
        {
            turnOrderUI.RebuildTurnOrderUI(ActionOrder);
        }

        OnEnemyPlanDone?.Invoke();
        stateMachine.TransitionTo(CombatPhase.Execute);
    }




    private void StartPlayerPlan()
    {
        Debug.LogWarning("[CombatManager] StartPlayerPlan is deprecated.");
    }

    public void SubmitPlayerTurnAction(SkillData skill, List<CombatUnit> targets)
    {
        if (!isWaitingForPlayerInput)
        {
            Debug.LogWarning("SubmitPlayerTurnAction called when not waiting for player input.");
            return;
        }

        if (skill.apCost > CurrentPlayerAP)
        {
            Debug.LogWarning($"[AP] Không đủ AP để dùng {skill.skillName}. Cần {skill.apCost}, có {CurrentPlayerAP}.");
            return;
        }

        var currentUnit = ActionOrder[turnIndex];
        if (currentUnit == null || !currentUnit.IsPlayer)
        {
            Debug.LogError("SubmitPlayerTurnAction called, but current unit is not a player or is null.");
            return;
        }

        CurrentPlayerAP -= skill.apCost;
        OnAPChanged?.Invoke(CurrentPlayerAP);
        currentUnit.SpendAP(skill.apCost);
        Debug.Log($"[AP] Đã dùng {skill.apCost} AP. Còn lại {CurrentPlayerAP}.");

        currentUnit.SelectSkill(skill, targets);
        Debug.Log($"[Player Input] {currentUnit.UnitName} đã chọn dùng {skill.skillName} lên {string.Join(", ", targets.Select(t => t.UnitName))}.");

        // KIỂM TRA NẾU SKILL KHÔNG KẾT THÚC LƯỢT
        if (skill.doesNotEndTurn)
        {
            Debug.Log($"[CombatManager] {currentUnit.UnitName} dùng kỹ năng không kết thúc lượt. Bắt đầu ExecuteAndRequestNewAction.");
            // Thực thi ngay lập tức và yêu cầu input mới
            StartCoroutine(ExecuteAndRequestNewAction(currentUnit));
        }
        else
        {
            Debug.Log($"[CombatManager] {currentUnit.UnitName} dùng kỹ năng kết thúc lượt. Chờ ExecuteRound tiếp tục.");
            // Logic cũ: kết thúc lượt
            isWaitingForPlayerInput = false;
        }
    }

    private IEnumerator ExecuteAndRequestNewAction(CombatUnit unit)
    {
        bool executionSuccessful = false;
        try
        {
            Debug.Log($"[Action] Attempting to execute skill for {unit.UnitName}.");
            unit.ExecuteSelectedSkill(0); // AP has been deducted previously
            unit.ClearSelection();
            Debug.Log($"[Action] Skill execution completed for {unit.UnitName}.");
            executionSuccessful = true;
        }
        catch (System.Exception e)
        {
            Debug.LogError($"[FATAL ERROR] in skill execution for {unit.UnitName}: {e.Message}\n{e.StackTrace}");
            isWaitingForPlayerInput = false; // Stop the turn to prevent getting stuck
        }

        if (executionSuccessful)
        {
            // Wait a moment for the effects to apply and be seen by the user
            yield return new WaitForSeconds(0.5f);

            // Request a new action from the same unit
            isWaitingForPlayerInput = true;
            OnPlayerTurnStart?.Invoke(unit); // Resend the event to reopen the UI
            Debug.Log($"[Action] {unit.UnitName} continues their turn.");
        }
    }

    public void SpendPlayerAP(int amount)
    {
        if (amount > CurrentPlayerAP)
        {
            Debug.LogError($"[AP] Attempted to spend {amount} AP, but only have {CurrentPlayerAP}.");
            return;
        }
        CurrentPlayerAP -= amount;
        OnAPChanged?.Invoke(CurrentPlayerAP);
        Debug.Log($"[AP] Spent {amount} AP. Remaining: {CurrentPlayerAP}.");
    }

    public void GainPlayerAP(int amount)
    {
        CurrentPlayerAP = Mathf.Min(CurrentPlayerAP + amount, MAX_PLAYER_AP);
        OnAPChanged?.Invoke(CurrentPlayerAP);
        Debug.Log($"[AP] Gained {amount} AP. Remaining: {CurrentPlayerAP}.");
    }

    private IEnumerator HandleStartOfTurnEffects(CombatUnit unit)
    {
        var burnStatus = unit.GetActiveStatus(StatusEffectType.ThieuDot);
        if (burnStatus != null)
        {
            int burnDamage = Mathf.RoundToInt(burnStatus.Value);
            Debug.Log($"<color=red>[ThieuDot] {unit.UnitName} nhận {burnDamage} sát thương thiêu đốt.</color>");
            unit.TakeDamage(null, burnDamage);
            
            var view = unitViews.FirstOrDefault(v => v.LinkedUnit == unit);
            if (view != null)
            {
                view.TriggerHitFlash();
            }

            yield return new WaitForSeconds(0.5f);
        }
    }

    private IEnumerator DoRetargetCheck()
    {
        yield return null;
        stateMachine.TransitionTo(CombatPhase.Execute);
    }

    private IEnumerator ResolveAction(PlannedAction action)
    {
        Debug.Log($"[Resolve] {action.Caster.UnitName} dùng {action.Skill.skillName}");
        var result = actionResolver.Resolve(action.Caster, action.Skill, action.Targets);
        OnActionResolved?.Invoke(result);

        // DATA-DRIVEN: Apply non-damage effects NGAY (heal, buff, status)
        // Damage effects được apply qua animation per-hit
        bool hasDamageEffects = false;
        if (action.Skill.effects != null && action.Skill.effects.Length > 0)
        {
            foreach (var effect in action.Skill.effects)
            {
                if (effect == null) continue;
                
                // Detect damage effects: kế thừa DamageEffect HOẶC tên effect có chứa "Damage" 
                bool isDamageEffect = (effect is DamageEffect) || effect.GetType().Name.ToLower().Contains("damage");
                if (isDamageEffect)
                {
                    // Damage effect: sẽ được xử lý trong animation per-hit
                    hasDamageEffects = true;
                    Debug.Log($"[Resolve] DamageEffect '{effect.GetType().Name}' deferred to animation.");
                }
                else
                {
                    // Non-damage effect: apply ngay (heal, buff, status)
                    Debug.Log($"[Resolve] Applying non-damage effect: {effect.GetType().Name}");
                    effect.Apply(action.Caster, action.Targets.ToArray());
                }
            }
        }

        // Fallback: nếu skill không có damage effects, dùng ActionResolver outcomes
        // NHƯNG vẫn defer vào animation per-hit (không apply ngay)
        if (!hasDamageEffects)
        {
            if (result.Outcomes.Count > 0)
            {
                // Gán outcomes vào pending để animation per-hit apply
                int hitCount = action.Skill != null ? Mathf.Max(1, action.Skill.hitCount) : 1;
                var actorView = GetUnitView(action.Caster);
                if (actorView != null)
                {
                    actorView.SetPendingOutcomes(result.Outcomes, action.Caster, hitCount);
                    Debug.Log($"[Resolve] Fallback outcomes deferred to animation: {result.Outcomes.Count} outcomes, {hitCount} hits.");
                }
            }
            else
            {
                Debug.LogWarning($"Skill '{action.Skill.skillName}' không có effects và không có fallback outcomes!");
            }
        }
        else
        {
            // Có damage effects: gán pending hits vào actorView để animation per-hit apply
            var actorView = GetUnitView(action.Caster);
            if (actorView != null)
            {
                actorView.SetPendingOutcomes(result.Outcomes, action.Caster, action.Skill != null ? Mathf.Max(1, action.Skill.hitCount) : 1);
                Debug.Log($"[Resolve] Set pending outcomes cho {action.Caster.UnitName}: {result.Outcomes.Count} outcomes, {Mathf.Max(1, action.Skill != null ? action.Skill.hitCount : 1)} hits.");
            }
        }

        if (clashSequence != null)
        {
            yield return StartCoroutine(clashSequence.PlayAction(result));
        }
        else
        {
            Debug.LogError("Clash sequence not set!");
            yield return new WaitForSeconds(1f);
        }

        // Kích hoạt sự kiện sau khi hành động đã được giải quyết hoàn toàn
        Debug.Log($"[EVENT] Chuẩn bị kích hoạt OnActionConfirmed cho {action.Caster.UnitName} với kỹ năng {action.Skill.skillName}.");
        action.Caster.RaiseActionConfirmed(action.Skill, action.Targets);

        if (CheckForCombatEnd())
        {
            yield break;
        }
    }



    private IEnumerator ExecuteRound()
    {
        OnExecuteStarted?.Invoke();
        Debug.Log("\n--- EXECUTE ---");

        for (turnIndex = 0; turnIndex < ActionOrder.Count; turnIndex++)
        {
            var currentUnit = ActionOrder[turnIndex];
            if (!currentUnit.IsAlive)
            {
                Debug.Log($"[Execute] Bỏ qua {currentUnit.UnitName} vì đã chết.");
                continue;
            }

            yield return StartCoroutine(HandleStartOfTurnEffects(currentUnit));
            if (!currentUnit.IsAlive)
            {
                Debug.Log($"[Execute] {currentUnit.UnitName} đã chết do hiệu ứng đầu lượt.");
                if (CheckForCombatEnd())
                {
                    yield break;
                }
                continue;
            }

            Debug.Log($"--- Lượt của: {currentUnit.UnitName} ---");
            OnUnitTurnStart?.Invoke(currentUnit);
            currentUnit.TriggerTurnStart();

            if (!currentUnit.IsPlayer)
            {
                enemyAI.PlanTurn(currentUnit, PlayerUnits);
            }
            else
            {
                if (!isFirstPlayerTurnOfRound)
                {
                    if (CurrentPlayerAP < MAX_PLAYER_AP)
                    {
                        CurrentPlayerAP++;
                        OnAPChanged?.Invoke(CurrentPlayerAP);
                        Debug.Log($"[AP] Hồi 1 AP. Hiện có: {CurrentPlayerAP}");
                    }
                }
                isFirstPlayerTurnOfRound = false;

                Debug.Log($"[Execute] Unit {currentUnit.UnitName} is a player. Waiting for input...");
                isWaitingForPlayerInput = true;
                OnPlayerTurnStart?.Invoke(currentUnit);

                yield return new WaitUntil(() => !isWaitingForPlayerInput);
            }

            if (currentUnit.SelectedSkill != null && currentUnit.SelectedTargets.Any())
            {
                var action = new PlannedAction(currentUnit, currentUnit.SelectedSkill, currentUnit.SelectedTargets);
                yield return StartCoroutine(ResolveAction(action));
            }
            else
            {
                Debug.LogWarning($"[Execute] {currentUnit.UnitName} không có hành động nào được chọn.");
            }

            currentUnit.TickStatuses();

            if (currentUnit.UnitName == "NoName")
            {
                int healAmount = Mathf.RoundToInt(currentUnit.MaxHP * 0.05f);
                currentUnit.Heal(healAmount);
                Debug.Log($"[Passive] NoName recovered {healAmount} HP.");
            }

            if (CheckForCombatEnd())
            {
                yield break;
            }
        }

        foreach (var unit in PlayerUnits.Concat(EnemyUnits))
        {
            unit.ClearSelection();
        }
        Debug.Log("--- Tất cả các lượt đã thực hiện ---");

        stateMachine.TransitionTo(CombatPhase.RoundEnd);
    }

    private bool CheckForCombatEnd()
    {
        if (!EnemyUnits.Any(e => e.IsAlive))
        {
            stateMachine.TransitionTo(CombatPhase.Victory);
            return true;
        }
        
        if (!PlayerUnits.Any(p => p.IsAlive))
        {
            stateMachine.TransitionTo(CombatPhase.Defeat);
            return true;
        }

        return false;
    }


    public UnitView GetUnitView(CombatUnit unit) =>
        unitViews.Find(v => v.LinkedUnit == unit);

    private void DoRoundEnd()
    {
        OnRoundEnded?.Invoke();
        Debug.Log("--- ROUND END ---\n");
        stateMachine.TransitionTo(CombatPhase.EnemyPlan);
    }

    private void DoVictory()
    {
        Debug.Log("=== VICTORY ===");
        OnVictory?.Invoke();
    }

    private void DoDefeat()
    {
        Debug.Log("=== DEFEAT ===");
        OnDefeat?.Invoke();
    }

    public bool WillAttackResultInClash(CombatUnit unitA, CombatUnit unitB)
    {
        return false;
    }
}