using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace EndlessCarChase
{
    /// <summary>
    /// The in-chase power-up buttons ( a column on the right edge ), in the style of CrowdRush's power-up bar:
    /// big colored round buttons with an icon, a ring that empties while a timed effect runs, and a tag under each button
    /// that tells how the next use is paid: «۱ رایگان» ( green ), its coin price ( dark ) or «ویدئو» ( purple ).
    /// A free use is applied right away. Otherwise ECCPowerUpPopup asks first ( coins, or a rewarded video when coins are
    /// short ), and the chase is paused while it's open, so the decision costs no game time.
    /// A power-up that can't do anything now ( still running, car fully repaired ) only shows a short message.
    /// </summary>
    public class ECCPowerUpBar : MonoBehaviour
    {
        [Tooltip("Position of the top button, from the right middle of the safe area")]
        public Vector2 barPosition = new Vector2(-20, 170);

        public float buttonSize = 92;

        [Tooltip("Size of the money balance in the top right corner during the chase")]
        public float balanceScale = 1.15f;
        public float spacing = 124;

        static readonly Color FreeColor = new Color(0.2f, 0.66f, 0.3f, 1);
        static readonly Color PriceColor = new Color(0.08f, 0.09f, 0.14f, 0.9f);
        static readonly Color VideoColor = new Color(0.56f, 0.36f, 0.97f, 1);

        class Slot
        {
            public ECCPowerUpType type;
            public RectTransform root;
            public Button button;
            public CanvasGroup group;
            public Image ring;
            public Image tag;
            public Text tagText;
            public Image tagIcon;
            public bool wasRunning;

            // The glowing halo and pulse that point the player at this button ( Highlight )
            public Image halo;
            public float highlightUntil;
        }

        ECCGameController gameController;
        ECCPowerUpManager manager;
        Canvas canvas;
        readonly List<Slot> slots = new List<Slot>();
        readonly Dictionary<ECCPowerUpType, float> lastTap = new Dictionary<ECCPowerUpType, float>();

        bool pausedForPopup = false;
        float empCooldown = 0;

        static ECCPowerUpBar instance;

        public static ECCPowerUpBar Create(ECCGameController gameController)
        {
            ECCPowerUpBar bar = gameController.GetComponent<ECCPowerUpBar>();

            if (bar == null) bar = gameController.gameObject.AddComponent<ECCPowerUpBar>();

            bar.gameController = gameController;

            return bar;
        }

        void Start()
        {
            instance = this;

            if (gameController == null) gameController = GetComponent<ECCGameController>();

            manager = GetComponent<ECCPowerUpManager>();

            ECCPowerUpInventory.ResetRun();
            ECCRunMessages.ResetRun();

            Build();

            ECCPowerUpInventory.Changed += Refresh;
            ECCWallet.BalanceChanged += OnBalanceChanged;
        }

        void OnDestroy()
        {
            ECCPowerUpInventory.Changed -= Refresh;
            ECCWallet.BalanceChanged -= OnBalanceChanged;

            if (instance == this) instance = null;
        }

        void Build()
        {
            RectTransform safe = ECCUIKit.CreateSafeCanvas("CanvasPowerUpBar", 52, out canvas);

            Vector2 rightMiddle = new Vector2(1, 0.5f);

            // The shared money balance in the top right corner, a little bigger than on the menus
            ECCCoinBalanceButton coins = ECCCoinBalanceButton.Create(safe, new Vector2(1, 1), new Vector2(-28, -28));
            coins.transform.localScale = Vector3.one * balanceScale;

            for (int index = 0; index < ECCPowerUpInventory.Types.Length; index++)
            {
                slots.Add(CreateSlot(safe, ECCPowerUpInventory.Types[index], barPosition + new Vector2(0, -index * spacing)));
            }

            Refresh();
        }

        Slot CreateSlot(RectTransform parent, ECCPowerUpType type, Vector2 position)
        {
            Slot slot = new Slot { type = type };

            Color color = ECCPowerUpManager.GetColor(type);
            Vector2 center = new Vector2(0.5f, 0.5f);

            slot.root = ECCRuntimeUI.CreateRect("PowerUp" + type, parent, new Vector2(1, 0.5f), position, new Vector2(buttonSize, buttonSize));
            slot.group = slot.root.gameObject.AddComponent<CanvasGroup>();

            // The halo behind everything, shown while the button is highlighted
            slot.halo = ECCUIKit.CreateCircle("Halo", slot.root, center, Vector2.zero, buttonSize + 46, new Color(color.r, color.g, color.b, 0));
            slot.halo.raycastTarget = false;
            slot.halo.gameObject.SetActive(false);

            // A darker disc under the face, a face, a soft shine on its top half, then the icon
            Image shade = ECCUIKit.CreateCircle("Shade", slot.root, center, new Vector2(0, -7), buttonSize, Shade(color, 0.55f));
            shade.raycastTarget = false;

            Image face = ECCUIKit.CreateCircle("Face", slot.root, center, Vector2.zero, buttonSize, color);

            Image shine = ECCUIKit.CreateCircle("Shine", face.transform, new Vector2(0.5f, 1), new Vector2(0, -buttonSize * 0.28f), 0, new Color(1, 1, 1, 0.2f));
            shine.rectTransform.sizeDelta = new Vector2(buttonSize * 0.72f, buttonSize * 0.4f);
            shine.raycastTarget = false;

            Image icon = ECCCarTheme.CreateIcon("Icon", face.transform, ECCPowerUpManager.GetIcon(type), center, new Vector2(0, 2), buttonSize * 0.58f, Color.white);
            Shadow iconShadow = icon.gameObject.AddComponent<Shadow>();
            iconShadow.effectColor = new Color(0, 0, 0, 0.25f);
            iconShadow.effectDistance = new Vector2(0, -3);

            // The ring that empties while a timed effect runs
            slot.ring = ECCRuntimeUI.CreateImage("Ring", slot.root, center, Vector2.zero, new Vector2(buttonSize + 14, buttonSize + 14), Color.white);
            slot.ring.sprite = ECCCarTheme.Icon("Icons/ring");
            slot.ring.type = Image.Type.Filled;
            slot.ring.fillMethod = Image.FillMethod.Radial360;
            slot.ring.fillOrigin = (int)Image.Origin360.Top;
            slot.ring.raycastTarget = false;
            slot.ring.gameObject.SetActive(false);

            // The tag under the button: free uses, price or video
            slot.tag = ECCCarTheme.CreatePanel("Tag", slot.root, new Vector2(0.5f, 0), new Vector2(0, -20), new Vector2(118, 42), PriceColor);
            slot.tagText = ECCRuntimeUI.CreateText("Text", slot.tag.transform, string.Empty, 22, center, Vector2.zero, new Vector2(118, 42), Color.white);
            slot.tagIcon = ECCRuntimeUI.CreateImage("Icon", slot.tag.transform, new Vector2(1, 0.5f), new Vector2(-6, 0), new Vector2(32, 32), Color.white);
            slot.tagIcon.rectTransform.pivot = new Vector2(1, 0.5f);
            slot.tagIcon.preserveAspect = true;
            slot.tagIcon.raycastTarget = false;

            slot.button = face.gameObject.AddComponent<Button>();
            slot.button.targetGraphic = face;
            slot.button.transition = Selectable.Transition.None;
            slot.button.onClick.AddListener(delegate { OnTap(slot); });

            return slot;
        }

        static Color Shade(Color color, float amount)
        {
            return new Color(color.r * amount, color.g * amount, color.b * amount, 1);
        }

        void OnBalanceChanged(int balance)
        {
            Refresh();
        }

        void Refresh()
        {
            foreach (Slot slot in slots)
            {
                ECCPowerUpInventory.PayMethod method = ECCPowerUpInventory.GetPayMethod(slot.type);

                RectTransform text = slot.tagText.rectTransform;

                if (method == ECCPowerUpInventory.PayMethod.Free)
                {
                    slot.tag.color = FreeColor;
                    slot.tagIcon.gameObject.SetActive(false);
                    text.anchoredPosition = Vector2.zero;

                    ECCRuntimeUI.SetText(slot.tagText, ECCPowerUpInventory.HasPassUse(slot.type) ? "رایگان" : ECCUIKit.PersianDigits(ECCPowerUpInventory.GetFreeUses(slot.type)) + " رایگان");
                }
                else if (method == ECCPowerUpInventory.PayMethod.Coins)
                {
                    slot.tag.color = PriceColor;
                    slot.tagIcon.gameObject.SetActive(true);
                    slot.tagIcon.sprite = ECCUIKit.CoinSprite;
                    text.anchoredPosition = new Vector2(-16, 0);

                    slot.tagText.font = ECCUIKit.NumberFont;
                    ECCRuntimeUI.SetText(slot.tagText, ECCWallet.Format(ECCPowerUpInventory.GetPrice(slot.type)));
                }
                else
                {
                    slot.tag.color = VideoColor;
                    slot.tagIcon.gameObject.SetActive(true);
                    slot.tagIcon.sprite = ECCCarTheme.Icon("Icons/play");
                    text.anchoredPosition = new Vector2(-14, 0);

                    ECCRuntimeUI.SetText(slot.tagText, "ویدئو");
                }

                if (method != ECCPowerUpInventory.PayMethod.Coins) slot.tagText.font = ECCRuntimeUI.DefaultFont;
            }
        }

        bool IsPlaying
        {
            get { return gameController && gameController.gameStarted && gameController.isGameOver == false && gameController.startDelay <= 0 && gameController.playerObject && gameController.isPaused == false; }
        }

        void Update()
        {
            if (canvas == null || gameController == null) return;

            // Opening the coin gift popup from the bar pauses the chase until it closes
            if (ECCCoinGiftPopup.IsOpen && pausedForPopup == false && IsPlaying)
            {
                pausedForPopup = true;
                gameController.Pause(false);
            }
            else if (pausedForPopup && ECCCoinGiftPopup.IsOpen == false && ECCPowerUpPopup.IsOpen == false && ECCAds.IsBusy == false)
            {
                pausedForPopup = false;
                gameController.Unpause();
            }

            bool visible = pausedForPopup || (IsPlaying && gameController.gameCanvas && gameController.gameCanvas.gameObject.activeInHierarchy);

            if (canvas.enabled != visible) canvas.enabled = visible;

            UpdateHighlights();

            if (visible == false) return;

            if (empCooldown > 0) empCooldown -= Time.deltaTime;

            // Only the rings animate; the tags change on events
            foreach (Slot slot in slots)
            {
                float left = manager ? manager.GetTimeLeft(slot.type) : 0;
                float duration = manager ? manager.GetDuration(slot.type) : 0;

                bool running = left > 0;

                if (running != slot.wasRunning)
                {
                    slot.wasRunning = running;
                    slot.ring.gameObject.SetActive(running);
                    slot.group.alpha = running ? 0.6f : 1;
                }

                if (running) slot.ring.fillAmount = Mathf.Clamp01(left / Mathf.Max(0.1f, duration));
            }
        }

        /// <summary>
        /// Points the player at power-up buttons for a while: the buttons pulse with a glowing halo behind them.
        /// Ends early when one of them is tapped. Used by the in-run guidance ( ECCRunMessages )
        /// </summary>
        public static void Highlight(float seconds, params ECCPowerUpType[] types)
        {
            if (instance == null) return;

            foreach (Slot slot in instance.slots)
            {
                if (Array.IndexOf(types, slot.type) < 0) continue;

                slot.highlightUntil = Time.unscaledTime + seconds;
                slot.halo.gameObject.SetActive(true);

                ECCPulse.Set(slot.root, true, 0.12f);
            }
        }

        /// <summary>
        /// True if the bar is on screen and has a button for this power-up
        /// </summary>
        public static bool HasButton(ECCPowerUpType type)
        {
            return instance != null && instance.slots.Exists(slot => slot.type == type);
        }

        void UpdateHighlights()
        {
            foreach (Slot slot in slots)
            {
                if (slot.highlightUntil <= 0) continue;

                if (Time.unscaledTime >= slot.highlightUntil)
                {
                    StopHighlight(slot);
                    continue;
                }

                // The halo breathes with the pulse
                Color color = slot.halo.color;
                color.a = 0.25f + 0.3f * (0.5f + 0.5f * Mathf.Sin(Time.unscaledTime * 5));
                slot.halo.color = color;
            }
        }

        void StopHighlight(Slot slot)
        {
            slot.highlightUntil = 0;
            slot.halo.gameObject.SetActive(false);

            ECCPulse.Set(slot.root, false);
        }

        void OnTap(Slot slot)
        {
            ECCPowerUpType type = slot.type;

            if (slot.highlightUntil > 0) StopHighlight(slot);

            // One activation per burst of taps
            float tapTime;

            if (lastTap.TryGetValue(type, out tapTime) && Time.unscaledTime - tapTime < 0.35f) return;

            lastTap[type] = Time.unscaledTime;

            if (IsPlaying == false || manager == null || ECCPowerUpPopup.IsOpen || ECCAds.IsBusy) return;

            string reason;

            if (CanUse(type, out reason) == false)
            {
                ECCHintBanner.Show(ECCPowerUpManager.GetIcon(type), ECCRuntimeUI.AccentColor, reason, string.Empty, 1.6f, ECCHintBanner.Kind.Urgent);
                return;
            }

            ECCPowerUpInventory.PayMethod method = ECCPowerUpInventory.GetPayMethod(type);

            if (method == ECCPowerUpInventory.PayMethod.Free)
            {
                if (ECCPowerUpInventory.TryUseFreeOrCoins(type, delegate { return Apply(type); })) Punch(type);

                return;
            }

            // The explanation popup is shown the first two times; after that coins are paid right away
            if (method == ECCPowerUpInventory.PayMethod.Coins && ECCPowerUpInventory.ShouldShowHelp(type) == false)
            {
                int price = ECCPowerUpInventory.GetPrice(type);

                if (ECCPowerUpInventory.TryUseFreeOrCoins(type, delegate { return Apply(type); }))
                {
                    Punch(type);
                    // The power-up's own toast, with what it cost underneath
                    ECCHintBanner.Show(ECCPowerUpManager.GetIcon(type), ECCPowerUpManager.GetColor(type), ECCPowerUpManager.GetName(type) + "!", ECCUIKit.PersianDigits(price) + " سکه خرج شد", 1.6f, ECCHintBanner.Kind.Urgent);
                }

                return;
            }

            ECCPowerUpInventory.MarkHelpShown(type);

            // Coins or a video: ask first, with the chase paused
            pausedForPopup = true;
            gameController.Pause(false);

            ECCPowerUpPopup.Open(type);
        }

        bool CanUse(ECCPowerUpType type, out string reason)
        {
            if (manager.CanActivate(type, out reason) == false) return false;

            if (type == ECCPowerUpType.EMP && empCooldown > 0)
            {
                reason = "شوک الکتریکی چند لحظه‌ی دیگر آماده است";
                return false;
            }

            return true;
        }

        /// <summary>
        /// Called by the popup after the coins were accepted or the video was watched. The chase is resumed first, so
        /// the effect starts in a running game
        /// </summary>
        public static bool ApplyFromPopup(ECCPowerUpType type, bool payWithCoins)
        {
            if (instance == null) return false;

            instance.ResumeAfterPopup();

            string reason;

            if (instance.manager == null || instance.CanUse(type, out reason) == false) return false;

            bool applied = payWithCoins ? ECCPowerUpInventory.TryUseFreeOrCoins(type, delegate { return instance.Apply(type); }) : instance.Apply(type);

            if (applied) instance.Punch(type);

            return applied;
        }

        /// <summary>
        /// Resumes the chase when the popup closes without a power-up
        /// </summary>
        public static void OnPopupClosed()
        {
            if (instance) instance.ResumeAfterPopup();
        }

        void ResumeAfterPopup()
        {
            if (pausedForPopup == false) return;

            pausedForPopup = false;

            if (gameController && gameController.isGameOver == false) gameController.Unpause();
        }

        bool Apply(ECCPowerUpType type)
        {
            bool applied = manager && manager.Activate(type);

            if (applied && type == ECCPowerUpType.EMP) empCooldown = 1.5f;

            return applied;
        }

        void Punch(ECCPowerUpType type)
        {
            foreach (Slot slot in slots)
            {
                if (slot.type == type) ECCPunchScale.Play(slot.root);
            }
        }
    }

    /// <summary>
    /// A quick scale punch, like DOTween's DOPunchScale. Works while paused
    /// </summary>
    public class ECCPunchScale : MonoBehaviour
    {
        public float amount = 0.2f;
        public float duration = 0.3f;

        float time;

        public static void Play(Component target)
        {
            ECCPunchScale punch = target.GetComponent<ECCPunchScale>();

            if (punch == null) punch = target.gameObject.AddComponent<ECCPunchScale>();

            punch.time = 0;
            punch.enabled = true;
        }

        void Update()
        {
            time += Time.unscaledDeltaTime;

            float progress = Mathf.Clamp01(time / duration);

            // A decaying wobble around 1
            transform.localScale = Vector3.one * (1 + amount * Mathf.Sin(progress * Mathf.PI * 3) * (1 - progress));

            if (progress >= 1)
            {
                transform.localScale = Vector3.one;
                enabled = false;
            }
        }
    }
}
