using System.Collections.Generic;

namespace ParkSort.Core
{
    public enum GameStatus
    {
        Playing,
        Won,
        Lost,
    }

    public enum StepKind
    {
        CarToGarage,
        CarToWaiting,
        CarOverflow,
        GarageClosed,
        GarageOpened,
        WaitingToGarage,
        CarRevealed,
    }

    public struct Step
    {
        public StepKind kind;
        public int car;
        public int lane;
        public int queueIndex;
        public int slot;
        public int fromSlot;
        public byte color;
    }

    public sealed class MoveResult
    {
        public readonly List<Step> Steps = new List<Step>(32);
        public GameStatus Status;

        public void Clear()
        {
            Steps.Clear();
            Status = GameStatus.Playing;
        }
    }
}
