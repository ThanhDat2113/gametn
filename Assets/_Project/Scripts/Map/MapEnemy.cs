using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class MapEnemy : MonoBehaviour
{
    [Header("Enemy Data")]
    public EnemyGroupData enemyGroup;

    [Header("Loot Override (Tùy chọn)")]
    [Tooltip("Ghi đè loot table cho prefab này. Để trống nếu muốn dùng loot từ EnemyGroupData.")]
    public EnemyGroupData.LootEntry[] overrideLoot;

    [Header("Transition")]
    public float fadeDuration = 0.5f;

    private bool isTriggered = false;

    /// <summary>
    /// Lấy danh sách loot: ưu tiên overrideLoot (trên prefab), fallback về enemyGroup.lootTable.
    /// </summary>
    public List<EnemyGroupData.LootEntry> GetLoot()
    {
        if (overrideLoot != null && overrideLoot.Length > 0)
            return new List<EnemyGroupData.LootEntry>(overrideLoot);

        if (enemyGroup != null && enemyGroup.lootTable != null)
            return new List<EnemyGroupData.LootEntry>(enemyGroup.lootTable);

        return new List<EnemyGroupData.LootEntry>();
    }

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

        // ✅ Ghi nhận rằng đây là từ map (tham số thứ ba = true)
        CombatSessionData.Set(FormationDataStorage.PendingFormation, enemyGroup, fromMap: true);

        CombatSceneStarter.RegisterLastEnemy(this);

        if (FadeController.Instance != null)
            yield return FadeController.Instance.FadeToBlack();

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

        SceneLoaderManager.LoadCombatScene();
    }

    public void MarkAsDefeated()
    {
        Destroy(gameObject);
    }
}