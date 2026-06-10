using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// Panel chiến thắng có sẵn trong scene, ban đầu bị ẩn.
/// </summary>
public class VictoryPanel : MonoBehaviour
{
    [Header("UI References")]
    public Transform contentParent;          // Content của ScrollRect
    public GameObject entryPrefab;            // Prefab cho một dòng nhân vật

    [Header("Fade Settings (tuỳ chọn)")]
    public CanvasGroup panelCanvasGroup;
    public float fadeDuration = 0.3f;

    private List<GameObject> currentEntries = new List<GameObject>();
    private bool isWaitingForClick = false;

    private void Awake()
    {
        // Đảm bảo panel bắt đầu ẩn
        gameObject.SetActive(false);
        
        if (panelCanvasGroup == null)
            panelCanvasGroup = GetComponent<CanvasGroup>();
        if (panelCanvasGroup == null && GetComponent<Canvas>() != null)
            panelCanvasGroup = gameObject.AddComponent<CanvasGroup>();
    }

    private void Update()
    {
        // Nếu đang chờ click chuột, bắt sự kiện click bất kỳ
        if (isWaitingForClick && Input.GetMouseButtonDown(0))
        {
            isWaitingForClick = false;
            OnContinue();
        }
    }

    /// <summary>
    /// Hiển thị panel với dữ liệu từ đội thắng và EXP nhận được.
    /// </summary>
    public void Show(List<CombatUnit> playerUnits, Dictionary<CharacterData, int> expGained)
    {
        // Xoá các dòng cũ
        foreach (var entry in currentEntries)
            Destroy(entry);
        currentEntries.Clear();

        // Tạo dòng mới cho từng nhân vật
        foreach (var unit in playerUnits)
        {
            if (unit == null || unit.Data == null) continue;

            GameObject entryGO = Instantiate(entryPrefab, contentParent);
            currentEntries.Add(entryGO);
            VictoryEntryUI entry = entryGO.GetComponent<VictoryEntryUI>();
            if (entry == null)
            {
                Debug.LogError("Entry prefab thiếu component VictoryEntryUI!");
                continue;
            }

            CharacterData data = unit.Data;
            int gained = expGained.ContainsKey(data) ? expGained[data] : 0;
            int currentLevel = PlayerProgression.Instance.GetLevel(data);
            int currentExp = PlayerProgression.Instance.GetCurrentExp(data);
            int neededExp = PlayerProgression.Instance.GetExpToNextLevel(data);

            entry.Setup(data, gained, currentLevel, currentExp, neededExp);
        }

        // Hiển thị panel
        gameObject.SetActive(true);
        isWaitingForClick = false; // Chưa cho click ngay — chờ hết fade

        // Fade in rồi mới bắt đầu lắng nghe click
        if (panelCanvasGroup != null)
        {
            panelCanvasGroup.alpha = 0f;
            StartCoroutine(FadeInThenListen());
        }
        else
        {
            // Không có fade: đợi 1 frame để tránh bắt click cũ
            StartCoroutine(EnableClickNextFrame());
        }
    }

    private IEnumerator FadeInThenListen()
    {
        float elapsed = 0f;
        while (elapsed < fadeDuration)
        {
            elapsed += Time.deltaTime;
            panelCanvasGroup.alpha = Mathf.Clamp01(elapsed / fadeDuration);
            yield return null;
        }
        panelCanvasGroup.alpha = 1f;

        // Đợi thêm 1 frame để chắc chắn click hiện tại không bị bắt
        yield return null;
        isWaitingForClick = true;
    }

    private IEnumerator EnableClickNextFrame()
    {
        yield return null;
        isWaitingForClick = true;
    }

    private void OnContinue()
    {
        // Gỡ scene combat và quay về map
        SceneLoaderManager.UnloadCombatScene();

        // Ẩn panel (không huỷ, để dùng lại lần sau)
        gameObject.SetActive(false);
    }
}