using UnityEngine;
using UnityEngine.UI;

namespace EndlessCarChase
{
    /// <summary>
    /// Small helpers for building UI from code, so new screens work without editing the canvas prefabs.
    /// All texts go through ECCPersianText, so pass normal Persian strings.
    /// </summary>
    public static class ECCRuntimeUI
    {
        static Font defaultFont;

        // The game's Persian font, imported as Assets/ECCAssets/Resources/Fonts/Vazirmatn.ttf
        public const string PersianFontPath = "Fonts/Vazirmatn";

        /// <summary>
        /// The Vazirmatn font when it's imported, otherwise Unity's built-in dynamic font, which falls back to the system fonts.
        /// Unity 2022.2+ renamed the built-in font to LegacyRuntime.ttf and throws for Arial.ttf
        /// </summary>
        public static Font DefaultFont
        {
            get
            {
                if (defaultFont == null) defaultFont = Resources.Load<Font>(PersianFontPath);

                if (defaultFont == null)
                {
#if UNITY_2022_2_OR_NEWER
                    defaultFont = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
#else
                    defaultFont = Resources.GetBuiltinResource<Font>("Arial.ttf");
#endif
                }

                return defaultFont;
            }
        }

        /// <summary>
        /// Creates a screen-space overlay canvas that scales with the screen size
        /// </summary>
        public static Canvas CreateCanvas(string name, int sortingOrder)
        {
            GameObject canvasObject = new GameObject(name);

            Canvas canvas = canvasObject.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = sortingOrder;

            CanvasScaler scaler = canvasObject.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1280, 720);
            scaler.matchWidthOrHeight = 0.5f;

            canvasObject.AddComponent<GraphicRaycaster>();

            return canvas;
        }

        /// <summary>
        /// Creates an empty rect anchored at the given point of its parent
        /// </summary>
        public static RectTransform CreateRect(string name, Transform parent, Vector2 anchor, Vector2 anchoredPosition, Vector2 size)
        {
            GameObject rectObject = new GameObject(name, typeof(RectTransform));

            RectTransform rect = rectObject.GetComponent<RectTransform>();
            rect.SetParent(parent, false);
            rect.anchorMin = rect.anchorMax = rect.pivot = anchor;
            rect.anchoredPosition = anchoredPosition;
            rect.sizeDelta = size;

            return rect;
        }

        public static Image CreateImage(string name, Transform parent, Vector2 anchor, Vector2 anchoredPosition, Vector2 size, Color color)
        {
            Image image = CreateRect(name, parent, anchor, anchoredPosition, size).gameObject.AddComponent<Image>();
            image.color = color;

            return image;
        }

        /// <summary>
        /// Creates a text. Persian text is shaped automatically
        /// </summary>
        public static Text CreateText(string name, Transform parent, string content, int fontSize, Vector2 anchor, Vector2 anchoredPosition, Vector2 size, Color color)
        {
            Text text = CreateRect(name, parent, anchor, anchoredPosition, size).gameObject.AddComponent<Text>();
            text.font = DefaultFont;
            text.fontSize = fontSize;
            text.alignment = TextAnchor.MiddleCenter;
            text.color = color;
            text.horizontalOverflow = HorizontalWrapMode.Overflow;
            text.verticalOverflow = VerticalWrapMode.Overflow;
            text.raycastTarget = false;
            text.text = ECCPersianText.Fix(content);

            Outline outline = text.gameObject.AddComponent<Outline>();
            outline.effectColor = new Color(0, 0, 0, 0.6f);
            outline.effectDistance = new Vector2(2, -2);

            return text;
        }

        /// <summary>
        /// Creates a full screen dimmed background ( which blocks clicks to the UI behind it ) with a centered panel.
        /// Returns the panel; destroy panel.parent to close the modal
        /// </summary>
        public static RectTransform CreateModal(Transform canvas, Vector2 size)
        {
            Image dim = CreateImage("Modal", canvas, new Vector2(0.5f, 0.5f), Vector2.zero, Vector2.zero, ECCCarTheme.CardDim);
            dim.rectTransform.anchorMin = Vector2.zero;
            dim.rectTransform.anchorMax = Vector2.one;
            dim.rectTransform.sizeDelta = Vector2.zero;

            Image panel = CreateImage("Panel", dim.transform, new Vector2(0.5f, 0.5f), Vector2.zero, size, PanelColor);

            // The CrowdRush card: white, rounded, a soft shadow; its texts and buttons are styled once it's built
            ECCCarTheme.Card(panel.rectTransform);

            return panel.rectTransform;
        }

        public static readonly Color PanelColor = new Color(0.1f, 0.1f, 0.15f, 0.96f);
        public static readonly Color AccentColor = new Color(1, 0.55f, 0.1f, 1);
        public static readonly Color ButtonColor = new Color(0.22f, 0.22f, 0.3f, 1);
        public static readonly Color GoodColor = new Color(0.3f, 0.85f, 0.35f, 1);
        public static readonly Color BadColor = new Color(1, 0.3f, 0.25f, 1);
        public static readonly Color StarColor = new Color(1, 0.8f, 0.1f, 1);
        public static readonly Color StarOffColor = new Color(1, 1, 1, 0.2f);

        /// <summary>
        /// Creates a diamond ( a rotated square ), used as a star icon that doesn't need a font glyph
        /// </summary>
        public static Image CreateDiamond(string name, Transform parent, Vector2 anchor, Vector2 anchoredPosition, float size, Color color)
        {
            Image diamond = CreateImage(name, parent, anchor, anchoredPosition, new Vector2(size, size), color);
            diamond.rectTransform.pivot = new Vector2(0.5f, 0.5f);
            diamond.rectTransform.localEulerAngles = Vector3.forward * 45;
            diamond.raycastTarget = false;

            return diamond;
        }

        /// <summary>
        /// Creates a row of star diamonds centered on the given position
        /// </summary>
        public static Image[] CreateStarRow(Transform parent, Vector2 anchor, Vector2 center, int count, int litCount, float size)
        {
            Image[] stars = new Image[count];

            float spacing = size * 1.8f;

            for (int index = 0; index < count; index++)
            {
                Vector2 position = center + new Vector2((index - (count - 1) * 0.5f) * spacing, 0);

                stars[index] = CreateDiamond("Star" + index, parent, anchor, position, size, index < litCount ? StarColor : StarOffColor);
            }

            return stars;
        }

        /// <summary>
        /// Sets the text of a label, shaping Persian text
        /// </summary>
        public static void SetText(Text text, string content)
        {
            if (text == null) return;

            // Skip the shaping work when the text didn't change ( HUDs set their texts every frame )
            string previous;

            if (rawTexts.TryGetValue(text, out previous) && previous == content) return;

            if (rawTexts.Count > 500) rawTexts.Clear();

            rawTexts[text] = content;

            text.text = ECCPersianText.Fix(content);
        }

        static readonly System.Collections.Generic.Dictionary<Text, string> rawTexts = new System.Collections.Generic.Dictionary<Text, string>();

        /// <summary>
        /// Gets the label of a button created with CreateButton
        /// </summary>
        public static Text GetLabel(Button button)
        {
            return button.GetComponentInChildren<Text>();
        }

        /// <summary>
        /// Creates a button with a background and a centered label
        /// </summary>
        public static Button CreateButton(string name, Transform parent, string label, int fontSize, Vector2 anchor, Vector2 anchoredPosition, Vector2 size, Color color)
        {
            Image background = CreateImage(name, parent, anchor, anchoredPosition, size, color);

            Button button = background.gameObject.AddComponent<Button>();
            button.targetGraphic = background;

            CreateText("Label", background.transform, label, fontSize, new Vector2(0.5f, 0.5f), Vector2.zero, size, Color.white);

            // Rounded, with a darker lip: the car theme's button
            ECCCarTheme.Chunky(button);

            return button;
        }
    }
}
