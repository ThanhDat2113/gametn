# 📋 KẾ HOẠCH IMPLEMENT HỆ THỐNG LEVEL & EXP

**Date**: June 1, 2026  
**Status**: Planning  
**Priority**: High  

---

## 🎯 **MỤC ĐÍCH**

Thêm hệ thống Level & EXP persistent để:
1. Player nhân vật có thể level up sau các trận đấu
2. Quái có thể cho EXP rewards
3. Level được lưu giữa các scene
4. Mỗi character có config EXP curve riêng

---

## 📊 **OVERVIEW - 7 FILES CẦN EDIT/TẠO**

| # | File | Hành động | Mô tả |
|---|------|----------|--------|
| 1 | `Assets/Scripts/Enums/CombatEnums.cs` | ✏️ Edit | Thêm `CharacterType` enum |
| 2 | `Assets/Scripts/Data/CharacterData.cs` | ✏️ Edit | Thêm 5 fields: type, level, exp |
| 3 | `Assets/Scripts/Data/PlayerProgressData.cs` | 📝 Tạo | Progress tracking singleton |
| 4 | `Assets/Scripts/Combat/CombatExperienceManager.cs` | 📝 Tạo | EXP calculation & award |
| 5 | `Assets/Scripts/Combat/CombatManager.cs` | ✏️ Edit | Integrate EXP trong DoVictory |
| 6 | `Assets/Scripts/Managers/GameInitializer.cs` | 📝 Tạo | Auto-create singletons |
| 7 | `Assets/Scripts/Combat/FormationProgressHelper.cs` | 📝 Tạo | Helper to access levels |

---

## 🔧 **PHASE 1: CORE ENUMS & DATA**

### **1.1 Edit CombatEnums.cs**
**Path**: `Assets/Scripts/Enums/CombatEnums.cs`

**Action**: Thêm enum CharacterType vào đầu file

```csharp
public enum CharacterType
{
    Player,     // Nhân vật người chơi (team của ta)
    Enemy       // Quái địch (có thể cho EXP)
}
```

**Reason**: Để phân biệt character là Player hay Enemy

---

### **1.2 Edit CharacterData.cs**
**Path**: `Assets/Scripts/Data/CharacterData.cs`

**Changes**:

#### **1.2.1 Thêm Type & Role header**
Chèn trước `[Header("Behavior")]`:

```csharp
[Header("Type & Role")]
[Tooltip("Chọn loại: Player (nhân vật của ta) hay Enemy (quái)")]
public CharacterType characterType = CharacterType.Player;
```

#### **1.2.2 Thêm Level Settings header**
Chèn trước `[Header("Growth Per Level")]`:

```csharp
[Header("Level Settings")]
[Tooltip("Level mặc định khi character này được tạo")]
public int baseLevel = 1;

[Tooltip("Chỉ dùng nếu characterType = Enemy. EXP mà toàn team nhận khi quái này bị đánh bại")]
public int expReward = 100;

[Header("Level-Up Configuration")]
[Tooltip("EXP cần để lên từ level 1→2")]
public int baseExpThreshold = 100;

[Tooltip("Mỗi level sẽ thêm thêm X EXP cần")]
public int expIncrementPerLevel = 50;
```

**Formula**: 
```
EXP cần để lên Level X = baseExpThreshold + expIncrementPerLevel × (X - 2)

Ví dụ (baseExp=100, increment=50):
  Level 1→2: 100 + 50×(2-2) = 100 EXP
  Level 2→3: 100 + 50×(3-2) = 150 EXP
  Level 3→4: 100 + 50×(4-2) = 200 EXP
  Level 4→5: 100 + 50×(5-2) = 250 EXP
  Level 10→11: 100 + 50×(11-2) = 550 EXP
```

---

## 💾 **PHASE 2: PROGRESS TRACKING SYSTEM**

### **2.1 Tạo PlayerProgressData.cs**
**Path**: `Assets/Scripts/Data/PlayerProgressData.cs` (NEW FILE)

**Nội dung cần có**:

```csharp
/// CharacterProgress class:
/// - Lưu trữ per-character: level, EXP, thresholds
/// - Properties: characterData, currentLevel, currentEXP, levelUpThresholds
/// - Events: OnLevelUp, OnEXPGain
/// - Methods:
///   - Constructor(CharacterData data): Sinh thresholds động
///   - GainEXP(amount): Thêm EXP + check level-up
///   - CheckLevelUp(): Tự động level up nếu đủ EXP
///   - SetLevel(newLevel): Set level trực tiếp (debug/load)
///   - Reset(): Reset lại level 1
///   - GetEXPToNextLevel(): Lấy EXP còn cần

/// PlayerProgressData singleton:
/// - Instance pattern
/// - DontDestroyOnLoad(gameObject)
/// - Dictionary<CharacterData, CharacterProgress>
/// - Methods:
///   - GetOrCreateProgress(characterData): Lấy hoặc tạo mới
///   - SetProgress(characterData, level, exp): Set trực tiếp
///   - ResetAll(): Reset tất cả (khi new game)
///   - ExportProgress(): Export list (để save game)
///   - ImportProgress(data): Import từ file (khi load game)
///   - GetAllProgress(): Lấy tất cả progress dictionary
```

**Responsibility**: Quản lý level & EXP của toàn bộ character, persist giữa scenes

---

## ⚡ **PHASE 3: EXP REWARD SYSTEM**

### **3.1 Tạo CombatExperienceManager.cs**
**Path**: `Assets/Scripts/Combat/CombatExperienceManager.cs` (NEW FILE)

**Nội dung cần có**:

```csharp
/// CombatExperienceManager singleton:
/// - Instance pattern
/// - Methods:
///   - CalculateTotalEXPFromEnemies(List<CombatUnit>):
///     Loop through enemies, check characterType == Enemy
///     Formula: baseEXP + levelBonus (levelBonus = (level-1) * 10)
///     Return total EXP
///   
///   - AwardEXPToPlayers(List<CombatUnit>, int totalEXP):
///     Chia EXP bình đẳng: expPerPlayer = totalEXP / playerCount
///     Gọi progress.GainEXP() cho mỗi player
///   
///   - OnVictory(List<CombatUnit> playerUnits, List<CombatUnit> enemyUnits):
///     Wrapper: tính + phát hành EXP
///   
///   - GetEXPRewardInfo(List<CombatUnit>):
///     Return string display "EXP Reward: XXX"
```

**EXP Calculation**:
```
Per Enemy:
  finalEXP = enemy.Data.expReward + (enemy.Level - 1) * 10

Per Player:
  expPerPlayer = totalEXP / playerUnits.Count
  
Then: player.progress.GainEXP(expPerPlayer)
  → Automatically checks level-up
```

**Responsibility**: Tính toán và phát hành EXP khi combat kết thúc

---

## 🎮 **PHASE 4: COMBAT INTEGRATION**

### **4.1 Edit CombatManager.cs**
**Path**: `Assets/Scripts/Combat/CombatManager.cs`

**Change**: Update `DoVictory()` method

**Before**:
```csharp
private void DoVictory()
{
    Debug.Log("=== VICTORY ===");
    OnVictory?.Invoke();
}
```

**After**:
```csharp
private void DoVictory()
{
    Debug.Log("=== VICTORY ===");
    
    // Award EXP to players
    var expManager = CombatExperienceManager.Instance;
    if (expManager != null)
    {
        expManager.OnVictory(PlayerUnits, EnemyUnits);
    }
    else
    {
        Debug.LogWarning("[CombatManager] CombatExperienceManager not found, EXP not awarded");
    }
    
    OnVictory?.Invoke();
}
```

**Responsibility**: Gọi EXP manager khi combat thắng

---

## 🚀 **PHASE 5: INITIALIZATION**

### **5.1 Tạo GameInitializer.cs**
**Path**: `Assets/Scripts/Managers/GameInitializer.cs` (NEW FILE)

**Nội dung**:
```csharp
/// GameInitializer: MonoBehaviour
/// Trong Awake():
///   - Check if PlayerProgressData.Instance == null
///     → Create new GameObject + AddComponent<PlayerProgressData>()
///   - Check if CombatExperienceManager.Instance == null
///     → Create new GameObject + AddComponent<CombatExperienceManager>()
///
/// Usage: Gắn script này vào bất kỳ GameObject nào trong scene đầu tiên
///        (VD: Main Menu hoặc Intro scene)
```

**Responsibility**: Đảm bảo 2 singletons luôn tồn tại

---

## 🛠️ **PHASE 6: HELPER METHODS**

### **6.1 Tạo FormationProgressHelper.cs**
**Path**: `Assets/Scripts/Combat/FormationProgressHelper.cs` (NEW FILE)

**Nội dung**:
```csharp
/// FormationProgressHelper: static helper class
/// Methods:
///   - GetCurrentLevel(CharacterData):
///     Lấy từ PlayerProgressData.Instance
///     Fallback về characterData.baseLevel nếu progress == null
///   
///   - SetLevel(CharacterData, int level):
///     Set level trực tiếp (debug/testing)
///   
///   - ResetLevel(CharacterData):
///     Reset về characterData.baseLevel
```

**Responsibility**: Dễ dàng access level từ bất kỳ đâu

---

## 📈 **EXP FORMULA SUMMARY**

### **Level-Up Thresholds (Động)**
```
baseExpThreshold = 100 (default)
expIncrementPerLevel = 50 (default)

EXP cần Level X = baseExpThreshold + expIncrementPerLevel × (X - 2)

Examples:
  Lv1→2:  100 + 50×0 = 100
  Lv2→3:  100 + 50×1 = 150
  Lv3→4:  100 + 50×2 = 200
  Lv4→5:  100 + 50×3 = 250
  Lv5→6:  100 + 50×4 = 300
  Lv10→11: 100 + 50×9 = 550
  Lv20→21: 100 + 50×19 = 1050
  Lv50→51: 100 + 50×49 = 2550
```

### **EXP Rewards**
```
Per Enemy Defeated:
  baseEXP = characterData.expReward
  levelBonus = (enemy.Level - 1) * 10
  finalEXP = baseEXP + levelBonus

Example:
  Goblin (Lv1, expReward=100) → 100 + 0 = 100 EXP
  Boss (Lv5, expReward=100) → 100 + 40 = 140 EXP
  Archboss (Lv10, expReward=200) → 200 + 90 = 290 EXP

Team Split (5 players defeat 3 enemies):
  Total = 100 + 100 + 100 = 300 EXP
  Per Player = 300 / 5 = 60 EXP each
```

---

## 🔄 **COMPLETE FLOW**

```
┌─ Game Start ─────────────────────────────────────┐
│ 1. GameInitializer.Awake()                       │
│    ├→ Create PlayerProgressData singleton       │
│    └→ Create CombatExperienceManager singleton   │
└──────────────┬─────────────────────────────────┘
               │
┌──────────────▼─────────────────────────────────┐
│ 2. Player Opens Formation Menu                  │
│ FormationManager.RefreshCharacterList()        │
│   └→ Use FormationProgressHelper.GetCurrentLevel() │
│      to show character levels                   │
└──────────────┬─────────────────────────────────┘
               │
┌──────────────▼─────────────────────────────────┐
│ 3. Player Enters Combat                         │
│ CombatManager.StartCombat()                    │
│   └→ Spawn units with levels from PlayerProgressData │
└──────────────┬─────────────────────────────────┘
               │
┌──────────────▼─────────────────────────────────┐
│ 4. Battle Happens...                            │
└──────────────┬─────────────────────────────────┘
               │
┌──────────────▼─────────────────────────────────┐
│ 5. Player Wins!                                 │
│ CombatManager.DoVictory()                      │
│   └→ CombatExperienceManager.OnVictory()       │
│      ├→ CalculateTotalEXPFromEnemies()         │
│      ├→ AwardEXPToPlayers()                    │
│      └→ CharacterProgress.GainEXP()            │
│         └→ CheckLevelUp()                      │
│            └→ OnLevelUp event (if lv-up!)      │
└──────────────┬─────────────────────────────────┘
               │
┌──────────────▼─────────────────────────────────┐
│ 6. Return to Map                                │
│ Levels persist! (DontDestroyOnLoad)            │
│ Next battle: Characters use saved levels       │
└──────────────────────────────────────────────┘
```

---

## ✅ **IMPLEMENTATION CHECKLIST**

### **PHASE 1: Core Data (Easy)**
- [ ] 1.1 Edit `CombatEnums.cs` - Add CharacterType enum
- [ ] 1.2 Edit `CharacterData.cs` - Add 5 new fields

### **PHASE 2: Progress System (Medium)**
- [ ] 2.1 Create `PlayerProgressData.cs` - CharacterProgress + Singleton

### **PHASE 3: EXP System (Easy-Medium)**
- [ ] 3.1 Create `CombatExperienceManager.cs` - EXP calc & award

### **PHASE 4: Integration (Easy)**
- [ ] 4.1 Edit `CombatManager.cs` - Call EXP in DoVictory()

### **PHASE 5: Init (Easy)**
- [ ] 5.1 Create `GameInitializer.cs` - Auto-create singletons

### **PHASE 6: Helpers (Easy)**
- [ ] 6.1 Create `FormationProgressHelper.cs` - Helper methods

### **TESTING**
- [ ] Test: Add character → Set level → Battle → Check level-up
- [ ] Test: Multiple enemies → EXP distribution
- [ ] Test: Persistence → Level survives scene change

---

## 🎮 **TEST SCENARIOS**

### **Test 1: Basic Level-Up**
```
Setup: Aleus (Player) Lv.1 vs Goblin (Enemy) Lv.1, expReward=100
Battle: Aleus defeats Goblin
Result:
  - Aleus gains 100 EXP (100 + 0)
  - Total EXP = 100, threshold = 100 → LEVEL UP to Lv.2!
  - OnLevelUp event triggered
```

### **Test 2: Higher Level Enemy**
```
Setup: Aleus (Player) Lv.1 vs Boss (Enemy) Lv.5, expReward=100
Battle: Aleus defeats Boss
Result:
  - Aleus gains 140 EXP (100 + (5-1)*10)
  - Total EXP = 140, threshold = 100 → LEVEL UP to Lv.2!
  - Remaining EXP = 40/150
```

### **Test 3: Team EXP Distribution**
```
Setup: 
  - Team: Aleus, Lucia, Sophy (3 players)
  - Enemies: Goblin (100), Goblin (100), Wraith (150)
Battle: All defeated
Calculation:
  - Total EXP = 100 + 100 + 150 = 350
  - Per Player = 350 / 3 = 116.67 → 116 EXP each
Result:
  - Each player gains 116 EXP
```

### **Test 4: Level Persistence**
```
Setup: Aleus Lv.2 (after earlier battles)
Action: Return to map → Open formation UI → Re-enter combat
Result:
  - Aleus displays as Lv.2 in UI
  - Battle: Aleus spawns with Lv.2 stats
  - Level persists! ✅
```

### **Test 5: New Game Reset**
```
Action: New Game → PlayerProgressData.ResetAll()
Result:
  - All characters back to baseLevel
  - All EXP reset to 0
```

---

## 🔧 **CONFIG EXAMPLES**

### **Player Character (Default)**
```
characterType: Player
baseLevel: 1
expReward: 0 (không dùng)
baseExpThreshold: 100
expIncrementPerLevel: 50
```

### **Weak Enemy**
```
characterType: Enemy
baseLevel: 1
expReward: 50
baseExpThreshold: 100 (không dùng)
expIncrementPerLevel: 50 (không dùng)
```

### **Strong Boss**
```
characterType: Enemy
baseLevel: 10
expReward: 500
baseExpThreshold: 100 (không dùng)
expIncrementPerLevel: 50 (không dùng)
```

### **Easy Leveling Character** (NPC)
```
characterType: Player
baseLevel: 1
expReward: 0
baseExpThreshold: 50 (ít cần)
expIncrementPerLevel: 10 (tăng chậm)
```

---

## 📝 **IMPLEMENTATION NOTES**

1. **CharacterProgress sinh thresholds trong Constructor**
   - Loop từ level 1→100
   - Mỗi iteration: `threshold = baseExpThreshold + increment * (lv - 2)`
   - Lưu vào `List<int> levelUpThresholds`

2. **GainEXP tự động check level-up**
   - Thêm EXP
   - Gọi `CheckLevelUp()`
   - While loop check từng level
   - Fire `OnLevelUp` event mỗi khi level-up

3. **CombatManager.DoVictory thêm 5 dòng**
   - Get instance
   - Check null
   - Call `OnVictory()`
   - Log if null

4. **FormationProgressHelper static**
   - Không cần instance
   - Direct access `PlayerProgressData.Instance`
   - Fallback an toàn

5. **GameInitializer gắn vào scene đầu**
   - Main Menu hoặc Intro scene
   - Đảm bảo singletons tồn tại
   - DontDestroyOnLoad xử lý phần còn lại

---

## 🚀 **FUTURE ENHANCEMENTS**

- [ ] UI: Level-up popup/animation
- [ ] UI: Victory screen with EXP rewards
- [ ] UI: Character panel with EXP bar
- [ ] Save/Load: Persist levels in save file
- [ ] Balance: Dynamic enemy level scaling
- [ ] Events: OnLevelUp global event
- [ ] Notification: Floating "LEVEL UP!" text

---

## 📅 **TIMELINE**

- **Phase 1 (EDIT 2 files)**: 5 min
- **Phase 2 (CREATE 1 file)**: 10 min
- **Phase 3 (CREATE 1 file)**: 10 min
- **Phase 4 (EDIT 1 file)**: 2 min
- **Phase 5 (CREATE 1 file)**: 5 min
- **Phase 6 (CREATE 1 file)**: 3 min
- **Testing**: 10 min

**Total**: ~45 minutes

---

## 📞 **QUESTIONS ANSWERED**

**Q: Mỗi level cần EXP tăng không?**  
A: ✅ Yes - `EXP = baseExpThreshold + expIncrementPerLevel × (level - 2)`

**Q: Quái có thể cho EXP?**  
A: ✅ Yes - Khi `characterType == Enemy` và `expReward > 0`

**Q: Level persist giữa scenes?**  
A: ✅ Yes - `PlayerProgressData` có `DontDestroyOnLoad`

**Q: Có thể customize per-character?**  
A: ✅ Yes - Mỗi character có thể set `baseExpThreshold` & `expIncrementPerLevel` riêng

**Q: Cách test?**  
A: ✅ Xem TEST SCENARIOS section trên

---

**Status**: Ready for implementation ✅
