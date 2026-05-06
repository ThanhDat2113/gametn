using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class CombatManager : MonoBehaviour
{
    public static CombatManager Instance { get; private set; }

    private CombatStateMachine stateMachine = new();
    private ClashResolver clashResolver = new();
    private EnemyAI enemyAI = new();

    public List<CombatUnit> PlayerUnits { get; private set; } = new();
    public List<CombatUnit> EnemyUnits { get; private set; } = new();

    [Header("Grid Spawn Settings")]
    public Transform[] playerGridSlots;
    public Transform[] enemyGridSlots;
    private List<UnitView> unitViews = new();



    private static int SlotToRow(int slot) => 2 - (slot / 3);

    private int planningIndex = 0;
    public int CurrentRound { get; private set; } = 0;

    [Header("Animation")]
    public ClashAnimationSequence clashSequence;
    public CombatCameraManager cameraManager;

    public event System.Action OnCombatStarted;
    public event System.Action<CombatUnit> OnPlayerUnitPlanning;
    public event System.Action<List<CombatUnit>> OnPlayerPlanStarted;
    public event System.Action<CombatUnit> OnPlayerSkillSelected;
    public event System.Action OnEnemyPlanDone;
    public event System.Action OnExecuteStarted;
    public event System.Action<ClashResult> OnClashResolved;
    public event System.Action OnRoundEnded;
    public event System.Action OnVictory;
    public event System.Action OnDefeat;

    public CombatPhase CurrentPhase => stateMachine.Current;

    private void Awake()
    {
        if (Instance != null) { Destroy(gameObject); return; }
        Instance = this;
        stateMachine.OnPhaseChanged += HandlePhaseChanged;

        if (cameraManager == null)
            cameraManager = FindFirstObjectByType<CombatCameraManager>();

        // Đảm bảo TargetingArrowController tồn tại trong scene
        if (gameObject.GetComponent<TargetingArrowController>() == null)
        {
            gameObject.AddComponent<TargetingArrowController>();
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
        stateMachine.TransitionTo(CombatPhase.EnemyPlan);
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
            var go = Instantiate(prefab, gridSlots[slot].position, Quaternion.identity);
            var view = go.GetComponent<UnitView>();
            if (view == null) { Debug.LogError($"Prefab {prefab.name} thiếu UnitView!"); continue; }
            view.Setup(unit);
            unitViews.Add(view);
            Debug.Log($"[Spawn] {unit.UnitName} slot{slot} row{unit.GridRow}");
        }
    }

    private void HandlePhaseChanged(CombatPhase prev, CombatPhase next)
    {
        switch (next)
        {
            case CombatPhase.EnemyPlan: StartEnemyPlan(); break;
            case CombatPhase.PlayerPlan: StartPlayerPlan(); break;
            case CombatPhase.RetargetCheck: DoRetargetCheck(); break;
            case CombatPhase.Execute: StartCoroutine(ExecuteRound()); break;
            case CombatPhase.RoundEnd: DoRoundEnd(); break;
            case CombatPhase.Victory: DoVictory(); break;
            case CombatPhase.Defeat: DoDefeat(); break;
        }
    }

    private void StartEnemyPlan()
    {
        CurrentRound++;
        Debug.Log($"\n=== ROUND {CurrentRound} ===");

        foreach (var enemy in EnemyUnits.Where(e => e.IsAlive))
            enemyAI.PlanTurn(enemy, PlayerUnits);

        OnEnemyPlanDone?.Invoke();
        stateMachine.TransitionTo(CombatPhase.PlayerPlan);
    }

    private void StartPlayerPlan()
    {
        // Reset vị trí của tất cả các unit về vị trí ban đầu
        foreach (var view in unitViews)
        {
            if (view != null && view.gameObject.activeInHierarchy)
            {
                view.ResetPosition();
            }
        }

        planningIndex = 0;
        var alivePlayers = PlayerUnits.Where(u => u.IsAlive).ToList();
        OnPlayerPlanStarted?.Invoke(alivePlayers);
        if (OnPlayerPlanStarted == null)
            RequestNextPlayerInput();
    }

    private void RequestNextPlayerInput()
    {
        while (planningIndex < PlayerUnits.Count && !PlayerUnits[planningIndex].IsAlive)
            planningIndex++;

        Debug.Log($"[CombatManager] RequestNextPlayerInput index={planningIndex}");

        if (planningIndex >= PlayerUnits.Count)
        {
            Debug.Log("[CombatManager] Tất cả đã chọn → RetargetCheck");
            stateMachine.TransitionTo(CombatPhase.RetargetCheck);
            return;
        }

        OnPlayerUnitPlanning?.Invoke(PlayerUnits[planningIndex]);
    }

    public void SubmitAllPlayerChoices(
        List<(CombatUnit unit, SkillData skill, List<CombatUnit> targets)> choices)
    {
        if (stateMachine.Current != CombatPhase.PlayerPlan)
        {
            Debug.LogWarning("[CombatManager] SubmitAllPlayerChoices gọi sai phase!");
            return;
        }

        var orderedUnits = choices.Select(c => c.unit).ToList();
        foreach (var unit in PlayerUnits.Where(u => !orderedUnits.Contains(u) && u.IsAlive))
            orderedUnits.Add(unit);
        PlayerUnits.Clear();
        PlayerUnits.AddRange(orderedUnits);

        foreach (var (unit, skill, targets) in choices)
        {
            unit.SelectSkill(skill, targets);
            Debug.Log($"[Player] {unit.UnitName} chọn [{skill.skillName}] → [{string.Join(", ", targets.Select(t => t.UnitName))}]");
        }

        stateMachine.TransitionTo(CombatPhase.RetargetCheck);
    }

    public void SubmitPlayerChoice(SkillData skill, List<CombatUnit> targets)
    {
        if (stateMachine.Current != CombatPhase.PlayerPlan)
        {
            Debug.LogWarning("[CombatManager] SubmitPlayerChoice gọi sai phase!");
            return;
        }

        var unit = PlayerUnits[planningIndex];
        unit.SelectSkill(skill, targets);

        Debug.Log($"[Player] {unit.UnitName} chọn [{skill.skillName}] → [{string.Join(", ", targets.Select(t => t.UnitName))}]");

        OnPlayerSkillSelected?.Invoke(unit);

        planningIndex++;
        Debug.Log($"[CombatManager] planningIndex = {planningIndex}/{PlayerUnits.Count}");
        RequestNextPlayerInput();
    }

    private void DoRetargetCheck()
    {
        foreach (var enemy in EnemyUnits.Where(e => e.IsAlive))
        {
            if (enemy.SelectedSkill == null) continue;

            var playersTargetingEnemy = PlayerUnits
                .Where(p => p.IsAlive && p.SelectedTargets.Contains(enemy))
                .ToList();

            if (playersTargetingEnemy.Count == 0)
            {
                Debug.Log($"[Retarget] {enemy.UnitName}: không bị nhắm → giữ target [{string.Join(", ", enemy.SelectedTargets.Select(t => t.UnitName))}]");
                continue;
            }

            var firstPlayer = playersTargetingEnemy
                .OrderBy(p => PlayerUnits.IndexOf(p))
                .First();

            string prevTarget = string.Join(", ", enemy.SelectedTargets.Select(t => t.UnitName));
            enemy.SelectSkill(enemy.SelectedSkill, new List<CombatUnit> { firstPlayer });
            Debug.Log($"[Retarget] {enemy.UnitName}: {prevTarget} → {firstPlayer.UnitName} (player đầu tiên trong planning order nhắm nó)");
        }

        stateMachine.TransitionTo(CombatPhase.Execute);
    }

    private IEnumerator ExecuteRound()
    {
        OnExecuteStarted?.Invoke();
        Debug.Log("\n--- EXECUTE (theo thứ tự Action Bar) ---");

        var playerOrder = PlayerUnits.Where(p => p.IsAlive).ToList();
        if (playerOrder.Count == 0) yield break;

        var clashedEnemies = new HashSet<CombatUnit>();

        for (int i = 0; i < playerOrder.Count; i++)
        {
            var player = playerOrder[i];
            if (!player.IsAlive) continue;
            if (player.SelectedSkill == null) continue;

            var skill = player.SelectedSkill;
            var targets = player.SelectedTargets.Where(t => t.IsAlive).ToList();
            if (targets.Count == 0) continue;

            CombatUnit target = targets[0];
            bool isEnemy = !target.IsPlayer;

            bool isClash = false;
            CombatUnit enemy = null;
            if (isEnemy && skill.type == SkillType.Clash)
            {
                enemy = target;
                if (enemy.SelectedSkill != null && enemy.SelectedSkill.type == SkillType.Clash &&
                    !clashedEnemies.Contains(enemy) &&
                    enemy.SelectedTargets != null && enemy.SelectedTargets.Contains(player))
                {
                    isClash = true;
                }
            }

            if (isClash)
            {
                Debug.Log($"\n[CLASH] {player.UnitName} ↔ {enemy.UnitName}");
                var result = clashResolver.Resolve(player, enemy, skill, enemy.SelectedSkill);
                Debug.Log($"  → [{result.Winner.UnitName}] thắng ({result.WinnerScore} vs {result.LoserScore})");

                var playerView = GetUnitView(player);
                var enemyView = GetUnitView(enemy);
                var winnerView = result.Winner.IsPlayer ? playerView : enemyView;

                var hits = CalculateHits(result.Winner, result.Loser, result.WinnerSkill);
                winnerView?.SetPendingHits(hits, result.Loser);
                winnerView?.SetCurrentSkill(result.WinnerSkill);

                OnClashResolved?.Invoke(result);

                if (clashSequence != null && playerView != null && enemyView != null)
                {
                    bool done = false;
                    yield return clashSequence.PlayFullClashSequence(playerView, enemyView, result, () => done = true);
                    yield return new WaitUntil(() => done);
                }
                else
                {
                    result.Winner.ExecuteSelectedSkill();
                    yield return new WaitForSeconds(0.5f);
                }

                clashedEnemies.Add(enemy);
                enemy.ClearSelection(); // Xóa skill của enemy để không thể clash/tấn công tiếp

                yield return new WaitForSeconds(0.2f);
            }
            else
            {
                Debug.Log($"\n[FreeAttack] {player.UnitName} → {target.UnitName} [{skill.skillName}]");
                yield return StartCoroutine(ExecuteFreeAttack(player, target, skill));
                yield return new WaitForSeconds(0.2f);
            }
        }

        // Enemy còn lại tấn công free
        foreach (var enemy in EnemyUnits.Where(e => e.IsAlive))
        {
            if (enemy.SelectedSkill == null) continue;
            if (enemy.SelectedTargets.Count == 0) continue;
            var target = enemy.SelectedTargets[0];
            if (target == null || !target.IsAlive) continue;

            Debug.Log($"\n[EnemyFreeAttack] {enemy.UnitName} → {target.UnitName} [{enemy.SelectedSkill.skillName}]");
            yield return StartCoroutine(ExecuteFreeAttack(enemy, target, enemy.SelectedSkill));
            yield return new WaitForSeconds(0.2f);
        }

        foreach (var u in PlayerUnits.Concat(EnemyUnits).Where(u => u.IsAlive))
            u.ClearSelection();

        if (!EnemyUnits.Any(e => e.IsAlive))
        {
            stateMachine.TransitionTo(CombatPhase.Victory);
            yield break;
        }
        if (!PlayerUnits.Any(p => p.IsAlive))
        {
            stateMachine.TransitionTo(CombatPhase.Defeat);
            yield break;
        }

        stateMachine.TransitionTo(CombatPhase.RoundEnd);
    }

    private IEnumerator ExecuteFreeAttack(CombatUnit attacker, CombatUnit target, SkillData skill)
    {
        if (skill == null || !attacker.IsAlive || !target.IsAlive) yield break;

        Debug.Log($"[ExecuteFreeAttack] {attacker.UnitName} [{skill.skillName}] → {target.UnitName}");

        var attackerView = GetUnitView(attacker);
        var hits = CalculateHits(attacker, target, skill);

        if (attackerView == null)
        {
            foreach (var hit in hits) target.TakeDamage(attacker, hit.Damage, hit.HitIndex);
            yield break;
        }

        var targetView = GetUnitView(target);
        Vector3 origin = attackerView.transform.position;
        Vector3 targetPos = targetView != null ? targetView.transform.position : origin;
        Vector3 dir = (targetPos - origin).normalized;
        Vector3 rushDest = targetPos - dir * clashSequence.faceOffDistance;

        attackerView.SetAnimationTrigger("Rush");
        float elapsed = 0f;
        while (elapsed < clashSequence.rushDuration)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.Clamp01(elapsed / clashSequence.rushDuration);
            attackerView.transform.position = Vector3.Lerp(origin, rushDest, t);
            yield return null;
        }
        attackerView.transform.position = rushDest;

        attackerView.SetCurrentSkill(skill);
        attackerView.SetPendingHits(hits, target);
        string trigger = skill.animationTrigger;
        if (!string.IsNullOrEmpty(trigger))
        {
            attackerView.SetAnimationTrigger(trigger);
            yield return StartCoroutine(attackerView.WaitUntilAnimationDone(trigger));
        }
        else
        {
            foreach (var hit in hits) target.TakeDamage(attacker, hit.Damage, hit.HitIndex);
            yield return new WaitForSeconds(0.3f);
        }
        attackerView.ClearPendingHits();

        yield return new WaitForSeconds(clashSequence.postSkillWait);

        // Reset camera về chế độ xem mặc định NGAY LẬP TỨC sau khi skill kết thúc
        if (cameraManager != null)
            cameraManager.AutoFitUnitsInView();

        attackerView.SetAnimationTrigger("Idle");
        Vector3 currentPos = attackerView.transform.position;
        elapsed = 0f;
        while (elapsed < clashSequence.returnDuration)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.Clamp01(elapsed / clashSequence.returnDuration);
            attackerView.transform.position = Vector3.Lerp(currentPos, origin, t);
            yield return null;
        }
        attackerView.transform.position = origin;
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
}