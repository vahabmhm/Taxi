using System;
using UnityEngine;
using UnityEngine.UI;

namespace EndlessCarChase.Levels
{
    /// <summary>
    /// The victory / failure screen of the levels mode, on the shared result card ( ECCResultCard ).
    /// Victory: a trophy badge with turning light rays, «برنده شدی!», stars popping in, the money reward ( already paid by
    /// the level mode ), the chapter's progress, a preview of the next level, confetti, and a pulsing rewarded 2X button
    /// ( bonus = the same reward, once per screen, limited per day by ECCEconomyConfig.levelDoubleDailyLimit ), then
    /// «مرحله بعد» and «دوباره» / «منو».
    /// Failure: a busted badge, the goal, a driving tip, and after two losses in a row on the same level a skip option
    /// ( coins, or a video when the coins aren't enough ), then a pulsing «دوباره» and «منو».
    /// The continue / revive offers happen before this screen, while the run is frozen.
    /// </summary>
    public class ECCLevelResultUI : MonoBehaviour
    {
        static readonly string[] Tips =
        {
            "درست وسط دایره ترمز کن تا انعام بیشتری بگیری.",
            "هر ضربه و تصادف از امتیاز مسافر کم می‌کند؛ آرام رانندگی کن.",
            "چراغ قرمز، تصادف و خلاف جهت جریمه دارد و پلیس را دنبالت می‌اندازد.",
            "وقتی پلیس دنبالت است، دور شو تا گمت کنند و جریمه‌ها بخشیده شود.",
            "مسافر VIP چهار برابر کرایه می‌دهد؛ زودتر از رقیب‌ها برس.",
            "با شوک الکتریکی، تاکسی‌های رقیب چند ثانیه خاموش می‌شوند.",
            "آهن‌ربا دایره‌ی سوار کردن مسافر را بزرگ‌تر می‌کند.",
            "ون جا برای خانواده‌های سه‌نفره دارد.",
            "بوق بزن تا مسافرهای نزدیک صبر بیشتری کنند.",
        };

        static ECCLevelResultUI open;

        ECCGameController gameController;
        ECCLevelData level;
        bool victory;
        int reward;

        Button doubleButton;
        Text doubleTotal;
        string doubleTransaction;
        bool doubled = false;
        bool waitingForAd = false;
        bool leaving = false;

        public static bool IsOpen
        {
            get { return open != null; }
        }

        public static void Show(ECCGameController gameController, ECCLevelData level, bool victory, int stars, int reward, int score)
        {
            if (open) return;

            string header = level != null ? level.DisplayName : "مرحله";

            ECCResultCard card = ECCResultCard.Create("CanvasLevelResult", 80, header, victory ? ECCCarTheme.CandyGreen : ECCCarTheme.CandyRed);

            open = card.Panel.gameObject.AddComponent<ECCLevelResultUI>();
            open.gameController = gameController;
            open.level = level;
            open.victory = victory;
            open.reward = reward;
            open.doubleTransaction = "level-double-" + (level != null ? level.id : "none") + "-" + Guid.NewGuid().ToString("N");

            if (victory) open.BuildVictory(card, stars, score);
            else open.BuildFailure(card, score);

            card.Finish();
        }

        // ----- Daily limit of the 2X bonus -----

        static int DoublesLeftToday
        {
            get
            {
                int limit = ECCEconomyConfig.Instance.levelDoubleDailyLimit;

                if (limit <= 0) return int.MaxValue;

                ECCSaveData data = ECCSave.Data;

                return Mathf.Max(0, limit - (ECCDaily.IsToday(data.levelDoubleDay) ? data.levelDoubleCount : 0));
            }
        }

        static void CountDouble()
        {
            ECCSaveData data = ECCSave.Data;

            if (ECCDaily.IsToday(data.levelDoubleDay) == false)
            {
                data.levelDoubleDay = ECCDaily.Today;
                data.levelDoubleCount = 0;
            }

            data.levelDoubleCount++;
            ECCSave.Save();
        }

        // ----- Victory -----

        void BuildVictory(ECCResultCard card, int stars, int score)
        {
            card.Badge("Icons/trophy", ECCCarTheme.Hex(0xFFB020), true);
            card.Title("برنده شدی!", ECCCarTheme.CandyGreenShade, 46);

            if (level != null) card.Line(level.title, 24, 34, ECCCarTheme.CardInk);

            card.Stars(3, stars);

            if (reward > 0) card.MoneyPill("جایزه", reward);

            card.Line("پول جمع‌شده در این دور: " + ECCUIKit.PersianDigits(score) + " دلار", 20, 30);

            ECCLevelData nextLevel = level != null ? ECCLevelCatalog.Instance.GetNextLevel(level) : null;

            // The chapter's progress, and what comes next
            if (level != null)
            {
                ECCLevelData[] chapterLevels = ECCLevelCatalog.Instance.chapters[level.chapterIndex].levels;

                int passed = 0;

                foreach (ECCLevelData data in chapterLevels)
                {
                    if (ECCLevelProgress.IsPassed(data)) passed++;
                }

                card.Progress("فصل " + ECCUIKit.PersianDigits(level.chapterIndex + 1) + ": " + ECCUIKit.PersianDigits(passed) + " از " + ECCUIKit.PersianDigits(chapterLevels.Length) + " مرحله", passed / (float)Mathf.Max(1, chapterLevels.Length), ECCCarTheme.CandyGreen);
            }

            // The weekly league: stars added by this win and the rank change since the rank the player saw last
            ECCLeagueWin league = level != null ? ECCWeeklyLeague.TakePendingWin(level.id) : null;

            if (league != null)
            {
                Text leagueText = card.InfoBox("Icons/crown", ECCWeeklyLeagueUI.LeagueColor, ECCWeeklyLeague.StarsText(league) + "\n" + ECCWeeklyLeague.RankChangeText(league), 76);
                leagueText.transform.parent.gameObject.AddComponent<ECCPopIn>();
            }

            if (nextLevel != null) card.InfoBox("Icons/target", ECCCarTheme.CandyBlue, "بعدی: " + nextLevel.DisplayName + " · " + nextLevel.title + "\n" + nextLevel.ObjectiveDescription, 76);

            card.Space(4);

            if (reward > 0) BuildDoubleButton(card);

            if (nextLevel != null)
            {
                Button next = card.WideButton("ButtonNext", "مرحله بعد", ECCCarTheme.CandyGreen, "Icons/play", 92, 34);
                next.onClick.AddListener(delegate
                {
                    Navigate(true, delegate
                    {
                        ECCLevelCatalog.Instance.SelectedLevel = nextLevel;
                        gameController.ReloadScene(true);
                    });
                });

                if (reward <= 0) ECCPulse.Set(next, true, 0.04f);
            }

            BuildSecondaryRow(card, nextLevel == null ? ECCCarTheme.CandyOrange : ECCCarTheme.CandyBlue);

            ECCRewardFx.Confetti();

            if (reward > 0) ECCRewardFx.ShowCoins(reward);

            if (nextLevel == null && level != null) ECCToast.Show("همه‌ی مرحله‌ها را تمام کردی!", ECCRuntimeUI.StarColor);
        }

        void BuildDoubleButton(ECCResultCard card)
        {
            doubleButton = card.WideButton("ButtonDouble", string.Empty, ECCCarTheme.Hex(0x8E5CF7), null, 112, 26);

            RectTransform rect = (RectTransform)doubleButton.transform;
            float width = rect.sizeDelta.x;

            // «2X» in a gold disc on the right, the play mark on the left, the text and the sum in the middle
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

            doubleTotal = ECCRuntimeUI.CreateText("Total", doubleButton.transform, string.Empty, 24, new Vector2(0.5f, 0.5f), new Vector2(-20, -20), new Vector2(width - 220, 36), ECCCarTheme.Hex(0xFFE08A));
            ECCCarTheme.CandyLabel(doubleTotal);
            doubleTotal.color = ECCCarTheme.Hex(0xFFE08A);

            doubleButton.onClick.AddListener(WatchForDouble);

            RefreshDoubleButton();
        }

        void RefreshDoubleButton()
        {
            if (doubleButton == null) return;

            bool limitReached = DoublesLeftToday <= 0;
            bool available = doubled == false && limitReached == false;

            string text = doubled ? "جایزه دوبرابر شد!" : (limitReached ? "دوبرابر کردن امروز تمام شد" : "تماشای ویدئو و دوبرابر کردن جایزه");

            ECCRuntimeUI.SetText(ECCRuntimeUI.GetLabel(doubleButton), text);

            string total = ECCUIKit.PersianDigits(reward * 2);

            ECCRuntimeUI.SetText(doubleTotal, doubled ? "جمع: " + total + " دلار" : ECCUIKit.PersianDigits(reward) + " + " + ECCUIKit.PersianDigits(reward) + " = " + total + " دلار");

            doubleButton.interactable = available && waitingForAd == false;
            doubleButton.targetGraphic.color = available ? ECCCarTheme.Hex(0x8E5CF7) : ECCCarTheme.CandyGray;

            // Pulses to attract attention while the bonus is available
            ECCPulse.Set(doubleButton, available && waitingForAd == false, 0.05f);
        }

        void WatchForDouble()
        {
            if (waitingForAd || doubled || DoublesLeftToday <= 0) return;

            waitingForAd = true;
            RefreshDoubleButton();

            if (ECCAds.ShowRewarded("level_double", OnDoubleFinished) == false) OnDoubleFinished(false);
        }

        void OnDoubleFinished(bool rewarded)
        {
            waitingForAd = false;

            // The bonus equals the base reward, paid once per screen ( the base itself is never paid again )
            if (rewarded && doubled == false && ECCWallet.TryReward(doubleTransaction, reward))
            {
                doubled = true;

                CountDouble();

                ECCRewardFx.ShowCoins(reward);
            }

            if (this == null) return;

            RefreshDoubleButton();
        }

        // ----- Failure -----

        void BuildFailure(ECCResultCard card, int score)
        {
            card.Badge("Icons/lock", ECCCarTheme.CandyRed, false);
            card.Title("دستگیر شدی!", ECCCarTheme.CandyRed, 46);

            if (level != null)
            {
                card.Line(level.title, 24, 34, ECCCarTheme.CardInk);
                card.InfoBox("Icons/target", ECCCarTheme.CandyOrange, "هدف: " + level.ObjectiveDescription, 72);
            }

            card.Line("پول جمع‌شده در این دور: " + ECCUIKit.PersianDigits(score) + " دلار", 20, 30);

            if (ECCLevelProgress.CanOfferSkip(level)) BuildSkipOption(card);
            else card.InfoBox("Icons/bolt", ECCCarTheme.CandyBlue, Tips[UnityEngine.Random.Range(0, Tips.Length)], 72);

            card.Space(4);

            Button retry = card.WideButton("ButtonRetry", "دوباره", ECCCarTheme.CandyOrange, "Icons/play", 92, 34);
            retry.onClick.AddListener(delegate { Navigate(false, delegate { gameController.ReloadScene(true); }); });
            ECCPulse.Set(retry, true, 0.04f);

            card.ButtonRow(72, new ECCResultCard.ButtonSpec("ButtonMenu", "منو", ECCCarTheme.CandyGray, "Icons/home", delegate { Navigate(false, delegate { gameController.ReloadScene(false); }); }));
        }

        void BuildSkipOption(ECCResultCard card)
        {
            int price = ECCLevelProgress.SkipPrice;
            bool affordable = ECCWallet.CanAfford(price);

            card.Line("دو بار پشت سر هم باختی؛ می‌توانی این مرحله را رد کنی", 20, 30, ECCCarTheme.CardInk);

            Button skip = card.WideButton("ButtonSkip", affordable ? "رد کردن مرحله با " + ECCWallet.Format(price) + " دلار" : "تماشای ویدئو و رد کردن مرحله", ECCCarTheme.Hex(0x8E5CF7), affordable ? null : "Icons/play", 84, 26);

            if (affordable)
            {
                Image dollar = ECCRuntimeUI.CreateImage("Dollar", skip.transform, new Vector2(1, 0.5f), new Vector2(-18, 3), new Vector2(58, 58), Color.white);
                dollar.rectTransform.pivot = new Vector2(1, 0.5f);
                dollar.sprite = ECCCoinBalanceButton.DollarSprite;
                dollar.preserveAspect = true;
                dollar.raycastTarget = false;
            }

            card.Line("مرحله‌ی بعد باز می‌شود، ولی ستاره و جایزه‌ی این مرحله داده نمی‌شود", 17, 26);

            skip.onClick.AddListener(delegate { Skip(skip); });
            ECCPulse.Set(skip, true, 0.04f);
        }

        void Skip(Button skip)
        {
            if (waitingForAd || leaving || level == null || ECCLevelProgress.CanOfferSkip(level) == false) return;

            skip.interactable = false;

            int price = ECCLevelProgress.SkipPrice;

            if (ECCWallet.TrySpend(price))
            {
                FinishSkip();
                return;
            }

            // Not enough coins: a verified rewarded video skips instead
            waitingForAd = true;

            bool started = ECCAds.ShowRewarded("level_skip", delegate (bool rewarded)
            {
                waitingForAd = false;

                if (this == null) return;

                if (rewarded) FinishSkip();
                else skip.interactable = true;
            });

            if (started == false)
            {
                waitingForAd = false;
                skip.interactable = true;
            }
        }

        void FinishSkip()
        {
            ECCLevelData next = ECCLevelProgress.Skip(level);

            ECCToast.Show("مرحله رد شد!", ECCRuntimeUI.GoodColor);

            Navigate(false, delegate
            {
                if (next != null) gameController.ReloadScene(true);
                else gameController.ReloadScene(false);
            });
        }

        // ----- Navigation -----

        /// <summary>
        /// «دوباره» and «منو» side by side
        /// </summary>
        void BuildSecondaryRow(ECCResultCard card, Color retryColor)
        {
            card.ButtonRow(72,
                new ECCResultCard.ButtonSpec("ButtonRetry", "دوباره", retryColor, null, delegate { Navigate(false, delegate { gameController.ReloadScene(true); }); }),
                new ECCResultCard.ButtonSpec("ButtonMenu", "منو", ECCCarTheme.CandyGray, "Icons/home", delegate { Navigate(false, delegate { gameController.ReloadScene(false); }); }));
        }

        /// <summary>
        /// Closes this screen, then runs the post-victory popups ( after a win ) and the navigation
        /// </summary>
        void Navigate(bool isNextLevel, Action navigate)
        {
            if (leaving || waitingForAd) return;

            leaving = true;

            bool wasVictory = victory;

            Close();

            if (wasVictory) ECCPostVictoryFlow.Leave(isNextLevel, navigate);
            else navigate();
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
}
