# Cinematic Battle System - Setup Guide

## 🎬 What This Is

**Cinematic battle system** inspired by:
- 🎭 **Limbus Company** - Dramatic camera work & cinematic style (like Don Quixote/Sancho ending)
- ⚔️ **Jujutsu Kaisen** - Combat mechanic of projectile attacks (like Yuta vs Ryu)

**Features:**
- ✅ 2 static characters battling on map
- ✅ Dramatic camera movements (focus on attacker, then target)
- ✅ Rapid beam projectile exchange
- ✅ QTE system to increase beam power
- ✅ Intense final clash with camera shakes
- ✅ Smooth cinematic transitions
- ✅ Winner determination based on QTE progress

---

## ⚡ Quick Setup

### Step 1: Create Characters
1. Create 2 **3D Cubes** in your scene:
   - `Lucio` at position `(-3, 0, 0)` - Color Blue
   - `Cedric` at position `(3, 0, 0)` - Color Red

2. **Don't add scripts to them** - they're just static placeholders

### Step 2: Create Battle Manager
1. Create empty GameObject: `BattleManager`
2. Add **SimpleBattleSystem** script
3. In Inspector, assign:
   - **Lucio Transform**: Drag Lucio object
   - **Cedric Transform**: Drag Cedric object

### Step 3: Create Beam System
1. Create empty GameObject: `BeamSystem`
2. Add **SimpleBeamAttackSystem** script
3. Assign beam VFX prefabs:
   - **Lucio Beam VFX**: Your Lucio beam prefab
   - **Cedric Beam VFX**: Your Cedric beam prefab

### Step 4: Create QTE UI
1. Create **Canvas**
2. Add Text + ProgressBar (same as before)
3. Create empty GameObject: `QTESystem`
4. Add **QuickTimeEventSystem** script
5. Assign UI references

### Step 5: Connect Systems
1. Go back to **BattleManager** (SimpleBattleSystem)
2. Assign:
   - **Lucio Transform**: Drag Lucio object
   - **Cedric Transform**: Drag Cedric object
   - **Beam System**: Drag BeamSystem object
   - **Quick Time Event System**: Drag QTESystem object
   - **Main Camera**: Auto-assigned (or drag Camera manually)

### Step 6: Configure Battle (Optional)
Adjust in **SimpleBattleSystem** for desired pacing:
- `totalRounds`: 3 (exchanges before final)
- `roundDelay`: 1.5f (pause between exchanges)
- `cameraShakeMagnitude`: 0.3f (intensity of camera shake)
- `cameraShakeDuration`: 0.3f (how long shake lasts)

---

## 🎯 How to Start Battle

**In your code or Button:**

```csharp
// Get reference to SimpleBattleSystem
SimpleBattleSystem battleSystem = GetComponent<SimpleBattleSystem>();

// Start battle
battleSystem.StartBattle();
```

**Or via Button:**
1. Create UI Button
2. Add this script to it:

```csharp
public class BattleStartButton : MonoBehaviour
{
    [SerializeField] private SimpleBattleSystem battleSystem;
    
    public void OnClick()
    {
        battleSystem.StartBattle();
    }
}
```

3. Assign BattleSystem to button script
4. Drag button into Button's `OnClick()` event

---

## 📊 Battle Flow (Cinematic)

```
[0] INTRO - Dramatic Opening
    └─ Camera zooms to Lucio
    └─ Camera zooms to Cedric
    └─ Camera pulls back to show both

[1] ROUND 1 - Normal Speed (1x)
    ├─ Lucio attacks (0.3s setup)
    ├─ Camera SHAKE on impact
    ├─ Cedric counter-attacks (0.3s setup)
    ├─ Camera SHAKE on impact
    └─ Delay 1.5s (full speed)

[2] ROUND 2 - Faster (1.5x)
    ├─ Lucio attacks (0.2s setup - faster!)
    ├─ Cedric counter-attacks (0.2s setup)
    ├─ Delay 1s (reduced from 1.5s)
    └─ Everything 1.5x speed

[3] ROUND 3 - Much Faster (2x)
    ├─ Lucio attacks (0.15s setup)
    ├─ Cedric counter-attacks (0.15s setup)
    ├─ Delay 0.75s
    └─ All actions 2x speed

[4] FINAL CLASH - Ultimate Showdown ⚡
    ├─ Lucio prepares (wide shot)
    ├─ Cedric prepares (cut to them)
    ├─ Both return to wide shot
    ├─ SIMULTANEOUS attack (2 beams at once!)
    ├─ INTENSE camera SHAKE (0.5s)
    ├─ QTE runs - player changes outcome
    ├─ Winner determined at 50% threshold
    └─ Focus on victor
```

**Key Progression:**
- 🔄 **Each round gets faster** (tạo escalation)
- ⚡ **Speed multiplier**: 1x → 1.5x → 2x
- 🎬 **Camera timing reduces** proportionally (more dramatic)
- 💥 **Final clash**: Both attack simultaneously (ultimate power clash)
- 🎮 **QTE decides outcome** while beams collide


---

## 🎮 VFX Beam System

**How Beams Work:**

1. **Instantiate** prefab at source position
2. **Rotate** to face target direction
3. **Move** from source → target at `beamSpeed`
4. **Destroy** after `beamLifetime`

**Settings in SimpleBeamAttackSystem:**
- `beamSpeed`: 10f (units/second)
- `beamLifetime`: 3f (seconds before destroy)

---

## 📁 File Structure

```
Assets/Scripts/Cutscenes/LucioCutscene/
├── SimpleBeamAttackSystem.cs    (Shoots VFX beams)
├── SimpleBattleSystem.cs        (Manages battle flow)
├── QuickTimeEventSystem.cs      (QTE mechanics)
└── SIMPLE_BATTLE_SETUP.md       (This file)
```

---

## 🔧 Customization

### ⚡ Speed Progression (Most Important!)
The battle automatically speeds up each round:
```
GetSpeedMultiplier() in SimpleBattleSystem
Round 1: 1x (normal speed)
Round 2: 1.5x (50% faster)
Round 3: 2x (100% faster)
```

**How it works:**
- All attack timings divide by speed multiplier
- Creates escalating intensity
- Higher rounds = faster / more dramatic

**Formula in code:**
```csharp
float speedMultiplier = 1f + (round - 1) * 0.5f;
```

**To change progression:**
```csharp
// Slower progression:
return 1f + (round - 1) * 0.25f; // Only 0.25x increase per round

// Faster progression:
return 1f + (round - 1) * 0.75f; // 0.75x increase per round

// Extreme speedup:
return 1f + (round - 1) * 1f;    // Round 3 = 3x speed
```

---

### 🎬 Final Clash - Simultaneous Attack
The final phase now has **2 simultaneous beams**:
- Lucio and Cedric both fire at **the same time**
- Creates dramatic "beam clash" collision
- Player QTE determines winner
- ⚡ More dramatic than rapid exchanges

---

### Camera Settings
In **SimpleBattleSystem** Inspector:
```csharp
cameraShakeMagnitude: 0.3f    // Regular hit (increased to 0.5f+ in final)
cameraShakeDuration: 0.3f     // AUTO-SCALES with speed multiplier
```

**More intense final clash:**
```csharp
// In FinalShowdown(), line ~160:
yield return StartCoroutine(CameraShake(0.8f)); // Change from 0.5f
```

**More dramatic regular attacks:**
```csharp
cameraShakeMagnitude = 0.5f   // Increase from 0.3f (all rounds)
```

### Battle Timing
```csharp
totalRounds = 3               // Exchanges before final (increase for longer)
roundDelay = 1.5f            // Base delay (auto-scales with multiplier)
```

**Shorter battle:**
```csharp
totalRounds = 2              // Fewer exchanges, faster to final
```

**Longer battle:**
```csharp
totalRounds = 5              // More exchanges before climax
```

### Beam Settings
In **SimpleBeamAttackSystem** Inspector:
```csharp
beamSpeed = 10f              // How fast projectile travels
beamLifetime = 3f            // How long before disappear
```

### QTE Difficulty
In **QuickTimeEventSystem**:
```csharp
qteDuration = 5f             // How long QTE lasts
keyPressThrottleTime = 0.05f // Min time between presses (feel)
```

### Win Threshold
In **SimpleBattleSystem** line ~190:
```csharp
if (beamProgress > 0.5f)  // Change to 0.6f for harder, 0.4f for easier
```

---

## 💡 Tips for Cinematic Feel (Limbus Company / JJK Style)

**1. Camera Focus Timing**
- Keep camera focused on attacker before they attack
- Quick cut to defender after impact
- This creates dramatic tension like in scenes

**2. Camera Shake Intensity**
- Light shake (0.1-0.2f) = subtle impact
- Medium shake (0.3f) = solid hits (default)
- Heavy shake (0.5f+) = explosive power

**3. Beam Visuals**
- Use high-quality VFX particles for beams
- Add trails to projectiles (looks cinematic)
- Consider glow/bloom effects

**4. Sound Design**
- Add whoosh sounds for beam fire
- Impact sounds on camera shake
- Background music that builds during QTE

**5. Pacing**
- Longer `roundDelay` (2-3f) = more dramatic
- Faster attacks in final phase = intensity
- Hold on victor longer for impact

**6. Combat Dynamics**
- Add different attack patterns
- Vary beam colors/effects per character
- Mix horizontal and vertical attacks

**7. Final Phase Energy**
- Rapid exchanges feel like power struggle
- Camera shakes = unstable power collision
- QTE simultaneously = player controls outcome
- Matches JJK's "who can overwhelm whom" feeling

---

## 🐛 Troubleshooting

| Issue | Solution |
|-------|----------|
| Battle doesn't start | Call StartBattle() - it doesn't auto-start |
| Beams not visible | Check prefabs assigned to SimpleBeamAttackSystem |
| Beam at wrong position | Ensure character transforms assigned correctly |
| QTE not showing | Verify Canvas + UI elements created |
| Input not working | Check Space key in Input Manager |

---

## 📝 Script Quick Reference

### SimpleBattleSystem
```csharp
public void StartBattle()          // Begin battle
public bool IsBattleActive()       // Check if fighting
```

### SimpleBeamAttackSystem
```csharp
public void FireBeam(Vector3 from, Vector3 to, GameObject prefab)
public void LucioAttack(Vector3 lucioPos, Vector3 cedricPos)
public void CedricAttack(Vector3 cedricPos, Vector3 lucioPos)
public void RapidFire(Vector3 from, Vector3 to, GameObject prefab, int count, float delay)
```

---

**Now you have a simple, flexible battle system! 🚀**
