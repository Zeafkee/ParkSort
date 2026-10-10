using System;

namespace ParkSort.Core
{
    public sealed class ParkingGame
    {
        public const int NoCar = -1;
        private const int Blocked = -1;

        private readonly LevelDef _level;
        private readonly int[] _grid;
        private readonly bool[] _present;
        private readonly bool[] _revealed;
        private readonly int[] _laneQueueIndex;
        private readonly int[] _laneFill;
        private readonly int[] _laneCars;
        private readonly int[] _waiting;
        private readonly int[] _history;
        private readonly MoveResult _scratch = new MoveResult();
        private int _waitingCapacity;
        private int _nextQueue;
        private int _overflowCar = NoCar;

        public LevelDef Level => _level;
        public GameStatus Status { get; private set; }
        public int Remaining { get; private set; }
        public int Moves { get; private set; }
        public int WaitingCapacity => _waitingCapacity;
        public int WaitingCount { get; private set; }
        public int LaneCount => _laneQueueIndex.Length;
        public int NextQueueIndex => _nextQueue;
        public int OverflowCar => _overflowCar;
        public bool CanUndo => Moves > 0 && Status != GameStatus.Won;

        public ParkingGame(LevelDef level)
        {
            string problem = level.Validate();
            if (problem != null) throw new ArgumentException(problem, nameof(level));

            _level = level;
            _grid = new int[level.CellCount];
            _present = new bool[level.cars.Length];
            _revealed = new bool[level.cars.Length];
            _laneQueueIndex = new int[level.openGarages];
            _laneFill = new int[level.openGarages];
            _laneCars = new int[level.openGarages * level.garageCapacity];
            _waiting = new int[LevelDef.MaxCars];
            _history = new int[level.cars.Length];
            _waitingCapacity = level.waitingSlots;
            Reset();
        }

        public void Reset()
        {
            int capacity = _waitingCapacity;
            Restart();
            _waitingCapacity = capacity;
        }

        public void ResetAll()
        {
            Restart();
            _waitingCapacity = _level.waitingSlots;
        }

        private void Restart()
        {
            for (int i = 0; i < _grid.Length; i++) _grid[i] = 0;
            for (int i = 0; i < _level.blockedCells.Length; i++) _grid[_level.blockedCells[i]] = Blocked;
            for (int i = 0; i < _level.tunnels.Length; i++) _grid[_level.IndexOf(_level.tunnels[i].x, _level.tunnels[i].y)] = Blocked;

            for (int i = 0; i < _level.cars.Length; i++)
            {
                CarDef car = _level.cars[i];
                _present[i] = true;
                if (car.InTunnel) continue;
                for (int c = 0; c < car.length; c++)
                    _grid[_level.IndexOf(car.CellX(c), car.CellY(c))] = i + 1;
            }

            _nextQueue = 0;
            for (int lane = 0; lane < _laneQueueIndex.Length; lane++)
            {
                _laneFill[lane] = 0;
                _laneQueueIndex[lane] = _nextQueue < _level.garageQueue.Length ? _nextQueue++ : -1;
            }

            for (int i = 0; i < _laneCars.Length; i++) _laneCars[i] = NoCar;
            for (int i = 0; i < _waiting.Length; i++) _waiting[i] = NoCar;
            WaitingCount = 0;
            _overflowCar = NoCar;
            Remaining = _level.cars.Length;
            Moves = 0;
            Status = GameStatus.Playing;

            for (int i = 0; i < _revealed.Length; i++) _revealed[i] = IsVisibleNow(i);
        }

        public bool IsPresent(int car) => _present[car];
        public bool IsRevealed(int car) => _revealed[car];
        public int LaneQueueIndex(int lane) => _laneQueueIndex[lane];
        public int LaneFill(int lane) => _laneFill[lane];
        public int LaneCar(int lane, int slot) => _laneCars[lane * _level.garageCapacity + slot];
        public int WaitingCar(int slot) => _waiting[slot];

        public int LaneColor(int lane)
        {
            int index = _laneQueueIndex[lane];
            return index < 0 ? -1 : _level.garageQueue[index];
        }

        public bool IsTunnelFront(int car)
        {
            byte tunnel = _level.cars[car].tunnel;
            if (tunnel == 0 || !_present[car]) return false;
            for (int other = car + 1; other < _present.Length; other++)
                if (_present[other] && _level.cars[other].tunnel == tunnel) return false;
            return true;
        }

        public int TunnelFront(int tunnelIndex)
        {
            for (int car = _present.Length - 1; car >= 0; car--)
                if (_present[car] && _level.cars[car].tunnel == tunnelIndex + 1) return car;
            return NoCar;
        }

        public int TunnelCount(int tunnelIndex)
        {
            int count = 0;
            for (int car = 0; car < _present.Length; car++)
                if (_present[car] && _level.cars[car].tunnel == tunnelIndex + 1) count++;
            return count;
        }

        public int CarAt(int x, int y)
        {
            if (!_level.InBounds(x, y)) return NoCar;
            int value = _grid[_level.IndexOf(x, y)];
            if (value > 0) return value - 1;

            for (int i = 0; i < _level.tunnels.Length; i++)
                if (_level.tunnels[i].x == x && _level.tunnels[i].y == y) return TunnelFront(i);
            return NoCar;
        }

        public bool CanExit(int car)
        {
            if (!_present[car]) return false;
            if (_level.cars[car].InTunnel && !IsTunnelFront(car)) return false;
            return FirstBlocker(car) == 0;
        }

        public int BlockingCar(int car)
        {
            int value = FirstBlocker(car);
            return value > 0 ? value - 1 : NoCar;
        }

        public int FreeCellsAhead(int car)
        {
            CarDef def = _level.cars[car];
            int dx = Directions.Dx(def.direction), dy = Directions.Dy(def.direction);
            int x = def.HeadX + dx, y = def.HeadY + dy;
            int free = 0;
            while (_level.InBounds(x, y) && _grid[_level.IndexOf(x, y)] == 0)
            {
                free++;
                x += dx;
                y += dy;
            }

            return free;
        }

        private int FirstBlocker(int car)
        {
            CarDef def = _level.cars[car];
            int dx = Directions.Dx(def.direction), dy = Directions.Dy(def.direction);
            int x = def.HeadX + dx, y = def.HeadY + dy;
            while (_level.InBounds(x, y))
            {
                int value = _grid[_level.IndexOf(x, y)];
                if (value != 0) return value;
                x += dx;
                y += dy;
            }

            return 0;
        }

        private bool IsVisibleNow(int car)
        {
            CarDef def = _level.cars[car];
            if (def.InTunnel) return IsTunnelFront(car);
            return !def.hidden || FirstBlocker(car) == 0;
        }

        public bool TryMove(int car, MoveResult result)
        {
            result.Clear();
            if (Status != GameStatus.Playing || car < 0 || car >= _present.Length || !CanExit(car))
            {
                result.Status = Status;
                return false;
            }

            _history[Moves] = car;
            Apply(car, result);
            return true;
        }

        private void Apply(int car, MoveResult result)
        {
            CarDef def = _level.cars[car];
            if (!def.InTunnel)
            {
                for (int c = 0; c < def.length; c++)
                    _grid[_level.IndexOf(def.CellX(c), def.CellY(c))] = 0;
            }

            _present[car] = false;
            _revealed[car] = true;
            Remaining--;
            Moves++;

            int lane = FindLane(def.color);
            if (lane >= 0)
            {
                result.Steps.Add(new Step
                {
                    kind = StepKind.CarToGarage, car = car, lane = lane, queueIndex = _laneQueueIndex[lane],
                    slot = _laneFill[lane], color = def.color,
                });
                _laneCars[lane * _level.garageCapacity + _laneFill[lane]] = car;
                _laneFill[lane]++;
                ResolveGarages(result);
            }
            else
            {
                int slot = FreeWaitingSlot();
                if (slot >= 0)
                {
                    _waiting[slot] = car;
                    WaitingCount++;
                    result.Steps.Add(new Step { kind = StepKind.CarToWaiting, car = car, slot = slot, color = def.color });
                }
                else
                {
                    _overflowCar = car;
                    Status = GameStatus.Lost;
                    result.Steps.Add(new Step { kind = StepKind.CarOverflow, car = car, slot = _waitingCapacity, color = def.color });
                }
            }

            RevealFreedCars(result);
            if (Status == GameStatus.Playing && Remaining == 0 && WaitingCount == 0) Status = GameStatus.Won;
            result.Status = Status;
        }

        public bool Undo()
        {
            if (!CanUndo) return false;

            int keep = Moves - 1;
            Reset();
            for (int i = 0; i < keep; i++) Apply(_history[i], _scratch);
            _scratch.Clear();
            return true;
        }

        public bool AddWaitingSlot(MoveResult result)
        {
            result.Clear();
            if (_waitingCapacity >= 7 || Status == GameStatus.Won)
            {
                result.Status = Status;
                return false;
            }

            _waitingCapacity++;
            if (Status == GameStatus.Lost && _overflowCar != NoCar)
            {
                int slot = _waitingCapacity - 1;
                _waiting[slot] = _overflowCar;
                WaitingCount++;
                result.Steps.Add(new Step
                {
                    kind = StepKind.CarToWaiting, car = _overflowCar, slot = slot, color = _level.cars[_overflowCar].color,
                });
                _overflowCar = NoCar;
                Status = GameStatus.Playing;
            }

            result.Status = Status;
            return true;
        }

        private int FindLane(byte color)
        {
            for (int lane = 0; lane < _laneQueueIndex.Length; lane++)
            {
                int index = _laneQueueIndex[lane];
                if (index >= 0 && _level.garageQueue[index] == color && _laneFill[lane] < _level.garageCapacity) return lane;
            }

            return -1;
        }

        private int FreeWaitingSlot()
        {
            for (int slot = 0; slot < _waitingCapacity; slot++)
                if (_waiting[slot] == NoCar) return slot;
            return -1;
        }

        private void ResolveGarages(MoveResult result)
        {
            int capacity = _level.garageCapacity;
            bool changed = true;
            while (changed)
            {
                changed = false;
                for (int lane = 0; lane < _laneQueueIndex.Length; lane++)
                {
                    if (_laneQueueIndex[lane] < 0 || _laneFill[lane] < capacity) continue;

                    result.Steps.Add(new Step
                    {
                        kind = StepKind.GarageClosed, car = NoCar, lane = lane, queueIndex = _laneQueueIndex[lane],
                        color = _level.garageQueue[_laneQueueIndex[lane]],
                    });
                    _laneFill[lane] = 0;
                    _laneQueueIndex[lane] = -1;
                    for (int slot = 0; slot < capacity; slot++) _laneCars[lane * capacity + slot] = NoCar;
                    changed = true;

                    if (_nextQueue >= _level.garageQueue.Length) continue;

                    int index = _nextQueue++;
                    byte color = _level.garageQueue[index];
                    _laneQueueIndex[lane] = index;
                    result.Steps.Add(new Step { kind = StepKind.GarageOpened, car = NoCar, lane = lane, queueIndex = index, color = color });

                    for (int slot = 0; slot < _waitingCapacity && _laneFill[lane] < capacity; slot++)
                    {
                        int waitingCar = _waiting[slot];
                        if (waitingCar == NoCar || _level.cars[waitingCar].color != color) continue;

                        _waiting[slot] = NoCar;
                        WaitingCount--;
                        result.Steps.Add(new Step
                        {
                            kind = StepKind.WaitingToGarage, car = waitingCar, lane = lane, queueIndex = index,
                            slot = _laneFill[lane], fromSlot = slot, color = color,
                        });
                        _laneCars[lane * capacity + _laneFill[lane]] = waitingCar;
                        _laneFill[lane]++;
                    }
                }
            }
        }

        private void RevealFreedCars(MoveResult result)
        {
            for (int i = 0; i < _revealed.Length; i++)
            {
                if (_revealed[i] || !_present[i] || !IsVisibleNow(i)) continue;
                _revealed[i] = true;
                result.Steps.Add(new Step { kind = StepKind.CarRevealed, car = i, color = _level.cars[i].color });
            }
        }
    }
}
