using System;
using UnityEngine;

public enum QuestRewardType
{
    NewCharacter,   // Mở khóa nhân vật mới vào roster
    Item,           // Thêm item/equipment vào inventory
    Experience,     // Cộng kinh nghiệm cho party (số lượng = amount)
}

/// <summary>
/// Một phần thưởng đơn lẻ trong quest.
/// Gán vào QuestData.rewards[] trong Inspector.
/// </summary>
[Serializable]
public class QuestReward
{
    public QuestRewardType rewardType;

    [Tooltip("Dùng khi rewardType = NewCharacter")]
    public CharacterData character;

    [Tooltip("Dùng khi rewardType = Item")]
    public ItemData item;

    [Tooltip("Số lượng item (chỉ dùng với rewardType = Item)\nHoặc số kinh nghiệm (dùng với rewardType = Experience)")]
    public int amount = 1;

    // ── Helpers ───────────────────────────────────────────────

    /// <summary>Tên hiển thị trên bảng reward UI.</summary>
    public string DisplayName()
    {
        return rewardType switch
        {
            QuestRewardType.NewCharacter => character != null ? character.characterName : "(chưa gán)",
            QuestRewardType.Item         => item != null ? item.itemName : "(chưa gán)",
            QuestRewardType.Experience   => $"{amount} EXP",
            _                            => "Unknown"
        };
    }

    /// <summary>Icon hiển thị trên bảng reward UI.</summary>
    public Sprite DisplayIcon()
    {
        return rewardType switch
        {
            QuestRewardType.NewCharacter => character?.portrait,
            QuestRewardType.Item         => item?.icon,
            QuestRewardType.Experience   => null, // Có thể gán icon exp sau
            _                            => null
        };
    }

    /// <summary>Mô tả ngắn (loại phần thưởng + tên).</summary>
    public string DisplayLabel()
    {
        return rewardType switch
        {
            QuestRewardType.NewCharacter => "Đồng đội mới",
            QuestRewardType.Item         => amount > 1 ? $"x{amount}" : "Trang bị",
            QuestRewardType.Experience   => "Kinh nghiệm",
            _                            => ""
        };
    }
}