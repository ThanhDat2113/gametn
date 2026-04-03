using UnityEngine;
using System.Collections;

/// <summary>
/// Handles all camera animations for the Lucio cutscene
/// Manages zoom, shake, flick, and movement effects
/// </summary>
public class LucioCutsceneCameraController : MonoBehaviour
{
    private Camera mainCamera;
    private Vector3 originalPosition;
    private float originalSize;

    [Header("Camera Shake Settings")]
    [SerializeField] private float shakeMagnitude = 0.5f;
    [SerializeField] private float shakeFrequency = 20f;

    private void Start()
    {
        mainCamera = Camera.main;
        originalPosition = mainCamera.transform.position;
        originalSize = mainCamera.orthographicSize;
    }

    /// <summary>
    /// Zoom in gradually to each character
    /// </summary>
    public IEnumerator ZoomInToCharacters(CutsceneCharacter character1, CutsceneCharacter character2, float duration)
    {
        float elapsed = 0f;
        Vector3 originalCamPos = mainCamera.transform.position;
        float originalCamSize = mainCamera.orthographicSize;

        // Zoom to first character
        float halfDuration = duration / 2f;
        while (elapsed < halfDuration)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / halfDuration;

            // Move camera to character 1
            mainCamera.transform.position = Vector3.Lerp(originalCamPos, GetCharacterCameraPosition(character1), t);
            mainCamera.orthographicSize = Mathf.Lerp(originalCamSize, 5f, t);

            yield return null;
        }

        elapsed = 0f;
        // Zoom to second character
        while (elapsed < halfDuration)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / halfDuration;

            // Move camera to character 2
            mainCamera.transform.position = Vector3.Lerp(GetCharacterCameraPosition(character1), GetCharacterCameraPosition(character2), t);
            mainCamera.orthographicSize = Mathf.Lerp(5f, 5f, t);

            yield return null;
        }
    }

    /// <summary>
    /// Zoom out from center for depth sensation
    /// </summary>
    public IEnumerator CenterZoomOutForDepth(float duration)
    {
        float elapsed = 0f;
        Vector3 startPos = mainCamera.transform.position;
        float startSize = mainCamera.orthographicSize;

        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / duration;

            // Zoom out and move to center
            mainCamera.transform.position = Vector3.Lerp(startPos, Vector3.zero, t);
            mainCamera.orthographicSize = Mathf.Lerp(startSize, 10f, CurveEaseInOutQuad(t));

            yield return null;
        }

        mainCamera.transform.position = Vector3.zero;
        mainCamera.orthographicSize = 10f;
    }

    /// <summary>
    /// Quick flick between characters
    /// </summary>
    public IEnumerator FlickBetweenCharacters(CutsceneCharacter character1, CutsceneCharacter character2, float duration)
    {
        float elapsed = 0f;
        int flicks = 3;
        float timePerFlick = duration / flicks;

        for (int i = 0; i < flicks; i++)
        {
            elapsed = 0f;
            CutsceneCharacter targetChar = (i % 2 == 0) ? character1 : character2;

            while (elapsed < timePerFlick)
            {
                elapsed += Time.deltaTime;
                float t = elapsed / timePerFlick;

                // Quick easing for flick effect
                mainCamera.transform.position = Vector3.Lerp(mainCamera.transform.position, GetCharacterCameraPosition(targetChar), CurveEaseOutQuad(t));

                yield return null;
            }
        }
    }

    /// <summary>
    /// Pull camera back for impact effect
    /// </summary>
    public IEnumerator CameraImpactPullback(float duration)
    {
        float elapsed = 0f;
        Vector3 startPos = mainCamera.transform.position;
        float startSize = mainCamera.orthographicSize;

        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / duration;

            // Pull back slightly
            mainCamera.orthographicSize = Mathf.Lerp(startSize, startSize + 1.5f, t);
            mainCamera.transform.position = Vector3.Lerp(startPos, startPos + Vector3.back * 0.5f, t);

            yield return null;
        }
    }

    /// <summary>
    /// Subtle camera shake for force sensation
    /// </summary>
    public IEnumerator CameraShakeLight(float duration)
    {
        float elapsed = 0f;
        Vector3 originalPos = mainCamera.transform.position;

        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;

            // Generate shake offset using sine wave
            float shakeX = Mathf.Sin(elapsed * shakeFrequency) * shakeMagnitude;
            float shakeY = Mathf.Cos(elapsed * shakeFrequency * 0.7f) * shakeMagnitude;

            mainCamera.transform.position = originalPos + new Vector3(shakeX, shakeY, 0);

            yield return null;
        }

        mainCamera.transform.position = originalPos;
    }

    /// <summary>
    /// Return camera to center position
    /// </summary>
    public IEnumerator CameraReturnToCenter(float duration)
    {
        float elapsed = 0f;
        Vector3 startPos = mainCamera.transform.position;
        float startSize = mainCamera.orthographicSize;

        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / duration;

            mainCamera.transform.position = Vector3.Lerp(startPos, Vector3.zero, CurveEaseInOutQuad(t));
            mainCamera.orthographicSize = Mathf.Lerp(startSize, 8f, t);

            yield return null;
        }

        mainCamera.transform.position = Vector3.zero;
        mainCamera.orthographicSize = 8f;
    }

    /// <summary>
    /// Focus on victory character
    /// </summary>
    public IEnumerator CameraFocusVictory(CutsceneCharacter victor)
    {
        float duration = 1.5f;
        float elapsed = 0f;
        Vector3 startPos = mainCamera.transform.position;

        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / duration;

            mainCamera.transform.position = Vector3.Lerp(startPos, GetCharacterCameraPosition(victor), CurveEaseOutQuad(t));
            mainCamera.orthographicSize = Mathf.Lerp(mainCamera.orthographicSize, 6f, t);

            yield return null;
        }
    }

    /// <summary>
    /// Focus on defeated character
    /// </summary>
    public IEnumerator CameraFocusDefeat(CutsceneCharacter defeated)
    {
        float duration = 1.5f;
        float elapsed = 0f;
        Vector3 startPos = mainCamera.transform.position;

        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / duration;

            mainCamera.transform.position = Vector3.Lerp(startPos, GetCharacterCameraPosition(defeated), CurveEaseOutQuad(t));
            mainCamera.orthographicSize = Mathf.Lerp(mainCamera.orthographicSize, 6f, t);

            yield return null;
        }
    }

    private Vector3 GetCharacterCameraPosition(CutsceneCharacter character)
    {
        Vector3 charPos = character.GetPosition();
        return new Vector3(charPos.x, charPos.y, -10f);
    }

    // Easing functions
    private float CurveEaseInOutQuad(float t)
    {
        return t < 0.5f ? 2f * t * t : -1f + (4f - 2f * t) * t;
    }

    private float CurveEaseOutQuad(float t)
    {
        return 1f - (1f - t) * (1f - t);
    }
}
