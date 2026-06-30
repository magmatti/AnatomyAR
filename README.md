# Anatomy AR

AnatomyAR is a Unity AR application for exploring anatomical skeleton models through body and hand tracking.
It is built around AR Foundation, ARKit and a small native [iOS Vision](https://developer.apple.com/documentation/vision) plugin for hand pose detection.

## Features

- Body tracking scene follows ARKit human body joints and shows the matching skeleton part.
- Hand tracking scene detects hand joints with [Apple Vision](https://developer.apple.com/documentation/vision) and aligns a hand model in AR.
- Tap bones to see labels and highlight.

## Requirements

- Unity `6000.3.10f1` or a compatible Unity 6 version.
- iOS build support in Unity.
- An ARKit-capable iPhone or iPad.
- Xcode for building and deploying to iOS.

## How to Run

1. Fork or clone this repository.
2. Open the project folder in Unity Hub.
3. Let Unity restore packages from `Packages/manifest.json`.
4. Go to build profiles and add every scene in scene list.
5. Switch the build target to iOS and deploy to an ARKit-capable device.

## Project Structure

- `Assets/Scripts/BodyTracking` - AR body joint tracking, region selection, skeleton placement, and debug rendering.
- `Assets/Scripts/HandTracking` - iOS Vision hand tracking, joint smoothing, hand model placement, and debug rendering.
- `Assets/Scripts/BoneInteraction` - bone raycasting, labels, and highlighting.
- `Assets/Scripts/UIManagement` - scene navigation and tracking controls.
- `Assets/Models` - hand and skeleton model assets.

## Contributing

Contributions are welcome. Fork the project, create a focused branch and open a pull request with a clear description what changed.

## License

This project is licensed under the MIT License. See `LICENSE` for details.
