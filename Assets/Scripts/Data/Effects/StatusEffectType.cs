/// <summary>
/// Các loại trạng thái đặc biệt (buff/debuff) có thể cộng dồn hoặc có hiệu ứng riêng.
/// </summary>
public enum StatusEffectType
{
    None,

    // Trạng thái cơ bản
    Stun,           // Choáng: không thể hành động
    Invincible,     // Bất tử: không nhận sát thương
    Taunt,          // Khiêu khích: bắt buộc đối thủ tấn công mình
    ReflectDamage,  // Phản sát thương: trả lại % sát thương nhận vào

    // Trạng thái theo thiết kế nhân vật
    ThieuDot,       // Thiêu đốt: nhận sát thương theo thời gian, cộng dồn (Lei Heng)
    SieuViet,       // Siêu việt: tăng sát thương gây ra, cộng dồn (Lucio)
    DiemYeu,        // Điểm yếu: tăng sát thương nhận vào, cộng dồn (Lucio)
    GioTien,        // Gió tiên: buff đặc biệt cho Charlotte, cho phép skill1 tấn công thêm lần
    YChi,           // Ý chí: tăng sát thương gây ra, cộng dồn, reset sau tấn công (Lei Heng)
    BuiSao,         // Bụi sao: tăng sát thương gây ra, cộng dồn (Lilith)
    GiamSatThuong,  // Giảm sát thương nhận vào, cộng dồn (Celine)
    Empowered,      // Cường hóa đòn đánh tiếp theo (Lilith skill2)
}