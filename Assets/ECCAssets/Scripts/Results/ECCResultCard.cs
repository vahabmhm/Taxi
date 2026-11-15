using System;
using UnityEngine;
using UnityEngine.UI;

namespace EndlessCarChase
{
    /// <summary>
    /// The shared layout of the result screens ( win, loss, car wrecked, endless end ) and the pause screen: a white
    /// CrowdRush card with a colored ribbon, and everything below stacked top to bottom with fixed gaps, so nothing can
    /// overlap whatever the content: a round badge ( with turning light rays for good news ), a title, lines, stars,
    /// a money pill, info boxes, one big main button and a row of smaller buttons.
    /// The card's height follows its content ( Finish ).
    /// Usage: var card = ECCResultCard.Create(...); card.Badge(...); card.Title(...); ... card.Finish();
    /// </summary>
    public class ECCResultCard
    {
        public const float DefaultWidth = 620;
        const float Padding = 36;
        const float Gap = 14;

        static readonly Color Gold = ECCCarTheme.Hex(0xFFC83D);

        public RectTransform Panel { get; private set; }

        readonly float width;
        float cursor;
        Image ribbon;

        public float ContentWidth
        {
            get { return width - Padding * 2; }
        }

        ECCResultCard(RectTransform panel, float width)
        {
            Panel = panel;
            this.width = width;
        }

        /// <summary>
        /// A popup card ( dimmed background, pop-in, registered as an open popup ) with a ribbon header
        /// </summary>
        public static ECCResultCard Create(string canvasName, int sortingOrder, string header, Color headerColor, float width = DefaultWidth)
        {
            RectTransform panel = ECCUIKit.CreatePopup(canvasName, sortingOrder, new Vector2(width, 400));

            ECCResultCard card = new ECCResultCard(panel, width);
            card.AddRibbon(header, headerColor);

            return card;
        }

        /// <summary>
        /// The same layout on a panel made elsewhere ( the pause screen's own canvas )
        /// </summary>
        public static ECCResultCard On(RectTransform panel, string header, Color headerColor, float width = DefaultWidth)
        {
            ECCResultCard card = new ECCResultCard(panel, width);
            card.AddRibbon(header, headerColor);

            return card;
        }

        void AddRibbon(string header, Color color)
        {
            ribbon = ECCRuntimeUI.CreateImage("Ribbon", Panel, new Vector2(0.5f, 1), new Vector2(0, 30), new Vector2(Mathf.Min(width - 40, 470), 80), color);
            ribbon.rectTransform.pivot = new Vector2(0.5f, 0.5f);
            ribbon.sprite = ECCCarTheme.CardSprite("ribbon");
            ribbon.type = Image.Type.Sliced;
            ribbon.pixelsPerUnitMultiplier = 1.4f;
            ribbon.raycastTarget = false;
            ribbon.gameObject.AddComponent<ECCKeepColors>();

            Text title = ECCRuntimeUI.CreateText("Header", ribbon.transform, header, 28, new Vector2(0.5f, 0.5f), new Vector2(0, 4), new Vector2(ribbon.rectTransform.sizeDelta.x - 50, 56), Color.white);
            ECCCarTheme.CandyLabel(title);
            title.resizeTextForBestFit = true;
            title.resizeTextMinSize = 16;
            title.resizeTextMaxSize = 28;
            title.horizontalOverflow = HorizontalWrapMode.Wrap;

            cursor = 86;
        }

        Vector2 Top
        {
            get { return new Vector2(0.5f, 1); }
        }

        RectTransform Place(RectTransform rect, float height)
        {
            rect.anchorMin = rect.anchorMax = Top;
            rect.pivot = Top;
            rect.anchoredPosition = new Vector2(0, -cursor);

            cursor += height + Gap;

            return rect;
        }

        public void Space(float height)
        {
            cursor += height;
        }

        // ----- Content -----

        /// <summary>
        /// A round badge with an icon, a darker shade under it, and, for good news, a soft glow and light rays turning
        /// behind it. Returns the badge's face ( its children can be changed, like a countdown )
        /// </summary>
        public Image Badge(string icon, Color color, bool rays, float size = 132)
        {
            RectTransform holder = Place(ECCRuntimeUI.CreateRect("Badge", Panel, Top, Vector2.zero, new Vector2(size, size)), size + 4);
            holder.gameObject.AddComponent<ECCKeepColors>();

            Vector2 center = new Vector2(0.5f, 0.5f);

            if (rays)
            {
                Image glow = ECCRuntimeUI.CreateImage("Glow", holder, center, Vector2.zero, new Vector2(size * 2.4f, size * 2.4f), new Color(color.r, color.g, color.b, 0.55f));
                glow.rectTransform.pivot = center;
                glow.sprite = ECCCarTheme.CardSprite("glow");
                glow.raycastTarget = false;

                Image light = ECCRuntimeUI.CreateImage("Rays", holder, center, Vector2.zero, new Vector2(size * 2.6f, size * 2.6f), new Color(1, 0.95f, 0.7f, 0.55f));
                light.rectTransform.pivot = center;
                light.sprite = ECCCarTheme.CardSprite("rays");
                light.preserveAspect = true;
                light.raycastTarget = false;
                light.gameObject.AddComponent<ECCSpin>().degreesPerSecond = 22;
            }

            Image shade = ECCUIKit.CreateCircle("Shade", holder, center, new Vector2(0, -8), size, new Color(color.r * 0.68f, color.g * 0.68f, color.b * 0.68f, 1));
            shade.rectTransform.pivot = center;
            shade.raycastTarget = false;

            Image face = ECCUIKit.CreateCircle("Face", holder, center, Vector2.zero, size, color);
            face.rectTransform.pivot = center;
            face.raycastTarget = false;

            Image ring = ECCRuntimeUI.CreateImage("Ring", face.transform, center, Vector2.zero, new Vector2(size + 8, size + 8), Color.white);
            ring.rectTransform.pivot = center;
            ring.sprite = ECCCarTheme.Icon("Icons/ring");
            ring.raycastTarget = false;

            if (string.IsNullOrEmpty(icon) == false)
            {
                Image image = ECCCarTheme.CreateIcon("Icon", face.transform, icon, center, new Vector2(0, 2), size * 0.56f, Color.white);
                image.rectTransform.pivot = center;

                Shadow iconShadow = image.gameObject.AddComponent<Shadow>();
                iconShadow.effectColor = new Color(0, 0, 0, 0.25f);
                iconShadow.effectDistance = new Vector2(0, -3);
            }

            return face;
        }

        public Text Title(string text, Color color, int size = 44)
        {
            Text title = ECCRuntimeUI.CreateText("Title", Panel, text, size, Top, Vector2.zero, new Vector2(ContentWidth, size + 14), color);
            ECCCarTheme.InkText(title, color);
            title.font = ECCCarTheme.TitleFont;
            title.resizeTextForBestFit = true;
            title.resizeTextMinSize = 20;
            title.resizeTextMaxSize = size;
            title.horizontalOverflow = HorizontalWrapMode.Wrap;

            Place(title.rectTransform, size + 14);

            return title;
        }

        /// <summary>
        /// A line of text ( or two, it wraps ), dark gray by default
        /// </summary>
        public Text Line(string text, int size = 22, float height = 34, Color? color = null)
        {
            Color ink = color ?? ECCCarTheme.CardSubInk;

            Text line = ECCRuntimeUI.CreateText("Line", Panel, text, size, Top, Vector2.zero, new Vector2(ContentWidth, height), ink);
            ECCCarTheme.InkText(line, ink);
            line.horizontalOverflow = HorizontalWrapMode.Wrap;
            line.resizeTextForBestFit = true;
            line.resizeTextMinSize = 14;
            line.resizeTextMaxSize = size;

            Place(line.rectTransform, height);

            return line;
        }

        /// <summary>
        /// Stars in a row ( the middle one bigger for three ), earned ones gold, popping in one after another
        /// </summary>
        public void Stars(int count, int lit, float size = 64)
        {
            RectTransform row = Place(ECCRuntimeUI.CreateRect("Stars", Panel, Top, Vector2.zero, new Vector2(ContentWidth, size + 16)), size + 16);
            row.gameObject.AddComponent<ECCKeepColors>();

            float step = size * 1.1f;

            for (int index = 0; index < count; index++)
            {
                bool middle = count == 3 && index == 1;
                float starSize = middle ? size * 1.2f : size;

                // Right to left, like the reading order
                float x = ((count - 1) * 0.5f - index) * step;

                Image star = ECCCarTheme.CreateIcon("Star" + index, row, "Icons/star", new Vector2(0.5f, 0.5f), new Vector2(x, middle ? 8 : 0), starSize, index < lit ? Gold : new Color(ECCCarTheme.CardInk.r, ECCCarTheme.CardInk.g, ECCCarTheme.CardInk.b, 0.15f));
                star.rectTransform.pivot = new Vector2(0.5f, 0.5f);

                if (index < lit) star.gameObject.AddComponent<ECCDelayedPop>().delay = 0.25f + index * 0.22f;
            }
        }

        /// <summary>
        /// A dark pill with the dollar coin and an amount, like «+۲۵۰»
        /// </summary>
        public Text MoneyPill(string label, int amount)
        {
            RectTransform pill = Place(ECCCarTheme.CreatePanel("Money", Panel, Top, Vector2.zero, new Vector2(Mathf.Min(ContentWidth, 420), 78), ECCCarTheme.CardInk).rectTransform, 78);
            pill.gameObject.AddComponent<ECCKeepColors>();

            Image pillImage = pill.GetComponent<Image>();
            pillImage.pixelsPerUnitMultiplier = 48 / 39f;

            Image dollar = ECCRuntimeUI.CreateImage("Dollar", pill, new Vector2(0, 0.5f), new Vector2(-8, 0), new Vector2(86, 86), Color.white);
            dollar.rectTransform.pivot = new Vector2(0, 0.5f);
            dollar.sprite = ECCCoinBalanceButton.DollarSprite;
            dollar.preserveAspect = true;
            dollar.raycastTarget = false;

            Text amountText = ECCRuntimeUI.CreateText("Amount", pill, "+" + ECCWallet.Format(amount), 38, new Vector2(0, 0.5f), new Vector2(86, 2), new Vector2(170, 64), Gold);
            amountText.rectTransform.pivot = new Vector2(0, 0.5f);
            amountText.alignment = TextAnchor.MiddleLeft;
            ECCCarTheme.CandyLabel(amountText);
            amountText.color = Gold;

            Text labelText = ECCRuntimeUI.CreateText("Label", pill, label, 24, new Vector2(1, 0.5f), new Vector2(-24, 2), new Vector2(170, 56), Color.white);
            labelText.rectTransform.pivot = new Vector2(1, 0.5f);
            labelText.alignment = TextAnchor.MiddleRight;
            labelText.resizeTextForBestFit = true;
            labelText.resizeTextMinSize = 14;
            labelText.resizeTextMaxSize = 24;
            ECCCarTheme.InkText(labelText, new Color(1, 1, 1, 0.85f));

            return amountText;
        }

        /// <summary>
        /// A light gray box with an icon on the right and a text, for a goal, a tip or a stat
        /// </summary>
        public Text InfoBox(string icon, Color iconColor, string text, float height = 70)
        {
            Image box = ECCCarTheme.CreatePanel("Info", Panel, Top, Vector2.zero, new Vector2(ContentWidth, height), ECCCarTheme.Hex(0xEEF1F6));
            box.pixelsPerUnitMultiplier = 2;
            box.gameObject.AddComponent<ECCKeepColors>();
            Place(box.rectTransform, height);

            Image disc = ECCUIKit.CreateCircle("Icon", box.transform, new Vector2(1, 0.5f), new Vector2(-14, 0), height - 18, iconColor);
            disc.rectTransform.pivot = new Vector2(1, 0.5f);
            disc.raycastTarget = false;

            Image image = ECCCarTheme.CreateIcon("Glyph", disc.transform, icon, new Vector2(0.5f, 0.5f), Vector2.zero, (height - 18) * 0.6f, Color.white);
            image.rectTransform.pivot = new Vector2(0.5f, 0.5f);

            Text label = ECCRuntimeUI.CreateText("Text", box.transform, text, 21, new Vector2(1, 0.5f), new Vector2(-(height + 4), 0), new Vector2(ContentWidth - height - 24, height - 8), ECCCarTheme.CardInk);
            label.rectTransform.pivot = new Vector2(1, 0.5f);
            label.alignment = TextAnchor.MiddleRight;
            label.horizontalOverflow = HorizontalWrapMode.Wrap;
            label.resizeTextForBestFit = true;
            label.resizeTextMinSize = 14;
            label.resizeTextMaxSize = 21;
            ECCCarTheme.InkText(label, ECCCarTheme.CardInk);

            return label;
        }

        /// <summary>
        /// A thin progress bar with a caption above it ( right to left: fills from the right )
        /// </summary>
        public void Progress(string caption, float fraction, Color color)
        {
            Line(caption, 20, 28);

            Image track = ECCCarTheme.CreatePanel("Track", Panel, Top, Vector2.zero, new Vector2(ContentWidth, 18), ECCCarTheme.Hex(0xE6EAF1));
            track.pixelsPerUnitMultiplier = 48 / 9f;
            track.gameObject.AddComponent<ECCKeepColors>();
            Place(track.rectTransform, 18);

            Image fill = ECCCarTheme.CreatePanel("Fill", track.transform, new Vector2(1, 0.5f), Vector2.zero, new Vector2(ContentWidth * Mathf.Clamp01(fraction), 18), color);
            fill.rectTransform.pivot = new Vector2(1, 0.5f);
            fill.pixelsPerUnitMultiplier = 48 / 9f;
        }

        // ----- Buttons -----

        /// <summary>
        /// A full-width candy button with an optional icon on the right
        /// </summary>
        public Button WideButton(string name, string label, Color color, string icon, float height = 96, int fontSize = 32)
        {
            Button button = ECCRuntimeUI.CreateButton(name, Panel, label, fontSize, Top, Vector2.zero, new Vector2(ContentWidth, height), color);
            ECCUIKit.KeepColorWhenDisabled(button);
            Place((RectTransform)button.transform, height + 6);

            Text text = ECCRuntimeUI.GetLabel(button);
            text.horizontalOverflow = HorizontalWrapMode.Wrap;
            text.resizeTextForBestFit = true;
            text.resizeTextMinSize = 16;
            text.resizeTextMaxSize = fontSize;

            if (string.IsNullOrEmpty(icon) == false)
            {
                float iconSize = Mathf.Min(height * 0.5f, 50);

                Image image = ECCCarTheme.CreateIcon("Icon", button.transform, icon, new Vector2(1, 0.5f), new Vector2(-26 - iconSize * 0.5f, 3), iconSize, Color.white);
                image.rectTransform.pivot = new Vector2(0.5f, 0.5f);

                text.rectTransform.sizeDelta = new Vector2(ContentWidth - iconSize - 70, height - 10);
                text.rectTransform.anchoredPosition = new Vector2(-(iconSize + 26) * 0.5f, 3);
            }
            else
            {
                text.rectTransform.sizeDelta = new Vector2(ContentWidth - 40, height - 10);
                text.rectTransform.anchoredPosition = new Vector2(0, 3);
            }

            return button;
        }

        /// <summary>
        /// Buttons side by side in one row, equal widths, right to left in the given order
        /// </summary>
        public Button[] ButtonRow(float height, params ButtonSpec[] specs)
        {
            Button[] buttons = new Button[specs.Length];

            float gap = 14;
            float buttonWidth = (ContentWidth - gap * (specs.Length - 1)) / Mathf.Max(1, specs.Length);

            RectTransform row = Place(ECCRuntimeUI.CreateRect("ButtonRow", Panel, Top, Vector2.zero, new Vector2(ContentWidth, height)), height + 6);

            for (int index = 0; index < specs.Length; index++)
            {
                ButtonSpec spec = specs[index];

                float x = (ContentWidth - buttonWidth) * 0.5f - index * (buttonWidth + gap);

                Button button = ECCRuntimeUI.CreateButton(spec.name, row, spec.label, 26, new Vector2(0.5f, 0.5f), new Vector2(x, 0), new Vector2(buttonWidth, height), spec.color);
                ((RectTransform)button.transform).pivot = new Vector2(0.5f, 0.5f);
                ECCUIKit.KeepColorWhenDisabled(button);

                Text text = ECCRuntimeUI.GetLabel(button);
                text.horizontalOverflow = HorizontalWrapMode.Wrap;
                text.resizeTextForBestFit = true;
                text.resizeTextMinSize = 14;
                text.resizeTextMaxSize = 26;

                if (string.IsNullOrEmpty(spec.icon) == false)
                {
                    float iconSize = Mathf.Min(height * 0.48f, 40);

                    Image image = ECCCarTheme.CreateIcon("Icon", button.transform, spec.icon, new Vector2(1, 0.5f), new Vector2(-16 - iconSize * 0.5f, 3), iconSize, Color.white);
                    image.rectTransform.pivot = new Vector2(0.5f, 0.5f);

                    text.rectTransform.sizeDelta = new Vector2(buttonWidth - iconSize - 36, height - 10);
                    text.rectTransform.anchoredPosition = new Vector2(-(iconSize + 16) * 0.5f, 3);
                }
                else
                {
                    text.rectTransform.sizeDelta = new Vector2(buttonWidth - 20, height - 10);
                    text.rectTransform.anchoredPosition = new Vector2(0, 3);
                }

                if (spec.onClick != null) button.onClick.AddListener(spec.onClick);

                buttons[index] = button;
            }

            return buttons;
        }

        /// <summary>
        /// A row holder for custom content ( like the sound and music buttons )
        /// </summary>
        public RectTransform CustomRow(float height)
        {
            RectTransform row = Place(ECCRuntimeUI.CreateRect("Row", Panel, Top, Vector2.zero, new Vector2(ContentWidth, height)), height);
            row.gameObject.AddComponent<ECCKeepColors>();

            return row;
        }

        /// <summary>
        /// Sizes the card to its content ( keeps it on screen ) and puts the ribbon on top of everything
        /// </summary>
        public void Finish()
        {
            Finish(true);
        }

        /// <summary>
        /// Finish; tapGuard false for screens opened by a tap in the menu ( no game finger to guard against )
        /// </summary>
        public void Finish(bool tapGuard)
        {
            float height = cursor - Gap + Padding;

            Panel.sizeDelta = new Vector2(width, height);

            // A tall card is scaled down to fit short screens ( the portrait canvas is 720 units wide, see
            // ECCUIKit.CreateCanvas ), leaving room for the ribbon on top
            float screenHeight = Screen.height >= Screen.width ? 720f * Screen.height / Mathf.Max(1, Screen.width) : 1000;
            float scale = Mathf.Min(1, (screenHeight - 140) / height);

            ECCPopIn popIn = Panel.GetComponent<ECCPopIn>();

            if (popIn) popIn.restScale = scale;
            else Panel.localScale = Vector3.one * scale;

            ribbon.transform.SetAsLastSibling();

            // A close button stays above the ribbon
            Transform close = Panel.Find("ButtonClose");
            if (close) close.SetAsLastSibling();

            // The buttons sit where the thumb was steering: they don't take taps until the finger has left the screen
            if (tapGuard && Panel.GetComponent<ECCTapGuard>() == null) Panel.gameObject.AddComponent<ECCTapGuard>();
        }

        public struct ButtonSpec
        {
            public string name;
            public string label;
            public Color color;
            public string icon;
            public UnityEngine.Events.UnityAction onClick;

            public ButtonSpec(string name, string label, Color color, string icon, UnityEngine.Events.UnityAction onClick)
            {
                this.name = name;
                this.label = label;
                this.color = color;
                this.icon = icon;
                this.onClick = onClick;
            }
        }
    }

    /// <summary>
    /// Keeps a just-opened screen's buttons from taking a tap that was meant for the game: the result screens pop up
    /// right where the thumb was steering, so a press still held, or a quick reflex tap, would start the next level.
    /// The buttons are faded and ignore taps for a moment, and until every finger has left the screen; then they fade in.
    /// Taps meanwhile land on the dimmed background, which does nothing ( unscaled time, works while paused )
    /// </summary>
    public class ECCTapGuard : MonoBehaviour
    {
        public float minimumTime = 0.9f;

        // Once the screen is free of fingers, a short wait more, for a reflex tap right after lifting
        public float releaseTime = 0.25f;

        const float LockedAlpha = 0.45f;

        CanvasGroup group;
        CanvasGroup[] buttonGroups;
        float time = 0;
        float releasedTime = 0;

        void Start()
        {
            group = gameObject.GetComponent<CanvasGroup>();
            if (group == null) group = gameObject.AddComponent<CanvasGroup>();

            group.blocksRaycasts = false;

            Button[] buttons = GetComponentsInChildren<Button>(true);
            buttonGroups = new CanvasGroup[buttons.Length];

            for (int index = 0; index < buttons.Length; index++)
            {
                CanvasGroup buttonGroup = buttons[index].GetComponent<CanvasGroup>();
                if (buttonGroup == null) buttonGroup = buttons[index].gameObject.AddComponent<CanvasGroup>();

                buttonGroup.alpha = LockedAlpha;
                buttonGroups[index] = buttonGroup;
            }
        }

        void Update()
        {
            time += Time.unscaledDeltaTime;

            bool pressed = Input.touchCount > 0 || Input.GetMouseButton(0);

            releasedTime = pressed ? 0 : releasedTime + Time.unscaledDeltaTime;

            if (time < minimumTime || releasedTime < releaseTime) return;

            group.blocksRaycasts = true;

            StartCoroutine(FadeIn());

            enabled = false;
        }

        System.Collections.IEnumerator FadeIn()
        {
            for (float fade = 0; fade < 0.2f; fade += Time.unscaledDeltaTime)
            {
                SetAlpha(Mathf.Lerp(LockedAlpha, 1, fade / 0.2f));

                yield return null;
            }

            SetAlpha(1);
        }

        void SetAlpha(float alpha)
        {
            foreach (CanvasGroup buttonGroup in buttonGroups)
            {
                if (buttonGroup) buttonGroup.alpha = alpha;
            }
        }
    }

    /// <summary>
    /// Hidden, then pops in with a small overshoot after a delay ( unscaled time, works while paused )
    /// </summary>
    public class ECCDelayedPop : MonoBehaviour
    {
        public float delay = 0.2f;

        float time = 0;
        Vector3 rest;

        void Start()
        {
            rest = transform.localScale;
            transform.localScale = Vector3.zero;
        }

        void Update()
        {
            time += Time.unscaledDeltaTime;

            float progress = Mathf.Clamp01((time - delay) / 0.3f);

            if (progress <= 0) return;

            // Ease out with an overshoot
            float scale = 1 + Mathf.Sin(progress * Mathf.PI) * 0.35f * (1 - progress) + (progress - 1) * (1 - progress);

            transform.localScale = rest * Mathf.Max(0, scale);

            if (progress >= 1)
            {
                transform.localScale = rest;
                enabled = false;
            }
        }
    }
}
