using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace EndlessCarChase
{
    public enum ECCAdResult
    {
        Rewarded = 0, // The SDK confirmed the video was watched to the end
        Skipped = 1,  // Shown, but closed before the reward
        Failed = 2,   // Failed after it was ( or may have been ) on screen
        Unavailable = 3, // No network could present a video: the NoAds panel is shown
    }

    /// <summary>
    /// One advertising provider. The unified Adivery / Tapsell Mediation manager ( AdsManagerNew.cs, Task 5 ) registers
    /// itself here with ECCAds.RegisterProvider; it does the Adivery-first rotation and the fallback between the two networks.
    /// </summary>
    public interface IECCAdProvider
    {
        string Name { get; }

        bool IsRewardedReady { get; }

        /// <summary>
        /// True when the network reported that it can't load a rewarded video now ( no fill, error ), so there's no need to wait
        /// </summary>
        bool HasRewardedFailed { get; }

        /// <summary>
        /// Starts loading a rewarded video if the network needs an explicit request
        /// </summary>
        void RequestRewarded();

        /// <summary>
        /// Shows a rewarded video. Must call done exactly once ( ECCAds also ignores duplicate calls )
        /// </summary>
        void ShowRewarded(Action<ECCAdResult> done);
    }

    /// <summary>
    /// A network that can also show interstitials ( optional )
    /// </summary>
    public interface IECCInterstitialProvider
    {
        bool IsInterstitialReady { get; }

        /// <summary>
        /// Shows an interstitial. done(true) once it was shown and closed, done(false) if it couldn't be shown
        /// </summary>
        void ShowInterstitial(Action<bool> done);
    }

    /// <summary>
    /// The single entry point every feature uses for rewarded videos: coin gift, daily challenge +30 seconds and 2X,
    /// power-ups, revive, level 2X and level skip.
    ///
    /// - Only one ad flow can run at a time; extra requests are refused while one is running.
    /// - The callback receives true ONLY when a provider reports a verified rewarded completion, and is called exactly once.
    /// - A shared loading popup is shown for at least 2 seconds before the video ( ECCAdLoadingPopup ).
    /// - When no provider has a video, the NoAds panel is shown and the callback receives false.
    /// No reward is ever simulated: without a configured provider every request ends in the NoAds panel.
    /// </summary>
    public static class ECCAds
    {
        // How long to wait for a provider to get a video ready before giving up
        public const float ReadyTimeout = 6;

        static readonly List<IECCAdProvider> providers = new List<IECCAdProvider>();
        static bool defaultsRegistered = false;

        /// <summary>
        /// True while an ad flow ( loading, showing ) is running
        /// </summary>
        public static bool IsBusy { get; private set; }

        /// <summary>
        /// Set by interstitial placements when an interstitial is scheduled for the current screen transition.
        /// Other popups ( the review prompt ) stay away while it's true
        /// </summary>
        public static bool IsInterstitialScheduled { get; set; }

        public static void RegisterProvider(IECCAdProvider provider)
        {
            if (provider != null && providers.Contains(provider) == false) providers.Add(provider);
        }

        static void RegisterDefaults()
        {
            if (defaultsRegistered) return;

            defaultsRegistered = true;

            // AdsManagerNew registers itself when it starts on an Android device with configured IDs
        }

        /// <summary>
        /// True if any provider could show a rewarded video right now ( used only for hints, never to grant rewards )
        /// </summary>
        public static bool IsRewardedReady
        {
            get
            {
                RegisterDefaults();

                foreach (IECCAdProvider provider in providers)
                {
                    if (provider.IsRewardedReady) return true;
                }

                return false;
            }
        }

        /// <summary>
        /// True if a provider could show an interstitial right now ( its own cooldown included )
        /// </summary>
        public static bool IsInterstitialReady
        {
            get
            {
                RegisterDefaults();

                foreach (IECCAdProvider provider in providers)
                {
                    IECCInterstitialProvider interstitial = provider as IECCInterstitialProvider;

                    if (interstitial != null && interstitial.IsInterstitialReady) return true;
                }

                return false;
            }
        }

        /// <summary>
        /// Shows a rewarded video. onFinished(true) only after a verified rewarded completion.
        /// Returns false ( and doesn't call onFinished ) if another ad flow is already running
        /// </summary>
        public static bool ShowRewarded(string placement, Action<bool> onFinished)
        {
            if (IsBusy) return false;

            RegisterDefaults();

            IsBusy = true;

            ECCAdRunner.Instance.StartCoroutine(RewardedRoutine(placement, onFinished));

            return true;
        }

        static IEnumerator RewardedRoutine(string placement, Action<bool> onFinished)
        {
            bool finished = false;

            Action<bool> finish = delegate (bool rewarded)
            {
                // Duplicate SDK callbacks are ignored
                if (finished) return;

                finished = true;
                IsBusy = false;

                AudioListener.pause = false;

                if (onFinished != null) onFinished(rewarded);
            };

            foreach (IECCAdProvider provider in providers) provider.RequestRewarded();

            ECCAdLoadingPopup.Show();

            float startTime = Time.realtimeSinceStartup;

            IECCAdProvider ready = null;

            // Wait at least the minimum popup time, and until a provider is ready or the timeout passes
            while (true)
            {
                float elapsed = Time.realtimeSinceStartup - startTime;

                if (ready == null) ready = FindReadyProvider();

                // Stop waiting after the minimum popup time once a video is ready, every network reported a failure, or time is up
                if (elapsed >= ECCAdLoadingPopup.MinimumTime && (ready != null || AllRewardedFailed || elapsed >= ReadyTimeout)) break;

                yield return null;
            }

            ECCAdLoadingPopup.Hide();

            if (ready == null)
            {
                ECCNoAdsPanel.Show();

                finish(false);

                yield break;
            }

            AudioListener.pause = true;

            try
            {
                ready.ShowRewarded(delegate (ECCAdResult result)
                {
                    if (finished) return;

                    // Every network failed before presenting anything
                    if (result == ECCAdResult.Unavailable) ECCNoAdsPanel.Show();

                    finish(result == ECCAdResult.Rewarded);
                });
            }
            catch (Exception exception)
            {
                Debug.LogWarning("Rewarded ad failed to show: " + exception.Message);

                ECCNoAdsPanel.Show();

                finish(false);
            }
        }

        static bool AllRewardedFailed
        {
            get
            {
                foreach (IECCAdProvider provider in providers)
                {
                    if (provider.HasRewardedFailed == false) return false;
                }

                return true;
            }
        }

        /// <summary>
        /// Shows an interstitial at a natural break, with the same loading popup, readiness check and single-flow rule.
        /// done(true) when one was shown. Used by ECCPostVictoryFlow on «مرحله بعد» ( while it runs, IsInterstitialScheduled
        /// keeps other popups like the review prompt away )
        /// </summary>
        public static bool ShowInterstitial(string placement, Action<bool> done)
        {
            if (IsBusy) return false;

            RegisterDefaults();

            IsBusy = true;
            IsInterstitialScheduled = true;

            ECCAdRunner.Instance.StartCoroutine(InterstitialRoutine(done));

            return true;
        }

        static IEnumerator InterstitialRoutine(Action<bool> done)
        {
            bool finished = false;

            Action<bool> finish = delegate (bool shown)
            {
                if (finished) return;

                finished = true;
                IsBusy = false;
                IsInterstitialScheduled = false;

                AudioListener.pause = false;

                if (done != null) done(shown);
            };

            ECCAdLoadingPopup.Show();

            float startTime = Time.realtimeSinceStartup;
            IECCInterstitialProvider ready = null;

            while (true)
            {
                float elapsed = Time.realtimeSinceStartup - startTime;

                if (ready == null)
                {
                    foreach (IECCAdProvider provider in providers)
                    {
                        IECCInterstitialProvider interstitial = provider as IECCInterstitialProvider;

                        if (interstitial != null && interstitial.IsInterstitialReady)
                        {
                            ready = interstitial;
                            break;
                        }
                    }
                }

                if (elapsed >= ECCAdLoadingPopup.MinimumTime && (ready != null || elapsed >= ReadyTimeout || providers.Count == 0)) break;

                yield return null;
            }

            ECCAdLoadingPopup.Hide();

            // An unavailable interstitial is simply skipped ( no NoAds panel: the player didn't ask for it )
            if (ready == null)
            {
                finish(false);
                yield break;
            }

            AudioListener.pause = true;

            try
            {
                ready.ShowInterstitial(finish);
            }
            catch (Exception exception)
            {
                Debug.LogWarning("Interstitial failed to show: " + exception.Message);

                finish(false);
            }
        }

        static IECCAdProvider FindReadyProvider()
        {
            foreach (IECCAdProvider provider in providers)
            {
                if (provider.IsRewardedReady) return provider;
            }

            return null;
        }
    }

    /// <summary>
    /// Runs the ad coroutines on an object that survives scene loads
    /// </summary>
    public class ECCAdRunner : MonoBehaviour
    {
        static ECCAdRunner instance;

        public static ECCAdRunner Instance
        {
            get
            {
                if (instance == null)
                {
                    GameObject runner = new GameObject("ECCAdRunner");
                    DontDestroyOnLoad(runner);

                    instance = runner.AddComponent<ECCAdRunner>();
                }

                return instance;
            }
        }
    }
}
