using System.Collections;
using UnityEngine;

/// <summary>
/// Quản lý camera trong combat:
/// - Zoom in/out khi clash hoặc damage
/// - Follow unit hoặc clash target
/// - Shake effect khi impact
/// - Smooth transition về default
///
/// SETUP:
/// 1. Gán script vào GameObject có Camera component
/// 2. Gọi Subscribe event khi CombatManager initialize
/// 3. Tự động follow khi clash, damage, ...
/// </summary>
public class CombatCameraManager : MonoBehaviour
{
    // ── Camera Component ──────────────────────────────────────
    private Camera mainCamera;
    private Transform cameraTransform;

    // ── Default State ─────────────────────────────────────────
    [Header("Default State")]
    [Tooltip("Default camera size khi không zoom (TĂNG NỀU CÒN QUÁZO) - Default: 15-18")]
    public float defaultOrthoSize = 16f;
    public Vector3 defaultPosition = Vector3.zero;
    [Tooltip("Z position của camera (phía sau sân) - TĂNG NỀU CẬP THẤP")]
    public float cameraHeight = 8f;  // Z position (tăng từ 5)

    // ── Zoom Settings ─────────────────────────────────────────
    [Header("Zoom Settings")]
    [Tooltip("Size khi zoom in clash (tương ứng tỉ lệ phóng to) - TĂNG NỀU KHÔNG THẤY UNITS")]
    public float clashZoomSize = 7f;
    [Tooltip("Size khi zoom vào unit bị damage - nên > clashZoomSize")]
    public float damageZoomSize = 8f;

    [Tooltip("Thời gian zoom (giây)")]
    public float zoomInDuration = 0.15f;
    public float zoomOutDuration = 0.2f;

    // ── Follow Settings ───────────────────────────────────────
    [Header("Follow Settings")]
    [Tooltip("Target khi zoom")]
    private Transform followTarget;
    
    [Tooltip("Offset từ target (nên = (0, 0, -cameraHeight))")]
    public Vector3 followOffset = new Vector3(0, 0, -8f);

    [Tooltip("Tốc độ follow (0-1). Càng thấp = càng mượt nhưng chậm. Để 0.08-0.12 cho smooth")]
    public float followSmoothness = 0.10f;

    // ── Shake Settings ────────────────────────────────────────
    [Header("Shake Settings")]
    [Tooltip("Mức độ rung (0.15-0.25 cho combat normal)")]
    public float shakeIntensity = 0.35f;
    [Tooltip("Thời gian rung (0.15-0.25s per hit)")]
    public float shakeDuration = 0.35f;
    [Tooltip("Tốc độ rung (20-25 là normal)")]
    public float shakeFrequency = 22f;

    [Tooltip("Mức độ rung cho final hit (mạnh hơn thường)")]
    public float finalHitShakeIntensity = 0.60f;
    [Tooltip("Thời gian rung cho final hit")]
    public float finalHitShakeDuration = 0.45f;

    // ── State ─────────────────────────────────────────────────
    private float currentOrthoSize;
    private Vector3 targetPosition;
    private Vector3 shakeOffset = Vector3.zero;
    private float shakeElapsed = 0f;
    private bool isShaking = false;

    private Coroutine zoomCoroutine;
    private Coroutine followCoroutine;
    private Coroutine shakeCoroutine;

    // ── Clash tracking (cho múltiples clashes cùng lúc) ─────────
    private int activeClashCount = 0;

    // ─────────────────────────────────────────────────────────
    private void Awake()
    {
        mainCamera = GetComponent<Camera>();
        cameraTransform = transform;

        if (mainCamera == null)
        {
            Debug.LogError("[CombatCameraManager] Camera component not found!");
            return;
        }

        currentOrthoSize = defaultOrthoSize;
        mainCamera.orthographicSize = currentOrthoSize;
        targetPosition = defaultPosition + new Vector3(0, 0, cameraHeight);
        cameraTransform.position = targetPosition;
    }

    private void Start()
    {
        // Subscribe vào CombatManager events
        if (CombatManager.Instance != null)
        {
            CombatManager.Instance.OnCombatStarted += HandleCombatStarted;
            CombatManager.Instance.OnClashResolved += HandleClashResolved;
            CombatManager.Instance.OnRoundEnded += HandleRoundEnded;
            CombatManager.Instance.OnDefeat += HandleCombatEnd;
            CombatManager.Instance.OnVictory += HandleCombatEnd;
        }

        // Lắng nghe phase changes để reset camera khi vào PlayerPlan
        StartCoroutine(MonitorPhaseChanges());
    }

    private IEnumerator MonitorPhaseChanges()
    {
        CombatPhase lastPhase = CombatManager.Instance.CurrentPhase;

        while (true)
        {
            yield return new WaitForSeconds(0.1f);

            if (CombatManager.Instance == null) yield break;

            CombatPhase currentPhase = CombatManager.Instance.CurrentPhase;

            // Khi vào PlayerPlan → Reset camera để nhìn toàn bộ
            if (currentPhase == CombatPhase.PlayerPlan && lastPhase != CombatPhase.PlayerPlan)
            {
                // Stop existing coroutines
                StopCoroutineIfRunning(zoomCoroutine);
                StopCoroutineIfRunning(followCoroutine);

                // Reset immediately
                followTarget = null;
                currentOrthoSize = defaultOrthoSize;
                targetPosition = defaultPosition + new Vector3(0, 0, cameraHeight);
                shakeOffset = Vector3.zero;

                // Auto fit sau khi reset
                yield return new WaitForSeconds(0.2f);
                AutoFitUnitsInView();
                
                Debug.Log("[CombatCamera] Entered PlayerPlan - Reset camera to view all units");
            }

            lastPhase = currentPhase;
        }
    }

    private void OnDestroy()
    {
        if (CombatManager.Instance != null)
        {
            CombatManager.Instance.OnCombatStarted -= HandleCombatStarted;
            CombatManager.Instance.OnClashResolved -= HandleClashResolved;
            CombatManager.Instance.OnRoundEnded -= HandleRoundEnded;
            CombatManager.Instance.OnDefeat -= HandleCombatEnd;
            CombatManager.Instance.OnVictory -= HandleCombatEnd;
        }
    }

    private void LateUpdate()
    {
        // Follow target smooth
        if (followTarget != null)
        {
            Vector3 desiredPos = followTarget.position + followOffset;
            cameraTransform.position = Vector3.Lerp(
                cameraTransform.position,
                desiredPos,
                followSmoothness) + shakeOffset;
        }
        else
        {
            // Về default position
            cameraTransform.position = Vector3.Lerp(
                cameraTransform.position,
                targetPosition,
                followSmoothness) + shakeOffset;
        }

        // Update ortho size
        mainCamera.orthographicSize = Mathf.Lerp(
            mainCamera.orthographicSize,
            currentOrthoSize,
            0.15f);
    }

    // ── PUBLIC METHODS ────────────────────────────────────────

    /// <summary>
    /// Zoom vào unit, shake, rồi theo dõi
    /// </summary>
    public void ZoomToUnit(Transform unit, float zoomSize = 0)
    {
        if (unit == null) return;

        if (zoomSize <= 0) zoomSize = damageZoomSize;

        StopCoroutineIfRunning(zoomCoroutine);
        StopCoroutineIfRunning(followCoroutine);

        followTarget = unit;
        zoomCoroutine = StartCoroutine(ZoomInCoroutine(zoomSize));
    }

    /// <summary>
    /// Clash sequence: zoom vào điểm giữa + freeze + shake + zoom out
    /// </summary>
    public void PlayClashZoom(Transform unit1, Transform unit2)
    {
        if (unit1 == null || unit2 == null) return;

        // Incrementar số clash đang hoạt động
        activeClashCount++;
        Debug.Log($"[CombatCamera] PlayClashZoom started. Active clashes: {activeClashCount}");

        Vector3 midpoint = (unit1.position + unit2.position) * 0.5f;
        StartCoroutine(ClashZoomSequence(midpoint));
    }

    /// <summary>
    /// Kết thúc 1 clash zoom. Chỉ zoom out khi toàn bộ clash kết thúc
    /// Gọi sau khi hết đòn tấn công của kẻ chiến thắng
    /// </summary>
    public void EndClashZoom()
    {
        if (activeClashCount > 0)
        {
            activeClashCount--;
            Debug.Log($"[CombatCamera] EndClashZoom. Remaining clashes: {activeClashCount}");

            // Chỉ zoom out khi không còn clash nào
            if (activeClashCount == 0)
            {
                StopCoroutineIfRunning(zoomCoroutine);
                zoomCoroutine = StartCoroutine(ZoomOutCoroutine());
            }
        }
    }

    /// <summary>
    /// Shake effect khi impact
    /// </summary>
    public void PlayImpactShake()
    {
        StopCoroutineIfRunning(shakeCoroutine);
        shakeCoroutine = StartCoroutine(ShakeCoroutine());
    }

    /// <summary>
    /// Shake effect mạnh hơn cho hit cuối cùng
    /// </summary>
    public void PlayFinalImpactShake()
    {
        StopCoroutineIfRunning(shakeCoroutine);
        shakeCoroutine = StartCoroutine(ShakeCoroutine(true));
    }

    /// <summary>
    /// Zoom ra thường mặc định
    /// </summary>
    public void ResetCamera()
    {
        StopCoroutineIfRunning(zoomCoroutine);
        StopCoroutineIfRunning(followCoroutine);

        followTarget = null;
        shakeOffset = Vector3.zero;
        isShaking = false;
        activeClashCount = 0;

        // Reset ngay lập tức về default state
        currentOrthoSize = defaultOrthoSize;
        targetPosition = defaultPosition + new Vector3(0, 0, cameraHeight);

        Debug.Log($"[CombatCamera] Reset: size={currentOrthoSize:F2}, pos={targetPosition}");
    }

    /// <summary>
    /// AUTO-ADJUST: Tính ortho size để fit toàn bộ units trên sân
    /// Gọi này nếu camera quá zoom và không thấy units
    /// </summary>
    public void AutoFitUnitsInView()
    {
        var unitViews = FindObjectsOfType<UnitView>();
        if (unitViews.Length == 0)
        {
            Debug.LogWarning("[CombatCamera] Không tìm thấy units để fit view");
            return;
        }

        // Tính bounding box của tất cả units
        Vector3 min = unitViews[0].transform.position;
        Vector3 max = unitViews[0].transform.position;

        foreach (var view in unitViews)
        {
            Vector3 pos = view.transform.position;
            min = Vector3.Min(min, pos);
            max = Vector3.Max(max, pos);
        }

        // Tính center & size
        Vector3 center = (min + max) * 0.5f;
        float width = Mathf.Abs(max.x - min.x);
        float height = Mathf.Abs(max.y - min.y);

        // OrthoSize = height / 2 (vì camera height = full size * 2)
        // Thêm buffer 40% để thoải mái (tăng từ 30% lên 40%)
        float requiredSize = Mathf.Max(Mathf.Max(width, height) * 0.6f, 10f);  // Min 10 để safe
        float bufferSize = Mathf.Max(requiredSize * 1.4f, 14f);  // Min 14 để không quá zoom

        defaultOrthoSize = bufferSize;
        defaultPosition = center;
        currentOrthoSize = bufferSize;
        targetPosition = center + new Vector3(0, 0, cameraHeight);
        
        // Đảm bảo không follow ai
        followTarget = null;
        shakeOffset = Vector3.zero;

        Debug.Log($"[CombatCamera] Auto-fit: Size={bufferSize:F2}, Center={center}, Units={unitViews.Length}");
    }

    /// <summary>
    /// Zoom ra thường mặc định
    /// </summary>
    public void ZoomToArea(Vector3 center, float radius)
    {
        if (zoomCoroutine != null) StopCoroutine(zoomCoroutine);

        followTarget = null;
        targetPosition = center + new Vector3(0, 0, cameraHeight);
        zoomCoroutine = StartCoroutine(ZoomInCoroutine(damageZoomSize));
    }

    /// <summary>
    /// Quick adjust camera distance (utility method)
    /// hệ số > 1 = zoom xa hơn, < 1 = zoom gần hơn
    /// </summary>
    public void ScamAdjustDistance(float factor)
    {
        defaultOrthoSize = Mathf.Max(defaultOrthoSize * Mathf.Clamp(factor, 0.5f, 2f), 8f);
        Debug.Log($"[CombatCamera] Distance adjusted: {defaultOrthoSize:F2}");
    }

    // ── COROUTINES ────────────────────────────────────────────

    private IEnumerator ZoomInCoroutine(float targetSize)
    {
        float startSize = currentOrthoSize;
        float elapsed = 0f;

        while (elapsed < zoomInDuration)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.Clamp01(elapsed / zoomInDuration);
            currentOrthoSize = Mathf.Lerp(startSize, targetSize, 
                EaseInOutQuad(t));
            yield return null;
        }

        currentOrthoSize = targetSize;
    }

    private IEnumerator ZoomOutCoroutine()
    {
        float startSize = currentOrthoSize;
        float elapsed = 0f;

        while (elapsed < zoomOutDuration)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.Clamp01(elapsed / zoomOutDuration);
            currentOrthoSize = Mathf.Lerp(startSize, defaultOrthoSize, 
                EaseInOutQuad(t));
            yield return null;
        }

        currentOrthoSize = defaultOrthoSize;
        followTarget = null;
        targetPosition = defaultPosition + new Vector3(0, 0, cameraHeight);
    }

    private IEnumerator ResetCameraSmooth()
    {
        // Zoom out smooth
        yield return StartCoroutine(ZoomOutCoroutine());

        // Return position smooth
        Vector3 startPos = cameraTransform.position;
        float elapsed = 0f;

        while (elapsed < zoomOutDuration)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.Clamp01(elapsed / zoomOutDuration);
            cameraTransform.position = Vector3.Lerp(startPos, targetPosition, t);
            yield return null;
        }

        cameraTransform.position = targetPosition;
    }

    private IEnumerator ClashZoomSequence(Vector3 clashCenter)
    {
        // Phase 1: Zoom vào clash point
        float startSize = currentOrthoSize;
        float elapsed = 0f;

        targetPosition = clashCenter + new Vector3(0, 0, cameraHeight);

        while (elapsed < zoomInDuration)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.Clamp01(elapsed / zoomInDuration);
            currentOrthoSize = Mathf.Lerp(startSize, clashZoomSize, 
                EaseInOutQuad(t));
            yield return null;
        }

        currentOrthoSize = clashZoomSize;

        // Phase 2: Shake impact
        yield return new WaitForSeconds(0.1f);
        yield return StartCoroutine(ShakeCoroutine());

        // Phase 3: Hold (Giữ camera zoom cho đến khi gọi EndClashZoom)
        // Không zoom out tự động tại đây nữa
        yield return new WaitForSeconds(0.3f);
    }

    private IEnumerator ShakeCoroutine(bool isFinalHit = false)
    {
        isShaking = true;
        shakeElapsed = 0f;

        float intensity = isFinalHit ? finalHitShakeIntensity : shakeIntensity;
        float duration = isFinalHit ? finalHitShakeDuration : shakeDuration;

        Vector3 originalPos = cameraTransform.position;

        while (shakeElapsed < duration)
        {
            shakeElapsed += Time.deltaTime;
            float decay = 1f - (shakeElapsed / duration);
            
            // Perlin noise cho smooth shake
            float noiseX = Mathf.PerlinNoise(shakeElapsed * shakeFrequency, 0f) - 0.5f;
            float noiseY = Mathf.PerlinNoise(shakeElapsed * shakeFrequency, 1f) - 0.5f;

            shakeOffset = new Vector3(
                noiseX * intensity * decay,
                noiseY * intensity * decay,
                0f);

            yield return null;
        }

        shakeOffset = Vector3.zero;
        isShaking = false;
    }

    // ── EVENT HANDLERS ────────────────────────────────────────

    private void HandleCombatStarted()
    {
        // Auto-fit camera để thấy tất cả units lần đầu
        Invoke(nameof(AutoFitUnitsInView), 0.2f);
    }

    private void HandleClashResolved(ClashResult result)
    {
        // Zoom vào winner khi clash xong
        if (result.Winner != null)
        {
            // Tìm UnitView của winner
            var unitView = FindUnitView(result.Winner);
            if (unitView != null)
            {
                ZoomToUnit(unitView.transform, damageZoomSize);
                PlayImpactShake();
            }
        }
    }

    private void HandleRoundEnded()
    {
        // Reset camera cuối vòng
        ResetCamera();
    }

    private void HandleCombatEnd()
    {
        // Reset khi combat kết thúc
        ResetCamera();
    }

    // ── UTILITIES ─────────────────────────────────────────────

    private void StopCoroutineIfRunning(Coroutine coroutine)
    {
        if (coroutine != null)
            StopCoroutine(coroutine);
    }

    private UnitView FindUnitView(CombatUnit unit)
    {
        var unitViews = FindObjectsOfType<UnitView>();
        foreach (var view in unitViews)
        {
            if (view.LinkedUnit == unit)
                return view;
        }
        return null;
    }

    // ── EASING ────────────────────────────────────────────────

    private float EaseInOutQuad(float t)
    {
        return t < 0.5f
            ? 2f * t * t
            : -1f + (4f - 2f * t) * t;
    }

    private float EaseOutQuad(float t)
    {
        return 1f - (1f - t) * (1f - t);
    }
}
