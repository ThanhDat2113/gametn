# TODO: Charlotte Passive Rework (Gió Tiên)

## ✅ Complete

Cơ chế mới: mỗi khi đồng minh (hoặc chính Charlotte) áp debuff lên kẻ địch,
Charlotte sẽ NHẢY LƯỢT (bỏ qua lượt chọn của cô) và ngay lập tức dùng
skill 1 (Cắt Gió) vào đúng kẻ địch vừa nhận debuff.

## Steps
1. ✅ Add `OnDebuffApplied` event + `TriggerDebuffApplied` to `CombatManager.cs`.
2. ✅ Add Charlotte follow-up state & methods to `CombatManager.cs`
   (`RequestCharlotteFollowUp`, `ProcessCharlotteFollowUp`, count reset, loop integration).
3. ✅ Add debuff detection trigger in `ApplyStatusEffect.cs`.
4. ✅ Rewrite `CharlottePassive.cs` to subscribe to `OnDebuffApplied` and request follow-up.
5. ✅ `CombatManager`: thay `UnitName == "Charlotte"` bằng `GetCharlotteUnit()` (tìm theo passive type)
   vì nhân vật thật trong data tên **"Kurumi"** nhưng dùng script `CharlottePassive`.
6. ✅ Verify compilation / summarize.

## Lưu ý
- `RequestCharlotteFollowUp(target)` đánh dấu `Charlotte.HasActedThisTurn = true`
  để UI bỏ qua lượt chọn của cô.
- `ProcessCharlotteFollowUp()` dùng skill 1 vào target, trừ AP, resolve action.
- Tối đa 2 lần follow-up mỗi lượt player (reset ở đầu `DoPlayerTurn`).
- Không dùng hệ thống cũ (50% sát thương thêm) — dùng skill 1 đầy đủ.
