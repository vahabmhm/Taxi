using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace EndlessCarChase
{
    /// <summary>
    /// Shared assets and small building blocks of the new UI: the coin icon, the number font, procedural sprites,
    /// safe-area fitting and the modal registry used to keep popups from stacking.
    /// </summary>
    public static class ECCUIKit
    {
        // Resources paths
        const string CoinIconPath = "ECCUI/CoinIcon";
        const string NumberFontPath = ECCRuntimeUI.PersianFontPath;

        static Sprite coinSprite;
        static Sprite whiteSprite;
        static Sprite circleSprite;
        static Font numberFont;
        static bool numberFontWarned = false;

        /// <summary>
        /// The game's money icon ( Textures/Cash.png, copied to Resources/ECCUI/CoinIcon.png )
        /// </summary>
        public static Sprite CoinSprite
        {
            get
            {
                if (coinSprite == null)
                {
                    Texture2D texture = Resources.Load<Texture2D>(CoinIconPath);

                    if (texture) coinSprite = Sprite.Create(texture, new Rect(0, 0, texture.width, texture.height), new Vector2(0.5f, 0.5f), 100);
                    else coinSprite = CircleSprite;
                }

                return coinSprite;
            }
        }

        /// <summary>
        /// The font of the coin numbers: the game's Vazirmatn font ( Assets/ECCAssets/Resources/Fonts/Vazirmatn.ttf ).
        /// Until it's imported the default UI font is used as a temporary fallback.
        /// </summary>
        public static Font NumberFont
        {
            get
            {
                if (numberFont == null)
                {
                    numberFont = Resources.Load<Font>(NumberFontPath);

                    if (numberFont == null)
                    {
                        if (numberFontWarned == false) Debug.LogWarning("Vazirmatn font not found at Resources/" + NumberFontPath + ". Using the default font as a temporary fallback.");

                        numberFontWarned = true;

                        return ECCRuntimeUI.DefaultFont;
                    }
                }

                return numberFont;
            }
        }

        public static Sprite WhiteSprite
        {
            get
            {
                if (whiteSprite == null)
                {
                    Texture2D texture = new Texture2D(4, 4, TextureFormat.RGBA32, false);

                    Color[] pixels = new Color[16];
                    for (int index = 0; index < pixels.Length; index++) pixels[index] = Color.white;

                    texture.SetPixels(pixels);
                    texture.Apply();

                    whiteSprite = Sprite.Create(texture, new Rect(0, 0, 4, 4), new Vector2(0.5f, 0.5f), 100);
                }

                return whiteSprite;
            }
        }

        /// <summary>
        /// A soft-edged white disc, used for round buttons, badges, the wheel and the loading ring
        /// </summary>
        public static Sprite CircleSprite
        {
            get
            {
                if (circleSprite == null)
                {
                    const int size = 128;

                    Texture2D texture = new Texture2D(size, size, TextureFormat.RGBA32, false);
                    texture.wrapMode = TextureWrapMode.Clamp;

                    Color[] pixels = new Color[size * size];
                    float radius = size * 0.5f;

                    for (int y = 0; y < size; y++)
                    {
                        for (int x = 0; x < size; x++)
                        {
                            float distance = Vector2.Distance(new Vector2(x + 0.5f, y + 0.5f), new Vector2(radius, radius));

                            pixels[y * size + x] = new Color(1, 1, 1, Mathf.Clamp01(radius - distance));
                        }
                    }

                    texture.SetPixels(pixels);
                    texture.Apply();

                    circleSprite = Sprite.Create(texture, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f), 100);
                }

                return circleSprite;
            }
        }

        public static Image CreateCoinIcon(string name, Transform parent, Vector2 anchor, Vector2 position, float size)
        {
            Image icon = ECCRuntimeUI.CreateImage(name, parent, anchor, position, new Vector2(size, size), Color.white);
            icon.sprite = CoinSprite;
            icon.preserveAspect = true;
            icon.raycastTarget = false;

            return icon;
        }

        public static Image CreateCircle(string name, Transform parent, Vector2 anchor, Vector2 position, float size, Color color)
        {
            Image circle = ECCRuntimeUI.CreateImage(name, parent, anchor, position, new Vector2(size, size), color);
            circle.sprite = CircleSprite;

            return circle;
        }

        /// <summary>
        /// A label for coin numbers, in the number font. Numbers are left-to-right, so no Persian shaping is needed
        /// </summary>
        public static Text CreateNumberText(string name, Transform parent, string content, int fontSize, Vector2 anchor, Vector2 position, Vector2 size, Color color)
        {
            Text text = ECCRuntimeUI.CreateText(name, parent, string.Empty, fontSize, anchor, position, size, color);
            text.font = NumberFont;
            text.text = content;

            return text;
        }

        /// <summary>
        /// Stretches a rect over the whole parent
        /// </summary>
        public static void Stretch(RectTransform rect)
        {
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.anchoredPosition = Vector2.zero;
            rect.sizeDelta = Vector2.zero;
        }

        /// <summary>
        /// A canvas for the new portrait screens: always 720 units wide in portrait ( so 680-wide popups fit every phone,
        /// from 4:3 tablets to 20:9 phones ), and at least 1000 units tall in landscape ( Editor game view )
        /// </summary>
        public static Canvas CreateCanvas(string name, int sortingOrder)
        {
            Canvas canvas = ECCRuntimeUI.CreateCanvas(name, sortingOrder);

            CanvasScaler scaler = canvas.GetComponent<CanvasScaler>();
            scaler.referenceResolution = new Vector2(720, 1000);
            scaler.matchWidthOrHeight = Screen.height >= Screen.width ? 0 : 1;

            return canvas;
        }

        /// <summary>
        /// Creates a canvas with a child rect that covers the screen's safe area ( notches, rounded corners )
        /// </summary>
        public static RectTransform CreateSafeCanvas(string name, int sortingOrder, out Canvas canvas)
        {
            canvas = CreateCanvas(name, sortingOrder);

            RectTransform safe = ECCRuntimeUI.CreateRect("SafeArea", canvas.transform, new Vector2(0.5f, 0.5f), Vector2.zero, Vector2.zero);
            Stretch(safe);
            safe.gameObject.AddComponent<ECCSafeArea>();

            return safe;
        }

        /// <summary>
        /// A modal popup: a dimmed full screen background that blocks the UI behind it, a centered panel and a registry entry,
        /// so other daily popups wait until it closes. Destroy the returned panel's parent to close it
        /// </summary>
        public static RectTransform CreatePopup(string name, int sortingOrder, Vector2 size)
        {
            Canvas canvas = CreateCanvas(name, sortingOrder);
            canvas.gameObject.AddComponent<ECCModalTracker>();

            RectTransform panel = ECCRuntimeUI.CreateModal(canvas.transform, size);

            // Pop-in animation
            panel.gameObject.AddComponent<ECCPopIn>();

            return panel;
        }

        /// <summary>
        /// Closes a popup created with CreatePopup ( from its panel or any child of it )
        /// </summary>
        public static void ClosePopup(Component anyChild)
        {
            if (anyChild == null) return;

            Canvas canvas = anyChild.GetComponentInParent<Canvas>();

            if (canvas) Object.Destroy(canvas.rootCanvas.gameObject);
        }

        /// <summary>
        /// A round close (X) button at the top right corner of a panel
        /// </summary>
        public static Button CreateCloseButton(RectTransform panel)
        {
            Image background = CreateCircle("ButtonClose", panel, new Vector2(1, 1), new Vector2(-8, -8), 64, ECCRuntimeUI.BadColor);
            background.rectTransform.pivot = new Vector2(1, 1);

            Button button = background.gameObject.AddComponent<Button>();
            button.targetGraphic = background;

            Text label = ECCRuntimeUI.CreateText("X", background.transform, string.Empty, 34, new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(64, 64), Color.white);
            label.text = "X";

            // CrowdRush's round red close button with an X icon, on the corner
            ECCCarTheme.CandyClose(button);

            return button;
        }

        /// <summary>
        /// Persian digits for numbers inside Persian sentences, like ۳۰
        /// </summary>
        public static string PersianDigits(int value)
        {
            char[] digits = value.ToString(System.Globalization.CultureInfo.InvariantCulture).ToCharArray();

            for (int index = 0; index < digits.Length; index++)
            {
                if (digits[index] >= '0' && digits[index] <= '9') digits[index] = (char)('۰' + (digits[index] - '0'));
            }

            return new string(digits);
        }

        /// <summary>
        /// Shows or hides a button and makes its disabled tint not hide its color
        /// </summary>
        public static void KeepColorWhenDisabled(Button button)
        {
            ColorBlock colors = button.colors;
            colors.disabledColor = new Color(1, 1, 1, 0.55f);
            button.colors = colors;
        }
    }

    /// <summary>
    /// Counts the open popups, so the daily popups ( challenge, login calendar, recommendation, review ) never stack
    /// </summary>
    public class ECCModalTracker : MonoBehaviour
    {
        static readonly List<ECCModalTracker> open = new List<ECCModalTracker>();

        public static bool AnyOpen
        {
            get
            {
                open.RemoveAll(tracker => tracker == null);

                return open.Count > 0;
            }
        }

        void OnEnable()
        {
            if (open.Contains(this) == false) open.Add(this);
        }

        void OnDisable()
        {
            open.Remove(this);
        }
    }

    /// <summary>
    /// Fits a rect to Screen.safeArea, and refits when the screen size or orientation changes
    /// </summary>
    public class ECCSafeArea : MonoBehaviour
    {
        Rect applied;
        Vector2Int screenSize;

        void OnEnable()
        {
            Apply();
        }

        void Update()
        {
            if (applied != Screen.safeArea || screenSize.x != Screen.width || screenSize.y != Screen.height) Apply();
        }

        void Apply()
        {
            RectTransform rect = (RectTransform)transform;

            Rect safe = Screen.safeArea;

            applied = safe;
            screenSize = new Vector2Int(Screen.width, Screen.height);

            if (Screen.width <= 0 || Screen.height <= 0) return;

            rect.anchorMin = new Vector2(safe.xMin / Screen.width, safe.yMin / Screen.height);
            rect.anchorMax = new Vector2(safe.xMax / Screen.width, safe.yMax / Screen.height);
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;
        }
    }

    /// <summary>
    /// A looping, gentle scale pulse ( localScale only, so the layout of neighbours doesn't move ). Works while paused
    /// </summary>
    public class ECCPulse : MonoBehaviour
    {
        public float amount = 0.08f;
        public float speed = 5;

        void OnDisable()
        {
            transform.localScale = Vector3.one;
        }

        void Update()
        {
            transform.localScale = Vector3.one * (1 + Mathf.Sin(Time.unscaledTime * speed) * amount);
        }

        /// <summary>
        /// Starts or stops the pulse on a target
        /// </summary>
        public static void Set(Component target, bool pulsing, float amount = 0.08f)
        {
            if (target == null) return;

            ECCPulse pulse = target.GetComponent<ECCPulse>();

            if (pulsing)
            {
                if (pulse == null) pulse = target.gameObject.AddComponent<ECCPulse>();

                pulse.amount = amount;
                pulse.enabled = true;
            }
            else if (pulse)
            {
                pulse.enabled = false;
            }
        }
    }

    /// <summary>
    /// Spins a rect at a constant speed ( loading indicators ). Works while paused
    /// </summary>
    public class ECCSpin : MonoBehaviour
    {
        public float degreesPerSecond = -300;

        void Update()
        {
            transform.Rotate(0, 0, degreesPerSecond * Time.unscaledDeltaTime);
        }
    }

    /// <summary>
    /// A short scale pop when a panel appears
    /// </summary>
    public class ECCPopIn : MonoBehaviour
    {
        // The scale the panel ends at ( a tall card is scaled down to fit the screen )
        public float restScale = 1;

        float time = 0;

        void OnEnable()
        {
            time = 0;
            transform.localScale = Vector3.one * 0.7f * restScale;
        }

        void Update()
        {
            time += Time.unscaledDeltaTime;

            float progress = Mathf.Clamp01(time / 0.22f);

            // Ease out with a small overshoot
            float scale = progress < 1 ? 0.7f + 0.38f * Mathf.Sin(progress * Mathf.PI * 0.62f) / Mathf.Sin(Mathf.PI * 0.62f) : 1;

            transform.localScale = Vector3.one * Mathf.Min(scale, 1.06f) * restScale;

            if (progress >= 1)
            {
                transform.localScale = Vector3.one * restScale;
                enabled = false;
            }
        }
    }
}
