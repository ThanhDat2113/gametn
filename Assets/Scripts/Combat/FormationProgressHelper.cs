
using UnityEngine;

/// <summary>
/// Lớp helper tĩnh để cung cấp các phương thức tiện ích liên quan đến tiến trình của nhân vật.
/// Giúp truy cập dữ liệu từ PlayerProgressData một cách an toàn và dễ dàng.
/// </summary>
public static class FormationProgressHelper
{
    /// <summary>
    /// Lấy cấp độ hiện tại của một nhân vật từ hệ thống tiến trình.
    /// Nếu không tìm thấy, sẽ trả về cấp độ cơ bản từ CharacterData.
    /// </summary>
    public static int GetCurrentLevel(CharacterData characterData)
    {
        if (PlayerProgressData.Instance != null)
        {
            var progress = PlayerProgressData.Instance.GetOrCreateProgress(characterData);
            return progress.CurrentLevel;
        }
        
        // Fallback nếu PlayerProgressData chưa được khởi tạo
        Debug.LogWarning($"[Helper] PlayerProgressData not found. Falling back to base level for {characterData.characterName}.");
        return characterData.baseLevel;
    }

    /// <summary>
    /// Thiết lập cấp độ cho một nhân vật (dùng cho debug/testing).
    /// </summary>
    public static void SetLevel(CharacterData characterData, int level)
    {
        if (PlayerProgressData.Instance != null)
        {
            PlayerProgressData.Instance.SetProgress(characterData, level, 0);
        }
        else
        {
            Debug.LogError("[Helper] Cannot set level. PlayerProgressData not found.");
        }
    }

    /// <summary>
    /// Reset cấp độ và EXP của một nhân vật về trạng thái ban đầu.
    /// </summary>
    public static void ResetLevel(CharacterData characterData)
    {
        if (PlayerProgressData.Instance != null)
        {
            var progress = PlayerProgressData.Instance.GetOrCreateProgress(characterData);
            progress.Reset();
        }
        else
        {
            Debug.LogError("[Helper] Cannot reset level. PlayerProgressData not found.");
        }
    }
}