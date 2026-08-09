using UnityEngine;

public class HitEventReceiver : MonoBehaviour
{
    // Lắng nghe từ UnitView
    public event System.Action<int> OnHitFrame;
    public event System.Action<int> OnVFXFrame;

    // Báo hiệu skill animation đã chạy xong toàn bộ đòn
    public event System.Action OnSkillEnd;

    // ── Gọi từ Animation Event ────────────────────────────────
    // Tên hàm phải khớp CHÍNH XÁC với Function trong Animation Event window

    // Đặt event: Function = "OnHit", Int = hitIndex (bắt đầu từ 0)
    public void OnHit(int hitIndex)
    {
        OnHitFrame?.Invoke(hitIndex);
    }

    // Đặt event: Function = "OnSpawnVFX", Int = vfxIndex
    public void OnSpawnVFX(int vfxIndex)
    {
        OnVFXFrame?.Invoke(vfxIndex);
    }

    // Đặt event này trên frame CUỐI CÙNG của mỗi skill clip
    // Function = "OnSkillAnimationEnd", không cần tham số
    public void OnSkillAnimationEnd()
    {
        OnSkillEnd?.Invoke();
    }
}