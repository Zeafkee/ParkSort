# Architecture

ParkSort is a portrait colour-sorting parking puzzle. Coloured cars are jammed in a lot; tapping a car
whose way is clear drives it out and into the garage of its colour. Two garages are open at a time,
each takes three cars, and a car with no open garage waits in a small bay. If the bay overflows the
level is lost.

The project is built around one idea: **levels are generated, proven solvable and rated by code, not
drawn by hand**. That needs rules that run without Unity, so the code is split in two.

```
Assets/_Project/Scripts
├── Core/      Pure C#, no UnityEngine (the assembly has noEngineReferences).
│              LevelDef, ParkingGame, MoveResult, CompactState, Solver, AutoPlayer, LevelGenerator, Rng
├── Runtime/
│   ├── Game/      GameController (level flow, input, boosters, economy)
│   ├── View/      BoardView, CarView, GarageLane, BoardLayout, CarPath, CameraRig
│   ├── UI/        Hud, Screens, TapArea, PressScale, SafeArea
│   ├── Data/      LevelCatalog, GameTheme (ScriptableObjects)
│   ├── Audio/ Save/ Services/ Dev/
├── Editor/    SceneBuilder (scene and assets from code), LevelBaker, model importer, DevTools
└── Tests/     EditMode tests: rules, solver, generator, layout and paths
```

## Model and view

`ParkingGame` owns the state: which cells are occupied, which cars are still in the lot, the two open
garages and how full they are, the waiting bay. `TryMove(car, result)` applies a whole move at once and
writes what happened into a reusable `MoveResult`, a list of steps:

```
CarToGarage(car, lane, slot)      CarToWaiting(car, slot)      CarOverflow(car)
GarageClosed(lane)                GarageOpened(lane, colour)   WaitingToGarage(car, lane, slot)
CarRevealed(car)
```

The view never decides anything. `BoardView.Play(result)` turns the steps into things to animate: a
car gets a mission (drive to this garage slot), a garage lane gets a queued operation (close, then open
with a new colour). Because the model is already finished, the player can tap the next car while the
previous one is still driving; input is validated against the model, not against what is on screen.

Animation order is enforced where it matters: a garage only closes when its three cars have actually
arrived, a car heading for a garage that has not opened yet waits at the gate, and a waiting car only
leaves the bay when its garage is visibly open. The win screen appears when the model has won *and*
nothing is moving any more.

## Rules in detail

- A car occupies 2 or 3 cells in a row and faces along them. It can leave if every cell from its nose
  to the edge of the lot is empty. Obstacles (planters, cones) never move.
- Leaving car → the open garage of its colour that has room; otherwise the first free waiting slot;
  otherwise the game is lost (the move is still applied, so an extra slot can revive the game).
- A full garage closes and the next colour in the queue opens in the same lane. Waiting cars of that
  colour move in immediately, which can fill and close it again in the same move.
- Mystery cars have a hidden colour until their way is clear.
- A tunnel is one cell that holds a stack of cars. Only its front car can leave, along the tunnel's
  direction, and the cell stays an obstacle even when the tunnel is empty.
- Undo replays the recorded moves except the last one. The rules are deterministic, so there is no
  separate "reverse a move" code to keep correct; the view is rebuilt from the model state.

## Solver

A level is a search problem: the state is *which cars are left* plus the garage state.

- `CompactLevel` precomputes, for every car, a bitmask of the cars standing in its way. "Can car i
  leave?" becomes `(blockers[i] & present) == 0`. The lot itself is never touched during search.
  Tunnels need no special case: the cars stacked in front of a tunnel car are simply added to its mask.
- The garage state (queue position, lane contents, waiting cars per colour) is a small struct that
  packs into 62 bits; with the 64-bit car mask it forms the key of a visited set.
- `Solver` is a depth-first search with that set as a transposition table of dead states. Moves that
  go straight into an open garage are tried first, which finds a solution for most boards almost
  without backtracking.
- It reports *solved*, *unsolvable* or *gave up* (node limit), and the move list.

The solver is used in three places: the generator (reject unsolvable boards), the **hint booster** (solve
from the current position and show the first move) and the guided first levels.

A cheaper copy of the rules existing next to `ParkingGame` is a risk, so a test plays random games on
both and checks that they agree after every move.

## Level generation

`LevelGenerator` builds a board backwards from a guaranteed solution:

1. **Placement.** Cars are placed one by one (or pushed into a tunnel); each new car must have a
   free way out past the cars already placed. The last car placed can therefore leave first, the one before it second, and so on:
   the reverse placement order is a valid exit order. No search is needed to know the board can be
   emptied.
2. **Colours.** That exit order is walked while simulating the garages. Each car gets the colour of an
   open garage (it would park directly) or, with some probability, of a garage that opens a little later
   (it would have to wait). The probability and the number of planned waiters are the difficulty knobs.
3. **Proof.** The solver checks the result under the real rules.

Generation is deterministic for a seed (own xorshift generator, no `System.Random`).

## Difficulty is measured, not guessed

`AutoPlayer` plays a level many times with two simple players: one taps random legal cars, one prefers
cars that can park directly. Their win rates say how forgiving a level is.

`LevelBaker` (editor) builds the 60-level campaign from this:

- a table of tiers (board size, number of cars and colours, vans, mystery cars, obstacles, tunnels),
- a target win-rate curve that falls slowly; from level 15 every fifth level is labelled Hard or
  Very Hard in turn and gets a lower target, and the level after it gets a higher one,
- for each level 80 candidates are generated, measured with 160 games per bot, and the one closest to
  the target is kept.

New mechanics arrive one at a time (vans at level 6, mystery cars at 11, obstacles at 16, tunnels at
31). The level that introduces one is kept easy and opens with a "new feature" card. After level 60
the game generates levels on the device with the hardest tier.

## View layer

- **Layout** (`BoardLayout`) is pure geometry: cell centres, the ring road around the lot, garage and
  waiting slots, and the route along the ring between two points (the shorter way round).
- **Paths** (`CarPath`): a polyline with rounded corners and cumulative lengths. A car is a distance
  along its path; position and heading come from one lookup. Cars leaving the waiting bay reverse out.
- **One update loop.** `BoardView.Update` moves every car and garage. Cars are plain objects in a pool
  of 64 created at startup; nothing is instantiated or destroyed while playing.
- **Rendering.** One toon shader for everything. Paintable parts of a mesh are marked in vertex alpha
  and multiplied by the material colour, so the same car mesh is used for every colour with one
  material per colour (SRP Batcher friendly). The ground is one quad whose shader draws the lot, the
  parking lines, the road and the grass from world position, so any board size needs no new art.
  No textures, no real-time lights, no shadow maps, no post-processing.
- **Camera.** Orthographic and fixed; `CameraRig` fits the board into the space between the top and
  bottom HUD for any aspect ratio.

## Game layer

`GameController` is the only class that talks to both sides: it loads a level, forwards taps, plays
results on the board, and owns boosters (undo, hint, extra waiting slot), coins and screen state
(Home, Playing, Won, Failed). The first five levels start without the home screen. UI classes listen to its events. Analytics calls go through an
`IAnalytics` interface (level start, complete, fail, booster used) with a logging implementation, so
an SDK can be plugged in without touching gameplay code.

### Economy

Coins come from finishing levels (25, or 50 through the "double the reward" offer) and go to three
sinks: boosters bought one at a time in a level when the stock is empty, booster packs in the shop,
and themes. A second extra waiting slot in the same level costs more than the first, so reviving is
never free of thought. The shop content (themes, packs, prices) is one ScriptableObject.

The rewarded-ad offer goes through an `IAdService` interface. The project ships a placeholder that
simply waits and reports success; a real ad SDK would implement the same two members. No ad SDK is
included.

### Themes

A theme is data: six ground colours and a light and ambient colour. Applying one sets the properties
of the procedural ground material and two global shader values that the toon shader uses for lighting,
so every object changes mood without touching its material. Car colours never change with the theme,
because they are gameplay information.

## Tooling

- `ParkSort > Setup > Build Everything`: materials, theme, baked levels and the whole scene from code.
- `ParkSort > Dev > Auto-Play`: a bot that plays with the solver; at 6x speed it clears the 60 levels
  in under two minutes. Used as a soak test.
- Models are written in Python for Blender (`Tools/blender`), sprites and sounds are generated by
  `Tools/generate_ui.py` and `Tools/generate_audio.py`.

## Decisions and trade-offs

| Decision | Why | Cost |
|---|---|---|
| Rules in a Unity-free assembly | Solver, generator and bots run thousands of games per second; tests need no Play Mode | View has to translate steps into animation |
| Generate backwards from a solution | Every board is solvable by construction | Boards cannot be much denser than about 70 % of the cells |
| Bitmask search state | A move is two integer operations; states hash cheaply | At most 64 cars and 8 colours |
| Bot win rate as difficulty | Cheap, automatic, comparable across levels | Bots are not people; real tuning needs player data |
| Model resolves a move instantly | Input never waits for animation | The view needs its own ordering rules |
| Procedural ground shader | One quad for any board size | Road layout is fixed to a ring |
