using System.Collections.Generic;

/// <summary>
/// Registry mô tả nội tại dùng cho tính năng hover xem thông tin unit trong combat.
/// Ưu tiên tra theo TÊN UNIT (ByUnit — phe ta, vì tên unit ≠ tên class passive),
/// fallback theo tên class passive (ByClass — boss/quái).
/// Chỉnh sửa mô tả ở ĐÂY — 1 nơi duy nhất.
/// </summary>
public static class PassiveDescriptions
{
    // ── Phe ta: key = characterName trong CHAR_*.asset ──
    private static readonly Dictionary<string, string> ByUnit = new Dictionary<string, string>
    {
        { "Aqua", "Chỉ Dẫn Trưởng Lão: Mỗi khi một đồng minh (kể cả bản thân) dùng kỹ năng tốn từ 2 AP trở lên, hành động đó được cường hóa +10% sát thương." },
        { "Escanor", "Ý Chí: Mỗi lần nhận sát thương tích 1 stack Ý Chí (+5% sát thương/stack); tự tiêu hết khi tấn công." },
        { "Eugeo", "Xuyên Giáp Thú Săn: Bỏ qua 30% phòng thủ của mọi kẻ địch. Mỗi khi hạ gục 1 kẻ địch, cộng thêm 10% xuyên giáp (tối đa +20%)." },
        { "Kaneki", "Hút Máu: Mọi đòn đánh hồi cho bản thân 20% sát thương gây ra." },
        { "Kurumi", "Zafkiel: Mỗi lần một kẻ địch nhận 1 hiệu ứng xấu, Kurumi lập tức tấn công kẻ địch đó bằng Skill 1 (giới hạn 2 lần mỗi lượt)." },
        { "Naofumi", "Thủ Thế: Chịu thay đồng minh 30% sát thương mỗi đòn; khi đang Thủ Thế mà bị đánh → choáng kẻ tấn công 1 lượt (hồi sau 3 lượt)." },
        { "Rin", "Ánh Sáng Trừng Phạt: Mỗi đòn đánh trúng kẻ địch đang bị Điểm Yếu → tích 1 stack Siêu Việt (+10% sát thương/stack)." },
        { "Saber", "Giáp Tích Lũy: Mỗi lần tấn công hoặc bị tấn công tích 1 lớp giáp — mỗi lớp chặn 5% sát thương của 1 đòn (tối đa 5 lớp)." },
        { "Sakura", "Bụi Sao: Mỗi AP tiêu tốn tích 1 stack Bụi Sao (+5% sát thương/stack)." },
        { "Tharja", "Hồi Máu Phản Công: Nếu được hồi máu, lập tức tấn công vào kẻ địch đang yếu máu nhất." },
        { "Tohka", "Hút Máu: Mọi đòn đánh hồi cho bản thân 20% sát thương gây ra." },
        { "Toji", "Xuyên Giáp Thú Săn: Bỏ qua 30% phòng thủ của mọi kẻ địch. Mỗi khi hạ gục 1 kẻ địch, cộng thêm 10% xuyên giáp (tối đa +20%)." },
        { "Vergil", "Thợ Săn Tiền Thưởng: +20% tỷ lệ chí mạng; kết liễu kẻ địch → hành động thêm 1 lượt ngay lập tức." },
    };

    // ── Boss & quái: key = tên class passive ──
    private static readonly Dictionary<string, string> ByClass = new Dictionary<string, string>
    {
        { "EdwardPassive", "Giả Kim Thuật Hút Sinh: Hồi máu bằng 60% sát thương gây ra; 3 hành động/lượt; mọi skill giảm 20% ATK mục tiêu trúng đòn (2 lượt); sau khi hành động xong, mỗi lần bị đánh sẽ phản công bằng Skill 1 (tối đa 20 sát thương)." },
        { "GilgameshPassive", "King's Accumulation: Mỗi lần gây sát thương tăng vĩnh viễn 1% sát thương; 20% tấn công thêm bằng Skill 1 khi tấn công; khi bị đánh → 100% phản đòn ngay lập tức + thêm lượt hành động (không giới hạn); khi gục xuống kích hoạt Enuma Elish — Final gây 25% máu tối đa (sát thương chuẩn) lên toàn bộ team; 2 hành động/lượt." },
        { "MadaraPassive", "Shadow Clone + Izanagi: Khi HP dưới 50% tạo 1 Phân Thân (lặp lại mọi skill của Madara); lần đầu gục xuống, Izanagi hồi sinh với 30% máu và xóa toàn bộ hiệu ứng xấu; 4 hành động/lượt (giảm còn 3 sau Izanagi)." },
        { "HassanPassive", "Zabaniya — Ảo Ảnh Tử Thần: Đầu trận tạo 2 Ảo Ảnh (1 HP); không thể bị nhắm khi còn Ảo Ảnh; hết Ảo Ảnh → lộ diện với ATK +50% (1 lượt) và tái tạo Ảo Ảnh sau 2 lượt; hồi sinh 25% máu lần đầu gục; hút 5% máu khi hạ gục; 2 hành động/lượt, bỏ qua khiêu khích." },
        { "ReinhardPassive", "Huyết Mạch Kiếm Thánh: 40% khi bị đánh → ATK +10% trong 2 lượt; 100% phản đòn ngay lập tức khi bị đánh + thêm lượt hành động; 2 hành động/lượt, bỏ qua khiêu khích." },
        { "WolfPassive", "Sói Đi Săn: Luôn hành động đầu tiên trong trận, trước cả player." },
        { "SlimePassive", "Chất Nhầy Dính: Đòn đánh đầu tiên gây choáng mục tiêu bị đánh (chỉ khi team đối phương còn từ 2 người trở lên)." },
        { "SkeletonPassive", "Xương Khô Bất Diệt: Hồi sinh với 30% máu tối đa sau lần chết đầu tiên." },
        { "SpiderPassive", "Nọc Độc: 30% khi bị tấn công cận chiến → kẻ tấn công trúng Thiêu Đốt 2 lượt." },
        { "MushroomPassive", "Bào Tử Nổ: Khi chết, gây 10% máu tối đa (sát thương chuẩn) lên toàn bộ đối thủ." },
        { "TreantPassive", "Sinh Khí Dồi Dào: Hồi 5% máu tối đa mỗi đầu lượt của bản thân." },
        { "OrcPassive", "Cơn Thịnh Nộ: Khi HP dưới 50%, tăng vĩnh viễn 30% sát thương." },
        { "EliteSoldierPassive", "Kỷ Luật Sắt: Khi HP dưới 30%, tăng vĩnh viễn 50% sát thương." },
        { "GoblinPassive", "Bầy Đàn: Khi hành động, mỗi đồng minh còn sống (kể cả bản thân) tăng 10% sát thương." },
    };

    /// <summary>
    /// Lấy mô tả nội tại của unit. Ưu tiên theo tên unit (phe ta),
    /// fallback theo tên class passive (boss/quái), cuối cùng là description
    /// của passiveScript nếu nó là SkillData (trường hợp Tharja).
    /// Trả về null nếu không tìm thấy.
    /// </summary>
    public static string Get(CombatUnit unit)
    {
        if (unit == null) return null;

        // 1. Theo tên unit (phe ta)
        if (ByUnit.TryGetValue(unit.UnitName, out var byUnit)) return byUnit;

        // 2. Theo tên class passive (boss/quái)
        var passive = unit.Passive;
        if (passive != null && ByClass.TryGetValue(passive.GetType().Name, out var byClass)) return byClass;

        // 3. passiveScript là SkillData (passive dạng asset, vd Tharja)
        if (unit.Data != null && unit.Data.passiveScript is SkillData sd && !string.IsNullOrEmpty(sd.description))
            return sd.description;

        return null;
    }
}