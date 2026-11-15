using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace EndlessCarChase
{
    /// <summary>
    /// The car theme of the new UI: night garage colors, rounded panels, chunky buttons with a lip, siren lights,
    /// license plates and the "police report" look of the daily challenge and the win / loss screens.
    /// Everything is drawn from code ( procedural sprites ) or from the game's own icons in Resources/ECCUI.
    /// </summary>
    public static class ECCCarTheme
    {
        // Night garage
        public static readonly Color Night = Hex(0x26215C);
        public static readonly Color NightMid = Hex(0x3C3489);
        public static readonly Color NightBright = Hex(0x534AB7);
        public static readonly Color NightLine = Hex(0x7F77DD);
        public static readonly Color NightText = Hex(0xCECBF6);

        // Road
        public static readonly Color Asphalt = Hex(0x2C2C2A);
        public static readonly Color AsphaltLight = Hex(0x444441);
        public static readonly Color Concrete = Hex(0x888780);
        public static readonly Color Yellow = Hex(0xEF9F27);
        public static readonly Color YellowDark = Hex(0x854F0B);
        public static readonly Color YellowInk = Hex(0x412402);

        // Siren
        public static readonly Color SirenRed = Hex(0xE24B4A);
        public static readonly Color SirenRedDark = Hex(0x791F1F);
        public static readonly Color SirenBlue = Hex(0x378ADD);

        // Police report paper
        public static readonly Color Paper = Hex(0xFAEEDA);
        public static readonly Color Ink = Hex(0x412402);
        public static readonly Color InkAmber = Hex(0x854F0B);
        public static readonly Color InkGreen = Hex(0x3B6D11);
        public static readonly Color InkRed = Hex(0xA32D2D);
        public static readonly Color InkOrange = Hex(0x993C1D);

        // License plate
        public static readonly Color Plate = Hex(0xF1EFE8);
        public static readonly Color PlateBlue = Hex(0x185FA5);

        static Sprite roundedSprite;
        static Sprite frameSprite;
        static readonly Dictionary<string, Sprite> icons = new Dictionary<string, Sprite>();

        public static Color Hex(int rgb, float alpha = 1)
        {
            return new Color(((rgb >> 16) & 255) / 255f, ((rgb >> 8) & 255) / 255f, (rgb & 255) / 255f, alpha);
        }

        /// <summary>
        /// A white rounded rectangle ( 9-sliced ), for panels and buttons
        /// </summary>
        public static Sprite RoundedSprite
        {
            get
            {
                if (roundedSprite == null) roundedSprite = CreateRoundedSprite(64, 22, 0);

                return roundedSprite;
            }
        }

        /// <summary>
        /// The outline of a rounded rectangle ( 9-sliced ), for stamps and frames
        /// </summary>
        public static Sprite FrameSprite
        {
            get
            {
                if (frameSprite == null) frameSprite = CreateRoundedSprite(64, 18, 6);

                return frameSprite;
            }
        }

        static Sprite CreateRoundedSprite(int size, int radius, int thickness)
        {
            Texture2D texture = new Texture2D(size, size, TextureFormat.RGBA32, false);
            texture.wrapMode = TextureWrapMode.Clamp;

            Color[] pixels = new Color[size * size];

            float half = size * 0.5f;

            for (int y = 0; y < size; y++)
            {
                for (int x = 0; x < size; x++)
                {
                    // Signed distance to the rounded rectangle's edge ( negative inside )
                    float qx = Mathf.Abs(x + 0.5f - half) - (half - radius);
                    float qy = Mathf.Abs(y + 0.5f - half) - (half - radius);
                    float distance = new Vector2(Mathf.Max(qx, 0), Mathf.Max(qy, 0)).magnitude + Mathf.Min(Mathf.Max(qx, qy), 0) - radius;

                    float alpha = Mathf.Clamp01(0.5f - distance);

                    // Hollow frame: only a band of "thickness" pixels inside the edge
                    if (thickness > 0) alpha *= Mathf.Clamp01(distance + thickness + 0.5f);

                    pixels[y * size + x] = new Color(1, 1, 1, alpha);
                }
            }

            texture.SetPixels(pixels);
            texture.Apply();

            float border = radius + 2;

            return Sprite.Create(texture, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f), 100, 0, SpriteMeshType.FullRect, new Vector4(border, border, border, border));
        }

        /// <summary>
        /// One of the game's icons copied to Resources/ECCUI ( SteeringIcon, SpeedIcon, WheelIcon, StarIcon, GarageIcon, GameArt )
        /// </summary>
        public static Sprite Icon(string name)
        {
            Sprite sprite;

            if (icons.TryGetValue(name, out sprite) && sprite) return sprite;

            Texture2D texture = Resources.Load<Texture2D>("ECCUI/" + name);

            sprite = texture ? Sprite.Create(texture, new Rect(0, 0, texture.width, texture.height), new Vector2(0.5f, 0.5f), 100) : ECCUIKit.CircleSprite;

            icons[name] = sprite;

            return sprite;
        }

        public static Image CreateIcon(string name, Transform parent, string icon, Vector2 anchor, Vector2 position, float size, Color color)
        {
            Image image = ECCRuntimeUI.CreateImage(name, parent, anchor, position, new Vector2(size, size), color);
            image.sprite = Icon(icon);
            image.preserveAspect = true;
            image.raycastTarget = false;

            return image;
        }

        /// <summary>
        /// Gives an image rounded corners
        /// </summary>
        public static Image Round(Image image)
        {
            image.sprite = RoundedSprite;
            image.type = Image.Type.Sliced;

            return image;
        }

        public static Image CreatePanel(string name, Transform parent, Vector2 anchor, Vector2 position, Vector2 size, Color color)
        {
            Image panel = Round(ECCRuntimeUI.CreateImage(name, parent, anchor, position, size, color));
            panel.raycastTarget = false;

            return panel;
        }

        // ----- CrowdRush template: palette, card sprites, title font -----

        public static readonly Color CardInk = Hex(0x2B2D42);
        public static readonly Color CardSubInk = Hex(0x6B7083);
        public static readonly Color CandyGreen = Hex(0x34C76F);
        public static readonly Color CandyGreenShade = Hex(0x1F9150);
        public static readonly Color CandyOrange = Hex(0xFF9F1C);
        public static readonly Color CandyOrangeShade = Hex(0xC97100);
        public static readonly Color CandyBlue = Hex(0x3FA9F5);
        public static readonly Color CandyRed = Hex(0xF2545B);
        public static readonly Color CandyGray = Hex(0xAEB4C2);
        public static readonly Color CardDim = new Color(0.02f, 0.03f, 0.08f, 0.68f);

        static readonly Dictionary<string, Sprite> cardSprites = new Dictionary<string, Sprite>();
        static Font titleFont;

        /// <summary>
        /// A 9-sliced sprite of the CrowdRush card set ( Resources/ECCUI/Card: panel, shadow, ribbon, glow, rays, panel_outline )
        /// </summary>
        public static Sprite CardSprite(string name)
        {
            Sprite sprite;

            if (cardSprites.TryGetValue(name, out sprite) && sprite) return sprite;

            sprite = Resources.Load<Sprite>("ECCUI/Card/" + name);

            if (sprite == null) sprite = RoundedSprite;

            cardSprites[name] = sprite;

            return sprite;
        }

        /// <summary>
        /// Vazirmatn Black, the heavy font of CrowdRush's titles and buttons ( falls back to the default font )
        /// </summary>
        public static Font TitleFont
        {
            get
            {
                if (titleFont == null) titleFont = Resources.Load<Font>("Fonts/Vazirmatn-Black");
                if (titleFont == null) titleFont = ECCRuntimeUI.DefaultFont;

                return titleFont;
            }
        }

        /// <summary>
        /// The CrowdRush button color for one of the old shared colors ( others stay as they are )
        /// </summary>
        public static Color CandyColor(Color color)
        {
            if (Near(color, ECCRuntimeUI.ButtonColor)) return CandyGray;
            if (Near(color, ECCRuntimeUI.AccentColor)) return CandyOrange;
            if (Near(color, ECCRuntimeUI.GoodColor)) return CandyGreen;
            if (Near(color, ECCRuntimeUI.BadColor)) return CandyRed;

            return color;
        }

        /// <summary>
        /// A CrowdRush "candy" button: rounded face, a darker shade under it ( follows the face color ), a soft shine on
        /// its top half, and a heavy white label with a small drop shadow
        /// </summary>
        public static Button Chunky(Button button)
        {
            Image image = button.targetGraphic as Image;

            if (image == null || image.sprite == ECCUIKit.CircleSprite) return button;

            Round(image);
            image.color = CandyColor(image.color);

            // A plain Shadow ( an Outline is a Shadow too, and must stay an outline )
            Shadow lip = null;

            foreach (Shadow shadow in button.GetComponents<Shadow>())
            {
                if (shadow.GetType() == typeof(Shadow)) lip = shadow;
            }

            if (lip == null) lip = button.gameObject.AddComponent<Shadow>();

            lip.effectDistance = new Vector2(0, -8);

            ECCCandyShade shade = button.GetComponent<ECCCandyShade>();
            if (shade == null) shade = button.gameObject.AddComponent<ECCCandyShade>();
            shade.Init(image, lip);

            if (image.transform.Find("Shine") == null)
            {
                Image shine = ECCRuntimeUI.CreateImage("Shine", image.transform, new Vector2(0.5f, 1), Vector2.zero, Vector2.zero, new Color(1, 1, 1, 0.18f));
                Round(shine);
                shine.raycastTarget = false;
                shine.rectTransform.anchorMin = new Vector2(0, 0.55f);
                shine.rectTransform.anchorMax = new Vector2(1, 1);
                shine.rectTransform.offsetMin = new Vector2(6, 0);
                shine.rectTransform.offsetMax = new Vector2(-6, -5);
                shine.transform.SetAsFirstSibling();
            }

            foreach (Text label in button.GetComponentsInChildren<Text>(true)) CandyLabel(label);

            return button;
        }

        /// <summary>
        /// A white heavy label with a small drop shadow ( instead of the dark outline )
        /// </summary>
        public static void CandyLabel(Text label)
        {
            if (label == null || label.transform.parent == null) return;

            label.font = label.font == ECCUIKit.NumberFont ? label.font : TitleFont;

            Outline outline = label.GetComponent<Outline>();
            if (outline) outline.enabled = false;

            Shadow drop = null;

            foreach (Shadow shadow in label.GetComponents<Shadow>())
            {
                if (shadow.GetType() == typeof(Shadow)) drop = shadow;
            }

            if (drop == null) drop = label.gameObject.AddComponent<Shadow>();

            drop.effectColor = new Color(0, 0, 0, 0.28f);
            drop.effectDistance = new Vector2(0, -3);
        }

        /// <summary>
        /// Turns a modal panel into a CrowdRush card: white, rounded ( 9-sliced card sprite ), a soft drop shadow behind
        /// it, no colored border. Its texts are recolored for the white card by ECCCardStyle
        /// </summary>
        public static void Card(RectTransform panel)
        {
            Image image = panel.GetComponent<Image>();

            if (image)
            {
                image.sprite = CardSprite("panel");
                image.type = Image.Type.Sliced;
                image.pixelsPerUnitMultiplier = 1.4f;
                image.color = Color.white;
            }

            Outline border = panel.GetComponent<Outline>();
            if (border) border.enabled = false;

            if (panel.parent && panel.parent.Find(panel.name + "Shadow") == null)
            {
                Image shadow = ECCRuntimeUI.CreateImage(panel.name + "Shadow", panel.parent, new Vector2(0.5f, 0.5f), Vector2.zero, Vector2.zero, new Color(0, 0, 0, 0.35f));
                shadow.sprite = CardSprite("shadow");
                shadow.type = Image.Type.Sliced;
                shadow.raycastTarget = false;
                shadow.transform.SetSiblingIndex(panel.GetSiblingIndex());

                ECCFollowRect follow = shadow.gameObject.AddComponent<ECCFollowRect>();
                follow.target = panel;
                follow.padding = new Vector4(22, 16, 22, 32);
            }

            if (panel.GetComponent<ECCCardStyle>() == null) panel.gameObject.AddComponent<ECCCardStyle>();
        }

        /// <summary>
        /// True while a panel is still a white card ( a screen may paint its panel another color )
        /// </summary>
        public static bool IsCard(RectTransform panel)
        {
            Image image = panel ? panel.GetComponent<Image>() : null;

            return image && image.sprite == CardSprite("panel") && image.color.r > 0.95f && image.color.g > 0.95f && image.color.b > 0.95f;
        }

        /// <summary>
        /// The CrowdRush round close button: a red candy circle with an X, on the card's top right corner
        /// </summary>
        public static void CandyClose(Button close)
        {
            if (close == null) return;

            RectTransform rect = (RectTransform)close.transform;
            rect.anchorMin = rect.anchorMax = new Vector2(1, 1);
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.sizeDelta = new Vector2(66, 66);
            rect.anchoredPosition = new Vector2(-30, -30);
            rect.SetAsLastSibling();

            Image face = close.targetGraphic as Image;
            if (face) face.color = CandyRed;

            Shadow lip = null;

            foreach (Shadow shadow in close.GetComponents<Shadow>())
            {
                if (shadow.GetType() == typeof(Shadow)) lip = shadow;
            }

            if (lip == null) lip = close.gameObject.AddComponent<Shadow>();

            lip.effectColor = new Color(CandyRed.r * 0.72f, CandyRed.g * 0.72f, CandyRed.b * 0.72f, 1);
            lip.effectDistance = new Vector2(0, -6);

            foreach (Text text in close.GetComponentsInChildren<Text>(true)) text.gameObject.SetActive(false);

            if (close.transform.Find("Icon") == null) CreateIcon("Icon", close.transform, "Icons/close", new Vector2(0.5f, 0.5f), Vector2.zero, 34, Color.white);
        }

        /// <summary>
        /// Sets a label color for a light background ( no dark outline )
        /// </summary>
        public static void InkText(Text text, Color color)
        {
            if (text == null) return;

            text.color = color;

            Outline outline = text.GetComponent<Outline>();

            if (outline) outline.enabled = false;
        }

        /// <summary>
        /// A red / blue siren bar. Its two halves flash in turn
        /// </summary>
        public static RectTransform CreateSirenBar(string name, Transform parent, float height)
        {
            RectTransform bar = ECCRuntimeUI.CreateRect(name, parent, new Vector2(0.5f, 1), Vector2.zero, new Vector2(0, height));
            bar.anchorMin = new Vector2(0, 1);
            bar.anchorMax = new Vector2(1, 1);
            bar.pivot = new Vector2(0.5f, 1);

            Image red = ECCRuntimeUI.CreateImage("Red", bar, new Vector2(1, 0.5f), Vector2.zero, Vector2.zero, SirenRed);
            red.rectTransform.anchorMin = new Vector2(0.5f, 0);
            red.rectTransform.anchorMax = new Vector2(1, 1);
            red.rectTransform.sizeDelta = Vector2.zero;
            red.raycastTarget = false;

            Image blue = ECCRuntimeUI.CreateImage("Blue", bar, new Vector2(0, 0.5f), Vector2.zero, Vector2.zero, SirenBlue);
            blue.rectTransform.anchorMin = new Vector2(0, 0);
            blue.rectTransform.anchorMax = new Vector2(0.5f, 1);
            blue.rectTransform.sizeDelta = Vector2.zero;
            blue.raycastTarget = false;

            ECCSirenFlash flash = bar.gameObject.AddComponent<ECCSirenFlash>();
            flash.red = red;
            flash.blue = blue;

            return bar;
        }

        // ----- Police report: the daily challenge popup and the win / loss screens -----

        public const float ReportHeaderHeight = 54;

        /// <summary>
        /// Turns a popup ( the daily challenge, the win / loss screens ) into a CrowdRush card: white card with a soft
        /// shadow, a colored ribbon over the top edge with the title, dark ink texts, candy buttons and the round red close
        /// button. Call it after the popup's content is built.
        /// stamp: an optional colored tag under the ribbon, on the top left ( like «فرار موفق» or «دستگیر شد» );
        /// its color is also the ribbon's color
        /// </summary>
        public static void PoliceReport(RectTransform panel, string header, string stamp, Color stampColor)
        {
            Card(panel);

            // Make room for the ribbon: the panel grows and the top-anchored content moves down
            panel.sizeDelta += new Vector2(0, ReportHeaderHeight);

            foreach (RectTransform child in panel)
            {
                if (child.anchorMin.y < 1 || child.anchorMax.y < 1 || child.name == "ButtonClose") continue;

                child.anchoredPosition -= new Vector2(0, ReportHeaderHeight);
            }

            RecolorForCard(panel);

            Color ribbonColor = string.IsNullOrEmpty(stamp) ? CandyOrange : stampColor;

            // The ribbon sits across the top edge, a little wider than the text
            Image ribbon = ECCRuntimeUI.CreateImage("ReportHeader", panel, new Vector2(0.5f, 1), new Vector2(0, 8), new Vector2(480, 76), ribbonColor);
            ribbon.sprite = CardSprite("ribbon");
            ribbon.type = Image.Type.Sliced;
            ribbon.pixelsPerUnitMultiplier = 1.4f;
            ribbon.raycastTarget = false;
            ribbon.gameObject.AddComponent<ECCKeepColors>();

            Text title = ECCRuntimeUI.CreateText("Header", ribbon.transform, header, 26, new Vector2(0.5f, 0.5f), new Vector2(0, 4), new Vector2(420, 50), Color.white);
            CandyLabel(title);

            Button close = null;

            foreach (Button button in panel.GetComponentsInChildren<Button>(true))
            {
                if (button.name == "ButtonClose") close = button;
                else if (button.GetComponentInParent<ECCCoinBalanceButton>() == null) Chunky(button);
            }

            if (close) CandyClose(close);

            if (string.IsNullOrEmpty(stamp) == false) CreateStamp(panel, stamp, stampColor);
        }

        /// <summary>
        /// A small colored tag on the top left of a card, under its ribbon
        /// </summary>
        public static Image CreateStamp(RectTransform panel, string text, Color color)
        {
            Image stamp = ECCRuntimeUI.CreateImage("Stamp", panel, new Vector2(0, 1), new Vector2(96, -ReportHeaderHeight - 30), new Vector2(160, 46), color);
            Round(stamp);
            stamp.raycastTarget = false;
            stamp.rectTransform.pivot = new Vector2(0.5f, 0.5f);
            stamp.rectTransform.localEulerAngles = Vector3.forward * 6;
            stamp.gameObject.AddComponent<ECCKeepColors>();

            Text label = ECCRuntimeUI.CreateText("Text", stamp.transform, text, 22, new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(154, 42), Color.white);
            CandyLabel(label);

            return stamp;
        }

        /// <summary>
        /// Dark ink for the labels drawn directly on a white card ( not the ones inside buttons, the coin balance or
        /// ECCKeepColors parts ), and light gray instead of dark see-through boxes. Safe to run again
        /// </summary>
        public static void RecolorForCard(RectTransform panel)
        {
            foreach (Text text in panel.GetComponentsInChildren<Text>(true))
            {
                if (text.GetComponentInParent<Button>() != null || text.GetComponentInParent<ECCKeepColors>() != null || text.GetComponentInParent<ECCCoinBalanceButton>() != null) continue;

                Color ink = InkFor(text.color);

                if (ink == text.color) continue;

                InkText(text, ink);

                if (text.fontSize >= 32 && text.font != ECCUIKit.NumberFont) text.font = TitleFont;
            }

            foreach (Image image in panel.GetComponentsInChildren<Image>(true))
            {
                if (image.transform == panel || image.GetComponentInParent<Button>() != null || image.GetComponentInParent<ECCKeepColors>() != null || image.GetComponentInParent<ECCCoinBalanceButton>() != null) continue;

                Color color = image.color;

                // Faint white shapes ( empty stars, rings ) become faint ink
                if (color.a < 0.5f && color.r > 0.9f && color.g > 0.9f && color.b > 0.9f) image.color = new Color(CardInk.r, CardInk.g, CardInk.b, Mathf.Max(color.a, 0.15f));

                // Dark see-through boxes ( info boxes, rows ) become the card's light gray
                else if (color.a < 0.8f && color.r < 0.2f && color.g < 0.2f && color.b < 0.25f) image.color = Hex(0xEEF1F6);
            }
        }

        static Color InkFor(Color color)
        {
            Color ink;

            if (Near(color, ECCRuntimeUI.StarColor)) ink = CandyOrangeShade;
            else if (Near(color, ECCRuntimeUI.GoodColor)) ink = CandyGreenShade;
            else if (Near(color, ECCRuntimeUI.BadColor)) ink = CandyRed;
            else if (Near(color, ECCRuntimeUI.AccentColor)) ink = CandyOrangeShade;
            else if (Near(color, NightText)) ink = CardSubInk;
            else if (color.r > 0.9f && color.g > 0.9f && color.b > 0.9f) ink = CardInk;
            else return color;

            return new Color(ink.r, ink.g, ink.b, Mathf.Max(color.a, 0.6f));
        }

        static bool Near(Color a, Color b)
        {
            return Mathf.Abs(a.r - b.r) + Mathf.Abs(a.g - b.g) + Mathf.Abs(a.b - b.b) < 0.08f;
        }

        /// <summary>
        /// A case number for today's report, like «پرونده‌ی شماره‌ی ۷۱۲»
        /// </summary>
        public static string CaseNumber
        {
            get { return ECCUIKit.PersianDigits(400 + System.DateTime.Now.DayOfYear); }
        }
    }

    /// <summary>
    /// Marks a dark element on a police report ( like the reward pill ): the report keeps its light colors
    /// </summary>
    public class ECCKeepColors : MonoBehaviour
    {
    }

    /// <summary>
    /// Flashes the two halves of a siren bar in turn. Works while paused
    /// </summary>
    public class ECCSirenFlash : MonoBehaviour
    {
        public Image red;
        public Image blue;
        public float period = 0.9f;

        void Update()
        {
            if (red == null || blue == null) return;

            bool redOn = Mathf.Repeat(Time.unscaledTime, period) < period * 0.5f;

            red.color = new Color(red.color.r, red.color.g, red.color.b, redOn ? 1 : 0.35f);
            blue.color = new Color(blue.color.r, blue.color.g, blue.color.b, redOn ? 0.35f : 1);
        }
    }

    /// <summary>
    /// The darker shade under a candy button. Follows the face color, so buttons recolored later keep a matching shade
    /// </summary>
    public class ECCCandyShade : MonoBehaviour
    {
        Image face;
        Shadow lip;
        Color lastColor = new Color(-1, -1, -1, -1);

        public void Init(Image faceImage, Shadow lipShadow)
        {
            face = faceImage;
            lip = lipShadow;
            Refresh();
        }

        void LateUpdate()
        {
            if (face && face.color != lastColor) Refresh();
        }

        void Refresh()
        {
            if (face == null || lip == null) return;

            lastColor = face.color;

            lip.effectColor = new Color(lastColor.r * 0.68f, lastColor.g * 0.68f, lastColor.b * 0.68f, Mathf.Max(0.6f, lastColor.a));
        }
    }

    /// <summary>
    /// Keeps a rect ( a card's drop shadow ) over another rect, with some padding ( left, top, right, bottom )
    /// </summary>
    public class ECCFollowRect : MonoBehaviour
    {
        public RectTransform target;
        public Vector4 padding;

        void LateUpdate()
        {
            if (target == null)
            {
                Destroy(gameObject);
                return;
            }

            RectTransform rect = (RectTransform)transform;

            rect.anchorMin = target.anchorMin;
            rect.anchorMax = target.anchorMax;
            rect.pivot = target.pivot;
            rect.sizeDelta = target.sizeDelta + new Vector2(padding.x + padding.z, padding.y + padding.w);
            rect.anchoredPosition = target.anchoredPosition + new Vector2(0, (padding.y - padding.w) * 0.5f);
            rect.localScale = target.localScale;

            gameObject.SetActive(target.gameObject.activeSelf);
        }
    }

    /// <summary>
    /// On a CrowdRush card ( ECCCarTheme.Card ), keeps the texts readable: white labels become dark ink, and the
    /// buttons become candy buttons. Runs after the screen was built, and again now and then for rows added later.
    /// Does nothing while the panel isn't a white card ( a screen that paints its panel keeps its own colors )
    /// </summary>
    public class ECCCardStyle : MonoBehaviour
    {
        float nextCheck = 0;

        void Start()
        {
            Apply();
        }

        void LateUpdate()
        {
            if (Time.unscaledTime < nextCheck) return;

            nextCheck = Time.unscaledTime + 0.3f;

            Apply();
        }

        void Apply()
        {
            RectTransform panel = (RectTransform)transform;

            if (ECCCarTheme.IsCard(panel) == false) return;

            ECCCarTheme.RecolorForCard(panel);

            foreach (Button button in panel.GetComponentsInChildren<Button>(true))
            {
                if (button.GetComponentInParent<ECCCoinBalanceButton>() != null || button.GetComponent<ECCCandyShade>()) continue;

                if (button.name == "ButtonClose") ECCCarTheme.CandyClose(button);
                else ECCCarTheme.Chunky(button);

                // Candy shade marks the button as done ( round buttons get no shade from Chunky )
                if (button.GetComponent<ECCCandyShade>() == null) button.gameObject.AddComponent<ECCCandyShade>();
            }
        }
    }
}
