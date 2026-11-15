using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace EndlessCarChase
{
    /// <summary>
    /// The «شخصی‌سازی ماشین» sheet in the garage: it covers the garage's card and leaves the spinning car visible above it,
    /// so every option is tried on the car right away. Tabs for paint, rims, neon and tire trail ( right to left ).
    /// Tapping an owned option puts it on the car; tapping one that isn't bought previews it and shows the buy button.
    /// Closing puts the car's saved look back ( an unbought preview doesn't stay ).
    /// </summary>
    public class ECCCarStyleUI : MonoBehaviour
    {
        const float SheetHeight = 660;
        const float TileWidth = 150;
        const float TileHeight = 156;

        static ECCCarStyleUI open;

        Canvas canvas;
        RectTransform sheet;
        RectTransform content;
        Text carTitle;
        readonly List<Image> tabs = new List<Image>();
        Button buyButton;
        Text buyLabel;
        Text hint;

        Transform preview;
        string carName;
        ECCStyleCategory category = ECCStyleCategory.Paint;
        ECCStyleOption previewed;

        public static bool IsOpen
        {
            get { return open != null; }
        }

        /// <summary>
        /// Opens the sheet for an owned car. preview is the car object shown in the garage ( may be null )
        /// </summary>
        public static void Open(string carName, Transform preview)
        {
            if (open) return;

            RectTransform safe;
            Canvas canvas;

            safe = ECCUIKit.CreateSafeCanvas("CanvasCarStyle", 63, out canvas);
            canvas.gameObject.AddComponent<ECCModalTracker>();

            open = canvas.gameObject.AddComponent<ECCCarStyleUI>();
            open.canvas = canvas;
            open.carName = carName;
            open.preview = preview;
            open.Build(safe);
        }

        void Build(RectTransform safe)
        {
            Vector2 bottomCenter = new Vector2(0.5f, 0);
            Vector2 topCenter = new Vector2(0.5f, 1);

            // The white sheet over the bottom of the screen ( also under the safe area )
            Image card = ECCRuntimeUI.CreateImage("Sheet", canvas.transform, bottomCenter, new Vector2(0, -40), new Vector2(0, SheetHeight + 40), Color.white);
            card.rectTransform.anchorMin = new Vector2(0, 0);
            card.rectTransform.anchorMax = new Vector2(1, 0);
            card.rectTransform.pivot = bottomCenter;
            card.sprite = ECCCarTheme.CardSprite("panel");
            card.type = Image.Type.Sliced;
            card.pixelsPerUnitMultiplier = 1.4f;
            card.raycastTarget = true;

            // Behind the safe area: the sheet's content ( tabs, options, buttons ) is drawn over it and takes the taps
            card.transform.SetAsFirstSibling();

            sheet = ECCRuntimeUI.CreateRect("Content", safe, bottomCenter, Vector2.zero, new Vector2(680, SheetHeight));
            sheet.pivot = bottomCenter;

            Text title = ECCRuntimeUI.CreateText("Title", sheet, "شخصی‌سازی ماشین", 32, topCenter, new Vector2(40, -34), new Vector2(420, 46), ECCCarTheme.CardInk);
            title.font = ECCCarTheme.TitleFont;
            ECCCarTheme.InkText(title, ECCCarTheme.CardInk);

            carTitle = ECCRuntimeUI.CreateText("Car", sheet, ECCCarNames.GetByName(carName), 20, topCenter, new Vector2(40, -70), new Vector2(420, 28), ECCCarTheme.CardSubInk);
            ECCCarTheme.InkText(carTitle, ECCCarTheme.CardSubInk);

            // The shared coin balance, top left
            ECCCoinBalanceButton coins = ECCCoinBalanceButton.Create(sheet, new Vector2(0, 1), new Vector2(46, -12));
            ((RectTransform)coins.transform).pivot = new Vector2(0, 1);
            coins.transform.localScale = Vector3.one * 0.72f;

            Button close = ECCRuntimeUI.CreateButton("ButtonClose", sheet, string.Empty, 20, new Vector2(1, 1), new Vector2(-20, -14), new Vector2(70, 70), ECCCarTheme.CandyRed);
            ((RectTransform)close.transform).pivot = new Vector2(1, 1);
            ECCCarTheme.CreateIcon("Icon", close.transform, "Icons/close", new Vector2(0.5f, 0.5f), Vector2.zero, 34, Color.white);
            close.onClick.AddListener(Close);

            // Tabs, right to left
            ECCStyleCategory[] categories = { ECCStyleCategory.Paint, ECCStyleCategory.Rims, ECCStyleCategory.Neon, ECCStyleCategory.Trail };

            for (int index = 0; index < categories.Length; index++)
            {
                ECCStyleCategory tabCategory = categories[index];
                float x = 240 - index * 160;

                Button tab = ECCRuntimeUI.CreateButton("Tab" + tabCategory, sheet, ECCCarStyle.CategoryName(tabCategory), 21, topCenter, new Vector2(x, -128), new Vector2(150, 58), ECCCarTheme.CandyGray);
                ECCUIKit.KeepColorWhenDisabled(tab);
                tab.onClick.AddListener(delegate { ShowCategory(tabCategory); });

                tabs.Add((Image)tab.targetGraphic);
            }

            content = ECCScrollList.Create(sheet, new Vector2(0, -170), new Vector2(660, SheetHeight - 290));

            // The buy button of a previewed option, or a hint
            buyButton = ECCRuntimeUI.CreateButton("ButtonBuy", sheet, string.Empty, 28, bottomCenter, new Vector2(0, 26), new Vector2(560, 88), ECCCarTheme.CandyGreen);
            ECCCarTheme.Chunky(buyButton);
            buyLabel = ECCRuntimeUI.GetLabel(buyButton);
            buyButton.onClick.AddListener(BuyPreviewed);

            hint = ECCRuntimeUI.CreateText("Hint", sheet, "روی هر گزینه بزن تا روی ماشین ببینی", 21, bottomCenter, new Vector2(0, 60), new Vector2(600, 32), ECCCarTheme.CardSubInk);
            ECCCarTheme.InkText(hint, ECCCarTheme.CardSubInk);

            ShowCategory(ECCStyleCategory.Paint);
        }

        void ShowCategory(ECCStyleCategory shown)
        {
            category = shown;
            previewed = null;

            ApplySaved();

            for (int index = 0; index < tabs.Count; index++)
            {
                tabs[index].color = index == (int)shown ? ECCCarTheme.CandyOrange : ECCCarTheme.CandyGray;
            }

            RefreshGrid();
        }

        void RefreshGrid()
        {
            for (int index = content.childCount - 1; index >= 0; index--) Destroy(content.GetChild(index).gameObject);

            List<ECCStyleOption> options = ECCCarStyle.InCategory(category);
            string chosen = ECCCarStyle.GetChoice(carName, category);

            const int columns = 4;

            for (int index = 0; index < options.Count; index++)
            {
                int row = index / columns;
                int column = index % columns;

                // Right to left
                float x = (1.5f - column) * (TileWidth + 6);
                float y = -10 - row * (TileHeight + 12);

                CreateTile(options[index], new Vector2(x, y), options[index].id == chosen);
            }

            ECCScrollList.SetHeight(content, 20 + Mathf.CeilToInt(options.Count / (float)columns) * (TileHeight + 12));

            RefreshBuyBar();
        }

        void CreateTile(ECCStyleOption option, Vector2 position, bool equipped)
        {
            Vector2 center = new Vector2(0.5f, 0.5f);
            bool owned = ECCCarStyle.IsOwned(option);
            bool isPreview = previewed == option;

            Image tile = ECCRuntimeUI.CreateImage("Option_" + option.id, content, new Vector2(0.5f, 1), position, new Vector2(TileWidth, TileHeight), equipped || isPreview ? ECCCarTheme.Hex(0xFFF4D6) : ECCCarTheme.Hex(0xF1F4F9));
            tile.rectTransform.pivot = new Vector2(0.5f, 1);
            ECCCarTheme.Round(tile);
            tile.gameObject.AddComponent<ECCKeepColors>();

            if (equipped || isPreview)
            {
                Outline frame = tile.gameObject.AddComponent<Outline>();
                frame.effectColor = equipped ? ECCCarTheme.CandyGreen : ECCCarTheme.CandyOrange;
                frame.effectDistance = new Vector2(3, -3);
            }

            Button button = tile.gameObject.AddComponent<Button>();
            button.targetGraphic = tile;
            button.transition = Selectable.Transition.None;
            button.onClick.AddListener(delegate { OnOption(option); });
            tile.gameObject.AddComponent<ECCPressScale>();

            CreateSwatch(tile.transform, option, new Vector2(0, -50));

            Text name = ECCRuntimeUI.CreateText("Name", tile.transform, option.name, 18, new Vector2(0.5f, 1), new Vector2(0, -102), new Vector2(TileWidth - 10, 26), ECCCarTheme.CardInk);
            name.resizeTextForBestFit = true;
            name.resizeTextMinSize = 12;
            name.resizeTextMaxSize = 18;
            ECCCarTheme.InkText(name, ECCCarTheme.CardInk);

            if (equipped)
            {
                Text state = ECCRuntimeUI.CreateText("State", tile.transform, "انتخاب شده", 16, new Vector2(0.5f, 1), new Vector2(0, -132), new Vector2(TileWidth - 10, 24), ECCCarTheme.InkGreen);
                ECCCarTheme.InkText(state, ECCCarTheme.InkGreen);
            }
            else if (owned)
            {
                Text state = ECCRuntimeUI.CreateText("State", tile.transform, "داری", 16, new Vector2(0.5f, 1), new Vector2(0, -132), new Vector2(TileWidth - 10, 24), ECCCarTheme.CardSubInk);
                ECCCarTheme.InkText(state, ECCCarTheme.CardSubInk);
            }
            else
            {
                RectTransform price = ECCRuntimeUI.CreateRect("Price", tile.transform, new Vector2(0.5f, 1), new Vector2(0, -132), new Vector2(TileWidth - 10, 26));
                ECCUIKit.CreateCoinIcon("Coin", price, new Vector2(0.5f, 0.5f), new Vector2(34, 0), 22);
                Text amount = ECCUIKit.CreateNumberText("Amount", price, ECCWallet.Format(option.price), 17, new Vector2(0.5f, 0.5f), new Vector2(-10, 0), new Vector2(80, 24), ECCCarTheme.InkAmber);
                ECCCarTheme.InkText(amount, ECCCarTheme.InkAmber);
            }
        }

        /// <summary>
        /// The option's color in a disc: a ring for rims, a soft glow for neon, a stripe for trails, all colors for rainbow
        /// </summary>
        static void CreateSwatch(Transform parent, ECCStyleOption option, Vector2 position)
        {
            Vector2 top = new Vector2(0.5f, 1);
            Vector2 center = new Vector2(0.5f, 0.5f);

            Image shade = ECCUIKit.CreateCircle("SwatchShade", parent, top, position + new Vector2(0, -4), 74, new Color(0, 0, 0, 0.18f));
            shade.rectTransform.pivot = center;
            shade.raycastTarget = false;

            if (option.rainbow)
            {
                Color[] colors = { ECCCarTheme.Hex(0xFF1744), ECCCarTheme.Hex(0xFF9100), ECCCarTheme.Hex(0xFFEA00), ECCCarTheme.Hex(0x00E676), ECCCarTheme.Hex(0x2979FF), ECCCarTheme.Hex(0xD500F9) };

                for (int index = 0; index < colors.Length; index++)
                {
                    float angle = index / (float)colors.Length * Mathf.PI * 2;

                    Image dot = ECCUIKit.CreateCircle("Dot", parent, top, position + new Vector2(Mathf.Sin(angle), Mathf.Cos(angle)) * 20, 32, colors[index]);
                    dot.rectTransform.pivot = center;
                    dot.raycastTarget = false;
                }

                return;
            }

            bool none = string.IsNullOrEmpty(option.id);

            Image disc = ECCUIKit.CreateCircle("Swatch", parent, top, position, 70, none && option.category == ECCStyleCategory.Paint ? ECCCarTheme.Hex(0xD9DEE8) : option.color);
            disc.rectTransform.pivot = center;
            disc.raycastTarget = false;

            if (none)
            {
                // The car's own look: a check for the factory paint, a slash for "none"
                string icon = option.category == ECCStyleCategory.Paint || option.category == ECCStyleCategory.Rims ? "Icons/check" : "Icons/close";
                ECCCarTheme.CreateIcon("Icon", disc.transform, icon, center, Vector2.zero, 34, Color.white).rectTransform.pivot = center;
                return;
            }

            if (option.category == ECCStyleCategory.Rims)
            {
                Image hole = ECCUIKit.CreateCircle("Hub", disc.transform, center, Vector2.zero, 26, ECCCarTheme.Asphalt);
                hole.rectTransform.pivot = center;
                hole.raycastTarget = false;
            }
            else if (option.category == ECCStyleCategory.Neon)
            {
                Image car = ECCRuntimeUI.CreateImage("Car", disc.transform, center, new Vector2(0, 6), new Vector2(40, 20), ECCCarTheme.Asphalt);
                ECCCarTheme.Round(car);
                car.raycastTarget = false;
            }
            else if (option.category == ECCStyleCategory.Trail)
            {
                Image stripe = ECCRuntimeUI.CreateImage("Stripe", disc.transform, center, Vector2.zero, new Vector2(16, 56), new Color(1, 1, 1, 0.55f));
                ECCCarTheme.Round(stripe);
                stripe.rectTransform.localEulerAngles = new Vector3(0, 0, -35);
                stripe.raycastTarget = false;
            }
        }

        void OnOption(ECCStyleOption option)
        {
            if (ECCCarStyle.IsOwned(option))
            {
                previewed = null;

                ECCCarStyle.Equip(carName, option);
                ApplySaved();

                ECCHaptics.Vibrate(ECCHaptics.Kind.Light);
            }
            else
            {
                previewed = option;
                ApplyPreview(option);
            }

            RefreshGrid();
        }

        void RefreshBuyBar()
        {
            bool show = previewed != null;

            buyButton.gameObject.SetActive(show);
            hint.gameObject.SetActive(show == false);

            if (show == false) return;

            bool affordable = ECCWallet.CanAfford(previewed.price);

            buyButton.targetGraphic.color = affordable ? ECCCarTheme.CandyGreen : ECCCarTheme.CandyGray;
            ECCRuntimeUI.SetText(buyLabel, "خرید «" + previewed.name + "» با " + ECCWallet.Format(previewed.price) + " سکه");
        }

        void BuyPreviewed()
        {
            if (previewed == null) return;

            if (ECCCarStyle.Buy(previewed) == false)
            {
                ECCToast.Show("سکه‌ی کافی نداری", ECCRuntimeUI.AccentColor);
                ECCCoinGiftPopup.Open();
                return;
            }

            ECCCarStyle.Equip(carName, previewed);

            ECCToast.Show("«" + previewed.name + "» مال تو شد!", ECCRuntimeUI.GoodColor);
            ECCRewardFx.Confetti();
            ECCHaptics.Vibrate(ECCHaptics.Kind.Reward);

            previewed = null;

            ApplySaved();
            RefreshGrid();
        }

        void ApplySaved()
        {
            if (preview == null) return;

            // A car model the look can't handle must never break the sheet
            try
            {
                ECCCarStyle.ApplyTo(preview, carName);
            }
            catch (System.Exception exception)
            {
                Debug.LogWarning("Couldn't apply the car's look: " + exception.Message);
            }
        }

        void ApplyPreview(ECCStyleOption option)
        {
            if (preview == null) return;

            ECCStyleOption paint = option.category == ECCStyleCategory.Paint ? option : ECCCarStyle.Find(ECCStyleCategory.Paint, ECCCarStyle.GetChoice(carName, ECCStyleCategory.Paint));
            ECCStyleOption rims = option.category == ECCStyleCategory.Rims ? option : ECCCarStyle.Find(ECCStyleCategory.Rims, ECCCarStyle.GetChoice(carName, ECCStyleCategory.Rims));
            ECCStyleOption neon = option.category == ECCStyleCategory.Neon ? option : ECCCarStyle.Find(ECCStyleCategory.Neon, ECCCarStyle.GetChoice(carName, ECCStyleCategory.Neon));
            ECCStyleOption trail = option.category == ECCStyleCategory.Trail ? option : ECCCarStyle.Find(ECCStyleCategory.Trail, ECCCarStyle.GetChoice(carName, ECCStyleCategory.Trail));

            try
            {
                ECCCarStyle.ApplyTo(preview, paint, rims, neon, trail);
            }
            catch (System.Exception exception)
            {
                Debug.LogWarning("Couldn't preview the car's look: " + exception.Message);
            }
        }

        void Update()
        {
            // The coin balance can change while the sheet is open ( a gift video )
            if (previewed != null) RefreshBuyBar();

            // The garage was left ( a chase started ): close
            ECCGameController gameController = ECCGameController.Instance;

            if (gameController && gameController.gameStarted) Close();
        }

        public void Close()
        {
            previewed = null;

            ApplySaved();

            if (open == this) open = null;

            Destroy(canvas.gameObject);
        }

        void OnDestroy()
        {
            if (open == this) open = null;
        }
    }
}
