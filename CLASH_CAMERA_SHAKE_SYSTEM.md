# 🎥 CLASH CAMERA & SHAKE SYSTEM (v1.0.4)

**Issue**: 
- Camera tidak hold cukup lama saat clash
- Shake effect hanya 1 kali, bukan per-hit

**Fix Applied**: ✅ DONE

---

## 🚀 Changes in v1.0.4

### 1. **Camera Hold Duration Extended**

**File**: `ClashAnimationSequence.cs`

```csharp
// Before
public float postSkillWait = 0.3f;

// After (v1.0.4)
public float postSkillWait = 0.5f;  // +67% longer!
```

**Effect**: Camera zoom in lama hơn 200ms (0.5s) → Player lebih kelihatan aksi

### 2. **Per-Hit Shake System**

**File**: `UnitView.cs` (OnDamageTaken event)

```csharp
unit.OnDamageTaken += (dmg, hitIndex) => 
{
    TriggerHitFlash();
    if (cameraManager != null)
    {
        // MỖI hit → trigger shake!
        cameraManager.PlayImpactShake();  // ← Per-hit shake
    }
};
```

**How it works**:
- Setiap hit dalam multi-hit skill → fire `OnDamageTaken`
- Setiap event call `PlayImpactShake()`
- Multiple shakes run in parallel (blend smoothly)

### 3. **Improved Shake Settings**

```csharp
// Untuk handle multi-hit better
shakeIntensity:  0.15 → 0.20  (stronger)
shakeDuration:   0.2  → 0.25  (longer per-hit)
shakeFrequency:  20   → 22    (slightly faster)
```

---

## 📊 Timeline: Clash dengan Per-Hit Shake

```
Time (s) | Event               | Camera              | Shake
─────────┼─────────────────────┼────────────────────┼──────────
0.0      | Rush starts         | PlayClashZoom      | -
0.3      | Units meet          | Hold zoom (size=6) | -
0.8      | Dice animation      | Hold zoom          | -
1.0      | KnockBack hit       | -                  | ← Shake
1.2      | Winner attack anim  | Stay zoomed        | -
1.25     | HIT #1              | Hold               | ✅ Shake #1
1.35     | HIT #2              | Hold               | ✅ Shake #2
1.45     | HIT #3              | Hold               | ✅ Shake #3
1.6      | Animation ends      | Hold               | -
2.1      | Return start        | Zoom out (0.5s)    | -
2.6      | Complete            | Default view       | -
```

---

## ✅ Expected Behavior

### Before (v1.0.3)
```
Clash animation:
  → Camera zoom in
  → 1x shake at start
  → Camera zoom out quickly
  
❌ Problem: Shake hanya 1 kali, tidak responsive per-hit
```

### After (v1.0.4)
```
Clash animation:
  → Camera zoom in & HOLD (0.5s)
  → Shake #1 pada hit pertama
  → Shake #2 pada hit kedua
  → Shake #3 pada hit ketiga
  → ... (per masing-masing hit)
  → Camera zoom out slowly
  
✅ Result: Dramatic, responsive, full impact!
```

---

## 🧪 Verification

**Play & check**:

```
1. Start clash
   ↓
   ✅ Camera zoom in
   ✅ Stay zoomed during winner attack

2. During damage application
   ↓
   ✅ See shake #1 (first hit)
   ✅ See shake #2 (second hit if exists)
   ✅ etc...
   ✓ Each shake clear & distinct

3. After animation
   ↓
   ✅ Camera zoom out smooth
   ✅ Back to full board
```

---

## 🎨 Fine-Tuning Options

**Jika shake perlu stronger**:
```
shakeIntensity: 0.20 → 0.25-0.30
```

**Jika shake perlu lebih lama**:
```
shakeDuration: 0.25 → 0.30-0.35
```

**Jika camera hold perlu lebih lama**:
```
postSkillWait: 0.5 → 0.6-0.7
```

**Jika per-hit shake overlap/messy**:
```
shakeDuration: 0.25 → 0.15 (shorter, cleaner)
```

---

## 🔗 Code Flow

```
clash → skill animation start
        ↓
    per frame:
    ├─ OnHitFrame triggered by animation event
    ├─ ProcessHitAtFrame called
    ├─ unit.TakeDamage(damage, hitIndex)
    ├─ unit.OnDamageTaken event fired
    └─ UnitView.OnDamageTaken handler:
       └─ cameraManager.PlayImpactShake()  ← HERE!
           
→ Multiple shakes in sequence (per-hit)
→ All running in parallel threads (blend)
```

---

## 📝 Version Changes Summary

| Feature | v1.0.3 | v1.0.4 |
|---------|--------|--------|
| Camera hold time | 0.3s | **0.5s** ↑ |
| Per-hit shake | ❌ No | **✅ Yes** |
| Shake intensity | 0.15 | **0.20** ↑ |
| Shake duration | 0.2s | **0.25s** ↑ |
| Result | Single shake | **Multiple shakes/hit** |

---

## 💡 Design Philosophy

**Why per-hit shake?**
- Multi-hit skills should feel different from single-hit
- Each impact should register visually
- Accumulative shake = build excitement

**Why hold camera longer?**
- Player sees full impact animation
- Shake effects visible & impactful
- Dramatic timing matches action

---

**Version**: 1.0.4  
**Status**: ✅ Production Ready  

Clash camera & shake now feels EPIC! 🎉
