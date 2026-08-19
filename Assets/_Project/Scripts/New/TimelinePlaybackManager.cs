using UnityEngine;

/// <summary>
/// Quản lý trạng thái "đang chạy Timeline" trên map.
/// Khi Timeline đang chạy:
///   - Player không thể di chuyển (HSRPlayerController kiểm tra flag này)
///   - Quest UI bị ẩn (QuestUI kiểm tra flag này)
///   - Minimap bị ẩn (MinimapController kiểm tra flag này)
///
/// Các script phát Timeline (QuestTimelineTrigger, QuestTeleportTrigger, ...)
/// gọi BeginTimeline() trước khi Play() và EndTimeline() sau khi Timeline xong.
/// </summary>
public static class TimelinePlaybackManager
{
    private static int _activeTimelineCount = 0;

    /// <summary>True nếu có ít nhất 1 Timeline đang chạy.</summary>
    public static bool IsTimelinePlaying => _activeTimelineCount > 0;

    /// <summary>
    /// Gọi khi bắt đầu phát Timeline. Tăng counter — hỗ trợ nhiều Timeline
    /// chạy cùng lúc (chỉ khi tất cả kết thúc mới coi là không còn Timeline).
    /// </summary>
    public static void BeginTimeline()
    {
        _activeTimelineCount++;
        Debug.Log($"[TimelinePlaybackManager] BeginTimeline → active={_activeTimelineCount}");
    }

    /// <summary>
    /// Gọi khi Timeline kết thúc. Giảm counter.
    /// </summary>
    public static void EndTimeline()
    {
        if (_activeTimelineCount <= 0)
        {
            Debug.LogWarning("[TimelinePlaybackManager] EndTimeline được gọi nhưng không có Timeline nào đang chạy — bỏ qua.");
            return;
        }

        _activeTimelineCount--;
        Debug.Log($"[TimelinePlaybackManager] EndTimeline → active={_activeTimelineCount}");
    }

    /// <summary>Reset toàn bộ trạng thái (dùng khi load scene mới).</summary>
    public static void Reset()
    {
        _activeTimelineCount = 0;
    }
}