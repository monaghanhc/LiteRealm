# LiteRealm MVP Playtest Checklist

## Core Loop
- Start scene and confirm player can move, sprint, crouch, jump.
- Toggle camera with `V` and verify first/third person both work.
- Fire weapon with `Mouse0`, reload with `R`, swap with `1`/`2`/`3`.
- Confirm recoil and hit impacts appear.

## Survival
- Verify HUD bars update for health/stamina/hunger/thirst.
- Sprint for 10+ seconds and confirm stamina drains.
- Wait and confirm hunger/thirst drain and stamina regen slowdown when low.
- Consume quickslot items with `5`/`6`/`7`/`8`.

## World + AI
- Confirm day/night transitions over time and lighting changes smoothly.
- Validate zombies become more aggressive / more numerous at night.
- Check zombie AI wander, chase, and melee attack behavior.
- Spawn boss and verify melee + spit attack behavior.

## Loot + Inventory
- Open loot container with `E`, take all loot, verify inventory updates.
- Kill zombie and confirm loot drops into world pickups.
- Pick up world loot and verify stack behavior.

## NPC + Quests
- Talk to NPC with `E`, accept quest in dialogue.
- Progress kill/retrieve/boss objectives and open quest log with `J`.
- Turn in completed quest and verify rewards are granted.

## Save / Load
- Press `F5` to save.
- Move player, consume item, open another container.
- Press `F9` to load and verify player position, stats, inventory, quests, and container states restore.

## Debug Panel
- Toggle debug panel with `F1`.
- Use debug actions: spawn zombie, spawn boss, give item, set time, toggle god mode.
