using System;
using ParkSort.Audio;
using ParkSort.Core;
using ParkSort.Data;
using ParkSort.Save;
using ParkSort.Services;
using ParkSort.View;
using UnityEngine;

namespace ParkSort.Game
{
    public enum GameScreen
    {
        Home,
        Playing,
        Won,
        Failed,
    }

    public sealed class GameController : MonoBehaviour
    {
        [SerializeField] private LevelCatalog catalog;
        [SerializeField] private SkinCatalog shop;
        [SerializeField] private BoardView board;
        [SerializeField] private CameraRig cameraRig;

        [Header("Economy")]
        [SerializeField] private int winReward = 25;
        [SerializeField] private int hintCost = 40;
        [SerializeField] private int extraSlotCost = 60;
        [SerializeField] private int repeatedSlotCost = 150;
        [SerializeField] private int undoCost = 30;
        [SerializeField] private int maxExtraSlotsPerLevel = 2;
        [SerializeField] private int guidedLevels = 2;
        [SerializeField] private int levelsWithoutMenu = 5;

        private readonly MoveResult _result = new MoveResult();
        private readonly Solver _solver = new Solver();
        private readonly LevelGenerator _generator = new LevelGenerator();
        private ParkingGame _game;
        private float _startTime;
        private int _hintsUsed;
        private int _slotsUsed;
        private bool _failPending;
        private bool _rewardDoubled;
        private bool _adRunning;

        public GameScreen Screen { get; private set; } = GameScreen.Home;
        public ParkingGame Game => _game;
        public BoardView Board => board;
        public int LevelIndex => SaveSystem.Data.level;
        public int LevelNumber => SaveSystem.Data.level + 1;
        public int Coins => SaveSystem.Data.coins;
        public int Hints => SaveSystem.Data.hints;
        public int ExtraSlots => SaveSystem.Data.extraSlots;
        public int HintCost => hintCost;
        public int ExtraSlotCost => _slotsUsed == 0 ? extraSlotCost : repeatedSlotCost;
        public SkinCatalog Shop => shop;
        public int SelectedSkin => Mathf.Clamp(SaveSystem.Data.skin, 0, shop.skins.Length - 1);
        public bool RewardDoubled => _rewardDoubled;
        public bool CanDoubleReward => Screen == GameScreen.Won && !_rewardDoubled && !_adRunning && Ads.Current.RewardedReady;
        public int Undos => SaveSystem.Data.undos;
        public int UndoCost => undoCost;
        public LevelEntry Entry { get; private set; }
        public LevelDifficulty Difficulty => Entry != null ? Entry.difficulty : LevelDifficulty.Normal;
        public int WinReward => winReward;
        public string Caption { get; private set; }
        public int GuideCar { get; private set; } = ParkingGame.NoCar;
        public bool CanAddSlot => _game != null && _slotsUsed < maxExtraSlotsPerLevel && _game.WaitingCapacity < 7;

        public event Action<GameScreen> ScreenChanged;
        public event Action LevelStarted;
        public event Action BoardChanged;
        public event Action InventoryChanged;

        private void Awake()
        {
            board.Completed += HandleCompleted;
            board.Failed += HandleFailed;
        }

        private void Start()
        {
            board.ApplySkin(shop.skins[SelectedSkin]);
            PrepareLevel();
            if (LevelIndex < levelsWithoutMenu) BeginPlaying();
            else SetScreen(GameScreen.Home);
        }

        private void OnDestroy()
        {
            if (board == null) return;
            board.Completed -= HandleCompleted;
            board.Failed -= HandleFailed;
        }

        private LevelDef LoadLevel(int index, out string caption)
        {
            caption = null;
            Entry = null;
            if (index < catalog.Count)
            {
                Entry = catalog.levels[index];
                caption = Entry.caption;
                return Entry.level;
            }

            uint seed = (uint)(index + 1) * 2654435761u + 17u;
            LevelDef generated = _generator.Generate(catalog.endlessSettings, seed);
            return generated ?? catalog.levels[catalog.Count - 1 - index % Mathf.Min(10, catalog.Count)].level;
        }

        private void PrepareLevel()
        {
            LevelDef level = LoadLevel(LevelIndex, out string caption);
            Caption = caption;
            _game = new ParkingGame(level);
            _hintsUsed = 0;
            _slotsUsed = 0;
            _failPending = false;
            GuideCar = ParkingGame.NoCar;
            board.Build(_game);
            BoardChanged?.Invoke();
        }

        public void Play()
        {
            if (Screen == GameScreen.Playing) return;
            if (Screen != GameScreen.Home) PrepareLevel();
            BeginPlaying();
        }

        public void Restart()
        {
            PrepareLevel();
            BeginPlaying();
        }

        public void GoHome()
        {
            PrepareLevel();
            SetScreen(GameScreen.Home);
        }

        private void BeginPlaying()
        {
            _startTime = Time.time;
            Analytics.Current.LevelStarted(LevelNumber);
            SetScreen(GameScreen.Playing);
            LevelStarted?.Invoke();
            UpdateGuide();
        }

        public void Tap(Vector2 screenPosition)
        {
            if (Screen != GameScreen.Playing || _game.Status != GameStatus.Playing) return;

            Ray ray = cameraRig.Camera.ScreenPointToRay(screenPosition);
            int car = board.PickCar(ray);
            if (car == ParkingGame.NoCar) return;
            TapCar(car);
        }

        public void TapCar(int car)
        {
            if (Screen != GameScreen.Playing || _game.Status != GameStatus.Playing) return;

            if (!_game.TryMove(car, _result))
            {
                if (_game.IsPresent(car)) board.Bump(car);
                return;
            }

            AudioManager.Play(Sfx.Tap);
            board.Play(_result);
            if (_result.Status == GameStatus.Lost) _failPending = true;
            BoardChanged?.Invoke();
            UpdateGuide();
        }

        private void UpdateGuide()
        {
            int guide = ParkingGame.NoCar;
            if (LevelIndex < guidedLevels && _game.Status == GameStatus.Playing && _solver.Solve(_game) == SolveOutcome.Solved && _solver.SolutionLength > 0)
                guide = _solver.Solution[0];

            GuideCar = guide;
            if (guide != ParkingGame.NoCar) board.ShowHint(guide);
        }

        public bool UseHint()
        {
            if (Screen != GameScreen.Playing || _game.Status != GameStatus.Playing) return false;
            if (_solver.Solve(_game) != SolveOutcome.Solved || _solver.SolutionLength == 0) return false;
            if (!Pay(ref SaveSystem.Data.hints, hintCost)) return false;

            _hintsUsed++;
            board.ShowHint(_solver.Solution[0]);
            AudioManager.Play(Sfx.Hint);
            Analytics.Current.BoosterUsed(LevelNumber, "hint");
            InventoryChanged?.Invoke();
            return true;
        }

        public bool UseExtraSlot()
        {
            if (Screen != GameScreen.Playing && Screen != GameScreen.Failed) return false;
            if (!CanAddSlot) return false;
            if (!Pay(ref SaveSystem.Data.extraSlots, ExtraSlotCost)) return false;

            _game.AddWaitingSlot(_result);
            _slotsUsed++;
            _failPending = false;
            board.ShowExtraWaitingSlot();
            board.Play(_result);
            AudioManager.Play(Sfx.Hint);
            Analytics.Current.BoosterUsed(LevelNumber, "extra_slot");
            InventoryChanged?.Invoke();
            BoardChanged?.Invoke();
            if (Screen == GameScreen.Failed) SetScreen(GameScreen.Playing);
            return true;
        }

        public bool UseUndo()
        {
            if (Screen != GameScreen.Playing && Screen != GameScreen.Failed) return false;
            if (!_game.CanUndo) return false;
            if (!Pay(ref SaveSystem.Data.undos, undoCost)) return false;

            _game.Undo();
            _failPending = false;
            board.Build(_game);
            AudioManager.Play(Sfx.Hint);
            Analytics.Current.BoosterUsed(LevelNumber, "undo");
            InventoryChanged?.Invoke();
            BoardChanged?.Invoke();
            if (Screen == GameScreen.Failed) SetScreen(GameScreen.Playing);
            UpdateGuide();
            return true;
        }

        private static bool Pay(ref int stock, int price)
        {
            SaveData data = SaveSystem.Data;
            if (stock > 0) stock--;
            else if (data.coins >= price) data.coins -= price;
            else return false;

            SaveSystem.Save();
            return true;
        }

        private void HandleCompleted()
        {
            if (Screen != GameScreen.Playing) return;

            Analytics.Current.LevelCompleted(LevelNumber, _game.Moves, Time.time - _startTime, _hintsUsed, _slotsUsed);
            SaveData data = SaveSystem.Data;
            data.level++;
            data.coins += winReward;
            SaveSystem.Save();
            _rewardDoubled = false;

            board.Celebrate();
            AudioManager.Play(Sfx.Win);
            SetScreen(GameScreen.Won);
            InventoryChanged?.Invoke();
        }

        public bool DoubleReward(Action finished)
        {
            if (!CanDoubleReward) return false;

            _adRunning = true;
            int level = LevelNumber - 1;
            Ads.Current.ShowRewarded("double_reward", rewarded =>
            {
                _adRunning = false;
                if (rewarded && !_rewardDoubled)
                {
                    _rewardDoubled = true;
                    SaveSystem.Data.coins += winReward;
                    SaveSystem.Save();
                    AudioManager.Play(Sfx.Coin);
                    Analytics.Current.BoosterUsed(level, "double_reward");
                    InventoryChanged?.Invoke();
                }

                finished?.Invoke();
            });
            return true;
        }

        public bool OwnsSkin(int index) => (SaveSystem.Data.ownedSkins & (1 << index)) != 0;

        public bool BuyOrSelectSkin(int index)
        {
            if (index < 0 || index >= shop.skins.Length) return false;

            SaveData data = SaveSystem.Data;
            if (!OwnsSkin(index))
            {
                int price = shop.skins[index].price;
                if (data.coins < price) return false;
                data.coins -= price;
                data.ownedSkins |= 1 << index;
                AudioManager.Play(Sfx.Coin);
                Analytics.Current.BoosterUsed(LevelNumber, "skin_" + shop.skins[index].title);
            }

            data.skin = index;
            SaveSystem.Save();
            board.ApplySkin(shop.skins[index]);
            InventoryChanged?.Invoke();
            return true;
        }

        public bool BuyPack(int index)
        {
            if (index < 0 || index >= shop.packs.Length) return false;

            BoosterPack pack = shop.packs[index];
            SaveData data = SaveSystem.Data;
            if (data.coins < pack.price) return false;

            data.coins -= pack.price;
            switch (pack.kind)
            {
                case BoosterKind.Undo: data.undos += pack.amount; break;
                case BoosterKind.Hint: data.hints += pack.amount; break;
                default: data.extraSlots += pack.amount; break;
            }

            SaveSystem.Save();
            AudioManager.Play(Sfx.Coin);
            Analytics.Current.BoosterUsed(LevelNumber, "pack_" + pack.kind);
            InventoryChanged?.Invoke();
            return true;
        }

        private void HandleFailed()
        {
            if (Screen != GameScreen.Playing || !_failPending) return;

            Analytics.Current.LevelFailed(LevelNumber, _game.Moves, Time.time - _startTime, _game.Remaining);
            AudioManager.Play(Sfx.Lose);
            SetScreen(GameScreen.Failed);
        }

        private void SetScreen(GameScreen screen)
        {
            Screen = screen;
            ScreenChanged?.Invoke(screen);
        }
    }
}
