namespace ParkSort.Core
{
    public enum BotPolicy
    {
        Random,
        Greedy,
    }

    public struct DifficultyReport
    {
        public float randomWinRate;
        public float greedyWinRate;
        public float averageChoices;
    }

    public sealed class AutoPlayer
    {
        private readonly int[] _movable = new int[LevelDef.MaxCars];

        public bool Play(CompactLevel compact, BotPolicy policy, ref Rng rng, out float averageChoices)
        {
            ulong present = compact.AllCars;
            GarageState state = compact.InitialGarages();
            int capacity = compact.Level.waitingSlots;
            int turns = 0;
            int choices = 0;

            while (present != 0)
            {
                int count = 0;
                int matching = 0;
                for (int car = 0; car < compact.CarCount; car++)
                {
                    if ((present & (1UL << car)) == 0 || !compact.CanExit(car, present)) continue;

                    if (policy == BotPolicy.Greedy && compact.MatchesOpenGarage(ref state, compact.Colors[car]))
                    {
                        _movable[count++] = _movable[matching];
                        _movable[matching++] = car;
                    }
                    else
                    {
                        _movable[count++] = car;
                    }
                }

                turns++;
                choices += count;
                if (count == 0) break;

                int pick = matching > 0 ? _movable[rng.Range(matching)] : _movable[rng.Range(count)];
                if (!compact.Exit(ref state, compact.Colors[pick], capacity))
                {
                    averageChoices = choices / (float)turns;
                    return false;
                }

                present &= ~(1UL << pick);
            }

            averageChoices = turns == 0 ? 0f : choices / (float)turns;
            return present == 0;
        }

        public DifficultyReport Measure(LevelDef level, int runs, uint seed)
        {
            var compact = new CompactLevel(level);
            var rng = new Rng(seed);
            int randomWins = 0, greedyWins = 0;
            float choices = 0f;

            for (int i = 0; i < runs; i++)
            {
                if (Play(compact, BotPolicy.Random, ref rng, out float average)) randomWins++;
                choices += average;
                if (Play(compact, BotPolicy.Greedy, ref rng, out _)) greedyWins++;
            }

            return new DifficultyReport
            {
                randomWinRate = randomWins / (float)runs,
                greedyWinRate = greedyWins / (float)runs,
                averageChoices = choices / runs,
            };
        }
    }
}
