# Beam VFX Prefab Setup Guide

## 概述
`BeamAttackSystem` hiện đã được cập nhật để sử dụng **VFX Prefab** thay vì LineRenderer. Beam sẽ scale dọc theo **X axis** dựa trên QTE progress.

---

## 🎯 Cách Hoạt Động

### Luci's Beam (QTE Controlled)
- **Scale động**: Tăng từ 0 đến `maxBeamScale` dựa trên mức độ spam
- **Real-time update**: Mỗi lần nhấn nút → scale tăng → VFX phát triển
- **Hướng**: Từ Lucio → Cedric

### Cedric's Beam (Counter-attack)
- **Scale cố định**: Luôn ở `maxBeamScale` (100%)
- **Hướng**: Từ Cedric → Lucio

---

## 📋 Setup Steps

### Step 1: Assign Beam Prefabs
1. Mở **BeamAttackSystem** trong Inspector
2. Tìm section **Beam Prefabs**
3. Assign 2 prefab VFX:
   - **Lucio Beam Prefab**: VFX beam của Lucio
   - **Cedric Beam Prefab**: VFX beam của Cedric

### Step 2: Configure Beam Settings
Trong **Beam Settings**:
- **Max Beam Scale**: `10` (default)
  - Giá trị này là scale X tối đa (khi progress = 100%)
  - Thay đổi để phù hợp với size prefab của bạn
  
- **Beam Duration**: `0.5` (default)
  - Thời gian beam hiển thị (giây)

### Step 3: Test
1. Play scene
2. Cutscene chạy, QTE bắt đầu
3. **Spam Space** và xem beam scale tăng dần!

---

## ⚙️ Prefab Requirements

Các prefab VFX của bạn cần:

1. **Có local scale mặc định (1, 1, 1)**
   - Script sẽ thay đổi X axis: `(progress * maxBeamScale, 1, 1)`

2. **Không có parent transform lạ**
   - Được instantiate tại vị trí nhân vật

3. **Hướng mặc định**: +X (phải)
   - Script sẽ rotate dựa trên hướng từ source → target

---

## 📊 Scale Progression Example

Nếu `maxBeamScale = 10`:

| QTE Progress | Beam Scale X | Visual |
|---|---|---|
| 0% | 0 | Không hiển thị |
| 25% | 2.5 | Ngắn |
| 50% | 5 | Nửa đường |
| 75% | 7.5 | Gần trúng |
| 100% | 10 | Tối đa |

---

## 🎮 Real-time Update Flow

```
Player spam Space
    ↓
QuickTimeEventSystem.UpdateLucioBeamProgress(progress)
    ↓
BeamAttackSystem.UpdateLucioBeamProgress(progress)
    ↓
lucioBeamInstance.scale.x = progress * maxBeamScale
    ↓
Beam VFX mở rộng trực tiếp!
```

---

## 🔧 Customization

### Thay đổi tốc độ scale tăng
Trong `QuickTimeEventSystem.cs`:
```csharp
// Mỗi nhấn nút += bao nhiêu progress
currentBeamProgress = Mathf.Min(1f, currentBeamProgress + 0.05f); // 5% per press
// Thay đổi 0.05f để tăng/giảm khó độ
```

### Thay đổi scale tối đa
Trong `BeamAttackSystem.cs`:
```csharp
[SerializeField] private float maxBeamScale = 10f; // Điều chỉnh giá trị này
```

### Thay đổi thời gian beam
```csharp
[SerializeField] private float beamDuration = 0.5f; // Beam hiển thị bao lâu
```

---

## 🐛 Troubleshooting

### Beam không hiển thị
- ✅ Kiểm tra prefab đã assign chưa
- ✅ Xem prefab có active không
- ✅ Check Console có error không

### Beam không scale
- ✅ Kiểm tra `maxBeamScale > 0`
- ✅ Xác nhận QTE đang chạy (`updateLucioBeamLive = true`)
- ✅ Debug: In `currentLucioBeamProgress` ra Console

### Beam không ở vị trí đúng
- ✅ Prefab được instantiate tại `source.GetPosition()`
- ✅ Kiểm tra xem nhân vật position có đúng không
- ✅ Xê dịch prefab nếu cần offset

### Beam có pivot point sai
- ✅ Prefab phải có **pivot ở trái** (để scale từ trái sang phải)
- ✅ Nếu pivot ở giữa, hãy điều chỉnh trong prefab editor

---

## 💡 Tips

1. **Để beam ít xuyên động**: Tăng `beamDuration`
2. **Để beam dễ nhìn thấy scale**: Giữ `maxBeamScale` = 10
3. **Để QTE khó hơn**: Giảm `qteDuration` hoặc giảm progress per press
4. **Để VFX ấn tượng**: Dùng particle effects trong prefab

---

## 📍 Key Methods

### Trong BeamAttackSystem.cs

```csharp
// Cập nhật progress của Lucio beam (gọi từ QTE)
public void UpdateLucioBeamProgress(float progress)

// Cập nhật scale của beam theo progress
private void UpdateBeamScale(GameObject beam, float progress)

// Dừng tất cả beam attacks
public void StopBeamAttack()

// Xóa tất cả active beams
private void DestroyAllBeams()
```

---

**Vậy thế là xong! Giờ beam VFX của bạn sẽ scale động theo mức độ spam! 🚀**
