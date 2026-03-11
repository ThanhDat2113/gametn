using System.Collections.Generic;
using System.Linq;
using UnityEngine;

/// <summary>
/// Test UI dùng OnGUI để chạy combat với tối đa 5 nhân vật mỗi bên.
/// Thay thế CombatUI cũ.
///
/// Setup trong Inspector:
///   ─ Player Roster: kéo tối đa 5 CharacterData vào (có thể để trống)
///   ─ Player Levels:  level tương ứng cho từng slot (mặc định = 1)
///   ─ Player Slots:   gridSlot 0-8 cho từng nhân vật
///   ─ Enemy Group:    kéo EnemyGroupData asset vào
/// </summary>
public class CombatTestUI : MonoBehaviour
{
    [Header("Player Setup (tối đa 5)")]
    [Tooltip("Kéo CharacterData vào đây, để trống = không dùng slot đó")]
    public CharacterData[] playerRoster = new CharacterData[5];

    [Tooltip("Level của từng nhân vật (index tương ứng playerRoster)")]
    public int[] playerLevels = { 1, 1, 1, 1, 1 };

    [Tooltip("GridSlot 0-8 của từng nhân vật\n" +
             "0-2=Front, 3-5=Mid, 6-8=Back\n" +
             "Col: slot%3  (0=trái, 1=giữa, 2=phải)")]
    public int[] playerGridSlots = { 0, 1, 2, 3, 4 };

    [Header("Enemy Setup")]
    [Tooltip("Kéo EnemyGroupData asset vào đây")]
    public EnemyGroupData enemyGroup;

    // ── Internal ──────────────────────────────────────────────
    private CombatManager combat;
    private CombatUnit planningUnit;
    private SkillData selectedSkill;

    // ─────────────────────────────────────────────────────────
    private void Start()
    {
        combat = CombatManager.Instance;

        combat.OnPlayerUnitPlanning += unit =>
        {
            planningUnit = unit;
            selectedSkill = null;
        };
        combat.OnVictory += () => planningUnit = null;
        combat.OnDefeat += () => planningUnit = null;
    }

    // ─────────────────────────────────────────────────────────
    private void OnGUI()
    {
        GUILayout.BeginArea(new Rect(10, 10, 420, 900));

        // ── Chưa bắt đầu ──────────────────────────────────────
        if (combat.CurrentPhase == CombatPhase.Init ||
            combat.CurrentPhase == CombatPhase.Defeat)
        {
            GUILayout.Label($"Phase: {combat.CurrentPhase}");
            if (GUILayout.Button("START COMBAT", GUILayout.Height(40)))
                StartCombat();

            GUILayout.EndArea();
            return;
        }

        // ── HP bars ────────────────────────────────────────────
        DrawUnitList("── PLAYER ──", combat.PlayerUnits);
        DrawUnitList("── ENEMY ──", combat.EnemyUnits);
        GUILayout.Space(8);

        // ── Phase label ────────────────────────────────────────
        GUILayout.Label($"Phase: {combat.CurrentPhase}   " +
                        $"Round: {combat.CurrentRound}");

        // ── Chọn skill ────────────────────────────────────────
        if (combat.CurrentPhase == CombatPhase.PlayerPlan &&
            planningUnit != null)
        {
            DrawSkillSelection();
        }

        GUILayout.EndArea();
    }

    // ─────────────────────────────────────────────────────────
    private void DrawUnitList(string label, List<CombatUnit> units)
    {
        GUILayout.Label(label);
        foreach (var u in units)
        {
            string row = u.GridRow == 2 ? "Front" : u.GridRow == 1 ? "Mid" : "Back";
            string info = u.IsAlive
                ? $"  {u.UnitName} [{row}]  HP:{u.CurrentHP}/{u.MaxHP}"
                : $"  {u.UnitName}  [DEAD]";
            GUILayout.Label(info);
        }
        GUILayout.Space(4);
    }

    // ─────────────────────────────────────────────────────────
    private void DrawSkillSelection()
    {
        GUILayout.Label($"Chọn skill cho: {planningUnit.UnitName}");

        for (int i = 0; i < planningUnit.Data.skills.Length; i++)
        {
            var skill = planningUnit.Data.skills[i];
            bool ready = planningUnit.IsSkillReady(i);
            int cd = planningUnit.SkillCooldowns[i];

            string cdStr = ready ? "" : $" (CD:{cd})";
            string label = $"[{skill.skillName}]  Base:{skill.basePoint}" +
                            $"  {skill.type}{cdStr}";

            GUI.enabled = ready;
            if (GUILayout.Button(label))
                selectedSkill = skill;
            GUI.enabled = true;
        }

        if (selectedSkill != null)
        {
            GUILayout.Space(6);
            GUILayout.Label($"Chọn mục tiêu cho: {selectedSkill.skillName}");

            bool targetAlly = selectedSkill.targetType == TargetType.SingleAlly ||
                              selectedSkill.targetType == TargetType.AllAllies;

            var validTargets = targetAlly
                ? combat.PlayerUnits.Where(p => p.IsAlive).ToList()
                : combat.EnemyUnits.Where(e => e.IsAlive).ToList();

            // All target
            if (selectedSkill.targetType == TargetType.AllAllies ||
                selectedSkill.targetType == TargetType.AllEnemies)
            {
                if (GUILayout.Button($"→ Tất cả ({validTargets.Count})"))
                    Submit(selectedSkill, validTargets);
            }
            else
            {
                foreach (var t in validTargets)
                {
                    string row = t.GridRow == 2 ? "Front" :
                                 t.GridRow == 1 ? "Mid" : "Back";
                    if (GUILayout.Button($"→ {t.UnitName} [{row}]" +
                                         $"  HP:{t.CurrentHP}/{t.MaxHP}"))
                        Submit(selectedSkill, new List<CombatUnit> { t });
                }
            }
        }
    }

    // ─────────────────────────────────────────────────────────
    private void Submit(SkillData skill, List<CombatUnit> targets)
    {
        selectedSkill = null;
        combat.SubmitPlayerChoice(skill, targets);
    }

    // ─────────────────────────────────────────────────────────
    private void StartCombat()
    {
        if (enemyGroup == null)
        {
            Debug.LogError("[CombatTestUI] Chưa kéo EnemyGroupData vào Inspector!");
            return;
        }

        // Xây player setup từ Inspector
        var playerSetup = new List<(CharacterData, int, int)>();

        for (int i = 0; i < playerRoster.Length; i++)
        {
            if (playerRoster[i] == null) continue;

            int level = i < playerLevels.Length ? playerLevels[i] : 1;
            int slot = i < playerGridSlots.Length ? playerGridSlots[i] : i;

            playerSetup.Add((playerRoster[i], level, slot));
        }

        if (playerSetup.Count == 0)
        {
            Debug.LogError("[CombatTestUI] Chưa có nhân vật nào! " +
                           "Kéo CharacterData vào Player Roster.");
            return;
        }

        // Xây enemy setup từ EnemyGroupData
        var enemySetup = new List<(CharacterData, int, int)>();
        foreach (var e in enemyGroup.enemies)
        {
            if (e?.data == null) continue;
            enemySetup.Add((e.data, e.level, e.gridSlot));
        }

        if (enemySetup.Count == 0)
        {
            Debug.LogError("[CombatTestUI] EnemyGroupData không có enemy nào!");
            return;
        }

        combat.StartCombat(playerSetup, enemySetup);
    }
}