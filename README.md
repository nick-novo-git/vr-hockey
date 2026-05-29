# VR Hockey

Unity prototype for Meta Quest VR hockey stickhandling.

The game is built around a Quest controller mounted to a real hockey stick. The controller is treated as a tracked reference point on the shaft, while the game projects a virtual stick blade, puck, rink, and incoming obstacles for stickhandling practice.

## Current Features

- Meta Quest Android/OpenXR prototype scene
- Mounted-controller stick tracking
- Automatic rest calibration before gameplay starts
- 3D hockey stick visual aligned to the tracked shaft
- Thin blue physics blade reference
- Puck handling and reset behavior
- Infinite obstacle dodging with increasing level difficulty
- World-space score, timer, hits, and level display
- VR menu with Play and handedness selection

## Project Layout

- `Assets/HockeyStickhandling/Scripts` - gameplay, tracking, menu, puck, and obstacle code
- `Assets/HockeyStickhandling/Resources/Models` - hockey stick OBJ assets and textures
- `Assets/HockeyStickhandling/Editor` - scene creation and Android build setup
- `Assets/HockeyStickhandling/PrototypeScene.unity` - generated prototype scene
- `StableBaselines` - saved checkpoints from known-good prototype states

## Unity Version

This project has been developed with Unity `2022.3.49f1`.

Required Unity modules:

- Android Build Support
- Android SDK/NDK Tools
- OpenJDK

## Build

The editor build script configures Android/OpenXR settings and writes:

`Builds/Android/QuestHockeyStickhandling.apk`

Command-line build:

```sh
/Applications/Unity/Hub/Editor/2022.3.49f1/Unity.app/Contents/MacOS/Unity \
  -batchmode \
  -quit \
  -projectPath "/path/to/vr-hockey" \
  -executeMethod HockeyStickhandlingEditor.AndroidQuestBuildSetup.BuildAndroidApk \
  -logFile /tmp/hockey_android_build.log
```

Install to a connected Quest:

```sh
/Applications/Unity/Hub/Editor/2022.3.49f1/PlaybackEngines/AndroidPlayer/SDK/platform-tools/adb install -r Builds/Android/QuestHockeyStickhandling.apk
```

## Hardware Notes

- Target headset: Meta Quest 3S
- Stick tracker: mounted Quest controller
- The controller mount position and orientation are still being tuned.
- The current prototype preserves a simple blue physics blade while a 3D stick model is aligned visually.

## Git Ignore

Unity-generated folders such as `Library`, `Builds`, `Logs`, and `UserSettings` are intentionally ignored.
