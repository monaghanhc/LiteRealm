# LiteRealm MVP Scene Setup (Unity 2022.3 LTS+)

## 1) Packages
- Required: **AI Navigation** (`com.unity.ai.navigation`) for NavMesh workflows.
- Optional: **Input System**. Current scripts use legacy Input Manager (`Input.GetAxis/GetKey`) so no package is required.
- Optional: **Cinemachine** is not required; camera is custom.

## 2) Scene Root
Create scene `Assets/_Project/Scenes/Main.unity` with:
- `__App`
- `World`
- `Gameplay`
- `UI`

## 3) Core Managers (`__App`)
Add GameObjects and scripts:
- `GameEventHub` -> `LiteRealm.Core.GameEventHub`
- `DayNightCycle` -> `LiteRealm.World.DayNightCycleManager` (assign Directional Light as Sun)
- `QuestManager` -> `LiteRealm.Quests.QuestManager`
- `BossSpawnManager` -> `LiteRealm.AI.BossSpawnManager`
- `SaveSystem` -> `LiteRealm.Saving.SaveSystem`

Wire references between managers in Inspector.

## 4) World (`World`)
- Create Unity Terrain (~1000x1000) and sculpt hills/valleys.
- Paint terrain layers (grass/rock/dirt).
- Add trees/rocks with primitives or Terrain Paint Trees.
- Add a water plane (`Plane` + blue material).
- Add POIs:
  - Cabin (cubes + roof prism)
  - Campsite (tent from primitives + campfire cylinder)
  - Ruined area (broken walls from cubes)
  - Boss arena (large clearing circle)
- Create spawn marker empties using `LiteRealm.World.WorldSpawnPoint` for:
  - zombie spawns
  - loot containers
  - NPCs
  - boss spawn

## 5) Player (`Gameplay/Player`)
Components on `Player` object:
- `CharacterController`
- `LiteRealm.Player.PlayerStats`
- `LiteRealm.Player.PlayerController`
- `LiteRealm.Player.PlayerInteractor`
- `LiteRealm.Combat.WeaponManager`
- Tag object as `Player`

Create child `WeaponRig` and put weapon prefabs under it.
Create camera object `Main Camera` with `LiteRealm.CameraSystem.PlayerCameraController`.
Assign camera target = `Player`.

## 6) Weapons
- Create `Rifle` prefab with:
  - `LiteRealm.Combat.HitscanRifle`
  - `AudioSource`
  - optional muzzle `ParticleSystem`
- Assign weapon in `WeaponManager.weapons`.
- Set hit mask to include environment + enemies.

## 7) Enemies
### Zombie prefab
- Capsule/cube model root
- `NavMeshAgent`
- `LiteRealm.Core.HealthComponent`
- `LiteRealm.AI.ZombieAI`
- `LiteRealm.Loot.LootDropper`

### Boss prefab
- Larger capsule model root
- `NavMeshAgent`
- `LiteRealm.Core.HealthComponent` (high health)
- `LiteRealm.AI.BossAI`
- `LiteRealm.Loot.LootDropper` (configure guaranteed special drop)

### Boss projectile prefab
- Sphere with trigger collider
- `LiteRealm.AI.BossProjectile`

## 8) Spawners
Create spawner zone GameObjects and attach:
- `LiteRealm.AI.SpawnerZone` (assign zombie prefab + spawn points + day/night caps)
- `BossSpawnManager` assign boss prefab + spawn point.

## 9) Inventory + Loot Assets
Create ScriptableObjects:
- `ItemDefinition` assets: water bottle, canned food, medkit, ammo, special boss drop
- `ItemDatabase` and include all items
- `LootTable` assets for:
  - common container
  - zombie drop
  - boss drop

Add `LootContainer` script to crate/chest objects and assign unique `containerId` and loot table.

## 10) Quests + NPCs
Create quest assets:
- kill zombies
- retrieve item
- defeat boss

Create `QuestDatabase` and register all quests.
NPC prefab:
- primitive character
- `LiteRealm.Quests.NPCQuestGiver`
Assign quest line list on NPC.

## 11) UI (`UI`)
Create Canvas with panels:
- HUD panel:
  - 4 sliders (health/stamina/hunger/thirst)
  - quickslots text
  - ammo text
  - time text
  - `LiteRealm.UI.SurvivalHUDController`
- Interaction prompt panel/text with `LiteRealm.UI.InteractionPromptUI`
- Loot panel with text/buttons + `LiteRealm.UI.LootUIController`
- Dialogue panel with texts/buttons + `LiteRealm.UI.DialogueUIController`
- Quest log panel + text + `LiteRealm.UI.QuestLogUIController`
- Debug panel (toggle F1) with input fields/buttons + `LiteRealm.UI.DebugPanelController`

Assign all references in inspectors.

## 12) NavMesh Bake
- Mark terrain and static geometry as `Navigation Static`.
- Open `Window > AI > Navigation`.
- In Bake tab, configure agent radius/height based on zombie size.
- Bake.
- Ensure zombies and boss with `NavMeshAgent` move on baked mesh.

## 13) Save/Load
`SaveSystem` defaults:
- Save key: `F5`
- Load key: `F9`
- File path: `Application.persistentDataPath/savegame.json`

## 14) Keybindings
- Move: `WASD`
- Jump: `Space`
- Sprint: `Left Shift`
- Crouch toggle: `C`
- Interact: `E`
- Toggle camera FP/TP: `V`
- Shoot: `Mouse0`
- Reload: `R`
- Weapon swap: `1`,`2`,`3` or mouse wheel
- Consumables quick use: `5`,`6`,`7`,`8`
- Quest log: `J`
- Debug panel: `F1`
- Quick Save/Load: `F5` / `F9`

## 15) Testing
Use `Assets/_Project/Tests/TestingChecklist.md` and run through each checklist item in Play Mode.
