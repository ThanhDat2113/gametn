# TODO - Thay đổi Passive Gilgamesh

## Mục tiêu
- Thay cơ chế "phản sát thương ngẫu nhiên" (20% proc) của Gilgamesh bằng cơ chế
  "nhảy lượt" giống Reinhard, không giới hạn số lần.

## Trạng thái
- [x] 1. `GilgameshPassive.cs`: đoạn `OnTakeDamage` bỏ proc ngẫu nhiên, thay bằng
      100% `RequestInterrupt` + `GrantExtraAction` (giống Reinhard), không giới hạn.

## Ghi chú
- Camera focus AOE (FocusAOEAction / AdvanceAOEZoom) đã bị loại bỏ hoàn toàn
  theo yêu cầu — sẽ xử lý lại sau nếu cần.

## Kiểm tra
- [ ] 2. Kiểm tra compile + chạy trong Unity Editor.
