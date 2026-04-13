# Changelog

All notable changes to this package will be documented in this file.

The format is based on [Keep a Changelog](http://keepachangelog.com/en/1.0.0/).
This project adheres to [Semantic Versioning](http://semver.org/spec/v2.0.0.html).

## [3.2.2] - 2026-03-20

### Fixed

- Added support for Unity 6.4

## [3.2.0] - 2025-05-13

### Added

- Support for Unity 6!

## [3.1.3] - 2024-06-28

This version of Motion Blur is compatible with Unity 2022.3.0f1.

### Fixed

- Fixed an issue causing warnings about cameraColorTarget usage.

## [3.1.2] - 2024-01-29

This version of Motion Blur is compatible with Unity 2022.3.0f1.

### Fixed

- Fixed a potential division by zero.

## [3.1.1] - 2023-11-17

This version of Motion Blur is compatible with Unity 2022.3.0f1.

### Fixed

- Fixed an issue with TAA compatibility.
- Fixed an issue causing warnings in the inspector when masking set to off.

## [3.1.0] - 2023-11-14

This version of Motion Blur is compatible with Unity 2022.3.0f1.

### Added

- Added support for object masking.

### Changed

- Improved Editor GUI.
- Changed default Render Pass Event to After Rendering Post Processing.

### Fixed

- Fixed an error that was appearing when attempting to use the _MotionVectorTexture before it was available

## [3.0.3] - 2023-09-09

This version of Motion Blur is compatible with Unity 2022.3.0f1.

### Fixed

- Fixed an issue with Depth Separation.
- Fixed an issue where Motion Blur was causing Transparent Depth Write to fail.

## [3.0.2] - 2023-08-11

This version of Motion Blur is compatible with Unity 2022.3.0f1.

### Changed

- Various performance improvements

## [3.0.1] - 2023-08-11

This version of Motion Blur is compatible with Unity 2022.3.0f1.

### Fixed

- Fixed an issue causing the motion vector texture to render blank.
- Fixed an issue causing the motion Depth Separation Mode to work incorrectly.

### Added

- Added editor checks to ensure Depth Separation Distance and Shutter Speed never go below 0.

## [3.0.0] - 2023-07-21

This version of Motion Blur is compatible with Unity 2022.3.0f1.

### Added

- Added settings menu to control Render Pass Event option.
- This asset has always been incompatible with OpenGLES3, but the Compute Shader didn't specifically exclude that renderer. This led to console warnings. We've added a #pragma exclude_renderers to omit that renderer, thereby avoiding the warnings and clarifying renderer support.

### Changed

- Migrated to RTHandle and Blitter APIs
- Unity was rendering Motion Vectors after the Motion Blur pass was executing. We now render our own copy of the Motion Vector texture to guarantee that it is available for our Motion Blur pass.

## [2.0.0] - 2023-05-22

This version of Motion Blur is compatible with Unity 2021.3.0f1.

### Changed

- Switched to Package style. Recommend you to delete the /Assets/OccaSoftware/MotionBlur/~ directory before upgrading.
- Moved Motion Blur override from Post-Processing -> Better Motion Blur to OccaSoftware -> Better Motion Blur.

## [1.3.1] - 2023-05-01

- Fixed an issue with the dithering system.

## [1.3.0] - 2023-04-27

- Added dithering to the motion blur sampling. Motion Blur now looks smoother and has less visible banding.
- Changed the screen edge detection algorithm. Motion Blur now correctly samples edge pixels.

## [1.2.0] - 2023-04-18

- Added depth separation feature.
- Added properties to control depth separation mode and depth separation distance.

## [1.1.0] - 2023-04-18

- Various improvements

## [1.0.0] - 2023-02-28

- Initial release
