using UnityEngine;
using UnityEngine.UI;

namespace EndlessCarChase
{
    /// <summary>
    /// Confirmation for a power-up without free uses ( like CrowdRush's power-up popup ): the power-up's badge, name and
    /// what it does, then «استفاده با X» to pay the coins, or «تماشای ویدئو» when the coins aren't enough.
    /// The chase stays paused while it's open ( ECCPowerUpBar paused it ). The power-up is only given after the coins were
    /// spent on a working effect, or after a verified rewarded video; a failed, skipped or missing video gives nothing.
    /// </summary>
    public class ECCPowerUpPopup : MonoBehaviour
    {
        static readonly Color BuyColor = new Color(0.2f, 0.66f, 0.3f, 1);
        static readonly Color WatchColor = new Color(0.56f, 0.36f, 0.97f, 1);

        static ECCPowerUpPopup instance;

        ECCPowerUpType type;
        RectTransform panel;
        Text note;
        Text status;
        Button buy;
        Button watch;
        Button cancel;
        bool requesting = false;

        public static bool IsOpen
        {
            get { return instance != null; }
        }

        public static void Open(ECCPowerUpType type)
        {
            if (instance) return;

            RectTransform panel = ECCUIKit.CreatePopup("CanvasPowerUpPopup", 84, new Vector2(620, 660));

            instance = panel.gameObject.AddComponent<ECCPowerUpPopup>();
            instance.type = type;
            instance.panel = panel;
            instance.Build();
        }

        void Build()
        {
            Color color = ECCPowerUpManager.GetColor(type);
            Vector2 center = new Vector2(0.5f, 0.5f);
            Vector2 topCenter = new Vector2(0.5f, 1);
            Vector2 bottomCenter = new Vector2(0.5f, 0);

            Outline border = panel.GetComponent<Outline>();
            if (border) border.effectColor = color;

            // The badge sits on the top edge of the panel
            Image badgeShade = ECCUIKit.CreateCircle("BadgeShade", panel, topCenter, new Vector2(0, -8), 176, new Color(color.r * 0.55f, color.g * 0.55f, color.b * 0.55f, 1));
            badgeShade.rectTransform.pivot = center;
            badgeShade.raycastTarget = false;

            Image badge = ECCUIKit.CreateCircle("Badge", panel, topCenter, Vector2.zero, 176, color);
            badge.rectTransform.pivot = center;
            badge.raycastTarget = false;

            ECCCarTheme.CreateIcon("Icon", badge.transform, ECCPowerUpManager.GetIcon(type), center, new Vector2(0, 2), 104, Color.white);

            Image ring = ECCRuntimeUI.CreateImage("Ring", badge.transform, center, Vector2.zero, new Vector2(196, 196), Color.white);
            ring.sprite = ECCCarTheme.Icon("Icons/ring");
            ring.raycastTarget = false;

            Text title = ECCRuntimeUI.CreateText("Title", panel, ECCPowerUpManager.GetName(type), 46, topCenter, new Vector2(0, -130), new Vector2(560, 64), color);
            title.fontStyle = FontStyle.Bold;

            ECCRuntimeUI.CreateText("Description", panel, ECCPersianText.Wrap(ECCPowerUpManager.GetDescription(type), 34), 28, topCenter, new Vector2(0, -220), new Vector2(560, 100), Color.white);

            note = ECCRuntimeUI.CreateText("Note", panel, string.Empty, 24, topCenter, new Vector2(0, -318), new Vector2(560, 70), ECCCarTheme.NightText);

            status = ECCRuntimeUI.CreateText("Status", panel, string.Empty, 22, bottomCenter, new Vector2(0, 236), new Vector2(560, 36), ECCRuntimeUI.BadColor);

            // Coin price: «استفاده با ۹۰» and the coin icon
            buy = ECCRuntimeUI.CreateButton("ButtonBuy", panel, string.Empty, 34, bottomCenter, new Vector2(0, 140), new Vector2(440, 100), BuyColor);
            ECCRuntimeUI.GetLabel(buy).rectTransform.anchoredPosition = new Vector2(-30, 0);
            ECCUIKit.CreateCoinIcon("Coin", buy.transform, new Vector2(1, 0.5f), new Vector2(-50, 0), 60);
            buy.onClick.AddListener(OnBuy);

            watch = ECCRuntimeUI.CreateButton("ButtonWatch", panel, "تماشای ویدئو", 34, bottomCenter, new Vector2(0, 140), new Vector2(440, 100), WatchColor);
            ECCRuntimeUI.GetLabel(watch).rectTransform.anchoredPosition = new Vector2(-30, 0);
            ECCCarTheme.CreateIcon("Play", watch.transform, "Icons/play", new Vector2(1, 0.5f), new Vector2(-50, 0), 52, Color.white);
            watch.onClick.AddListener(OnWatch);

            cancel = ECCRuntimeUI.CreateButton("ButtonCancel", panel, "انصراف", 28, bottomCenter, new Vector2(0, 42), new Vector2(260, 64), ECCRuntimeUI.ButtonColor);
            cancel.onClick.AddListener(Close);

            Refresh();

            ECCWallet.BalanceChanged += OnBalanceChanged;
        }

        void OnDestroy()
        {
            ECCWallet.BalanceChanged -= OnBalanceChanged;

            if (instance == this) instance = null;
        }

        void OnBalanceChanged(int balance)
        {
            if (requesting == false) Refresh();
        }

        /// <summary>
        /// The coins can change while it's open ( the coin balance button's gift video )
        /// </summary>
        void Refresh()
        {
            int price = ECCPowerUpInventory.GetPrice(type);
            bool affordable = ECCWallet.CanAfford(price);

            buy.gameObject.SetActive(affordable);
            watch.gameObject.SetActive(affordable == false);

            ECCRuntimeUI.SetText(ECCRuntimeUI.GetLabel(buy), "استفاده با " + ECCUIKit.PersianDigits(price));

            ECCRuntimeUI.SetText(note, affordable
                ? "قیمت: " + ECCUIKit.PersianDigits(price) + " سکه"
                : ECCPersianText.Wrap("سکه‌ی کافی نداری (" + ECCUIKit.PersianDigits(price) + " سکه لازم است). با تماشای ویدئو رایگان فعالش کن!", 40));
        }

        void Close()
        {
            // While a video is loading or playing, its result decides
            if (requesting) return;

            Destroy(panel.root.gameObject);

            ECCPowerUpBar.OnPopupClosed();
        }

        void OnBuy()
        {
            if (requesting) return;

            ECCPowerUpType bought = type;

            Destroy(panel.root.gameObject);

            // The coins are only spent when the effect really starts
            if (ECCPowerUpBar.ApplyFromPopup(bought, true) == false) ECCToast.Show("الان نمی‌شود از این قدرت استفاده کرد؛ سکه‌ای کم نشد", ECCRuntimeUI.AccentColor);
        }

        void OnWatch()
        {
            if (requesting || ECCAds.IsBusy) return;

            requesting = true;
            SetButtons(false);
            ECCRuntimeUI.SetText(status, string.Empty);

            ECCPowerUpType requested = type;

            bool started = ECCAds.ShowRewarded("powerup_" + requested, delegate (bool rewarded)
            {
                if (this == null) return;

                requesting = false;

                if (rewarded)
                {
                    Destroy(panel.root.gameObject);

                    // Granted only after a verified rewarded video. If the chase changed meanwhile, the use is kept for later
                    if (ECCPowerUpBar.ApplyFromPopup(requested, false) == false)
                    {
                        ECCPowerUpInventory.AddFreeUses(requested, 1);
                        ECCToast.Show("یک " + ECCPowerUpManager.GetName(requested) + " رایگان برایت ذخیره شد", ECCRuntimeUI.GoodColor);
                    }

                    return;
                }

                // Skipped, failed or no video ( the NoAds panel is shown by ECCAds ): nothing is given
                SetButtons(true);
                ECCRuntimeUI.SetText(status, "ویدئو کامل دیده نشد؛ قدرت فعال نشد.");
            });

            if (started == false)
            {
                requesting = false;
                SetButtons(true);
                ECCRuntimeUI.SetText(status, "ویدئوی دیگری در حال پخش است.");
            }
        }

        void SetButtons(bool interactable)
        {
            watch.interactable = interactable;
            cancel.interactable = interactable;

            ECCRuntimeUI.SetText(ECCRuntimeUI.GetLabel(watch), interactable ? "تماشای ویدئو" : "در حال بارگذاری...");
        }
    }
}
