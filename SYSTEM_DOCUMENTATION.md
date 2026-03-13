# TỔNG QUAN HỆ THỐNG GAMETN RPG COMBAT

## 📋 Mục lục
1. [Kiến trúc tổng thể](#kiến-trúc-tổng-thể)
2. [Hệ thống chiến đấu](#hệ-thống-chiến-đấu)
3. [Dữ liệu & Assets](#dữ-liệu--assets)
4. [Giao diện & UI](#giao-diện--ui)
5. [Luồng trò chơi](#luồng-trò-chơi)
6. [Hiệu ứng & Skills](#hiệu-ứng--skills)
7. [AI & Bot](#ai--bot)
8. [Animation & Visual](#animation--visual)
9. [Hệ thống Camera](#hệ-thống-camera)

---

## 🏗️ Kiến trúc tổng thể

### Cấu trúc thư mục chính

```
Assets/
  ├── Scripts/
  │   ├── Combat/          # Lõi hệ thống chiến đấu
  │   ├── Data/            # ScriptableObjects & Data structures
  │   ├── UI/              # Giao diện người dùng
  │   ├── Enums/           # Định nghĩa enumerations
  │   ├── StatusEffects/   # Hệ thống buff/debuff
  │   └── TutorialInfo/    # Hướng dẫn & thông tin
  ├── Scenes/              # Scene chơi game
  ├── Prefabs/             # Character prefabs & UI prefabs
  ├── Animations/          # Animation clips & Animator controllers
  ├── Settings/            # Project settings
  └── TextMesh Pro/        # Font assets
```

---

## ⚔️ Hệ thống chiến đấu

### 1. CombatManager (Trung tâm điều phối)

**File**: `Assets/Scripts/Combat/CombatManager.cs`

**Chức năng**:
- Quản lý toàn bộ trận chiến = Singleton pattern
- Khởi tạo các đơn vị (units) từ Formation
- Điều phối các phase chiến đấu thông qua State Machine
- Quản lý danh sách nhân vật của Player & Enemy

**Key Properties**:
```csharp
public static CombatManager Instance { get; private set; }
public List<CombatUnit> PlayerUnits { get; private set; }
public List<CombatUnit> EnemyUnits { get; private set; }
public int CurrentRound { get; private set; }
public CombatPhase CurrentPhase => stateMachine.Current;
```

**Key Methods**:
- `StartCombat(FormationData, EnemyGroupData)` : Bắt đầu trận chiến
- `SpawnUnitViews()` : Tạo visual cho các unit
- `Transition state` : Chuyển trạng thái chiến đấu

**Grid System** (3x3):
```
PLAYER SIDE:                ENEMY SIDE:
[6] [7] [8] ← Row 0 (Back)  [6] [7] [8] ← Row 0 (Back)
[3] [4] [5] ← Row 1 (Mid)   [3] [4] [5] ← Row 1 (Mid)
[0] [1] [2] ← Row 2 (Front) [0] [1] [2] ← Row 2 (Front)

GridRow  = 0: Back | 1: Mid | 2: Front
GridSlot = 0-8 (Row * 3 + Col)
```

### 2. CombatUnit (Đơn vị chiến đấu)

**File**: `Assets/Scripts/Combat/CombatUnit.cs`

**Chức năng**: Đại diện cho một nhân vật trong trận chiến

**Key Properties**:
```csharp
// Identity
public CharacterData Data { get; private set; }
public string UnitName { get; private set; }
public bool IsPlayer { get; private set; }
public int Level { get; private set; }

// Stats
public int MaxHP { get; private set; }
public int CurrentHP { get; private set; }
public int ATK { get; private set; }          // Attack
public int PDEF { get; private set; }         // Physical Defense
public int MDEF { get; private set; }         // Magic Defense
public int Luck { get; private set; }

// Combat State
public SkillData SelectedSkill { get; private set; }
public List<CombatUnit> SelectedTargets { get; private set; }
public int[] SkillCooldowns { get; private set; }
public bool IsAlive => CurrentHP > 0;

// Effects
public ChallengeStack ChallengeStack { get; private set; }
```

**Key Methods**:
- `Initialize(CharacterData, int, bool)` : Khởi tạo unit
- `TakeDamage(int)` : Nhận damage
- `Heal(int)` : Hồi HP
- `AddBuff(StatType, float, int)` : Thêm buff
- `SelectSkill(SkillData, List)` : Chọn skill & target
- `ExecuteSelectedSkill()` : Thực hiện skill
- `TickCooldowns()` : Giảm cooldown skill

### 3. CombatStateMachine (Máy trạng thái)

**File**: `Assets/Scripts/Combat/CombatStateMachine.cs`

**Các Phase**:
```csharp
enum CombatPhase
{
    Init,          // Khởi tạo trận chiến
    EnemyPlan,     // Enemy lên kế hoạch
    PlayerPlan,    // Player chọn skill & target
    RetargetCheck, // Kiểm tra lại mục tiêu
    Execute,       // Thực hiện hành động
    RoundEnd,      // Kết thúc vòng
    Victory,       // Chiến thắng
    Defeat         // Thua cuộc
}
```

**Luồng chuyển trạng thái**:
```
Init 
  ↓
[EnemyPlan → PlayerPlan → RetargetCheck → Execute → RoundEnd]
  ↓ (lặp lại)
  ↓
Victory hoặc Defeat
```

**Events**:
```csharp
event System.Action<CombatPhase, CombatPhase> OnPhaseChanged;
```

### 4. ClashResolver (Xử lý va chạm)

**File**: `Assets/Scripts/Combat/ClashResolver.cs`

**Chức năng**: Giải quyết kết quả va chạm giữa hai unit

**Clash Logic** (Cuộc chiến tay đôi):
1. Cả 2 unit tung xúc xắc (1-6)
2. Ba số = Base Point + Dice + Luck Bonus
3. Ai cao hơn thắng

```csharp
Score = basePoint + dice(1-6) + (luck / 20)

Ví dụ:
Player: basePoint=4, dice=3, luck=20 → 4 + 3 + 1 = 8
Enemy:  basePoint=4, dice=2, luck=10 → 4 + 2 + 0 = 6
→ Player wins!
```

**ClashResult Output**:
```csharp
public class ClashResult
{
    public CombatUnit Winner;
    public CombatUnit Loser;
    public SkillData WinnerSkill;
    public SkillData LoserSkill;
    public int WinnerScore;
    public int LoserScore;
    public ClashVisualData VisualData;  // Để hiển thị UI
}
```

---

## 📦 Dữ liệu & Assets

### 1. CharacterData (Dữ liệu nhân vật)

**File**: `Assets/Scripts/Data/CharacterData.cs`

**Type**: ScriptableObject (Create → RPG → Character)

**Fields**:
```csharp
// Hình ảnh
public GameObject prefab;
public Sprite portrait;
public Sprite battleSprite;

// Thông tin
public string characterName;
public string lore;

// Stats cơ bản (Level 1)
public int baseHP = 100;
public int baseATK = 20;
public int basePDEF = 10;
public int baseMDEF = 10;
public int baseLuck = 10;

// Tăng trưởng mỗi level
public int hpPerLevel = 5;
public int atkPerLevel = 2;
public int pdefPerLevel = 1;
public int mdefPerLevel = 1;
public int luckPerLevel = 1;

// Danh sách skill (tối đa 5)
public SkillData[] skills;
```

**Tính toán Stats**:
```csharp
Stat(level) = BaseStat + (PerLevel * (level - 1))

Ví dụ lv10:
  HP = 100 + 5 * 9 = 145
  ATK = 20 + 2 * 9 = 38
```

### 2. SkillData (Dữ liệu kỹ năng)

**File**: `Assets/Scripts/Data/SkillData.cs`

**Type**: ScriptableObject (Create → RPG → Skill)

**Fields**:
```csharp
// Thông tin
public string skillName;
public string description;
public Sprite icon;

// Loại skill
public SkillType type;              // Clash, Auto, Passive
public TargetType targetType;       // SingleEnemy, AllEnemies, ...

// Clash settings (nếu type = Clash)
public int basePoint = 4;           // Điểm base khi clash

// Đánh số lần
public int hitCount = 1;            // Số lần đánh mỗi hit

// Cooldown
public int cooldown = 0;            // Số vòng phải chờ

// Animation
public string animationTrigger;     // Trigger trong Animator
public GameObject vfxPrefab;        // VFX prefab
public float vfxOffset = 0f;

// Hiệu ứng
public SkillEffect[] effects;       // Mảng effect (damage, heal, buff...)
```

**Skill Types**:
- `Clash`: Skill chiến đấu (va chạm trực tiếp)
- `Auto`: Skill tự động (thiệt tướng cuối vòng)
- `Passive`: Kỹ năng thụ động (luôn hoạt động)

**Target Types**:
- `SingleEnemy`: Một quái
- `SingleAlly`: Một đồng minh
- `AllEnemies`: Tất cả quái
- `AllAllies`: Tất cả đồng minh
- `Self`: Bản thân

### 3. SkillEffect (Hiệu ứng kỹ năng)

**Base Class**: `Assets/Scripts/Data/SkillEffect.cs`

**Abstract Method**:
```csharp
public abstract void Apply(CombatUnit caster, CombatUnit[] targets);
```

**Các Subclass**:

#### DamageEffect
- **File**: `Assets/Scripts/Data/Effects/DamageEffect.cs`
- **Công thức**: `Damage = (ATK * Multiplier * BuffMultiplier) - PDEF/MDEF`
- **Tối thiểu**: 1 damage per hit
- **Fields**:
  ```csharp
  public float multiplier = 1f;
  public DamageType damageType = DamageType.Physical;
  ```

#### HealEffect
- **File**: `Assets/Scripts/Data/Effects/HealEffect.cs`
- **Công thức**: `Heal = MaxHP * HealPercent`
- **Fields**:
  ```csharp
  public float healPercent = 0.3f;  // 30% MaxHP
  ```

#### BuffEffect
- **File**: `Assets/Scripts/Data/Effects/BuffEffect.cs`
- **Công thức**: `StatValue = OriginalValue * Multiplier`
- **Fields**:
  ```csharp
  public StatType stat;             // ATK, PDEF, MDEF...
  public float multiplier = 1.2f;   // 1.2 = +20%
  public int duration = 2;          // Duration (tính round)
  ```

### 4. CharacterData & EnemyGroupData

**FormationData** (Đội hình Player):
- Runtime data (không phải asset)
- Chứa 9 slot được tạo bởi Formation UI
- Pass vào CombatManager để khởi động

**EnemyGroupData** (Đội quái):
- **File**: `Assets/Scripts/Combat/EnemyGroupData.cs`
- **Type**: ScriptableObject (Create → RPG → EnemyGroup)
- **Fields**:
  ```csharp
  [Serializable]
  public class EnemyEntry
  {
      public CharacterData data;
      public int level = 1;
      public int gridSlot = 0;  // 0-8
  }
  public EnemyEntry[] enemies;
  ```

---

## 🎮 Giao diện & UI

### 1. CombatPlanningUI (UI chọn skill)

**File**: `Assets/Scripts/Combat/CombatPlanningUI.cs`

**Chức năng**: Giao diện chính để player chọn skill & target

**Các phần**:

#### Skill Wheel (Bánh xe kỹ năng)
```
Khi click vào nhân vật:
  
  [Skill3]        [Skill5]
  [Skill2]    []  [ULTIMATE]
  [Skill1]        [Skill4]
  
  Trái: Skill 1,2,3
  Phải: Skill 4,5, Ultimate
```

**Properties**:
```csharp
public GameObject skillButtonPrefab;
public GameObject skillButtonUltiPrefab;
public RectTransform leftSkillContainer;
public RectTransform rightSkillContainer;
public float skillColumnOffset = 160f;
public float skillRowSpacing = 70f;

// Color
public Color skillNormalColor;
public Color skillHoverColor;
public Color skillCooldownColor;
public Color skillSelectedColor;
```

#### Action Bar (Thanh hành động)
- Hiển thị các unit đã chọn skill
- Kéo để đổi thứ tự hành động (PlanningOrder)
- Mỗi slot hiển thị: Portrait + Tên skill

**Properties**:
```csharp
public RectTransform actionBarPanel;
public GameObject actionSlotPrefab;
public RectTransform actionSlotContainer;
public Button confirmButton;
public TextMeshProUGUI instructionText;
```

#### Target Selection
- Click skill → Select target mode
- Click enemy/ally để chọn target

### 2. CombatTestUI (Test UI)

**File**: `Assets/Scripts/UI/CombatTestUI.cs`

**Chức năng**: UI debug/test dùng OnGUI

**Setup**:
```csharp
[Header("Player Setup")]
public CharacterData[] playerRoster = new CharacterData[5];
public int[] playerLevels = { 1, 1, 1, 1, 1 };
public int[] playerGridSlots = { 0, 1, 2, 3, 4 };

[Header("Enemy Setup")]
public EnemyGroupData enemyGroup;
```

**OnGUI Display**:
- HP bars của cả 2 bên
- Phase hiện tại & Round counter
- Nút chọn skill (nếu PlayerPlan)
- Nút confirm

### 3. ActionSlotUI (Slot trong Action Bar)

**File**: `Assets/Scripts/Combat/ActionSlotUI.cs`

**Chức năng**: 1 slot trong Action Bar

**Features**:
- Hiển thị portrait nhân vật
- Hiển thị tên skill được chọn
- Draggable để đổi thứ tự
- Ghost indicator khi kéo

**Implements**: `IBeginDragHandler, IDragHandler, IEndDragHandler, IPointerEnterHandler`

---

## 🔄 Luồng trò chơi

### Một vòng chiến đấu hoàn chỉnh

```
1. START COMBAT (CombatManager.StartCombat)
   ├─ Tạo danh sách PlayerUnits & EnemyUnits
   ├─ Spawn UnitViews (visual + animator)
   └─ Fire OnCombatStarted event

2. ENEMY PLAN (EnemyPhase)
   ├─ Mỗi enemy gọi EnemyAI.PlanTurn()
   │  ├─ Chọn skill sẵn sàng (ready)
   │  └─ Chọn target dựa trọng số
   ├─ SelectedSkill & SelectedTargets được set
   └─ Fire OnEnemyPlanDone event

3. PLAYER PLAN (PlayerPhase)
   ├─ Hiển thị CombatPlanningUI
   ├─ Player chọn skill cho từng unit
   ├─ Kéo Action Bar để sắp xếp thứ tự
   └─ Click CONFIRM → SubmitAllPlayerChoices

4. RETARGET CHECK (RetargetCheckPhase)
   ├─ Nếu target chết → chọn target mới
   └─ Sắp xếp lại PlanningOrder

5. EXECUTE (ExecutePhase)
   ├─ Duyệt theo PlanningOrder
   │  ├─ Nếu Clash skill → ClashResolver
   │  │  ├─ Roll dice → Xác định winner/loser
   │  │  ├─ Phát animation (Rush → ClashIdle → KnockBack → Skill)
   │  │  └─ Apply damage/effect của winner
   │  │
   │  ├─ Nếu Auto/Passive → Apply effect ngay
   │  │
   │  └─ Nếu target chết → update UI, trigger OnRoundEnd
   │
   └─ Cập nhật Cooldown

6. ROUND END (RoundEndPhase)
   ├─ Tick cooldown (-1)
   ├─ Kiểm tra victory/defeat
   ├─ Trigger OnRoundEnd event
   └─ Quay lại Phase 2

7. VICTORY / DEFEAT
   ├─ Loot rewards
   ├─ Return to map
   └─ Fire OnVictory / OnDefeat
```

### Phase Transitions
```csharp
// Trong CombatManager
stateMachine.TransitionTo(CombatPhase.EnemyPlan);  // Bắt đầu
stateMachine.TransitionTo(CombatPhase.PlayerPlan); // Sau enemy
stateMachine.TransitionTo(CombatPhase.Execute);    // Sau player confirm
stateMachine.TransitionTo(CombatPhase.RoundEnd);   // Sau execute
```

---

## ⚡ Hiệu ứng & Skills

### Stat Types
```csharp
enum StatType
{
    HP,
    ATK,    // Attack
    PDEF,   // Physical Defense
    MDEF,   // Magic Defense
    Luck
}
```

### Damage Types
```csharp
enum DamageType
{
    Physical,   // Dùng PDEF
    Magical,    // Dùng MDEF
    True        // Vô hiệu hóa armor
}
```

### Skill Effect Trigger
```csharp
enum SkillEffectTrigger
{
    OnUse,      // Khi dùng skill
    OnClashWin, // Khi thắng clash
    OnClashLose,// Khi thua clash
    OnRoundEnd  // Cuối vòng
}
```

### ChallengeStack (Hệ thống stack damage)

**File**: `Assets/Scripts/Combat/StatusEffects/ChallengeStack.cs`

**Chức năng**: Tăng critical chance/damage khi unit bị tấn công

```csharp
public class ChallengeStack
{
    public const int MaxStacks = 20;
    public int Stacks { get; private set; }
    
    public float GetCritRateBonus() => Stacks * 0.05f;      // +5% per stack
    public float GetCritDmgBonus() => Stacks * 0.10f;       // +10% per stack
    
    public void AddStack(int amount = 1) { ... }
    public void Explode() { ... }  // Reset stacks
}
```

---

## 🤖 AI & Bot

### EnemyAI (Trí tuệ nhân tạo kẻ địch)

**File**: `Assets/Scripts/Combat/EnemyAI.cs`

**Target Selection** (Ưu tiên hàng):
```
Front Row:  60%  ← Ưu tiên cao nhất
Mid Row:    25%
Back Row:   15%
```

**Skill Selection**:
1. Lọc danh sách skill đã sẵn sàng (cooldown = 0)
2. Random pick từ list

**Methods**:
```csharp
public void PlanTurn(CombatUnit enemy, List<CombatUnit> playerUnits)
    // Gọi từ EnemyPhase → set SelectedSkill & SelectedTargets

private SkillData ChooseSkill(CombatUnit enemy)
    // Random trong ready skills

private List<CombatUnit> ChooseTargets(...)
    // Dựa vào TargetType & RowWeights

private CombatUnit WeightedRandomTarget(List<CombatUnit> units)
    // Weighted random dựa GridRow
```

---

## 🎬 Animation & Visual

### 1. UnitView (Visual representation)

**File**: `Assets/Scripts/Combat/UnitView.cs`

**Components**:
```csharp
public SpriteRenderer spriteRenderer;
public Animator animator;
public HitEventReceiver hitReceiver;
```

**Key Methods**:
```csharp
public void Setup(CombatUnit unit)
    // Khởi tạo visual

public void SetAnimationTrigger(string triggerName)
    // Set trigger animation

public IEnumerator WaitUntilAnimationDone(string triggerName)
    // Chờ animation hoàn thành
```

**Animation Triggers**:
```
Rush          → Lao vào (Clash)
ClashIdle     → Đứng đối mặt
KnockBack     → Bị đánh lùi
Skill1-5      → Dùng skill
Idle          → Đứng yên
```

### 2. ClashAnimationSequence (Chuỗi animation chạm)

**File**: `Assets/Scripts/Combat/ClashAnimationSequence.cs`

**Thứ tự animation**:
```
1. Rush (2 bên lao vào)
   ├─ Tính midpoint
   ├─ Lerp từ current → target (rushDuration)
   └─ Set ClashIdle

2. ClashVisualController (Hiện xúc xắc)
   ├─ Roll animation cho cả 2
   ├─ Hiện tổng điểm
   └─ Hiện kết quả (WIN/LOSE)

3. KnockBack (Bên thua)
   └─ Unit loser play animation

4. Skill (Bên thắng)
   ├─ Trigger skill animation
   ├─ OnHitFrame → Apply damage
   └─ Chờ animation xong

5. Return (Cả 2 quay lại)
   └─ Lerp về vị trí gốc (returnDuration)

6. Idle (Đứng yên)
```

**Timing Settings**:
```csharp
public float rushDuration = 0.5f;
public float faceOffDistance = 1.2f;
public float postKnockbackWait = 0.3f;
public float postSkillWait = 0.3f;
public float returnDuration = 0.4f;
```

### 3. ClashVisualController (UI xúc xắc)

**File**: `Assets/Scripts/Combat/ClashVisualController.cs`

**Display**:
```
┌──────────────────────┐
│  Player    vs    Enemy │
│   Base: 4          Base: 4 │
│  [?] Dice [?]      │
│   = Score          = Score │
│                          │
│    PLAYER WINS!          │
└──────────────────────┘
```

**Animation**:
1. Roll animation (số nhảy lung tung)
2. Hiện dice result (1-6)
3. Tính tổng score
4. Hiện winner text

---

## 📊 Events & Callbacks

### CombatManager Events

```csharp
public event System.Action OnCombatStarted;
public event System.Action<CombatUnit> OnPlayerUnitPlanning;
public event System.Action<List<CombatUnit>> OnPlayerPlanStarted;
public event System.Action OnEnemyPlanDone;
public event System.Action OnExecuteStarted;
public event System.Action<ClashResult> OnClashResolved;
public event System.Action OnRoundEnded;
public event System.Action OnVictory;
public event System.Action OnDefeat;
```

### CombatUnit Events

```csharp
public event System.Action<int, int> OnDamageTaken;  // (damage, hitIndex)
public event System.Action<int> OnHealed;            // (healAmount)
public event System.Action OnDied;
```

### CombatStateMachine Events

```csharp
public event System.Action<CombatPhase, CombatPhase> OnPhaseChanged;
```

---

## 🛠️ Enums Chính

### SkillType
```csharp
public enum SkillType 
{
    Clash,      // Chiến đấu trực tiếp (clash)
    Auto,       // Tự động (cuối vòng)
    Passive     // Thụ động (luôn hoạt động)
}
```

### DamageType
```csharp
public enum DamageType 
{
    Physical,   // Defence = PDEF
    Magical,    // Defence = MDEF
    True        // Vô hiệu hóa armor
}
```

### TargetType
```csharp
public enum TargetType 
{
    SingleEnemy,    // 1 kẻ địch
    SingleAlly,     // 1 đồng minh
    AllEnemies,     // Tất cả kẻ địch
    AllAllies,      // Tất cả đồng minh
    Self            // Bản thân
}
```

### CombatPhase (đã nêu ở trên)

---

## 🔗 Luồng dữ liệu

```
CharacterData (Asset)
    ├─ Stats & skills
    └─ Battle Sprite & Prefab

FormationData (Runtime)
    ├─ Tạo từ Formation UI
    └─ Chứa 9 slot với CharacterData + Level + GridSlot

CombatManager.StartCombat(Formation, EnemyGroup)
    │
    ├─ Tạo CombatUnit cho mỗi slot
    ├─ Set Stats từ CharacterData
    └─ Spawn UnitViews
    
    CombatUnit
        ├─ Initialize(CharacterData, Level)
        ├─ SelectSkill(SkillData, Targets)
        ├─ ExecuteSelectedSkill()
        │   └─ Duyệt SkillEffect[]
        │       └─ Apply(caster, targets)
        │           ├─ DamageEffect.Apply() → TakeDamage()
        │           ├─ HealEffect.Apply() → Heal()
        │           └─ BuffEffect.Apply() → AddBuff()
        └─ Events (OnDamageTaken, OnHealed, OnDied)

ClashResolver.Resolve(attacker, defender, atkSkill, defSkill)
    ├─ Roll xúc xắc
    ├─ Tính score
    └─ Return ClashResult
        └─ Pass vào ClashAnimationSequence
            └─ PlayFullClashSequence() → Animation + Apply Effect
```

---

## 🎓 Ví dụ luồng thực tế

### Ví dụ 1: Single Clash Attack

```
1. Player Unit "Hero" select Skill "Slash" (Single Enemy)
   → SelectedSkill = SlashSkill
   → SelectedTargets = [Enemy1]

2. Execute Phase:
   → ClashResolver.Resolve(Hero, Enemy1, SlashSkill, Enemy1DefaultSkill)
   → Dice: Hero=4, Enemy1=3
   → Hero wins! Score 6 vs 5

3. Animation:
   → Hero play Rush animation
   → Enemy1 play Rush animation
   → Both play ClashIdle
   → Show Dice UI + Result
   → Enemy1 play KnockBack
   → Hero play "Slash" animation
   → OnHitFrame trigger → Hero.ExecuteSelectedSkill()
   → SlashSkill effect = Damage x1.5
   → Enemy1.TakeDamage(heroATK * 1.5 - enemy1PDEF)
   → Both return to Idle

4. Check:
   → Enemy1.CurrentHP <= 0?
   → If yes → OnDied event → Remove from EnemyUnits
   → Check Victory condition
```

### Ví dụ 2: Heal Spell (Multi-target)

```
1. Ally "Healer" select Skill "Heal All" (AllAllies)
   → SelectedSkill = HealAllSkill
   → SelectedTargets = [Hero, Ally1, Ally2]

2. Execute Phase (không clash):
   → Skip ClashResolver
   → Trigger "HealAll" animation
   → HealAllSkill.Apply(Healer, [Hero, Ally1, Ally2])
   → Mỗi target: HealEffect.Apply()
   → target.Heal(MaxHP * 0.3)

3. Cooldown:
   → Healer.PutOnCooldown(skillIndex)
   → SkillCooldowns[skillIndex] = cooldown value
   → Cuối vòng → TickCooldowns() → Giảm 1
```

---

## ⚙️ Cách mở rộng hệ thống

### Thêm Skill Effect mới

1. Tạo file: `Assets/Scripts/Data/Effects/MyEffect.cs`
2. Inherit từ `SkillEffect`
3. Implement `Apply(CombatUnit, CombatUnit[])`
4. Add menu: `[CreateAssetMenu(menuName = "RPG/Effects/MyEffect")]`
5. Create asset trong Project
6. Kéo vào SkillData.effects[]

### Thêm Skill mới

1. Create menu → RPG → Skill
2. Set Name, Description, Icon
3. Set SkillType, TargetType, BasePoint
4. Kéo HitCount, Cooldown, Animation Trigger
5. Assign VFX prefab (nếu có)
6. Kéo Effects vào mảng
7. Add vào CharacterData.skills[]

### Thêm Character mới

1. Tạo avatar sprite (32x32 px để fit grid)
2. Create → RPG → Character
3. Điền Info: Name, Portrait, BattleSprite
4. Điền Base Stats & Growth
5. Assign Prefab từ Prefabs folder
6. Select 1-5 Skills từ danh sách
7. Tạo FormationData hoặc EnemyGroupData để dùng

---

## 🎥 Hệ thống Camera

### 1. CombatCameraManager (Quản lý camera)

**File**: `Assets/Scripts/Camera/CombatCameraManager.cs`

**Chức năng**: Điều khiển camera trong combat với các hiệu ứng

**Key Features**:
```csharp
// Zoom vào unit
public void ZoomToUnit(Transform unit, float zoomSize)
    → Zoom + follow target

// Clash zoom sequence (rush → shake → zoom out)
public void PlayClashZoom(Transform unit1, Transform unit2)
    → Zoom vào điểm giữa 2 unit
    → Shake effect khi impact

// Shake effect (khi damage/hit)
public void PlayImpactShake()
    → Rung camera ngắn hạn

// Reset camera về default
public void ResetCamera()
    → Zoom out smooth → Trở về vị trí gốc
```

**Inspector Settings** (Tune-able):
```csharp
// Default State
float defaultOrthoSize = 10f;              // Kích cỡ mặc định
Vector3 defaultPosition = Vector3.zero;    // Vị trí mặc định

// Zoom Settings
float clashZoomSize = 6f;                  // Size khi clash (gần sát)
float damageZoomSize = 7f;                 // Size khi damage (vừa phải)
float zoomInDuration = 0.3f;               // Thời gian zoom in (s)
float zoomOutDuration = 0.5f;              // Thời gian zoom out (s)

// Follow Settings
Vector3 followOffset = (0, 0, -5f);       // Offset từ target
float followSmoothness = 0.15f;           // Tốc độ follow (0-1)

// Shake Settings
float shakeIntensity = 0.15f;             // Mức độ rung
float shakeDuration = 0.2f;               // Thời gian rung (s)
float shakeFrequency = 20f;               // Tốc độ rung (Hz)
```

**Events Support**:
- Auto-subscribe vào: `OnClashResolved`, `OnRoundEnded`, `OnVictory`, `OnDefeat`
- Auto-reset khi combat kết thúc

### 2. ClashAnimationSequence + Camera Integration

**Updates**: 
- `PlayFullClashSequence()` bây giờ gọi camera effects
- Phase 1 (Rush): `PlayClashZoom()`
- Phase 4 (Winner Attack): `ZoomToUnit()` + `PlayImpactShake()`
- Phase 5 (Return): `ResetCamera()`

**Code Flow**:
```csharp
public IEnumerator PlayFullClashSequence(...)
{
    // Phase 1: Rush
    if (cameraManager != null)
        cameraManager.PlayClashZoom(playerView.transform, enemyView.transform);
    
    // Phase 4: Winner Attack
    if (cameraManager != null)
    {
        cameraManager.ZoomToUnit(winnerView.transform, ...);
        cameraManager.PlayImpactShake();
    }
    
    // Phase 5: Return
    if (cameraManager != null)
        cameraManager.ResetCamera();
}
```

### 3. UnitView + Camera Integration

**Updates**:
- `OnDamageTaken` event bây giờ trigger camera zoom
- Mỗi hit → auto camera zoom + shake

**Code Flow**:
```csharp
public void Setup(CombatUnit unit)
{
    // Find camera manager
    if (cameraManager == null)
        cameraManager = FindObjectOfType<CombatCameraManager>();
    
    // On damage event
    unit.OnDamageTaken += (dmg, hitIndex) => 
    {
        TriggerHitFlash();
        // Camera effect
        if (cameraManager != null)
        {
            cameraManager.ZoomToUnit(transform, cameraManager.damageZoomSize);
            cameraManager.PlayImpactShake();
        }
    };
}
```

### 4. Timing & Kịch bản

**Clash Sequence Camera Timeline**:
```
0.0s: Rush start
      → PlayClashZoom(unit1, unit2)
      → Zoom in to clashZoomSize (0.3s)
      
0.5s: Both at faceOff
      → Hold zoom + show dice UI
      
0.8s: Dice animation complete
      
1.0s: Loser KnockBack
      → PlayImpactShake()
      
1.3s: Winner attack
      → ZoomToUnit(winner)
      → PlayImpactShake()
      
1.6s: Return sequence
      → ResetCamera()
      → Zoom out to defaultSize (0.5s)
      → Follow: smooth line back to default position
      
2.1s: Complete
```

**Damage-only Camera Timeline**:
```
0.0s: Unit takes damage
      → TakeDamage()
      → OnDamageTaken event
      → ZoomToUnit(damagedUnit, damageZoomSize)
      → PlayImpactShake()
      
0.2s: Shake effect end
      
[Camera persists on damagedUnit until next action]
```

### 5. CombatCameraAnimationIntegration (Optional)

**File**: `Assets/Scripts/Camera/CombatCameraAnimationIntegration.cs`

**Chức năng**: Bridge có thể reuse cho custom integrations

**Methods**:
```csharp
public void OnClashAnimationStart(Transform attacker, Transform defender)
    → Gọi từ ngoài khi clash bắt đầu

public void OnClashWinnerAttack(Transform winner)
    → Gọi khi winner tấn công

public void OnUnitTakeDamage(Transform targetUnit)
    → Gọi khi unit bị damage
```

### 6. Camera Easing Functions

**Smooth animations dùng easing**:
```csharp
// EaseInOutQuad: Smooth start & end
t < 0.5f ? 2*t*t : -1 + (4-2*t)*t

// Dùng cho: Zoom in/out, position lerp
// Kết quả: Animation không bị jump/jerk
```

---



### Để chơi một trận

1. Mở Scene "CombatScene"
2. **Setup Camera** (nếu chưa có):
   - Tạo GameObject "CombatCamera" có Camera component
   - Add script `CombatCameraManager` vào nó
   - Adjust zoom sizes: clashZoomSize=6, damageZoomSize=7
   - Gán Camera Manager reference vào ClashAnimationSequence inspector
3. Setup trong CombatTestUI Inspector:
   - Kéo characters vào Player Roster
   - Chọn Level, GridSlot
   - Kéo EnemyGroupData vào Enemy Group
4. Play → Bắt đầu
5. Click "START COMBAT"
6. Chọn Skill → Click Target → Confirm
7. **Xem camera zoom in/shake khi clash xảy ra!**

### Để thêm trận mới

1. Create → RPG → EnemyGroup (tạo asset mới)
2. Thêm enemies vào mảng
3. Đặt level & grid slot
4. Lưu asset
5. Reference trong scene

---

## 📝 Ghi chú kỹ thuật

- **Singleton**: CombatManager là singleton (chỉ 1 instance)
- **State Machine**: Quản lý phase qua virtual stack
- **Event System**: Loose coupling thông qua events & delegates
- **Coroutines**: Animation sequences dùng coroutines với await
- **ScriptableObject**: Dữ liệu được lưu như SO để reusable
- **Pooling**: Có thể tối ưu pool UnitViews cho performance
- **Grid System**: 3x3 lưới cho positioning & targeting

---

## 📌 To-Do / Improvements

- [x] **Hệ thống Camera** - ✅ DONE (Zoom, Follow, Shake effects)
- [ ] Thêm MP/Resource system
- [ ] Thêm Status Condition (Stun, Silence, etc)
- [ ] Thêm Ability Tree cho characters
- [ ] Thêm Loot system & Equipment
- [ ] Thêm Sound effects & Music
- [ ] Optimize performance (pooling, culling)
- [ ] Thêm Tutorial & Onboarding
- [ ] Balancing stats & difficulty

---

**Game System Documentation v1.0**  
**Last Updated**: March 2026
