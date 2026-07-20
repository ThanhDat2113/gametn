using UnityEngine;
using System.Collections;
using TMPro; // Cần dùng cho TextMeshPro

public class StandingNPCTalk : MonoBehaviour
{
    [Header("Settings")]
    public float idleDuration = 4f;   // Thời gian NPC đứng im trước khi nói (VD: 4 giây)
    public float talkDuration = 5f;   // Thời gian khung chat hiện lên (VD: 5 giây)

    [Header("References")]
    public GameObject dialogueBubble; // Kéo cái DialogueCanvas vào đây
    public TextMeshProUGUI dialogueText; // Kéo cái Text (TMP) vào đây
    public string[] lines;            // Mảng chứa các câu thoại

    private int currentLineIndex = 0; // Chỉ số câu thoại hiện tại

    private void Start()
    {
        // Đảm bảo bong bóng tắt ngay khi vào game
        dialogueBubble.SetActive(false);
        // Bắt đầu chu trình nói chuyện
        StartCoroutine(TalkRoutine());
    }

    IEnumerator TalkRoutine()
    {
        while (true) // Lặp lại mãi mãi
        {
            // 1. Đứng yên chờ một lúc
            yield return new WaitForSeconds(idleDuration);

            // 2. Bật bong bóng chat lên
            dialogueBubble.SetActive(true);
            if (lines.Length > 0)
            {
                dialogueText.text = lines[currentLineIndex];
                currentLineIndex = (currentLineIndex + 1) % lines.Length;
            }

            // 3. Giữ khung chat trong thời gian quy định
            yield return new WaitForSeconds(talkDuration);

            // 4. Tắt bong bóng chat
            dialogueBubble.SetActive(false);
        }
    }
}