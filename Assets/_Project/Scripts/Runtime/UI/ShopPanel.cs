using DG.Tweening;
using ParkSort.Audio;
using ParkSort.Data;
using ParkSort.Game;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace ParkSort.UI
{
    public sealed class ShopPanel : MonoBehaviour
    {
        [SerializeField] private GameController controller;
        [SerializeField] private GameObject root;
        [SerializeField] private RectTransform window;
        [SerializeField] private TMP_Text coinLabel;
        [SerializeField] private Button closeButton;

        [Header("Booster packs")]
        [SerializeField] private Button[] packButtons = new Button[0];
        [SerializeField] private TMP_Text[] packAmounts = new TMP_Text[0];
        [SerializeField] private TMP_Text[] packPrices = new TMP_Text[0];

        [Header("Themes")]
        [SerializeField] private Button[] skinButtons = new Button[0];
        [SerializeField] private Image[] skinButtonImages = new Image[0];
        [SerializeField] private TMP_Text[] skinButtonLabels = new TMP_Text[0];
        [SerializeField] private TMP_Text[] skinTitles = new TMP_Text[0];
        [SerializeField] private Image[] skinGrass = new Image[0];
        [SerializeField] private Image[] skinLot = new Image[0];
        [SerializeField] private Color buyColor = new Color(1f, 0.62f, 0.17f, 1f);
        [SerializeField] private Color useColor = new Color(0.24f, 0.53f, 0.96f, 1f);
        [SerializeField] private Color activeColor = new Color(0.26f, 0.77f, 0.43f, 1f);

        private void Awake()
        {
            root.SetActive(false);
            closeButton.onClick.AddListener(() =>
            {
                AudioManager.Play(Sfx.Click);
                root.SetActive(false);
            });

            for (int i = 0; i < packButtons.Length; i++)
            {
                int index = i;
                packButtons[i].onClick.AddListener(() => Buy(packButtons[index].transform, controller.BuyPack(index)));
            }

            for (int i = 0; i < skinButtons.Length; i++)
            {
                int index = i;
                skinButtons[i].onClick.AddListener(() => Buy(skinButtons[index].transform, controller.BuyOrSelectSkin(index)));
            }

            controller.InventoryChanged += Refresh;
        }

        private void OnDestroy()
        {
            if (controller != null) controller.InventoryChanged -= Refresh;
        }

        public void Show()
        {
            Refresh();
            root.SetActive(true);
            window.DOKill();
            window.localScale = Vector3.one * 0.85f;
            window.DOScale(1f, 0.28f).SetEase(Ease.OutBack);
        }

        private static void Buy(Transform button, bool success)
        {
            button.DOKill(true);
            if (success)
            {
                button.DOPunchScale(Vector3.one * 0.12f, 0.22f, 5, 0.6f);
                return;
            }

            AudioManager.Play(Sfx.Bump);
            button.DOShakePosition(0.25f, new Vector3(10f, 0f, 0f), 18, 0f);
        }

        private void Refresh()
        {
            SkinCatalog shop = controller.Shop;
            coinLabel.SetText("{0}", (float)controller.Coins);

            for (int i = 0; i < packButtons.Length; i++)
            {
                bool used = i < shop.packs.Length;
                packButtons[i].gameObject.SetActive(used);
                if (!used) continue;
                packAmounts[i].SetText("x{0}", (float)shop.packs[i].amount);
                packPrices[i].SetText("{0}", (float)shop.packs[i].price);
            }

            for (int i = 0; i < skinButtons.Length; i++)
            {
                bool used = i < shop.skins.Length;
                skinButtons[i].transform.parent.gameObject.SetActive(used);
                if (!used) continue;

                Skin skin = shop.skins[i];
                skinTitles[i].text = skin.title;
                skinGrass[i].color = skin.grass;
                skinLot[i].color = skin.lot;

                if (controller.SelectedSkin == i)
                {
                    skinButtonLabels[i].text = "IN USE";
                    skinButtonImages[i].color = activeColor;
                }
                else if (controller.OwnsSkin(i))
                {
                    skinButtonLabels[i].text = "USE";
                    skinButtonImages[i].color = useColor;
                }
                else
                {
                    skinButtonLabels[i].SetText("{0}", (float)skin.price);
                    skinButtonImages[i].color = buyColor;
                }
            }
        }
    }
}
