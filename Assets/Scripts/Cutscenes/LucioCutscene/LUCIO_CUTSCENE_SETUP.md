# Lucio Cutscene Setup Guide

## Overview
This folder contains the complete Lucio vs Cedric cutscene system with:
- Camera animations (zoom, flick, shake effects)
- Beam attack system with QTE-controlled intensity
- Quick Time Event (QTE) with button spam to push Lucio's beam further

## Key Mechanic
**The more you spam Space during QTE, the further Lucio's beam travels toward Cedric!**
- 0% progress: Beam barely leaves (Cedric wins)
- 50% progress: Beam halfway between characters
- 100% progress: Beam reaches Cedric (Lucio wins)

## Scene Setup Instructions

### 1. Create Characters
1. Create two 3D cubes (or 2D squares) in your scene
2. Name them `Lucio` and `Cedric`
3. Position them:
   - **Lucio**: Left side (e.g., -3, 0, 0)
   - **Cedric**: Right side (e.g., 3, 0, 0)
4. Attach `CutsceneCharacter` script to both
   - Set Character ID: 0 for Lucio, 1 for Cedric
   - Assign SpriteRenderer or get it automatically

### 2. Set Up Camera
1. Set Main Camera orthographic size to suitable value (e.g., 10)
2. Add `LucioCutsceneCameraController` script to a GameObject (or Main Camera)
3. Adjust shake settings if needed:
   - Shake Magnitude: 0.5 (default)
   - Shake Frequency: 20 (default)

### 3. Create Beam System
1. Create empty GameObject named "BeamAttackSystem"
2. Add `BeamAttackSystem` script
3. Configure beam settings:
   - Beam Width: 0.3
   - Beam Speed: 15
   - Beam Count: 2 exchanges
   - Beam Interval: 1.5 seconds

### 4. Create QTE UI
1. Create Canvas in your scene
2. Create UI Elements:
   - Image for progress bar (shows beam progress %)
   - Text for instructions (shows countdown timer)
3. Add `QuickTimeEventSystem` script to Canvas or separate GameObject
4. Assign:
   - QTE Button: Space (configurable)
   - QTE Duration: 5 seconds
   - Assign QTE UI references (CanvasGroup, Text, Progress Bar Image)

### 5. Create Main Manager
1. Create empty GameObject named "LucioCutsceneManager"
2. Add `LucioCutsceneManager` script
3. Assign all references in Inspector:
   - Camera Controller
   - Lucio Character
   - Cedric Character
   - Beam Attack System
   - Quick Time Event System

### 6. Configure Cutscene Timing
Adjust these values in LucioCutsceneManager for pacing:
- **Zoom In Duration**: 2 seconds (time to zoom to each character)
- **Center Zoom Out Duration**: 2 seconds
- **Flick Duration**: 0.5 seconds (quick camera flicks)
- **Beam Attack Duration**: 3 seconds
- **QTE Start Delay**: 0.5 seconds (delay before QTE starts)

## Script Components

### LucioCutsceneManager.cs
Main orchestrator that sequences all cutscene events.
- Registers beam progress callback from QTE
- Runs beam attack and QTE in parallel (they both happen at the same time)
- Determines winner based on final beam progress (>50% = Lucio wins)
- Automatically starts on scene load

### LucioCutsceneCameraController.cs
Handles all camera animations:
- `ZoomInToCharacters()` - Zoom to each character
- `CenterZoomOutForDepth()` - Create depth effect
- `FlickBetweenCharacters()` - Quick camera flicks
- `CameraShakeLight()` - Subtle shake during beam
- `CameraImpactPullback()` - Pull back on impact
- `CameraReturnToCenter()` - Return to center position
- `CameraFocusVictory()` / `CameraFocusDefeat()` - End focus

### CutsceneCharacter.cs
Character data holder:
- Tracks position and state
- Provides beam origin points
- Color distinction (Blue = Lucio, Red = Cedric)

### BeamAttackSystem.cs
Manages beam visual effects:
- Creates LineRenderer beams
- **Lucio's beam progress is controlled by QTE** (UpdateLucioBeamProgress callback)
- **Cedric's beam always goes full strength**
- Beam travel animation follows QTE progress update

### QuickTimeEventSystem.cs
Quick Time Event system:
- Player spams button to increase beam progress
- Each button press increases progress by 5%
- Shows live percentage and countdown timer
- Callback system: notifies BeamAttackSystem of progress changes in real-time
- `GetFinalBeamProgress()` returns final progress (0-1)

## Cutscene Flow

1. **Zoom In** (2s): Camera zooms to Lucio, then Cedric
2. **Depth Creation** (2s): Camera zooms out from center
3. **Flick** (0.5s): Quick camera flicks between characters
4. **Beam Attack + QTE** (Run in parallel for 3s):
   - Lucio fires beam → distance controlled by QTE progress
   - Cedric counter-attacks with full strength
   - Camera shakes lightly throughout
   - **Player must SPAM SPACE to push Lucio's beam further**
5. **Winner Determined**:
   - If Lucio's beam reached >50% distance: Lucio wins
   - Otherwise: Cedric wins
6. **End**: Camera focuses on winner

## Customization Tips

### QTE Difficulty
- Decrease `keyPressThrottleTime` to allow faster input (default 0.05s)
- Increase `qteDuration` to give more time (default 5s)
- Each button press adds 5% progress - change by modifying `0.05f` in QuickTimeEventSystem

### Camera Shake Intensity
Increase `Shake Magnitude` for more impact, decrease for subtlety.

### Beam Visuals
- Change beam colors in `CreateBeamRenderer()`
- Adjust `beamWidth` for thicker/thinner beams

### Win/Loss Threshold
In LucioCutsceneManager's QuickTimeEventSequence:
- Change `beamProgress > 0.5f` to different threshold (e.g., `> 0.6f` for harder)

### Timing
Adjust individual durations in LucioCutsceneManager for your desired pacing.

## Testing

1. Create a test scene with the setup above
2. Press Play
3. Cutscene will auto-start
4. During QTE, spam Space to push Lucio's beam towards Cedric
5. At >50% progress, Lucio wins!
6. Observe camera movement and beam progress visuals

## Real-Time Feedback

The UI shows:
- Progress bar: Fills as you spam (shows beam distance %)
- Text: "Spam Space! Beam Power: XX% Time: 4.3s"

The beam visually travels based on current QTE progress in real-time!

## Future Enhancements

- Add particle effects for beams
- Add sound effects and music
- Add character animation states
- Add damage numbers or visual feedback
- Add screen shake on beam impact
- Support for different victory/defeat outcomes
- Screen flash/impact effect when beams collide

