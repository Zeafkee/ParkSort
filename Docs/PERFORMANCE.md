# Performance

The goal for a puzzle game of this size: instant input response, a steady 60 FPS on low-end Android,
a small download, and no garbage collection during play.

> **Where the numbers come from.** Everything below was measured in the Unity Editor on a desktop PC
> with the tools in this repo. They are useful for counting work and allocations, not as phone numbers.
> Frame time and memory on a real device still have to be measured with a development build.

## Measured

| What | Result | How |
|---|---|---|
| Rules: allocations per move | **0 bytes** | `Moves_DoNotAllocate` (Test Framework `AllocatingGCMemory` constraint) |
| Path evaluation: allocations | **0 bytes** | `Path_Evaluate_DoesNotAllocate` |
| Solver, shipped levels | 0.005 ms average, 0.010 ms worst, about 14 nodes | 60 baked levels, stopwatch in the editor |
| Level generation, hardest tier | 0.05 ms per level, 200 of 200 seeds produced a level | `LevelGenerator.Generate` |
| Baking the campaign | about 3 s for 60 levels (4,800 candidates, about 1.5 million bot games) | `LevelBaker.Bake` |
| SetPass calls per frame | about 18, board and HUD together | `ProfilerRecorder`, level 45 |
| Triangles per frame | about 15,000 | `ProfilerRecorder`, level 45 |
| Car objects | 64, created once, reused for every level | `BoardView` pool |
| Tests | 34 EditMode tests, about 1.3 s | Test Runner |
| Soak run | 70 levels in a row (60 baked + generated ones), no errors | `DevTools.AutoPlay` at 6x |

Not measured reliably: per-frame garbage of the view layer. The editor's own allocations
(about 120 KB per frame, identical with the game idle) drown out the signal; this needs a device build.

## What was done, and why

### CPU
- **The lot is never scanned during search.** Blocking relations are precomputed as bitmasks, so the
  solver and the bots work on two 64-bit values per state.
- **A move is resolved once, in the model.** The view only plays back a list of steps.
- **One `Update` for the whole board.** Cars and garages are plain objects driven from one loop; there is
  no `MonoBehaviour` per car.
- **No physics.** Picking a car is a ray against a plane and a grid lookup; physics simulation is off.
- **Hints solve on demand.** The solver runs when the booster is pressed, not every frame.

### GPU
- **One shader, few materials.** Cars share one mesh per size and one material per colour; the SRP
  Batcher groups them. Paint is a vertex-alpha mask, so there are no textures to sample.
- **Procedural ground.** Lot, lines, road and grass are computed in one fragment shader on one quad.
- **No lights, shadow maps or post-processing.** Lighting is a fixed half-Lambert in the vertex shader;
  shadows are blob quads. HDR and MSAA are off.
- **Orthographic camera.** Nothing scales with distance, so overdraw is limited to UI and blob shadows.

### Memory / GC
- **Pools.** Cars, garages, waiting slots, obstacles and decoration are created at startup and toggled.
- **Reused results.** `MoveResult` and its step list are reused for every move; paths are rebuilt into
  the same lists.
- **Levels are data.** 60 levels are a few kilobytes in a ScriptableObject; later levels are generated
  on the device from a seed instead of being stored.
- **Audio.** Effects are decompressed on load, music streams; a minimum interval per effect keeps the
  voice count low when many cars arrive together.

### Build
- IL2CPP, ARM64, engine code stripping, incremental GC, 60 FPS cap with frame pacing.
- No textures besides the small UI sprites and the font atlas; models are a few hundred triangles each.

## Still open

- **Measure on a device:** frame time, memory and per-frame GC on a low-end Android phone.
- **View-layer garbage:** DOTween callbacks and the garage sequencing use a few delegates per move;
  expected to be small, not verified.
- **Solver worst case:** the shipped levels solve almost without backtracking. A position reached after
  many player mistakes can be much harder; the node limit (200,000) bounds it, but the time at that
  limit has not been measured on a phone.
- **Bots versus players:** the difficulty curve is tuned on bot win rates. Real fail rates per level
  (the analytics events are in place) should replace them.
