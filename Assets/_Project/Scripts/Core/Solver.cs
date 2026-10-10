using System.Collections.Generic;

namespace ParkSort.Core
{
    public enum SolveOutcome
    {
        Solved,
        Unsolvable,
        GaveUp,
    }

    public sealed class Solver
    {
        private readonly HashSet<StateKey> _dead = new HashSet<StateKey>();
        private readonly int[] _path = new int[LevelDef.MaxCars];
        private readonly int[][] _candidates = new int[LevelDef.MaxCars + 1][];
        private CompactLevel _compact;
        private int _waitingCapacity;
        private int _nodeLimit;
        private bool _gaveUp;

        public int Nodes { get; private set; }
        public int SolutionLength { get; private set; }
        public int[] Solution => _path;

        public Solver()
        {
            for (int i = 0; i < _candidates.Length; i++) _candidates[i] = new int[LevelDef.MaxCars];
        }

        public SolveOutcome Solve(LevelDef level, int nodeLimit = 200000)
        {
            var compact = new CompactLevel(level);
            return Solve(compact, compact.AllCars, compact.InitialGarages(), level.waitingSlots, nodeLimit);
        }

        public SolveOutcome Solve(ParkingGame game, int nodeLimit = 200000)
        {
            var compact = new CompactLevel(game.Level);
            CompactLevel.Export(game, out ulong present, out GarageState state);
            return Solve(compact, present, state, game.WaitingCapacity, nodeLimit);
        }

        public SolveOutcome Solve(CompactLevel compact, ulong present, GarageState state, int waitingCapacity, int nodeLimit)
        {
            _compact = compact;
            _waitingCapacity = waitingCapacity;
            _nodeLimit = nodeLimit;
            _gaveUp = false;
            _dead.Clear();
            Nodes = 0;
            SolutionLength = 0;

            if (Search(present, state, 0)) return SolveOutcome.Solved;
            return _gaveUp ? SolveOutcome.GaveUp : SolveOutcome.Unsolvable;
        }

        private bool Search(ulong present, GarageState state, int depth)
        {
            if (present == 0)
            {
                SolutionLength = depth;
                return true;
            }

            var key = new StateKey { present = present, garages = state.Pack() };
            if (_dead.Contains(key)) return false;

            if (++Nodes > _nodeLimit)
            {
                _gaveUp = true;
                return false;
            }

            int[] candidates = _candidates[depth];
            int count = 0;
            int matching = 0;
            for (int car = 0; car < _compact.CarCount; car++)
            {
                if ((present & (1UL << car)) == 0 || !_compact.CanExit(car, present)) continue;

                if (_compact.MatchesOpenGarage(ref state, _compact.Colors[car]))
                {
                    candidates[count++] = candidates[matching];
                    candidates[matching++] = car;
                }
                else
                {
                    candidates[count++] = car;
                }
            }

            for (int i = 0; i < count; i++)
            {
                int car = candidates[i];
                GarageState next = state;
                if (!_compact.Exit(ref next, _compact.Colors[car], _waitingCapacity)) continue;

                _path[depth] = car;
                if (Search(present & ~(1UL << car), next, depth + 1)) return true;
                if (_gaveUp) return false;
            }

            _dead.Add(key);
            return false;
        }
    }
}
