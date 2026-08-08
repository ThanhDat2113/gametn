# TODO - Thay đổi Passive Gilgamesh + Camera AOE

## Mục tiêu
- Thay cơ chế "phản sát thương ngẫu nhiên" của Gilgamesh bằng cơ chế "nhảy lượt"
  giống Reinhard, không giới hạn số lần.
- Cải thiện camera cho skill AOE: zoom out toàn bộ đội hình địch, center = main
  target, zoom dần theo từng hit.

## Phần 1: Passive Gilgamesh (HOÀN THÀNH)
- [x] 1. Sửa `GilgameshPassive.cs`: đoạn `OnTakeDamage` bỏ 20% proc ngẫu nhiên,
      thay bằng 100% `RequestInterrupt` + `GrantExtraAction` (giống Reinhard),
      không giới hạn.

## Phần 2: Camera AOE skill (HOÀN THÀNH)
- [x] 1. `CombatPlanningUI.cs`: đặt enemy người chơi click (main target) vào đầu
      `finalTargets` → `InitialTargets.First()` = main target.
- [x] 2. `CombatCameraManager.cs`: thêm `FocusAOEAction` + `AdvanceAOEZoom` +
      `ZoomFromToCoroutine` (zoom từ orthographicSize thực tế → có hiệu ứng zoom ra).
- [x] 3. `ClashAnimationSequence.cs`: thêm `IsAOESkill`; `SetupPhase` xử lý AOE
      TRƯỚC `isMoving`; `ExecutePhase`'s onHitHandler gọi `AdvanceAOEZoom`.
- [x] 4. `CombatCameraAnimationIntegration.cs`: bỏ qua skill AOE (không gọi
      `ZoomToUnit` override) — để `FocusAOEAction` điều khiển camera.

## Kiểm tra
- [ ] 5. Kiểm tra compile + chạy trong Unity Editor.
