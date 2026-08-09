# TODO - Sửa lỗi animation combat (delay, VFX lặp, VFX spawn hết ở hit đầu, hit đầu mất VFX, skill 4 hit chỉ spawn 3 VFX & sai thứ tự)

## Triệu chứng (feedback từ test)
1. **Toàn bộ đơn vị đều bị delay** (không riêng Gilgamesh).
2. **VFX thường bị lặp lại**, animation cũng bị lặp.
3. **VFX spawn toàn bộ ở hit đầu tiên** thay vì spawn theo từng hit.
4. *(Sau lần sửa đầu)* **Hit đầu của skill nhiều hit bị mất VFX.**
5. *(Sau lần sửa thứ hai)* **Skill 4 hit chỉ spawn 3 VFX và VFX spawn sai thứ tự.**

## Nguyên nhân gốc
- Có **2 hệ thống spawn VFX song song**:
  - `ClashAnimationSequence.SpawnSkillVFX()` → spawn TẤT CẢ `vfxEvents` ngay khi skill bắt đầu (hit 0).
  - `UnitView.ProcessVFXAtFrame` (qua `hitReceiver.OnVFXFrame`) → spawn theo vfx-frame event trong animation clip.
  → spawn trùng (Bug 2), spawn hết ở hit đầu (Bug 3).
- Delay toàn bộ (Bug 1): `SpawnSkillVFX` instantiate rất nhiều cùng lúc → hitch; quá nhiều `Debug.Log` trong hot-path.
- Bug 4: khi bỏ hẳn `SpawnSkillVFX` mà chỉ dựa vào `OnVFXFrame` (animation event), các skill nhiều hit không có VFX-frame event ở hit đầu → hit đầu mất VFX.
- Bug 5 (đã xác minh qua dữ liệu `.asset`):
  - **Dữ liệu skill** chỉ có **1 `vfxEvents`** trong khi `hitCount >= 2` (Gilgamesh_Skill1: `hitCount=4` + 1 vfxEvents; Skill2: `hitCount=2` + 1; Skill3: `hitCount=6` + 1).
  - Code cũ `GetVFXEvent(index)` trả `vfxEvents[index]` khi `index < vfxEvents.Length`, else `null` → chỉ spawn **đúng 1 VFX** (index 0) cho skill 4 hit.
  - Ngay cả với cách dùng `currentHitIndex` (index damage) + `SpawnRemainingVFX`, số VFX chỉ = `vfxEvents.Length` nếu không cycle → **không bao giờ spawn đủ hitCount**.

## Giải pháp triệt để (đã thực hiện)
**Tách VFX khỏi animation hit event hoàn toàn — spawn bằng coroutine rải theo thời lượng animation:**

- `UnitView.cs`:
  - Thêm `PlayHitVFXSequence(float duration)` + coroutine `SpawnHitVFXSequence(duration)` — spawn **ĐỦ `hitCount` VFX** rải đều trong cửa sổ animation, **đúng thứ tự** `vfxEvents`, không phụ thuộc clip có bao nhiêu hit-frame event.
  - `GetVFXEvent(index)` **CYCLE/REUSE** các event có sẵn bằng `index % vfxEvents.Length` → skill `hitCount=4` / 1 event sẽ spawn **4 lần** VFX đó (1 mỗi hit), dù chỉ có 1 prefab. Fallback `vfxPrefab` đơn lẻ khi không có `vfxEvents`.
  - `ProcessHitAtFrame` giờ **chỉ chịu damage** (bỏ VFX khỏi hit frame).
  - `FlushPendingOutcomes` giữ `SpawnRemainingVFX()` làm **safety net** (no-op khi coroutine đã spawn đủ).
- `ClashAnimationSequence.cs`:
  - `ExecutePhase` gọi `actorView.PlayHitVFXSequence(animLength)` sau khi set animation trigger.

## Kết quả kỳ vọng cho Gilgamesh_Skill1 (hitCount=4, 1 vfxEvents)
→ Spiel 4 lần VFX duy nhất, mỗi lần cách đều nhau trong animation. Đầy đủ 4 VFX, đúng thứ tự.

## Thay đổi đã thực hiện
- [x] `ClashAnimationSequence.cs`: bỏ hẳn `SpawnSkillVFX()` + `GetVFXPosition()` + `InstantiateVFX()`; gọi `PlayHitVFXSequence`.
- [x] `HitEventReceiver.cs`: bỏ `Debug.Log` mỗi frame.
- [x] `UnitView.cs`: thêm `PlayHitVFXSequence` + `SpawnHitVFXSequence`; `GetVFXEvent` cycle theo modulo; `ProcessHitAtFrame` chỉ damage; giữ `SpawnRemainingVFX` safety net.

## Trạng thái
- [x] Đã sửa xong code.
- [ ] Chờ test lại trong Unity (xác nhận skill 4 hit spawn đủ 4 VFX, đúng thứ tự).

