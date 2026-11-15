using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace EndlessCarChase
{
    /// <summary>
    /// The one money balance button used on every screen that shows the player's money, in the look of CrowdRush's
    /// balance pill: a dark rounded pill with a gold outline, a big dollar coin on its right edge ( a little larger than
    /// the pill ), the formatted balance in heavy white digits beside it, and a round green (+) half outside the left edge.
    /// Tapping the pill or the (+) opens the coin gift popup. The number updates from ECCWallet.BalanceChanged ( no
    /// per-frame polling, no copy of the balance ); gains count up and make the dollar bounce.
    /// Usage: ECCCoinBalanceButton.Create(parent, anchor, position);
    /// </summary>
    public class ECCCoinBalanceButton : MonoBehaviour
    {
        public static readonly Vector2 Size = new Vector2(220, 66);
        const float PlusSize = 58;
        const float DollarSize = 74;
        const string DollarIconPath = "ECCUI/DollarIcon";

        static readonly Color PillColor = new Color(0.09f, 0.1f, 0.18f, 0.88f);
        static readonly Color Gold = ECCCarTheme.Hex(0xFFC83D);

        static Sprite dollarSprite;

        Text balanceText;
        RectTransform dollar;
        int shownBalance = -1;
        int targetBalance = 0;
        float countFrom = 0;
        float countTime = 0;
        bool counting = false;
        float punch = 0;

        /// <summary>
        /// The dollar coin of the balance button ( Resources/ECCUI/DollarIcon.png ), the game's coin icon if it's missing
        /// </summary>
        public static Sprite DollarSprite
        {
            get
            {
                if (dollarSprite == null)
                {
                    Texture2D texture = Resources.Load<Texture2D>(DollarIconPath);

                    dollarSprite = texture ? Sprite.Create(texture, new Rect(0, 0, texture.width, texture.height), new Vector2(0.5f, 0.5f), 100) : ECCUIKit.CoinSprite;
                }

                return dollarSprite;
            }
        }

        // The balance buttons on screen, the last one shown first
        static readonly System.Collections.Generic.List<ECCCoinBalanceButton> shown = new System.Collections.Generic.List<ECCCoinBalanceButton>();

        /// <summary>
        /// The dollar of the balance button on screen ( the one shown last ), where collected money flies to. Null if none
        /// </summary>
        public static RectTransform FlyTarget
        {
            get
            {
                for (int index = shown.Count - 1; index >= 0; index--)
                {
                    if (shown[index] && shown[index].dollar && shown[index].isActiveAndEnabled) return shown[index].dollar;
                }

                return null;
            }
        }

        public static ECCCoinBalanceButton Create(Transform parent, Vector2 anchor, Vector2 anchoredPosition)
        {
            RectTransform root = ECCRuntimeUI.CreateRect("CoinBalanceButton", parent, anchor, anchoredPosition, Size);

            ECCCoinBalanceButton balanceButton = root.gameObject.AddComponent<ECCCoinBalanceButton>();
            balanceButton.Build(root);

            return balanceButton;
        }

        void Build(RectTransform root)
        {
            // Rounded ends: the card sprite's 48 pixel border becomes half the pill's height
            float roundness = 48 / (Size.y * 0.5f);

            Image shade = CreatePillImage("Shade", root, "panel", new Color(0, 0, 0, 0.35f), roundness);
            shade.rectTransform.offsetMin = new Vector2(0, -5);
            shade.rectTransform.offsetMax = new Vector2(0, -5);

            Image pill = CreatePillImage("Pill", root, "panel", PillColor, roundness);
            pill.raycastTarget = true;

            CreatePillImage("Edge", root, "panel_outline", Gold, roundness);

            Button pillButton = root.gameObject.AddComponent<Button>();
            pillButton.targetGraphic = pill;
            pillButton.transition = Selectable.Transition.None;
            pillButton.onClick.AddListener(OpenGift);
            root.gameObject.AddComponent<ECCPressScale>();

            // The dollar on the right edge, a little larger than the pill
            Image dollarImage = ECCRuntimeUI.CreateImage("Dollar", root, new Vector2(1, 0.5f), new Vector2(-22, 0), new Vector2(DollarSize, DollarSize), Color.white);
            dollarImage.rectTransform.pivot = new Vector2(0.5f, 0.5f);
            dollarImage.sprite = DollarSprite;
            dollarImage.preserveAspect = true;
            dollarImage.raycastTarget = false;
            dollar = dollarImage.rectTransform;

            // The balance right beside the dollar
            balanceText = ECCRuntimeUI.CreateText("Balance", root, string.Empty, 32, new Vector2(0.5f, 0.5f), Vector2.zero, Vector2.zero, Color.white);
            balanceText.font = ECCCarTheme.TitleFont;
            balanceText.alignment = TextAnchor.MiddleRight;
            balanceText.rectTransform.anchorMin = Vector2.zero;
            balanceText.rectTransform.anchorMax = Vector2.one;
            balanceText.rectTransform.pivot = new Vector2(0.5f, 0.5f);
            balanceText.rectTransform.offsetMin = new Vector2(36, 0);
            balanceText.rectTransform.offsetMax = new Vector2(-64, 3);
            balanceText.horizontalOverflow = HorizontalWrapMode.Wrap;
            balanceText.verticalOverflow = VerticalWrapMode.Truncate;
            balanceText.resizeTextForBestFit = true;
            balanceText.resizeTextMinSize = 16;
            balanceText.resizeTextMaxSize = 32;
            ECCCarTheme.CandyLabel(balanceText);
            balanceText.font = ECCCarTheme.TitleFont;

            foreach (Shadow shadow in balanceText.GetComponents<Shadow>())
            {
                if (shadow.GetType() == typeof(Shadow)) shadow.effectColor = new Color(0, 0, 0, 0.45f);
            }

            // The (+) is centered on the pill's left edge: half inside, half outside. It's the last child so it's drawn
            // and hit-tested on top. It stays on the left in the RTL layout on purpose
            RectTransform plus = ECCRuntimeUI.CreateRect("ButtonPlus", root, new Vector2(0, 0.5f), Vector2.zero, new Vector2(PlusSize, PlusSize));
            plus.pivot = new Vector2(0.5f, 0.5f);

            Image plusShade = ECCUIKit.CreateCircle("Shade", plus, new Vector2(0.5f, 0.5f), new Vector2(0, -4), PlusSize, ECCCarTheme.CandyGreenShade);
            plusShade.raycastTarget = false;

            Image plusFace = ECCUIKit.CreateCircle("Face", plus, new Vector2(0.5f, 0.5f), Vector2.zero, PlusSize, ECCCarTheme.CandyGreen);

            ECCCarTheme.CreateIcon("Ring", plus, "Icons/ring", new Vector2(0.5f, 0.5f), Vector2.zero, PlusSize + 4, Color.white);

            Image plusIcon = ECCCarTheme.CreateIcon("Icon", plus, "Icons/plus", new Vector2(0.5f, 0.5f), Vector2.zero, PlusSize - 28, Color.white);
            ECCPulse pulse = plusIcon.gameObject.AddComponent<ECCPulse>();
            pulse.amount = 0.12f;
            pulse.speed = 4.5f;

            Button plusButton = plus.gameObject.AddComponent<Button>();
            plusButton.targetGraphic = plusFace;
            plusButton.transition = Selectable.Transition.None;
            plusButton.onClick.AddListener(OpenGift);
            plus.gameObject.AddComponent<ECCPressScale>();

            plus.SetAsLastSibling();

            SetBalance(ECCWallet.Balance);
        }

        static Image CreatePillImage(string name, RectTransform root, string sprite, Color color, float roundness)
        {
            Image image = ECCRuntimeUI.CreateImage(name, root, new Vector2(0.5f, 0.5f), Vector2.zero, Vector2.zero, color);
            ECCUIKit.Stretch(image.rectTransform);
            image.sprite = ECCCarTheme.CardSprite(sprite);
            image.type = Image.Type.Sliced;
            image.pixelsPerUnitMultiplier = roundness;
            image.raycastTarget = false;

            return image;
        }

        void OnEnable()
        {
            ECCWallet.BalanceChanged += Refresh;

            shown.Remove(this);
            shown.Add(this);

            // The balance may have changed while this screen was hidden
            if (balanceText) SetBalance(ECCWallet.Balance);
        }

        void OnDisable()
        {
            ECCWallet.BalanceChanged -= Refresh;

            shown.Remove(this);

            punch = 0;
            if (dollar) dollar.localScale = Vector3.one;
        }

        void Refresh(int balance)
        {
            if (balanceText == null) return;

            // Gains count up after a short delay ( while the reward effect plays ) and the dollar bounces
            if (shownBalance >= 0 && balance > shownBalance && isActiveAndEnabled)
            {
                countFrom = shownBalance;
                targetBalance = balance;
                countTime = -0.35f;
                counting = true;
                punch = 1;

                return;
            }

            SetBalance(balance);
        }

        void SetBalance(int balance)
        {
            counting = false;
            targetBalance = shownBalance = balance;
            balanceText.text = ECCWallet.Format(balance);
        }

        void Update()
        {
            if (counting)
            {
                countTime += Time.unscaledDeltaTime;

                if (countTime >= 0)
                {
                    float progress = Mathf.Clamp01(countTime / 0.8f);

                    // Ease out
                    float eased = 1 - Mathf.Pow(1 - progress, 3);

                    int value = Mathf.RoundToInt(Mathf.Lerp(countFrom, targetBalance, eased));

                    if (value != shownBalance)
                    {
                        shownBalance = value;
                        balanceText.text = ECCWallet.Format(value);
                    }

                    if (progress >= 1) SetBalance(targetBalance);
                }
            }

            if (punch > 0 && dollar)
            {
                punch = Mathf.Max(0, punch - Time.unscaledDeltaTime * 1.6f);

                // A short wobbling scale punch, like DOTween's DOPunchScale
                float elapsed = 1 - punch;
                dollar.localScale = Vector3.one * (1 + Mathf.Sin(elapsed * Mathf.PI * 6) * 0.3f * punch);

                if (punch <= 0) dollar.localScale = Vector3.one;
            }
        }

        void OpenGift()
        {
            ECCCoinGiftPopup.Open();
        }
    }

    /// <summary>
    /// Shrinks a button a little while it's held ( around the scale it was placed with )
    /// </summary>
    public class ECCPressScale : MonoBehaviour, IPointerDownHandler, IPointerUpHandler
    {
        Vector3 restScale;
        bool pressed = false;

        public void OnPointerDown(PointerEventData eventData)
        {
            if (pressed) return;

            pressed = true;
            restScale = transform.localScale;
            transform.localScale = restScale * 0.94f;
        }

        public void OnPointerUp(PointerEventData eventData)
        {
            Release();
        }

        void OnDisable()
        {
            Release();
        }

        void Release()
        {
            if (pressed == false) return;

            pressed = false;
            transform.localScale = restScale;
        }
    }
}
