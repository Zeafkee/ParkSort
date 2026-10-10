using System.Linq;
using NUnit.Framework;

namespace ParkSort.Core.Tests
{
    public class TunnelAndUndoTests
    {
        private static CarDef Car(int x, int y, Direction direction, int length, int color) => new CarDef
        {
            x = (byte)x, y = (byte)y, direction = direction, length = (byte)length, color = (byte)color,
        };

        private static CarDef TunnelCar(TunnelDef tunnel, int tunnelIndex, int color) => new CarDef
        {
            x = tunnel.x, y = tunnel.y, direction = tunnel.direction, length = 1, color = (byte)color, tunnel = (byte)(tunnelIndex + 1),
        };

        private static LevelDef TunnelLevel()
        {
            var tunnel = new TunnelDef { x = 0, y = 0, direction = Direction.Right };
            return new LevelDef
            {
                width = 4, height = 2, garageCapacity = 1, openGarages = 2, waitingSlots = 3,
                garageQueue = new byte[] { 0, 1, 2 },
                tunnels = new[] { tunnel },
                cars = new[]
                {
                    TunnelCar(tunnel, 0, 2),
                    TunnelCar(tunnel, 0, 1),
                    Car(2, 0, Direction.Up, 2, 0),
                },
            };
        }

        [Test]
        public void Tunnel_OnlyItsFrontCarCanLeave_AndOnlyWhenTheWayIsClear()
        {
            var game = new ParkingGame(TunnelLevel());
            var result = new MoveResult();

            Assert.IsTrue(game.IsTunnelFront(1));
            Assert.IsFalse(game.IsTunnelFront(0));
            Assert.IsFalse(game.CanExit(1));
            Assert.AreEqual(2, game.BlockingCar(1));
            Assert.IsFalse(game.CanExit(0));

            game.TryMove(2, result);
            Assert.IsTrue(game.CanExit(1));
            Assert.IsFalse(game.CanExit(0));

            game.TryMove(1, result);
            Assert.IsTrue(game.IsTunnelFront(0));
            Assert.IsTrue(game.CanExit(0));
            Assert.IsTrue(result.Steps.Any(s => s.kind == StepKind.CarRevealed && s.car == 0));

            game.TryMove(0, result);
            Assert.AreEqual(GameStatus.Won, game.Status);
        }

        [Test]
        public void Tunnel_CellIsPickedAsItsFrontCar_AndCountsDown()
        {
            var game = new ParkingGame(TunnelLevel());
            var result = new MoveResult();

            Assert.AreEqual(1, game.CarAt(0, 0));
            Assert.AreEqual(2, game.TunnelCount(0));

            game.TryMove(2, result);
            game.TryMove(1, result);

            Assert.AreEqual(0, game.CarAt(0, 0));
            Assert.AreEqual(1, game.TunnelCount(0));
        }

        [Test]
        public void Tunnel_StaysAnObstacleAfterItIsEmpty()
        {
            var tunnel = new TunnelDef { x = 2, y = 0, direction = Direction.Up };
            var level = new LevelDef
            {
                width = 4, height = 2, garageCapacity = 1, openGarages = 2, waitingSlots = 3,
                garageQueue = new byte[] { 0, 1 },
                tunnels = new[] { tunnel },
                cars = new[] { Car(0, 0, Direction.Right, 2, 0), TunnelCar(tunnel, 0, 1) },
            };
            var game = new ParkingGame(level);
            var result = new MoveResult();

            game.TryMove(1, result);

            Assert.IsFalse(game.CanExit(0));
            Assert.AreEqual(SolveOutcome.Unsolvable, new Solver().Solve(level));
        }

        [Test]
        public void Solver_HandlesTunnels()
        {
            LevelDef level = TunnelLevel();
            var solver = new Solver();

            Assert.AreEqual(SolveOutcome.Solved, solver.Solve(level));
            CollectionAssert.AreEqual(new[] { 2, 1, 0 }, solver.Solution.Take(solver.SolutionLength).ToArray());
        }

        [Test]
        public void Generator_BuildsSolvableLevelsWithTunnels()
        {
            GeneratorSettings settings = GeneratorSettings.Default;
            settings.width = 6;
            settings.height = 7;
            settings.cars = 15;
            settings.colors = 4;
            settings.longCarChance = 0.25f;
            settings.tunnels = 2;
            settings.carsPerTunnel = 3;
            settings.directChance = 0.6f;

            var generator = new LevelGenerator();
            var solver = new Solver();
            int produced = 0;
            int tunnelCars = 0;

            for (uint seed = 1; seed <= 30; seed++)
            {
                LevelDef level = generator.Generate(settings, seed);
                if (level == null) continue;
                produced++;
                tunnelCars += level.cars.Count(c => c.InTunnel);

                Assert.IsNull(level.Validate());
                Assert.AreEqual(2, level.tunnels.Length);
                Assert.AreEqual(SolveOutcome.Solved, solver.Solve(level));

                var game = new ParkingGame(level);
                var result = new MoveResult();
                for (int i = 0; i < solver.SolutionLength; i++) Assert.IsTrue(game.TryMove(solver.Solution[i], result));
                Assert.AreEqual(GameStatus.Won, game.Status);
            }

            Assert.GreaterOrEqual(produced, 20);
            Assert.Greater(tunnelCars, produced * 3);
        }

        [Test]
        public void Undo_ReturnsToTheStateBeforeTheLastMove()
        {
            LevelDef level = new LevelGenerator().Generate(GeneratorSettings.Default, 3);
            var solver = new Solver();
            solver.Solve(level);
            int first = solver.Solution[0], second = solver.Solution[1];
            var game = new ParkingGame(level);
            var result = new MoveResult();

            Assert.IsFalse(game.CanUndo);
            game.TryMove(first, result);
            int waitingAfterFirst = game.WaitingCount;
            int fillAfterFirst = game.LaneFill(0) + game.LaneFill(1);
            game.TryMove(second, result);

            Assert.IsTrue(game.Undo());

            Assert.AreEqual(1, game.Moves);
            Assert.IsFalse(game.IsPresent(first));
            Assert.IsTrue(game.IsPresent(second));
            Assert.AreEqual(waitingAfterFirst, game.WaitingCount);
            Assert.AreEqual(fillAfterFirst, game.LaneFill(0) + game.LaneFill(1));
            Assert.IsTrue(game.CanExit(second));

            Assert.IsTrue(game.Undo());
            Assert.AreEqual(level.cars.Length, game.Remaining);
            Assert.IsFalse(game.Undo());
        }

        [Test]
        public void Undo_RevivesALostGame_AndKeepsBoughtSlots()
        {
            var level = new LevelDef
            {
                width = 2, height = 4, garageCapacity = 1, openGarages = 2, waitingSlots = 1,
                garageQueue = new byte[] { 0, 1, 2, 3 },
                cars = new[]
                {
                    Car(0, 0, Direction.Right, 2, 2),
                    Car(0, 1, Direction.Right, 2, 3),
                    Car(0, 2, Direction.Right, 2, 1),
                    Car(0, 3, Direction.Right, 2, 0),
                },
            };
            var game = new ParkingGame(level);
            var result = new MoveResult();
            game.TryMove(0, result);
            game.TryMove(1, result);
            Assert.AreEqual(GameStatus.Lost, game.Status);

            Assert.IsTrue(game.Undo());
            Assert.AreEqual(GameStatus.Playing, game.Status);
            Assert.AreEqual(1, game.WaitingCount);
            Assert.AreEqual(ParkingGame.NoCar, game.OverflowCar);

            game.AddWaitingSlot(result);
            game.TryMove(1, result);
            game.Undo();
            Assert.AreEqual(2, game.WaitingCapacity);
        }

        [Test]
        public void LaneCars_ListTheCarsStandingInAGarage()
        {
            var level = new LevelDef
            {
                width = 2, height = 2, garageCapacity = 2, openGarages = 1, waitingSlots = 2,
                garageQueue = new byte[] { 0 },
                cars = new[] { Car(0, 0, Direction.Right, 2, 0), Car(0, 1, Direction.Right, 2, 0) },
            };
            var game = new ParkingGame(level);
            var result = new MoveResult();

            game.TryMove(1, result);

            Assert.AreEqual(1, game.LaneCar(0, 0));
            Assert.AreEqual(ParkingGame.NoCar, game.LaneCar(0, 1));
        }
    }
}
