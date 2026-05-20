using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

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
    public CanvasGroup combatUICanvasGroup; // Kéo CanvasGroup cha vào đây
    private TargetingArrowController arrowController;

    public event System.Action OnCombatStarted;
    public event System.Action<CombatUnit> OnPlayerUnitPlanning;
    public event System.Action<CombatUnit> OnPlayerTurnStart; // Sự kiện mới cho turn-based
    public event System.Action<CombatUnit> OnUnitTurnStart; // Sự kiện cho mỗi lượt của unit bất kỳ
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
        
        // Tự động hóa việc thiết lập CanvasGroup cho UI
        if (combatUICanvasGroup == null)
        {
            var planningUI = FindFirstObjectByType<CombatPlanningUI>();
            if (planningUI != null && planningUI.planningCanvas != null)
            {
                combatUICanvasGroup = planningUI.planningCanvas.GetComponent<CanvasGroup>();
                if (combatUICanvasGroup == null)
                {
                    Debug.Log("[CombatManager] Tự động thêm CanvasGroup vào planningCanvas.");
                    combatUICanvasGroup = planningUI.planningCanvas.gameObject.AddComponent<CanvasGroup>();
                }
            }
            else
            {
                Debug.LogWarning("[CombatManager] Không thể tự động tìm thấy UI Canvas. Fade UI sẽ không hoạt động.");
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

            // Vị trí cuối cùng trên lưới
            Vector3 finalGridPosition = gridSlots[slot].position;

            // Nếu là enemy, spawn tại rally point. Nếu không, spawn tại grid.
            bool isEnemy = !unit.IsPlayer;
            Vector3 spawnPos = isEnemy && enemyRallyPoint != null ? enemyRallyPoint.position : finalGridPosition;

            var go = Instantiate(prefab, spawnPos, Quaternion.identity);
            var view = go.GetComponent<UnitView>();
            if (view == null) { Debug.LogError($"Prefab {prefab.name} thiếu UnitView!"); continue; }
            view.Setup(unit);

            // Lưu vị trí cuối cùng trên lưới vào UnitView để sử dụng sau
            view.StoreOriginalPosition(finalGridPosition);

            unitViews.Add(view);
            Debug.Log($"[Spawn] {unit.UnitName} slot{slot} at {spawnPos}. Final grid pos: {finalGridPosition}");
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

        // 0. Hide UI
        yield return FadeUI(0f, 0.5f); // Fade to transparent

        // 1. Mờ đen, hiện ra ở vị trí lệch, rồi pan vào trung tâm của grid team địch
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
            // Camera pan từ trái qua, nhanh hơn và xa hơn
            yield return cameraManager.FadeInAndSetPosition(enemyGridCenter, 7.5f, Vector3.left * 20f, 1.0f);
        }
        else
        {
            Debug.LogError("[Intro] Không có enemy grid slots hợp lệ để tính trung tâm! Dùng vị trí mặc định.");
            yield return cameraManager.FadeInAndSetPosition(Vector3.zero, 10f, Vector3.zero, 0f);
        }

        // --- KỊCH BẢN NÂNG CẤP ---

        // Thêm một khoảng lặng ngắn để camera "đứng" tại vị trí địch
        yield return new WaitForSeconds(0.75f);

        // 2. Bắt đầu di chuyển camera lùi ra (dolly out) và cho địch lao ra CÙNG LÚC.
        float dollyOutDuration = 2.0f;
        StartCoroutine(cameraManager.ZoomOutToFinalView(dollyOutDuration));

        var enemyViews = unitViews.Where(v => v.LinkedUnit != null && !v.LinkedUnit.IsPlayer).ToList();
        if (enemyViews.Count > 0)
        {
            // 3. "Tướng địch" (chọn tên ở giữa) lao ra trước tiên.
            var leader = enemyViews[enemyViews.Count / 2];
            enemyViews.Remove(leader); // Xóa khỏi danh sách để không xử lý lại
            StartCoroutine(MoveUnitToPosition(leader, leader.GetOriginalPosition(), 0.5f));

            // 4. Sau một khoảng trễ ngắn, những tên còn lại lao ra.
            yield return new WaitForSeconds(0.2f);

            foreach (var follower in enemyViews)
            {
                // Cho chúng lao ra với tốc độ và thời gian trễ ngẫu nhiên nhẹ để tạo sự hỗn loạn có tổ chức
                float randomSpeed = Random.Range(0.4f, 0.6f);
                StartCoroutine(MoveUnitToPosition(follower, follower.GetOriginalPosition(), randomSpeed));
                yield return new WaitForSeconds(Random.Range(0.05f, 0.15f));
            }
        }

        // 5. Đợi cho camera hoàn thành việc lùi ra.
        yield return new WaitForSeconds(dollyOutDuration);

        // 6. Show UI again
        yield return FadeUI(1f, 0.5f); // Fade to opaque

        // 7. Trận đấu bắt đầu
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
        // unitView.SetAnimationTrigger("Rush"); // TẠM VÔ HIỆU HÓA - Gây lỗi trên một số model
        // Tạm thời vô hiệu hóa animation để tránh lỗi "State could not be found"
        // Sẽ tìm giải pháp tốt hơn sau

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
        // unitView.ForcePlayAnimationState("Idle"); // TẠM VÔ HIỆU HÓA - Gây lỗi trên một số model
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
        Debug.Log($"\n=== ROUND {CurrentRound} ===");

        // Tạo danh sách lượt đánh dựa trên Speed, ngẫu nhiên hóa các unit có cùng speed
        var allUnits = PlayerUnits.Where(u => u.IsAlive).Concat(EnemyUnits.Where(u => u.IsAlive));
        ActionOrder = allUnits.OrderByDescending(u => u.Speed).ThenBy(u => Random.value).ToList();

        Debug.Log("--- Turn Order ---");
        for(int i = 0; i < ActionOrder.Count; i++)
        {
            Debug.Log($"{i+1}. {ActionOrder[i].UnitName} (Speed: {ActionOrder[i].Speed})");
        }

        OnRoundSetup?.Invoke(ActionOrder);

        // Gọi thủ công UI để đảm bảo nó được vẽ sau khi ActionOrder đã hoàn chỉnh
        var turnOrderUI = FindFirstObjectByType<TurnOrderUIController>();
        if (turnOrderUI != null)
        {
            turnOrderUI.RebuildTurnOrderUI(ActionOrder);
        }

        // AI planning will be done in ExecuteRound, turn-by-turn
        OnEnemyPlanDone?.Invoke();
        stateMachine.TransitionTo(CombatPhase.Execute);
    }

    private void StartPlayerPlan()
    {
        // This phase is now deprecated and replaced by turn-by-turn input.
        // The logic has been moved to ExecuteRound.
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

        isWaitingForPlayerInput = false; // Signal to the ExecuteRound coroutine that the player has made a choice.
    }


    private IEnumerator DoRetargetCheck()
    {
        // VÔ HIỆU HÓA TẠM THỜI THEO YÊU CẦU
        // Logic này tự động buộc người chơi phải target kẻ địch đang tấn công mình.
        // Bằng cách vô hiệu hóa nó, lựa chọn thủ công của người chơi sẽ được tôn trọng 100%.
        /*
        bool retargetHappened = false;

        // Logic mới: Người chơi bị buộc phải đối đầu
        foreach (var player in PlayerUnits.Where(p => p.IsAlive))
        {
            var attacker = EnemyUnits.FirstOrDefault(e => e.IsAlive && e.SelectedTargets.Contains(player));

            if (attacker != null)
            {
                // Nếu người chơi đã chọn skill và mục tiêu không phải là kẻ đang tấn công mình
                if (player.SelectedSkill != null && !player.SelectedTargets.Contains(attacker))
                {
                    string prevTargetName = player.SelectedTargets.Any() ? player.SelectedTargets[0].UnitName : "none";
                    Debug.Log($"[Retarget] {player.UnitName} was targeting {prevTargetName}, but is being attacked by {attacker.UnitName}. Forcing retarget.");
                    player.SelectSkill(player.SelectedSkill, new List<CombatUnit> { attacker });
                    retargetHappened = true;
                }
            }
        }

        if (retargetHappened)
        {
            Debug.Log("[Retarget] Targets were changed. Redrawing arrows and waiting.");
            arrowController.DrawAllArrows();
            yield return new WaitForSeconds(1.0f); 
        }
        */

        yield return null; // Logic is disabled, wait a frame before continuing.
        stateMachine.TransitionTo(CombatPhase.Execute);
    }

    private IEnumerator ExecuteRound()
    {
        OnExecuteStarted?.Invoke();
        Debug.Log("\n--- EXECUTE ---");

        // Tạm thời vô hiệu hóa Fade UI để TurnOrder luôn hiển thị
        // yield return FadeUI(0f, 0.2f); 

        // Vòng lặp chính thực thi theo lượt
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

            // 1. Lên kế hoạch hành động (AI hoặc Player)
            if (!currentUnit.IsPlayer)
            {
                // AI tự lên kế hoạch
                enemyAI.PlanTurn(currentUnit, PlayerUnits);
            }
            else
            {
                // Đến lượt người chơi -> Đợi input
                Debug.Log($"[Execute] {currentUnit.UnitName} is a player. Waiting for input...");
                isWaitingForPlayerInput = true;
                OnPlayerTurnStart?.Invoke(currentUnit);

                // Tạm dừng coroutine cho đến khi người chơi chọn hành động và isWaitingForPlayerInput được set về false
                yield return new WaitUntil(() => !isWaitingForPlayerInput);
            }

            // 2. Thực thi hành động và animation
            if (currentUnit.SelectedSkill != null && currentUnit.SelectedTargets.Any())
            {
                var actionResult = actionResolver.Resolve(currentUnit, currentUnit.SelectedSkill, currentUnit.SelectedTargets);

                // Gửi sự kiện để các hệ thống khác (như UI) cập nhật
                OnActionResolved?.Invoke(actionResult);

                // Chạy animation và ĐỢI cho nó xong
                if (clashSequence != null)
                {
                    yield return StartCoroutine(clashSequence.PlayAction(actionResult));
                }
                else
                {
                    Debug.LogError("[ExecuteRound] ClashAnimationSequence chưa được gán!");
                    actionResult.ApplyOutcomes(); // Apply outcomes immediately if no animation
                    yield return new WaitForSeconds(1f); 
                }
            }
            else
            {
                Debug.LogWarning($"[Execute] {currentUnit.UnitName} không có hành động nào được chọn.");
            }

            // 3. Kiểm tra điều kiện kết thúc trận đấu sau mỗi hành động
            if (CheckForCombatEnd())
            {
                yield break; // Thoát khỏi coroutine
            }
        }

        // 4. Khi tất cả đã hành động, xóa lựa chọn của tất cả các unit và chuyển sang kết thúc vòng
        foreach (var unit in PlayerUnits.Concat(EnemyUnits))
        {
            unit.ClearSelection();
        }
        Debug.Log("--- Tất cả các lượt đã thực hiện ---");
        
        // Tạm thời vô hiệu hóa Fade UI
        // yield return FadeUI(1f, 0.2f);

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

    private List<HitData> CalculateHits(CombatUnit attacker, CombatUnit target, SkillData skill)
    {
        var hits = new List<HitData>();
        int hitCount = Mathf.Max(1, skill.hitCount);
        int raw = Mathf.RoundToInt(attacker.ATK * attacker.GetBuffMultiplier(StatType.ATK));
        int defend = target.PDEF;
        int totalDmg = Mathf.Max(hitCount, raw - defend);
        for (int i = 0; i < hitCount; i++)
        {
            int dmg = (i == hitCount - 1)
                ? totalDmg - (totalDmg / hitCount) * (hitCount - 1)
                : totalDmg / hitCount;
            hits.Add(new HitData { Damage = dmg, HitIndex = i });
        }
        return hits;
    }

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

        // Một cuộc "Clash" trực quan xảy ra khi và chỉ khi cả hai đơn vị
        // cùng sử dụng kỹ năng loại Clash VÀ cùng nhắm vào nhau.
        // Logic này đối xứng và không phụ thuộc vào ActionOrder,
        // đảm bảo việc vẽ đường luôn chính xác trong giai đoạn lập kế hoạch.

        bool aTargetsBWithClash = unitA.SelectedSkill?.type == SkillType.Clash && unitA.SelectedTargets.Contains(unitB);
        bool bTargetsAWithClash = unitB.SelectedSkill?.type == SkillType.Clash && unitB.SelectedTargets.Contains(unitA);

        return aTargetsBWithClash && bTargetsAWithClash;
    }
}