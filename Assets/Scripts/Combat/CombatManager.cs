using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.SceneManagement; // ✅ Thêm để dùng MoveGameObjectToScene

public class CombatManager : MonoBehaviour
{
    public static CombatManager Instance { get; private set; }

    private CombatStateMachine stateMachine = new();
    private ActionResolver actionResolver = new();
    private EnemyAI enemyAI = new();

    public List<CombatUnit> PlayerUnits { get; private set; } = new();
    public List<CombatUnit> EnemyUnits { get; private set; } = new();
    public List<CombatUnit> ActionOrder { get; private set; } = new();

    [Header("Grid Spawn Settings")]
    public Transform[] playerGridSlots;
    public Transform[] enemyGridSlots;
    public Transform enemyRallyPoint; // Điểm tập kết cho kẻ địch
    private List<UnitView> unitViews = new();

    public List<UnitView> GetAllUnitViews()
    {
        return unitViews;
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
                    combatUICanvasGroup = planningUI.planningCanvas.gameObject.AddComponent<CanvasGroup>();
                }
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

    // ─────────────────────────────────────────────────────────
    // ✅ SỬA: Spawn unit vào đúng combat scene
    // ─────────────────────────────────────────────────────────
    private void SpawnUnitViews()
    {
        // Xóa các view cũ nếu có (phòng trường hợp restart)
        foreach (var view in unitViews)
            if (view != null) Destroy(view.gameObject);
        unitViews.Clear();

        // Lấy scene hiện tại (chính là combat scene)
        Scene currentScene = gameObject.scene;

        SpawnSide(PlayerUnits, playerGridSlots, currentScene);
        SpawnSide(EnemyUnits, enemyGridSlots, currentScene);
    }

    private void SpawnSide(List<CombatUnit> units, Transform[] gridSlots, Scene targetScene)
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

            // ✅ QUAN TRỌNG: Đưa object vào đúng combat scene
            if (go.scene != targetScene)
                SceneManager.MoveGameObjectToScene(go, targetScene);

            var view = go.GetComponent<UnitView>();
            if (view == null) { Debug.LogError($"Prefab {prefab.name} thiếu UnitView!"); continue; }
            view.Setup(unit);
            view.StoreOriginalPosition(finalGridPosition);
            unitViews.Add(view);

            Debug.Log($"[Spawn] {unit.UnitName} in scene {targetScene.name} at {spawnPos}");
        }
    }
    // ─────────────────────────────────────────────────────────

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
            yield return cameraManager.FadeInAndSetPosition(Vector3.zero, 10f, Vector3.zero, 0f);
        }

        yield return new WaitForSeconds(0.75f);

        float dollyOutDuration = 2.0f;
        StartCoroutine(cameraManager.ZoomOutToFinalView(dollyOutDuration));

        var enemyViews = unitViews.Where(v => v.LinkedUnit != null && !v.LinkedUnit.IsPlayer).ToList();
        if (enemyViews.Count > 0)
        {
            var leader = enemyViews[enemyViews.Count / 2];
            enemyViews.Remove(leader);
            StartCoroutine(MoveUnitToPosition(leader, leader.GetOriginalPosition(), 0.5f));
            yield return new WaitForSeconds(0.2f);
            foreach (var follower in enemyViews)
            {
                float randomSpeed = Random.Range(0.4f, 0.6f);
                StartCoroutine(MoveUnitToPosition(follower, follower.GetOriginalPosition(), randomSpeed));
                yield return new WaitForSeconds(Random.Range(0.05f, 0.15f));
            }
        }

        yield return new WaitForSeconds(dollyOutDuration);
        yield return FadeUI(1f, 0.5f);
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
        Vector3 startPosition = unitView.transform.position;
        float elapsed = 0f;
        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / duration;
            unitView.transform.position = Vector3.Lerp(startPosition, targetPosition, t);
            yield return null;
        }
        unitView.transform.position = targetPosition;
    }

    private void SetupRound()
    {
        CurrentRound++;
        Debug.Log($"\n=== ROUND {CurrentRound} ===");

        var allUnits = PlayerUnits.Where(u => u.IsAlive).Concat(EnemyUnits.Where(u => u.IsAlive));
        ActionOrder = allUnits.OrderByDescending(u => u.Speed).ThenBy(u => Random.value).ToList();

        Debug.Log("--- Turn Order ---");
        for (int i = 0; i < ActionOrder.Count; i++)
        {
            Debug.Log($"{i + 1}. {ActionOrder[i].UnitName} (Speed: {ActionOrder[i].Speed})");
        }

        OnRoundSetup?.Invoke(ActionOrder);

        var turnOrderUI = FindFirstObjectByType<TurnOrderUIController>();
        if (turnOrderUI != null) turnOrderUI.RebuildTurnOrderUI(ActionOrder);

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

        var currentUnit = ActionOrder[turnIndex];
        if (currentUnit == null || !currentUnit.IsPlayer)
        {
            Debug.LogError("SubmitPlayerTurnAction called, but current unit is not a player or is null.");
            return;
        }

        currentUnit.SelectSkill(skill, targets);
        Debug.Log($"[Player Turn] {currentUnit.UnitName} selected [{skill.skillName}] -> [{string.Join(", ", targets.Select(t => t.UnitName))}]");

        isWaitingForPlayerInput = false;
    }

    private IEnumerator DoRetargetCheck()
    {
        yield return null;
        stateMachine.TransitionTo(CombatPhase.Execute);
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

            Debug.Log($"--- Lượt của: {currentUnit.UnitName} ---");
            OnUnitTurnStart?.Invoke(currentUnit);

            if (!currentUnit.IsPlayer)
            {
                enemyAI.PlanTurn(currentUnit, PlayerUnits);
            }
            else
            {
                Debug.Log($"[Execute] {currentUnit.UnitName} is a player. Waiting for input...");
                isWaitingForPlayerInput = true;
                OnPlayerTurnStart?.Invoke(currentUnit);
                yield return new WaitUntil(() => !isWaitingForPlayerInput);
            }

            if (currentUnit.SelectedSkill != null && currentUnit.SelectedTargets.Any())
            {
                var actionResult = actionResolver.Resolve(currentUnit, currentUnit.SelectedSkill, currentUnit.SelectedTargets);
                OnActionResolved?.Invoke(actionResult);

                if (clashSequence != null)
                {
                    yield return StartCoroutine(clashSequence.PlayAction(actionResult));
                }
                else
                {
                    Debug.LogError("[ExecuteRound] ClashAnimationSequence chưa được gán!");
                    actionResult.ApplyOutcomes();
                    yield return new WaitForSeconds(1f);
                }
            }
            else
            {
                Debug.LogWarning($"[Execute] {currentUnit.UnitName} không có hành động nào được chọn.");
            }

            if (CheckForCombatEnd()) yield break;
        }

        foreach (var unit in PlayerUnits.Concat(EnemyUnits)) unit.ClearSelection();
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
        foreach (var u in PlayerUnits.Concat(EnemyUnits).Where(u => u.IsAlive))
            u.TickCooldowns();
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
        if (unitA == null || unitB == null) return false;
        bool aTargetsBWithClash = unitA.SelectedSkill?.type == SkillType.Clash && unitA.SelectedTargets.Contains(unitB);
        bool bTargetsAWithClash = unitB.SelectedSkill?.type == SkillType.Clash && unitB.SelectedTargets.Contains(unitA);
        return aTargetsBWithClash && bTargetsAWithClash;
    }
}