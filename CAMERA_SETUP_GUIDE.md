# SETUP HƯỚNG DẪN: CAMERA SYSTEM

## 📌 Tổng quan

Hệ thống camera kịch tính gồm:
- **Zoom in** khi clash (gần cảnh)
- **Zoom in** khi unit bị damage
- **Shake effect** khi impact
- **Follow** unit được chọn
- **Smooth zoom out** về default
- **Timing chính xác** tương ứng animation

---

## 🎮 Setup từng bước

### 1. **Tạo Camera GameObject**

```
Hierarchy:
└─ CombatScene
   ├─ CombatManager
   ├─ Canvas (UI)
   └─ CombatCamera (NEW)
       ├─ Tag: MainCamera
       └─ Component: Camera (Orthographic mode)
```

**Cài đặt Camera**:
- Mode: **Orthographic** (không perspective)
- Size: **10** (default size)
- Clear Flags: **Solid Color**
- Background: **Black** (#000000)
- Culling Mask: Chọn layer cho game content

### 2. **Cài đặt Camera**

```
CombatCamera GameObject:
├─ Transform
├─ Camera (Unity component)
└─ CombatCameraManager (thêm script này)
```

**Inspector Setup** (Recommended):

```
Default State
├─ Default Ortho Size: 16 ← TĂNG lên (từ 12, để nhìn rộng hơn)
├─ Default Position: (0, 0, 0)
└─ Camera Height: 8 ← TĂNG LÊN (từ 5, để camera cao hơn)

Zoom Settings
├─ Clash Zoom Size: 7 ← Nếu quá zoom sát, tăng lên 8-9
├─ Damage Zoom Size: 8 ← Nên cao hơn clash size
├─ Zoom In Duration: 0.3
└─ Zoom Out Duration: 0.5

Follow Settings
├─ Follow Offset: (0, 0, -5)
└─ Follow Smoothness: 0.10 ← Mượt mà, tránh jitter

Shake Settings
├─ Shake Intensity: 0.15
├─ Shake Duration: 0.2
└─ Shake Frequency: 20
```

### 3. **Setup ClashAnimationSequence**

Nếu **ClashAnimationSequence** là component của CombatManager:
```
Hierarchy:
└─ CombatManager
   ├─ Component: CombatManager
   └─ Component: ClashAnimationSequence
       └─ Camera Manager: (drag CombatCamera GameObject)
```

### 4. **Test & Verify**

```
Play game → Click START COMBAT
  ↓
✅ Nếu thấy units trên sân:
   → Clash khi 2 unit va chạm
   → Camera zoom in + shake
   → Clash kết thúc
   → Camera tự động zoom out + reset
   → Nhìn thấy lại toàn bộ units khi chọn skill ✅
   → DONE!
   
❌ Nếu vẫn chỉ thấy 2 units sau clash:
   → Xem CAMERA_FIX_PLAYER_PLAN.md
   → Hoặc gọi: cameraManager.AutoFitUnitsInView()
```


---

## 🔧 Cài đặt chi tiết

### Camera Size Settings

```
Default Size = 16       → Nhìn rộng toàn bộ sân (updated! từ 12)
Clash Size = 7          → Zoom gần vào 2 unit
Damage Size = 8         → Zoom vừa phải vào unit bị damage
```

**Điều chỉnh**:
- Thấy units bị cắt? → ↑ Default Size (thử 17-19)
- Vẫn quá zoom? → ↑ Default Size lên 20+
- Clash quá sát? → ↑ Clash Size (thử 8-10)
- Muốn zoom gần hơn? → ↓ con số (nhằng >= 6)
- Muốn zoom xa hơn? → ↑ con số

### Smooth Follow

```
Follow Smoothness = 0.15
├─ 0.0 = không theo (camera đứng im)
├─ 0.15 = mượt (default)
└─ 1.0 = snap instant (bị jank)

Recommended: 0.1-0.3
```

### Shake Effect

```
Shake Intensity = 0.15
├─ 0.05 = very subtle
├─ 0.15 = noticeable (default)
└─ 0.30 = aggressive (có thể gây chóng mặt)

Shake Duration = 0.2s (bao lâu rung)
Shake Frequency = 20 (tốc độ rung)
```

---

## 🎬 Luồng thực thi

### Khi Clash xảy ra

```
1. Clone animation bắt đầu
   ├─ Unit 1 & 2 lao vào nhau (Rush)
   └─ Camera.PlayClashZoom(unit1, unit2)

2. 2 unit đứng đối mặt
   └─ Camera zoom in → clashZoomSize

3. Xúc xắc hiển thị + kết quả
   └─ Camera maintain zoom

4. Bên thua KnockBack
   └─ Camera shake effect

5. Bên thắng tấn công (Skill animation)
   ├─ Camera.ZoomToUnit(winner)
   └─ Camera.PlayImpactShake()

6. Cả 2 trở về vị trí
   └─ Camera.ResetCamera()
      └─ Zoom out smooth + return position
```

### Khi Unit bị Damage (Non-Clash)

```
1. Unit nhận damage
   ├─ OnDamageTaken event trigger
   └─ Camera.ZoomToUnit(damagedUnit)

2. Camera zoom vào unit
   └─ damageZoomSize

3. Camera shake
   └─ PlayImpactShake()

4. Tiếp tục (có thể reset manual hoặc auto)
```

---

## 📋 Event Subscriptions

### Tự động (Built-in)

**CombatCameraManager** tự subscribe:
```csharp
// Start() method
CombatManager.Instance.OnClashResolved += HandleClashResolved;
CombatManager.Instance.OnRoundEnded += HandleRoundEnded;
CombatManager.Instance.OnVictory += HandleCombatEnd;
CombatManager.Instance.OnDefeat += HandleCombatEnd;
```

### Manual Integration (nếu cần)

```csharp
// Trong script riêng
cameraManager.ZoomToUnit(unitTransform, zoomSize);
cameraManager.PlayClashZoom(unit1Transform, unit2Transform);
cameraManager.PlayImpactShake();
cameraManager.ResetCamera();
cameraManager.ZoomToArea(centerPos, radius);
```

---

## 🛠️ Troubleshooting

### ❌ Chỉ thấy 2 unit khi chọn skill (hàng dọc)

**Vấn đề**: Sau clash xong, camera vẫn zoom vào cặp unit đang clash, không thấy toàn bộ
→ **Giải pháp** (tự động):

```
CombatCameraManager bây giờ tự động detect PlayerPlan phase
→ Immediate reset camera
→ AutoFit toàn bộ units (30% buffer)
→ Nhìn thấy hết khi chọn skill

Không cần làm gì thêm! ✅ Auto-magic
```

**Nếu vẫn không work**:
```
Check console log khi enter PlayerPlan:
  [CombatCamera] Entered PlayerPlan - Reset camera...
  [CombatCamera] Auto-fit: Size=X, Units=N

Nếu không có log:
  → CombatManager.Instance chưa initialize?
  → Check Scene có CombatManager không?
```

### ❌ Camera không zoom
→ **Giải pháp**:
1. ✅ CombatCameraManager được add vào Camera GameObject?
2. ✅ Camera component mode = Orthographic?
3. ✅ ClashAnimationSequence có gán Camera Manager?
4. ✅ Scene có CombatManager (Singleton)?

### ❌ Camera quá zoom, không thấy units (NHẤP NHÁY)

**Vấn đề**: Camera quá sát vào 1 điểm, units nhấp nháy/jitter
→ **Giải pháp** (thứ tự try):

**1️⃣ Quick Fix (ngay lập tức)**:
```
Trong CombatCameraManager Inspector:
├─ Default Ortho Size: 15          ← TĂNG từ 12 lên 15
├─ Clash Zoom Size: 8               ← TĂNG từ 7 lên 8
├─ Damage Zoom Size: 9              ← TĂNG từ 8 lên 9
└─ Follow Smoothness: 0.08          ← GIẢM từ 0.10 xuống 0.08

Rồi Play → Test
```

**2️⃣ Nếu vẫn không OK - Auto-fit**:
```
Thêm code vào CombatTestUI Start():

    private void Start()
    {
        var cameraManager = FindObjectOfType<CombatCameraManager>();
        if (cameraManager != null)
        {
            cameraManager.AutoFitUnitsInView();  // ← Auto fit ngay
        }
        
        combat = CombatManager.Instance;
        // ... rest of code
    }
```

**3️⃣ Nếu units vẫn không hiện**:
```
Check:
  ✅ Units có Z-position = 0 không?
  ✅ Units có layer nào bị exclude khỏi Camera culling mask?
  ✅ Unit sprites visible trong scene?
  ✅ Có bao nhiêu units? (Nếu < 2 units, camera có thể bị confuse)
```

### ❌ Camera xập xè / Jitter

**Vấn đề**: Camera rung bất thường hoặc nhảy vọt
→ **Giải pháp**:
1. ↓ Follow Smoothness (thử 0.08-0.12)
2. ↑ Default Ortho Size (thử 13-15)
3. Kiểm tra có Update conflict không?

### ❌ Zoom quá gần / quá xa

**Vấn đề**: Kích cỡ zoom không hợp ý
→ **Giải pháp**:
1. Điều chỉnh `clashZoomSize` (4-8 range)
2. Điều chỉnh `damageZoomSize` (6-9 range)
3. Test trong Play mode + điều chỉnh realtime

### Camera không reset

**Vấn đề**: Sau clash xong, camera không về vị trí cũ
→ **Giải pháp**:
1. ✅ ResetCamera() được gọi trong Phase 5?
2. ✅ Zoom Out Duration > 0?
3. Check debug log: `Debug.Log($"Camera reset: {cameraManager}");`

### Unit không thấy khi zoom

**Vấn đề**: Zoom in quá nhưng unit lại ra ngoài khung
→ **Giải pháp**:
1. ↑ Camera Size (thử 8 thay vì 6)
2. Kiểm tra `followOffset` = (0, 0, -cameraHeight)
3. Verify unit positions trên scene

---

## 🎨 Customization Examples

### Example 1: Zoom gần hơn (Cinematic)

```csharp
// ClashZoomSize: 5 (thay vì 6)
// DamageZoomSize: 6 (thay vì 7)
// ZoomInDuration: 0.2 (nhanh hơn)

Cảm giác: Dramatic, intense
```

### Example 2: Zoom nhẹ (Casual)

```csharp
// ClashZoomSize: 8
// DamageZoomSize: 9
// ShakeIntensity: 0.08 (rung nhẹ)

Cảm giác: Smooth, chưa quá intense
```

### Example 3: Smooth Follow (RPG-style)

```csharp
// FollowSmoothness: 0.08 (mượt hơn)
// ZoomInDuration: 0.4 (chậm)
// ZoomOutDuration: 0.6 (chậm)

Cảm giác: Cinematic, choreographed
```

### Example 4: Snappy (Action Game)

```csharp
// FollowSmoothness: 0.25 (snap nhanh)
// ZoomInDuration: 0.15 (gần instant)
// ShakeIntensity: 0.25 (rung mạnh)

Cảm giác: Punchy, responsive
```

---

## 📐 Math & Formulas

### Orthographic Size to World Height

```
World Height = OrthoSize * 2
World Width = World Height * AspectRatio

Example:
  OrthoSize = 10
  → World Height = 20 units (vertical)
  
  OrthoSize = 6
  → World Height = 12 units (zoom 60% in)
```

### Follow Position Lerp

```
targetPos = Lerp(currentPos, desiredPos, smoothness * deltaTime)

Smoothness = 0.15:
  → Thay 15% khoảng cách mỗi frame
  
  Tại 60FPS:
    Frame 1: Move 15% → Remain 85%
    Frame 2: Move 15% of 85% → Remain 72.25%
    ...
    → Reach ~99% trong ~30 frame (~0.5s)
```

### Perlin Noise Shake

```
offset.x = Noise(elapsed * frequency) * intensity * decay

decay = 1 - (elapsed / duration)
  → Rung mạnh lúc đầu
  → Giảm dần về 0
```

---

## ✅ Checklist Setup

- [ ] CombatCamera GameObject được tạo
- [ ] Camera component trong Orthographic mode
- [ ] CombatCameraManager script được add
- [ ] Default settings được adjust (size, position)
- [ ] ClashAnimationSequence có Camera Manager reference
- [ ] ClashAnimationSequence.Awake() có tìm camera auto
- [ ] UnitView được update (OnDamageTaken event)
- [ ] CombatManager có OnClashResolved, OnRoundEnded events
- [ ] Test Play mode: Clash animation
- [ ] Test Play mode: Damage effects
- [ ] Test Play mode: Camera reset
- [ ] Adjust zoom sizes để hợp ý

---

## 🎓 Advanced: Custom Camera Motion

### Để thêm effect mới

```csharp
// VD: Zoom ra từng bước khi damage multiple times
public void MultiDamageZoom(int hitCount)
{
    for (int i = 0; i < hitCount; i++)
    {
        yield return new WaitForSeconds(0.1f);
        PlayImpactShake();
        yield return new WaitForSeconds(0.3f);
    }
}

// VD: Pan khi ai đó dùng skill mục tiêu xa
public void PanToTarget(Transform target)
{
    followTarget = target;
    // Follow code sẽ tự handle
}
```

---

**Camera System v1.0 Setup Guide**  
**Last Updated**: March 2026
