using System;

namespace ParkSort.Core
{
    public enum Direction : byte
    {
        Up = 0,
        Right = 1,
        Down = 2,
        Left = 3,
    }

    public static class Directions
    {
        public const int Count = 4;

        public static int Dx(Direction direction)
        {
            switch (direction)
            {
                case Direction.Right: return 1;
                case Direction.Left: return -1;
                default: return 0;
            }
        }

        public static int Dy(Direction direction)
        {
            switch (direction)
            {
                case Direction.Up: return 1;
                case Direction.Down: return -1;
                default: return 0;
            }
        }

        public static Direction Opposite(Direction direction) => (Direction)(((int)direction + 2) % Count);
    }

    public enum LevelDifficulty : byte
    {
        Normal = 0,
        Hard = 1,
        VeryHard = 2,
    }

    [Serializable]
    public struct CarDef
    {
        public byte x;
        public byte y;
        public Direction direction;
        public byte length;
        public byte color;
        public bool hidden;
        public byte tunnel;

        public bool InTunnel => tunnel > 0;
        public int CellX(int index) => x + Directions.Dx(direction) * index;
        public int CellY(int index) => y + Directions.Dy(direction) * index;
        public int HeadX => CellX(length - 1);
        public int HeadY => CellY(length - 1);
    }

    [Serializable]
    public struct TunnelDef
    {
        public byte x;
        public byte y;
        public Direction direction;
    }

    [Serializable]
    public sealed class LevelDef
    {
        public const int MaxCars = 64;
        public const int MaxColors = 8;
        public const int MaxLanes = 3;

        public int width = 5;
        public int height = 5;
        public CarDef[] cars = new CarDef[0];
        public int[] blockedCells = new int[0];
        public TunnelDef[] tunnels = new TunnelDef[0];
        public byte[] garageQueue = new byte[0];
        public int garageCapacity = 3;
        public int openGarages = 2;
        public int waitingSlots = 5;

        public int CellCount => width * height;
        public int IndexOf(int x, int y) => y * width + x;
        public bool InBounds(int x, int y) => x >= 0 && y >= 0 && x < width && y < height;

        public string Validate()
        {
            if (width < 1 || height < 1) return "empty board";
            if (cars.Length == 0) return "no cars";
            if (cars.Length > MaxCars) return "too many cars";
            if (openGarages < 1 || openGarages > MaxLanes) return "unsupported number of open garages";
            if (garageCapacity < 1 || garageCapacity > 3) return "unsupported garage capacity";
            if (waitingSlots < 0 || waitingSlots > 7) return "unsupported number of waiting slots";

            var used = new bool[CellCount];
            for (int i = 0; i < blockedCells.Length; i++)
            {
                int cell = blockedCells[i];
                if (cell < 0 || cell >= CellCount) return "blocked cell outside the board";
                used[cell] = true;
            }

            for (int i = 0; i < tunnels.Length; i++)
            {
                if (!InBounds(tunnels[i].x, tunnels[i].y)) return "tunnel " + i + " is outside the board";
                int cell = IndexOf(tunnels[i].x, tunnels[i].y);
                if (used[cell]) return "tunnel " + i + " overlaps something";
                used[cell] = true;
            }

            var perColor = new int[MaxColors];
            for (int i = 0; i < cars.Length; i++)
            {
                CarDef car = cars[i];
                if (car.length < 1) return "car " + i + " has no length";
                if (car.color >= MaxColors) return "car " + i + " has an unknown color";
                perColor[car.color]++;

                if (car.InTunnel)
                {
                    if (car.tunnel > tunnels.Length) return "car " + i + " is in an unknown tunnel";
                    TunnelDef tunnel = tunnels[car.tunnel - 1];
                    if (car.x != tunnel.x || car.y != tunnel.y || car.direction != tunnel.direction || car.length != 1)
                        return "car " + i + " does not match its tunnel";
                    continue;
                }

                for (int c = 0; c < car.length; c++)
                {
                    int x = car.CellX(c), y = car.CellY(c);
                    if (!InBounds(x, y)) return "car " + i + " is outside the board";
                    int cell = IndexOf(x, y);
                    if (used[cell]) return "car " + i + " overlaps something";
                    used[cell] = true;
                }
            }

            for (int i = 0; i < garageQueue.Length; i++)
            {
                if (garageQueue[i] >= MaxColors) return "garage " + i + " has an unknown color";
                perColor[garageQueue[i]] -= garageCapacity;
            }

            for (int color = 0; color < MaxColors; color++)
                if (perColor[color] != 0) return "cars and garages of color " + color + " do not add up";

            return null;
        }
    }
}
