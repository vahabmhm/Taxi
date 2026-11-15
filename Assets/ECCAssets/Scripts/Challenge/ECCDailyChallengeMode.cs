using UnityEngine;
using EndlessCarChase.Levels;

namespace EndlessCarChase.Modes
{
    /// <summary>
    /// The daily challenge mode: today's generated level with a visible countdown.
    /// On a loss the run is not ended: the game is frozen in place ( same cars, money, progress and timer ) and the failure
    /// screen offers the once-per-day rewarded second chance. Only after a verified rewarded video the same run continues
    /// ( +30 seconds after a time-up, or a repaired car after a wreck ). Declining ends the run normally.
    /// </summary>
    public class ECCDailyChallengeMode : ECCLevelMode
    {
        public override GameModeType ModeType
        {
            get { return GameModeType.DailyChallenge; }
        }

        public override string HudTitle
        {
            get { return "چالش روز"; }
        }

        protected override ECCLevelData ResolveLevel()
        {
            return ECCDailyChallenge.Today;
        }

        public override string GetHighScoreKey(string sceneKey)
        {
            return sceneKey + "DailyChallenge";
        }

        // True while the failure screen is waiting for the player's choice
        bool failurePending = false;

        public bool IsFailurePending
        {
            get { return failurePending; }
        }

        protected override void OnTimeUp()
        {
            ShowFailure(false, null);
        }

        /// <summary>
        /// Called by the revive system when the player car is wrecked during the challenge. Returns true if the failure
        /// screen is shown ( the car must wait instead of dying )
        /// </summary>
        public bool OfferWreckContinue(ECCCar player)
        {
            if (failurePending || gameController.isGameOver || isRunning == false) return false;

            ShowFailure(true, player);

            return true;
        }

        void ShowFailure(bool wrecked, ECCCar player)
        {
            failurePending = true;

            // Freeze the exact state of the run behind the failure screen
            Time.timeScale = 0;

            ECCChallengeFailUI.Show(this, wrecked, player);
        }

        /// <summary>
        /// Continues the same run after a verified rewarded video. Returns false if nothing was waiting
        /// </summary>
        public bool ApplySecondChance(bool wrecked, ECCCar player)
        {
            if (failurePending == false || gameController.isGameOver) return false;

            failurePending = false;

            if (wrecked)
            {
                // Back on the road with the same timer: the car is repaired and briefly protected, like the regular revive
                if (player) ECCRevive.RestoreCar(player);

                Time.timeScale = 1;
            }
            else
            {
                // The time ran out: the same run continues with 30 more seconds
                ContinueWithExtraTime(ECCEconomyConfig.Instance.challengeReviveSeconds);
            }

            ECCHintBanner.Show("Icons/clock", ECCRuntimeUI.GoodColor, "ادامه‌ی چالش!", wrecked ? "ماشینت دوباره سالمه" : "+" + ECCUIKit.PersianDigits(ECCEconomyConfig.Instance.challengeReviveSeconds) + " ثانیه وقت اضافه", 2);

            return true;
        }

        /// <summary>
        /// The player declined the second chance: the run ends as a normal failure
        /// </summary>
        public void GiveUp(bool wrecked, ECCCar player)
        {
            if (failurePending == false) return;

            failurePending = false;

            Time.timeScale = 1;

            if (wrecked && player) player.FinishPlayerDeath();
            else if (wrecked == false) gameController.Fail();
        }

        public override void ShowResultScreen(bool victory)
        {
            isRunning = false;

            if (hud) hud.Hide();

            if (victory)
            {
                int paid = ECCDailyChallenge.CompleteToday();

                ECCChallengeResultUI.ShowVictory(gameController, paid, true);
            }
            else
            {
                ECCChallengeResultUI.ShowFailure(gameController);
            }

            // Freeze the game behind the result screen
            Time.timeScale = 0;
        }
    }
}
