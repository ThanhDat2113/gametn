using UnityEngine;
using System.Collections;

/// <summary>
/// Main manager for the Lucio vs Cedric cutscene
/// Orchestrates camera animations, character animations, beam attacks, and QTE
/// </summary>
public class LucioCutsceneManager : MonoBehaviour
{
    [SerializeField] private LucioCutsceneCameraController cameraController;
    [SerializeField] private CutsceneCharacter lucioCharacter;
    [SerializeField] private CutsceneCharacter cedricCharacter;
    [SerializeField] private BeamAttackSystem beamSystem;
    [SerializeField] private QuickTimeEventSystem quickTimeEventSystem;

    [Header("Cutscene Timing")]
    [SerializeField] private float zoomInDuration = 2f;
    [SerializeField] private float centerZoomOutDuration = 2f;
    [SerializeField] private float flickDuration = 0.5f;
    [SerializeField] private float beamAttackDuration = 3f;
    [SerializeField] private float qteStartDelay = 0.5f;

    private bool cutsceneActive = false;

    private void Start()
    {
        // Setup callback so QTE can control beam progress
        quickTimeEventSystem.SetBeamProgressCallback(beamSystem.UpdateLucioBeamProgress);

        // Auto-start cutscene when scene loads
        StartCutscene();
    }

    public void StartCutscene()
    {
        if (cutsceneActive) return;
        cutsceneActive = true;
        StartCoroutine(CutsceneSequence());
    }

    private IEnumerator CutsceneSequence()
    {
        // Phase 1: Zoom in to each character gradually
        yield return StartCoroutine(cameraController.ZoomInToCharacters(lucioCharacter, cedricCharacter, zoomInDuration));

        // Phase 2: Zoom in from center to create depth sensation
        yield return StartCoroutine(cameraController.CenterZoomOutForDepth(centerZoomOutDuration));

        // Phase 3: Quick flick between characters
        yield return StartCoroutine(cameraController.FlickBetweenCharacters(lucioCharacter, cedricCharacter, flickDuration));

        // Phase 4 & 5: Beam attack and QTE run in parallel
        yield return StartCoroutine(BeamAndQTESequence());

        // End cutscene
        EndCutscene();
    }

    private IEnumerator BeamAndQTESequence()
    {
        // Start beam attack and QTE systems in parallel
        Coroutine beamCoroutine = StartCoroutine(BeamAttackSequence());
        Coroutine qteCoroutine = StartCoroutine(QuickTimeEventSequence());

        // Wait for both to complete
        yield return beamCoroutine;
        yield return qteCoroutine;
    }

    private IEnumerator BeamAttackSequence()
    {
        // Start beam attack system
        beamSystem.StartBeamAttack(lucioCharacter, cedricCharacter, beamAttackDuration);

        // Pull back camera initially to show impact
        yield return StartCoroutine(cameraController.CameraImpactPullback(0.2f));

        // Camera shake throughout the beam duration
        yield return StartCoroutine(cameraController.CameraShakeLight(beamAttackDuration));

        // Bring camera back close to center for closeup during attacks
        yield return StartCoroutine(cameraController.CameraReturnToCenter(0.5f));
    }

    private IEnumerator QuickTimeEventSequence()
    {
        yield return new WaitForSeconds(qteStartDelay);

        // Start QTE system - runs until completion
        yield return StartCoroutine(quickTimeEventSystem.StartQTE());

        // Get the final beam progress from QTE system
        float beamProgress = quickTimeEventSystem.GetFinalBeamProgress();

        // Determine winner: if Lucio's beam traveled far enough (>50% of distance), he wins
        bool lucioWins = beamProgress > 0.5f;

        if (lucioWins)
        {
            // Lucio wins - camera follows victory
            yield return StartCoroutine(cameraController.CameraFocusVictory(lucioCharacter));
            Debug.Log($"Lucio Wins! Beam Progress: {beamProgress * 100f:F0}%");
        }
        else
        {
            // Lucio loses - camera follows defeat
            yield return StartCoroutine(cameraController.CameraFocusDefeat(cedricCharacter));
            Debug.Log($"Cedric Wins! Lucio's Beam Progress: {beamProgress * 100f:F0}%");
        }
    }

    private void EndCutscene()
    {
        cutsceneActive = false;
        Debug.Log("Lucio Cutscene ended");
    }

    public bool IsCutsceneActive() => cutsceneActive;
}
