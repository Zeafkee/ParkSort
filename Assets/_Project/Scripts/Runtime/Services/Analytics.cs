using UnityEngine;

namespace ParkSort.Services
{
    public interface IAnalytics
    {
        void LevelStarted(int level);
        void LevelCompleted(int level, int moves, float seconds, int hintsUsed, int slotsUsed);
        void LevelFailed(int level, int moves, float seconds, int carsLeft);
        void BoosterUsed(int level, string booster);
    }

    public static class Analytics
    {
        public static IAnalytics Current = new LogAnalytics();
    }

    public sealed class LogAnalytics : IAnalytics
    {
        public bool Verbose;

        public void LevelStarted(int level) => Write("level_start", level, string.Empty);

        public void LevelCompleted(int level, int moves, float seconds, int hintsUsed, int slotsUsed) =>
            Write("level_complete", level, $"moves={moves} seconds={seconds:F1} hints={hintsUsed} slots={slotsUsed}");

        public void LevelFailed(int level, int moves, float seconds, int carsLeft) =>
            Write("level_fail", level, $"moves={moves} seconds={seconds:F1} cars_left={carsLeft}");

        public void BoosterUsed(int level, string booster) => Write("booster_used", level, booster);

        private void Write(string name, int level, string details)
        {
            if (Verbose) Debug.Log($"[analytics] {name} level={level} {details}");
        }
    }
}
