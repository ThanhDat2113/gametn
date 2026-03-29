# Cấu trúc Hệ Thống Di Chuyển Map: Chronicles of the Void (HD-2D)

## Tổng quan cơ chế (Hybrid HSR x Octopath)
Hệ thống kết hợp sự tự do của di chuyển 3D (Honkai: Star Rail) với phong cách hình ảnh đồ họa giả lập góc nhìn tĩnh (Octopath Traveler).

* **Logic:** Xóa bỏ cơ chế Pathfinding (tìm đường theo Node). Người chơi sử dụng W/A/S/D để điều khiển `CharacterController` di chuyển trực tiếp trên môi trường 3D bằng tọa độ World Space.
* **Thị giác:** Camera sử dụng phép chiếu phối cảnh (Perspective) nhưng khóa cứng góc xoay (Fixed Rotation) và tịnh tiến theo người chơi. Sprite 2D sử dụng kỹ thuật Billboarding để luôn hướng về Camera.

## Cấu trúc Scripts
* `HD2DPlayerController.cs`: Gắn vào Object Player (Root). Xử lý input, vật lý va chạm và gửi Parameter cho Animator.
* `BillboardSprite.cs`: Gắn vào Object Sprite (Child của Player). Khóa xoay trục.
* `HD2DCameraController.cs`: Gắn vào Main Camera. Xử lý logic bám sát (Follow).
* `MapTriggerArea.cs`: Thay thế hệ thống `MapNode` cũ. Hoạt động dựa trên `OnTriggerEnter` của Unity Physics để xác định khi nào người chơi tiến vào một khu vực.

## Yêu cầu thiết lập
1.  Tất cả các khu vực mặt đất phải có Collider.
2.  Object Root của Player phải được gán Tag là `Player` để hệ thống Trigger Area hoạt động.
3.  Animator của Sprite cần có ít nhất các Parameter: `IsMoving` (Bool), `MoveX` (Float), `MoveY` (Float).