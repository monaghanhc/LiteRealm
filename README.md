# LiteRealm - Unity Zombie Survival MVP (Free-Only)

This repository contains the gameplay scaffold under `Assets/_Project` for a 3D singleplayer zombie survival shooter (Unity 2022.3 LTS+).

## Important

The repo root currently contains `Assets/` but not a full Unity project config (`ProjectSettings/`, `Packages/`).
Use the import workflow below for reliable setup.

## Quick Start (Recommended)

1. Open Unity Hub and create a new **3D (Built-In Render Pipeline)** project using **Unity 2022.3 LTS or newer**.
2. Close Unity.
3. Copy this repo's `Assets/_Project` folder into your new project's `Assets/` folder.
4. Re-open the Unity project.
5. In Unity, run:
   - `Tools > LiteRealm > Bootstrap > Setup + Validate (Step 1-5)`
6. Then run:
   - `Tools > LiteRealm > Bootstrap > Open Main Scene`
7. Press Play.

## What the Bootstrap Does

- Ensures required folder structure, tags, and layers.
- Sets Input Handling to `Both` (safe mode for first import).
- Applies scene setup through Steps 1-5 (world, player, combat, survival, loot, quests, save/load).
- Runs ProjectDoctor checks and logs pass/fail details to Console.

## Required / Optional Packages

- Required:
  - `com.unity.inputsystem`
  - `com.unity.ai.navigation` (or built-in NavMesh fallback)
- Optional:
  - `com.unity.cinemachine`

If missing, open `Window > Package Manager` and install them, then run bootstrap again.

## Controls

- `WASD`: Move
- `Mouse`: Look
- `Space`: Jump
- `Left Shift`: Sprint
- `Left Ctrl`: Crouch
- `V`: Toggle first/third person
- `E`: Interact (pickup/open/talk)
- `Mouse0`: Fire
- `R`: Reload
- `J`: Quest log
- `F1`: Debug panel
- `F5`: Quick save
- `F9`: Quick load

## Validation Checklist

1. `Tools > LiteRealm > Project Doctor > Open Window > Run Checks`
2. Confirm no **Error** checks fail.
3. Open `Window > General > Test Runner`
4. Run **EditMode** tests, then **PlayMode** tests.

## Save File Location

Save file name: `savegame.json` under Unity `Application.persistentDataPath`.

Typical Windows path:
- `%USERPROFILE%\\AppData\\LocalLow\\<CompanyName>\\<ProductName>\\savegame.json`

## Free-Only Policy

- The project works with built-in primitives/materials/Terrain only.
- Optional visual/audio upgrades must stay free (Unity Asset Store free assets).

## Troubleshooting

- Scripts fail to compile on first import:
  - Install missing packages in Package Manager.
  - Set `Edit > Project Settings > Player > Active Input Handling` to `Both`.
  - Reopen Unity and run bootstrap again.
- Scene missing references:
  - Re-run `Tools > LiteRealm > Bootstrap > Setup + Validate (Step 1-5)`.
