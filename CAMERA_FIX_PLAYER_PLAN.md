# 🎥 CAMERA QUICK FIX v2 - Hanya 2 units hiển thị

**Issue**: Khi chọn skill, camera chỉ hiển thị 2 unit đang clash, không thấy toàn bộ

**Penyebab**: Camera tidak reset khi vào PlayerPlan phase

**Status**: ✅ FIXED

---

## 🚀 Fix Applied

### Automatik Reset Camera saat PlayerPlan

```
Clash xảy ra
  ↓
ResetCamera (instant)
  ↓
Vào PlayerPlan phase
  ↓
Detect phase change → Stop zoom coroutines
  ↓
Immediate reset camera + target
  ↓
Wait 0.2s
  ↓
AutoFitUnitsInView (buffer 30%)
  ↓
✅ Nhìn thấy toàn bộ units
```

---

## 📝 Thay đổi Code

### 1. Menambahkan MonitorPhaseChanges

**File**: `CombatCameraManager.cs`

```csharp
private IEnumerator MonitorPhaseChanges()
{
    // Monitor phase changes
    // When entering PlayerPlan:
    //   → Stop zoom coroutines
    //   → Reset camera immediately
    //   → Auto-fit all units
}
```

### 2. Improved ResetCamera

```csharp
public void ResetCamera()
{
    // Immediate reset (tidak wait)
    followTarget = null;
    currentOrthoSize = defaultOrthoSize;
    targetPosition = defaultPosition + new Vector3(0, 0, cameraHeight);
}
```

### 3. Improved AutoFitUnitsInView

```csharp
// Buffer terbaru: 30% (naik dari 20%)
// Min size: 8f (safety check)
float bufferSize = requiredSize * 1.3f;

// Pastikan tidak follow anyone
followTarget = null;
shakeOffset = Vector3.zero;
```

---

## ✅ Testing

**Expected behavior**:

```
1. Play game
2. Click START COMBAT
3. See all units on screen ✓
4. Click to start clash
5. Clash animation (2 units zoom in) ✓
6. Clash ends
7. Back to PlayerPlan phase
8. Camera zooms out → See ALL units again ✓
9. Can select skills normally
```

---

## 📝 Changed Defaults (v1.0.2 - Latest)

**What's updated**:
- ✅ Default Ortho Size: 12 → **16** (tăng, nhìn rộng hơn)
- ✅ Buffer in AutoFit: 30% → **40%** (thêm breathing room)
- ✅ Min buffer: 14f (không quá zoom)
- ✅ New method: `ScamAdjustDistance(factor)` (quick adjust)

**Result**: Camera không còn quá gần! 🎉

---

## 🔍 Debug

Kalau masih tidak kerja, check console log:

```
[CombatCamera] Entered PlayerPlan - Reset camera to view all units
[CombatCamera] Auto-fit: Size=X.XX, Center=(...), Units=N
```

Jika tidak ada log → PlayerPlan phase tidak terdeteksi → Check CombatManager

---

## 📊 Before vs After

| Aspek | Before | After |
|-------|--------|-------|
| Saat Player Plan | ❌ 2 units only | ✅ All units |
| Jitter | ⚠️ Possible | ✅ Fixed |
| Buffer | 20% | 30% (better) |
| Auto-fit delay | 0.3s | 0.2s (faster) |

---

## 🎯 TL;DR

**Masalah**: Camera stuck zoom lalu player skill
**Fix**: Auto-detect PlayerPlan phase → Reset + AutoFit
**Result**: ✅ Lihat semua units saat skill selection

Test sekarang! 🚀
