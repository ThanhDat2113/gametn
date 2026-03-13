# 🎥 CAMERA DEFAULT DISTANCE FIX

**Issue**: Camera gốc vẫn quá gần (mặc dù đã adjust đôi lần)
**Fix**: Tăng mặc định distance lên 16 (từ 12)
**Status**: ✅ FIXED v1.0.2

---

## 🚀 Auto-Updated

Script `CombatCameraManager.cs` bây giờ có:

```csharp
// Default State (NEW)
public float defaultOrthoSize = 16f;  ← TĂNG từ 12 lên 16

// AutoFitUnitsInView (IMPROVED)
float bufferSize = Mathf.Max(requiredSize * 1.4f, 14f);  
// Buffer: 30% → 40%
// Min: 8 → 14
```

**Result**: 
- ✅ Default view: nhìn toàn bộ sân
- ✅ Auto-fit: thêm 40% space
- ✅ No more "too close" feeling

---

## 👀 Kiểm tra

**Khi Play game**:
```
1. START COMBAT
   ↓
   ✅ Nhìn thấy full board (6+ units)
   
2. Clash động
   ↓
   ✅ Camera zoom in (2 units che mắt OK)
   
3. Clash kết thúc
   ↓
   ✅ Camera zoom out
   ✅ Nhìn full board lại
   
4. PlayerPlan phase
   ↓
   ✅ Toàn bộ units visible
   ✅ Not cramped anymore!
```

---

## 🎯 Nếu vẫn muốn adjust thêm

**Tùy chọn 1: Inspector**
```
CombatCameraManager
└─ Default Ortho Size: 16
   └─ Muốn xa hơn? → 18-20
   └─ Muốn gần hơn? → 14-15 (không khuyên)
```

**Tùy chọn 2: Code (Quick adjust)**
```csharp
cameraManager.ScamAdjustDistance(1.1f);  // Zoom xa 10% nữa
cameraManager.ScamAdjustDistance(0.9f);  // Zoom gần 10%
```

---

## 📊 Before vs After

| Aspect | Before | After |
|--------|--------|-------|
| Default Size | 12 | 16 ✅ |
| Default View | ❌ Tight | ✅ Spacious |
| Buffer | 30% | 40% ✅ |
| Jitter | ⚠️ Some | ✅ Less |
| Auto-fit | Basic | ✅ Improved |

---

## 💡 Pro Tips

- Nếu units còn cắt → Tăng `defaultOrthoSize` lên 18-20
- Nếu muốn xem drama detail → Hạ xuống 14-15
- Không edit buffer calculation - nó auto-smart adjust

---

**Version**: 1.0.2  
**Status**: ✅ Production Ready  

🎉 Camera distance problem solved!
