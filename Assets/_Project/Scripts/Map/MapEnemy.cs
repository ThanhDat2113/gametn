using UnityEngine;
using System.Collections;

public class MapEnemy : MonoBehaviour
{
    [Header("Enemy Data")]
    public EnemyGroupData enemyGroup;

    [Header("Transition")]
    public float fadeDuration = 0.5f;

    private bool isTriggered = false;

    private void OnTriggerEnter(Collider other)
    {
        if (!isTriggered && other.CompareTag("Player"))
        {
            isTriggered = true;
            StartCoroutine(StartCombatTransition());
        }
    }

    private IEnumerator StartCombatTransition()
    {
        StopPlayer();

        var formationManager = FindFirstObjectByType<FormationManager>();
        if (formationManager == null)
        {
            Debug.LogError("[MapEnemy] Khong tim thay FormationManager!");
            RestorePlayer();
            yield break;
        }
        formationManager.SaveFormation();

        CombatSessionData.Set(FormationDataStorage.PendingFormation, enemyGroup, fromMap: true);
        CombatSceneStarter.RegisterLastEnemy(this);

        if (FadeController.Instance != null)
            yield return FadeController.Instance.FadeToBlack();

        // --- Ẩn MapRoot ---
        var mapRoot = GameObject.Find("MapRoot");
        if (mapRoot != null)
        {
            SceneLoaderManager.MapRoot = mapRoot;
            mapRoot.SetActive(false);
            Debug.Log("[MapEnemy] MapRoot found and deactivated.");
        }
        else
        {
            Debug.LogError("[MapEnemy] Không tìm thấy MapRoot! Hãy tạo một GameObject tên 'MapRoot' chứa toàn bộ map.");
        }

        // 🔥 Ẩn PersistentContainer
        var persistentContainer = GameObject.Find("PersistentContainer");
        if (persistentContainer != null)
        {
            SceneLoaderManager.PersistentContainer = persistentContainer;
            persistentContainer.SetActive(false);
            Debug.Log("[MapEnemy] PersistentContainer found and deactivated.");
        }
        else
        {
            // Không bắt buộc, nhưng nếu không có thì không ẩn gì
            Debug.LogWarning("[MapEnemy] Không tìm thấy PersistentContainer (không bắt buộc).");
        }

        SceneLoaderManager.LoadCombatScene();
    }

    private void StopPlayer()
    {
        GameObject player = PlayerManager.Instance?.GetPlayer();
        if (player == null) return;

        var hsrController = player.GetComponent<HSRPlayerController>();
        if (hsrController != null)
            hsrController.ResetToIdle();

        var movementScript = PlayerManager.Instance?.playerMovementScript;
        if (movementScript != null && movementScript.enabled)
        {
            movementScript.enabled = false;
            Debug.Log("[MapEnemy] Disabled player movement script.");
        }

        var cc = player.GetComponent<CharacterController>();
        if (cc != null && cc.enabled)
        {
            cc.enabled = false;
            Debug.Log("[MapEnemy] Disabled CharacterController.");
        }

        var rb = player.GetComponent<Rigidbody>();
        if (rb != null)
        {
            rb.linearVelocity = Vector3.zero;
            rb.angularVelocity = Vector3.zero;
        }

        Debug.Log("[MapEnemy] Player stopped for combat transition.");
    }

    private void RestorePlayer()
    {
        GameObject player = PlayerManager.Instance?.GetPlayer();
        if (player == null) return;

        var cc = player.GetComponent<CharacterController>();
        if (cc != null && !cc.enabled)
        {
            cc.enabled = true;
            Debug.Log("[MapEnemy] Re-enabled CharacterController.");
        }

        var movementScript = PlayerManager.Instance?.playerMovementScript;
        if (movementScript != null && !movementScript.enabled)
        {
            movementScript.enabled = true;
            Debug.Log("[MapEnemy] Re-enabled player movement script.");
        }
    }

    public void MarkAsDefeated()
    {
        Destroy(gameObject);
    }
}
