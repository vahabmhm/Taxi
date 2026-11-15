using System;
using UnityEngine;

namespace EndlessCarChase
{
    /// <summary>
    /// What happens when the player leaves a NORMAL level victory screen ( Next Level, Retry or Menu ), before the scene changes.
    /// At most one extra popup or ad per transition, in priority order:
    /// 1. an interstitial on every Nth Next Level tap ( ECCEconomyConfig.interstitialEveryLevels, default 4 ), only if one is
    ///    ready; otherwise it stays due for the next Next Level tap
    /// 2. today's other-game recommendation ( once per day )
    /// 3. the one-time store review prompt ( ECCReviewPrompt ), only for Next Level and only without a scheduled interstitial
    /// The daily challenge result never goes through here.
    /// </summary>
    public static class ECCPostVictoryFlow
    {
        static bool running = false;

        /// <summary>
        /// Runs the transition popups ( if any ) and then the navigation. Repeated taps while running are ignored
        /// </summary>
        public static void Leave(bool isNextLevel, Action navigate)
        {
            // A transition popup is still open: ignore the repeated tap
            if (running && (ECCGameRecommendationUI.IsOpen || ECCReviewPromptUI.IsOpen || ECCAds.IsBusy)) return;

            running = true;

            Action finish = delegate
            {
                running = false;

                if (navigate != null) navigate();
            };

            // 1. The interstitial every few levels, right after the victory's Next Level tap
            if (isNextLevel && TryShowInterstitial(finish)) return;

            // 2. The daily recommendation; the review prompt waits for a later transition
            if (ECCGameRecommendation.TryShow(finish)) return;

            // 3. The one-time review prompt: only on Next Level, never when an interstitial uses this transition
            if (isNextLevel && ECCAds.IsInterstitialScheduled == false && ECCReviewPrompt.TryShow(finish)) return;

            finish();
        }

        /// <summary>
        /// Counts this Next Level tap and shows an interstitial when enough levels passed since the last one.
        /// Nothing is shown ( and the loading popup doesn't appear ) when no interstitial is ready
        /// </summary>
        static bool TryShowInterstitial(Action finish)
        {
            ECCSave.Data.levelsSinceInterstitial++;
            ECCSave.Save();

            if (ECCSave.Data.levelsSinceInterstitial < Mathf.Max(1, ECCEconomyConfig.Instance.interstitialEveryLevels)) return false;

            if (ECCAds.IsBusy || ECCModalTracker.AnyOpen || ECCAds.IsInterstitialReady == false) return false;

            return ECCAds.ShowInterstitial("next_level", delegate (bool shown)
            {
                if (shown)
                {
                    ECCSave.Data.levelsSinceInterstitial = 0;
                    ECCSave.Save();
                }

                finish();
            });
        }
    }
}
