using DG.Tweening;
using ParkSort.Audio;
using ParkSort.Core;
using ParkSort.Data;
using ParkSort.Game;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace ParkSort.UI
{
    public sealed class Hud : MonoBehaviour
    {
        [SerializeField] private GameController controller;
        [SerializeField] private GameTheme theme;
        [SerializeField] private TapArea tapArea;
        [SerializeField] private GameObject root;

        [Header("Top bar")]
        [SerializeField] private TMP_Text levelLabel;
        [SerializeField] private TMP_Text coinLabel;
        [SerializeField] private RectTransform coinIcon;
        [SerializeField] private Button settingsButton;
        [SerializeField] private Button restartButton;
        [SerializeField] private Image[] queueChips = new Image[0];
        [SerializeField] private GameObject queueRoot;

        [Header("Boosters")]
        [SerializeField] private Button hintButton;
        [SerializeField] private TMP_Text hintCount;
        [SerializeField] private Button slotButton;
        [SerializeField] private TMP_Text slotCount;
        [SerializeField] private Button undoButton;
        [SerializeField] private TMP_Text undoCount;

        [Header("Status")]
        [SerializeField] private TMP_Text waitingLabel;
        [SerializeField] private Image waitingPill;
        [SerializeField] private GameObject difficultyBadge;
        [SerializeField] private Image difficultyImage;
        [SerializeField] private TMP_Text difficultyLabel;
        [SerializeField] private Color waitingNormal = new Color(0.12f, 0.16f, 0.34f, 0.7f);
        [SerializeField] private Color waitingDanger = new Color(0.94f, 0.3f, 0.32f, 0.95f);
        [SerializeField] private Color hardColor = new Color(0.94f, 0.35f, 0.36f, 1f);
        [SerializeField] private Color veryHardColor = new Color(0.62f, 0.41f, 0.93f, 1f);

        [Header("Guidance")]
        [SerializeField] private RectTransform hand;
        [SerializeField] private RectTransform canvasRect;
        [SerializeField] private Camera worldCamera;
        [SerializeField] private TMP_Text caption;
        [SerializeField] private CanvasGroup captionGroup;

        private Screens _screens;
        private int _shownCoins = -1;
        private int _shownWaiting = -1;
        private int _shownCapacity = -1;

        public void Bind(Screens screens) => _screens = screens;

        private void Awake()
        {
            tapArea.Tapped += controller.Tap;
            controller.ScreenChanged += HandleScreen;
            controller.LevelStarted += HandleLevelStarted;
            controller.BoardChanged += Refresh;
            controller.InventoryChanged += RefreshInventory;

            hintButton.onClick.AddListener(() => { if (!controller.UseHint()) Deny(hintButton.transform); });
            slotButton.onClick.AddListener(() => { if (!controller.UseExtraSlot()) Deny(slotButton.transform); });
            undoButton.onClick.AddListener(() => { if (!controller.UseUndo()) Deny(undoButton.transform); });
            restartButton.onClick.AddListener(() => { AudioManager.Play(Sfx.Click); controller.Restart(); });
            settingsButton.onClick.AddListener(() => { AudioManager.Play(Sfx.Click); if (_screens != null) _screens.ShowSettings(); });
        }

        private void OnDestroy()
        {
            if (controller == null) return;
            tapArea.Tapped -= controller.Tap;
            controller.ScreenChanged -= HandleScreen;
            controller.LevelStarted -= HandleLevelStarted;
            controller.BoardChanged -= Refresh;
            controller.InventoryChanged -= RefreshInventory;
        }

        private static void Deny(Transform target)
        {
            AudioManager.Play(Sfx.Bump);
            target.DOKill(true);
            target.DOShakePosition(0.25f, new Vector3(10f, 0f, 0f), 18, 0f);
        }

        private void HandleScreen(GameScreen screen)
        {
            bool playing = screen == GameScreen.Playing || screen == GameScreen.Failed || screen == GameScreen.Won;
            root.SetActive(playing);
            if (screen != GameScreen.Playing) hand.gameObject.SetActive(false);
            RefreshInventory();
        }

        private void HandleLevelStarted()
        {
            levelLabel.SetText("LEVEL {0}", (float)(controller.LevelNumber));
            Refresh();

            LevelDifficulty difficulty = controller.Difficulty;
            difficultyBadge.SetActive(difficulty != LevelDifficulty.Normal);
            if (difficulty != LevelDifficulty.Normal)
            {
                difficultyImage.color = difficulty == LevelDifficulty.Hard ? hardColor : veryHardColor;
                difficultyLabel.text = difficulty == LevelDifficulty.Hard ? "HARD" : "VERY HARD";
                Transform badge = difficultyBadge.transform;
                badge.DOKill();
                badge.localScale = Vector3.one * 2.2f;
                badge.DOScale(1f, 0.45f).SetEase(Ease.OutBack).SetDelay(0.15f);
            }

            captionGroup.DOKill();
            bool hasFeature = controller.Entry != null && !string.IsNullOrEmpty(controller.Entry.featureTitle);
            bool hasCaption = !hasFeature && !string.IsNullOrEmpty(controller.Caption);
            captionGroup.gameObject.SetActive(hasCaption);
            if (!hasCaption) return;

            caption.text = controller.Caption;
            captionGroup.alpha = 0f;
            DOTween.Sequence().SetTarget(captionGroup)
                .Append(captionGroup.DOFade(1f, 0.3f))
                .AppendInterval(4.5f)
                .Append(captionGroup.DOFade(0f, 0.5f))
                .OnComplete(() => captionGroup.gameObject.SetActive(false));
        }

        private void Refresh()
        {
            ParkingGame game = controller.Game;
            if (game == null) return;

            LevelDef level = game.Level;
            int next = game.NextQueueIndex;
            bool any = false;
            for (int i = 0; i < queueChips.Length; i++)
            {
                bool used = next + i < level.garageQueue.Length;
                queueChips[i].gameObject.SetActive(used);
                if (!used) continue;
                any = true;
                queueChips[i].color = theme.colors[level.garageQueue[next + i]];
            }

            queueRoot.SetActive(any);

            if (game.WaitingCount != _shownWaiting || game.WaitingCapacity != _shownCapacity)
            {
                bool grew = game.WaitingCount > _shownWaiting && _shownWaiting >= 0;
                _shownWaiting = game.WaitingCount;
                _shownCapacity = game.WaitingCapacity;
                waitingLabel.SetText("{0}/{1}", (float)_shownWaiting, (float)_shownCapacity);
                waitingPill.color = _shownWaiting >= _shownCapacity - 1 && _shownWaiting > 0 ? waitingDanger : waitingNormal;
                if (grew)
                {
                    waitingPill.transform.DOKill(true);
                    waitingPill.transform.DOPunchScale(Vector3.one * 0.18f, 0.25f, 5, 0.6f);
                }
            }

            RefreshInventory();
        }

        private void RefreshInventory()
        {
            if (controller.Coins != _shownCoins)
            {
                bool gained = _shownCoins >= 0 && controller.Coins > _shownCoins;
                _shownCoins = controller.Coins;
                coinLabel.SetText("{0}", (float)(_shownCoins));
                if (gained)
                {
                    coinIcon.DOKill(true);
                    coinIcon.DOPunchScale(Vector3.one * 0.3f, 0.3f, 5, 0.6f);
                }
            }

            SetCount(hintCount, controller.Hints, controller.HintCost);
            SetCount(slotCount, controller.ExtraSlots, controller.ExtraSlotCost);
            SetCount(undoCount, controller.Undos, controller.UndoCost);
            slotButton.interactable = controller.CanAddSlot;
        }

        private static void SetCount(TMP_Text label, int stock, int price)
        {
            if (stock > 0) label.SetText("x{0}", (float)(stock));
            else label.SetText("{0}", (float)(price));
        }

        private void LateUpdate()
        {
            int car = controller.Screen == GameScreen.Playing ? controller.GuideCar : ParkingGame.NoCar;
            bool show = car != ParkingGame.NoCar && controller.LevelIndex == 0;
            if (hand.gameObject.activeSelf != show) hand.gameObject.SetActive(show);
            if (!show) return;

            Vector3 world = controller.Board.CarPosition(car) + Vector3.up * 0.6f;
            Vector2 screen = worldCamera.WorldToScreenPoint(world);
            RectTransformUtility.ScreenPointToLocalPointInRectangle(canvasRect, screen, null, out Vector2 local);
            float bob = Mathf.Sin(Time.unscaledTime * 6f) * 14f;
            hand.anchoredPosition = local + new Vector2(46f, -70f + bob);
        }
    }
}
