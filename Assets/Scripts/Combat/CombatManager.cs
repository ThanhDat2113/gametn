using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class CombatManager : MonoBehaviour
{
    public static CombatManager Instance { get; private set; }

    // ── References ────────────────────────────────────────────
    private CombatStateMachine stateMachine = new();
    private ClashResolver clashResolver = new();
    private EnemyAI enemyAI = new();

    // ── Units ─────────────────────────────────────────────────
    public List<CombatUnit> PlayerUnits { get; private set; } = new();
    public List<CombatUnit> EnemyUnits { get; private set; } = new();

    // ── Views ─────────────────────────────────────────────────
    [Header("Grid Spawn Settings")]
    [Tooltip("9 Transform tương ứng 9 ô lưới 3x3 phía player (index 0-8)")]
    public Transform[] playerGridSlots;
    [Tooltip("9 Transform tương ứng 9 ô lưới 3x3 phía enemy (index 0-8)")]
    public Transform[] enemyGridSlots;

    private List<UnitView> unitViews = new();

    // Col0 (slot 0,1,2) = Front → Row 2
    // Col1 (slot 3,4,5) = Mid   → Row 1
    // Col2 (slot 6,7,8) = Back  → Row 0
    private static int SlotToRow(int slot) => 2 - (slot / 3);

    // ── Planning ──────────────────────────────────────────────
    private int planningIndex = 0;

    // ── Round ─────────────────────────────────────────────────
    public int CurrentRound { get; private set; } = 0;

    // ── Animation ─────────────────────────────────────────────
    [Header("Animation")]
    public ClashAnimationSequence clashSequence;

    // ── Events ────────────────────────────────────────────────
    public event System.Action OnCombatStarted;
    // Legacy: gọi từng nhân vật một (không dùng với UI mới)
    public event System.Action<CombatUnit> OnPlayerUnitPlanning;
    // UI mới: fired khi bắt đầu PlayerPlan, truyền toàn bộ danh sách để UI tự xử lý
    public event System.Action<List<CombatUnit>> OnPlayerPlanStarted;
    // Fired sau khi enemy đã plan xong — dùng để hiện mũi tên chỉ target
    public event System.Action OnEnemyPlanDone;
    public event System.Action OnExecuteStarted;
    public event System.Action<ClashResult> OnClashResolved;
    public event System.Action OnRoundEnded;
    public event System.Action OnVictory;
    public event System.Action OnDefeat;

    // ── Public ────────────────────────────────────────────────
    public CombatPhase CurrentPhase => stateMachine.Current;

    // ─────────────────────────────────────────────────────────
    private void Awake()
    {
        if (Instance != null) { Destroy(gameObject); return; }
        Instance = this;
        stateMachine.OnPhaseChanged += HandlePhaseChanged;
    }

    // ── Init ──────────────────────────────────────────────────
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

    // Overload tiện dụng cho CombatTestUI
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

    // ── Spawn Views ───────────────────────────────────────────
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

    // ── Phase Handler ─────────────────────────────────────────
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

    // ── ENEMY PLAN ────────────────────────────────────────────
    private void StartEnemyPlan()
    {
        CurrentRound++;
        Debug.Log($"\n=== ROUND {CurrentRound} ===");

        foreach (var enemy in EnemyUnits.Where(e => e.IsAlive))
            enemyAI.PlanTurn(enemy, PlayerUnits);

        OnEnemyPlanDone?.Invoke();
        stateMachine.TransitionTo(CombatPhase.PlayerPlan);
    }

    // ── PLAYER PLAN ───────────────────────────────────────────
    private void StartPlayerPlan()
    {
        planningIndex = 0;
        // Fire new UI event với toàn bộ danh sách player alive
        var alivePlayers = PlayerUnits.Where(u => u.IsAlive).ToList();
        OnPlayerPlanStarted?.Invoke(alivePlayers);
        // Legacy fallback nếu không có listener mới
        if (OnPlayerPlanStarted == null)
            RequestNextPlayerInput();
    }

    private void RequestNextPlayerInput()
    {
        while (planningIndex < PlayerUnits.Count &&
               !PlayerUnits[planningIndex].IsAlive)
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

    /// <summary>
    /// UI mới: submit tất cả lựa chọn cùng lúc sau khi player nhấn CONFIRM.
    /// choices: list theo thứ tự hành động (index 0 = hành động đầu tiên)
    /// </summary>
    public void SubmitAllPlayerChoices(
        List<(CombatUnit unit, SkillData skill, List<CombatUnit> targets)> choices)
    {
        if (stateMachine.Current != CombatPhase.PlayerPlan)
        {
            Debug.LogWarning("[CombatManager] SubmitAllPlayerChoices gọi sai phase!");
            return;
        }

        // Reorder PlayerUnits theo thứ tự player đã sắp xếp
        // (ảnh hưởng đến IndexOf → PlanningOrder → Retarget)
        var orderedUnits = choices.Select(c => c.unit).ToList();
        foreach (var unit in PlayerUnits.Where(u => !orderedUnits.Contains(u)))
            orderedUnits.Add(unit); // unit chưa chọn (dead, etc.) thêm vào cuối
        PlayerUnits.Clear();
        PlayerUnits.AddRange(orderedUnits);

        // Gán skill và target
        foreach (var (unit, skill, targets) in choices)
        {
            unit.SelectSkill(skill, targets);
            Debug.Log($"[Player] {unit.UnitName} chọn [{skill.skillName}] " +
                      $"→ [{string.Join(", ", targets.Select(t => t.UnitName))}]");
        }

        stateMachine.TransitionTo(CombatPhase.RetargetCheck);
    }

    /// <summary>
    /// Legacy: submit từng nhân vật một (dùng cho CombatTestUI cũ)
    /// </summary>
    public void SubmitPlayerChoice(SkillData skill, List<CombatUnit> targets)
    {
        if (stateMachine.Current != CombatPhase.PlayerPlan)
        {
            Debug.LogWarning("[CombatManager] SubmitPlayerChoice gọi sai phase!");
            return;
        }

        var unit = PlayerUnits[planningIndex];
        unit.SelectSkill(skill, targets);

        Debug.Log($"[Player] {unit.UnitName} chọn [{skill.skillName}] " +
                  $"→ [{string.Join(", ", targets.Select(t => t.UnitName))}]");

        planningIndex++;
        Debug.Log($"[CombatManager] planningIndex = {planningIndex}/{PlayerUnits.Count}");
        RequestNextPlayerInput();
    }

    // ── RETARGET CHECK ────────────────────────────────────────
    // Logic mới (dựa trên planning order):
    // - Nếu có player nhắm vào enemy này → enemy clash với player ĐẦU TIÊN
    //   trong planning order nhắm nó (index thấp nhất = hành động sớm nhất)
    // - Nếu không có player nào nhắm → enemy giữ nguyên target AI đã chọn (free attack)
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
                // Không ai nhắm → giữ target gốc AI đã chọn, không làm gì
                Debug.Log($"[Retarget] {enemy.UnitName}: không bị nhắm → " +
                          $"giữ target [{string.Join(", ", enemy.SelectedTargets.Select(t => t.UnitName))}]");
                continue;
            }

            // Có người nhắm → clash với player ĐẦU TIÊN trong planning order
            // PlayerUnits đã được reorder theo planning order từ SubmitAllPlayerChoices
            // → index thấp nhất = hành động sớm nhất
            var firstPlayer = playersTargetingEnemy
                .OrderBy(p => PlayerUnits.IndexOf(p))
                .First();

            string prevTarget = string.Join(", ", enemy.SelectedTargets.Select(t => t.UnitName));
            enemy.SelectSkill(enemy.SelectedSkill, new List<CombatUnit> { firstPlayer });
            Debug.Log($"[Retarget] {enemy.UnitName}: {prevTarget} → {firstPlayer.UnitName} " +
                      $"(player đầu tiên trong planning order nhắm nó)");
        }

        stateMachine.TransitionTo(CombatPhase.Execute);
    }

    // ── EXECUTE ───────────────────────────────────────────────
    private IEnumerator ExecuteRound()
    {
        OnExecuteStarted?.Invoke();
        Debug.Log("\n--- EXECUTE ---");

        // allUnits xử lý theo planning order:
        // PlayerUnits đã được reorder bởi SubmitAllPlayerChoices → giữ nguyên thứ tự
        // EnemyUnits theo thứ tự spawn
        var allUnits = PlayerUnits.Concat(EnemyUnits)
                                   .Where(u => u.IsAlive)
                                   .ToList();

        // ── Bước 1: Xây dựng clash pairs + afterClash queue ──
        //
        // Quy tắc:
        // - Nếu enemy nhắm player A (sau retarget) VÀ player A nhắm enemy
        //   VÀ cả hai dùng Clash skill → tạo clash pair
        // - Các player KHÁC cùng nhắm enemy đó → afterClashQueue (đánh sau)
        // - Enemy không có clash pair → Bước 4 xử lý (free attack target gốc)
        // - Player không có clash pair và không trong afterClashQueue → Bước 4

        var clashPairs = new List<(CombatUnit player, CombatUnit enemy)>();
        var handledUnits = new HashSet<CombatUnit>();
        var afterClashQueue = new Dictionary<CombatUnit, List<(CombatUnit player, SkillData skill)>>();

        foreach (var enemy in EnemyUnits.Where(e => e.IsAlive))
        {
            if (enemy.SelectedSkill?.type != SkillType.Clash) continue;
            if (enemy.SelectedTargets.Count == 0) continue;

            // Sau retarget, SelectedTargets[0] là player enemy sẽ clash
            var clashPlayer = enemy.SelectedTargets[0];

            if (!clashPlayer.IsAlive) continue;
            if (clashPlayer.SelectedSkill?.type != SkillType.Clash) continue;
            if (!clashPlayer.SelectedTargets.Contains(enemy)) continue;

            // Xác nhận clash pair
            clashPairs.Add((clashPlayer, enemy));
            handledUnits.Add(clashPlayer);
            handledUnits.Add(enemy);

            Debug.Log($"[ClashPair] {clashPlayer.UnitName} ↔ {enemy.UnitName}");

            // Các player KHÁC cùng nhắm enemy này → đánh sau clash
            // Thứ tự theo planning order (PlayerUnits đã được sort theo planning order)
            // Snapshot skill ngay bây giờ trước khi bị ClearSelection
            var otherPlayers = PlayerUnits
                .Where(p => p.IsAlive
                         && p != clashPlayer
                         && p.SelectedSkill != null
                         && p.SelectedTargets.Contains(enemy))
                .OrderBy(p => PlayerUnits.IndexOf(p))  // planning order
                .Select(p => (player: p, skill: p.SelectedSkill))
                .ToList();

            if (otherPlayers.Count > 0)
            {
                afterClashQueue[enemy] = otherPlayers;
                foreach (var (p, _) in otherPlayers)
                {
                    handledUnits.Add(p);
                    Debug.Log($"[AfterClash] {p.UnitName} sẽ tấn công {enemy.UnitName} sau clash");
                }
            }
        }

        // ── Bước 2: Xử lý từng Clash pair ────────────────────
        foreach (var (player, enemy) in clashPairs)
        {
            Debug.Log($"\n[CLASH] {player.UnitName} ↔ {enemy.UnitName}");

            var result = clashResolver.Resolve(
                player, enemy,
                player.SelectedSkill,
                enemy.SelectedSkill);

            Debug.Log($"  → [{result.Winner.UnitName}] thắng " +
                      $"({result.WinnerScore} vs {result.LoserScore})");

            var playerView = unitViews.Find(v => v.LinkedUnit == player);
            var enemyView = unitViews.Find(v => v.LinkedUnit == enemy);
            var winnerView = result.Winner.IsPlayer ? playerView : enemyView;

            var hits = CalculateHits(result.Winner, result.Loser, result.WinnerSkill);
            winnerView?.SetPendingHits(hits, result.Loser);
            winnerView?.SetCurrentSkill(result.WinnerSkill);

            OnClashResolved?.Invoke(result);

            if (clashSequence != null && playerView != null && enemyView != null)
            {
                bool done = false;
                yield return clashSequence.PlayFullClashSequence(
                    playerView, enemyView, result,
                    onComplete: () => done = true);
                yield return new WaitUntil(() => done);
            }
            else
            {
                result.Winner.ExecuteSelectedSkill();
                yield return new WaitForSeconds(0.5f);
            }

            yield return new WaitForSeconds(0.2f);

            // ── Bước 3: Các player còn lại tấn công sau clash ─
            if (afterClashQueue.TryGetValue(enemy, out var remainingPlayers))
            {
                foreach (var (remainPlayer, savedSkill) in remainingPlayers)
                {
                    if (!remainPlayer.IsAlive)
                    {
                        Debug.Log($"[AfterClash] {remainPlayer.UnitName} đã chết, skip");
                        continue;
                    }

                    if (!enemy.IsAlive)
                    {
                        Debug.Log($"[AfterClash] {enemy.UnitName} đã chết, " +
                                  $"{remainPlayer.UnitName} bỏ qua");
                        continue;
                    }

                    Debug.Log($"[AfterClash] {remainPlayer.UnitName} tấn công " +
                              $"{enemy.UnitName} bằng [{savedSkill.skillName}]");
                    Debug.Log($"[AfterClash] AnimTrigger: " +
                              $"'{savedSkill.animationTrigger}'");

                    // Truyền skill đã lưu trước khi bị clear
                    yield return StartCoroutine(
                        ExecuteFreeAttack(remainPlayer, enemy, savedSkill));

                    yield return new WaitForSeconds(0.2f);
                }
            }

            yield return new WaitForSeconds(0.1f);
        }

        // ── Bước 4: Xử lý unit chưa được xử lý ──────────────
        foreach (var unit in allUnits.Where(u => !handledUnits.Contains(u)))
        {
            if (unit.SelectedSkill == null) continue;
            if (!unit.IsAlive) continue;

            var savedSkill = unit.SelectedSkill;

            if (savedSkill.type == SkillType.Clash)
            {
                var targets = unit.SelectedTargets.Where(t => t.IsAlive).ToList();
                if (targets.Count == 0) continue;

                Debug.Log($"\n[FreeAttack] {unit.UnitName} → {targets[0].UnitName} " +
                          $"[{savedSkill.skillName}]");

                yield return StartCoroutine(
                    ExecuteFreeAttack(unit, targets[0], savedSkill));
            }
            else
            {
                Debug.Log($"\n[AUTO] {unit.UnitName} → {savedSkill.skillName}");
                unit.ExecuteSelectedSkill();
                yield return new WaitForSeconds(0.3f);
            }

            yield return new WaitForSeconds(0.1f);
        }

        // ── Dọn selection ─────────────────────────────────────
        foreach (var u in allUnits) u.ClearSelection();

        // ── Kiểm tra kết thúc ─────────────────────────────────
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

    // ── Free Attack ───────────────────────────────────────────
    private IEnumerator ExecuteFreeAttack(CombatUnit attacker,
                                           CombatUnit target,
                                           SkillData skill)
    {
        if (skill == null)
        {
            Debug.LogWarning($"[ExecuteFreeAttack] {attacker.UnitName} skill = null!");
            yield break;
        }

        if (!attacker.IsAlive || !target.IsAlive) yield break;

        Debug.Log($"[ExecuteFreeAttack] {attacker.UnitName} [{skill.skillName}] " +
                  $"→ {target.UnitName}");

        var attackerView = unitViews.Find(v => v.LinkedUnit == attacker);
        var hits = CalculateHits(attacker, target, skill);

        if (attackerView == null)
        {
            Debug.LogWarning($"[ExecuteFreeAttack] Không tìm thấy view của {attacker.UnitName}!");
            foreach (var hit in hits) target.TakeDamage(hit.Damage, hit.HitIndex);
            yield break;
        }

        var targetView = unitViews.Find(v => v.LinkedUnit == target);
        Vector3 origin = attackerView.transform.position;
        Vector3 targetPos = targetView != null ? targetView.transform.position : origin;
        Vector3 dir = (targetPos - origin).normalized;
        Vector3 rushDest = targetPos - dir * clashSequence.faceOffDistance;

        // Phase 1: Rush
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

        // Phase 2: Attack
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
            Debug.LogWarning($"[ExecuteFreeAttack] '{skill.skillName}' không có animationTrigger!");
            foreach (var hit in hits) target.TakeDamage(hit.Damage, hit.HitIndex);
            yield return new WaitForSeconds(0.3f);
        }
        attackerView.ClearPendingHits();

        yield return new WaitForSeconds(clashSequence.postSkillWait);

        // Phase 3: Return
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

    // ── Public Helpers ────────────────────────────────────────
    public UnitView GetUnitView(CombatUnit unit) =>
        unitViews.Find(v => v.LinkedUnit == unit);

    // ── Calculate Hits ────────────────────────────────────────
    private List<HitData> CalculateHits(CombatUnit attacker,
                                         CombatUnit target,
                                         SkillData skill)
    {
        var hits = new List<HitData>();
        int hitCount = Mathf.Max(1, skill.hitCount);

        int raw = Mathf.RoundToInt(attacker.ATK
                       * attacker.GetBuffMultiplier(StatType.ATK));
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

    // ── ROUND END ─────────────────────────────────────────────
    private void DoRoundEnd()
    {
        foreach (var u in PlayerUnits.Concat(EnemyUnits).Where(u => u.IsAlive))
            u.TickCooldowns();

        OnRoundEnded?.Invoke();
        Debug.Log("--- ROUND END ---\n");

        stateMachine.TransitionTo(CombatPhase.EnemyPlan);
    }

    // ── END CONDITIONS ────────────────────────────────────────
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