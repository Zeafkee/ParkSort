using System.Linq;
using NUnit.Framework;

namespace ParkSort.Core.Tests
{
    public class ParkingGameTests
    {
        private static CarDef Car(int x, int y, Direction direction, int length, int color, bool hidden = false) => new CarDef
        {
            x = (byte)x, y = (byte)y, direction = direction, length = (byte)length, color = (byte)color, hidden = hidden,
        };

        private static LevelDef Level(int width, int height, int capacity, int waiting, byte[] queue, params CarDef[] cars) => new LevelDef
        {
            width = width, height = height, cars = cars, garageQueue = queue, garageCapacity = capacity,
            openGarages = 2, waitingSlots = waiting,
        };

        private static LevelDef Row() => Level(4, 1, 1, 2, new byte[] { 0, 1 },
            Car(0, 0, Direction.Right, 2, 0),
            Car(2, 0, Direction.Right, 2, 1));

        [Test]
        public void Car_WithAnotherCarInFront_CannotExit()
        {
            var game = new ParkingGame(Row());

            Assert.IsFalse(game.CanExit(0));
            Assert.AreEqual(1, game.BlockingCar(0));
            Assert.IsTrue(game.CanExit(1));
        }

        [Test]
        public void BlockedMove_ChangesNothing()
        {
            var game = new ParkingGame(Row());
            var result = new MoveResult();

            Assert.IsFalse(game.TryMove(0, result));
            Assert.AreEqual(2, game.Remaining);
            Assert.AreEqual(0, result.Steps.Count);
        }

        [Test]
        public void Car_GoesToTheGarageOfItsColor_AndTheFullGarageCloses()
        {
            var game = new ParkingGame(Row());
            var result = new MoveResult();

            Assert.IsTrue(game.TryMove(1, result));

            Assert.AreEqual(StepKind.CarToGarage, result.Steps[0].kind);
            Assert.AreEqual(1, result.Steps[0].lane);
            Assert.AreEqual(StepKind.GarageClosed, result.Steps[1].kind);
            Assert.AreEqual(-1, game.LaneQueueIndex(1));
            Assert.IsTrue(game.CanExit(0));
        }

        [Test]
        public void ClearingTheLot_Wins()
        {
            var game = new ParkingGame(Row());
            var result = new MoveResult();

            game.TryMove(1, result);
            game.TryMove(0, result);

            Assert.AreEqual(GameStatus.Won, game.Status);
            Assert.AreEqual(GameStatus.Won, result.Status);
        }

        private static LevelDef ThreeColors() => Level(2, 3, 1, 1, new byte[] { 0, 1, 2 },
            Car(0, 0, Direction.Right, 2, 2),
            Car(0, 1, Direction.Right, 2, 1),
            Car(0, 2, Direction.Right, 2, 0));

        [Test]
        public void Car_WithoutAnOpenGarage_Waits_AndEntersWhenItsGarageOpens()
        {
            var game = new ParkingGame(ThreeColors());
            var result = new MoveResult();

            game.TryMove(0, result);
            Assert.AreEqual(StepKind.CarToWaiting, result.Steps[0].kind);
            Assert.AreEqual(1, game.WaitingCount);

            game.TryMove(2, result);
            StepKind[] kinds = result.Steps.Select(s => s.kind).ToArray();
            CollectionAssert.AreEqual(new[]
            {
                StepKind.CarToGarage, StepKind.GarageClosed, StepKind.GarageOpened, StepKind.WaitingToGarage, StepKind.GarageClosed,
            }, kinds);
            Assert.AreEqual(0, game.WaitingCount);
        }

        private static LevelDef Overflow() => Level(2, 4, 1, 1, new byte[] { 0, 1, 2, 3 },
            Car(0, 0, Direction.Right, 2, 2),
            Car(0, 1, Direction.Right, 2, 3),
            Car(0, 2, Direction.Right, 2, 1),
            Car(0, 3, Direction.Right, 2, 0));

        [Test]
        public void FullWaitingBay_Loses()
        {
            var game = new ParkingGame(Overflow());
            var result = new MoveResult();

            game.TryMove(0, result);
            game.TryMove(1, result);

            Assert.AreEqual(GameStatus.Lost, game.Status);
            Assert.AreEqual(StepKind.CarOverflow, result.Steps[0].kind);
            Assert.AreEqual(1, game.OverflowCar);
        }

        [Test]
        public void ExtraWaitingSlot_RevivesALostGame()
        {
            var game = new ParkingGame(Overflow());
            var result = new MoveResult();
            game.TryMove(0, result);
            game.TryMove(1, result);

            Assert.IsTrue(game.AddWaitingSlot(result));

            Assert.AreEqual(GameStatus.Playing, game.Status);
            Assert.AreEqual(2, game.WaitingCount);
            Assert.AreEqual(StepKind.CarToWaiting, result.Steps[0].kind);

            game.TryMove(3, result);
            game.TryMove(2, result);
            Assert.AreEqual(GameStatus.Won, game.Status);
        }

        [Test]
        public void HiddenCar_IsRevealed_WhenItsWayBecomesClear()
        {
            LevelDef level = Level(4, 1, 1, 2, new byte[] { 0, 1 },
                Car(0, 0, Direction.Right, 2, 0, hidden: true),
                Car(2, 0, Direction.Right, 2, 1));
            var game = new ParkingGame(level);
            var result = new MoveResult();

            Assert.IsFalse(game.IsRevealed(0));
            game.TryMove(1, result);

            Assert.IsTrue(game.IsRevealed(0));
            Assert.IsTrue(result.Steps.Any(s => s.kind == StepKind.CarRevealed && s.car == 0));
        }

        [Test]
        public void Obstacle_BlocksACarForever()
        {
            LevelDef level = Level(3, 1, 1, 2, new byte[] { 0 }, Car(0, 0, Direction.Right, 2, 0));
            level.blockedCells = new[] { 2 };
            var game = new ParkingGame(level);

            Assert.IsFalse(game.CanExit(0));
            Assert.AreEqual(ParkingGame.NoCar, game.BlockingCar(0));
        }

        [Test]
        public void Validate_RejectsOverlapsAndWrongCounts()
        {
            LevelDef overlap = Level(3, 1, 1, 2, new byte[] { 0, 1 },
                Car(0, 0, Direction.Right, 2, 0),
                Car(1, 0, Direction.Right, 2, 1));
            LevelDef counts = Level(4, 1, 1, 2, new byte[] { 0 },
                Car(0, 0, Direction.Right, 2, 0),
                Car(2, 0, Direction.Right, 2, 1));

            Assert.IsNotNull(overlap.Validate());
            Assert.IsNotNull(counts.Validate());
            Assert.IsNull(Row().Validate());
        }

        [Test]
        public void Reset_RestoresTheStartPosition()
        {
            var game = new ParkingGame(Row());
            var result = new MoveResult();
            game.TryMove(1, result);

            game.Reset();

            Assert.AreEqual(2, game.Remaining);
            Assert.IsFalse(game.CanExit(0));
            Assert.AreEqual(0, game.LaneQueueIndex(0));
            Assert.AreEqual(GameStatus.Playing, game.Status);
        }
    }
}
