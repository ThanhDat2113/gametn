using UnityEngine;
using System.Collections;

/// <summary>
/// Quick Time Event system for the cutscene
/// Players spam a button to push Lucio's beam towards Cedric
/// The more you spam, the further the beam goes
/// </summary>
public class QuickTimeEventSystem : MonoBehaviour
{
    [Header("QTE Settings")]
    [SerializeField] private KeyCode qteButton = KeyCode.Space;
    [SerializeField] private float qteDuration = 5f;
    [SerializeField] private float keyPressThrottleTime = 0.05f; // Throttle for each press

    [Header("UI References")]
    [SerializeField] private CanvasGroup qteUICanvasGroup;
    [SerializeField] private UnityEngine.UI.Text qteInstructionText;
    [SerializeField] private UnityEngine.UI.Image qteProgressBar;

    [Header("Beam Progress Callback")]
    private System.Action<float> onBeamProgressChanged; // Callback to notify beam progress

    private int currentButtonPresses = 0;
    private float lastKeyPressTime = 0f;
    private bool qteActive = false;
    private float currentBeamProgress = 0f; // 0 = no progress, 1 = full distance
    public void SetUIReferences(CanvasGroup canvasGroup, UnityEngine.UI.Text instructionText, UnityEngine.UI.Image progressBar)
    {
        qteUICanvasGroup = canvasGroup;
        qteInstructionText = instructionText;
        qteProgressBar = progressBar;
    }
    public void SetBeamProgressCallback(System.Action<float> callback)
    {
        onBeamProgressChanged = callback;
    }

    public IEnumerator StartQTE()
    {
        qteActive = true;
        currentButtonPresses = 0;
        lastKeyPressTime = Time.time;
        currentBeamProgress = 0f;

        ShowQTEUI();

        float startTime = Time.time;

        while (Time.time - startTime < qteDuration && qteActive)
        {
            // Check for button press with throttle
            if (Input.GetKeyDown(qteButton) && Time.time - lastKeyPressTime >= keyPressThrottleTime)
            {
                currentButtonPresses++;
                lastKeyPressTime = Time.time;

                // Calculate beam progress: more presses = further the beam goes
                // Increase progress by increments (e.g., each press adds 5%)
                currentBeamProgress = Mathf.Min(1f, currentBeamProgress + 0.05f);

                // Notify beam system of progress change
                onBeamProgressChanged?.Invoke(currentBeamProgress);

                // Update UI
                if (qteProgressBar != null)
                {
                    qteProgressBar.fillAmount = currentBeamProgress;
                }
            }

            // Update timer display
            float timeRemaining = qteDuration - (Time.time - startTime);
            if (qteInstructionText != null)
            {
                qteInstructionText.text = $"Spam {qteButton}!\nBeam Power: {(currentBeamProgress * 100f):F0}%\nTime: {timeRemaining:F1}s";
            }

            yield return null;
        }

        HideQTEUI();
        // Return final beam progress (>0.5f = Lucio wins, otherwise Cedric wins)
        yield return currentBeamProgress;
    }

    private void ShowQTEUI()
    {
        if (qteUICanvasGroup != null)
        {
            qteUICanvasGroup.alpha = 1f;
            qteUICanvasGroup.gameObject.SetActive(true);
        }

        if (qteProgressBar != null)
        {
            qteProgressBar.fillAmount = 0f;
        }
    }

    private void HideQTEUI()
    {
        if (qteUICanvasGroup != null)
        {
            StartCoroutine(FadeOutUI());
        }
    }

    private IEnumerator FadeOutUI()
    {
        float duration = 0.5f;
        float elapsed = 0f;

        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            if (qteUICanvasGroup != null)
            {
                qteUICanvasGroup.alpha = Mathf.Lerp(1f, 0f, elapsed / duration);
            }
            yield return null;
        }

        if (qteUICanvasGroup != null)
        {
            qteUICanvasGroup.gameObject.SetActive(false);
        }
    }

    public void CancelQTE()
    {
        qteActive = false;
    }

    public int GetCurrentButtonPresses() => currentButtonPresses;

    public float GetFinalBeamProgress() => currentBeamProgress;
}
