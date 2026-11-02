# Mirage Run

A 3D endless desert runner built with **Unity 6** and **C#**. Switch lanes, jump over rocks and crates, dodge cacti and trees, and collect coins and gems while the desert gets faster the longer you survive.

> Capstone project for Game Design and Development, School of Computer Science and Artificial Intelligence, SR University, 2025-2026.

![Gameplay](docs/screenshots/gameplay.png)

## Features

- Three-lane movement with single-tap lane switching
- Jump with a buffered input window
- Endless track generated ahead of the player, with old segments removed behind them
- Six hand-built segment layouts with obstacles, coin arcs and gems
- Speed that ramps from 11 m/s to 26 m/s over about two minutes
- Live HUD for gems, coins, distance and score
- Countdown start, pause menu, Game Over screen with a "new best" message
- Saved coins, gems, best distance and best score through `PlayerPrefs`
- Four scenes: Main Menu, Stage Select, Information, Gameplay
- Sound effects, ambient wind and a music loop, with a mute toggle
- URP visuals: procedural sky, ACES tonemapping, bloom, vignette, PBR sand and rock textures, dune meshes, pickup particles and running dust
- Screen fades between scenes

## Controls

| Action | Keys |
| --- | --- |
| Move left | `A` or `Left Arrow` |
| Move right | `D` or `Right Arrow` |
| Jump | `Space`, `W` or `Up Arrow` |
| Pause | `P` or `Esc` |
| Mute | `M` |

## Requirements

- Unity **6000.5.8f1** (Unity 6) or newer
- Universal Render Pipeline (the 3D URP project template)
- TextMeshPro essential resources

## Getting started

1. Create or open a Unity 6 project from the **3D (URP)** template.
2. Copy this repository's `Assets` folder into your project's `Assets` folder.
3. Import TextMeshPro essentials if Unity asks: **Window > TextMeshPro > Import TMP Essential Resources**.
4. Make sure a URP pipeline asset with a renderer is assigned under **Edit > Project Settings > Graphics** and in each **Quality** level.
5. Run **Mirage Run > Build Everything** from the Unity menu bar. This generates the scenes, prefabs, materials, UI and build settings.
   - If it reports that the input setting changed, restart Unity and run it again. The scripts use the legacy `Input` API, so **Active Input Handling** must be set to **Both**.
6. Open `Assets/Scenes/MainMenu` and press **Play**.

Build order is `MainMenu (0)`, `StageSelect (1)`, `Information (2)`, `GamePlay (3)`.

## Using a rigged character

The supplied character mesh has no skeleton, so by default the hero uses a procedural run bounce. For a real running animation:

1. Download a character from [Mixamo](https://www.mixamo.com) with the **Running** animation, as FBX Binary, **With Skin**.
2. Save it as `Assets/Models/Character/Hero_Running.fbx`.
3. Run **Mirage Run > Build Everything** again.

If your own mesh is rigged, save it as `Assets/Models/Character/Hero.fbx` and keep `Assets/Animations/Running.fbx`. If the hero faces the camera while running, use **Build Everything (turn hero 180 degrees)**.

## Project structure

```
Assets/
  Animations/    Running animation and generated Animator controller
  Audio/         Sound effects, wind loop and music
  Editor/        MirageRunBuilder, the scene and prefab generator
  Materials/     Generated URP materials and post-processing profile
  Models/        Character and environment meshes
  Prefabs/       Player, start segment and desert segments
  Scenes/        MainMenu, StageSelect, Information, GamePlay
  Scripts/       Gameplay scripts
  Textures/      Character and desert textures
```

## Scripts

| Script | Purpose |
| --- | --- |
| `PlayerMovement` | Lane switching, jumping, forward movement, death fall |
| `GameManager` | Game state, speed ramp, countdown, pause, Game Over, saving |
| `SegmentGenerator` | Spawns track segments ahead and removes them behind the player |
| `Collectible` | Coin and gem pickup with spin, rise, shrink and particle burst |
| `Obstacle` | Ends the run when the player touches it |
| `MasterLevelInfo` | Coin, gem, distance and score counters and HUD text |
| `CameraFollow` | Follow camera with field-of-view change and shake |
| `AudioManager` | Persistent audio, sound effects and mute |
| `ScreenFader` | Fade in and out between scenes |
| `MenuControl` | Menu buttons, saved progress display, quit |
| `AutoLoadScene` | Loading bar on the Information scene |
| `PlayerDust`, `Fx` | Running dust and pickup particle effects |
| `RunBob`, `Spin`, `MenuCameraSway` | Fallback run animation and menu motion |

## Team

| Name | Roll No. |
| --- | --- |
| _Add team member_ | _Roll number_ |

**Guide:** Dr. Vijaya Chandra Jadala, Assistant Professor, School of CS&AI, SR University

## Credits

- Running animation and character rig: [Mixamo](https://www.mixamo.com)
- Sound effects, ambience and music loop: generated procedurally for this project
- Sand, rock and wood textures, dune and rock meshes: generated procedurally for this project

## License

Add a license before publishing. The MIT License is a common choice for student projects.
