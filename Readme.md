# Ponex

> Pong to the max.

An arena battler built in Unity that takes Pong way past its breaking point: wild ball types, playable characters with unique abilities, and modes for solo, couch, and online play.

## Features

- **15+ ball types** — Meteor, Illusion, Mine, Shock, Frost, Ghost, Nano, Orbit, Size, Time, Clone, Burn, Explosion, Mimic, Shape, and more, each with its own behavior
- **10+ playable characters** — each with unique movement, bumpers, and super abilities
- **Game modes** — Versus (local multiplayer), Arcade (progressive rounds), Story, and Training
- **Team / co-op mode** — automatically offered for larger lobbies
- **Online multiplayer** — lobbies hosted over Firebase with relay connections
- **Full select screens** — character, field, ball, team, and position select with paging
- **Hazard fields** — drifting logs, cannon towers, sand dunes, space portals, and more

## Controls

| Action | Keyboard | Gamepad |
|--------|----------|---------|
| Move | WASD / Arrow keys | Left stick |
| Attack | Enter / Left click | West button |
| Jump | Space | South button |
| Sprint | Left Shift | Left stick press |
| Interact | E | — |
| Previous / Next selection | 1 / 2 | — |

Supports keyboard, mouse, gamepad, and joystick via Unity's Input System.

## Getting started

1. Clone the repo:
   ```bash
   git clone https://github.com/TrueEmpty/Ponex.git
   ```
2. Open the project in **Unity Hub** (Universal Render Pipeline project).
3. Open the main menu scene and press **Play**.

## Project structure

```
Scripts/
├── Arcade/      # Arcade mode: runs, matches, records
├── Ball/        # Ball types and movement/physics
├── Bump/        # Bumpers
├── Character/   # Playable characters + CPU AI
├── Field/       # Fields, hazards, camera fit
├── General/     # Menus, character/field/ball select, settings
├── Online/      # Firebase + relay multiplayer
├── Player/      # Player state, movement, stats
├── Story/       # Story mode
├── Super/       # Super abilities
├── UI/          # Buttons, pause menu, options
└── Walls/       # Wall/boundary behavior
Editor/          # Editor tooling (prefab builders, backup/restore)
Tests/           # Edit-mode tests
```

## Contributing

Found a bug? [Open an issue](https://github.com/TrueEmpty/Ponex/issues) with the file/line number, what happened, and what you expected. Pull requests are welcome — please keep changes focused and test in Play mode before submitting.

## License

TBD — add a license file before distributing builds.
