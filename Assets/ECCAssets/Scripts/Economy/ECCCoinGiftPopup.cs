using System;
using UnityEngine;
using UnityEngine.UI;

namespace EndlessCarChase
{
    /// <summary>
    /// "Watch a video, get free coins". Opened by every coin balance button.
    /// The offered amount is fixed when the popup opens and is exactly what's paid, only after a verified rewarded completion.
    /// </summary>
    public class ECCCoinGiftPopup : MonoBehaviour
    {
        static ECCCoinGiftPopup open;

        int offer;
        string transactionId;
        Button watchButton;
        Text statusText;
        bool waiting = false;

        public static bool IsOpen
        {
            get { return open != null; }
        }

        public static void Open()
        {
            // One popup at a time, and never while a video flow is running
            if (open || ECCAds.IsBusy) return;

            ECCResultCard card = ECCPopupKit.Create("CanvasCoinGift", 92, "سکه رایگان", ECCCarTheme.CandyOrange, null, null, false, delegate { if (open) open.Close(); }, 560, 120);

            open = card.Panel.gameObject.AddComponent<ECCCoinGiftPopup>();
            open.Build(card);

            card.Finish(false);
        }

        static bool LimitReached
        {
            get
            {
                int limit = ECCEconomyConfig.Instance.rewardedCoinGiftDailyLimit;

                if (limit <= 0) return false;

                ECCSaveData data = ECCSave.Data;

                return ECCDaily.IsToday(data.coinGiftDay) && data.coinGiftCount >= limit;
            }
        }

        void Build(ECCResultCard card)
        {
            offer = Mathf.Max(1, ECCEconomyConfig.Instance.rewardedCoinGift);
            transactionId = "coingift-" + Guid.NewGuid().ToString("N");

            Vector2 center = new Vector2(0.5f, 0.5f);

            // The badge shows the coin itself instead of an icon
            Transform badge = card.Panel.Find("Badge");
            Image coinOnBadge = ECCUIKit.CreateCoinIcon("BigCoin", card.Panel, new Vector2(0.5f, 1), new Vector2(0, -150), 84);
            coinOnBadge.rectTransform.pivot = center;
            if (badge) coinOnBadge.transform.SetParent(badge, true);

            card.Title("با تماشای ویدئو، سکه جایزه بگیر!", ECCCarTheme.CandyOrangeShade, 30);

            // The offer: a cream pill with +amount and the coin
            RectTransform offerRow = card.CustomRow(96);
            Image pill = ECCRuntimeUI.CreateImage("Offer", offerRow, center, Vector2.zero, new Vector2(300, 90), ECCCarTheme.Hex(0xFFF1CC));
            ECCCarTheme.Round(pill);
            pill.pixelsPerUnitMultiplier = 1.2f;
            pill.raycastTarget = false;
            ECCUIKit.CreateNumberText("Amount", pill.transform, "+" + ECCWallet.Format(offer), 54, center, new Vector2(-40, 0), new Vector2(200, 84), ECCCarTheme.InkAmber);
            ECCUIKit.CreateCoinIcon("Coin", pill.transform, center, new Vector2(98, 0), 74);

            statusText = card.Line(string.Empty, 20, 30, ECCCarTheme.CardSubInk);

            watchButton = card.WideButton("ButtonWatch", "تماشای ویدئو", ECCCarTheme.CandyGreen, "Icons/play", 92, 32);
            watchButton.onClick.AddListener(Watch);
            ECCPulse.Set(watchButton, true, 0.04f);

            if (LimitReached)
            {
                watchButton.interactable = false;
                ECCPulse.Set(watchButton, false);
                ECCRuntimeUI.SetText(statusText, "سکه‌های رایگان امروز رو گرفتی. فردا دوباره سر بزن!");
            }
        }

        void Watch()
        {
            if (waiting || LimitReached) return;

            waiting = true;
            watchButton.interactable = false;
            ECCRuntimeUI.SetText(statusText, string.Empty);

            bool started = ECCAds.ShowRewarded("coin_gift", OnAdFinished);

            if (started == false)
            {
                waiting = false;
                watchButton.interactable = true;
            }
        }

        void OnAdFinished(bool rewarded)
        {
            // The popup may have been closed while the video was playing; the reward is still paid exactly once
            int amount = offer;
            string id = transactionId;

            // The video is over: Close() refuses only while one is running
            waiting = false;

            if (rewarded && ECCWallet.TryReward(id, amount))
            {
                ECCSaveData data = ECCSave.Data;

                if (ECCDaily.IsToday(data.coinGiftDay) == false)
                {
                    data.coinGiftDay = ECCDaily.Today;
                    data.coinGiftCount = 0;
                }

                data.coinGiftCount++;
                ECCSave.Save();

                ECCRewardFx.ShowCoins(amount);

                if (this) Close();

                return;
            }

            if (this == null) return;

            watchButton.interactable = LimitReached == false;
        }

        void Close()
        {
            if (waiting) return;

            if (open == this) open = null;

            ECCUIKit.ClosePopup(this);
        }

        void OnDestroy()
        {
            if (open == this) open = null;
        }
    }
}
