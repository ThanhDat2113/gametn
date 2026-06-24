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

    private List<VictoryEntryUI> entries = new List<VictoryEntryUI>();
    private int completedAnimations = 0;
    private Dictionary<CharacterData, int> pendingExpGains;
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
        foreach (var entry in entries)
            if (entry != null) Destroy(entry.gameObject);
        entries.Clear();
        completedAnimations = 0;
        pendingExpGains = expGained;

        // Tạo dòng mới cho từng nhân vật
        foreach (var unit in playerUnits)
        {
            if (unit == null || unit.Data == null) continue;

            GameObject entryGO = Instantiate(entryPrefab, contentParent);
            VictoryEntryUI entry = entryGO.GetComponent<VictoryEntryUI>();
            if (entry == null)
            {
                Debug.LogError("Entry prefab thiếu component VictoryEntryUI!");
                continue;
            }
            entries.Add(entry);

            CharacterData data = unit.Data;
            int gained = expGained.ContainsKey(data) ? expGained[data] : 0;
            int startLevel = PlayerProgression.Instance.GetLevel(data);
            int startExp = PlayerProgression.Instance.GetCurrentExp(data);

            entry.Setup(data, gained, startLevel, startExp);
        }

        // Hiển thị panel
        gameObject.SetActive(true);
        isWaitingForClick = false;

        // Fade in rồi chạy animation
        if (panelCanvasGroup != null)
        {
            panelCanvasGroup.alpha = 0f;
            StartCoroutine(FadeInAndAnimate());
        }
        else
        {
            StartCoroutine(AnimateAllEntries());
        }
    }

    private IEnumerator FadeInAndAnimate()
    {
        float elapsed = 0f;
        while (elapsed < fadeDuration)
        {
            elapsed += Time.deltaTime;
            panelCanvasGroup.alpha = Mathf.Clamp01(elapsed / fadeDuration);
            yield return null;
        }
        panelCanvasGroup.alpha = 1f;

        yield return StartCoroutine(AnimateAllEntries());
    }

    private IEnumerator AnimateAllEntries()
    {
        // Chạy animation cho mỗi entry
        foreach (var entry in entries)
            StartCoroutine(entry.AnimateExpGain(PlayerProgression.Instance, () => {
                completedAnimations++;
            }));

        // Chờ tất cả hoàn tất
        while (completedAnimations < entries.Count)
            yield return null;

        // Sau animation mới cho phép click Continue
        isWaitingForClick = true;
    }

    private void OnContinue()
    {
        // Cộng EXP thật vào PlayerProgression
        foreach (var kvp in pendingExpGains)
        {
            PlayerProgression.Instance.AddExperience(kvp.Key, kvp.Value);
        }

        // Gỡ scene combat và quay về map
        SceneLoaderManager.UnloadCombatScene();

        // Ẩn panel (không huỷ, để dùng lại lần sau)
        gameObject.SetActive(false);
    }
}