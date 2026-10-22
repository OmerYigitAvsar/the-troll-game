# The Troll Game

The Troll Game is a 2D platforming challenge built with Unity 6. The project combines conventional movement and jumping with traps that alter platforms, hazards, controls, movement, and gravity.

## Features

- 48 authored level objects contained in the main gameplay scene
- Ten themed level groups covering different trap types
- Keyboard and on-screen mobile controls
- Variable-height jumping with coyote time and input buffering
- Automatic restart at the current level's spawn point after touching a hazard
- Attempt counter and level-progress display
- Main menu, level selection, continue, reset, and completion interfaces
- Progress, unlocked levels, and attempt count saved with Unity PlayerPrefs
- Reusable trigger-based trap architecture
- Custom Unity Editor level-authoring utility

Trap systems represented in the project include rising and moving spikes, falling and disappearing platforms, moving objects, fake or shifting exits, reversed controls, disabled jumping, slippery or slowed movement, and gravity changes.

## Controls

| Action | Keyboard | Mobile |
| --- | --- | --- |
| Move left | `A` or `Left Arrow` | Left button |
| Move right | `D` or `Right Arrow` | Right button |
| Jump | `Space` or `Up Arrow` | Jump button |
| Return to menu | `Escape` | Menu UI |

## Requirements

- Unity `6000.3.8f1`
- Universal Render Pipeline `17.3.0`
- Unity 2D packages listed in `Packages/manifest.json`

The exact editor version is recorded in `ProjectSettings/ProjectVersion.txt`. Package versions are locked in `Packages/packages-lock.json`.

## Getting Started

1. Clone the repository.
2. Open Unity Hub and choose **Add project from disk**.
3. Select the repository root.
4. Open the project with Unity `6000.3.8f1` and allow Unity to restore packages and regenerate local cache files.
5. Open `Assets/Scenes/OYUNLAR/OMER/SinirBozucu/SinirBozucu.unity` and enter Play mode.

## Project Structure

```text
Assets/
  Scenes/OYUNLAR/OMER/SinirBozucu/
    Art/             Character and pixel-world art and animations
    Editor/          Level-authoring editor utility
    Prefabs/         Gameplay platforms, hazards, boundaries, and exits
    Scripts/         Player, level, trap, camera, and UI logic
  Settings/          2D renderer and Universal Render Pipeline settings
Packages/            Unity package manifest and lock file
ProjectSettings/     Shared Unity project configuration
```

Unity-generated caches, local editor preferences, IDE files, build outputs, and signing material are intentionally excluded from version control. Unity `.meta` files remain tracked because they preserve asset GUIDs and references.

## Build Settings Note

The gameplay scene is not currently enabled in `ProjectSettings/EditorBuildSettings.asset`; only `Assets/Scenes/SampleScene.unity` is enabled. Add `SinirBozucu.unity` to the build configuration before creating a distributable build.

## Asset Licensing

Before distributing the game, verify that every included font, image, sound, and other third-party asset has an appropriate license for the intended use.
