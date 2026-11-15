using UnityEngine;
using UnityEngine.UI;
using EndlessCarChase.Types;

namespace EndlessCarChase
{
    /// <summary>
    /// «امتحان ماشین قفل»: in the garage, a car that isn't bought yet can be driven for one chase after a rewarded video.
    /// The chase is played with the locked car ( with its own look and no upgrades ), then the game goes back to the
    /// player's own car. Back on the main menu, a small offer shows the car's price with a buy button.
    /// The video goes through the shared ad service ( ECCAds ): the trial only starts after a verified rewarded video.
    /// </summary>
    public static class ECCCarTrial
    {
        // The car of the trial that is starting ( set before the chase starts )
        static string pendingCar;

        /// <summary>
        /// The car driven in the current chase as a trial, or null
        /// </summary>
        public static string TrialCar { get; private set; }

        // The car to offer on the next main menu visit
        static string offerCar;

        public static bool IsTrialRun
        {
            get { return string.IsNullOrEmpty(TrialCar) == false; }
        }

        public static bool HasPendingOffer
        {
            get { return string.IsNullOrEmpty(offerCar) == false; }
        }

        /// <summary>
        /// Shows a rewarded video, and after it was watched starts a chase with the shop's current ( locked ) car
        /// </summary>
        public static void Request(ECCShop shop, System.Action onStarted)
        {
            if (shop == null || shop.items == null || shop.currentItem < 0 || shop.currentItem >= shop.items.Length) return;

            ShopItem item = shop.items[shop.currentItem];

            if (item == null || item.itemIcon == null || item.lockState == 1) return;

            ECCCar car = item.itemIcon.GetComponent<ECCCar>();

            if (car == null) return;

            string carName = item.itemIcon.name;

            bool started = ECCAds.ShowRewarded("car_trial", delegate (bool rewarded)
            {
                if (rewarded == false) return;

                // The shop may have moved on while the video was playing
                if (shop == null || shop.gameController == null || shop.gameController.gameStarted) return;

                pendingCar = carName;

                shop.gameController.playerObject = car;
                shop.gameController.StartGame();

                if (shop.currentIcon) Object.Destroy(shop.currentIcon.gameObject);
                if (shop.shopCamera) shop.shopCamera.SetActive(false);

                shop.gameObject.SetActive(false);

                if (onStarted != null) onStarted();
            });

            if (started == false) ECCToast.Show("یک لحظه صبر کن...", ECCRuntimeUI.AccentColor);
        }

        /// <summary>
        /// Called when any chase starts: a chase that isn't the trial clears the trial state
        /// </summary>
        public static void OnRunStarted(ECCGameController gameController)
        {
            TrialCar = pendingCar;
            pendingCar = null;

            if (IsTrialRun == false) return;

            offerCar = TrialCar;

            ECCHintBanner.Show("Icons/play", ECCCarTheme.CandyOrange, "حالت امتحانی", "این دور رو با " + ECCCarNames.GetByName(TrialCar) + " رانندگی کن؛ اگه خوشت اومد بخرش");
        }

        /// <summary>
        /// The buy offer after a trial, once, on the main menu
        /// </summary>
        public static void ShowOffer(ECCGameController gameController)
        {
            string car = offerCar;
            offerCar = null;

            ECCShop shop = gameController ? gameController.shopMenu : null;

            if (string.IsNullOrEmpty(car) || shop == null || shop.items == null) return;

            int index = -1;

            for (int item = 0; item < shop.items.Length; item++)
            {
                if (shop.items[item] != null && shop.items[item].itemIcon && shop.items[item].itemIcon.name == car) index = item;
            }

            // Bought in the meantime, or not in the garage
            if (index < 0 || PlayerPrefs.GetInt(car + "(Clone)", shop.items[index].lockState) == 1) return;

            ECCCarTrialOffer.Open(shop, index);
        }

        /// <summary>
        /// Buys a garage car with coins ( the shop's own lock key ) and selects it. False without enough coins
        /// </summary>
        public static bool Buy(ECCShop shop, int index)
        {
            ShopItem item = shop.items[index];

            if (ECCWallet.TrySpend(item.price) == false) return false;

            item.lockState = 1;

            PlayerPrefs.SetInt(item.itemIcon.name + "(Clone)", 1);
            PlayerPrefs.SetInt(shop.currentPlayerprefs, index);
            PlayerPrefs.Save();

            shop.currentItem = index;

            return true;
        }
    }

    /// <summary>
    /// The «از این ماشین خوشت اومد؟» popup after a trial
    /// </summary>
    public class ECCCarTrialOffer : MonoBehaviour
    {
        ECCShop shop;
        int index;

        public static void Open(ECCShop shop, int index)
        {
            RectTransform panel = ECCUIKit.CreatePopup("CanvasCarTrialOffer", 72, new Vector2(600, 470));

            ECCCarTrialOffer offer = panel.gameObject.AddComponent<ECCCarTrialOffer>();
            offer.shop = shop;
            offer.index = index;
            offer.Build(panel);
        }

        void Build(RectTransform panel)
        {
            Vector2 topCenter = new Vector2(0.5f, 1);
            Vector2 bottomCenter = new Vector2(0.5f, 0);

            ShopItem item = shop.items[index];
            string name = ECCCarNames.Get(item.itemIcon);

            ECCRuntimeUI.CreateText("Title", panel, "از این ماشین خوشت اومد؟", 34, topCenter, new Vector2(0, -56), new Vector2(540, 50), ECCRuntimeUI.AccentColor);

            ECCRuntimeUI.CreateText("Car", panel, name, 40, topCenter, new Vector2(0, -130), new Vector2(540, 56), ECCCarTheme.CardInk);

            ECCRuntimeUI.CreateText("Info", panel, "همین حالا بخرش و همیشه باهاش رانندگی کن", 22, topCenter, new Vector2(0, -190), new Vector2(540, 34), ECCCarTheme.CardSubInk);

            // The price: [coin] amount
            RectTransform price = ECCRuntimeUI.CreateRect("Price", panel, topCenter, new Vector2(0, -250), new Vector2(260, 56));
            ECCUIKit.CreateCoinIcon("Coin", price, new Vector2(1, 0.5f), new Vector2(-28, 0), 46);
            Text amount = ECCUIKit.CreateNumberText("Amount", price, ECCWallet.Format(item.price), 34, new Vector2(1, 0.5f), new Vector2(-60, 0), new Vector2(190, 50), ECCCarTheme.InkAmber);
            amount.alignment = TextAnchor.MiddleRight;
            amount.rectTransform.pivot = new Vector2(1, 0.5f);

            bool affordable = ECCWallet.CanAfford(item.price);

            Button buy = ECCRuntimeUI.CreateButton("ButtonBuy", panel, affordable ? "خرید" : "پول کافی نداری", 30, bottomCenter, new Vector2(120, 40), new Vector2(240, 88), affordable ? ECCCarTheme.CandyGreen : ECCCarTheme.CandyGray);
            ECCCarTheme.Chunky(buy);
            buy.onClick.AddListener(Buy);

            if (affordable) ECCPulse.Set(buy, true, 0.04f);

            Button later = ECCRuntimeUI.CreateButton("ButtonLater", panel, "بعداً", 28, bottomCenter, new Vector2(-140, 40), new Vector2(200, 88), ECCCarTheme.CandyGray);
            ECCCarTheme.Chunky(later);
            later.onClick.AddListener(Close);

            Button close = ECCUIKit.CreateCloseButton(panel);
            close.onClick.AddListener(Close);
        }

        void Buy()
        {
            if (shop == null) return;

            if (ECCCarTrial.Buy(shop, index) == false)
            {
                ECCToast.Show("پول کافی نداری", ECCRuntimeUI.AccentColor);
                ECCCoinGiftPopup.Open();
                return;
            }

            ECCToast.Show(ECCCarNames.Get(shop.items[index].itemIcon) + " مال تو شد!", ECCRuntimeUI.GoodColor);
            ECCRewardFx.Confetti();
            ECCHaptics.Vibrate(ECCHaptics.Kind.Reward);

            Close();
        }

        void Close()
        {
            ECCUIKit.ClosePopup(this);
        }
    }
}
