using UnityEngine;

/// <summary>
/// Điều khiển background của CombatScene.
/// Tự động tìm SpriteRenderer background (object có scale lớn, nằm xa camera)
/// và cho phép đổi sprite theo EnemyGroupData.
/// </summary>
public class CombatBackgroundController : MonoBehaviour
{
    [Header("Background Renderer")]
    [Tooltip("Nếu để trống, sẽ tự tìm SpriteRenderer background trong scene.")]
    public SpriteRenderer backgroundRenderer;

    private void Awake()
    {
        if (backgroundRenderer == null)
            backgroundRenderer = FindBackgroundRenderer();
    }

    /// <summary>
    /// Đổi sprite background. Nếu sprite null, giữ nguyên background hiện tại.
    /// </summary>
    public void SetBackground(Sprite sprite)
    {
        if (sprite == null) return;
        if (backgroundRenderer == null)
            backgroundRenderer = FindBackgroundRenderer();
        if (backgroundRenderer == null)
        {
            Debug.LogWarning("[CombatBackground] Không tìm thấy SpriteRenderer background!");
            return;
        }
        backgroundRenderer.sprite = sprite;
        Debug.Log($"[CombatBackground] Đã đổi background thành: {sprite.name}");
    }

    /// <summary>
    /// Tìm SpriteRenderer background: object có scale lớn (>= 5) và không phải UnitView.
    /// </summary>
    private SpriteRenderer FindBackgroundRenderer()
    {
        var allRenderers = FindObjectsByType<SpriteRenderer>(FindObjectsSortMode.None);
        SpriteRenderer best = null;
        float bestScale = 0f;

        foreach (var sr in allRenderers)
        {
            if (sr == null) continue;
            // Bỏ qua các SpriteRenderer thuộc UnitView (nhân vật)
            if (sr.GetComponentInParent<UnitView>() != null) continue;

            float scaleMag = sr.transform.lossyScale.magnitude;
            // Background thường có scale rất lớn so với nhân vật
            if (scaleMag > 5f && scaleMag > bestScale)
            {
                best = sr;
                bestScale = scaleMag;
            }
        }

        if (best == null)
            Debug.LogWarning("[CombatBackground] Không tìm thấy SpriteRenderer background (scale > 5).");
        return best;
    }
}