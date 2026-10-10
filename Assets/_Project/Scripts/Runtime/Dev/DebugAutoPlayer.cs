#if UNITY_EDITOR || DEBUG
using ParkSort.Core;
using ParkSort.Game;
using UnityEngine;

namespace ParkSort.Dev
{
    public sealed class DebugAutoPlayer : MonoBehaviour
    {
        [SerializeField] private GameController controller;
        [SerializeField] private float moveInterval = 0.35f;
        [SerializeField, Range(0f, 1f)] private float mistakeChance;
        [SerializeField] private float screenDelay = 1.2f;

        private readonly Solver _solver = new Solver();
        private float _timer;
        private Rng _rng = new Rng(12345);

        public int LevelsWon { get; private set; }
        public int LevelsFailed { get; private set; }

        public void Bind(GameController gameController, float interval, float mistakes)
        {
            controller = gameController;
            moveInterval = interval;
            mistakeChance = mistakes;
        }

        private void Update()
        {
            if (controller == null) return;

            _timer -= Time.deltaTime;
            if (_timer > 0f) return;

            switch (controller.Screen)
            {
                case GameScreen.Home:
                    controller.Play();
                    _timer = screenDelay;
                    break;

                case GameScreen.Won:
                    LevelsWon++;
                    controller.Play();
                    _timer = screenDelay;
                    break;

                case GameScreen.Failed:
                    LevelsFailed++;
                    if (!controller.UseExtraSlot()) controller.Restart();
                    _timer = screenDelay;
                    break;

                case GameScreen.Playing:
                    _timer = moveInterval;
                    Move();
                    break;
            }
        }

        private void Move()
        {
            ParkingGame game = controller.Game;
            if (game.Status != GameStatus.Playing) return;

            if (_rng.Chance(mistakeChance))
            {
                int count = game.Level.cars.Length;
                int start = _rng.Range(count);
                for (int i = 0; i < count; i++)
                {
                    int car = (start + i) % count;
                    if (!game.IsPresent(car)) continue;
                    controller.TapCar(car);
                    return;
                }
            }

            if (_solver.Solve(game) == SolveOutcome.Solved && _solver.SolutionLength > 0) controller.TapCar(_solver.Solution[0]);
        }
    }
}
#endif
