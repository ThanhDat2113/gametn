# Lucio Cutscene - Quick Start Setup Guide

## 📋 Overview
This is a complete cutscene system featuring **Lucio vs Cedric** with interactive QTE mechanic:
- **Spam Space** during the battle to make Lucio's beam push toward Cedric
- More spam = beam travels further
- **>50% progress = Lucio wins**, otherwise Cedric wins
- Dynamic camera with shakes, zooms, and flicks for an epic feel

---

## ⚡ Quick Setup (5 Minutes)

### Option A: Use Auto-Setup Helper (Recommended)
1. Create an empty GameObject in your scene
2. Add `LucioCutsceneSetupHelper` script to it
3. In Inspector, right-click → **Setup Lucio Cutscene**
4. Wait for setup to complete
5. Check Console for confirmation

**That's it!** The helper will auto-create all components.

---

### Option B: Manual Setup

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
3. Keep default beam settings

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

## ▶️ Testing

1. **Play** the scene
2. Cutscene starts automatically
3. Camera **zooms** to each character
4. QTE starts - **SPAM SPACE** during the beam battle
5. Watch your spam fill the progress bar!
6. Winner determined at the end

---

## 🎮 How It Works

### Cutscene Sequence:
```
[1] Zoom In          (2s) - Camera focuses on each character
  ↓
[2] Zoom Out         (2s) - Create depth effect
  ↓
[3] Quick Flick      (0.5s) - Rapid camera movement
  ↓
[4] BEAM BATTLE      (3s) - Spam Space!
    ├─ Lucio's beam: Controlled by your input
    ├─ Cedric's beam: Counter-attack (full power)
    └─ Camera: Shakes for impact
  ↓
[5] Winner Focus     (1.5s) - Camera shows victor
  ↓
[6] End              - Cutscene complete
```

### Beam Progress System:
- **Each Space press**: +5% progress
- **Progress > 50%**: Lucio wins
- **Progress ≤ 50%**: Cedric wins
- **Real-time feedback**: Progress bar fills as you spam

---

## ⚙️ Customization

### Make QTE Harder:
```csharp
// In LucioCutsceneManager
[SerializeField] private float qteDuration = 3f; // Reduce time
[SerializeField] private float qteStartDelay = 0f; // Start immediately
```

### Make QTE Easier:
```csharp
// In QuickTimeEventSystem
// Increase progress per press: 0.05f → 0.1f or higher
currentBeamProgress = Mathf.Min(1f, currentBeamProgress + 0.1f);
```

### Adjust Camera Shake:
```csharp
// In LucioCutsceneCameraController
[SerializeField] private float shakeMagnitude = 1f; // Increase from 0.5f
[SerializeField] private float shakeFrequency = 30f; // More frequent shake
```

### Adjust Beam Win Threshold:
```csharp
// In LucioCutsceneManager QuickTimeEventSequence()
bool lucioWins = beamProgress > 0.6f; // Change from 0.5f
```

### Change Colors:
- Lucio Beam: **Cyan** (change in BeamAttackSystem.cs)
- Cedric Beam: **Yellow** (change in BeamAttackSystem.cs)
- Lucio Character: **Blue** (set in CutsceneCharacter.cs)
- Cedric Character: **Red** (set in CutsceneCharacter.cs)

---

## 🐛 Troubleshooting

### Cutscene doesn't start
- ✅ Check if `LucioCutsceneManager` exists in scene
- ✅ Verify all references are assigned
- ✅ Check Console for errors

### QTE UI not showing
- ✅ Verify Canvas is set to `ScreenSpaceOverlay`
- ✅ Check CanvasGroup is assigned in QTESystem
- ✅ Verify Text and ProgressBar references are set

### Beam not moving with input
- ✅ Check if `QuickTimeEventSystem.SetBeamProgressCallback()` is called
- ✅ Verify Space key input is detected (check Console spam)
- ✅ Make sure `BeamAttackSystem.UpdateLucioBeamProgress()` is called

### Camera not shaking
- ✅ Verify `LucioCutsceneCameraController` is attached
- ✅ Check `shakeMagnitude` is > 0
- ✅ Ensure Main Camera tag is set correctly

---

## 📁 File Structure
```
Assets/Scripts/Cutscenes/LucioCutscene/
├── LucioCutsceneManager.cs           (Main orchestrator)
├── LucioCutsceneCameraController.cs  (Camera effects)
├── CutsceneCharacter.cs              (Character data)
├── BeamAttackSystem.cs               (Beam visuals)
├── QuickTimeEventSystem.cs           (QTE mechanics)
├── LucioCutsceneSetupHelper.cs       (Auto-setup tool)
├── LUCIO_CUTSCENE_SETUP.md           (Detailed docs)
├── QUICK_START_SETUP.md              (This file)
```

---

## 🎯 Key Features

✅ **Real-time Beam Control** - Input directly affects beam distance  
✅ **Dynamic Camera** - Zoom, shake, flick for cinematic feel  
✅ **Progressive Difficulty** - Easy to hard just by spamming less/more  
✅ **Visual Feedback** - Progress bar shows real-time performance  
✅ **Flexible Timing** - All durations customizable  
✅ **Both Characters** - Lucio QTE-controlled, Cedric AI counterattack  

---

## 💡 Tips

1. **For more visceral combat**: Increase camera shake magnitude
2. **For more forgiving QTE**: Decrease qteDuration or increase progress per press
3. **For epic feeling**: Keep default timings - they're balanced
4. **For quick testing**: Run cutscene on scene load automatically ✓

---

## 📞 Support

Check the following if issues arise:
- Console warnings/errors
- Inspector references all assigned
- All GameObjects named correctly
- Canvas properly configured
- Main Camera tag set

---

**Happy Testing! Spam that Space key! 🚀**
