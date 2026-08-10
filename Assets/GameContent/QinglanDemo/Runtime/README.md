# Qinglan Demo runtime inputs

This folder contains checked-in, build-time inputs for the formal Qinglan Demo player.

- `QinglanDemoContentPack.release.json` is deterministically promoted from the frozen G3.4
  authoring bake. The promotion changes only the pack manifest to a first-party Release
  manifest and recomputes its verified content hash.
- `QinglanInputActions.asset` is the formal copy of the reviewed keyboard/gamepad action map.

Development authoring remains under `Assets/GameAssets/Placeholder`; Release scenes and
Addressables must never depend on those authoring assets.
