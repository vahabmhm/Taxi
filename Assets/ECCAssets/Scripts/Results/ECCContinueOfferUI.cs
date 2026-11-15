using System;
using UnityEngine;
using UnityEngine.UI;

namespace EndlessCarChase
{
    /// <summary>
    /// The shared "continue?" loss screen shown while the run is frozen ( «ماشینت نابود شد!», «وقت تمام شد!» ), on the
    /// shared result card ( ECCResultCard ): a countdown badge, a big pulsing rewarded-video button, an optional coin
    /// option and a decline button ( the countdown waits while a video is loading or the NoAds panel is open ). The continuation is applied by the caller, only after a verified rewarded video or a successful payment.
    /// </summary>
    public class ECCContinueOfferUI : MonoBehaviour
    {
        public class Options
        {
            public string title;
            public string reason;
            public string videoLabel;
            public string videoTag;
            public string placement = "continue";
            public int coinPrice = 0;          // 0: no coin option
            public string coinLabel;
            public float decisionTime = 8;
            public Action onVideoRewarded;
            public Action onPaid;
            public Action onDeclined;
        }

        static ECCContinueOfferUI open;

        Options options;
        Button videoButton;
        Button payButton;
        Button declineButton;
        Text countdownText;
        Image countdownRing;
        float countdown;
        bool waitingForAd = false;
        bool resolved = false;

        public static bool IsOpen
        {
            get { return open != null; }
        }

        public static void Show(Options options)
        {
            if (open || options == null) return;

            ECCResultCard card = ECCResultCard.Create("CanvasContinueOffer", 85, "هنوز فرصت داری", ECCCarTheme.CandyRed);

            open = card.Panel.gameObject.AddComponent<ECCContinueOfferUI>();
            open.options = options;
            open.Build(card);

            card.Finish();
        }

        /// <summary>
        /// The countdown badge ( a ring that empties around the seconds left ), the title and reason, the big pulsing
        /// video button with its tag, the coin option and a small decline button, stacked so they never overlap
        /// </summary>
        void Build(ECCResultCard card)
        {
            bool timed = options.decisionTime > 0;

            Image badge = card.Badge(timed ? null : "Icons/clock", ECCCarTheme.CandyRed, false, 124);

            if (timed)
            {
                Vector2 center = new Vector2(0.5f, 0.5f);

                countdownRing = ECCRuntimeUI.CreateImage("CountdownRing", badge.transform, center, Vector2.zero, new Vector2(108, 108), ECCCarTheme.Hex(0xFFC83D));
                countdownRing.rectTransform.pivot = center;
                countdownRing.sprite = ECCCarTheme.Icon("Icons/ring");
                countdownRing.type = Image.Type.Filled;
                countdownRing.fillMethod = Image.FillMethod.Radial360;
                countdownRing.fillOrigin = (int)Image.Origin360.Top;
                countdownRing.raycastTarget = false;

                countdownText = ECCRuntimeUI.CreateText("Seconds", badge.transform, string.Empty, 54, center, new Vector2(0, 2), new Vector2(100, 80), Color.white);
                ECCCarTheme.CandyLabel(countdownText);
                countdownText.font = ECCCarTheme.TitleFont;
            }

            card.Title(options.title, ECCCarTheme.CandyRed, 44);

            if (string.IsNullOrEmpty(options.reason) == false) card.Line(options.reason, 23, 36);

            card.Space(6);

            // The main call to action: watch a video to continue
            videoButton = card.WideButton("ButtonContinueVideo", options.videoLabel, ECCCarTheme.CandyGreen, "Icons/play", 112, 28);
            videoButton.onClick.AddListener(WatchVideo);
            ECCPulse.Set(videoButton, true, 0.05f);

            if (string.IsNullOrEmpty(options.videoTag) == false)
            {
                Image tag = ECCRuntimeUI.CreateImage("Tag", videoButton.transform, new Vector2(0, 1), new Vector2(18, 16), new Vector2(150, 40), ECCCarTheme.CandyOrange);
                tag.rectTransform.pivot = new Vector2(0, 0.5f);
                ECCCarTheme.Round(tag);
                tag.pixelsPerUnitMultiplier = 48 / 20f;
                tag.raycastTarget = false;

                Text tagText = ECCRuntimeUI.CreateText("TagText", tag.transform, options.videoTag, 20, new Vector2(0.5f, 0.5f), new Vector2(0, 1), new Vector2(140, 36), Color.white);
                ECCCarTheme.CandyLabel(tagText);
                tagText.resizeTextForBestFit = true;
                tagText.resizeTextMinSize = 12;
                tagText.resizeTextMaxSize = 20;
            }

            if (options.coinPrice > 0)
            {
                payButton = card.WideButton("ButtonContinuePay", options.coinLabel + " " + ECCWallet.Format(options.coinPrice) + " دلار", ECCCarTheme.CandyOrange, null, 84, 28);

                Image dollar = ECCRuntimeUI.CreateImage("Dollar", payButton.transform, new Vector2(1, 0.5f), new Vector2(-18, 3), new Vector2(60, 60), Color.white);
                dollar.rectTransform.pivot = new Vector2(1, 0.5f);
                dollar.sprite = ECCCoinBalanceButton.DollarSprite;
                dollar.preserveAspect = true;
                dollar.raycastTarget = false;

                payButton.interactable = ECCWallet.CanAfford(options.coinPrice);
                payButton.onClick.AddListener(Pay);
            }

            declineButton = card.WideButton("ButtonDecline", "نه، تمام", ECCCarTheme.CandyGray, null, 66, 24);
            declineButton.onClick.AddListener(Decline);

            countdown = options.decisionTime;
        }

        void Update()
        {
            if (resolved || options.decisionTime <= 0) return;

            // The countdown waits while a video is loading or playing, or the NoAds panel is open
            if (waitingForAd || ECCAds.IsBusy || ECCNoAdsPanel.IsOpen) return;

            countdown -= Time.unscaledDeltaTime;

            if (countdownText) countdownText.text = ECCUIKit.PersianDigits(Mathf.CeilToInt(Mathf.Max(0, countdown)));
            if (countdownRing) countdownRing.fillAmount = Mathf.Clamp01(countdown / options.decisionTime);

            if (countdown <= 0) Decline();
        }

        void SetButtons(bool interactable)
        {
            videoButton.interactable = interactable;
            declineButton.interactable = interactable;

            if (payButton) payButton.interactable = interactable && ECCWallet.CanAfford(options.coinPrice);
        }

        void WatchVideo()
        {
            if (waitingForAd || resolved) return;

            waitingForAd = true;
            SetButtons(false);

            if (ECCAds.ShowRewarded(options.placement, OnVideoFinished) == false) OnVideoFinished(false);
        }

        void OnVideoFinished(bool rewarded)
        {
            waitingForAd = false;

            if (this == null || resolved) return;

            if (rewarded)
            {
                Resolve(options.onVideoRewarded);
                return;
            }

            // Nothing granted; the player can try again, pay, or decline. Give them a few more seconds to choose
            countdown = Mathf.Max(countdown, 4);
            SetButtons(true);
        }

        void Pay()
        {
            if (waitingForAd || resolved || options.coinPrice <= 0) return;

            if (ECCWallet.TrySpend(options.coinPrice) == false)
            {
                SetButtons(true);
                return;
            }

            Resolve(options.onPaid);
        }

        void Decline()
        {
            if (waitingForAd || resolved) return;

            Resolve(options.onDeclined);
        }

        void Resolve(Action action)
        {
            resolved = true;

            if (open == this) open = null;

            ECCUIKit.ClosePopup(this);

            if (action != null) action();
        }

        void OnDestroy()
        {
            if (open == this) open = null;
        }
    }
}
