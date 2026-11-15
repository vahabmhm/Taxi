using UnityEngine;
using UnityEngine.UI;
using EndlessCarChase.Types;
using EndlessCarChase.Levels;
using EndlessCarChase.Modes;

namespace EndlessCarChase
{
    /// <summary>
    /// The garage, as a car dealership «بنگاه ماشین», in the CrowdRush look of the new UI:
    /// - 3D: the spinning car stands on a turntable in a showroom ( ECCShowroom ) instead of the chase ground
    /// - top: a purple home button, the orange «بنگاه ماشین» ribbon and the shared money balance ( placed by ECCMenuHub )
    /// - a white name plate with the car's name and its number in the list, a price tag under it ( or «مال توست» ),
    ///   and the car's class ( C, B, A, S by price ); confetti when a car is bought
    /// - round blue candy arrows on both sides of the car
    /// - a white bottom card with the car's stats ( speed, handling, armor ) as bars, each with its own green upgrade
    ///   button, and one big main button: green «خرید» with the price for a car for sale, or orange «شروع با این ماشین»
    ///   for an owned car ( with the selected game mode )
    /// The old shop canvas ( CanvasShop ) is hidden, but its ECCShop logic is used for changing, buying and starting with
    /// a car, and its buttons' listeners are kept, so the scene wiring keeps working.
    /// In portrait, the shop camera is moved back and lowered so the whole car fits above the card, with the showroom's
    /// back wall behind it.
    /// It's built from code by the game controller; place it on a GameObject in the scene to change its settings.
    /// </summary>
    public class ECCGarageMenu : MonoBehaviour
    {
        [Tooltip("Height of the bottom card with the stats and the main button")]
        public float cardHeight = 560;

        [Tooltip("Where the car appears on a portrait screen ( 0 bottom, 1 top )")]
        [Range(0.3f, 0.8f)]
        public float portraitCarHeight = 0.64f;

        [Tooltip("Degrees the portrait garage camera looks down at the car. Lower shows more of the showroom's back wall")]
        public float cameraElevation = 20;

        static readonly ECCUpgradeType[] StatTypes = { ECCUpgradeType.Speed, ECCUpgradeType.Handling, ECCUpgradeType.Health };

        static readonly Color HomePurple = ECCCarTheme.Hex(0xA45CFF);
        static readonly Color TrackColor = ECCCarTheme.Hex(0xE6EAF1);

        const float BarWidth = 300;
        const float BarHeight = 20;

        ECCGameController gameController;
        ECCShop shop;
        Canvas canvas;
        RectTransform safeArea;
        ECCShowroom showroom;

        Text carName;
        Text carCount;
        Image priceTag;
        Text priceText;
        Image priceDollar;
        Image classTag;
        Text classText;

        // Right of the price tag: «شخصی‌سازی» for an owned car, «امتحان با ویدئو» for a car for sale
        Button sideButton;
        Text sideLabel;
        Button mainButton;
        Text mainLabel;
        Text mainDetail;
        Image mainDollar;

        class StatRow
        {
            public ECCUpgradeType type;
            public Image fill;
            public Image upgradeFill;
            public Text level;
            public Button upgrade;
            public Text upgradeLabel;
            public Image upgradeDollar;
        }

        readonly StatRow[] rows = new StatRow[StatTypes.Length];

        int shownItem = -1;
        bool framed = false;

        static ECCGarageMenu instance;

        /// <summary>
        /// The garage has no modal screens of its own any more ( kept for the other scripts that check it )
        /// </summary>
        public static bool IsModalOpen
        {
            get { return false; }
        }

        public static ECCGarageMenu Create(ECCGameController gameController)
        {
            ECCGarageMenu menu = FindObjectOfType<ECCGarageMenu>();

            if (menu == null) menu = new GameObject("GarageMenu").AddComponent<ECCGarageMenu>();

            menu.Build(gameController);

            return menu;
        }

        void Build(ECCGameController controller)
        {
            if (canvas) return;

            gameController = controller;
            shop = controller.shopMenu;
            instance = this;

            safeArea = ECCUIKit.CreateSafeCanvas("CanvasGarage", 61, out canvas);
            canvas.transform.SetParent(transform, false);

            BuildTopBar();
            BuildCarArea();
            BuildCard();

            if (shop) showroom = ECCShowroom.Create(shop.itemIconPosition, shop.itemSpinSpeed);

            canvas.enabled = false;
        }

        void OnEnable()
        {
            ECCWallet.BalanceChanged += OnBalanceChanged;
        }

        void OnDisable()
        {
            ECCWallet.BalanceChanged -= OnBalanceChanged;
        }

        void OnDestroy()
        {
            if (instance == this) instance = null;
        }

        void OnBalanceChanged(int balance)
        {
            if (canvas && canvas.enabled) Refresh();
        }

        bool IsVisible
        {
            get { return gameController && gameController.gameStarted == false && shop && shop.gameObject.activeInHierarchy; }
        }

        void Update()
        {
            if (canvas == null) return;

            bool visible = IsVisible;

            if (canvas.enabled != visible)
            {
                canvas.enabled = visible;

                if (showroom) showroom.gameObject.SetActive(visible);

                if (visible)
                {
                    HideOldShop();
                    FrameCar();
                    shownItem = -1;
                }
            }

            // The shop changes cars with the keyboard too, so follow its current car
            if (visible && shop.currentItem != shownItem) Refresh();
        }

        // ----- Layout -----

        void BuildTopBar()
        {
            Vector2 topLeft = new Vector2(0, 1);
            Vector2 topCenter = new Vector2(0.5f, 1);

            // A purple candy home button
            Button home = ECCRuntimeUI.CreateButton("ButtonHome", safeArea, string.Empty, 20, topLeft, new Vector2(24, -24), new Vector2(84, 84), HomePurple);
            ECCCarTheme.CreateIcon("Icon", home.transform, "Icons/home", new Vector2(0.5f, 0.5f), new Vector2(0, 2), 50, Color.white);
            home.onClick.AddListener(BackToMenu);

            // The dealership's orange ribbon
            Image ribbon = ECCRuntimeUI.CreateImage("Ribbon", safeArea, topCenter, new Vector2(0, -126), new Vector2(430, 84), ECCCarTheme.CandyOrange);
            ribbon.sprite = ECCCarTheme.CardSprite("ribbon");
            ribbon.type = Image.Type.Sliced;
            ribbon.pixelsPerUnitMultiplier = 1.4f;
            ribbon.raycastTarget = false;

            Text title = ECCRuntimeUI.CreateText("Title", ribbon.transform, "بنگاه ماشین", 38, new Vector2(0.5f, 0.5f), new Vector2(0, 4), new Vector2(400, 60), Color.white);
            ECCCarTheme.CandyLabel(title);
        }

        void BuildCarArea()
        {
            Vector2 topCenter = new Vector2(0.5f, 1);

            // The white name plate
            Image plate = ECCRuntimeUI.CreateImage("Plate", safeArea, topCenter, new Vector2(0, -222), new Vector2(380, 96), Color.white);
            plate.sprite = ECCCarTheme.CardSprite("panel");
            plate.type = Image.Type.Sliced;
            plate.pixelsPerUnitMultiplier = 2;
            plate.raycastTarget = false;

            Shadow plateShadow = plate.gameObject.AddComponent<Shadow>();
            plateShadow.effectColor = new Color(0, 0, 0, 0.25f);
            plateShadow.effectDistance = new Vector2(0, -5);

            carName = ECCRuntimeUI.CreateText("Name", plate.transform, string.Empty, 34, new Vector2(0.5f, 0.5f), new Vector2(0, 12), new Vector2(360, 50), ECCCarTheme.CardInk);
            ECCCarTheme.InkText(carName, ECCCarTheme.CardInk);
            carName.font = ECCCarTheme.TitleFont;

            carCount = ECCRuntimeUI.CreateText("Count", plate.transform, string.Empty, 20, new Vector2(0.5f, 0.5f), new Vector2(0, -26), new Vector2(360, 28), ECCCarTheme.CardSubInk);
            ECCCarTheme.InkText(carCount, ECCCarTheme.CardSubInk);

            // The price tag ( or «مال توست» ) under the plate
            priceTag = ECCRuntimeUI.CreateImage("PriceTag", safeArea, topCenter, new Vector2(0, -322), new Vector2(240, 58), ECCCarTheme.CandyGreen);
            ECCCarTheme.Round(priceTag);
            priceTag.raycastTarget = false;

            Shadow tagLip = priceTag.gameObject.AddComponent<Shadow>();
            tagLip.effectDistance = new Vector2(0, -5);

            ECCCandyShade tagShade = priceTag.gameObject.AddComponent<ECCCandyShade>();
            tagShade.Init(priceTag, tagLip);

            priceText = ECCRuntimeUI.CreateText("Text", priceTag.transform, string.Empty, 26, new Vector2(0.5f, 0.5f), new Vector2(0, 2), new Vector2(230, 50), Color.white);
            ECCCarTheme.CandyLabel(priceText);

            priceDollar = ECCRuntimeUI.CreateImage("Dollar", priceTag.transform, new Vector2(1, 0.5f), new Vector2(-8, 0), new Vector2(46, 46), Color.white);
            priceDollar.rectTransform.pivot = new Vector2(1, 0.5f);
            priceDollar.sprite = ECCCoinBalanceButton.DollarSprite;
            priceDollar.preserveAspect = true;
            priceDollar.raycastTarget = false;

            // The car's class, beside the price tag: C, B, A or S by its price among the garage's cars
            classTag = ECCRuntimeUI.CreateImage("ClassTag", safeArea, topCenter, new Vector2(-196, -322), new Vector2(118, 58), ECCCarTheme.CandyGray);
            ECCCarTheme.Round(classTag);
            classTag.raycastTarget = false;

            Shadow classLip = classTag.gameObject.AddComponent<Shadow>();
            classLip.effectDistance = new Vector2(0, -5);

            ECCCandyShade classShade = classTag.gameObject.AddComponent<ECCCandyShade>();
            classShade.Init(classTag, classLip);

            classText = ECCRuntimeUI.CreateText("Text", classTag.transform, string.Empty, 24, new Vector2(0.5f, 0.5f), new Vector2(0, 2), new Vector2(110, 50), Color.white);
            ECCCarTheme.CandyLabel(classText);

            // Customize ( owned ) or try with a video ( for sale )
            sideButton = ECCRuntimeUI.CreateButton("ButtonSide", safeArea, string.Empty, 20, topCenter, new Vector2(214, -322), new Vector2(170, 58), ECCCarTheme.Hex(0x8E5CF7));
            ECCCarTheme.Chunky(sideButton);
            sideLabel = ECCRuntimeUI.GetLabel(sideButton);
            sideLabel.resizeTextForBestFit = true;
            sideLabel.resizeTextMinSize = 14;
            sideLabel.resizeTextMaxSize = 20;
            sideButton.onClick.AddListener(OnSideButton);

            // Round arrows on both sides of the car
            CreateArrow("ButtonPrevious", new Vector2(0, portraitCarHeight), new Vector2(20, 0), false, delegate { ChangeCar(-1); });
            CreateArrow("ButtonNext", new Vector2(1, portraitCarHeight), new Vector2(-20, 0), true, delegate { ChangeCar(1); });
        }

        void CreateArrow(string name, Vector2 anchor, Vector2 position, bool pointsRight, UnityEngine.Events.UnityAction onClick)
        {
            const float size = 92;

            RectTransform root = ECCRuntimeUI.CreateRect(name, safeArea, anchor, position, new Vector2(size, size));
            root.pivot = new Vector2(pointsRight ? 1 : 0, 0.5f);

            Image shade = ECCUIKit.CreateCircle("Shade", root, new Vector2(0.5f, 0.5f), new Vector2(0, -6), size, ECCCarTheme.Hex(0x2378B5));
            shade.raycastTarget = false;

            Image face = ECCUIKit.CreateCircle("Face", root, new Vector2(0.5f, 0.5f), Vector2.zero, size, ECCCarTheme.CandyBlue);

            // The chevron icon points left
            Image icon = ECCCarTheme.CreateIcon("Icon", face.transform, "Icons/chevron", new Vector2(0.5f, 0.5f), Vector2.zero, 50, Color.white);
            if (pointsRight) icon.rectTransform.localEulerAngles = new Vector3(0, 0, 180);

            Button button = face.gameObject.AddComponent<Button>();
            button.targetGraphic = face;
            button.transition = Selectable.Transition.None;
            button.onClick.AddListener(onClick);
            root.gameObject.AddComponent<ECCPressScale>();
        }

        void BuildCard()
        {
            Vector2 bottomCenter = new Vector2(0.5f, 0);
            Vector2 topCenter = new Vector2(0.5f, 1);

            // The white card covers the bottom of the screen, also below the safe area
            Image shadow = ECCRuntimeUI.CreateImage("CardShadow", canvas.transform, bottomCenter, new Vector2(0, -40), new Vector2(40, cardHeight + 70), new Color(0, 0, 0, 0.3f));
            shadow.rectTransform.anchorMin = new Vector2(0, 0);
            shadow.rectTransform.anchorMax = new Vector2(1, 0);
            shadow.rectTransform.pivot = bottomCenter;
            shadow.sprite = ECCCarTheme.CardSprite("shadow");
            shadow.type = Image.Type.Sliced;
            shadow.raycastTarget = false;

            Image card = ECCRuntimeUI.CreateImage("Card", canvas.transform, bottomCenter, new Vector2(0, -40), new Vector2(0, cardHeight + 40), Color.white);
            card.rectTransform.anchorMin = new Vector2(0, 0);
            card.rectTransform.anchorMax = new Vector2(1, 0);
            card.rectTransform.pivot = bottomCenter;
            card.sprite = ECCCarTheme.CardSprite("panel");
            card.type = Image.Type.Sliced;
            card.pixelsPerUnitMultiplier = 1.4f;
            card.raycastTarget = true;

            // Behind the safe area's content ( the stats and buttons are drawn on the card )
            card.transform.SetAsFirstSibling();
            shadow.transform.SetAsFirstSibling();

            // The content follows the safe area horizontally
            RectTransform content = ECCRuntimeUI.CreateRect("Content", safeArea, bottomCenter, Vector2.zero, new Vector2(680, cardHeight));
            content.pivot = bottomCenter;

            Text heading = ECCRuntimeUI.CreateText("Heading", content, "مشخصات و ارتقا", 26, topCenter, new Vector2(0, -36), new Vector2(600, 40), ECCCarTheme.CardInk);
            ECCCarTheme.InkText(heading, ECCCarTheme.CardInk);
            heading.font = ECCCarTheme.TitleFont;

            for (int index = 0; index < StatTypes.Length; index++)
            {
                rows[index] = CreateStatRow(content, StatTypes[index], -100 - index * 100);
            }

            // The main button: buy, or start with this car
            mainButton = ECCRuntimeUI.CreateButton("ButtonMain", content, string.Empty, 36, bottomCenter, new Vector2(0, 44), new Vector2(600, 116), ECCCarTheme.CandyGreen);
            mainLabel = ECCRuntimeUI.GetLabel(mainButton);
            mainLabel.rectTransform.anchoredPosition = new Vector2(0, 14);

            mainDetail = ECCRuntimeUI.CreateText("Detail", mainButton.transform, string.Empty, 21, new Vector2(0.5f, 0.5f), new Vector2(0, -28), new Vector2(560, 30), Color.white);
            ECCCarTheme.CandyLabel(mainDetail);
            mainDetail.font = ECCRuntimeUI.DefaultFont;

            mainDollar = ECCRuntimeUI.CreateImage("Dollar", mainButton.transform, new Vector2(0, 0.5f), new Vector2(26, 6), new Vector2(76, 76), Color.white);
            mainDollar.rectTransform.pivot = new Vector2(0, 0.5f);
            mainDollar.sprite = ECCCoinBalanceButton.DollarSprite;
            mainDollar.preserveAspect = true;
            mainDollar.raycastTarget = false;

            mainButton.onClick.AddListener(OnMainButton);
            ECCPulse.Set(mainButton, true, 0.025f);
        }

        /// <summary>
        /// A stat, right to left: icon and name, the bar ( base value, then the bought upgrades lighter ), the level,
        /// and the green upgrade button with its price on the left
        /// </summary>
        StatRow CreateStatRow(RectTransform parent, ECCUpgradeType type, float y)
        {
            StatRow row = new StatRow { type = type };

            Vector2 topRight = new Vector2(1, 1);
            Vector2 topLeft = new Vector2(0, 1);
            Color color = StatColor(type);

            Image discShade = ECCUIKit.CreateCircle("IconShade" + type, parent, topRight, new Vector2(-46, y - 4), 64, Shade(color));
            discShade.rectTransform.pivot = new Vector2(0.5f, 0.5f);
            discShade.raycastTarget = false;

            Image disc = ECCUIKit.CreateCircle("Icon" + type, parent, topRight, new Vector2(-46, y), 64, color);
            disc.rectTransform.pivot = new Vector2(0.5f, 0.5f);
            disc.raycastTarget = false;
            ECCCarTheme.CreateIcon("Icon", disc.transform, StatIcon(type), new Vector2(0.5f, 0.5f), Vector2.zero, 40, Color.white);

            Text name = ECCRuntimeUI.CreateText("Name", parent, ECCUpgrades.GetName(type), 26, topRight, new Vector2(-90, y + 18), new Vector2(200, 36), ECCCarTheme.CardInk);
            name.rectTransform.pivot = new Vector2(1, 0.5f);
            name.alignment = TextAnchor.MiddleRight;
            ECCCarTheme.InkText(name, ECCCarTheme.CardInk);
            name.font = ECCCarTheme.TitleFont;

            row.level = ECCRuntimeUI.CreateText("Level", parent, string.Empty, 20, topRight, new Vector2(-300, y + 18), new Vector2(150, 30), ECCCarTheme.CardSubInk);
            row.level.rectTransform.pivot = new Vector2(1, 0.5f);
            row.level.alignment = TextAnchor.MiddleLeft;
            ECCCarTheme.InkText(row.level, ECCCarTheme.CardSubInk);

            // The bar fills from the right
            Image track = ECCCarTheme.CreatePanel("Bar", parent, topRight, new Vector2(-90, y - 18), new Vector2(BarWidth, BarHeight), TrackColor);
            track.rectTransform.pivot = new Vector2(1, 0.5f);
            track.pixelsPerUnitMultiplier = 48 / BarHeight;

            row.upgradeFill = ECCCarTheme.CreatePanel("Upgrades", track.transform, new Vector2(1, 0.5f), Vector2.zero, new Vector2(0, BarHeight), Color.white);
            row.upgradeFill.rectTransform.pivot = new Vector2(1, 0.5f);
            row.upgradeFill.pixelsPerUnitMultiplier = 48 / BarHeight;

            row.fill = ECCCarTheme.CreatePanel("Base", track.transform, new Vector2(1, 0.5f), Vector2.zero, new Vector2(0, BarHeight), color);
            row.fill.rectTransform.pivot = new Vector2(1, 0.5f);
            row.fill.pixelsPerUnitMultiplier = 48 / BarHeight;

            row.upgrade = ECCRuntimeUI.CreateButton("Upgrade" + type, parent, string.Empty, 24, topLeft, new Vector2(20, y), new Vector2(200, 74), ECCCarTheme.CandyGreen);
            ((RectTransform)row.upgrade.transform).pivot = new Vector2(0, 0.5f);
            ECCUIKit.KeepColorWhenDisabled(row.upgrade);

            row.upgradeLabel = ECCRuntimeUI.GetLabel(row.upgrade);

            row.upgradeDollar = ECCRuntimeUI.CreateImage("Dollar", row.upgrade.transform, new Vector2(1, 0.5f), new Vector2(-8, 2), new Vector2(46, 46), Color.white);
            row.upgradeDollar.rectTransform.pivot = new Vector2(1, 0.5f);
            row.upgradeDollar.sprite = ECCCoinBalanceButton.DollarSprite;
            row.upgradeDollar.preserveAspect = true;
            row.upgradeDollar.raycastTarget = false;

            row.upgrade.onClick.AddListener(delegate { BuyUpgrade(type); });

            return row;
        }

        static Color Shade(Color color)
        {
            return new Color(color.r * 0.68f, color.g * 0.68f, color.b * 0.68f, 1);
        }

        static Color StatColor(ECCUpgradeType type)
        {
            switch (type)
            {
                case ECCUpgradeType.Speed: return ECCCarTheme.Hex(0xFF7A2F);
                case ECCUpgradeType.Handling: return ECCCarTheme.CandyBlue;
                default: return ECCCarTheme.CandyGreen;
            }
        }

        static string StatIcon(ECCUpgradeType type)
        {
            switch (type)
            {
                case ECCUpgradeType.Speed: return "Icons/fire";
                case ECCUpgradeType.Handling: return "SteeringIcon";
                default: return "Icons/shield";
            }
        }

        // ----- The old shop -----

        /// <summary>
        /// The old shop canvas stays active ( its ECCShop script runs the 3D car and the buying ), but isn't drawn
        /// </summary>
        void HideOldShop()
        {
            foreach (Canvas old in shop.GetComponentsInChildren<Canvas>(true)) old.enabled = false;
        }

        /// <summary>
        /// In portrait the shop camera ( made for landscape ) would cut the car off. It's moved back so the spinning car
        /// fits the screen width, a little above the car so the showroom's back wall shows behind it, and turned so the
        /// car stands above the bottom card
        /// </summary>
        void FrameCar()
        {
            if (framed || shop.shopCamera == null || Screen.height <= Screen.width) return;

            Camera camera = shop.shopCamera.GetComponentInChildren<Camera>(true);

            if (camera == null) return;

            framed = true;

            const float carRadius = 2.5f;      // Half the length of the longest car, with a margin
            const float verticalFov = 60;

            camera.fieldOfView = verticalFov;

            float tanHalfHeight = Mathf.Tan(verticalFov * 0.5f * Mathf.Deg2Rad);
            float tanHalfWidth = tanHalfHeight * camera.aspect;
            float distance = carRadius / Mathf.Max(0.05f, tanHalfWidth);

            Vector3 target = shop.itemIconPosition + Vector3.up * 0.6f;

            // Turn down a bit more, so the car is drawn above the screen center
            float offset = Mathf.Atan((portraitCarHeight - 0.5f) * 2 * tanHalfHeight) * Mathf.Rad2Deg;

            camera.transform.position = target + Quaternion.Euler(cameraElevation, 0, 0) * Vector3.back * distance;
            camera.transform.rotation = Quaternion.Euler(cameraElevation + offset, 0, 0);
        }

        void BackToMenu()
        {
            if (ECCModalTracker.AnyOpen) return;

            // The old back button ( ButtonShop ) hides the shop and shows the menu, as wired in the scene
            foreach (Button button in shop.GetComponentsInChildren<Button>(true))
            {
                if (button.name == "ButtonShop")
                {
                    button.onClick.Invoke();
                    return;
                }
            }

            // No back button in this shop: just leave the garage
            shop.gameObject.SetActive(false);
        }

        void ChangeCar(int direction)
        {
            if (ECCModalTracker.AnyOpen) return;

            shop.ChangeItem(direction);

            Refresh();
        }

        // ----- Actions -----

        ShopItem CurrentItem
        {
            get { return shop.items.Length > 0 ? shop.items[Mathf.Clamp(shop.currentItem, 0, shop.items.Length - 1)] : null; }
        }

        bool IsOwned
        {
            get { return CurrentItem != null && CurrentItem.lockState == 1; }
        }

        void OnMainButton()
        {
            if (ECCModalTracker.AnyOpen || CurrentItem == null) return;

            if (IsOwned == false && ECCWallet.CanAfford(CurrentItem.price) == false)
            {
                // Not enough money: the gift popup offers some
                ECCToast.Show("پول کافی نداری", ECCRuntimeUI.AccentColor);
                ECCCoinGiftPopup.Open();
                return;
            }

            bool buying = IsOwned == false;

            // Buys a car for sale, or starts the chase with an owned one
            shop.SelectItem();

            if (buying && IsOwned)
            {
                ECCToast.Show(ECCCarNames.Get(CurrentItem.itemIcon) + " مال تو شد!", ECCRuntimeUI.GoodColor);
                ECCMenuHub.RefreshIndicators();
                ECCPunchScale.Play(priceTag);
                ECCRewardFx.Confetti();
            }

            Refresh();
        }

        void OnSideButton()
        {
            if (ECCModalTracker.AnyOpen || CurrentItem == null) return;

            if (IsOwned) OpenStyle();
            else ECCCarTrial.Request(shop, null);
        }

        void OpenStyle()
        {
            if (CurrentItem == null || CurrentItem.itemIcon == null) return;

            ECCCarStyleUI.Open(CurrentItem.itemIcon.name, shop.currentIcon);
        }

        void BuyUpgrade(ECCUpgradeType type)
        {
            if (ECCModalTracker.AnyOpen || IsOwned == false) return;

            string car = CurrentItem.itemIcon.name;

            int cost = ECCUpgrades.GetNextCost(car, type);

            if (cost < 0) return;

            if (ECCWallet.CanAfford(cost) == false)
            {
                ECCToast.Show("پول کافی نداری", ECCRuntimeUI.AccentColor);
                ECCCoinGiftPopup.Open();
                return;
            }

            if (ECCUpgrades.TryBuy(car, type))
            {
                ECCToast.Show(ECCUpgrades.GetName(type) + " ارتقا یافت!", ECCRuntimeUI.GoodColor);

                foreach (StatRow row in rows)
                {
                    if (row.type == type) ECCPunchScale.Play(row.upgrade);
                }
            }

            Refresh();
        }

        // ----- Refresh -----

        void Refresh()
        {
            if (shop == null || CurrentItem == null || CurrentItem.itemIcon == null) return;

            shownItem = shop.currentItem;

            ShopItem item = CurrentItem;
            string car = item.itemIcon.name;
            bool owned = IsOwned;

            ECCRuntimeUI.SetText(carName, ECCCarNames.Get(item.itemIcon));
            ECCRuntimeUI.SetText(carCount, "ماشین " + ECCUIKit.PersianDigits(shop.currentItem + 1) + " از " + ECCUIKit.PersianDigits(shop.items.Length));

            // The price tag: the price for a car for sale, «مال توست» for an owned one
            if (owned)
            {
                priceTag.color = ECCCarTheme.CandyBlue;
                priceDollar.gameObject.SetActive(false);
                priceText.font = ECCCarTheme.TitleFont;
                priceText.rectTransform.anchoredPosition = new Vector2(0, 2);
                ECCRuntimeUI.SetText(priceText, "مال توست");
            }
            else
            {
                priceTag.color = ECCCarTheme.CandyOrange;
                priceDollar.gameObject.SetActive(true);
                priceText.font = ECCCarTheme.TitleFont;
                priceText.rectTransform.anchoredPosition = new Vector2(-24, 2);
                ECCRuntimeUI.SetText(priceText, ECCWallet.Format(item.price));
            }

            SetClass(item);

            // The car's saved look on the spinning car, and the side button
            if (shop.currentIcon)
            {
                try
                {
                    ECCCarStyle.ApplyTo(shop.currentIcon, car);
                }
                catch (System.Exception exception)
                {
                    Debug.LogWarning("Couldn't apply the car's look: " + exception.Message);
                }
            }

            sideButton.targetGraphic.color = owned ? ECCCarTheme.Hex(0x8E5CF7) : ECCCarTheme.CandyOrange;
            ECCRuntimeUI.SetText(sideLabel, owned ? "شخصی‌سازی" : "امتحان با ویدئو");

            ECCCar stats = item.itemIcon.GetComponent<ECCCar>();

            foreach (StatRow row in rows)
            {
                int level = ECCUpgrades.GetLevel(car, row.type);
                int cost = ECCUpgrades.GetNextCost(car, row.type);

                float baseValue = stats ? GetStat(stats, row.type) : 0;
                float best = BestStat(row.type);
                float perLevel = PerLevel(row.type);
                float full = best + ECCUpgrades.MaxLevel * perLevel;

                row.fill.rectTransform.sizeDelta = new Vector2(BarWidth * Mathf.Clamp01(baseValue / full), BarHeight);
                row.upgradeFill.rectTransform.sizeDelta = new Vector2(BarWidth * Mathf.Clamp01((baseValue + level * perLevel) / full), BarHeight);
                row.upgradeFill.color = Color.Lerp(StatColor(row.type), Color.white, 0.5f);

                ECCRuntimeUI.SetText(row.level, "سطح " + ECCUIKit.PersianDigits(level) + " از " + ECCUIKit.PersianDigits(ECCUpgrades.MaxLevel));

                Text label = row.upgradeLabel;

                if (owned == false)
                {
                    // A car for sale shows its stats, but can't be upgraded until it's bought
                    row.upgrade.interactable = false;
                    row.upgrade.targetGraphic.color = ECCCarTheme.CandyGray;
                    row.upgradeDollar.gameObject.SetActive(false);
                    label.rectTransform.anchoredPosition = Vector2.zero;
                    label.font = ECCCarTheme.TitleFont;
                    ECCRuntimeUI.SetText(label, "اول بخرش");
                }
                else if (cost < 0)
                {
                    row.upgrade.interactable = false;
                    row.upgrade.targetGraphic.color = ECCCarTheme.CandyGray;
                    row.upgradeDollar.gameObject.SetActive(false);
                    label.rectTransform.anchoredPosition = Vector2.zero;
                    label.font = ECCCarTheme.TitleFont;
                    ECCRuntimeUI.SetText(label, "حداکثر");
                }
                else
                {
                    // Tapping without enough money opens the gift popup, so it stays clickable
                    row.upgrade.interactable = true;
                    row.upgrade.targetGraphic.color = ECCWallet.CanAfford(cost) ? ECCCarTheme.CandyGreen : ECCCarTheme.CandyGray;
                    row.upgradeDollar.gameObject.SetActive(true);
                    label.rectTransform.anchoredPosition = new Vector2(-24, 2);
                    label.font = ECCCarTheme.TitleFont;
                    ECCRuntimeUI.SetText(label, ECCWallet.Format(cost));
                }
            }

            // The main button
            if (owned)
            {
                mainButton.targetGraphic.color = ECCCarTheme.CandyOrange;
                mainDollar.gameObject.SetActive(false);
                mainLabel.rectTransform.anchoredPosition = new Vector2(0, 14);
                ECCRuntimeUI.SetText(mainLabel, "شروع با این ماشین");
                ECCRuntimeUI.SetText(mainDetail, ModeDetail());
            }
            else
            {
                bool affordable = ECCWallet.CanAfford(item.price);

                mainButton.targetGraphic.color = affordable ? ECCCarTheme.CandyGreen : ECCCarTheme.CandyGray;
                mainDollar.gameObject.SetActive(true);
                mainLabel.rectTransform.anchoredPosition = new Vector2(30, 14);
                ECCRuntimeUI.SetText(mainLabel, "خرید با " + ECCWallet.Format(item.price) + " دلار");
                ECCRuntimeUI.SetText(mainDetail, affordable ? "بعد از خرید می‌توانی ارتقایش بدهی" : "پول کافی نداری؛ برای پول رایگان بزن");
            }

            mainLabel.font = ECCCarTheme.TitleFont;
        }

        static readonly string[] ClassNames = { "C", "B", "A", "S" };
        static readonly Color[] ClassColors = { ECCCarTheme.Hex(0x7D8BA6), ECCCarTheme.CandyGreen, ECCCarTheme.Hex(0x8E5CF7), ECCCarTheme.Hex(0xF5A623) };

        /// <summary>
        /// The car's class from its price's rank among all the garage's cars ( the free starting car is class C )
        /// </summary>
        void SetClass(ShopItem item)
        {
            int cheaper = 0;

            foreach (ShopItem other in shop.items)
            {
                if (other != null && other.price < item.price) cheaper++;
            }

            int tier = Mathf.Clamp(cheaper * ClassNames.Length / Mathf.Max(1, shop.items.Length), 0, ClassNames.Length - 1);

            classTag.color = ClassColors[tier];
            ECCRuntimeUI.SetText(classText, "کلاس " + ClassNames[tier]);
        }

        /// <summary>
        /// The game mode the start button plays, so it's clear from the garage too
        /// </summary>
        static string ModeDetail()
        {
            if (ECCGameModeManager.CurrentMode == GameModeType.Levels)
            {
                ECCLevelData level = ECCLevelCatalog.Instance.SelectedLevel;

                if (level != null) return "حالت مرحله‌ای: " + level.DisplayName;
            }

            return "حالت بی‌نهایت";
        }

        static float GetStat(ECCCar car, ECCUpgradeType type)
        {
            switch (type)
            {
                case ECCUpgradeType.Speed: return car.speed;
                case ECCUpgradeType.Handling: return car.rotateSpeed;
                default: return car.health;
            }
        }

        static float PerLevel(ECCUpgradeType type)
        {
            switch (type)
            {
                case ECCUpgradeType.Speed: return ECCUpgrades.SpeedPerLevel;
                case ECCUpgradeType.Handling: return ECCUpgrades.HandlingPerLevel;
                default: return ECCUpgrades.HealthPerLevel;
            }
        }

        /// <summary>
        /// The highest base value of a stat among the garage's cars ( the bars are relative to it )
        /// </summary>
        float BestStat(ECCUpgradeType type)
        {
            float best = 0.01f;

            foreach (ShopItem item in shop.items)
            {
                ECCCar car = item.itemIcon ? item.itemIcon.GetComponent<ECCCar>() : null;

                if (car) best = Mathf.Max(best, GetStat(car, type));
            }

            return best;
        }
    }

    /// <summary>
    /// Persian names of the cars, from their prefab names
    /// </summary>
    public static class ECCCarNames
    {
        public static string Get(Transform car)
        {
            if (car == null) return "-";

            return GetByName(car.name);
        }

        public static string GetByName(string carName)
        {
            if (string.IsNullOrEmpty(carName)) return "-";

            string name = carName.Replace("(Clone)", string.Empty).Trim();

            switch (name)
            {
                case "Coupe": return "کوپه";
                case "Sedan": return "سدان";
                case "PickupTruck": return "وانت";
                case "Van": return "ون";
                case "MonsterTruck": return "مانستر تراک";

                case "SedanCop1": return "سدان پلیس";
                case "VanCop1": return "ون پلیس";
                case "PickupTruckCop1": return "وانت پلیس";

                // PolygonApocalypse vehicles
                case "SM_Veh_Buggy_01": return "باگی";
                case "SM_Veh_IceCream_01": return "ماشین بستنی";
                case "SM_Veh_Car_Medium_01": return "سواری";
                case "SM_Veh_Army_Truck_01": return "کامیون ارتشی";
                case "SM_Veh_Prison_Bus_01": return "اتوبوس زندان";
                case "SM_Veh_NewsVan_01": return "ون خبرنگار";
                case "SM_Veh_Ute_01": return "وانت اسپرت";
                case "SM_Veh_Muscle_01": return "ماسل کار";
                case "SM_Veh_BigRig_01": return "تریلی";

                // Kenney Car Kit
                case "Kenney_HatchbackSports": return "هاچ‌بک اسپرت";
                case "Kenney_Taxi": return "تاکسی";
                case "Kenney_SedanSports": return "سدان اسپرت";
                case "Kenney_SuvLuxury": return "شاسی‌بلند";
                case "Kenney_Race": return "ماشین مسابقه";
                case "Kenney_RaceFuture": return "ماشین آینده";

                default: return name.Replace('_', ' ');
            }
        }
    }
}
