using UnityEngine;

/// <summary>
/// Gắn thêm vào CÙNG GameObject với MapEnemy (không cần sửa gì MapEnemy.cs).
/// Khi player chạm vào quái này (trước khi combat bắt đầu), đánh dấu để
/// CombatTutorialManager BẮT BUỘC chạy tutorial ngay khi combat bắt đầu —
/// dùng cho quái "dạy cách đánh" đặt sẵn ở đầu map.
///
/// SETUP:
///   1. Chọn GameObject quái trên map đã có sẵn MapEnemy + Collider (Is Trigger).
///   2. Add component này vào CÙNG GameObject đó.
///   3. (Tuỳ chọn) Bật/tắt forceEvenIfAlreadySeen tuỳ ý muốn.
/// </summary>
[RequireComponent(typeof(MapEnemy))]
public class TutorialEnemyTrigger : MonoBehaviour
{
    [Tooltip("true: luôn chạy tutorial khi chạm quái này, kể cả người chơi đã xem trước đó rồi.\n" +
             "false: chỉ chạy nếu người chơi CHƯA từng xem tutorial này (giống hành vi auto-play mặc định).")]
    [SerializeField] private bool forceEvenIfAlreadySeen = true;

    private void OnTriggerEnter(Collider other)
    {
        if (!other.CompareTag("Player")) return;

        // Đánh dấu TRƯỚC khi MapEnemy load CombatScene — không quan trọng thứ tự
        // OnTriggerEnter giữa 2 component này chạy trước/sau, vì CombatTutorialManager
        // chỉ đọc cờ này SAU KHI CombatScene đã load xong (đủ thời gian để cờ được set).
        CombatTutorialManager.RequestForcedTutorial(forceEvenIfAlreadySeen);
    }
}
