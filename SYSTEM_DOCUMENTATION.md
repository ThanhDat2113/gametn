# Hệ Thống Game - Tài Liệu Toàn Diện

## Tổng Quan Kiến Trúc

Game là một **turn-based RPG** được phát triển trên Unity, với cấu trúc mã nguồn chính nằm trong hai thư mục:
- `Assets/_Project/Scripts/` - Mã nguồn chính của game (hệ thống mới)
- `Assets/Scripts/` - Mã nguồn cũ/cổ điển (đang được chuyển đổi dần)

---

## MỤC LỤC

1. [Core Systems](#1-core-systems)
2. [Combat System](#2-combat-system)
3. [Data System](#3-data-system)
4. [Character & Skills System](#4-character--skills-system)
5. [Map & Exploration System](#5-map--exploration-system)
6. [Dialogue System](#6-dialogue-system)
7. [Quest System](#7-quest-system)
8. [Inventory & Equipment System](#8-inventory--equipment-system)
9. [Audio System](#9-audio-system)
10. [UI System](#10-ui-system)
11. [Enums & Constants](#11-enums--constants)
12. [Legacy Scripts (Assets/Scripts)](#12-legacy-scripts)
13. [Event Flow Diagrams](#13-event-flow-diagrams)

---

## 1. Core Systems

### 1.1 EventManager (`Core/EventManager.cs`)
**Singleton** cung cấp event bus pub/sub trung tâm.

```csharp
public class EventManager : MonoBehaviour
```

**Fields:**
- `Instance` (static) - Singleton instance
- `eventListeners` (Dictionary<EventType, List<Action<GameEvent>>>) - Bản đồ sự kiện

**Methods:**
| Method | Description |
|---|---|
| `Awake()` | Thiết lập singleton, DontDestroyOnLoad |
| `RegisterListener(EventType, Action<GameEvent>)` | Đăng ký listener cho event type |
| `UnregisterListener(EventType, Action<GameEvent>)` | Hủy đăng ký listener |
| `TriggerEvent(GameEvent)` | Kích hoạt tất cả listeners của event type |

### 1.2 GameEvent (`Core/GameEvent.cs`)
**ScriptableObject** đại diện cho một sự kiện trong game.

```csharp
public class GameEvent : ScriptableObject
```

**Fields:** description, eventType, targetID, intValue, stringValue, floatValue

**Event Types:**
| Enum Value | Description |
|---|---|
| `None` | Không có |
| `SpawnEnemy` | Sinh quái |
| `UnlockZone` | Mở khóa vùng |
| `GiveItem` | Trao item |
| `SetFlag` | Đặt cờ |
| `StartCombat` | Bắt đầu combat |
| `Teleport` | Dich chuyển |
| `PlayCutscene` | Phát cutscene |
| `AddQuest` | Thêm nhiệm vụ |
| `CompleteQuest` | Hoàn thành nhiệm vụ |

### 1.3 FadeController (`Core/FadeController.cs`)
**Singleton** quản lý hiệu ứng fade (fade to black / fade from black).

```csharp
public class FadeController : MonoBehaviour
```

**Methods:**
| Method | Description |
|---|---|
| `FadeToBlack(Action onComplete)` | Fade dần sang đen, chặn click |
| `FadeFromBlack(Action onComplete)` | Fade dần từ đen ra, bỏ chặn click |
| `SetAlpha(float)` | Set alpha trực tiếp cho fadeImage |

### 1.4 GameInitializer (`Core/GameInitializer.cs`)
Khởi tạo game ở Boot Scene: tạo singleton, load Persistent Scene, unload Boot Scene.

```csharp
public class GameInitializer : MonoBehaviour
```

**Chức năng:**
- Tự động tạo `PlayerProgression`, `SceneLoaderManager`, `AudioManager` nếu chưa có
- Load Persistent Scene additive
- Unload Boot Scene sau khi hoàn tất

### 1.5 PlayerProgressData (`Core/PlayerProgressData.cs`)
Quản lý level & EXP cho từng nhân vật.

**Class `CharacterProgress`:**
| Method | Description |
|---|---|
| `GainEXP(int)` | Nhận EXP, tự động kiểm tra level-up |
| `GetEXPToNextLevel()` | Lấy EXP cần để lên cấp |
| `SetLevel(int, int)` | Set level & EXP trực tiếp |
| `Reset()` | Reset tiến trình |

**Class `PlayerProgressData` (Singleton):**
| Method | Description |
|---|---|
| `GetOrCreateProgress(CharacterData)` | Lấy/tạo tiến trình cho nhân vật |
| `ResetAll()` | Reset tất cả |

### 1.6 PlayerProgression (`Core/PlayerProgression.cs`)
**Singleton** quản lý cấp độ & kinh nghiệm cho team.

```csharp
public class PlayerProgression : MonoBehaviour
```

**Key Methods:**
| Method | Description |
|---|---|
| `GetAveragePartyLevel()` | Tính level trung bình của party |
| `SetLevel(CharacterData, int)` | Set level cho nhân vật |
| `GetLevel(CharacterData)` | Lấy level nhân vật (mặc định baseLevel) |
| `AddExperience(CharacterData, int)` | Cộng EXP cho 1 nhân vật |
| `AddPartyExperience(int)` | Cộng EXP đồng đều cho toàn party |
| `SaveProgress()` / `LoadProgress()` | Lưu/tải tiến trình qua PlayerPrefs |

**Events:**
- `OnCharacterLevelUp(CharacterData, int)` - Khi lên cấp
- `OnExperienceGained(CharacterData, int)` - Khi nhận EXP

### 1.7 SceneLoader (`Core/SceneLoader.cs`)
Static class đơn giản lưu tên scene cần load.

### 1.8 SceneLoaderManager (`Core/SceneLoaderManager.cs`)
**Singleton** quản lý load/unload CombatScene additive.

```csharp
public class SceneLoaderManager : MonoBehaviour
```

**Methods:**
| Method | Description |
|---|---|
| `LoadCombatScene()` | Load CombatScene additive, set active |
| `UnloadCombatScene()` | Unload CombatScene, reactivate MapRoot & PersistentContainer |
| `ReloadCombatScene()` | Unload rồi load lại CombatScene (retry) |

**Static Fields:**
- `MapRoot` - GameObject gốc của map
- `PersistentContainer` - Container persistent objects

---

## 2. Combat System

### 2.1 CombatStateMachine (`Combat/CombatStateMachine.cs`)
**State Machine** quản lý phase combat.

```csharp
public enum CombatPhase { None, Intro, PlayerTurn, EnemyTurn, Victory, Defeat }
```

**Transition events:** `OnPhaseChanged(CombatPhase prev, CombatPhase next)`

### 2.2 CombatManager (`Combat/CombatManager.cs`)
**Singleton** - Trung tâm điều khiển combat.

```csharp
public class CombatManager : MonoBehaviour
```

**Core Fields:**
| Field | Type | Description |
|---|---|---|
| `PlayerUnits` | List\<CombatUnit\> | Danh sách player units |
| `EnemyUnits` | List\<CombatUnit\> | Danh sách enemy units |
| `CurrentPlayerAP` | int | AP hiện tại của player (shared pool) |
| `playerGridSlots` | Transform[] | Grid slots cho player (3x3) |
| `enemyGridSlots` | Transform[] | Grid slots cho enemy (3x3) |

**AP System:**
- Max: 5 AP
- Starting: 3 AP
- Reset mỗi đầu lượt Player

**Combat Flow:**
```
Intro → PlayerTurn (loop: select unit → skill → target → resolve) → EnemyTurn → (loop back) → Victory/Defeat
```

**Key Methods:**
| Method | Description |
|---|---|
| `StartCombat(FormationData, EnemyGroupData)` | Khởi tạo combat với formation & enemy group |
| `SubmitPlayerAction(CombatUnit, SkillData, List<CombatUnit>)` | UI gọi khi player chọn action |
| `EndPlayerTurn()` | UI gọi khi player bấm End Turn |
| `GrantExtraAction(CombatUnit)` | Cho unit hành động thêm |
| `SpendPlayerAP(int)` / `GainPlayerAP(int)` | Quản lý AP |

**Events:**
- `OnCombatStarted`, `OnPlayerTurnStart`, `OnUnitTurnStart`
- `OnPlayerTurnEnd`, `OnEnemyTurnStart`, `OnEnemyTurnEnd`
- `OnActionResolved(ActionResult)`
- `OnVictory(Dictionary<CharacterData, int>)` - EXP rewards
- `OnDefeat`
- `OnAPChanged(int)`
- `OnDamageCalculation(ActionOutcome, CombatUnit)`

### 2.3 CombatUnit (`Combat/CombatUnit.cs`)
Đại diện cho một unit trong combat (player hoặc enemy).

```csharp
public class CombatUnit
```

**Stats:**
| Field | Description |
|---|---|
| `MaxHP`, `CurrentHP` | Máu |
| `ATK` | Tấn công |
| `PDEF` | Phòng thủ vật lý |
| `MDEF` | Phòng thủ phép |
| `CritChance` | Tỉ lệ crit (0-1) |
| `CritDamage` | Sát thương crit (mặc định 1.5x) |
| `ArmorPenetration` | Xuyên giáp |

**Flags:** `IgnoreTaunt`, `HasActedThisTurn`

**Grid System:** `GridRow` (0=Back, 1=Mid, 2=Front), `GridSlot` (0-8)

**Status & Buff System:**
| Method | Description |
|---|---|
| `ApplyBuff(StatType, float, int)` | Buff chỉ số (ATK x1.5, 2 lượt) |
| `ApplyStatus(StatusEffectType, int, float, int)` | Apply trạng thái |
| `GetActiveStatus(StatusEffectType)` | Lấy trạng thái đang active |
| `ClearStatus(StatusEffectType)` | Xóa trạng thái |
| `TickStatuses()` | Giảm duration mỗi lượt |
| `GetDamageMultiplier()` | Tính hệ số nhân damage (SiêuViệt, BụiSao, ÝChí) |
| `GetDamageTakenMultiplier()` | Hệ số nhân damage nhận vào (ĐiểmYếu) |
| `GetDamageReductionMultiplier()` | Giảm sát thương (GiảmSátThương) |
| `GetEmpowerMultiplier()` | Hệ số Empowered (tiêu thụ sau khi tấn công) |

**Damage & Healing:**
| Method | Description |
|---|---|
| `TakeDamage(CombatUnit, int, bool)` | Nhận sát thương (isTrueDamage bỏ qua giảm trừ) |
| `Heal(int)` | Hồi máu |
| `RecalculateStatsForLevel(int)` | Tính lại stats khi lên cấp |

**Damage Reduction Charges:**
- `AddDamageReductionCharges(int, float)` - Thêm lớp giáp giảm % damage

**Reflect Damage:** `StatusEffectType.ReflectDamage` phản % sát thương nhận vào

**Subclasses:**
| Class | Description |
|---|---|
| `ActiveBuff` | Lưu StatType, Multiplier, Duration |
| `ActiveStatus` | Lưu StatusEffectType, Duration, Value, Stacks |

### 2.4 ActionResolver (`Combat/ClashResolver.cs`)
Tính toán sát thương và lưu vào ActionOutcomes.

```csharp
public class ActionResolver
```

**Class `ActionResult`:**
| Field | Description |
|---|---|
| `Actor` | Unit thực hiện |
| `Skill` | Skill sử dụng |
| `InitialTargets` | Danh sách target ban đầu |
| `Outcomes` | List\<ActionOutcome\> kết quả cho từng target |

**Class `ActionOutcome`:**
| Field | Description |
|---|---|
| `Target` | Target nhận sát thương |
| `Damage` | Lượng sát thương |
| `EmpowerMultiplier` | Hệ số Empowered |

**Resolution Logic:**
1. Lấy `empowerMultiplier` từ Empowered stacks
2. Nếu skill có `DamageEffect`: tính tổng damage từ tất cả DamageEffect
3. Nếu skill không có effects: fallback `ATK - PDEF`
4. Gọi `OnDamageCalculation` hook
5. Tiêu thụ Empowered stacks sau khi tính

### 2.5 PlannedAction (`Combat/CombatManager.cs`)
```csharp
public class PlannedAction
{
    public CombatUnit Caster { get; }
    public SkillData Skill { get; }
    public List<CombatUnit> Targets { get; }
}
```

### 2.6 UnitView (`Combat/UnitView.cs`)
**MonoBehaviour** - View đại diện trực quan cho CombatUnit (sprite, animator, health bar).

```csharp
public class UnitView : MonoBehaviour
```

**Key Methods:**
| Method | Description |
|---|---|
| `Setup(CombatUnit)` | Gán unit data, thiết lập sprite/animator/health bar |
| `SetAnimationTrigger(string)` | Kích hoạt trigger animation |
| `PlayAnimation(string)` | Play animation state |
| `SetPendingOutcomes(...)` | Lưu outcomes chờ xử lý theo hit frame |
| `FlushPendingOutcomes()` | Fallback xử lý damage khi không có animation |
| `ProcessHitAtFrame(int)` | Animation event - xử lý damage theo hit index |
| `ProcessVFXAtFrame(int)` | Animation event - spawn VFX |
| `UpdateHealthBar()` | Cập nhật thanh máu |
| `TriggerHitFlash()` | Hiệu ứng flash đỏ khi bị hit |
| `TriggerHealFlash()` | Hiệu ứng flash xanh khi được heal |
| `DeathFade()` | Hiệu ứng fade out khi chết |

**Animation Events:**
- `OnHitAnimationEvent` - Khi animation hit frame
- `OnAnimationEndEvent` - Khi animation kết thúc

### 2.7 ClashAnimationSequence (`Combat/ClashAnimationSequence.cs`)
**MonoBehaviour** - Điều phối toàn bộ visual sequence cho skill action.

**Chức năng:**
- Setup: dim các unit không liên quan, camera zoom
- Approach: rush đến target
- Execute: play animation, spawn VFX, xử lý hit events
- Return: quay về vị trí gốc
- Cleanup: thông báo kết thúc

### 2.8 ClashVisualController (`Combat/ClashVisualController.cs`)
Quản lý các hiệu ứng visual cho clash (nếu có).

### 2.9 CombatCameraManager (`Combat/CombatCameraManager.cs`)
**MonoBehaviour** - Quản lý camera trong combat.

**Methods:**
| Method | Description |
|---|---|
| `BeginIntroSequence()` / `EndIntroSequence()` | Bắt đầu/kết thúc intro |
| `FadeInAndSetPosition(Vector3, float, Vector3, float)` | Fade in + set vị trí |
| `ZoomOutToFinalView(float)` | Zoom ra view cuối cùng |
| `ZoomToUnit(Transform, float)` | Zoom đến unit cụ thể |
| `PlayImpactShake()` | Rung màn hình khi impact |
| `PlayPlayerImpactEffect(Transform)` | Effect impact player |

### 2.10 AI System (`Combat/AI/`)

#### EnemyAI (`Combat/AI/EnemyAI.cs`)
**Class `EnemyAI`:**
```csharp
public class EnemyAI
{
    public void PlanTurn(CombatUnit enemy, List<CombatUnit> playerUnits)
}
```

**Chiến thuật hành động:**
1. Lọc skill có thể dùng (đủ AP, skill chủ động)
2. Ưu tiên skill buff ATK nếu enemy có ATK thấp hơn target
3. Chọn target dựa trên weighted random theo grid rows:
   - Front row: 60% trọng số
   - Mid row: 25% trọng số
   - Back row: 15% trọng số
4. Nếu player có unit đang taunt: ưu tiên target unit đó
5. Nếu skill AoE: chọn target ngẫu nhiên
6. Nếu Single: chọn player còn sống hoặc tank khi có taunt
7. Self-debuff handling từ `StatusEffectType.SelfDamage` hoặc skill không damage

#### Behavior Tree (`Combat/AI/BehaviorTree/`)
| File | Class | Description |
|---|---|---|
| `Node.cs` | `Node` (abstract) | Base class với `NodeState` enum (Running/Success/Failure), abstract `Evaluate()` |
| `Selector.cs` | `Selector` | OR logic: Success nếu 1 child success, Failure nếu tất cả fail |
| `Sequence.cs` | `Sequence` | AND logic: Failure nếu 1 child fail, Success nếu tất cả success |
| `AttackClosestEnemyNode.cs` | `AttackClosestEnemyNode` | Tìm enemy gần nhất trong range, tạo DamageCommand |

### 2.11 Effects System (`Combat/Effects/`)

**Base Class `SkillEffect`:**
```csharp
public abstract class SkillEffect : ScriptableObject
{
    public abstract void Apply(CombatUnit caster, CombatUnit[] targets);
}
```

**Các effect implementation (7 files):**
| File | Class | Description |
|---|---|---|
| `DamageEffect.cs` | `DamageEffect` | Gây sát thương vật lý/phép/thuần. Tính `ATK * multiplier - target.PDEF/MDEF`. Hỗ trợ crit `(CritChance * CritDamage)` và Empowered stacks. Method `CalculateHits()` trả về List<HitData> |
| `HealEffect.cs` | `HealEffect` | Hồi máu theo % ATK của caster |
| `ApplyStatusEffect.cs` | `ApplyStatusEffect` | Apply status effect lên target (Stun, Burn, Taunt, Empowered, etc.) với duration, value, stacks |
| `BuffStatEffect.cs` | `BuffStatEffect` | Buff/Debuff stats (ATK, PDEF, MDEF) với multiplier và duration |
| `ShieldEffect.cs` | `ShieldEffect` | Tạo shield (damage reduction charges) giảm % damage |
| `DamageReductionChargeEffect.cs` | `DamageReductionChargeEffect` | Thêm lớp giáp giảm sát thương |
| `LifeStealEffect.cs` | `LifeStealEffect` | Hút máu theo % sát thương gây ra |

### 2.12 Status Effects (`Combat/StatusEffects/`)

| File | Description |
|---|---|
| `StatusEffectType.cs` | Enum định nghĩa tất cả status types |
| `ChallengeStack.cs` | `ChallengeStack` class - Cơ chế stack thử thách (tăng dần sát thương khi bị tấn công liên tiếp) |

**Status Types:**
| Type | Description |
|---|---|
| `Stun` | Không thể hành động |
| `Taunt` | Phải tấn công người đã taunt |
| `ThieuDot` (Burn) | Sát thương mỗi lượt |
| `DiemYeu` (Weakness) | Tăng sát thương nhận vào |
| `GiamSatThuong` (Damage Reduction) | Giảm % sát thương nhận |
| `ReflectDamage` | Phản % sát thương |
| `Empowered` | Tăng sát thương (tiêu thụ sau khi đánh) |
| `SieuViet` (Superior) | Tăng damage multiplier |
| `BuiSao` (Stardust) | Tăng damage multiplier |
| `YChi` (Willpower) | Tăng damage multiplier |
| `SelfDamage` | Gây sát thương lên bản thân |

### 2.13 Commands (`Combat/Commands/`)
**Interface `ICombatCommand`:**
```csharp
public interface ICombatCommand
{
    IEnumerator Execute();
}
```

**Các command cụ thể:**
| File | Class | Description |
|---|---|---|
| `ICombatCommand.cs` | `ICombatCommand` | Interface cho tất cả commands |
| `DamageCommand.cs` | `DamageCommand` | Gây sát thương cho 1 target, hỗ trợ coroutine |
| `MultiHitDamageCommand.cs` | `MultiHitDamageCommand` | Multi-hit damage, mỗi hit có damage riêng, chạy tuần tự |

### 2.14 Combat Experience (`Combat/CombatExperienceManager.cs`)
Quản lý EXP nhận được sau combat: hiển thị trên VictoryPanel, gọi `PlayerProgression.AddPartyExperience()`.

### 2.15 Combat Camera Animation Integration (`Combat/CombatCameraAnimationIntegration.cs`)
Tích hợp camera animation với các sự kiện combat.

### 2.16 Combat Audio (`Combat/CombatAudioManager.cs`)
Quản lý audio trong combat:
- `PlayCombatBGM(int, AudioClip)` - Phát BGM theo combat area
- SFX cho các hành động

### 2.17 HitData (`Combat/HitData.cs`)
```csharp
public class HitData
{
    public CombatUnit target;
    public int damage;
}
```

### 2.18 HitEventReceiver (`Combat/HitEventReceiver.cs`)
Nhận animation events từ Animator:
- `OnHitFrame(int hitIndex)` - Khi đến frame hit
- `OnVFXFrame(int vfxIndex)` - Khi đến frame VFX

### 2.19 Combat Result UI (`Combat/CombatResultUI.cs`)
Hiển thị kết quả combat (Victory/Defeat):
- Victory Panel: EXP nhận được, party member list
- Defeat: tùy chọn retry hoặc quit

### 2.20 Combat Scene Starter (`Combat/CombatSceneStarter.cs`)
Khởi tạo combat scene khi load:
- Đọc `CombatSessionData`
- Gọi `PlayerManager.Instance.EnableMovement()` / `DisableMovement()`
- Gọi `CombatManager.Instance.StartCombat()`

### 2.21 Combat Test Starter (`Combat/CombatTestStarter.cs`)
Dùng để test combat từ scene editor:
- Tạo enemy group test
- Tạo formation test

### 2.22 Enemy Animator Setup (`Combat/EnemyAnimatorSetup.cs`)
Thiết lập animator override controller cho enemy.

---

## 3. Data System

### 3.1 CombatSessionData (`Data/CombatSessionData.cs`)
**Static class** - Nguồn dữ liệu duy nhất cho session combat.

```csharp
public static class CombatSessionData
```

**Fields:**
- `Formation` - Formation của player
- `EnemyGroup` - Enemy group hiện tại
- `HasData` - Kiểm tra có dữ liệu không
- `IsFromMap` - Có phải từ map encounter không
- `QuestTargetId` - ID của quest step đang chờ combat

**Lifecycle:**
1. `Set()` - Khi bắt đầu combat (từ MapEnemy, NPCInteraction, EncounterZone)
2. Tồn tại qua retry (không xóa khi thua)
3. `Clear()` - Khi thắng hoặc quit về map

### 3.2 EnemyGroupData (`Data/EnemyGroupData.cs`)
**ScriptableObject** - Định nghĩa một nhóm enemy.

```csharp
public class EnemyGroupData : ScriptableObject
```

**Fields:**
| Field | Description |
|---|---|
| `combatArea` | Khu vực combat (1 = grass, 2 = desert, etc.) |
| `bgmClip` | BGM cho combat này |
| `introStinger` | Âm thanh intro khi gặp quái |
| `victoryFanfare` | Âm thanh chiến thắng |
| `enemies` | EnemyEntry[] - Danh sách enemy |

**Class `EnemyEntry`:**
| Field | Description |
|---|---|
| `data` | CharacterData |
| `level` | Cấp độ |
| `gridSlot` | Vị trí grid (0-8) |

### 3.3 ExperienceConfig (`Data/ExperienceConfig.cs`)
**ScriptableObject** - Cấu hình EXP.

```csharp
public class ExperienceConfig : ScriptableObject
```

**Fields:** `maxLevel`, `baseExpCurve` (AnimationCurve)

**Methods:** `GetExpNeededForLevelUp(int level)` - Lấy EXP cần cho mỗi cấp

### 3.4 FormationData (`Data/FormationData.cs`)
**ScriptableObject** - Định nghĩa đội hình.

```csharp
public class FormationData : ScriptableObject
```

**Fields:**
| Field | Description |
|---|---|
| `formationName` | Tên đội hình |
| `slots` | FormationSlot[] - Các slot |

**Class `FormationSlot`:**
| Field | Description |
|---|---|
| `data` | CharacterData |
| `level` | Cấp độ |
| `gridSlot` | Vị trí grid (0-8) |

### 3.5 FormationDataStorage (`Data/FormationDataStorage.cs`)
**Static class** - Lưu trữ formation tạm thời (được replace bởi CombatSessionData).
```csharp
public static class FormationDataStorage
```
- `PendingFormation` - Formation đang chờ

---

## 4. Character & Skills System

### 4.1 CharacterData (`Characters/CharacterData.cs`)
**ScriptableObject** - Định nghĩa nhân vật.

```csharp
[CreateAssetMenu(fileName = "CH_New", menuName = "RPG/Character")]
public class CharacterData : ScriptableObject
```

**Fields:**
| Field | Description |
|---|---|
| `characterName` | Tên nhân vật |
| `characterType` | Player / Enemy |
| `baseLevel` | Level cơ bản |
| `baseExpThreshold` | EXP cần cho level đầu tiên |
| `expIncrementPerLevel` | EXP tăng thêm mỗi level |
| `maxHP`, `atk`, `pdef`, `mdef` | Stats có sẵn |
| `hpg`, `atkg`, `pdefg`, `mdefg` | Tăng trưởng stats mỗi level |
| `expReward` | EXP thưởng khi bị đánh bại |
| `skills` | SkillData[] - Các skill |
| `battleSprite` | Sprite trong combat |
| `prefab` | Prefab trong combat scene |
| `flipOnSpawn` | Có flip sprite khi spawn không |
| `passiveScript` | MonoScript - Passive ability |

**Stat Calculation Methods:**
- `GetHP(int level)` = `maxHP + hpg * (level - 1)`
- `GetATK(int level)` = `atk + atkg * (level - 1)`
- `GetPDEF(int level)` = `pdef + pdefg * (level - 1)`
- `GetMDEF(int level)` = `mdef + mdefg * (level - 1)`

### 4.2 Passives (`Characters/Passives/`)
**Base Class `PassiveAbility`:**
```csharp
public abstract class PassiveAbility
{
    public CombatUnit Owner { get; protected set; }
    public virtual void Initialize(CombatUnit owner) { Owner = owner; }
    public virtual void OnTurnStart() { }
    public virtual void OnDealDamage(CombatUnit target, int damage) { }
    public virtual void OnTakeDamage(CombatUnit attacker, int damage) { }
    public virtual void OnHeal(int amount) { }
    public virtual void OnKill(CombatUnit target) { }
    public virtual void OnDied() { }
    public virtual void OnSpendAP(int amount) { }
}
```

**Passive Implementations:**
| Class | Character | Effect |
|---|---|---|
| `AeosPassive` | Aeos | Nếu ATK < target PDEF: x2 damage. Hồi 10% máu khi tiêu diệt kẻ địch |
| `AleusPassive` | Aleus | Tấn công kẻ địch có thể gây choáng (30%) hoặc giảm phòng thủ (50%) |
| `CelinePassive` | Celine | Nếu máu dưới 50%, nhân đôi tấn công. Hồi 5% máu mỗi lượt |
| `CharlottePassive` | Charlotte | Khi nhận sát thương, 25% phản lại 50% sát thương. Khi tấn công hút 10% máu |
| `HanaPassive` | Hana | Tăng 10% sát thương mỗi đòn combo. Max 100% (10 stack). Reset khi đổi mục tiêu |
| `LucioPassive` | Lucio | Khi bị tấn công, 20% khóa máu ở 1 HP. Hồi 5% máu mỗi khi dùng skill |

### 4.3 SkillData (`Skills/SkillData.cs`)
**ScriptableObject** - Định nghĩa kỹ năng.

```csharp
[CreateAssetMenu(fileName = "SK_New", menuName = "RPG/Skill")]
public class SkillData : ScriptableObject
```

**Fields:**
| Field | Description |
|---|---|
| `skillName` | Tên skill |
| `description` | Mô tả |
| `skillType` | Auto / Passive |
| `damageType` | Physical / Magical / True |
| `targetType` | SingleEnemy / SingleAlly / AllEnemies / AllAllies / Self |
| `apCost` | AP cost |
| `hitCount` | Số hit (multi-hit) |
| `doesNotEndTurn` | Skill không kết thúc lượt |
| `effects` | SkillEffect[] - Mảng effect |

**VFX Fields:**
| Field | Description |
|---|---|
| `vfxPrefab` | Legacy VFX prefab |
| `vfxOffset` | Offset Y cho legacy VFX |
| `vfxEvents` | VFXEvent[] - Mảng VFX events mới |
| `sfxClip` | SFX clip khi dùng skill |
| `voiceClip` | Voice clip khi dùng skill |
| `animationTrigger` | Animation trigger name |
| `movementOverride` | SkillMovementOverride - Override movement animation |

**Struct `VFXEvent`:**
| Field | Description |
|---|---|
| `vfxPrefab` | Prefab VFX |
| `spawnMode` | VFXSpawnMode (AtCaster / AtTarget / HitOnEachTarget) |
| `offset` | Vector3 offset |
| `attachToCaster` | Có attach vào caster không |
| `scale` | Scale |

**Struct `SkillMovementOverride`:**
| Field | Description |
|---|---|
| `moveToTargetDuration` | Thời gian di chuyển đến target |
| `returnDuration` | Thời gian quay về |
| `faceOffDistance` | Khoảng cách dừng so với target |

### 4.4 SkillEffect (`Skills/SkillEffect.cs`)
Base class trừu tượng:
```csharp
public abstract class SkillEffect : ScriptableObject
{
    public abstract void Apply(CombatUnit caster, CombatUnit[] targets);
}
```

---

## 5. Map & Exploration System

### 5.1 PlayerManager (`New/PlayerManager.cs`)
**Singleton** - Quản lý player trên map.

```csharp
public class PlayerManager : MonoBehaviour
```

**Fields:**
| Field | Description |
|---|---|
| `playerPrefab` | Prefab player |
| `playerSpawnPoint` | Spawn point |
| `playerMovementScript` | Movement script reference |
| `IsMovementDisabled` | bool - movement có bị disable không |

**Methods:**
| Method | Description |
|---|---|
| `SpawnPlayer()` | Spawn player tại spawn point |
| `GetPlayer()` | Lấy GameObject player |
| `DisableMovement()` | Disable movement & CharacterController |
| `EnableMovement()` | Enable movement & CharacterController |

### 5.2 SceneTransitionManager (`New/SceneTransitionManager.cs`)
**Singleton** - Quản lý chuyển scene map.

```csharp
public class SceneTransitionManager : MonoBehaviour
```

**Methods:**
| Method | Description |
|---|---|
| `TransitionToMap(string mapName, Vector3 spawnPosition)` | Chuyển đến map khác |
| `GetCurrentMapName()` | Lấy tên map hiện tại |

### 5.3 EncounterZone (`EncounterZone.cs`)
**MonoBehaviour** - Vùng encounter ngẫu nhiên.

**Cơ chế hoạt động:**
- Player di chuyển trong vùng → tích lũy thời gian `movingTimeAccumulated`
- 0 → 5s: An toàn (không encounter)
- 5 → 8s: Xác suất `encounterChance`%
- 8s+: Chắc chắn encounter
- Đứng yên: timer dừng
- Cooldown 3s sau mỗi encounter

**Encounter Pool:** `List<WeightedEncounter>` - Chọn weighted random, có level offset.

**Flow:**
1. TriggerEncounter() → dừng player → fade to black
2. Lưu Formation → tạo scaled clone của enemy group
3. Set CombatSessionData → load combat scene additive
4. Khi quay về (OnEnable): restore player movement

### 5.4 MapEnemy (`New/MapEnemy.cs`)
Enemy hiển thị trên map (có thể chạm vào để trigger combat).

**Fields:** Health bar, sprites, `Idle`/`Run` animations, `EncounterZone` reference

**Methods:**
- `TakeDamage(int)`, `Die()`, `Respawn()`
- Gọi `CombatSessionData.Set()` khi bắt đầu combat

### 5.5 MapEnemyPatrol (`New/MapEnemyPatrol.cs`)
AI di chuyển cho enemy trên map.

**Fields:** waypoints, speed, rotation speed, pause time at waypoints

### 5.6 NPCInteraction (`New/NPCInteraction.cs`)
**MonoBehaviour** - Tương tác NPC.

**Các loại interaction:**
- `Dialogue` - Mở hội thoại
- `Quest` - Bắt đầu/hoàn thành quest
- `Shop` - Mở cửa hàng
- `Combat` - Bắt đầu combat với NPC

### 5.7 Portal (`New/Portal.cs`)
Cổng dịch chuyển giữa các map.

**Fields:** `destinationMapName`, `spawnPosition`, `fadeDuration`

### 5.8 SpawnPoint (`New/SpawnPoint.cs`)
Điểm spawn cho player khi vào map.

**Fields:** `spawnID`

### 5.9 LocationTrigger (`New/LocationTrigger.cs`)
Trigger khi player đến vị trí cụ thể.

**Fields:** `onTriggerEnter` / `onTriggerExit` GameEvent, `oneTimeOnly`

### 5.10 ItemPickup (`New/ItemPickup.cs`)
Item có thể nhặt trên map.

**Fields:** `ItemData`, `ItemPickupUI`, hiệu ứng particle

### 5.11 FormationManager (`Map/Formation/FormationManager.cs`)
**MonoBehaviour** - Quản lý đội hình trên map.

```csharp
public class FormationManager : MonoBehaviour
```

**Methods:**
| Method | Description |
|---|---|
| `SaveFormation()` | Lưu formation hiện tại vào FormationDataStorage |
| `GetCurrentFormationData()` | Lấy formation hiện tại |

### 5.12 MapUIController (`Map/MapUIController.cs`)
**MonoBehaviour** - Điều khiển UI trên map.

**Chức năng:**
- Hiển thị tên map
- Quản lý UI elements (quest marker, location label)
- Xử lý tạm dừng (pause menu)

### 5.13 BillboardSprite (`Map/BillboardSprite.cs`)
Sprite luôn hướng về camera (dùng cho NPC/enemy name plates).

### 5.14 Quest Visibility (`New/QuestVisibilityConfig.cs` & `QuestVisibilityController.cs`)
Quản lý hiển thị object trên map dựa trên quest progress:
- `ShowBeforeQuest`: hiển thị trước khi nhận quest
- `ShowDuringQuest`: hiển thị trong khi quest
- `ShowAfterQuest`: hiển thị sau khi quest hoàn thành
- `HideAfterQuest`: ẩn sau khi quest hoàn thành

---

## 6. Dialogue System

### 6.1 DialogueTrigger (`Systems/Dialogue/DialogueTrigger.cs`)
**MonoBehaviour** - Điều khiển hội thoại.

```csharp
public class DialogueTrigger : MonoBehaviour
```

**Fields:**
| Field | Description |
|---|---|
| `dialogueLines` | DialogueLineData[] - Các dòng hội thoại |
| `dialogueCharacter` | DialogueCharacter - Nhân vật nói |
| `onDialogueEnd` | GameEvent - Sự kiện khi hội thoại kết thúc |
| `allowMovementDuringDialogue` | Cho phép di chuyển trong hội thoại |
| `teleportPlayerTo` | Transform - Dịch chuyển player đến điểm này khi bắt đầu |
| `useCurrentPlayerPosition` | Giữ nguyên vị trí player |

**Camera & Effects:**
| Field | Description |
|---|---|
| `switchToDialogueCamera` | Chuyển camera khi hội thoại |
| `facePlayerTowardsNPC` | Xoay player về phía NPC |
| `flipCharacterSprite` | Flip sprite nhân vật |
| `fadeToBlackBefore` | Fade to black trước hội thoại |
| `useSpeakerNameText` | Hiển thị tên người nói |

**Methods:**
- `StartDialogue()` / `EndDialogue()` / `NextLine()`
- `DialogueCameraEnter()` / `DialogueCameraExit()`
- `AddOnDialogueEndListener()` / `RemoveOnDialogueEndListener()`

### 6.2 DialogueLineData (`Systems/Dialogue/DialogueLineData.cs`)
**ScriptableObject** - Một dòng hội thoại.

```csharp
[CreateAssetMenu(fileName = "DL_New", menuName = "RPG/Dialogue/DialogueLine")]
public class DialogueLineData : ScriptableObject
```

**Fields:**
| Field | Description |
|---|---|
| `speakerName` | Tên người nói |
| `dialogueText` | Nội dung (hỗ trợ rich text) |
| `emotion` | DialogueEmotion (Normal/Happy/Angry/Sad/Surprised/Cry/Blush) |
| `typingSpeed` | Tốc độ gõ chữ |
| `sfxOnDisplay` | SFX khi hiển thị chữ |
| `voiceClip` | Voice clip |
| `eventsOnStart` | GameEvent[] - Sự kiện khi bắt đầu dòng |
| `eventsOnEnd` | GameEvent[] - Sự kiện khi kết thúc dòng |

### 6.3 DialogueCharacter (`Systems/Dialogue/DialogueCharacter.cs`)
**ScriptableObject** - Nhân vật hội thoại.

**Fields:** `characterName`, `nameColor`, `portraits` (List<PortraitEntry>)

**Method:** `GetPortrait(string emotionKey)` - Lấy sprite theo emotion

**Class `PortraitEntry`:** `emotionKey`, `sprite`

### 6.4 DialogueCamera (`Systems/Dialogue/DialogueCamera.cs`)
**MonoBehaviour** - Tag component để tìm camera dialogue trong scene.

---

## 7. Quest System

### 7.1 QuestManager (`Systems/Quest/QuestManager.cs`)
**Singleton** - Quản lý toàn bộ quest.

```csharp
public class QuestManager : MonoBehaviour
```

**Fields:**
| Field | Description |
|---|---|
| `questDatabase` | QuestData[] - Database quest |
| `activeQuests` | Dictionary<string, QuestProgress> |
| `completedQuests` | HashSet<string> |

**Methods:**
| Method | Description |
|---|---|
| `StartQuest(string)` | Bắt đầu quest |
| `CompleteQuest(string)` | Hoàn thành quest |
| `UpdateQuestProgress(string, string, int)` | Cập nhật tiến trình step |
| `OnEnemyDefeated(string)` | Gọi khi enemy bị tiêu diệt (check quest) |
| `OnEnemyGroupDefeated(string)` | Gọi khi enemy group bị tiêu diệt (NPC combat) |
| `OnItemCollected(string, int)` | Gọi khi nhặt item |
| `OnNPCTalked(string)` | Gọi khi nói chuyện NPC |
| `OnLocationReached(string)` | Gọi khi đến địa điểm |
| `GetQuestProgress(string)` | Lấy tiến trình quest |

### 7.2 QuestData (`Systems/Quest/QuestData.cs`)
**ScriptableObject** - Định nghĩa quest.

```csharp
public class QuestData : ScriptableObject
```

**Fields:**
| Field | Description |
|---|---|
| `questID` | ID quest |
| `questName` | Tên quest |
| `description` | Mô tả |
| `questSteps` | QuestStep[] |
| `prerequisites` | QuestPrerequisite[] - Quest cần hoàn thành trước |
| `rewardEXP` | EXP thưởng |
| `rewardItems` | ItemReward[] |

**Class `QuestStep`:**
| Field | Description |
|---|---|
| `stepID` | ID step |
| `description` | Mô tả |
| `stepType` | QuestStepType (DefeatEnemy, DefeatEnemyGroup, CollectItem, TalkToNPC, ReachLocation) |
| `targetID` | ID mục tiêu |
| `requiredAmount` | Số lượng yêu cầu |
| `eventsOnStart` / `eventsOnComplete` | GameEvent[] |

**Class `QuestPrerequisite`:** `questID`

**Class `ItemReward`:** `item` (ItemData), `amount`

### 7.3 QuestProgress (`Systems/Quest/QuestProgress.cs`)
**Serializable class** - Lưu tiến trình quest runtime.

```csharp
public class QuestProgress
```

**Fields:**
| Field | Description |
|---|---|
| `QuestID` | string |
| `StepProgress` | Dictionary<string, int> - stepID → current progress |
| `CurrentStepIndex` | int |
| `IsCompleted` | bool |

**Methods:**
- `UpdateProgress(string, int)` - Tăng progress cho step
- `GetCurrentStep()` - Lấy step hiện tại
- `IsStepComplete(string)` - Kiểm tra step đã hoàn thành chưa

### 7.4 Puzzle System (`Systems/Quest/`)
Hệ thống puzzle gồm 15 files, tất cả đều nằm trong `Systems/Quest/`:

#### Base Class & Config
| File | Class | Description |
|---|---|---|
| `PuzzleBase.cs` | `PuzzleBase : MonoBehaviour` (abstract) | Base class cho tất cả puzzle UI. Event `OnPuzzleFinished(bool)`. Methods: `StartPuzzle()`, `CompletePuzzle()`, `ClosePuzzle()` |
| `PuzzleData.cs` | `PuzzleData : ScriptableObject` | Config data cho puzzle: `puzzleType` (enum), `puzzleName`, `description`, `dialogueLines`, `successEvent`/`failEvent` (GameEvent), các tham số riêng cho từng loại puzzle |
| `PuzzleTrigger.cs` | `PuzzleTrigger : MonoBehaviour` | Trigger để bắt đầu puzzle khi player đến gần. Có `oneTimeOnly`, `autoStart`, `PuzzleData` reference |
| `QuestReward.cs` | `QuestReward : MonoBehaviour` | Phần thưởng khi hoàn thành quest/puzzle: EXP, items, quest completion |
| `QuestStep.cs` | `QuestStep : ScriptableObject` | Serializable class cho step quest (có thể được tách riêng) |

#### Puzzle Implementations
| File | Class | Description |
|---|---|---|
| `SlidePuzzle.cs` | `SlidePuzzle : PuzzleBase` | Puzzle trượt ô (sliding puzzle). Grid NxN, kéo thả các ô để sắp xếp đúng thứ tự |
| `FlowPuzzle.cs` | `FlowPuzzle : PuzzleBase` | Puzzle nối dòng (flow/pipe). Nối các điểm cùng màu bằng đường đi không chồng chéo |
| `RiddleGatePuzzle.cs` | `RiddleGatedPuzzle : PuzzleBase` | Puzzle câu đố cổng. Trả lời đúng câu hỏi để mở cửa |
| `SymbolSequencePuzzle.cs` | `SymbolSequencePuzzle : PuzzleBase` | Puzzle dãy ký tự. Nhập đúng sequence symbols để giải |
| `SpirePuzzle.cs` | `SpirePuzzle : PuzzleBase` | Puzzle tower spire. Cơ chế đặc biệt cho dungeon |
| `MemoryGrovePuzzle.cs` | `MemoryGrovePuzzle : PuzzleBase` | Puzzle memory/thẻ nhớ. Lật thẻ và tìm cặp |

#### Interactive Components
| File | Class | Description |
|---|---|---|
| `WireDragItem.cs` | `WireDragItem : MonoBehaviour, IBeginDragHandler, IDragHandler, IEndDragHandler` | Item có thể kéo thả (dây điện) cho puzzle nối dây |
| `WireDropTarget.cs` | `WireDropTarget : MonoBehaviour, IDropHandler` | Drop target cho wire drag items |

---

## 8. Inventory & Equipment System

### 8.1 Inventory (`Systems/Inventory/Inventory.cs`)
**Serializable class** - Hệ thống túi đồ.

```csharp
[Serializable]
public class Inventory
```

**Fields:**
| Field | Description |
|---|---|
| `items` | List<ItemStack> |

**Class `ItemStack`:** `item` (ItemData), `amount` (int)

**Methods:**
| Method | Description |
|---|---|
| `AddItem(ItemData, int)` | Thêm item (stack nếu đã có) |
| `RemoveItem(ItemData, int)` | Xóa item |
| `HasItem(ItemData)` | Kiểm tra có item không |
| `GetItemCount(ItemData)` | Lấy số lượng |
| `Clear()` | Xóa tất cả |
| `Save(string)` / `Load(string)` | Save/Load binary |

### 8.2 ItemData (`Systems/Inventory/ItemData.cs`)
**ScriptableObject** - Định nghĩa item.

```csharp
[CreateAssetMenu(fileName = "IT_New", menuName = "RPG/Item")]
public class ItemData : ScriptableObject
```

**Fields:** `itemName`, `description`, `icon`, `maxStack`, `itemType` (Consumable/Equipment/KeyItem/Material)

### 8.3 CharacterEquipment (`Systems/Equipment/CharacterEquipment.cs`)
Trang bị của một nhân vật.

```csharp
[Serializable]
public class CharacterEquipment
```

**Fields:** 4 slot (Weapon, Helmet, Armor, Accessory) - `EquipmentData`

**Methods:**
| Method | Description |
|---|---|
| `Equip(EquipmentData)` | Trang bị item (tự động chọn slot theo type) |
| `Unequip(EquipmentSlot)` | Tháo trang bị |
| `GetHPBonus()` / `GetATKBonus()` / `GetPDEFBonus()` / `GetMDEFBonus()` | Lấy tổng bonus từ trang bị |

### 8.4 EquipmentData (`Systems/Equipment/EquipmentData.cs`)
**ScriptableObject** extends ItemData - Trang bị.

```csharp
[CreateAssetMenu(fileName = "EQ_New", menuName = "RPG/Equipment")]
public class EquipmentData : ItemData
```

**Fields:** `slot` (EquipmentSlot), `hpBonus`, `atkBonus`, `pdefBonus`, `mdefBonus`

**Enum `EquipmentSlot`:** Weapon, Helmet, Armor, Accessory

### 8.5 EquipmentManager (`Systems/Equipment/EquipmentManager.cs`)
**Singleton** - Quản lý trang bị cho tất cả nhân vật.

```csharp
public class EquipmentManager : MonoBehaviour
```

**Methods:**
| Method | Description |
|---|---|
| `EquipItem(CharacterData, EquipmentData)` | Mặc trang bị |
| `UnequipItem(CharacterData, EquipmentSlot)` | Tháo trang bị |
| `GetEquipment(CharacterData)` | Lấy trang bị hiện tại |
| `HasEquipment(CharacterData, EquipmentSlot)` | Kiểm tra có trang bị không |

---

## 9. Audio System

### 9.1 AudioManager (`Audio/AudioManager.cs`)
**Singleton** - Quản lý toàn bộ âm thanh.

```csharp
public class AudioManager : MonoBehaviour
```

**Fields:**
| Field | Description |
|---|---|
| `musicSource` | AudioSource cho nhạc nền |
| `sfxSource` | AudioSource cho SFX |
| `voiceSource` | AudioSource cho voice |
| `musicVolume`, `sfxVolume`, `voiceVolume` | Volume settings |

**Methods:**
| Method | Description |
|---|---|
| `PlayMusic(AudioClip, float, bool)` | Phát nhạc nền |
| `PlaySFX2D(AudioClip, float)` | Phát SFX 2D |
| `PlayVoice(AudioClip, float)` | Phát voice |
| `StopMusic()` / `PauseMusic()` / `ResumeMusic()` |
| `SetMusicVolume(float)` / `SetSFXVolume(float)` / `SetVoiceVolume(float)` |

### 9.2 AudioSourcePool (`Audio/AudioSourcePool.cs`)
Pool các AudioSource để phát SFX không giới hạn (tránh tạo mới liên tục).

### 9.3 CombatAudioManager (`Combat/CombatAudioManager.cs`)
**Singleton** - Quản lý audio riêng cho combat.
- `PlayCombatBGM(int, AudioClip)` - Phát BGM theo combat area
- SFX cho clash, victory, defeat

---

## 10. UI System

### 10.1 Combat UI (`UI/Combat/`)
| File | Description |
|---|---|
| `CombatPlanningUI.cs` | UI chính khi player lập kế hoạch (skill buttons, target selection) |
| `SkillButtonUI.cs` | Button cho skill trong combat |
| `HealthBarUI.cs` | Thanh máu UI |
| `TurnOrderUI.cs` | UI thứ tự lượt |
| `FloatingTextController.cs` | Text nổi (+dmg, -heal) |
| `TargetingArrowController.cs` | Mũi tên chỉ target | 
| `ActionConfirmationUI.cs` | UI xác nhận action |
| `VictoryPanel.cs` | Panel chiến thắng (EXP, item drops) |
| `DefeatPanel.cs` | Panel thua cuộc (retry / quit) |
| `APBarUI.cs` | Thanh AP |

### 10.2 Dialogue UI (`UI/Dialogue/`)
| File | Description |
|---|---|
| `DialogueUI.cs` | UI chính cho hội thoại (text box, portrait, options) |

### 10.3 Equipment UI (`UI/Equipment/`)
| File | Description |
|---|---|
| `EquipmentPanel.cs` | Panel trang bị (4 slots + drag-drop) |
| `EquipmentSlotUI.cs` | Slot UI (drag target, click handlers) |
| `CharacterSelectUI.cs` | Chọn nhân vật để xem trang bị |

### 10.4 Inventory UI (`UI/Inventory/`)
| File | Description |
|---|---|
| `InventoryPanel.cs` | Panel túi đồ (grid, item details) |
| `InventorySlotUI.cs` | Slot item UI (icon, stack count) |

### 10.5 Map UI (`UI/Map/`)
| File | Description |
|---|---|
| `MapUI.cs` | UI chính trên map |
| `MinimapUI.cs` | Minimap |
| `LocationLabel.cs` | Label tên địa điểm |

### 10.6 Quest UI (`UI/Quest/`)
| File | Description |
|---|---|
| `QuestPanel.cs` | Panel danh sách quest |
| `QuestLogUI.cs` | UI chi tiết quest & progress |

### 10.7 Quest Marker UI (`UI/QuestMarker/`)
| File | Description |
|---|---|
| `QuestMarker.cs` | Marker trên map chỉ quest objective |
| `QuestMarkerManager.cs` | Quản lý tất cả markers |
| `QuestIndicatorUI.cs` | UI chỉ dẫn hướng |
| `NPCMarker.cs` | Marker cho NPC có quest |
| `MinimapQuestIcon.cs` | Icon quest trên minimap |
| `WaypointArrow.cs` | Mũi tên chỉ đường |

### 10.8 Shared UI (`UI/Shared/`)
| File | Description |
|---|---|
| `TooltipManager.cs` | Quản lý tooltip |
| `UIConstants.cs` | Constants cho UI (màu sắc, kích thước) |
| `UIAnimationHelper.cs` | Helper cho UI animation |

### 10.9 Loading UI (`UI/Loading/`)
| File | Description |
|---|---|
| `LoadingScreen.cs` | Màn hình loading |

### 10.10 MainMenu UI (`UI/MainMenu.cs`)
Menu chính: New Game, Load Game, Settings, Quit.

### 10.11 Other UI (`UI/`)
| File | Description |
|---|---|
| `AudioTestUI.cs` | UI test âm thanh |
| `CombatTestUI.cs` | UI test combat |
| `SaveGameUI.cs` / `LoadGameUI.cs` | UI lưu/tải game |
| `TabController.cs` | Controller cho tab UI |
| `TurnOrderUIController.cs` | Controller thứ tự lượt |

### 10.12 Legacy UI from `Assets/Scripts/`
| File | Description |
|---|---|
| `CircularToggleButton.cs` | Nút toggle tròn |
| `CombatTestUI.cs` | Test combat |
| `LoadGameUI.cs` / `SaveGameUI.cs` | Lưu/tải |
| `TabController.cs` | Tab controller |
| `TurnOrderUIController.cs` | Turn order |

---

## 11. Enums & Constants

### 11.1 CombatEnums (`Enums/CombatEnums.cs`)
```csharp
public enum CharacterType { Player, Enemy }
public enum SkillType { Auto, Passive }
public enum DamageType { Physical, Magical, True }
public enum TargetType { SingleEnemy, SingleAlly, AllEnemies, AllAllies, Self }
public enum StatType { HP, MaxHP, ATK, PDEF, MDEF }
public enum SkillEffectTrigger { OnUse }
```

### 11.2 StatusEffectType
```csharp
public enum StatusEffectType
{
    Stun,           // Choáng
    Taunt,          // Khiêu khích
    ThieuDot,       // Burn (Thiêu Đốt)
    DiemYeu,        // Weakness (Điểm Yếu)
    GiamSatThuong,  // Damage Reduction
    ReflectDamage,  // Phản sát thương
    Empowered,      // Cường hóa
    SieuViet,       // Superior
    BuiSao,         // Stardust
    YChi,           // Willpower
    SelfDamage      // Tự gây sát thương
}
```

### 11.3 Animation Constants (`Combat/AnimationConstants.cs`)
```csharp
public static class AnimationConstants
{
    public const string Idle = "Idle";
    public const string Rush = "Rush";
    public const string Hurt = "Hurt";
    public const string Knockback = "Knockback";
    public const string Die = "Die";
    public const string Attack = "Attack";
    public const string Skill1 = "Skill1";
}
```

---

## 12. Legacy Scripts (Assets/Scripts)

### 12.1 Equipment (`Assets/Scripts/Equipment/EquipmentSlotUI.cs`)
Class `EquipmentSlotUI : MonoBehaviour, IDropHandler, IPointerClickHandler`
- Drag-and-drop equip
- Right-click/double-click unequip
- Validate slot type

### 12.2 Timeline (`Assets/Scripts/Timeline/`)
Custom Timeline tracks:
- `ImageFadeBehaviour/Clip/MixerBehaviour/Track` - Fade UI Image
- `TimelineStarterOnClick` - Play Timeline on click, show skip button

### 12.3 AI (`Assets/Scripts/AI/BehaviorTree/`)
Chứa Behavior Tree cho AI (đang trong quá trình phát triển).

### 12.4 Combat & UI Legacy
Các file UI cũ được sử dụng song song với UI mới trong quá trình chuyển đổi.

---

## 13. Event Flow Diagrams

### 13.1 Game Initialization Flow
```
Boot Scene
    ↓
GameInitializer.Awake()
    ├── Tạo PlayerProgression (nếu chưa có)
    ├── Tạo SceneLoaderManager (nếu chưa có)
    ├── Khởi tạo AudioManager (nếu chưa có)
    ├── Load PersistentScene (additive)
    └── Unload Boot Scene
            ↓
    PersistentScene chứa:
    ├── EventManager (singleton)
    ├── FadeController (singleton)
    ├── PlayerManager (singleton)
    ├── SceneTransitionManager (singleton)
    ├── QuestManager (singleton)
    └── EquipmentManager (singleton)
```

### 13.2 Encounter Flow (Random)
```
Player moves in EncounterZone
    ├── movingTime < safeTime → an toàn
    ├── safeTime ≤ movingTime < maxTime → roll encounterChance%
    └── movingTime ≥ maxTime → 100% encounter

TriggerEncounter()
    ├── StopPlayer() // disable movement, reset animation
    ├── PickWeightedEncounter() // weighted random từ pool
    ├── CreateScaledClone() // clone + level offset
    ├── FormationManager.SaveFormation()
    ├── CombatSessionData.Set(formation, enemyGroup, fromMap=true)
    ├── FadeToBlack()
    ├── MapRoot.SetActive(false)
    ├── PersistentContainer.SetActive(false)
    └── SceneLoaderManager.LoadCombatScene()
```

### 13.3 Combat Flow (Side-Based)
```
CombatManager.StartCombat(formation, enemyGroup)
    ├── Tạo PlayerUnits từ FormationData + Equipment bonuses
    ├── Tạo EnemyUnits từ EnemyGroupData
    ├── SpawnUnitViews() (có passive initialization)
    └── stateMachine.TransitionTo(Intro)

CombatPhase.Intro
    ├── Camera intro sequence
    ├── Enemy rush animation
    ├── Fade UI in
    └── stateMachine.TransitionTo(PlayerTurn)

CombatPhase.PlayerTurn
    ├── Reset AP = 3, HasActedThisTurn = false
    ├── Loop: chờ player chọn unit → skill → target
    │   ├── Kiểm tra AP (skill.apCost)
    │   ├── Trừ AP
    │   ├── ResolveAction → ActionResolver.Resolve()
    │   │   ├── Tính damage từ effects
    │   │   ├── Gọi DamageModificationHook
    │   │   ├── ClashAnimationSequence.PlayAction()
    │   │   └── Unit xử lý hit events (ProcessHitAtFrame)
    │   ├── CheckForCombatEnd()
    │   ├── TickAllStatuses()
    │   └── CheckForCombatEnd()
    └── stateMachine.TransitionTo(EnemyTurn)

CombatPhase.EnemyTurn
    ├── Mỗi enemy còn sống:
    │   ├── HandleStartOfTurnEffects (burn)
    │   ├── EnemyAI.PlanTurn()
    │   ├── ResolveAction()
    │   └── TickAllStatuses()
    └── stateMachine.TransitionTo(PlayerTurn)

Victory
    ├── Tính EXP từ enemies (baseReward + bonus from level)
    ├── Chia đều cho player còn sống
    ├── Fanfare sound
    └── Gọi CombatSessionData.Clear()

Defeat
    ├── Defeat sound
    ├── Cho phép retry (sử dụng CombatSessionData cũ)
    └── Hoặc quit về map
```

### 13.4 Quest Flow
```
NPCInteraction → StartDialogue
    └── QuestManager.StartQuest(questID)
        ├── Thêm vào activeQuests
        ├── Đặt CurrentStepIndex = 0
        └── Gọi eventsOnStart của step đầu

During Quest:
    ├── Enemy defeated → QuestManager.OnEnemyDefeated(enemyName)
    │       └── UpdateQuestProgress(questID, stepID, +1)
    ├── Item collected → QuestManager.OnItemCollected(itemID)
    │       └── UpdateQuestProgress(questID, stepID, +1)
    ├── NPC talked → QuestManager.OnNPCTalked(npcID)
    │       └── UpdateQuestProgress(questID, stepID, +1)
    └── Location reached → QuestManager.OnLocationReached(locationID)
            └── UpdateQuestProgress(questID, stepID, +1)

Complete step:
    └── Gọi eventsOnComplete của step
        └── Chuyển sang step tiếp theo

Complete quest:
    ├── QuestManager.CompleteQuest(questID)
    │   ├── Di chuyển sang completedQuests
    │   ├── Trao thưởng EXP (PlayerProgression.AddPartyExperience)
    │   └── Trao thưởng items (Inventory.AddItem)
    └── Cập nhật QuestVisibilityController
```

### 13.5 Dialogue Flow
```
Player approaches NPC → NPCInteraction
    └── DialogueTrigger.StartDialogue()
        ├── (Optional) Teleport player
        ├── (Optional) Switch to dialogue camera
        ├── (Optional) Fade to black
        ├── (Optional) Flip character sprite
        ├── Hiển thị DialogueLine đầu tiên
        │   ├── Gọi eventsOnStart
        │   ├── Play typing animation + SFX
        │   ├── Cập nhật portrait theo emotion
        │   └── (Optional) Play voice clip
        │
        └── Player click → NextLine()
            ├── Gọi eventsOnEnd của dòng hiện tại
            └── Hiển thị dòng tiếp theo

        └── EndDialogue()
            ├── (Optional) Switch back to gameplay camera
            ├── (Optional) Fade from black
            └── Gọi onDialogueEnd GameEvent
```

### 13.6 Equipment Flow
```
InventoryPanel → Drag item
    └── Drop on EquipmentSlotUI
        ├── Kiểm tra slot type phù hợp
        ├── Nếu slot đã có item → Unequip trước
        │       ├── EquipmentManager.UnequipItem()
        │       └── Inventory.AddItem() (trả item cũ về túi)
        ├── EquipmentManager.EquipItem()
        │       └── Inventory.RemoveItem()
        └── Cập nhật stats: HP/ATK/PDEF/MDEF bonus
            └── Ảnh hưởng đến CombatUnit khi combat bắt đầu
```

### 13.7 Save/Load Flow
```
Save:
    ├── PlayerProgression.SaveProgress() (PlayerPrefs)
    ├── Inventory.Save() (Binary)
    ├── QuestManager.SaveProgress() (serializable)
    └── EquipmentManager.Save() (serializable)

Load:
    ├── PlayerProgression.LoadProgress() (PlayerPrefs)
    ├── Inventory.Load() (Binary)
    ├── QuestManager.LoadProgress() (deserialize)
    └── EquipmentManager.Load() (deserialize)
```

---

## Tổng Kết Kiến Trúc

**Pattern chính:**
- **Singleton** cho hầu hết manager (EventManager, CombatManager, PlayerManager, AudioManager, QuestManager, EquipmentManager, SceneLoaderManager)
- **ScriptableObject** cho data-driven design (CharacterData, SkillData, SkillEffect, EnemyGroupData, ItemData, QuestData, DialogueLineData)
- **State Machine** cho combat phases (CombatStateMachine)
- **Command Pattern** cho combat actions (ICombatCommand)
- **Observer Pattern** với EventManager cho game events
- **Pool Pattern** cho AudioSource
- **Weighted Random** cho encounter/quest rewards

**Data Flow:**
- **CombatSessionData** là cầu nối giữa Map và Combat Scene
- **FormationData** lưu đội hình player
- **EnemyGroupData** định nghĩa nhóm enemy
- **CharacterProgress / PlayerProgression** quản lý level/EXP

**Scenes:**
- BootScene → PersistentScene → MapScene ↔ CombatScene (additive)
- MapScene có thể chuyển đổi qua SceneTransitionManager