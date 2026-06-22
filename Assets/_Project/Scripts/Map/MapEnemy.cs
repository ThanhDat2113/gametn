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
        var formationManager = FindFirstObjectByType<FormationManager>();
        if (formationManager == null)
        {
            Debug.LogError("[MapEnemy] Không tìm thấy FormationManager!");
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

    public void MarkAsDefeated()
    {
        Destroy(gameObject);
    }
}