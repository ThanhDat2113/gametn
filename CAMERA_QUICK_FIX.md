# 🎥 CAMERA QUICK FIX CHEATSHEET

## ⚡ Vấn đề camera quá zoom / không thấy units

### 🚀 Fix #1 - Ngay lập tức (1 phút)

**Mở CombatCameraManager dan thay:**

```
❌ Cũ:
  Default Ortho Size: 10
  Clash Zoom Size: 6
  Damage Zoom Size: 7
  Follow Smoothness: 0.15

✅ Baru (v1.0.2):
  Default Ortho Size: 16      ← TĂNG LÊN (nhìn rộng hơn)
  Clash Zoom Size: 7
  Damage Zoom Size: 8
  Follow Smoothness: 0.10
```

**Rồi Play & Test** → Phải khác hẳn! 

---

## 🔧 Fix #2 - Nếu vẫn không OK

### Nếu vẫn thấy jitter/nhấp nháy:

```
Default Ortho Size: 15      ← Tăng lên 15
Follow Smoothness: 0.08     ← Giảm xuống 0.08
```

### Nếu vẫn không thấy units (đen toàn mặn):

```
Thêm dòng này vào CombatTestUI.cs Start():

    private void Start()
    {
        // ... your code ...
        
        var cameraManager = FindObjectOfType<CombatCameraManager>();
        if (cameraManager != null)
            cameraManager.AutoFitUnitsInView();  // ← Add line này
        
        combat = CombatManager.Instance;
        // ... rest ...
    }
```

---

## 📊 Size Reference Chart

```
DefaultSize  | Clear View? | Feel
─────────────┼─────────────┼─────────────
12           | ⚠️ Tight    | Original (too close)
14-15        | 😐 OK       | Borderline
16           | ✅ Tốt      | Recommended ← DEFAULT NOW
17-19        | ✅ Rộng     | Very safe
20+          | ⚠️ Far      | Can see multiple groups

ClashSize    | Intensity   | Feel
─────────────┼─────────────┼─────────────
5            | 💥 Very hot | Too close
6            | 🔥 Hot      | Original
7            | ✅ Balanced | Recommended ← DEFAULT NOW
8-9          | 😌 Chill    | More safe
```

---

## ✅ Testing Flow

```
1. Change values in Inspector
   ↓
2. Play game
   ↓
3. Click START COMBAT
   ↓
4. See units? 
   ├─ YES ✅ → Click to start clash
   │          → Check if zoom works
   │
   └─ NO ❌ → Try Fix #2 above
```

---

## 🎬 What Should Happen

**Before clash:**
- Thấy tất cả units trên sân
- Không thấy jitter/blinky

**During clash:**
- Camera zoom in sát vào 2 unit
- Có thể không thấy hết units (bình thường)
- Camera rung (impact shake)

**After clash:**
- Camera zoom out mượt
- Quay lại nhìn full sân

---

## 🚨 If Still Not Working

Check list:

- [ ] CombatCamera có Camera component không? (phải có)
- [ ] CombatCameraManager script được add không? (phải có)
- [ ] Camera Mode = Orthographic không? (phải là ortho)
- [ ] Units có Z > camera height không? (units phải ở Z=0)
- [ ] Scene có CombatManager GameObject không? (phải có)
- [ ] Play mode, units có spawn không? (check console log)

**Still stuck?**
→ Add Debug log:
```csharp
void Start()
{
    var units = FindObjectsOfType<UnitView>();
    Debug.Log($"Found {units.Length} units in scene");
    
    var cam = FindObjectOfType<CombatCameraManager>();
    Debug.Log($"CombatCamera manager: {cam}");
}
```

---

## 📝 Changed Defaults (v1.0.1)

**What's new:**
- ✅ Default Ortho Size: 10 → **12** (thêm breathing room)
- ✅ Clash Zoom Size: 6 → **7** (tránh quá zoom)
- ✅ Damage Zoom Size: 7 → **8** (keep ratio)
- ✅ Follow Smoothness: 0.15 → **0.10** (tránh jitter)
- ✅ New method: `AutoFitUnitsInView()` (auto-fix khi cần)

**Why:**
- Users reported "camera too close, can't see units"
- Smoother follow = less jitter
- Auto-fit = instant fix without manual tweaking

---

## 🎯 TL;DR (Quá dài, bỏ qua)

**Camera quá zoom?**

1. Update CombatCameraManager:
   - Default Ortho Size = 12
   - Clash Zoom Size = 7
   - Follow Smoothness = 0.10

2. Play & Test

3. Still broken?
   ```
   cameraManager.AutoFitUnitsInView();
   ```

✅ Done!
