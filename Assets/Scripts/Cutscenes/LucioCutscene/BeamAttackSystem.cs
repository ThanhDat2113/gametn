using UnityEngine;
using System.Collections;
using System.Collections.Generic;

/// <summary>
/// Manages beam attacks between characters using VFX prefabs
/// Lucio's beam is controlled by QTE input, Cedric's beam is a counter-attack
/// Beams scale along X axis based on progress (0-1)
/// </summary>
public class BeamAttackSystem : MonoBehaviour
{
    [Header("Beam Prefabs")]
    [SerializeField] private GameObject lucioBeamPrefab;
    [SerializeField] private GameObject cedricBeamPrefab;

    [Header("Beam Settings")]
    [SerializeField] private float maxBeamScale = 10f; // Maximum scale value along X axis
    [SerializeField] private float beamDuration = 0.5f;

    [Header("Beam Count")]
    [SerializeField] private int beamCount = 2; // Number of beams exchanged
    [SerializeField] private float beamInterval = 1.5f; // Time between each beam attack

    private GameObject lucioBeamInstance;
    private GameObject cedricBeamInstance;
    private float currentLucioBeamProgress = 0f; // 0-1 controlling how far Lucio's beam goes
    private bool updateLucioBeamLive = false; // Flag for real-time updates

    private List<GameObject> activeBeams = new List<GameObject>();

    public void StartBeamAttack(CutsceneCharacter lucio, CutsceneCharacter cedric, float duration)
    {
        StartCoroutine(BeamAttackSequence(lucio, cedric, duration));
    }

    /// <summary>
    /// Update the progress of Lucio's beam in real-time (called by QTE system)
    /// </summary>
    public void UpdateLucioBeamProgress(float progress)
    {
        currentLucioBeamProgress = Mathf.Clamp01(progress);

        // Update live beam scale if active
        if (updateLucioBeamLive && lucioBeamInstance != null)
        {
            UpdateBeamScale(lucioBeamInstance, currentLucioBeamProgress);
        }
    }

    private IEnumerator BeamAttackSequence(CutsceneCharacter lucio, CutsceneCharacter cedric, float duration)
    {
        float timeRemaining = duration;

        for (int i = 0; i < beamCount && timeRemaining > 0; i++)
        {
            // Lucio attacks - controlled by QTE progress
            yield return StartCoroutine(FireControlledBeam(lucio, cedric, lucioBeamPrefab, beamDuration));
            timeRemaining -= beamInterval;

            yield return new WaitForSeconds(beamInterval / 2);

            // Cedric counter-attacks - full strength
            yield return StartCoroutine(FireBeam(cedric, cedricBeamPrefab, beamDuration, 1f));
            timeRemaining -= beamInterval;

            yield return new WaitForSeconds(beamInterval / 2);
        }

        // Clean up any remaining beams
        DestroyAllBeams();
    }

    /// <summary>
    /// Fire beam controlled by QTE progress (Lucio's beam)
    /// Scale changes in real-time as player spams
    /// </summary>
    private IEnumerator FireControlledBeam(CutsceneCharacter source, CutsceneCharacter target, GameObject prefab, float duration)
    {
        if (prefab == null)
        {
            Debug.LogError("Lucio beam prefab not assigned!");
            yield break;
        }

        // Instantiate beam at source position
        lucioBeamInstance = Instantiate(prefab, source.GetPosition(), Quaternion.identity);
        lucioBeamInstance.name = "LucioBeam_Active";
        activeBeams.Add(lucioBeamInstance);

        // Determine beam direction (from Lucio to Cedric or vice versa)
        Vector3 direction = (target.GetPosition() - source.GetPosition()).normalized;
        lucioBeamInstance.transform.right = direction;

        float elapsed = 0f;
        updateLucioBeamLive = true;

        // Animate beam over duration while allowing real-time scale updates
        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            
            // Update beam scale based on current progress
            UpdateBeamScale(lucioBeamInstance, currentLucioBeamProgress);

            yield return null;
        }

        updateLucioBeamLive = false;

        // Hold beam at final scale for a moment
        UpdateBeamScale(lucioBeamInstance, currentLucioBeamProgress);
        yield return new WaitForSeconds(0.2f);

        // Destroy beam
        DestroyBeam(lucioBeamInstance);
        lucioBeamInstance = null;
    }

    /// <summary>
    /// Fire normal beam (full strength, instant scale)
    /// </summary>
    private IEnumerator FireBeam(CutsceneCharacter source, GameObject prefab, float duration, float progressMultiplier = 1f)
    {
        if (prefab == null)
        {
            Debug.LogError("Cedric beam prefab not assigned!");
            yield break;
        }

        // Instantiate beam at source position
        cedricBeamInstance = Instantiate(prefab, source.GetPosition(), Quaternion.identity);
        cedricBeamInstance.name = "CedricBeam_Active";
        activeBeams.Add(cedricBeamInstance);

        // Scale to full immediately
        UpdateBeamScale(cedricBeamInstance, progressMultiplier);

        yield return new WaitForSeconds(duration);

        // Destroy beam
        DestroyBeam(cedricBeamInstance);
        cedricBeamInstance = null;
    }

    /// <summary>
    /// Update beam scale based on progress
    /// Progress 0-1 maps to scale 0 to maxBeamScale on X axis
    /// </summary>
    private void UpdateBeamScale(GameObject beam, float progress)
    {
        if (beam == null) return;

        Vector3 currentScale = beam.transform.localScale;
        currentScale.x = progress * maxBeamScale;
        beam.transform.localScale = currentScale;
    }

    private void DestroyBeam(GameObject beam)
    {
        if (beam != null)
        {
            activeBeams.Remove(beam);
            Destroy(beam);
        }
    }

    private void DestroyAllBeams()
    {
        foreach (GameObject beam in activeBeams)
        {
            if (beam != null)
                Destroy(beam);
        }
        activeBeams.Clear();
        lucioBeamInstance = null;
        cedricBeamInstance = null;
    }

    public void StopBeamAttack()
    {
        updateLucioBeamLive = false;
        DestroyAllBeams();
    }
}
