using UnityEngine;
using System.Collections;

/// <summary>
/// Cinematic Battle System inspired by Limbus Company & JJK
/// 2 static characters battle with dramatic camera work and combat effects
/// Like Yuta vs Ryu: projectile attacks with QTE mechanics
/// </summary>
public class SimpleBattleSystem : MonoBehaviour
{
    [Header("Characters")]
    [SerializeField] private Transform lucioTransform;
    [SerializeField] private Transform cedricTransform;
    [SerializeField] private Vector3 centerPoint; // Position between 2 characters for final clash

    [Header("Systems")]
    [SerializeField] private SimpleBeamAttackSystem beamSystem;
    [SerializeField] private QuickTimeEventSystem quickTimeEventSystem;
    [SerializeField] private Camera mainCamera;

    [Header("Battle Settings")]
    [SerializeField] private int totalRounds = 3;
    [SerializeField] private float roundDelay = 1.5f;
    [SerializeField] private float attackDelay = 0.5f;

    [Header("Camera Settings")]
    [SerializeField] private float cameraShakeMagnitude = 0.3f;
    [SerializeField] private float cameraShakeDuration = 0.3f;
    
    [Header("Final Clash Settings")]
    [SerializeField] private float finalBeamDuration = 0.6f;
    [SerializeField] private float winnerBeamGrowDuration = 1f;

    private int currentRound = 0;
    private bool battleActive = false;
    private Vector3 cameraOriginalPos;

    private void Start()
    {
        if (mainCamera == null)
            mainCamera = Camera.main;

        cameraOriginalPos = mainCamera.transform.position;
    }

    /// <summary>
    /// Start the cinematic battle!
    /// </summary>
    public void StartBattle()
    {
        if (battleActive) return;
        battleActive = true;
        StartCoroutine(BattleSequence());
    }

    private IEnumerator BattleSequence()
    {
        Debug.Log("=== CINEMATIC BATTLE START ===");

        // Opening: dramatic intro
        yield return StartCoroutine(IntroSequence());

        // Main battle rounds - Speed increases each round
        for (int round = 0; round < totalRounds; round++)
        {
            currentRound = round + 1;
            float speedMultiplier = GetSpeedMultiplier(currentRound);
            
            Debug.Log($"\n>>> ROUND {currentRound}/{totalRounds} - Speed: {speedMultiplier:F1}x <<<");

            // Lucio attacks (faster each round)
            yield return StartCoroutine(LucioAttackSequence(speedMultiplier));
            yield return new WaitForSeconds(roundDelay / speedMultiplier);

            // Cedric counter-attacks (faster each round)
            yield return StartCoroutine(CedricAttackSequence(speedMultiplier));
            yield return new WaitForSeconds(roundDelay / speedMultiplier);
        }

        // Climax: Final Clash with QTE - Ultimate speed
        yield return StartCoroutine(FinalShowdown());

        battleActive = false;
        Debug.Log("=== BATTLE ENDED ===\n");
    }

    /// <summary>
    /// Calculate speed multiplier based on round number
    /// Round 1: 1x speed (normal)
    /// Round 2: 1.5x speed (faster)
    /// Round 3: 2x speed (much faster)
    /// </summary>
    private float GetSpeedMultiplier(int round)
    {
        return 1f + (round - 1) * 0.5f;
    }

    private IEnumerator IntroSequence()
    {
        Debug.Log("Intro: Camera focuses on characters...");

        // Zoom to Lucio
        yield return StartCoroutine(CameraFocus(lucioTransform.position, 0.8f));
        yield return new WaitForSeconds(0.5f);

        // Zoom to Cedric
        yield return StartCoroutine(CameraFocus(cedricTransform.position, 0.8f));
        yield return new WaitForSeconds(0.5f);

        // Both in view
        Vector3 centerPos = (lucioTransform.position + cedricTransform.position) / 2f;
        yield return StartCoroutine(CameraFocus(centerPos, 0.5f));
        yield return new WaitForSeconds(1f);
    }

    private IEnumerator LucioAttackSequence(float speedMultiplier = 1f)
    {
        Debug.Log("Lucio fires attack!");

        // Camera focus on Lucio - preparing (faster each round)
        yield return StartCoroutine(CameraFocus(lucioTransform.position, 0.3f / speedMultiplier));
        yield return new WaitForSeconds(0.3f / speedMultiplier);

        // Attack!
        beamSystem.LucioAttack(lucioTransform.position, cedricTransform.position);

        // Camera shake on impact
        yield return StartCoroutine(CameraShake(cameraShakeDuration / speedMultiplier));

        // Quick cut to Cedric receiving hit
        yield return new WaitForSeconds(0.2f / speedMultiplier);
        yield return StartCoroutine(CameraFocus(cedricTransform.position, 0.2f / speedMultiplier));
        yield return new WaitForSeconds(0.3f / speedMultiplier);
    }

    private IEnumerator CedricAttackSequence(float speedMultiplier = 1f)
    {
        Debug.Log("Cedric counter-attacks!");

        // Camera focus on Cedric - preparing (faster each round)
        yield return StartCoroutine(CameraFocus(cedricTransform.position, 0.3f / speedMultiplier));
        yield return new WaitForSeconds(0.3f / speedMultiplier);

        // Attack!
        beamSystem.CedricAttack(cedricTransform.position, lucioTransform.position);

        // Camera shake on impact
        yield return StartCoroutine(CameraShake(cameraShakeDuration / speedMultiplier));

        // Quick cut to Lucio receiving hit
        yield return new WaitForSeconds(0.2f / speedMultiplier);
        yield return StartCoroutine(CameraFocus(lucioTransform.position, 0.2f / speedMultiplier));
        yield return new WaitForSeconds(0.3f / speedMultiplier);
    }

    private IEnumerator FinalShowdown()
    {
        Debug.Log("\n!!! FINAL CLASH - ULTIMATE SHOWDOWN !!!");
        Debug.Log("Both beams collide at center point!\n");

        // If centerPoint not set, calculate middle
        Vector3 finalCenterPoint = centerPoint == Vector3.zero 
            ? (lucioTransform.position + cedricTransform.position) / 2f 
            : centerPoint;

        // Wide shot showing both
        yield return StartCoroutine(CameraFocus(finalCenterPoint, 0.3f));

        // Lucio prepares
        yield return StartCoroutine(CameraFocus(lucioTransform.position, 0.2f));
        yield return new WaitForSeconds(0.3f);

        // Cedric prepares
        yield return StartCoroutine(CameraFocus(cedricTransform.position, 0.2f));
        yield return new WaitForSeconds(0.3f);

        // BACK TO WIDE SHOT
        yield return StartCoroutine(CameraFocus(finalCenterPoint, 0.2f));
        yield return new WaitForSeconds(0.2f);

        // Both fire SIMULTANEOUSLY toward center point!
        beamSystem.ShootScaledBeam(lucioTransform.position, finalCenterPoint, 
            beamSystem.lucioBeamVFX, finalBeamDuration);
        beamSystem.ShootScaledBeam(cedricTransform.position, finalCenterPoint, 
            beamSystem.cedricBeamVFX, finalBeamDuration);

        Debug.Log("⚡ Both beams firing toward center...");
        yield return new WaitForSeconds(finalBeamDuration + 0.2f);

        // INTENSE CAMERA SHAKE
        yield return StartCoroutine(CameraShake(0.5f));

        // QTE starts - player determines which beam overpowers
        Debug.Log(">>> QTE ACTIVATED! <<<");
        yield return StartCoroutine(quickTimeEventSystem.StartQTE());

        // Get result
        float beamProgress = quickTimeEventSystem.GetFinalBeamProgress();
        yield return new WaitForSeconds(0.3f);

        bool lucioWins = beamProgress > 0.5f;

        if (lucioWins)
        {
            Debug.Log($"\n>>> LUCIO VICTORY! <<<");
            Debug.Log($"Beam Power: {beamProgress * 100f:F0}%");
            
            // Winner beam grows to "devour" enemy
            beamSystem.WinnerBeamGrow(lucioTransform.position, finalCenterPoint, 
                beamSystem.lucioBeamVFX, winnerBeamGrowDuration);
            
            yield return StartCoroutine(CameraFocus(lucioTransform.position, 0.5f));
            yield return new WaitForSeconds(winnerBeamGrowDuration);
        }
        else
        {
            Debug.Log($"\n>>> CEDRIC VICTORY! <<<");
            Debug.Log($"Lucio's Beam Power: {beamProgress * 100f:F0}%");
            
            // Winner beam grows to "devour" enemy
            beamSystem.WinnerBeamGrow(cedricTransform.position, finalCenterPoint, 
                beamSystem.cedricBeamVFX, winnerBeamGrowDuration);
            
            yield return StartCoroutine(CameraFocus(cedricTransform.position, 0.5f));
            yield return new WaitForSeconds(winnerBeamGrowDuration);
        }

        yield return new WaitForSeconds(1f);
    }

    private IEnumerator RapidBeamExchange()
    {
        // [DEPRECATED - Now using simultaneous beams in FinalShowdown]
        yield break;
    }

    /// <summary>
    /// Smoothly move camera to focus on position
    /// </summary>
    private IEnumerator CameraFocus(Vector3 targetPos, float duration)
    {
        Vector3 startPos = mainCamera.transform.position;
        targetPos.z = -10f; // Keep camera Z distance

        float elapsed = 0f;
        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / duration;

            mainCamera.transform.position = Vector3.Lerp(startPos, targetPos, EaseInOutQuad(t));
            yield return null;
        }

        mainCamera.transform.position = targetPos;
    }

    /// <summary>
    /// Camera shake effect on impact
    /// </summary>
    private IEnumerator CameraShake(float duration)
    {
        Vector3 originalPos = mainCamera.transform.position;
        float elapsed = 0f;

        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;

            float shakeX = Random.Range(-cameraShakeMagnitude, cameraShakeMagnitude);
            float shakeY = Random.Range(-cameraShakeMagnitude, cameraShakeMagnitude);

            mainCamera.transform.position = originalPos + new Vector3(shakeX, shakeY, 0);

            yield return null;
        }

        mainCamera.transform.position = originalPos;
    }

    private float EaseInOutQuad(float t)
    {
        return t < 0.5f ? 2f * t * t : -1f + (4f - 2f * t) * t;
    }

    public bool IsBattleActive() => battleActive;
}

