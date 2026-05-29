using UnityEngine;
using System.Collections.Generic;

public class CameraObstruction : MonoBehaviour
{
    public Transform player;               // Kéo nhân vật vào đây
    public LayerMask obstructionMask;      // Chọn layer Obstruction
    public float fadeAlpha = 0.3f;         // Độ trong suốt khi bị che (0.0 - 1.0)
    public float fadeSpeed = 5f;           // Tốc độ mờ dần

    // Lưu trữ các renderer đang bị làm mờ và màu gốc
    private Dictionary<Renderer, Color[]> originalColors = new Dictionary<Renderer, Color[]>();
    private List<Renderer> currentObstructions = new List<Renderer>();

    void Update()
    {
        if (player == null) return;

        // Vector từ camera đến player
        Vector3 direction = player.position - transform.position;
        float distance = direction.magnitude;

        // Bắn tia từ camera đến player, chỉ va chạm với layer Obstruction
        RaycastHit[] hits = Physics.RaycastAll(transform.position, direction.normalized, distance, obstructionMask);

        List<Renderer> newObstructions = new List<Renderer>();

        foreach (RaycastHit hit in hits)
        {
            Renderer rend = hit.collider.GetComponent<Renderer>();
            if (rend != null)
            {
                newObstructions.Add(rend);
            }
        }

        // Khôi phục những vật không còn che nữa
        for (int i = currentObstructions.Count - 1; i >= 0; i--)
        {
            Renderer rend = currentObstructions[i];
            if (!newObstructions.Contains(rend))
            {
                // Trả lại màu gốc
                StartCoroutine(FadeRenderer(rend, 1f));
                currentObstructions.RemoveAt(i);
            }
        }

        // Làm mờ những vật mới xuất hiện
        foreach (Renderer rend in newObstructions)
        {
            if (!currentObstructions.Contains(rend))
            {
                currentObstructions.Add(rend);
                StartCoroutine(FadeRenderer(rend, fadeAlpha));
            }
        }
    }

    // Coroutine thay đổi alpha dần dần
    System.Collections.IEnumerator FadeRenderer(Renderer rend, float targetAlpha)
    {
        Material[] materials = rend.materials;

        // Lưu màu gốc nếu chưa lưu
        if (!originalColors.ContainsKey(rend))
        {
            Color[] cols = new Color[materials.Length];
            for (int i = 0; i < materials.Length; i++)
            {
                // Lấy màu hiện tại (có thể khác màu trắng)
                if (materials[i].HasProperty("_Color"))
                    cols[i] = materials[i].color;
                else if (materials[i].HasProperty("_BaseColor"))
                    cols[i] = materials[i].GetColor("_BaseColor");
                else
                    cols[i] = Color.white; // mặc định
            }
            originalColors[rend] = cols;
        }

        // Lấy màu bắt đầu
        Color[] startColors = new Color[materials.Length];
        for (int i = 0; i < materials.Length; i++)
        {
            if (materials[i].HasProperty("_Color"))
                startColors[i] = materials[i].color;
            else if (materials[i].HasProperty("_BaseColor"))
                startColors[i] = materials[i].GetColor("_BaseColor");
        }

        // Thay đổi dần
        float elapsed = 0f;
        while (elapsed < 1f)
        {
            elapsed += Time.deltaTime * fadeSpeed;
            float t = Mathf.Clamp01(elapsed);

            for (int i = 0; i < materials.Length; i++)
            {
                Color newColor = Color.Lerp(startColors[i], new Color(startColors[i].r, startColors[i].g, startColors[i].b, targetAlpha), t);
                if (materials[i].HasProperty("_Color"))
                    materials[i].color = newColor;
                else if (materials[i].HasProperty("_BaseColor"))
                    materials[i].SetColor("_BaseColor", newColor);
            }
            yield return null;
        }

        // Đảm bảo đạt đúng alpha cuối cùng
        for (int i = 0; i < materials.Length; i++)
        {
            Color finalColor = startColors[i];
            finalColor.a = targetAlpha;
            if (materials[i].HasProperty("_Color"))
                materials[i].color = finalColor;
            else if (materials[i].HasProperty("_BaseColor"))
                materials[i].SetColor("_BaseColor", finalColor);
        }
    }
}