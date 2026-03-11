using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Vẽ mũi tên từ enemy đến target của chúng khi bắt đầu mỗi vòng.
/// Mũi tên hiện từ lúc enemy plan xong cho đến khi execute bắt đầu.
///
/// Setup:
///   1. Tạo GameObject "EnemyTargetArrows" trong CombatScene
///   2. Gán script này vào
///   3. Gán arrowHeadSprite (sprite mũi tên nhỏ, optional)
///   4. Chỉnh màu sắc và kích thước trong Inspector
/// </summary>
public class EnemyTargetArrow : MonoBehaviour
{
    [Header("Appearance")]
    [Tooltip("Màu đường kẻ")]
    public Color lineColor = new Color(1f, 0.3f, 0.3f, 0.85f);

    [Tooltip("Độ dày đường kẻ (world units)")]
    public float lineWidth = 0.06f;

    [Tooltip("Offset Y trên đầu unit (điểm bắt đầu mũi tên)")]
    public float sourceOffsetY = 1.2f;

    [Tooltip("Offset Y trên đầu target (điểm kết thúc mũi tên)")]
    public float targetOffsetY = 1.2f;

    [Tooltip("Material cho LineRenderer — để trống dùng Default-Line")]
    public Material lineMaterial;

    [Header("Arrow Head")]
    [Tooltip("Sprite đầu mũi tên — để trống thì không vẽ đầu mũi tên")]
    public Sprite arrowHeadSprite;

    [Tooltip("Scale của sprite đầu mũi tên")]
    public float arrowHeadScale = 0.35f;

    [Tooltip("Khoảng lùi từ target để đặt đầu mũi tên")]
    public float arrowHeadOffset = 0.3f;

    // ── Internal ──────────────────────────────────────────────
    private CombatManager combat;

    // Pool: mỗi enemy có 1 LineRenderer + 1 SpriteRenderer (đầu mũi tên)
    private readonly List<LineRenderer>    lines  = new();
    private readonly List<SpriteRenderer>  heads  = new();

    // ─────────────────────────────────────────────────────────
    private void Start()
    {
        combat = CombatManager.Instance;

        combat.OnEnemyPlanDone  += ShowArrows;
        combat.OnExecuteStarted += HideArrows;
        combat.OnVictory        += HideArrows;
        combat.OnDefeat         += HideArrows;
    }

    private void OnDestroy()
    {
        if (combat == null) return;
        combat.OnEnemyPlanDone  -= ShowArrows;
        combat.OnExecuteStarted -= HideArrows;
        combat.OnVictory        -= HideArrows;
        combat.OnDefeat         -= HideArrows;
    }

    // ─────────────────────────────────────────────────────────
    private void ShowArrows()
    {
        HideArrows();

        var enemies = combat.EnemyUnits;
        EnsurePool(enemies.Count);

        for (int i = 0; i < enemies.Count; i++)
        {
            var enemy = enemies[i];
            if (!enemy.IsAlive || enemy.SelectedTargets.Count == 0) continue;

            var target = enemy.SelectedTargets[0];
            if (target == null || !target.IsAlive) continue;

            // Tìm world position của enemy và target qua UnitView
            var enemyView  = combat.GetUnitView(enemy);
            var targetView = combat.GetUnitView(target);

            if (enemyView == null || targetView == null) continue;

            Vector3 from = enemyView.transform.position  + Vector3.up * sourceOffsetY;
            Vector3 to   = targetView.transform.position + Vector3.up * targetOffsetY;

            // Vẽ đường
            var lr = lines[i];
            lr.enabled = true;
            lr.SetPosition(0, from);
            lr.SetPosition(1, to);

            // Vẽ đầu mũi tên
            if (heads[i] != null)
            {
                heads[i].enabled = true;

                // Đặt đầu mũi tên gần target, quay về hướng đường kẻ
                Vector3 dir = (to - from).normalized;
                heads[i].transform.position = to - dir * arrowHeadOffset;

                float angle = Mathf.Atan2(dir.y, dir.x) * Mathf.Rad2Deg - 90f;
                heads[i].transform.rotation = Quaternion.Euler(0f, 0f, angle);
            }
        }
    }

    // ─────────────────────────────────────────────────────────
    private void HideArrows()
    {
        foreach (var lr in lines)
            if (lr != null) lr.enabled = false;

        foreach (var sr in heads)
            if (sr != null) sr.enabled = false;
    }

    // ─────────────────────────────────────────────────────────
    // Đảm bảo pool đủ lớn
    private void EnsurePool(int count)
    {
        while (lines.Count < count)
        {
            var go = new GameObject($"Arrow_{lines.Count}");
            go.transform.SetParent(transform);

            // LineRenderer
            var lr = go.AddComponent<LineRenderer>();
            lr.positionCount = 2;
            lr.startWidth    = lineWidth;
            lr.endWidth      = lineWidth;
            lr.startColor    = lineColor;
            lr.endColor      = lineColor;
            lr.useWorldSpace = true;
            lr.sortingOrder  = 10;

            if (lineMaterial != null)
                lr.material = lineMaterial;
            else
                lr.material = new Material(Shader.Find("Sprites/Default"));

            lr.enabled = false;
            lines.Add(lr);

            // Arrow head (SpriteRenderer con)
            if (arrowHeadSprite != null)
            {
                var headGO = new GameObject("Head");
                headGO.transform.SetParent(go.transform);
                var sr = headGO.AddComponent<SpriteRenderer>();
                sr.sprite       = arrowHeadSprite;
                sr.color        = lineColor;
                sr.sortingOrder = 11;
                sr.transform.localScale = Vector3.one * arrowHeadScale;
                sr.enabled = false;
                heads.Add(sr);
            }
            else
            {
                heads.Add(null);
            }
        }
    }
}