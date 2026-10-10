using NUnit.Framework;
using UnityEngine.TestTools.Constraints;
using Is = UnityEngine.TestTools.Constraints.Is;

namespace ParkSort.Core.Tests
{
    public class SolverAndGeneratorTests
    {
        private static CarDef Car(int x, int y, Direction direction, int length, int color) => new CarDef
        {
            x = (byte)x, y = (byte)y, direction = direction, length = (byte)length, color = (byte)color,
        };

        private static GeneratorSettings Medium => new GeneratorSettings
        {
            width = 6, height = 7, cars = 12, colors = 4, longCarChance = 0.25f, hiddenChance = 0.2f, obstacles = 1,
            waitingSlots = 5, directChance = 0.6f, lookahead = 2, plannedWaitingLimit = 3,
        };

        private static bool Replay(LevelDef level, Solver solver)
        {
            var game = new ParkingGame(level);
            var result = new MoveResult();
            for (int i = 0; i < solver.SolutionLength; i++)
                if (!game.TryMove(solver.Solution[i], result)) return false;
            return game.Status == GameStatus.Won;
        }

        [Test]
        public void Solver_FindsTheOnlyOrder()
        {
            var level = new LevelDef
            {
                width = 2, height = 3, garageCapacity = 1, openGarages = 2, waitingSlots = 0,
                garageQueue = new byte[] { 0, 1, 2 },
                cars = new[]
                {
                    Car(0, 0, Direction.Right, 2, 2),
                    Car(0, 1, Direction.Right, 2, 1),
                    Car(0, 2, Direction.Right, 2, 0),
                },
            };
            var solver = new Solver();

            Assert.AreEqual(SolveOutcome.Solved, solver.Solve(level));
            Assert.AreEqual(3, solver.SolutionLength);
            Assert.AreNotEqual(0, solver.Solution[0]);
            Assert.IsTrue(Replay(level, solver));
        }

        [Test]
        public void Solver_ReportsCarsFacingEachOtherAsUnsolvable()
        {
            var level = new LevelDef
            {
                width = 4, height = 1, garageCapacity = 1, openGarages = 2, waitingSlots = 5,
                garageQueue = new byte[] { 0, 1 },
                cars = new[]
                {
                    Car(0, 0, Direction.Right, 2, 0),
                    Car(3, 0, Direction.Left, 2, 1),
                },
            };

            Assert.AreEqual(SolveOutcome.Unsolvable, new Solver().Solve(level));
        }

        [Test]
        public void Solver_ReportsAColorTrapAsUnsolvable_UntilThereIsRoomToWait()
        {
            var level = new LevelDef
            {
                width = 4, height = 1, garageCapacity = 1, openGarages = 1, waitingSlots = 0,
                garageQueue = new byte[] { 0, 1 },
                cars = new[]
                {
                    Car(0, 0, Direction.Right, 2, 0),
                    Car(2, 0, Direction.Right, 2, 1),
                },
            };

            Assert.AreEqual(SolveOutcome.Unsolvable, new Solver().Solve(level));

            level.waitingSlots = 1;
            Assert.AreEqual(SolveOutcome.Solved, new Solver().Solve(level));
        }

        [Test]
        public void Solver_ContinuesFromTheMiddleOfAGame()
        {
            LevelDef level = new LevelGenerator().Generate(Medium, 11);
            var game = new ParkingGame(level);
            var solver = new Solver();
            var result = new MoveResult();

            Assert.AreEqual(SolveOutcome.Solved, solver.Solve(level));
            int first = solver.Solution[0];
            game.TryMove(first, result);

            Assert.AreEqual(SolveOutcome.Solved, solver.Solve(game));
            Assert.AreEqual(level.cars.Length - 1, solver.SolutionLength);
            for (int i = 0; i < solver.SolutionLength; i++) Assert.IsTrue(game.TryMove(solver.Solution[i], result));
            Assert.AreEqual(GameStatus.Won, game.Status);
        }

        [Test]
        public void Generator_IsDeterministicForASeed()
        {
            LevelDef a = new LevelGenerator().Generate(Medium, 1234);
            LevelDef b = new LevelGenerator().Generate(Medium, 1234);

            Assert.IsNotNull(a);
            Assert.AreEqual(a.cars.Length, b.cars.Length);
            for (int i = 0; i < a.cars.Length; i++)
            {
                Assert.AreEqual(a.cars[i].x, b.cars[i].x);
                Assert.AreEqual(a.cars[i].y, b.cars[i].y);
                Assert.AreEqual(a.cars[i].direction, b.cars[i].direction);
                Assert.AreEqual(a.cars[i].color, b.cars[i].color);
            }

            CollectionAssert.AreEqual(a.garageQueue, b.garageQueue);
        }

        [Test]
        public void Generator_ProducesValidSolvableLevels()
        {
            var generator = new LevelGenerator();
            var solver = new Solver();
            int produced = 0;

            for (uint seed = 1; seed <= 40; seed++)
            {
                LevelDef level = generator.Generate(Medium, seed);
                if (level == null) continue;
                produced++;

                Assert.IsNull(level.Validate());
                Assert.AreEqual(12, level.cars.Length);
                Assert.AreEqual(SolveOutcome.Solved, solver.Solve(level));
                Assert.IsTrue(Replay(level, solver), "the solution of seed " + seed + " does not win the real game");
            }

            Assert.GreaterOrEqual(produced, 30);
        }

        [Test]
        public void CompactSimulation_AgreesWithTheRealGame()
        {
            var generator = new LevelGenerator();
            var rng = new Rng(99);

            for (uint seed = 1; seed <= 20; seed++)
            {
                LevelDef level = generator.Generate(Medium, seed);
                if (level == null) continue;

                var compact = new CompactLevel(level);
                var game = new ParkingGame(level);
                var result = new MoveResult();
                ulong present = compact.AllCars;
                GarageState state = compact.InitialGarages();
                bool compactLost = false;

                while (game.Status == GameStatus.Playing)
                {
                    int pick = -1;
                    int seen = 0;
                    for (int car = 0; car < level.cars.Length; car++)
                    {
                        Assert.AreEqual(game.CanExit(car), (present & (1UL << car)) != 0 && compact.CanExit(car, present));
                        if (game.CanExit(car) && rng.Range(++seen) == 0) pick = car;
                    }

                    game.TryMove(pick, result);
                    compactLost = !compact.Exit(ref state, level.cars[pick].color, level.waitingSlots);
                    present &= ~(1UL << pick);
                    Assert.AreEqual(game.Status == GameStatus.Lost, compactLost);
                    if (!compactLost) Assert.AreEqual(game.WaitingCount, state.waitingTotal);
                }
            }
        }

        [Test]
        public void HarderSettings_LowerTheBotWinRate()
        {
            GeneratorSettings easy = Medium;
            easy.directChance = 0.95f;
            easy.colors = 3;
            GeneratorSettings hard = Medium;
            hard.directChance = 0.3f;
            hard.colors = 6;
            hard.width = 7;
            hard.height = 8;
            hard.cars = 18;
            hard.lookahead = 3;
            hard.plannedWaitingLimit = 5;

            var generator = new LevelGenerator();
            var bot = new AutoPlayer();
            float easyRate = 0f, hardRate = 0f;
            int count = 0;
            for (uint seed = 1; seed <= 12; seed++)
            {
                LevelDef a = generator.Generate(easy, seed);
                LevelDef b = generator.Generate(hard, seed);
                if (a == null || b == null) continue;
                easyRate += bot.Measure(a, 100, seed).greedyWinRate;
                hardRate += bot.Measure(b, 100, seed).greedyWinRate;
                count++;
            }

            Assert.Greater(count, 5);
            Assert.Greater(easyRate / count, hardRate / count);
        }

        [Test]
        public void Moves_DoNotAllocate()
        {
            LevelDef level = new LevelGenerator().Generate(Medium, 5);
            var solver = new Solver();
            solver.Solve(level);
            var game = new ParkingGame(level);
            var result = new MoveResult();
            int length = solver.SolutionLength;
            var order = new int[length];
            System.Array.Copy(solver.Solution, order, length);

            for (int i = 0; i < length; i++) game.TryMove(order[i], result);
            game.Reset();

            Assert.That(() =>
            {
                for (int i = 0; i < length; i++) game.TryMove(order[i], result);
            }, Is.Not.AllocatingGCMemory());
            Assert.AreEqual(GameStatus.Won, game.Status);
        }
    }
}
