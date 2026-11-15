using UnityEngine;
using UnityEngine.UI;
using EndlessCarChase.Modes;
using EndlessCarChase.Levels;

namespace EndlessCarChase
{
    /// <summary>
    /// The daily challenge popup «چالش روز» on the shared result card ( ECCResultCard ): today's goal, time and reward,
    /// with «شروع چالش امروز» and «امروز چالش نمی‌خوام», and when the next challenge comes
    /// </summary>
    public class ECCChallengePopup : MonoBehaviour
    {
        static ECCChallengePopup open;

        ECCGameController gameController;

        public static bool IsOpen
        {
            get { return open != null; }
        }

        public static void Open(ECCGameController gameController)
        {
            if (open || gameController == null) return;

            ECCResultCard card = ECCResultCard.Create("CanvasDailyChallenge", 72, "چالش روز", ECCCarTheme.Hex(0xFF6B4A));

            open = card.Panel.gameObject.AddComponent<ECCChallengePopup>();
            open.gameController = gameController;
            open.Build(card);

            card.Finish();
        }

        void Build(ECCResultCard card)
        {
            if (ECCDailyChallenge.IsCompletedToday)
            {
                BuildCompleted(card);
                return;
            }

            ECCLevelData challenge = ECCDailyChallenge.Today;

            card.Badge("Icons/clock", ECCCarTheme.Hex(0xFF6B4A), true);
            card.Title("چالش روز", ECCCarTheme.Hex(0xD9481F), 44);
            card.Line("چالش امروز را کامل کن و جایزه بگیر!", 23, 34, ECCCarTheme.CardInk);

            card.InfoBox("Icons/target", ECCCarTheme.CandyOrange, "هدف: " + challenge.ObjectiveDescription, 72);

            if (challenge.timeLimit > 0) card.InfoBox("Icons/clock", ECCCarTheme.CandyBlue, "زمان: " + ECCUIKit.PersianDigits(Mathf.RoundToInt(challenge.timeLimit)) + " ثانیه · یک بار در روز " + ECCUIKit.PersianDigits(ECCEconomyConfig.Instance.challengeReviveSeconds) + " ثانیه وقت اضافه با ویدئو", 66);

            card.MoneyPill("جایزه", challenge.reward);
            card.Line("بعد از برد، با تماشای ویدئو جایزه دوبرابر می‌شود", 19, 28);

            card.Space(4);

            Button start = card.WideButton("ButtonStart", "شروع چالش امروز", ECCCarTheme.CandyRed, "Icons/play", 96, 32);
            start.onClick.AddListener(StartChallenge);
            ECCPulse.Set(start, true, 0.04f);

            card.ButtonRow(68, new ECCResultCard.ButtonSpec("ButtonDecline", "امروز چالش نمی‌خوام", ECCCarTheme.CandyGray, null, Close));

            card.Line(NextChallengeText(), 18, 26);
        }

        void BuildCompleted(ECCResultCard card)
        {
            card.Badge("Icons/check", ECCCarTheme.CandyGreen, true);
            card.Title("چالش امروز را بردی!", ECCCarTheme.CandyGreenShade, 40);
            card.Line("فردا یک چالش تازه داریم.", 22, 32);

            card.MoneyPill("جایزه", ECCDailyChallenge.BaseReward);

            card.Space(4);

            Button result = card.WideButton("ButtonResult", ECCDailyChallenge.IsDoubleUsedToday ? "دیدن جایزه" : "دوبرابر کردن جایزه", ECCDailyChallenge.IsDoubleUsedToday ? ECCCarTheme.CandyBlue : ECCCarTheme.Hex(0x8E5CF7), ECCDailyChallenge.IsDoubleUsedToday ? null : "Icons/play", 88, 30);
            result.onClick.AddListener(delegate
            {
                ECCGameController controller = gameController;

                Close();

                ECCChallengeResultUI.ShowVictory(controller, 0, false);
            });

            if (ECCDailyChallenge.IsDoubleUsedToday == false) ECCPulse.Set(result, true, 0.04f);

            card.ButtonRow(68, new ECCResultCard.ButtonSpec("ButtonClose", "بستن", ECCCarTheme.CandyGray, null, Close));

            card.Line(NextChallengeText(), 18, 26);
        }

        static string NextChallengeText()
        {
            return ECCChallengePopupText.NextChallenge();
        }

        void StartChallenge()
        {
            ECCGameController controller = gameController;

            Close();

            ECCDailyChallenge.PlayRequested = true;

            if (controller) controller.ReloadScene(true);
        }

        void Close()
        {
            if (open == this) open = null;

            ECCUIKit.ClosePopup(this);
        }

        void OnDestroy()
        {
            if (open == this) open = null;
        }
    }

    /// <summary>
    /// The challenge failure screen, on the shared result card. The run is frozen behind it. Offers the once-per-day
    /// rewarded second chance ( +30 seconds after a time-up, a repaired car after a wreck ), or ends the run
    /// </summary>
    public class ECCChallengeFailUI : MonoBehaviour
    {
        static ECCChallengeFailUI open;

        ECCDailyChallengeMode mode;
        bool wrecked;
        ECCCar player;

        Button reviveButton;
        Button giveUpButton;
        bool waitingForAd = false;
        bool resolved = false;

        public static bool IsOpen
        {
            get { return open != null; }
        }

        public static void Show(ECCDailyChallengeMode mode, bool wrecked, ECCCar player)
        {
            if (open) return;

            ECCResultCard card = ECCResultCard.Create("CanvasChallengeFail", 86, "چالش روز", ECCCarTheme.CandyRed);

            open = card.Panel.gameObject.AddComponent<ECCChallengeFailUI>();
            open.mode = mode;
            open.wrecked = wrecked;
            open.player = player;
            open.Build(card);

            card.Finish();
        }

        void Build(ECCResultCard card)
        {
            card.Badge(wrecked ? "Icons/fire" : "Icons/clock", ECCCarTheme.CandyRed, false);
            card.Title(wrecked ? "ماشینت نابود شد!" : "وقت تمام شد!", ECCCarTheme.CandyRed, 42);
            card.Line("چالش هنوز تمام نشده", 22, 32);

            card.InfoBox("Icons/target", ECCCarTheme.CandyOrange, mode.ProgressText, 66);

            int seconds = ECCEconomyConfig.Instance.challengeReviveSeconds;

            card.Space(4);

            if (ECCDailyChallenge.IsReviveUsedToday == false)
            {
                // A big rewarded button with a tag: "+30 seconds" after a time-up, "continue" after a wreck
                string label = wrecked ? "تماشای ویدئو و ادامه‌ی چالش" : "تماشای ویدئو و دریافت " + ECCUIKit.PersianDigits(seconds) + " ثانیه فرصت دوباره";

                reviveButton = card.WideButton("ButtonRevive", label, ECCCarTheme.CandyGreen, "Icons/play", 112, 28);

                Image tag = ECCRuntimeUI.CreateImage("Tag", reviveButton.transform, new Vector2(0, 1), new Vector2(18, 16), new Vector2(150, 40), ECCCarTheme.CandyOrange);
                tag.rectTransform.pivot = new Vector2(0, 0.5f);
                ECCCarTheme.Round(tag);
                tag.pixelsPerUnitMultiplier = 48 / 20f;
                tag.raycastTarget = false;

                Text tagText = ECCRuntimeUI.CreateText("TagText", tag.transform, wrecked ? "ادامه" : "+" + ECCUIKit.PersianDigits(seconds) + " ثانیه", 20, new Vector2(0.5f, 0.5f), new Vector2(0, 1), new Vector2(140, 36), Color.white);
                ECCCarTheme.CandyLabel(tagText);

                reviveButton.onClick.AddListener(WatchForSecondChance);
                ECCPulse.Set(reviveButton, true, 0.04f);

                card.Line("فقط یک بار در روز", 18, 26);
            }
            else
            {
                card.InfoBox("Icons/lock", ECCCarTheme.CandyGray, "فرصت مجدد امروز استفاده شده", 66);
            }

            giveUpButton = card.WideButton("ButtonGiveUp", "پایان چالش", ECCCarTheme.CandyGray, null, 68, 26);
            giveUpButton.onClick.AddListener(GiveUp);
        }

        void WatchForSecondChance()
        {
            if (waitingForAd || resolved || ECCDailyChallenge.IsReviveUsedToday) return;

            waitingForAd = true;
            reviveButton.interactable = false;
            giveUpButton.interactable = false;

            if (ECCAds.ShowRewarded("challenge_revive", OnAdFinished) == false) OnAdFinished(false);
        }

        void OnAdFinished(bool rewarded)
        {
            waitingForAd = false;

            if (this == null || resolved) return;

            if (rewarded && ECCDailyChallenge.IsReviveUsedToday == false && mode && mode.ApplySecondChance(wrecked, player))
            {
                // The effect is applied; record today's use right away
                ECCDailyChallenge.ConsumeRevive();

                resolved = true;

                Close();

                return;
            }

            // No reward: nothing is consumed, the player can try again or give up
            reviveButton.interactable = ECCDailyChallenge.IsReviveUsedToday == false;
            giveUpButton.interactable = true;
        }

        void GiveUp()
        {
            if (waitingForAd || resolved) return;

            resolved = true;

            Close();

            if (mode) mode.GiveUp(wrecked, player);
        }

        void Close()
        {
            if (open == this) open = null;

            ECCUIKit.ClosePopup(this);
        }

        void OnDestroy()
        {
            if (open == this) open = null;
        }
    }

    /// <summary>
    /// The challenge result screens, on the shared result card: «برنده شدی!» with confetti, the base reward ( paid once )
    /// and the once-per-day rewarded 2X; or the failure screen with retry. The victory screen can be reopened from the menu the same day to claim the 2X later
    /// </summary>
    public class ECCChallengeResultUI : MonoBehaviour
    {
        static ECCChallengeResultUI open;

        ECCGameController gameController;
        bool inGame;

        Button doubleButton;
        Text totalText;
        bool waitingForAd = false;

        public static bool IsOpen
        {
            get { return open != null; }
        }

        /// <summary>
        /// paidNow: coins of the base reward paid by this victory ( 0 when reopened ). inGame: shown at the end of the run
        /// </summary>
        public static void ShowVictory(ECCGameController gameController, int paidNow, bool inGame)
        {
            if (open) return;

            ECCResultCard card = ECCResultCard.Create("CanvasChallengeResult", 80, "چالش روز", ECCCarTheme.CandyGreen);

            open = card.Panel.gameObject.AddComponent<ECCChallengeResultUI>();
            open.gameController = gameController;
            open.inGame = inGame;
            open.BuildVictory(card, paidNow);

            card.Finish();

            if (inGame) ECCRewardFx.Confetti();
            if (paidNow > 0) ECCRewardFx.ShowCoins(paidNow);
        }

        public static void ShowFailure(ECCGameController gameController)
        {
            if (open) return;

            ECCResultCard card = ECCResultCard.Create("CanvasChallengeResult", 80, "چالش روز", ECCCarTheme.CandyRed);

            open = card.Panel.gameObject.AddComponent<ECCChallengeResultUI>();
            open.gameController = gameController;
            open.inGame = true;
            open.BuildFailure(card);

            card.Finish();
        }

        void BuildVictory(ECCResultCard card, int paidNow)
        {
            int reward = ECCDailyChallenge.BaseReward;

            card.Badge("Icons/trophy", ECCCarTheme.Hex(0xFFB020), true);
            card.Title("برنده شدی!", ECCCarTheme.CandyGreenShade, 48);
            card.Line("چالش امروز را کامل کردی", 23, 34, ECCCarTheme.CardInk);

            card.MoneyPill("جایزه", reward);

            Button close = ECCUIKit.CreateCloseButton(card.Panel);
            close.onClick.AddListener(Leave);

            card.Space(4);

            // The 2X rewarded button: «2X» in a gold disc, the play mark, the text and the resulting total
            doubleButton = card.WideButton("ButtonDouble", string.Empty, ECCCarTheme.Hex(0x8E5CF7), null, 112, 26);

            float width = ((RectTransform)doubleButton.transform).sizeDelta.x;

            Image disc = ECCUIKit.CreateCircle("TwoXDisc", doubleButton.transform, new Vector2(1, 0.5f), new Vector2(-16, 3), 84, ECCCarTheme.Hex(0xFFC83D));
            disc.rectTransform.pivot = new Vector2(1, 0.5f);
            disc.raycastTarget = false;

            Text twoX = ECCRuntimeUI.CreateText("2X", disc.transform, string.Empty, 36, new Vector2(0.5f, 0.5f), new Vector2(0, 2), new Vector2(84, 60), ECCCarTheme.CardInk);
            twoX.text = "2X";
            twoX.font = ECCCarTheme.TitleFont;
            ECCCarTheme.InkText(twoX, ECCCarTheme.CardInk);

            Image play = ECCCarTheme.CreateIcon("Play", doubleButton.transform, "Icons/play", new Vector2(0, 0.5f), new Vector2(46, 3), 44, Color.white);
            play.rectTransform.pivot = new Vector2(0.5f, 0.5f);

            Text label = ECCRuntimeUI.GetLabel(doubleButton);
            label.rectTransform.sizeDelta = new Vector2(width - 220, 44);
            label.rectTransform.anchoredPosition = new Vector2(-20, 20);

            totalText = ECCRuntimeUI.CreateText("Total", doubleButton.transform, string.Empty, 24, new Vector2(0.5f, 0.5f), new Vector2(-20, -20), new Vector2(width - 220, 36), ECCCarTheme.Hex(0xFFE08A));
            ECCCarTheme.CandyLabel(totalText);
            totalText.color = ECCCarTheme.Hex(0xFFE08A);

            RefreshDoubleButton();

            doubleButton.onClick.AddListener(WatchForDouble);

            card.ButtonRow(70, new ECCResultCard.ButtonSpec("ButtonMenu", inGame ? "منو" : "بستن", ECCCarTheme.CandyGray, inGame ? "Icons/home" : null, Leave));

            card.Line(ECCChallengePopupText.NextChallenge(), 18, 26);
        }

        void RefreshDoubleButton()
        {
            bool used = ECCDailyChallenge.IsDoubleUsedToday;

            ECCRuntimeUI.SetText(ECCRuntimeUI.GetLabel(doubleButton), used ? "جایزه دوبرابر امروز دریافت شده" : "تماشای ویدئو و دوبرابر کردن جایزه");

            doubleButton.interactable = used == false && waitingForAd == false;
            doubleButton.targetGraphic.color = used ? ECCCarTheme.CandyGray : ECCCarTheme.Hex(0x8E5CF7);

            ECCPulse.Set(doubleButton, used == false && waitingForAd == false, 0.05f);

            int reward = ECCDailyChallenge.BaseReward;
            string total = ECCUIKit.PersianDigits(reward * 2);

            ECCRuntimeUI.SetText(totalText, used ? "جمع: " + total + " دلار" : ECCUIKit.PersianDigits(reward) + " + " + ECCUIKit.PersianDigits(reward) + " = " + total + " دلار");
        }

        void WatchForDouble()
        {
            if (waitingForAd || ECCDailyChallenge.IsDoubleUsedToday) return;

            waitingForAd = true;
            RefreshDoubleButton();

            if (ECCAds.ShowRewarded("challenge_double", OnDoubleAdFinished) == false) OnDoubleAdFinished(false);
        }

        void OnDoubleAdFinished(bool rewarded)
        {
            waitingForAd = false;

            // Paid even if the screen was closed during the video ( the bonus is still earned exactly once )
            int paid = rewarded ? ECCDailyChallenge.ClaimDouble() : 0;

            if (paid > 0) ECCRewardFx.ShowCoins(paid);

            if (this == null) return;

            RefreshDoubleButton();
        }

        void BuildFailure(ECCResultCard card)
        {
            card.Badge("Icons/lock", ECCCarTheme.CandyRed, false);
            card.Title("چالش امروز ناموفق بود", ECCCarTheme.CandyRed, 40);
            card.Line("تا آخر امروز می‌توانی دوباره امتحان کنی", 22, 32);

            card.InfoBox("Icons/target", ECCCarTheme.CandyOrange, "هدف: " + ECCDailyChallenge.Today.ObjectiveDescription, 70);

            card.Space(4);

            Button retry = card.WideButton("ButtonRetry", "دوباره تلاش کن", ECCCarTheme.CandyOrange, "Icons/play", 92, 32);
            retry.onClick.AddListener(delegate
            {
                ECCDailyChallenge.PlayRequested = true;
                ECCGameController controller = gameController;
                Close();
                if (controller) controller.ReloadScene(true);
            });
            ECCPulse.Set(retry, true, 0.04f);

            card.ButtonRow(70, new ECCResultCard.ButtonSpec("ButtonMenu", "منو", ECCCarTheme.CandyGray, "Icons/home", Leave));

            card.Line(ECCChallengePopupText.NextChallenge(), 18, 26);
        }

        /// <summary>
        /// Leaves the challenge: back to the main menu after a run, or just closes the reopened result
        /// </summary>
        void Leave()
        {
            if (waitingForAd) return;

            ECCGameController controller = gameController;
            bool reload = inGame;

            Close();

            if (reload)
            {
                ECCDailyChallenge.PlayRequested = false;

                if (controller) controller.ReloadScene(false);
            }
        }

        void Close()
        {
            if (open == this) open = null;

            ECCUIKit.ClosePopup(this);
        }

        void OnDestroy()
        {
            if (open == this) open = null;
        }
    }

    /// <summary>
    /// Texts shared by the challenge screens
    /// </summary>
    public static class ECCChallengePopupText
    {
        /// <summary>
        /// «چالش تازه تا ۵ ساعت دیگر» ( the next local midnight )
        /// </summary>
        public static string NextChallenge()
        {
            System.TimeSpan left = System.DateTime.Today.AddDays(1) - System.DateTime.Now;

            if (left.TotalHours >= 1) return "چالش تازه تا " + ECCUIKit.PersianDigits((int)left.TotalHours) + " ساعت دیگر";

            return "چالش تازه تا " + ECCUIKit.PersianDigits(Mathf.Max(1, (int)left.TotalMinutes)) + " دقیقه‌ی دیگر";
        }
    }
}
