using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;
using System;

using UnityEngine.UI;

public class CombatCameraManager : MonoBehaviour
{
    [Header("Intro Settings")]
    public Image fadePanel;
    public float fadeDuration = 0.5f;
    public float panDuration = 2.0f;
    public float enemyRushDelay = 0.5f;
    public float finalZoomOutDuration = 1.0f;



    [Header("Default State")]
    [Tooltip("Default camera size khi không zoom")]
    public float defaultOrthoSize = 10f;
    public Vector3 defaultPosition = Vector3.zero;
    [Tooltip("Z position của camera")]
    public float cameraHeight = 8f;

    [Header("Zoom Settings")]
    public float clashZoomSize = 7f;
    public float damageZoomSize = 8f;
    public float zoomInDuration = 0.15f;
    public float zoomOutDuration = 0.2f;

    [Header("Follow Settings")]
    private Transform followTarget;
    public Vector3 followOffset = new Vector3(0, 0, -8f);
    public float followSmoothness = 0.10f;

    [Header("Shake Settings")]
    public float shakeIntensity = 0.35f;
    public float shakeDuration = 0.35f;
    public float shakeFrequency = 22f;
    public float finalHitShakeIntensity = 0.60f;
    public float finalHitShakeDuration = 0.45f;

    [Header("Player Impact Settings")]
    public float playerImpactShakeIntensity = 0.5f;
    public float playerImpactShakeDuration = 0.4f;
    public float lungeAmount = 1.5f;
    public float lungeDuration = 0.1f;

    [Header("Slow Motion Settings")]
    public float hitStopDuration = 0.05f;
    public float slowMoFactor = 0.1f;
    public float slowMoDuration = 0.5f;

    private Camera mainCamera;
    private Transform cameraTransform;
    private float currentOrthoSize;
    private Vector3 targetPosition;
    private Vector3 shakeOffset = Vector3.zero;
    private float shakeElapsed = 0f;
    private bool isShaking = false;
    private Coroutine zoomCoroutine;
    private Coroutine followCoroutine;
    private Coroutine shakeCoroutine;
    private Coroutine slowMoCoroutine;
    private bool isSlowingDown = false;
    private List<Behaviour> disabledBehaviours = new List<Behaviour>();
    private bool isIntroSequenceActive = false;

#if CINEMACHINE_PRESENT
    private Cinemachine.CinemachineBrain brain;
    private List<Behaviour> disabledBrains = new List<Behaviour>();
    private List<GameObject> disabledVcamGameObjects = new List<GameObject>();
#endif

    private void Awake()
    {
        mainCamera = GetComponent<Camera>();
        cameraTransform = transform;
#if CINEMACHINE_PRESENT
        brain = GetComponent<Cinemachine.CinemachineBrain>();
#endif
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
        if (CombatManager.Instance != null)
        {
            CombatManager.Instance.OnCombatStarted += HandleCombatStarted;
            CombatManager.Instance.OnRoundEnded += HandleRoundEnded;
            CombatManager.Instance.OnDefeat += HandleCombatEnd;
            CombatManager.Instance.OnVictory += HandleCombatEnd;
        }
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
            if (currentPhase == CombatPhase.PlayerPlan && lastPhase != CombatPhase.PlayerPlan)
            {
                StopCoroutineIfRunning(zoomCoroutine);
                StopCoroutineIfRunning(followCoroutine);
                followTarget = null;
                currentOrthoSize = defaultOrthoSize;
                targetPosition = defaultPosition + new Vector3(0, 0, cameraHeight);
                shakeOffset = Vector3.zero;
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
            CombatManager.Instance.OnRoundEnded -= HandleRoundEnded;
            CombatManager.Instance.OnDefeat -= HandleCombatEnd;
            CombatManager.Instance.OnVictory -= HandleCombatEnd;
        }
    }

    private void LateUpdate()
    {
        if (isIntroSequenceActive) return; // Bỏ qua nếu intro đang chạy

        if (followTarget != null)
        {
            Vector3 desiredPos = followTarget.position + followOffset;
            cameraTransform.position = Vector3.Lerp(cameraTransform.position, desiredPos, followSmoothness) + shakeOffset;
        }
        else
        {
            cameraTransform.position = Vector3.Lerp(cameraTransform.position, targetPosition, followSmoothness) + shakeOffset;
        }
        mainCamera.orthographicSize = Mathf.Lerp(mainCamera.orthographicSize, currentOrthoSize, 0.15f);
    }

    public float GetCurrentOrthoSize()
    {
        return currentOrthoSize;
    }

    public void SetCameraSize(float newSize)
    {
        currentOrthoSize = newSize;
        mainCamera.orthographicSize = currentOrthoSize;
    }

    public void ZoomToUnit(Transform unit, float zoomSize = 0)
    {
        if (unit == null) return;
        if (zoomSize <= 0) zoomSize = damageZoomSize;
        StopCoroutineIfRunning(zoomCoroutine);
        StopCoroutineIfRunning(followCoroutine);
        followTarget = unit;
        zoomCoroutine = StartCoroutine(ZoomInCoroutine(zoomSize));
    }

    public void PlayImpactShake()
    {
        StopCoroutineIfRunning(shakeCoroutine);
        shakeCoroutine = StartCoroutine(ShakeCoroutine());
    }

    public void PlayFinalHitShake()
    {
        StopCoroutineIfRunning(shakeCoroutine);
        shakeCoroutine = StartCoroutine(ShakeCoroutine(true));
    }

    public void SetCameraPositionAndSize(Vector3 position, float size)
    {
        targetPosition = position + new Vector3(0, 0, cameraHeight);
        currentOrthoSize = size;
        // Di chuyển trực tiếp camera khi intro đang chạy hoặc không có mục tiêu theo dõi
        if (isIntroSequenceActive || followTarget == null)
        {
            cameraTransform.position = targetPosition;
            mainCamera.orthographicSize = currentOrthoSize;
        }
        followTarget = null;
    }

    public void ResetCamera()
    {
        StopCoroutineIfRunning(zoomCoroutine);
        StopCoroutineIfRunning(followCoroutine);
        followTarget = null;
        shakeOffset = Vector3.zero;
        isShaking = false;
        currentOrthoSize = defaultOrthoSize;
        targetPosition = defaultPosition + new Vector3(0, 0, cameraHeight);
        Debug.Log($"[CombatCamera] Reset: size={currentOrthoSize:F2}, pos={targetPosition}");
    }

    public void AutoFitUnitsInView()
    {
        var unitViews = FindObjectsOfType<UnitView>();
        if (unitViews.Length == 0)
        {
            Debug.LogWarning("[CombatCamera] Không tìm thấy units để fit view");
            return;
        }
        Vector3 min = unitViews[0].transform.position;
        Vector3 max = unitViews[0].transform.position;
        foreach (var view in unitViews)
        {
            Vector3 pos = view.transform.position;
            min = Vector3.Min(min, pos);
            max = Vector3.Max(max, pos);
        }
        Vector3 center = (min + max) * 0.5f;
        float width = Mathf.Abs(max.x - min.x);
        float height = Mathf.Abs(max.y - min.y);
        float requiredSize = Mathf.Max(Mathf.Max(width, height) * 0.6f, 10f);
        float bufferSize = Mathf.Max(requiredSize * 1.1f, 8f);
        defaultOrthoSize = bufferSize;
        defaultPosition = center;
        currentOrthoSize = bufferSize;
        targetPosition = center + new Vector3(0, 0, cameraHeight);
        followTarget = null;
        shakeOffset = Vector3.zero;
        Debug.Log($"[CombatCamera] Auto-fit: Size={bufferSize:F2}, Center={center}, Units={unitViews.Length}");
    }

    public void ZoomToArea(Vector3 center, float radius)
    {
        if (zoomCoroutine != null) StopCoroutine(zoomCoroutine);
        followTarget = null;
        targetPosition = center + new Vector3(0, 0, cameraHeight);
        zoomCoroutine = StartCoroutine(ZoomInCoroutine(damageZoomSize));
    }

    public void ScamAdjustDistance(float factor)
    {
        defaultOrthoSize = Mathf.Max(defaultOrthoSize * Mathf.Clamp(factor, 0.5f, 2f), 8f);
        Debug.Log($"[CombatCamera] Distance adjusted: {defaultOrthoSize:F2}");
    }

    public void PlayPlayerImpactEffect(Transform target)
    {
        StopCoroutineIfRunning(shakeCoroutine);
        shakeCoroutine = StartCoroutine(PlayerImpactCoroutine(target));
    }



    private IEnumerator PlayerImpactCoroutine(Transform target)
    {
        isShaking = true;
        Vector3 originalPos = cameraTransform.position;
        Vector3 targetDirection = (target.position - cameraTransform.position).normalized;
        targetDirection.z = 0;
        Vector3 lungeTargetPos = originalPos + targetDirection * lungeAmount;
        float elapsed = 0f;
        while (elapsed < lungeDuration)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.Clamp01(elapsed / lungeDuration);
            cameraTransform.position = Vector3.Lerp(originalPos, lungeTargetPos, EaseOutQuad(t));
            yield return null;
        }
        shakeElapsed = 0f;
        while (shakeElapsed < playerImpactShakeDuration)
        {
            shakeElapsed += Time.deltaTime;
            float decay = 1f - (shakeElapsed / playerImpactShakeDuration);
            float noiseX = Mathf.PerlinNoise(shakeElapsed * shakeFrequency, 0f) - 0.5f;
            float noiseY = Mathf.PerlinNoise(shakeElapsed * shakeFrequency, 1f) - 0.5f;
            shakeOffset = new Vector3(noiseX * playerImpactShakeIntensity * decay, noiseY * playerImpactShakeIntensity * decay, 0f);
            cameraTransform.position = lungeTargetPos + shakeOffset;
            yield return null;
        }
        shakeOffset = Vector3.zero;
        isShaking = false;
    }

    private IEnumerator ZoomInCoroutine(float targetSize)
    {
        float startSize = currentOrthoSize;
        float elapsed = 0f;
        while (elapsed < zoomInDuration)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.Clamp01(elapsed / zoomInDuration);
            currentOrthoSize = Mathf.Lerp(startSize, targetSize, EaseInOutQuad(t));
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
            currentOrthoSize = Mathf.Lerp(startSize, defaultOrthoSize, EaseInOutQuad(t));
            yield return null;
        }
        currentOrthoSize = defaultOrthoSize;
        followTarget = null;
    }

    private IEnumerator ShakeCoroutine(bool isFinalHit = false)
    {
        isShaking = true;
        shakeElapsed = 0f;
        float duration = isFinalHit ? finalHitShakeDuration : shakeDuration;
        float intensity = isFinalHit ? finalHitShakeIntensity : shakeIntensity;

        while (shakeElapsed < duration)
        {
            shakeElapsed += Time.deltaTime;
            float decay = 1f - (shakeElapsed / duration);
            float noiseX = Mathf.PerlinNoise(Time.time * shakeFrequency, 0f) * 2f - 1f;
            float noiseY = Mathf.PerlinNoise(Time.time * shakeFrequency, 1f) * 2f - 1f;
            shakeOffset = new Vector3(noiseX * intensity * decay, noiseY * intensity * decay, 0f);
            yield return null;
        }
        shakeOffset = Vector3.zero;
        isShaking = false;
    }

    private void StopCoroutineIfRunning(Coroutine coroutine)
    {
        if (coroutine != null)
        {
            StopCoroutine(coroutine);
        }
    }

    private void HandleCombatStarted()
    {
        AutoFitUnitsInView();
    }

    private void HandleRoundEnded()
    {
        ResetCamera();
    }

    private void HandleCombatEnd()
    {
        ResetCamera();
    }

    private float EaseInOutQuad(float t)
    {
        return t < 0.5f ? 2 * t * t : 1 - Mathf.Pow(-2 * t + 2, 2) / 2;
    }

    private float EaseOutQuad(float t)
    {
        return 1 - (1 - t) * (1 - t);
    }

    private float EaseInOutCubic(float t)
    {
        return t < 0.5f ? 4 * t * t * t : 1 - Mathf.Pow(-2 * t + 2, 3) / 2;
    }

    private float EaseOutCubic(float t)
    {
        return 1 - Mathf.Pow(1 - t, 3);
    }

    #region Intro Sequence Methods

    public IEnumerator FadeInAndSetPosition(Vector3 focusPoint, float targetSize, Vector3 panFromOffset, float panDuration)
    {
        if (fadePanel == null)
        {
            Debug.LogError("[CombatCameraManager] Fade Panel is not assigned! Aborting intro.", this);
            yield break;
        }

        // 1. Start with black screen
        fadePanel.color = Color.black;
        fadePanel.gameObject.SetActive(true);

        // 2. Instantly set camera to the starting PAN position (off-screen)
        Vector3 startPanPos = focusPoint + panFromOffset;
        SetCameraPositionAndSize(startPanPos, targetSize);
        yield return new WaitForSeconds(0.1f); 

        // 3. Fade in (alpha from 1 to 0)
        float elapsed = 0f;
        while (elapsed < fadeDuration)
        {
            elapsed += Time.deltaTime;
            float alpha = Mathf.Lerp(1f, 0f, elapsed / fadeDuration);
            fadePanel.color = new Color(0, 0, 0, alpha);
            yield return null;
        }
        fadePanel.gameObject.SetActive(false);
        Debug.Log("[IntroCamera] Fade-in complete.");

        // 4. Pan camera to the focus point
        elapsed = 0f;
        Vector3 currentPos = cameraTransform.position;
        Vector3 targetPanPos = focusPoint + new Vector3(0, 0, cameraHeight);

        while (elapsed < panDuration)
        {
            elapsed += Time.deltaTime;
            float t = EaseOutCubic(elapsed / panDuration);
            cameraTransform.position = Vector3.Lerp(currentPos, targetPanPos, t);
            yield return null;
        }
        cameraTransform.position = targetPanPos;
        Debug.Log("[IntroCamera] Pan complete.");
    }

    public IEnumerator ZoomOutToFinalView(float duration)
    {
        Debug.Log("[IntroCamera] Zooming out to final view.");
        float elapsed = 0f;
        float startSize = mainCamera.orthographicSize;
        Vector3 startPos = cameraTransform.position;

        // Calculate final framing based on all units in their correct positions
        AutoFitUnitsInView();
        float finalSize = defaultOrthoSize;
        Vector3 finalPos = defaultPosition + new Vector3(0, 0, cameraHeight);

        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float t = EaseOutCubic(elapsed / duration);
            mainCamera.orthographicSize = Mathf.Lerp(startSize, finalSize, t);
            cameraTransform.position = Vector3.Lerp(startPos, finalPos, t);
            yield return null;
        }

        mainCamera.orthographicSize = finalSize;
        cameraTransform.position = finalPos;
        Debug.Log("[IntroCamera] Final zoom-out complete.");
    }

    public void BeginIntroSequence()
    {
        isIntroSequenceActive = true;
        Debug.Log("[IntroCamera] Intro sequence BEGAN. Camera control is now locked.");
    }

    public void EndIntroSequence()
    {
        isIntroSequenceActive = false;
        Debug.Log("[IntroCamera] Intro sequence ENDED. Camera control is now unlocked.");
    }

    #endregion
}