public enum StatusEffectType
{
    None,
    Stun,           // Choáng (không thể hành động)
    Invincible,     // Bất tử

    // Hiệu ứng cơ bản
    Taunt,          // Khiêu khích
    ReflectDamage,  // Phản sát thương

    // Hiệu ứng theo thiết kế
    ThieuDot,       // Sát thương theo thời gian, cộng dồn (Lei Heng)
    SieuViet,       // Tăng sát thương, cộng dồn (Lucio)
    DiemYeu,        // Tăng sát thương nhận vào, cộng dồn (Lucio)
    GioTien,        // Buff đặc biệt cho Charlotte
    YChi,           // Tăng sát thương, cộng dồn, reset sau khi tấn công (Lei Heng)
    BuiSao,         // Tăng sát thương, cộng dồn (Lilith)
    GiamSatThuong,  // Giảm sát thương nhận vào, cộng dồn (Celine)
}