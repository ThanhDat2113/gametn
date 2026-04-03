using UnityEngine;
using System.Collections;

/// <summary>
/// Scaled Beam Attack System
/// Beams spawn at source, scale to reach target (NO movement - just scaling)
/// Normal attacks: Beam scales to opponent
/// Final attacks: Beam scales to center point
/// </summary>
public class SimpleBeamAttackSystem : MonoBehaviour
{
    [Header("Beam Prefabs")]
    public GameObject lucioBeamVFX; // Public so SimpleBattleSystem can access
    public GameObject cedricBeamVFX; // Public so SimpleBattleSystem can access

    [Header("Beam Settings")]
    [SerializeField] private float beamScaleSpeed = 2f; // How fast beam scales out
    [SerializeField] private float beamLifetime = 2f; // How long beam stays before destroy
    [SerializeField] private float beamGrowSpeed = 1.5f; // Speed of growth during "devour" phase

    /// <summary>
    /// Fire scaled beam from source toward target
    /// Beam STAYS at source position and scales to reach target
    /// </summary>
    public void ShootScaledBeam(Vector3 sourcePos, Vector3 targetPos, GameObject prefab, float duration)
    {
        if (prefab == null)
        {
            Debug.LogError("Beam prefab is not assigned!");
            return;
        }

        StartCoroutine(ScaledBeamSequence(sourcePos, targetPos, prefab, duration));
    }

    /// <summary>
    /// Lucio fires scaled beam at Cedric
    /// </summary>
    public void LucioAttack(Vector3 lucioPos, Vector3 cedricPos)
    {
        ShootScaledBeam(lucioPos, cedricPos, lucioBeamVFX, 0.3f);
    }

    /// <summary>
    /// Cedric fires scaled beam at Lucio
    /// </summary>
    public void CedricAttack(Vector3 cedricPos, Vector3 lucioPos)
    {
        ShootScaledBeam(cedricPos, lucioPos, cedricBeamVFX, 0.3f);
    }

    private IEnumerator ScaledBeamSequence(Vector3 sourcePos, Vector3 targetPos, GameObject prefab, float duration)
    {
        // Instantiate beam at source position (stays here, doesn't move)
        GameObject beamInstance = Instantiate(prefab, sourcePos, Quaternion.identity, null);
        beamInstance.transform.SetParent(null, worldPositionStays: true);
        
        // Calculate direction and distance to target
        Vector3 direction = (targetPos - sourcePos).normalized;
        float distance = Vector3.Distance(sourcePos, targetPos);
        
        // Rotate to face target
        beamInstance.transform.rotation = Quaternion.LookRotation(direction);
        
        // Scale from 0 to reach target distance
        float elapsed = 0f;
        Vector3 originalScale = beamInstance.transform.localScale;
        
        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float progress = elapsed / duration;
            
            // Scale along Z axis to reach target
            Vector3 newScale = originalScale;
            newScale.z = Mathf.Lerp(0.1f, distance, progress);
            beamInstance.transform.localScale = newScale;
            
            yield return null;
        }
        
        // Hold at full scale
        yield return new WaitForSeconds(beamLifetime);
        Destroy(beamInstance);
    }

    /// <summary>
    /// Winner beam - scales large to "devour" opponent
    /// </summary>
    public void WinnerBeamGrow(Vector3 sourcePos, Vector3 targetPos, GameObject prefab, float duration)
    {
        StartCoroutine(WinnerBeamGrowSequence(sourcePos, targetPos, prefab, duration));
    }

    private IEnumerator WinnerBeamGrowSequence(Vector3 sourcePos, Vector3 targetPos, GameObject prefab, float duration)
    {
        // Instantiate winner beam
        GameObject beamInstance = Instantiate(prefab, sourcePos, Quaternion.identity, null);
        beamInstance.transform.SetParent(null, worldPositionStays: true);
        
        Vector3 direction = (targetPos - sourcePos).normalized;
        float distance = Vector3.Distance(sourcePos, targetPos);
        
        beamInstance.transform.rotation = Quaternion.LookRotation(direction);
        Vector3 originalScale = beamInstance.transform.localScale;
        
        float elapsed = 0f;
        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float progress = elapsed / duration;
            
            // Scale grows much larger (1.5x - 3x normal size)
            Vector3 newScale = originalScale;
            newScale.z = Mathf.Lerp(distance, distance * 3f, progress);
            newScale.x = Mathf.Lerp(originalScale.x, originalScale.x * 2f, progress);
            newScale.y = Mathf.Lerp(originalScale.y, originalScale.y * 2f, progress);
            
            beamInstance.transform.localScale = newScale;
            yield return null;
        }
        
        // Hold while consuming
        yield return new WaitForSeconds(beamLifetime);
        Destroy(beamInstance);
    }

    /// <summary>
    /// Rapid-fire multiple beams (legacy)
    /// </summary>
    public void RapidFire(Vector3 sourcePos, Vector3 targetPos, GameObject prefab, int count, float delay)
    {
        StartCoroutine(RapidFireSequence(sourcePos, targetPos, prefab, count, delay));
    }

    private IEnumerator RapidFireSequence(Vector3 sourcePos, Vector3 targetPos, GameObject prefab, int count, float delay)
    {
        for (int i = 0; i < count; i++)
        {
            ShootScaledBeam(sourcePos, targetPos, prefab, 0.2f);
            yield return new WaitForSeconds(delay);
        }
    }
}
