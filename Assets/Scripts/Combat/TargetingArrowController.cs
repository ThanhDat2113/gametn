using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class TargetingArrowController : MonoBehaviour
{
    [Header("Settings")]
    [SerializeField] private float lineWidth = 0.1f;
    [SerializeField] private Material lineMaterial;

    [Header("Colors")]
    [SerializeField] private Color enemyAttackColor = Color.red;
    [SerializeField] private Color playerAttackColor = Color.cyan;
    [SerializeField] private Color clashColor = Color.yellow;

    private CombatManager combat;
    private readonly List<LineRenderer> lines = new List<LineRenderer>();
    private bool isSubscribed = false;

    // ─────────────────────────────────────────────────────────

    private void Update()
    {
        if (!isSubscribed && CombatManager.Instance != null)
        {
            SubscribeToEvents();
        }
    }

    private void SubscribeToEvents()
    {
        combat = CombatManager.Instance;
        if (combat == null) return;

        Debug.Log("[TargetingArrowController] Subscribed to CombatManager events.");

        // Đăng ký sự kiện
        combat.OnPlayerPlanStarted += HandlePlanStarted;
        combat.OnPlayerSkillSelected += HandleSkillSelected;
        combat.OnExecuteStarted += HideAllArrows;
        combat.OnVictory += HideAllArrows;
        combat.OnDefeat += HideAllArrows;

        isSubscribed = true;

        // Kiểm tra ngay lập tức nếu đang trong giai đoạn planning
        if (combat.CurrentPhase == CombatPhase.PlayerPlan)
        {
            Debug.Log("[TargetingArrowController] Already in planning phase. Drawing arrows now.");
            DrawAllArrows();
        }
    }

    private void OnDestroy()
    {
        if (combat != null)
        {
            combat.OnPlayerPlanStarted -= HandlePlanStarted;
            combat.OnPlayerSkillSelected -= HandleSkillSelected;
            combat.OnExecuteStarted -= HideAllArrows;
            combat.OnVictory -= HideAllArrows;
            combat.OnDefeat -= HideAllArrows;
        }
    }

    // Các hàm xử lý sự kiện mới
    private void HandlePlanStarted(List<CombatUnit> units) => DrawAllArrows();
    private void HandleSkillSelected(CombatUnit unit) => DrawAllArrows();

    private void HideAllArrows()
    {
        foreach (var lr in lines)
        {
            lr.enabled = false;
        }
    }

    private void DrawAllArrows()
    {
        HideAllArrows();

        var allUnits = combat.PlayerUnits.Concat(combat.EnemyUnits);
        var attacks = new Dictionary<CombatUnit, CombatUnit>();

        // Gather all selected attacks
        foreach (var unit in allUnits)
        {
            if (unit.IsAlive && unit.SelectedSkill != null && unit.SelectedTargets.Count > 0)
            {
                var target = unit.SelectedTargets[0];
                if (target != null && target.IsAlive)
                {
                    attacks[unit] = target;
                }
            }
        }

        EnsurePool(attacks.Count);
        int arrowIndex = 0;

        var drawnClashes = new HashSet<CombatUnit>();

        foreach (var attack in attacks)
        {
            var source = attack.Key;
            var target = attack.Value;

            // Check for a clash
            if (attacks.TryGetValue(target, out var reverseTarget) && reverseTarget == source)
            {
                // This is a clash
                if (drawnClashes.Contains(source) || drawnClashes.Contains(target)) continue;

                DrawLine(arrowIndex++, source, target, clashColor);
                drawnClashes.Add(source);
                drawnClashes.Add(target);
            }
            else
            {
                // This is a one-way attack
                Color color = source.IsPlayer ? playerAttackColor : enemyAttackColor;
                DrawLine(arrowIndex++, source, target, color);
            }
        }
    }

    private void DrawLine(int index, CombatUnit source, CombatUnit target, Color color)
    {
        if (index >= lines.Count) return;

        var sourceView = combat.GetUnitView(source);
        var targetView = combat.GetUnitView(target);

        if (sourceView == null || targetView == null) return;

        LineRenderer lr = lines[index];
        lr.enabled = true;
        lr.SetPosition(0, sourceView.transform.position);
        lr.SetPosition(1, targetView.transform.position);
        lr.startColor = lr.endColor = color;
    }

    private void EnsurePool(int count)
    {
        while (lines.Count < count)
        {
            var go = new GameObject($"Line_{lines.Count}");
            go.transform.SetParent(transform);

            var lr = go.AddComponent<LineRenderer>();
            lr.positionCount = 2;
            lr.startWidth = lineWidth;
            lr.endWidth = lineWidth;
            lr.useWorldSpace = true;
            lr.sortingOrder = 30000;

            if (lineMaterial != null)
            {
                lr.material = lineMaterial;
            }
            else
            {
                lr.material = new Material(Shader.Find("Sprites/Default"));
            }

            lr.enabled = false;
            lines.Add(lr);
        }
    }
}