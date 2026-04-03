# Lucio Cutscene - Complete Setup Guide

## 📋 Overview
This is a complete cutscene system featuring **Lucio vs Cedric** with interactive QTE mechanic:
- **Spam Space** during the battle to make Lucio's beam push toward Cedric
- More spam = beam travels further (VFX scale on X axis)
- **>50% progress = Lucio wins**, otherwise Cedric wins
- Dynamic camera with shakes, zooms, and flicks for an epic feel

---

## ⚡ Quick Setup (5 Minutes)

### Option A: Use Auto-Setup Helper (EASIEST - Recommended)

#### Step-by-Step:

**1. Create Setup Manager in Scene**
   - Right-click trong Hierarchy → `Create Empty`
   - Đặt tên: `LucioCutsceneSetupManager` (hoặc tên gì cũng được)

**2. Add Script**
   - Select GameObject vừa tạo
   - Trong Inspector → `Add Component`
   - Tìm: `LucioCutsceneSetupHelper`
   - Click để thêm

**3. Configure Setup Options (Tùy chọn)**
   - Mở rộng section **Auto-Setup Options**
   - Các toggle này (mặc định đã check ✓):
     - ✓ Auto Create Characters (tạo Lucio & Cedric)
     - ✓ Auto Create Camera Controller (add vào Main Camera)
     - ✓ Auto Create Beam System (tạo GameObject beam)
     - ✓ Auto Create QTEU (tạo UI Canvas + elements)

**4. Run Setup**
   - Click nút **Setup Lucio Cutscene** trong Inspector
   - Hoặc: Right-click trên component → `Setup Lucio Cutscene`
   - **CHỜ** 2-3 giây để script tự tạo tất cả

**5. Assign Beam Prefabs (QUAN TRỌNG!)**
   - Tìm `BeamAttackSystem` trong Hierarchy
   - Click vào nó
   - Trong Inspector, tìm section **Beam Prefabs**
   - Drag 2 prefab VFX beam của bạn vào:
     - `Lucio Beam Prefab`
     - `Cedric Beam Prefab`

**6. Play!**
   - Press Play
   - Cutscene chạy tự động
   - **SPAM SPACE** khi QTE bắt đầu

**That's it! 🎉**

---

#### What Gets Created Automatically?

Sau khi setup, script sẽ tạo ra những thứ này trong Hierarchy:

```
Scene Root
├── Lucio (Blue Cube)
│   └── CutsceneCharacter script
├── Cedric (Red Cube)
│   └── CutsceneCharacter script
├── BeamAttackSystem (Empty)
│   └── BeamAttackSystem script
├── LucioCutsceneManager (Empty)
│   └── LucioCutsceneManager script
└── Main Camera (Updated)
    └── LucioCutsceneCameraController script

Canvas (Updated hoặc tạo mới)
└── QTEPanel
    ├── ProgressBarBg
    │   └── ProgressBar (Fill)
    └── InstructionText
```

**Các file được tạo:**
- ✅ 2 Nhân vật (Lucio & Cedric)
- ✅ Camera controller (thêm vào Main Camera)
- ✅ Beam system (GameObject trống chưa có prefab)
- ✅ QTE UI (Canvas + UI elements)
- ✅ Cutscene manager (Main orchestrator)

**Điều duy nhất bạn phải làm:**
- 👉 **Assign 2 beam prefab vào BeamAttackSystem**

---

### Option B: Manual Setup (Detailed)

#### Step 1: Create Characters
1. Create two **3D Cubes** in the scene
2. Name them `Lucio` (Blue) and `Cedric` (Red)
3. Position:
   - **Lucio**: `(-3, 0, 0)`
   - **Cedric**: `(3, 0, 0)`
4. Scale both to `(1, 1, 0.1)` for flat appearance
5. Add **CutsceneCharacter** script to each:
   - Lucio: CharacterID = **0**
   - Cedric: CharacterID = **1**
6. Assign or auto-get **SpriteRenderer**

#### Step 2: Setup Camera
1. Select **Main Camera**
2. Set **Orthographic Size** to `10`
3. Add **LucioCutsceneCameraController** script
4. Keep default shake settings

#### Step 3: Create Beam System
1. Create empty GameObject: `BeamAttackSystem`
2. Add **BeamAttackSystem** script
3. **IMPORTANT**: Assign your 2 beam VFX prefabs:
   - **Lucio Beam Prefab**: Your Lucio beam VFX
   - **Cedric Beam Prefab**: Your Cedric beam VFX
4. Configure beam settings:
   - **Max Beam Scale**: `10` (adjust for your prefab size)
   - **Beam Duration**: `0.5s` (how long beam displays)

#### Step 4: Create QTE UI
1. **If no Canvas exists**: Create one (`RenderMode: ScreenSpaceOverlay`)

2. **Create QTE Panel** (Child of Canvas):
   - Add **Image** → Transparent background
   - Add **CanvasGroup**
   - Anchor: Stretch (full screen)

3. **Create Progress Bar** (Child of Panel):
   - Create background **Image** → Gray color
   - Position: Center bottom (`0, -50`)
   - Size: `(300, 50)`
   
4. **Create Progress Fill** (Child of Progress Background):
   - Add **Image** → Green color
   - Anchor Left: `(0, 0.5)`
   - Anchor Right: `(0, 0.5)`

5. **Create Text** (Child of Panel):
   - Add **Text** component
   - Font: Arial (built-in)
   - Font Size: 40
   - Color: White
   - Alignment: Center

6. **Add QTE System**:
   - Create empty GameObject: `QTESystem`
   - Add **QuickTimeEventSystem** script
   - Call `SetUIReferences()` or assign in Inspector

#### Step 5: Create Manager
1. Create empty GameObject: `LucioCutsceneManager`
2. Add **LucioCutsceneManager** script
3. Assign in Inspector:
   - Camera Controller
   - Lucio Character
   - Cedric Character
   - Beam Attack System
   - Quick Time Event System

#### Step 6: Configure Timing (Optional)
Adjust durations in **LucioCutsceneManager**:
- `zoomInDuration`: 2s (default)
- `centerZoomOutDuration`: 2s (default)
- `flickDuration`: 0.5s (default)
- `beamAttackDuration`: 3s (default)
- `qteStartDelay`: 0.5s (default)

---

## 🎬 Beam VFX System Details

### How It Works

**Lucio's Beam (QTE Controlled)**
- Scale increases from 0 to `maxBeamScale` as you spam Space
- Real-time feedback: Each key press = immediate scale increase
- Direction: Lucio → Cedric

**Cedric's Beam (Counter-attack)**
- Scale fixed at 100% (maximum)
- Direction: Cedric → Lucio

### Scale Mechanics

If `maxBeamScale = 10`:

| QTE Progress | Beam Scale X | Visual |
|---|---|---|
| 0% | 0 | Not visible |
| 25% | 2.5 | Short |
| 50% | 5 | Halfway |
| 75% | 7.5 | Nearly hit |
| 100% | 10 | Maximum |

### Real-time Update Flow

```
Player spam Space
    ↓
QuickTimeEventSystem.UpdateLucioBeamProgress(progress)
    ↓
BeamAttackSystem.UpdateLucioBeamProgress(progress)
    ↓
lucioBeamInstance.scale.x = progress * maxBeamScale
    ↓
Beam VFX expands in real-time!
```

### Prefab Requirements

Your beam VFX prefabs should have:

1. **Local scale default (1, 1, 1)**
   - Script changes X axis only: `(progress * maxBeamScale, 1, 1)`

2. **No strange parent transforms**
   - Instantiated at character position

3. **Default direction: +X (right)**
   - Script rotates based on source → target direction

4. **Proper pivot point**
   - For expanding beam: pivot should be on the **left side**
   - This makes beam grow to the right when X scale increases

---

## ▶️ Testing

1. **Play** the scene
2. Cutscene starts automatically
3. Camera **zooms** to each character
4. QTE starts - **SPAM SPACE** during the beam battle
5. Watch your spam fill the progress bar and beam grows!
6. At >50% progress, Lucio wins!
7. Observe camera movement and beam VFX scaling

---

## 🎮 Cutscene Sequence

```
[1] Zoom In          (2s) - Camera focuses on each character
  ↓
[2] Zoom Out         (2s) - Create depth effect
  ↓
[3] Quick Flick      (0.5s) - Rapid camera movement
  ↓
[4] BEAM BATTLE      (3s) - Spam Space!
    ├─ Lucio's beam: Controlled by your input (scale grows)
    ├─ Cedric's beam: Counter-attack (full scale)
    └─ Camera: Shakes for impact
  ↓
[5] Winner Focus     (1.5s) - Camera shows victor
  ↓
[6] End              - Cutscene complete
```

---

## ⚙️ Customization

### Make QTE Harder

**Reduce time:**
```csharp
// In LucioCutsceneManager
[SerializeField] private float qteDuration = 3f; // Reduce from 5f
```

**Reduce progress per press:**
```csharp
// In QuickTimeEventSystem.cs line ~58
currentBeamProgress = Mathf.Min(1f, currentBeamProgress + 0.02f); // Reduce from 0.05f
```

### Make QTE Easier

**Increase progress per press:**
```csharp
// In QuickTimeEventSystem.cs
currentBeamProgress = Mathf.Min(1f, currentBeamProgress + 0.1f); // Increase from 0.05f
```

**Increase time:**
```csharp
[SerializeField] private float qteDuration = 7f; // Increase from 5f
```

### Adjust Camera Shake

**More intense:**
```csharp
// In LucioCutsceneCameraController
[SerializeField] private float shakeMagnitude = 1f; // Increase from 0.5f
[SerializeField] private float shakeFrequency = 30f; // Increase from 20f
```

**Less intense:**
```csharp
[SerializeField] private float shakeMagnitude = 0.2f; // Decrease from 0.5f
```

### Adjust Beam Win Threshold

Make it easier to win:
```csharp
// In LucioCutsceneManager.cs QuickTimeEventSequence()
bool lucioWins = beamProgress > 0.4f; // Lower from 0.5f (easier)
```

Make it harder:
```csharp
bool lucioWins = beamProgress > 0.7f; // Higher from 0.5f (harder)
```

### Adjust Beam Scale

**Smaller beams:**
```csharp
// In BeamAttackSystem
[SerializeField] private float maxBeamScale = 5f; // Reduce from 10f
```

**Larger beams:**
```csharp
[SerializeField] private float maxBeamScale = 15f; // Increase from 10f
```

### Change Beam Duration

How long beams stay on screen:
```csharp
[SerializeField] private float beamDuration = 1f; // Increase from 0.5f for longer display
```

### Change Beam Colors

In **BeamAttackSystem**, find `CreateBeamRenderer()` and change colors:
```csharp
lr.startColor = name.Contains("Lucio") ? Color.cyan : Color.yellow;
```

---

## 🐛 Troubleshooting

### Cutscene doesn't start
- ✅ Check if `LucioCutsceneManager` exists in scene
- ✅ Verify all references are assigned (Inspector)
- ✅ Check Console for errors

### QTE UI not showing
- ✅ Verify Canvas is set to `RenderMode: ScreenSpaceOverlay`
- ✅ Check CanvasGroup is assigned in QTESystem
- ✅ Verify Text and ProgressBar references are set
- ✅ Check UI elements are child of Canvas

### Beam not visible
- ✅ Prefabs assigned to BeamAttackSystem?
- ✅ Prefabs are active in scene?
- ✅ Check Console for instantiation errors

### Beam not scaling
- ✅ Is `maxBeamScale > 0`?
- ✅ Is QTE running? (watch progress bar fill)
- ✅ Check if beam prefab has proper pivot (left side for growing right)

### Beam at wrong position
- ✅ Prefab instantiated at `source.GetPosition()`
- ✅ Check character positions are correct
- ✅ Add offset in prefab if needed

### Camera not shaking
- ✅ Verify `LucioCutsceneCameraController` attached
- ✅ Check `shakeMagnitude > 0`
- ✅ Ensure Main Camera tag set correctly

### Input not registering
- ✅ Is Space key set correctly? (default: Space)
- ✅ Check `keyPressThrottleTime` in QuickTimeEventSystem (default: 0.05s)
- ✅ Watch Console: add debug logs to verify inputs

---

## 📁 File Structure
```
Assets/Scripts/Cutscenes/LucioCutscene/
├── LucioCutsceneManager.cs              (Main orchestrator)
├── LucioCutsceneCameraController.cs     (Camera effects)
├── CutsceneCharacter.cs                 (Character data)
├── BeamAttackSystem.cs                  (Beam VFX control)
├── QuickTimeEventSystem.cs              (QTE mechanics)
├── LucioCutsceneSetupHelper.cs          (Auto-setup tool)
├── LUCIO_CUTSCENE_COMPLETE_SETUP.md     (This file)
├── LUCIO_CUTSCENE_SETUP.md              (Detailed tech docs)
```

---

## 🎯 Key Features

✅ **Real-time Beam Control** - Input directly affects beam scale  
✅ **VFX Prefab System** - Use your custom beam visuals  
✅ **Dynamic Camera** - Zoom, shake, flick for cinematic feel  
✅ **Progressive Difficulty** - Easy to hard by adjusting thresholds  
✅ **Visual Feedback** - Progress bar shows real-time performance  
✅ **Flexible Timing** - All durations customizable  
✅ **Both Characters** - Lucio QTE-controlled, Cedric AI counterattack  
✅ **Auto-Setup** - One-click setup helper included  

---

## Script Components

### LucioCutsceneManager.cs
Main orchestrator that sequences all events:
- Registers beam progress callback
- Runs beam attack and QTE in parallel
- Determines winner based on beam progress (>50% = win)
- Auto-starts on scene load

### LucioCutsceneCameraController.cs
Handles all camera animations:
- `ZoomInToCharacters()` - Zoom to each character
- `CenterZoomOutForDepth()` - Create depth
- `FlickBetweenCharacters()` - Quick flicks
- `CameraShakeLight()` - Shake effect
- `CameraImpactPullback()` - Pull back on impact
- `CameraReturnToCenter()` - Return to center
- `CameraFocusVictory()` / `CameraFocusDefeat()` - End focus

### CutsceneCharacter.cs
Character data holder:
- Tracks position and state
- Provides beam origin points
- Color distinction (Blue = Lucio, Red = Cedric)

### BeamAttackSystem.cs
Manages beam VFX prefabs:
- Instantiates prefabs at character positions
- **Lucio's beam**: Scale controlled by QTE progress (real-time)
- **Cedric's beam**: Scale always at 100%
- Updates scale on X axis: `scale.x = progress * maxBeamScale`
- Destroys beams after duration

### QuickTimeEventSystem.cs
Quick Time Event system:
- Player spams button to increase beam progress
- Each button press increases progress by 5% (configurable)
- Real-time callback notifies BeamAttackSystem
- Shows live percentage and countdown timer
- `GetFinalBeamProgress()` returns final progress (0-1)
- `SetUIReferences()` assigns UI elements

### LucioCutsceneSetupHelper.cs
Auto-setup tool:
- Creates all necessary GameObjects
- Adds all required scripts
- Configures basic settings
- Access via right-click → "Setup Lucio Cutscene"

---

## 💡 Pro Tips

1. **For visceral combat**: Increase camera shake magnitude
2. **For forgiving QTE**: Decrease qteDuration or increase progress per press
3. **For epic feel**: Keep default timings - they're carefully balanced
4. **For debugging**: Add Debug.Log in UpdateLucioBeamProgress to see real-time values
5. **For smooth VFX**: Ensure prefab pivot is correct before instantiation
6. **For better visuals**: Use particle effects within your beam prefabs
7. **For audio**: Add beam fire sounds in BeamAttackSystem.FireControlledBeam()

---

## 🔗 Quick Reference

### Most Changed Settings

```csharp
// Beam Settings (BeamAttackSystem)
maxBeamScale = 10f;           // Max VFX scale on X
beamDuration = 0.5f;          // Duration beam shows

// Timing (LucioCutsceneManager)
beamAttackDuration = 3f;      // Total beam time
qteStartDelay = 0.5f;         // Delay before QTE

// QTE (QuickTimeEventSystem)
qteDuration = 5f;             // Time player has
keyPressThrottleTime = 0.05f; // Min time between inputs
// Progress per press: 0.05f (set in code)

// Win Condition (LucioCutsceneManager)
beamProgress > 0.5f           // Win threshold
```

---

## 📞 Quick Help

| Issue | Solution |
|-------|----------|
| Nothing happens | Check LucioCutsceneManager assigned in scene |
| UI missing | Verify CanvasGroup and UI elements assigned |
| Beam invisible | Confirm prefabs assigned to BeamAttackSystem |
| Beam not scaling | Check maxBeamScale > 0 and prefab has correct pivot |
| Camera not moving | Verify LucioCutsceneCameraController attached to Main Camera |
| Input not working | Check Space key in Project Settings > Input Manager |

---

**Ready to test? Play the scene and spam that Space key! 🚀**

For detailed technical documentation, see [LUCIO_CUTSCENE_SETUP.md](LUCIO_CUTSCENE_SETUP.md)
