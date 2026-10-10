using DG.Tweening;
using ParkSort.Audio;
using ParkSort.Game;
using ParkSort.Save;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace ParkSort.UI
{
    public sealed class Screens : MonoBehaviour
    {
        [SerializeField] private GameController controller;
        [SerializeField] private Hud hud;

        [Header("Home")]
        [SerializeField] private GameObject home;
        [SerializeField] private TMP_Text homeLevel;
        [SerializeField] private TMP_Text homeCoins;
        [SerializeField] private Button playButton;
        [SerializeField] private RectTransform playRect;
        [SerializeField] private Button homeSettingsButton;
        [SerializeField] private Button homeShopButton;
        [SerializeField] private ShopPanel shopPanel;

        [Header("Win")]
        [SerializeField] private GameObject win;
        [SerializeField] private RectTransform winWindow;
        [SerializeField] private TMP_Text winTitle;
        [SerializeField] private TMP_Text winReward;
        [SerializeField] private Button nextButton;
        [SerializeField] private RectTransform[] stars = new RectTransform[0];
        [SerializeField] private Button doubleButton;
        [SerializeField] private TMP_Text doubleLabel;
        [SerializeField] private RectTransform rewardPill;

        [Header("Fail")]
        [SerializeField] private GameObject fail;
        [SerializeField] private RectTransform failWindow;
        [SerializeField] private Button reviveButton;
        [SerializeField] private TMP_Text reviveLabel;
        [SerializeField] private Button retryButton;

        [Header("New feature")]
        [SerializeField] private GameObject feature;
        [SerializeField] private RectTransform featureWindow;
        [SerializeField] private Image featureIcon;
        [SerializeField] private TMP_Text featureTitle;
        [SerializeField] private TMP_Text featureText;
        [SerializeField] private Button featureButton;

        [Header("Settings")]
        [SerializeField] private GameObject settings;
        [SerializeField] private RectTransform settingsWindow;
        [SerializeField] private Button closeSettingsButton;
        [SerializeField] private Button soundButton;
        [SerializeField] private TMP_Text soundLabel;
        [SerializeField] private Button musicButton;
        [SerializeField] private TMP_Text musicLabel;
        [SerializeField] private Button homeButton;
        [SerializeField] private Button settingsShopButton;

        private int _featureShownFor;

        private void Awake()
        {
            hud.Bind(this);
            controller.ScreenChanged += HandleScreen;
            controller.LevelStarted += HandleLevelStarted;
            featureButton.onClick.AddListener(() => Click(() => feature.SetActive(false)));
            feature.SetActive(false);

            playButton.onClick.AddListener(() => Click(controller.Play));
            nextButton.onClick.AddListener(() => Click(controller.Play));
            retryButton.onClick.AddListener(() => Click(controller.Restart));
            reviveButton.onClick.AddListener(() => { if (!controller.UseExtraSlot()) AudioManager.Play(Sfx.Bump); });
            homeSettingsButton.onClick.AddListener(() => Click(ShowSettings));
            homeShopButton.onClick.AddListener(() => Click(shopPanel.Show));
            settingsShopButton.onClick.AddListener(() => Click(() => { settings.SetActive(false); shopPanel.Show(); }));
            doubleButton.onClick.AddListener(WatchAdForDouble);
            closeSettingsButton.onClick.AddListener(() => Click(() => settings.SetActive(false)));
            soundButton.onClick.AddListener(() => Click(() => { SaveSystem.SoundOn = !SaveSystem.SoundOn; RefreshSettings(); }));
            musicButton.onClick.AddListener(() => Click(() => { SaveSystem.MusicOn = !SaveSystem.MusicOn; RefreshSettings(); }));
            homeButton.onClick.AddListener(() => Click(() => { settings.SetActive(false); controller.GoHome(); }));

            win.SetActive(false);
            fail.SetActive(false);
            settings.SetActive(false);
        }

        private void OnDestroy()
        {
            if (controller == null) return;
            controller.ScreenChanged -= HandleScreen;
            controller.LevelStarted -= HandleLevelStarted;
        }

        private void HandleLevelStarted()
        {
            Data.LevelEntry entry = controller.Entry;
            bool show = entry != null && !string.IsNullOrEmpty(entry.featureTitle) && _featureShownFor != controller.LevelNumber;
            if (show) _featureShownFor = controller.LevelNumber;
            feature.SetActive(show);
            if (!show) return;

            featureIcon.sprite = entry.featureIcon;
            featureIcon.enabled = entry.featureIcon != null;
            featureTitle.text = entry.featureTitle;
            featureText.text = entry.caption;
            Pop(featureWindow);
            featureIcon.rectTransform.DOKill();
            featureIcon.rectTransform.localScale = Vector3.zero;
            featureIcon.rectTransform.DOScale(1f, 0.45f).SetEase(Ease.OutBack).SetDelay(0.15f);
            AudioManager.Play(Sfx.Reveal);
        }

        private static void Click(System.Action action)
        {
            AudioManager.Play(Sfx.Click);
            action();
        }

        private void HandleScreen(GameScreen screen)
        {
            home.SetActive(screen == GameScreen.Home);
            win.SetActive(screen == GameScreen.Won);
            fail.SetActive(screen == GameScreen.Failed);
            if (screen != GameScreen.Playing) feature.SetActive(false);

            switch (screen)
            {
                case GameScreen.Home:
                    homeLevel.SetText("LEVEL {0}", (float)(controller.LevelNumber));
                    homeCoins.SetText("{0}", (float)(controller.Coins));
                    playRect.DOKill();
                    playRect.localScale = Vector3.one;
                    playRect.DOScale(1.06f, 0.7f).SetEase(Ease.InOutSine).SetLoops(-1, LoopType.Yoyo);
                    break;

                case GameScreen.Won:
                    playRect.DOKill();
                    winTitle.SetText("LEVEL {0}\nCOMPLETE!", (float)(controller.LevelNumber - 1));
                    winReward.SetText("+{0}", (float)(controller.WinReward));
                    doubleButton.gameObject.SetActive(controller.CanDoubleReward);
                    doubleButton.interactable = true;
                    doubleLabel.SetText("x2   +{0}", (float)(controller.WinReward));
                    Pop(winWindow);
                    for (int i = 0; i < stars.Length; i++)
                    {
                        stars[i].DOKill();
                        stars[i].localScale = Vector3.zero;
                        stars[i].DOScale(1f, 0.35f).SetEase(Ease.OutBack).SetDelay(0.25f + i * 0.14f);
                    }

                    break;

                case GameScreen.Failed:
                    playRect.DOKill();
                    bool canRevive = controller.CanAddSlot;
                    reviveButton.gameObject.SetActive(canRevive);
                    if (controller.ExtraSlots > 0) reviveLabel.SetText("+1 SPACE  (x{0})", (float)(controller.ExtraSlots));
                    else reviveLabel.SetText("+1 SPACE  ({0})", (float)(controller.ExtraSlotCost));
                    Pop(failWindow);
                    break;

                default:
                    playRect.DOKill();
                    break;
            }
        }

        private void WatchAdForDouble()
        {
            AudioManager.Play(Sfx.Click);
            if (!controller.DoubleReward(FinishDouble)) return;
            doubleButton.interactable = false;
            doubleLabel.text = "WATCHING AD...";
        }

        private void FinishDouble()
        {
            doubleButton.gameObject.SetActive(false);
            if (!controller.RewardDoubled) return;
            winReward.SetText("+{0}", (float)(controller.WinReward * 2));
            rewardPill.DOKill(true);
            rewardPill.DOPunchScale(Vector3.one * 0.25f, 0.35f, 6, 0.6f);
        }

        public void ShowSettings()
        {
            RefreshSettings();
            settings.SetActive(true);
            homeButton.gameObject.SetActive(controller.Screen != GameScreen.Home);
            Pop(settingsWindow);
        }

        private void RefreshSettings()
        {
            soundLabel.text = SaveSystem.SoundOn ? "SOUND: ON" : "SOUND: OFF";
            musicLabel.text = SaveSystem.MusicOn ? "MUSIC: ON" : "MUSIC: OFF";
        }

        private static void Pop(RectTransform window)
        {
            window.DOKill();
            window.localScale = Vector3.one * 0.8f;
            window.DOScale(1f, 0.3f).SetEase(Ease.OutBack);
        }
    }
}
