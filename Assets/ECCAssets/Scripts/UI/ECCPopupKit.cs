using UnityEngine;
using UnityEngine.UI;

namespace EndlessCarChase
{
    /// <summary>
    /// The shared look of the main menu's popups ( missions, login calendar, achievements, other games, free coins ):
    /// the white result card with a colored ribbon ( ECCResultCard ), a glowing badge, the coin balance in the corner,
    /// the round close button, and rounded light tiles with a colored strip on the right ( the start side in RTL ),
    /// small colored chips and thin progress bars. Everything is laid out right to left with the same paddings.
    /// </summary>
    public static class ECCPopupKit
    {
        public static readonly Color TileColor = ECCCarTheme.Hex(0xF6F8FC);
        public static readonly Color TileBorder = ECCCarTheme.Hex(0xE1E6EF);
        public static readonly Color TileShade = ECCCarTheme.Hex(0xD3D9E4);
        public static readonly Color TrackColor = ECCCarTheme.Hex(0xE3E8F0);

        /// <summary>
        /// A popup card with a ribbon, a badge and an optional subtitle; the close button and the coin balance in the
        /// corners. Call card.Finish(false) after adding the content
        /// </summary>
        public static ECCResultCard Create(string canvasName, int sortingOrder, string title, Color color, string icon, string subtitle, bool coins, UnityEngine.Events.UnityAction onClose, float width = 660, float badgeSize = 104)
        {
            ECCResultCard card = ECCResultCard.Create(canvasName, sortingOrder, title, color, width);

            card.Badge(icon, color, true, badgeSize);

            if (string.IsNullOrEmpty(subtitle) == false) card.Line(subtitle, 21, 32, ECCCarTheme.CardSubInk);

            if (coins)
            {
                // The shared balance button, top left under the ribbon ( its + stays inside the card )
                ECCCoinBalanceButton balance = ECCCoinBalanceButton.Create(card.Panel, new Vector2(0, 1), new Vector2(46, -20));
                ((RectTransform)balance.transform).pivot = new Vector2(0, 1);
                balance.transform.localScale = Vector3.one * 0.72f;
            }

            Button close = ECCUIKit.CreateCloseButton(card.Panel);
            if (onClose != null) close.onClick.AddListener(onClose);

            return card;
        }

        /// <summary>
        /// A rounded light tile with a thin border, a soft shade under it and a colored strip on its right edge.
        /// The tile keeps its own colors ( the card styling leaves it alone ); pivot at its top center
        /// </summary>
        public static Image Tile(string name, Transform parent, Vector2 anchor, Vector2 position, Vector2 size, Color accent)
        {
            Image border = ECCRuntimeUI.CreateImage(name, parent, anchor, position, size, TileBorder);
            border.rectTransform.pivot = new Vector2(0.5f, 1);
            ECCCarTheme.Round(border);
            border.gameObject.AddComponent<ECCKeepColors>();

            Shadow lip = border.gameObject.AddComponent<Shadow>();
            lip.effectColor = TileShade;
            lip.effectDistance = new Vector2(0, -5);

            Image face = ECCRuntimeUI.CreateImage("Face", border.transform, new Vector2(0.5f, 0.5f), Vector2.zero, Vector2.zero, TileColor);
            face.rectTransform.anchorMin = Vector2.zero;
            face.rectTransform.anchorMax = Vector2.one;
            face.rectTransform.offsetMin = new Vector2(3, 3);
            face.rectTransform.offsetMax = new Vector2(-3, -3);
            ECCCarTheme.Round(face);
            face.raycastTarget = false;

            if (accent.a > 0)
            {
                Image strip = ECCRuntimeUI.CreateImage("Accent", border.transform, new Vector2(1, 0.5f), new Vector2(-3, 0), new Vector2(12, size.y - 22), accent);
                strip.rectTransform.pivot = new Vector2(1, 0.5f);
                ECCCarTheme.Round(strip);
                strip.pixelsPerUnitMultiplier = 4;
                strip.raycastTarget = false;
            }

            return border;
        }

        /// <summary>
        /// A round icon in a colored disc with a darker shade under it
        /// </summary>
        public static Image IconDisc(Transform parent, Vector2 anchor, Vector2 position, float size, Color color, string icon, Sprite sprite = null)
        {
            Vector2 center = new Vector2(0.5f, 0.5f);

            Image shade = ECCUIKit.CreateCircle("IconShade", parent, anchor, position + new Vector2(0, -4), size, new Color(color.r * 0.7f, color.g * 0.7f, color.b * 0.7f, 1));
            shade.rectTransform.pivot = center;
            shade.raycastTarget = false;

            Image disc = ECCUIKit.CreateCircle("IconDisc", parent, anchor, position, size, color);
            disc.rectTransform.pivot = center;
            disc.raycastTarget = false;

            Image image = ECCRuntimeUI.CreateImage("Icon", disc.transform, center, Vector2.zero, new Vector2(size * 0.6f, size * 0.6f), Color.white);
            image.rectTransform.pivot = center;
            image.sprite = sprite ? sprite : ECCCarTheme.Icon(icon);
            image.preserveAspect = true;
            image.raycastTarget = false;

            return disc;
        }

        /// <summary>
        /// A small rounded colored label
        /// </summary>
        public static Text Chip(Transform parent, string name, string text, Color color, Vector2 anchor, Vector2 position, Vector2 size, int fontSize = 16)
        {
            Image chip = ECCRuntimeUI.CreateImage(name, parent, anchor, position, size, color);
            chip.rectTransform.pivot = anchor;
            ECCCarTheme.Round(chip);
            chip.pixelsPerUnitMultiplier = 48 / Mathf.Max(8, size.y * 0.5f);
            chip.raycastTarget = false;

            Text label = ECCRuntimeUI.CreateText("Text", chip.transform, text, fontSize, new Vector2(0.5f, 0.5f), new Vector2(0, 1), size, Color.white);
            label.font = ECCCarTheme.TitleFont;
            label.resizeTextForBestFit = true;
            label.resizeTextMinSize = 10;
            label.resizeTextMaxSize = fontSize;
            ECCCarTheme.InkText(label, Color.white);

            return label;
        }

        /// <summary>
        /// A thin rounded progress bar that fills from the right; pivot on its right end
        /// </summary>
        public static Image Bar(Transform parent, Vector2 anchor, Vector2 position, Vector2 size, float fraction, Color color)
        {
            Image track = ECCRuntimeUI.CreateImage("Bar", parent, anchor, position, size, TrackColor);
            track.rectTransform.pivot = new Vector2(1, 0.5f);
            ECCCarTheme.Round(track);
            track.pixelsPerUnitMultiplier = 48 / Mathf.Max(4, size.y * 0.5f);
            track.raycastTarget = false;

            float clamped = Mathf.Clamp01(fraction);

            Image fill = ECCRuntimeUI.CreateImage("Fill", track.transform, new Vector2(1, 0.5f), Vector2.zero, new Vector2(size.x * clamped, size.y), color);
            fill.rectTransform.pivot = new Vector2(1, 0.5f);
            ECCCarTheme.Round(fill);
            fill.pixelsPerUnitMultiplier = track.pixelsPerUnitMultiplier;
            fill.raycastTarget = false;
            fill.gameObject.SetActive(clamped > 0.001f);

            return fill;
        }

        /// <summary>
        /// [coin] amount, in a small cream pill; pivot at the anchor
        /// </summary>
        public static RectTransform CoinPill(Transform parent, int amount, Vector2 anchor, Vector2 position, float width = 118)
        {
            Image pill = ECCRuntimeUI.CreateImage("Reward", parent, anchor, position, new Vector2(width, 40), ECCCarTheme.Hex(0xFFF1CC));
            pill.rectTransform.pivot = anchor;
            ECCCarTheme.Round(pill);
            pill.pixelsPerUnitMultiplier = 2.4f;
            pill.raycastTarget = false;

            ECCUIKit.CreateCoinIcon("Coin", pill.transform, new Vector2(1, 0.5f), new Vector2(-20, 0), 32);

            Text text = ECCUIKit.CreateNumberText("Amount", pill.transform, ECCWallet.Format(amount), 21, new Vector2(1, 0.5f), new Vector2(-40, 0), new Vector2(width - 46, 36), ECCCarTheme.InkAmber);
            text.alignment = TextAnchor.MiddleRight;
            text.rectTransform.pivot = new Vector2(1, 0.5f);
            ECCCarTheme.InkText(text, ECCCarTheme.InkAmber);

            return pill.rectTransform;
        }

        /// <summary>
        /// A text on a tile, right aligned ( pivot on its right edge ), dark ink
        /// </summary>
        public static Text TileText(Transform parent, string name, string content, int size, Vector2 anchor, Vector2 position, Vector2 box, Color color, bool heavy = false)
        {
            Text text = ECCRuntimeUI.CreateText(name, parent, content, size, anchor, position, box, color);
            text.alignment = TextAnchor.MiddleRight;
            text.rectTransform.pivot = new Vector2(1, 0.5f);
            text.horizontalOverflow = HorizontalWrapMode.Wrap;
            text.resizeTextForBestFit = true;
            text.resizeTextMinSize = Mathf.Max(10, size - 7);
            text.resizeTextMaxSize = size;

            if (heavy) text.font = ECCCarTheme.TitleFont;

            ECCCarTheme.InkText(text, color);

            return text;
        }

        /// <summary>
        /// A claim style button: green and pulsing when ready, gray otherwise
        /// </summary>
        public static Button ClaimButton(Transform parent, string name, string label, bool ready, bool done, Vector2 anchor, Vector2 position, Vector2 size)
        {
            Color color = ready ? ECCCarTheme.CandyGreen : (done ? ECCCarTheme.Hex(0xB8C0CE) : ECCCarTheme.CandyGray);

            Button button = ECCRuntimeUI.CreateButton(name, parent, label, 19, anchor, position, size, color);
            ((RectTransform)button.transform).pivot = anchor;
            ECCCarTheme.Chunky(button);
            ECCUIKit.KeepColorWhenDisabled(button);
            button.interactable = ready;

            Text text = ECCRuntimeUI.GetLabel(button);
            text.resizeTextForBestFit = true;
            text.resizeTextMinSize = 12;
            text.resizeTextMaxSize = 19;

            if (done) ECCCarTheme.CreateIcon("Check", button.transform, "Icons/check", new Vector2(1, 0.5f), new Vector2(-14, 2), 22, Color.white).rectTransform.pivot = new Vector2(0.5f, 0.5f);

            if (ready) ECCPulse.Set(button, true, 0.05f);

            return button;
        }
    }
}
