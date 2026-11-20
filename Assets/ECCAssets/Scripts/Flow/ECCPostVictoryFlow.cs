using System;
using UnityEngine;

namespace EndlessCarChase
{
    /// <summary>
    /// What happens when the player leaves a NORMAL level victory screen ( Next Level, Retry or Menu ), before the scene changes.
    /// At most one extra popup or ad per transition, in priority order:
    /// 1. a due interstitial ( ECCInterstitialSchedule: none through level 10, every 4 completed levels after it, every 3
    ///    after level 50 ), only if one is ready and no rewarded video was watched on this victory screen; otherwise it
    ///    stays due for the next transition
    /// 2. today's other-game recommendation ( once per day )
    /// 3. the one-time store review prompt ( ECCReviewPrompt ), only for Next Level and never on a transition where an
    ///    interstitial was due, even if that interstitial then failed
    /// The daily challenge result never goes through here.
    /// </summary>
    public static class ECCPostVictoryFlow
    {
        static bool running = false;

        /// <summary>
        /// True while this transition chose an interstitial ( due ), so the review prompt waits for a later one
        /// </summary>
        public static bool InterstitialSelected { get; private set; }

        public static void Leave(bool isNextLevel, Action navigate)
        {
            Leave(isNextLevel, navigate, false);
        }

        /// <summary>
        /// Runs the transition popups ( if any ) and then the navigation. Repeated taps while running are ignored.
        /// rewardedWatched: a rewarded video was just watched on the victory screen ( the 2X ), so no interstitial now
        /// </summary>
        public static void Leave(bool isNextLevel, Action navigate, bool rewardedWatched)
        {
            // A transition popup is still open: ignore the repeated tap
            if (running && (ECCGameRecommendationUI.IsOpen || ECCReviewPromptUI.IsOpen || ECCAds.IsBusy)) return;

            running = true;
            InterstitialSelected = false;

            Action finish = delegate
            {
                running = false;
                InterstitialSelected = false;

                if (navigate != null) navigate();
            };

            // 1. A due interstitial ( never right after a rewarded video of the same transition )
            if (rewardedWatched == false && ECCInterstitialSchedule.IsDue)
            {
                InterstitialSelected = true;

                if (TryShowInterstitial(finish)) return;
            }

            // 2. The daily recommendation; the review prompt waits for a later transition
            if (ECCGameRecommendation.TryShow(finish)) return;

            // 3. The one-time review prompt: only on Next Level, never when an interstitial was due for this transition
            if (isNextLevel && InterstitialSelected == false && ECCAds.IsInterstitialScheduled == false && ECCReviewPrompt.TryShow(finish)) return;

            finish();
        }

        /// <summary>
        /// Shows the due interstitial. Nothing is shown ( and the loading popup doesn't appear ) when none is ready; then it
        /// simply stays due. The counter is reset only after a real display
        /// </summary>
        static bool TryShowInterstitial(Action finish)
        {
            if (ECCAds.IsBusy || ECCModalTracker.AnyOpen || ECCAds.IsInterstitialReady == false) return false;

            return ECCAds.ShowInterstitial("level_transition", delegate (bool shown)
            {
                if (shown) ECCInterstitialSchedule.MarkShown();

                finish();
            });
        }
    }
}
