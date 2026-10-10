# ParkSort

A colour-sorting parking puzzle for mobile, made with Unity 6. Tap a car to drive it out of the jammed
lot and into the garage of its colour. Two garages are open at a time and each takes three cars; cars
with nowhere to go wait in a small bay. Fill the bay and the level is lost.

![screenshots](Docs/screenshots.png)

**Play in the browser:** [zeafkee.github.io/play/parksort](https://zeafkee.github.io/play/parksort/) · **Android:** APK in [Releases](../../releases/latest)

## Features

- 60 levels, then endless levels generated on the device
- Every level is generated, proven solvable by a solver and rated by bots before it ships
- Cars and vans, mystery cars that hide their colour, obstacles and tunnels that release cars one by one;
  one new mechanic at a time, each introduced with a "new feature" card
- Hard and Very Hard levels every five levels, with a breather after each
- Boosters: undo, hint (the solver shows the next move) and an extra waiting slot that can also revive a lost level
- Coin economy: a shop with booster packs and five unlockable themes, a rising price for repeated
  revives, and an optional "double the reward" offer behind a rewarded-ad placeholder
- A guided first level with no menu in the way, sound and music switches, progress saved on the device
- Models, sprites, sounds and music made for this project (Blender Python, Pillow, numpy)

## Architecture

```
Assets/_Project/Scripts
├── Core/      Rules, solver, level generator and bots. Pure C#, no UnityEngine, unit tested.
├── Runtime/   Game (flow, boosters), View (board, cars, paths, camera), UI, Data, Audio, Save
├── Editor/    Scene builder, level baker, model importer, auto-play and capture tools
└── Tests/     EditMode tests for rules, solver, generator, layout and paths
```

- The **model resolves a move instantly** and returns a list of steps; the view only plays them back,
  so input never waits for animation.
- The **solver** is a depth-first search over a bitmask of remaining cars plus a packed garage state,
  with a table of dead states. It powers level validation, the hint booster and the tutorial.
- The **generator** places cars so that the reverse placement order is always a valid exit order, then
  assigns colours by simulating the garages. Solvable by construction, verified by the solver.
- **Difficulty is measured:** bots play each candidate hundreds of times and the level baker picks the
  one closest to a target win-rate curve.

Read more:
- **[Docs/ARCHITECTURE.md](Docs/ARCHITECTURE.md)** — rules, solver, generation, view layer, trade-offs
- **[Docs/PERFORMANCE.md](Docs/PERFORMANCE.md)** — what was measured, how, and what is still open

## Numbers

| | |
|---|---|
| Rules allocations per move | 0 bytes (tested) |
| Solver on shipped levels | about 0.005 ms |
| Generating a level | about 0.05 ms |
| Baking the 60-level campaign | about 3 s, about 1.5 million bot games |
| Tests | 34 EditMode tests |

Measured in the editor on a desktop PC; see [PERFORMANCE.md](Docs/PERFORMANCE.md) for method and caveats.

## Running it

1. Open the project with **Unity 6000.6** (URP).
2. Open `Assets/_Project/Scenes/Main.unity`, set the Game view (or Simulator) to a portrait phone, press Play.

Useful menu items under **ParkSort**:

| Menu | What it does |
|---|---|
| Setup > Build Everything | Regenerates materials, theme, levels and the scene from code |
| Setup > Bake Levels | Generates and rates the 60 levels again and logs their win rates |
| Dev > Auto-Play (normal speed / fast) | A bot plays level after level (Play Mode) |
| Dev > Log Status | Level, cars left, waiting bay, coins |
| Dev > Delete Save Data | Clears local progress |

Tests: `Window > General > Test Runner > EditMode`.

## Assets

Models are built with Python in Blender (`Tools/blender`) and imported from a small JSON format
(`.lpmesh`). UI sprites and the app icon come from `Tools/generate_ui.py`, every sound and the music
loop from `Tools/generate_audio.py`. Nothing is taken from other games. Third-party: DOTween
(Demigiant), TextMesh Pro with Liberation Sans (SIL OFL).

## Tech

Unity 6000.6 · URP · Input System · uGUI + TextMesh Pro · DOTween · Unity Test Framework ·
Blender (Python) for models
