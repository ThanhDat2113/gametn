using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Game.Combat;

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
    private const int MAX_PLAYER_AP = 5; // Giới hạn AP có thể tích trữ
    private const int STARTING_PLAYER_AP = 3; // AP khởi đầu
    private bool isFirstPlayerTurnOfRound;

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
    public event System.Action<int> OnAPChanged; // (newAPValue)

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

            // Vị trí cuối cùng trên lưới
            Vector3 finalGridPosition = gridSlots[slot].position;

            // Nếu là enemy, spawn tại rally point. Nếu không, spawn tại grid.
            bool isEnemy = !unit.IsPlayer;
            Vector3 spawnPos = isEnemy && enemyRallyPoint != null ? enemyRallyPoint.position : finalGridPosition;

            var go = Instantiate(prefab, spawnPos, Quaternion.identity);
            var view = go.GetComponent<UnitView>();
            if (view == null) { Debug.LogError($"Prefab {prefab.name} thiếu UnitView!"); continue; }
            view.Setup(unit);
            InitializePassives(unit); // GỌI KHỞI TẠO NỘI TẠI

            // Lưu vị trí cuối cùng trên lưới vào UnitView để sử dụng sau
            view.StoreOriginalPosition(finalGridPosition);

            unitViews.Add(view);
            Debug.Log($"[Spawn] {unit.UnitName} slot{slot} at {spawnPos}. Final grid pos: {finalGridPosition}");
        }
    }

    private void InitializePassives(CombatUnit unit)
    {
        if (unit.Data.passiveAbility != null)
        {
            var passiveInstance = Instantiate(unit.Data.passiveAbility);
            unit.SetPassive(passiveInstance);
            Debug.Log($"[Passive] Initialized passive '{passiveInstance.name.Replace("(Clone)","")}' for {unit.UnitName}.");
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
        // Kích hoạt animation chạy (nếu có)
        unitView.SetAnimationTrigger("Rush");

        Vector3 startPosition = unitView.transform.position;
        float elapsed = 0f;

        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / duration;
            // Sử dụng EaseOutQuad để di chuyển mượt hơn
            unitView.transform.position = Vector3.Lerp(startPosition, targetPosition, t * t);
            yield return null;
        }

        unitView.transform.position = targetPosition;
        
        // Đảm bảo unit chuyển về trạng thái Idle sau khi di chuyển xong
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
        isFirstPlayerTurnOfRound = true; // Reset lại cờ mỗi đầu round
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

        if (skill.apCost > CurrentPlayerAP)
        {
            Debug.LogWarning($"[AP] Không đủ AP để dùng {skill.skillName}. Cần {skill.apCost}, có {CurrentPlayerAP}.");
            // Có thể thêm một event ở đây để báo cho UI biết là hành động không thành công
            return;
        }

        var currentUnit = ActionOrder[turnIndex];
        if (currentUnit == null || !currentUnit.IsPlayer)
        {
            Debug.LogError("SubmitPlayerTurnAction called, but current unit is not a player or is null.");
            return;
        }

        // Trừ AP và kích hoạt sự kiện
        CurrentPlayerAP -= skill.apCost;
        OnAPChanged?.Invoke(CurrentPlayerAP);
        currentUnit.SpendAP(skill.apCost); // Kích hoạt passive của unit
        Debug.Log($"[AP] Đã dùng {skill.apCost} AP. Còn lại {CurrentPlayerAP}.");

        currentUnit.SelectSkill(skill, targets);
        Debug.Log($"[Player Turn] {currentUnit.UnitName} selected [{skill.skillName}] -> [{string.Join(", ", targets.Select(t => t.UnitName))}]");

        isWaitingForPlayerInput = false; // Signal to the ExecuteRound coroutine that the player has made a choice.
    }

    private IEnumerator HandleStartOfTurnEffects(CombatUnit unit)
    {
        var burnStatus = unit.GetActiveStatus(StatusEffectType.ThieuDot);
        if (burnStatus != null)
        {
            int burnDamage = Mathf.RoundToInt(burnStatus.Value);
            Debug.Log($"<color=red>[ThieuDot] {unit.UnitName} nhận {burnDamage} sát thương thiêu đốt.</color>");
            unit.TakeDamage(null, burnDamage); // Sát thương không có caster
            
            // Có thể thêm animation/VFX ở đây
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

            // Xử lý hiệu ứng đầu lượt (ví dụ: Burn)
            yield return StartCoroutine(HandleStartOfTurnEffects(currentUnit));
            if (!currentUnit.IsAlive)
            {
                Debug.Log($"[Execute] {currentUnit.UnitName} đã chết do hiệu ứng đầu lượt.");
                // 3. Kiểm tra điều kiện kết thúc trận đấu sau mỗi hành động
                if (CheckForCombatEnd())
                {
                    yield break; // Kết thúc coroutine ExecuteRound
                }
                continue; // Bỏ qua phần còn lại của lượt
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
                // Hồi AP nếu đây không phải là lượt đầu tiên của người chơi trong round
                if (!isFirstPlayerTurnOfRound)
                {
                    if (CurrentPlayerAP < MAX_PLAYER_AP)
                    {
                        CurrentPlayerAP++;
                        OnAPChanged?.Invoke(CurrentPlayerAP);
                        Debug.Log($"[AP] Hồi 1 AP. Hiện có: {CurrentPlayerAP}");
                    }
                }
                isFirstPlayerTurnOfRound = false; // Đánh dấu đã qua lượt người chơi đầu tiên

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

            // Tick status effects at the end of the turn
            currentUnit.TickStatuses();

            // Nội tại của NoName: Hồi 5% HP tối đa vào cuối lượt
            if (currentUnit.UnitName == "NoName")
            {
                int healAmount = Mathf.RoundToInt(currentUnit.MaxHP * 0.05f);
                currentUnit.Heal(healAmount);
                Debug.Log($"[Passive] NoName recovered {healAmount} HP.");
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

    private void DoRoundEnd()
    {
        OnRoundEnded?.Invoke();
        Debug.Log("--- ROUND END ---\n");
        // The state transition is now handled by the EnemyPlan phase start.
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