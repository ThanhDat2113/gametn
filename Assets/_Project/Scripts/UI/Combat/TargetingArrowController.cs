using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class TargetingArrowController : MonoBehaviour
{
    [Header("Settings")]
    [SerializeField] private float lineWidth = 0.1f;

    [Header("Colors")]
    [SerializeField] private Color enemyAttackColor = Color.red;
    [SerializeField] private Color playerAttackColor = Color.cyan;
    [SerializeField] private Color clashColor = Color.yellow;

    private CombatManager combat;
    private readonly List<LineRenderer> lines = new List<LineRenderer>();
    private bool isSubscribed = false;

    private Material solidMaterial;
    private Material dashedMaterialPrototype;
    private Camera mainCamera;
    private bool isHoveringOnUnit = false;
    private readonly Dictionary<LineRenderer, Color> lineColors = new Dictionary<LineRenderer, Color>();

    private void Start()
    {
        // Không cache Camera.main - dùng fresh mỗi lần trong Update
        CreateMaterials();
    }

    private void Update()
    {
        if (!isSubscribed && CombatManager.Instance != null)
        {
            SubscribeToEvents();
        }

        if (combat != null && combat.CurrentPhase == CombatPhase.Execute)
        {
            CheckForHover();
            UpdateLineVisuals();
        }
    }

    private void CreateMaterials()
    {
        solidMaterial = new Material(Shader.Find("Legacy Shaders/Particles/Alpha Blended Premultiply"));
        dashedMaterialPrototype = new Material(Shader.Find("Legacy Shaders/Particles/Alpha Blended Premultiply"));

        var tex = new Texture2D(32, 1, TextureFormat.RGBA32, false);
        tex.wrapMode = TextureWrapMode.Repeat;
        for (int i = 0; i < 32; ++i)
        {
            tex.SetPixel(i, 0, i < 16 ? Color.white : Color.clear);
        }
        tex.Apply();
        dashedMaterialPrototype.mainTexture = tex;
    }

    private void CheckForHover()
    {
        var cam = Camera.main;
        if (cam == null) return;
        var ray = cam.ScreenPointToRay(Input.mousePosition);
        if (Physics.Raycast(ray, out RaycastHit hit, 200f))
        {
            if (hit.collider.GetComponentInParent<UnitView>() != null)
            {
                isHoveringOnUnit = true;
                return;
            }
        }
        isHoveringOnUnit = false;
    }

    private void UpdateLineVisuals()
    {
        float blinkAlpha = 0.4f + Mathf.PingPong(Time.time * 2f, 0.6f);

        foreach (var lr in lines)
        {
            if (!lr.enabled || !lineColors.ContainsKey(lr)) continue;

            Color originalColor = lineColors[lr];

            if (originalColor != enemyAttackColor)
            {
                lr.material = solidMaterial;
                lr.startColor = lr.endColor = originalColor;
            }
            else
            {
                lr.material = dashedMaterialPrototype;
                Color blinkingColor = originalColor;
                blinkingColor.a = blinkAlpha;
                lr.startColor = lr.endColor = blinkingColor;
                float distance = Vector3.Distance(lr.GetPosition(0), lr.GetPosition(1));
                lr.material.mainTextureScale = new Vector2(distance * 1.5f, 1f);
            }
        }
    }

    private void SubscribeToEvents()
    {
        if (combat != null) return;

        combat = CombatManager.Instance;
        if (combat == null) return;

        combat.OnPlanChanged += DrawAllArrows;
        combat.OnExecuteStarted += HideAllArrows;
        combat.OnVictory += HideAllArrows;
        combat.OnDefeat += HideAllArrows;

        if (combat.CurrentPhase == CombatPhase.Execute)
        {
            DrawAllArrows();
        }

        isSubscribed = true;
    }

    private void OnDestroy()
    {
        if (combat != null)
        {
            combat.OnPlanChanged -= DrawAllArrows;
            combat.OnExecuteStarted -= HideAllArrows;
            combat.OnVictory -= HideAllArrows;
            combat.OnDefeat -= HideAllArrows;
        }
    }

    // Overload cho OnVictory (có tham số) gọi HideAllArrows gốc
    private void HideAllArrows(Dictionary<CharacterData, int> _)
    {
        HideAllArrows();
    }

    private void HideAllArrows()
    {
        foreach (var lr in lines)
        {
            lr.enabled = false;
        }
        lineColors.Clear();
    }

    public void DrawAllArrows()
    {
        HideAllArrows();

        if (combat == null) return;

        var allUnits = combat.PlayerUnits.Concat(combat.EnemyUnits).Where(u => u.IsAlive).ToList();
        var allAttacks = new List<(CombatUnit attacker, CombatUnit target)>();

        foreach (var unit in allUnits)
        {
            if (unit.SelectedSkill != null && unit.SelectedTargets.Count > 0)
            {
                foreach (var target in unit.SelectedTargets)
                {
                    if (target != null && target.IsAlive)
                    {
                        allAttacks.Add((unit, target));
                    }
                }
            }
        }

        EnsurePool(allAttacks.Count);
        int lineIndex = 0;

        var drawnClashes = new HashSet<(CombatUnit, CombatUnit)>();

        foreach (var (source, target) in allAttacks)
        {
            bool isClash = combat.WillAttackResultInClash(source, target);

            if (isClash)
            {
                var unit1 = source.Id < target.Id ? source : target;
                var unit2 = source.Id < target.Id ? target : source;
                if (drawnClashes.Contains((unit1, unit2)))
                {
                    continue;
                }

                DrawLine(lineIndex++, source, target, clashColor);
                drawnClashes.Add((unit1, unit2));
            }
            else
            {
                Color color = source.IsPlayer ? playerAttackColor : enemyAttackColor;
                DrawLine(lineIndex++, source, target, color);
            }
        }
        UpdateLineVisuals();
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

        lineColors[lr] = color;
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

            lr.enabled = false;
            lines.Add(lr);
        }
    }
}