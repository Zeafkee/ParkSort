using System;

namespace ParkSort.Core
{
    [Serializable]
    public struct GeneratorSettings
    {
        public int width;
        public int height;
        public int cars;
        public int colors;
        public float longCarChance;
        public float hiddenChance;
        public int obstacles;
        public int waitingSlots;
        public float directChance;
        public int lookahead;
        public int plannedWaitingLimit;
        public int tunnels;
        public int carsPerTunnel;

        public static GeneratorSettings Default => new GeneratorSettings
        {
            width = 5, height = 5, cars = 9, colors = 3, longCarChance = 0f, hiddenChance = 0f, obstacles = 0,
            waitingSlots = 5, directChance = 0.8f, lookahead = 2, plannedWaitingLimit = 3, tunnels = 0, carsPerTunnel = 3,
        };
    }

    public sealed class LevelGenerator
    {
        private const int GarageCapacity = 3;
        private const int OpenGarages = 2;
        private const int PlacementTries = 400;

        private readonly Solver _solver = new Solver();

        public LevelDef Generate(GeneratorSettings settings, uint seed, int attempts = 40)
        {
            if (settings.cars % GarageCapacity != 0) throw new ArgumentException("the number of cars must be a multiple of the garage capacity");
            if (settings.cars > LevelDef.MaxCars) throw new ArgumentException("too many cars");
            if (settings.colors < 1 || settings.colors > LevelDef.MaxColors) throw new ArgumentException("unsupported number of colors");

            var rng = new Rng(seed);
            for (int attempt = 0; attempt < attempts; attempt++)
            {
                LevelDef level = TryGenerate(settings, ref rng);
                if (level == null || level.Validate() != null) continue;
                if (_solver.Solve(level) == SolveOutcome.Solved) return level;
            }

            return null;
        }

        private static LevelDef TryGenerate(GeneratorSettings settings, ref Rng rng)
        {
            int width = settings.width, height = settings.height;
            var grid = new int[width * height];

            var blocked = new int[settings.obstacles];
            for (int i = 0; i < blocked.Length; i++)
            {
                int cell = -1;
                for (int t = 0; t < 50 && cell < 0; t++)
                {
                    int x = rng.Range(width), y = rng.Range(height);
                    if (grid[y * width + x] == 0) cell = y * width + x;
                }

                if (cell < 0) return null;
                grid[cell] = -1;
                blocked[i] = cell;
            }

            var tunnels = new TunnelDef[settings.tunnels];
            var tunnelRoom = new int[settings.tunnels];
            int tunnelCarsLeft = 0;
            for (int i = 0; i < tunnels.Length; i++)
            {
                if (!PlaceTunnel(grid, width, height, ref rng, out tunnels[i])) return null;
                grid[tunnels[i].y * width + tunnels[i].x] = -1;
                tunnelRoom[i] = settings.carsPerTunnel;
                tunnelCarsLeft += settings.carsPerTunnel;
            }

            var cars = new CarDef[settings.cars];
            for (int i = 0; i < cars.Length; i++)
            {
                int carsLeft = cars.Length - i;
                if (tunnelCarsLeft > 0 && rng.Chance(Math.Min(1f, tunnelCarsLeft * 1.6f / carsLeft)))
                {
                    int tunnel = rng.Range(tunnels.Length);
                    if (tunnelRoom[tunnel] > 0 && WayIsClear(grid, width, height, tunnels[tunnel].x, tunnels[tunnel].y, tunnels[tunnel].direction))
                    {
                        cars[i] = new CarDef
                        {
                            x = tunnels[tunnel].x, y = tunnels[tunnel].y, direction = tunnels[tunnel].direction, length = 1,
                            tunnel = (byte)(tunnel + 1),
                        };
                        tunnelRoom[tunnel]--;
                        tunnelCarsLeft--;
                        continue;
                    }
                }

                if (!PlaceCar(grid, width, height, settings.longCarChance, ref rng, out cars[i])) return null;
                for (int c = 0; c < cars[i].length; c++)
                    grid[cars[i].CellY(c) * width + cars[i].CellX(c)] = i + 1;
            }

            for (int i = 0; i < tunnels.Length; i++)
                if (tunnelRoom[i] == settings.carsPerTunnel) return null;

            var level = new LevelDef
            {
                width = width, height = height, cars = cars, blockedCells = blocked, tunnels = tunnels,
                garageCapacity = GarageCapacity, openGarages = OpenGarages, waitingSlots = settings.waitingSlots,
            };

            if (!AssignColors(level, settings, ref rng)) return null;
            HideCars(level, settings.hiddenChance, ref rng);
            return level;
        }

        private static bool WayIsClear(int[] grid, int width, int height, int x, int y, Direction direction)
        {
            int dx = Directions.Dx(direction), dy = Directions.Dy(direction);
            int px = x + dx, py = y + dy;
            while (px >= 0 && py >= 0 && px < width && py < height)
            {
                if (grid[py * width + px] != 0) return false;
                px += dx;
                py += dy;
            }

            return true;
        }

        private static bool PlaceTunnel(int[] grid, int width, int height, ref Rng rng, out TunnelDef tunnel)
        {
            tunnel = default;
            for (int t = 0; t < 80; t++)
            {
                int x = rng.Range(width), y = rng.Range(height);
                var direction = (Direction)rng.Range(Directions.Count);
                if (grid[y * width + x] != 0) continue;

                int dx = Directions.Dx(direction), dy = Directions.Dy(direction);
                int steps = 0;
                int px = x + dx, py = y + dy;
                while (px >= 0 && py >= 0 && px < width && py < height)
                {
                    steps++;
                    px += dx;
                    py += dy;
                }

                if (steps < 1 || !WayIsClear(grid, width, height, x, y, direction)) continue;

                tunnel = new TunnelDef { x = (byte)x, y = (byte)y, direction = direction };
                return true;
            }

            return false;
        }

        private static bool PlaceCar(int[] grid, int width, int height, float longChance, ref Rng rng, out CarDef car)
        {
            car = default;
            for (int t = 0; t < PlacementTries; t++)
            {
                int length = rng.Chance(longChance) ? 3 : 2;
                var direction = (Direction)rng.Range(Directions.Count);
                int dx = Directions.Dx(direction), dy = Directions.Dy(direction);
                int x = rng.Range(width), y = rng.Range(height);

                bool fits = true;
                for (int c = 0; c < length && fits; c++)
                {
                    int cx = x + dx * c, cy = y + dy * c;
                    fits = cx >= 0 && cy >= 0 && cx < width && cy < height && grid[cy * width + cx] == 0;
                }

                if (!fits) continue;

                int px = x + dx * length, py = y + dy * length;
                bool clear = true;
                while (clear && px >= 0 && py >= 0 && px < width && py < height)
                {
                    clear = grid[py * width + px] == 0;
                    px += dx;
                    py += dy;
                }

                if (!clear) continue;

                car = new CarDef { x = (byte)x, y = (byte)y, direction = direction, length = (byte)length };
                return true;
            }

            return false;
        }

        private static bool AssignColors(LevelDef level, GeneratorSettings settings, ref Rng rng)
        {
            int carCount = level.cars.Length;
            int garages = carCount / GarageCapacity;

            var queue = new byte[garages];
            for (int i = 0; i < garages; i++) queue[i] = (byte)(i % settings.colors);
            rng.Shuffle(queue, garages);
            for (int i = OpenGarages - 1; i + 1 < garages; i++)
            {
                if (queue[i] != queue[i + 1]) continue;
                int swap = rng.Range(garages);
                byte tmp = queue[i + 1];
                queue[i + 1] = queue[swap];
                queue[swap] = tmp;
            }

            var need = new int[garages];
            var pending = new int[garages];
            for (int i = 0; i < garages; i++) need[i] = GarageCapacity;

            var lanes = new int[OpenGarages];
            int next = 0;
            for (int lane = 0; lane < OpenGarages; lane++) lanes[lane] = next < garages ? next++ : -1;

            int waiting = 0;
            var options = new int[garages];

            for (int step = 0; step < carCount; step++)
            {
                int car = carCount - 1 - step;

                int direct = 0;
                for (int lane = 0; lane < OpenGarages; lane++)
                    if (lanes[lane] >= 0 && need[lanes[lane]] > 0) options[direct++] = lanes[lane];

                int target = -1;
                bool wait = false;
                if (direct > 0 && (rng.Chance(settings.directChance) || waiting >= settings.plannedWaitingLimit))
                {
                    target = options[rng.Range(direct)];
                }
                else
                {
                    int future = 0;
                    for (int q = next; q < garages && q < next + settings.lookahead; q++)
                        if (need[q] > 0) options[future++] = q;

                    if (future > 0 && waiting < settings.plannedWaitingLimit)
                    {
                        target = options[rng.Range(future)];
                        wait = true;
                    }
                    else if (direct > 0)
                    {
                        direct = 0;
                        for (int lane = 0; lane < OpenGarages; lane++)
                            if (lanes[lane] >= 0 && need[lanes[lane]] > 0) options[direct++] = lanes[lane];
                        target = options[rng.Range(direct)];
                    }
                }

                if (target < 0) return false;

                level.cars[car].color = queue[target];
                need[target]--;
                if (wait)
                {
                    pending[target]++;
                    waiting++;
                    continue;
                }

                bool changed = true;
                while (changed)
                {
                    changed = false;
                    for (int lane = 0; lane < OpenGarages; lane++)
                    {
                        if (lanes[lane] < 0 || need[lanes[lane]] > 0) continue;

                        changed = true;
                        if (next >= garages)
                        {
                            lanes[lane] = -1;
                            continue;
                        }

                        lanes[lane] = next++;
                        waiting -= pending[lanes[lane]];
                        pending[lanes[lane]] = 0;
                    }
                }
            }

            level.garageQueue = queue;
            return true;
        }

        private static void HideCars(LevelDef level, float chance, ref Rng rng)
        {
            if (chance <= 0f) return;

            var compact = new CompactLevel(level);
            for (int i = 0; i < level.cars.Length; i++)
                level.cars[i].hidden = !level.cars[i].InTunnel && !compact.CanExit(i, compact.AllCars) && rng.Chance(chance);
        }
    }
}
