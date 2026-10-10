using System;

namespace ParkSort.Core
{
    public struct GarageState
    {
        public const int NoGarage = -1;

        public int next;
        public int lane0, lane1, lane2;
        public int fill0, fill1, fill2;
        public uint waiting;
        public int waitingTotal;

        public int LaneIndex(int lane) => lane == 0 ? lane0 : lane == 1 ? lane1 : lane2;
        public int LaneFill(int lane) => lane == 0 ? fill0 : lane == 1 ? fill1 : fill2;

        public void SetLane(int lane, int index, int fill)
        {
            if (lane == 0) { lane0 = index; fill0 = fill; }
            else if (lane == 1) { lane1 = index; fill1 = fill; }
            else { lane2 = index; fill2 = fill; }
        }

        public int WaitingOf(int color) => (int)((waiting >> (color * 3)) & 7u);

        public void AddWaiting(int color, int delta)
        {
            int value = WaitingOf(color) + delta;
            waiting = (waiting & ~(7u << (color * 3))) | ((uint)value << (color * 3));
            waitingTotal += delta;
        }

        public ulong Pack()
        {
            ulong key = (ulong)(uint)next;
            key = (key << 8) | (uint)(lane0 + 1);
            key = (key << 2) | (uint)fill0;
            key = (key << 8) | (uint)(lane1 + 1);
            key = (key << 2) | (uint)fill1;
            key = (key << 8) | (uint)(lane2 + 1);
            key = (key << 2) | (uint)fill2;
            return (key << 24) | waiting;
        }
    }

    public struct StateKey : IEquatable<StateKey>
    {
        public ulong present;
        public ulong garages;

        public bool Equals(StateKey other) => present == other.present && garages == other.garages;
        public override bool Equals(object obj) => obj is StateKey other && Equals(other);
        public override int GetHashCode() => (present * 0x9E3779B97F4A7C15UL ^ garages * 0xC2B2AE3D27D4EB4FUL).GetHashCode();
    }

    public sealed class CompactLevel
    {
        public readonly LevelDef Level;
        public readonly int CarCount;
        public readonly ulong AllCars;
        public readonly ulong[] Blockers;
        public readonly bool[] NeverExits;
        public readonly byte[] Colors;

        public CompactLevel(LevelDef level)
        {
            Level = level;
            CarCount = level.cars.Length;
            AllCars = CarCount == 64 ? ulong.MaxValue : (1UL << CarCount) - 1UL;
            Blockers = new ulong[CarCount];
            NeverExits = new bool[CarCount];
            Colors = new byte[CarCount];

            var grid = new int[level.CellCount];
            for (int i = 0; i < level.blockedCells.Length; i++) grid[level.blockedCells[i]] = -1;
            for (int i = 0; i < level.tunnels.Length; i++) grid[level.IndexOf(level.tunnels[i].x, level.tunnels[i].y)] = -1;
            for (int i = 0; i < CarCount; i++)
            {
                CarDef car = level.cars[i];
                Colors[i] = car.color;
                if (car.InTunnel)
                {
                    for (int other = i + 1; other < CarCount; other++)
                        if (level.cars[other].tunnel == car.tunnel) Blockers[i] |= 1UL << other;
                    continue;
                }

                for (int c = 0; c < car.length; c++) grid[level.IndexOf(car.CellX(c), car.CellY(c))] = i + 1;
            }

            for (int i = 0; i < CarCount; i++)
            {
                CarDef car = level.cars[i];
                int dx = Directions.Dx(car.direction), dy = Directions.Dy(car.direction);
                int x = car.HeadX + dx, y = car.HeadY + dy;
                while (level.InBounds(x, y))
                {
                    int value = grid[level.IndexOf(x, y)];
                    if (value < 0) NeverExits[i] = true;
                    else if (value > 0) Blockers[i] |= 1UL << (value - 1);
                    x += dx;
                    y += dy;
                }
            }
        }

        public bool CanExit(int car, ulong present) => !NeverExits[car] && (Blockers[car] & present) == 0;

        public GarageState InitialGarages()
        {
            var state = new GarageState { lane0 = GarageState.NoGarage, lane1 = GarageState.NoGarage, lane2 = GarageState.NoGarage };
            for (int lane = 0; lane < Level.openGarages; lane++)
                state.SetLane(lane, state.next < Level.garageQueue.Length ? state.next++ : GarageState.NoGarage, 0);
            return state;
        }

        public bool MatchesOpenGarage(ref GarageState state, int color)
        {
            for (int lane = 0; lane < Level.openGarages; lane++)
            {
                int index = state.LaneIndex(lane);
                if (index >= 0 && Level.garageQueue[index] == color && state.LaneFill(lane) < Level.garageCapacity) return true;
            }

            return false;
        }

        public bool Exit(ref GarageState state, int color, int waitingCapacity)
        {
            for (int lane = 0; lane < Level.openGarages; lane++)
            {
                int index = state.LaneIndex(lane);
                if (index < 0 || Level.garageQueue[index] != color || state.LaneFill(lane) >= Level.garageCapacity) continue;

                state.SetLane(lane, index, state.LaneFill(lane) + 1);
                Resolve(ref state);
                return true;
            }

            if (state.waitingTotal >= waitingCapacity) return false;
            state.AddWaiting(color, 1);
            return true;
        }

        private void Resolve(ref GarageState state)
        {
            bool changed = true;
            while (changed)
            {
                changed = false;
                for (int lane = 0; lane < Level.openGarages; lane++)
                {
                    if (state.LaneIndex(lane) < 0 || state.LaneFill(lane) < Level.garageCapacity) continue;

                    changed = true;
                    if (state.next >= Level.garageQueue.Length)
                    {
                        state.SetLane(lane, GarageState.NoGarage, 0);
                        continue;
                    }

                    int index = state.next++;
                    int color = Level.garageQueue[index];
                    int moved = Math.Min(state.WaitingOf(color), Level.garageCapacity);
                    if (moved > 0) state.AddWaiting(color, -moved);
                    state.SetLane(lane, index, moved);
                }
            }
        }

        public static void Export(ParkingGame game, out ulong present, out GarageState state)
        {
            LevelDef level = game.Level;
            present = 0;
            for (int i = 0; i < level.cars.Length; i++)
                if (game.IsPresent(i)) present |= 1UL << i;

            state = new GarageState { lane0 = GarageState.NoGarage, lane1 = GarageState.NoGarage, lane2 = GarageState.NoGarage };
            state.next = game.NextQueueIndex;
            for (int lane = 0; lane < game.LaneCount; lane++)
                state.SetLane(lane, game.LaneQueueIndex(lane), game.LaneQueueIndex(lane) < 0 ? 0 : game.LaneFill(lane));
            for (int slot = 0; slot < game.WaitingCapacity; slot++)
            {
                int car = game.WaitingCar(slot);
                if (car != ParkingGame.NoCar) state.AddWaiting(level.cars[car].color, 1);
            }
        }
    }
}
